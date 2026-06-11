using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Characters;

namespace MarioBasketball.Presentation
{
    /// <summary>
    /// Builds a recognizable <b>placeholder</b> model for each character out of
    /// primitives (plus a procedural cone for horns/crowns/spikes). These are
    /// clearly dev-art silhouettes — color-coded with the team jersey tint and a
    /// few signature features per character — not final art.
    ///
    /// Built in a "model" space where y=0 is the feet and y=h is the top of the
    /// head, parented under the player root (whose origin is the controller
    /// centre). Swap this whole module for authored models/animation later.
    /// </summary>
    public static class CharacterModelBuilder
    {
        static readonly Color Skin = new Color(1f, 0.80f, 0.62f);
        static readonly Color Shorts = new Color(0.14f, 0.14f, 0.18f);
        static readonly Color Shoe = new Color(0.5f, 0.25f, 0.1f);
        static readonly Color Dark = new Color(0.2f, 0.13f, 0.08f);

        static Shader _shader;
        static Shader DefaultShader => _shader != null ? _shader
            : (_shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));

        static Mesh _cone;

        public static void Build(Transform root, CharacterStats stats, float h, Color jersey)
        {
            var go = new GameObject("Model");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, -h / 2f, 0f);
            Transform m = go.transform;

            switch (stats.characterName)
            {
                case "Bowser": Bowser(m, h, jersey); break;
                case "Donkey Kong": Ape(m, h, jersey, big: true); break;
                case "Diddy Kong": Ape(m, h, jersey, big: false, cap: new Color(0.85f, 0.1f, 0.1f)); break;
                case "Mario": Plumber(m, h, jersey, new Color(0.85f, 0.1f, 0.1f)); break;
                case "Luigi": Plumber(m, h, jersey, new Color(0.12f, 0.5f, 0.16f)); break;
                case "Wario": Plumber(m, h, jersey, new Color(0.95f, 0.8f, 0.1f), width: 1.3f); break;
                case "Waluigi": Plumber(m, h, jersey, new Color(0.45f, 0.15f, 0.6f), width: 0.8f); break;
                case "Baby Mario": Baby(m, h, jersey, new Color(0.85f, 0.1f, 0.1f)); break;
                case "Peach": Princess(m, h, jersey, new Color(1f, 0.86f, 0.25f)); break;
                case "Daisy": Princess(m, h, jersey, new Color(0.6f, 0.35f, 0.12f)); break;
                case "Toad": Toadstool(m, h, jersey); break;
                case "Yoshi": Yoshi(m, h, jersey); break;
                case "Birdo": Birdo(m, h, jersey); break;
                case "Boo": Boo(m, h); break;
                case "Piranha Plant": Piranha(m, h); break;
                case "Monty Mole": Mole(m, h, jersey); break;
                case "Koopa": Turtle(m, h, jersey); break;
                case "Kritter": Croc(m, h, jersey); break;
                case "Shyguy": Shyguy(m, h, jersey); break;
                default: Humanoid(m, h, jersey, Skin); break;
            }
        }

        // ---- Shared humanoid base ------------------------------------------

        /// <summary>Legs + jersey torso + two-segment arms + head. Limbs hang
        /// from named joint pivots ("JointArmL", "JointElbowR", "JointKneeL",
        /// "JointWristR", "JointLegR"…) so <c>ProceduralAnimator</c> can swing
        /// them at the shoulder/elbow/wrist/hip/knee. Returns the head centre
        /// height and diameter (model space) so callers can add features.</summary>
        static (float headY, float headDia) Humanoid(Transform m, float h, Color jersey, Color skin, float width = 1f)
        {
            // Leaner, more athletic proportions (slimmer than a blockout).
            float legLen = 0.50f * h, legR = 0.075f * h * width, hipX = 0.085f * h;
            Leg(m, "L", new Vector3(-hipX, legLen, 0f), legLen, legR, h, skin);
            Leg(m, "R", new Vector3(hipX, legLen, 0f), legLen, legR, h, skin);

            float torsoLen = 0.30f * h, torsoR = 0.135f * h * width, torsoY = legLen + torsoLen / 2f;
            // Tapered torso: broader shoulders, narrower waist (two stacked capsules).
            Capsule(m, new Vector3(0f, torsoY - torsoLen * 0.18f, 0f), new Vector3(torsoR * 1.8f, torsoLen / 2f, torsoR * 1.25f), jersey, "waist");
            Capsule(m, new Vector3(0f, torsoY + torsoLen * 0.22f, 0f), new Vector3(torsoR * 2.2f, torsoLen / 2.6f, torsoR * 1.35f), jersey, "chest");

            float armLen = 0.46f * h, armR = 0.046f * h * width;
            float shoulderY = torsoY + torsoLen * 0.34f;
            float shoulderX = torsoR * 1.05f + armR;
            // Arms pivot at the shoulder and hang down at rest.
            Arm(m, "L", new Vector3(-shoulderX, shoulderY, 0f), armLen, armR, skin);
            Arm(m, "R", new Vector3(shoulderX, shoulderY, 0f), armLen, armR, skin);

            float neck = 0.05f * h;
            Capsule(m, new Vector3(0f, torsoY + torsoLen * 0.55f, 0f), new Vector3(torsoR * 0.7f, neck, torsoR * 0.7f), skin, "neck");

            float headDia = 0.23f * h, headY = legLen + torsoLen + headDia * 0.5f;
            Sphere(m, new Vector3(0f, headY, 0f), headDia, skin, "head");
            return (headY, headDia);
        }

        static Transform Joint(Transform parent, string name, Vector3 localPos)
        {
            var j = new GameObject(name);
            j.transform.SetParent(parent, false);
            j.transform.localPosition = localPos;
            return j.transform;
        }

        /// <summary>Two-segment arm: shoulder joint → upper arm, elbow joint →
        /// forearm, wrist joint → hand, so the animator can bend the elbow and
        /// flick the wrist for dribbling and shooting form.</summary>
        static void Arm(Transform m, string side, Vector3 shoulderPos, float armLen, float armR, Color skin)
        {
            Transform sh = Joint(m, "JointArm" + side, shoulderPos);
            float upper = armLen * 0.52f, lower = armLen * 0.48f;
            Capsule(sh, new Vector3(0f, -upper / 2f, 0f), new Vector3(armR * 2f, upper / 2f, armR * 2f), skin, "upperArm" + side);
            Transform el = Joint(sh, "JointElbow" + side, new Vector3(0f, -upper, 0f));
            Capsule(el, new Vector3(0f, -lower / 2f, 0f), new Vector3(armR * 1.8f, lower / 2f, armR * 1.8f), skin, "forearm" + side);
            Hand(el, side, lower, armR, skin);
        }

        /// <summary>Articulated hand: wrist joint → flattened palm + four
        /// fingers + an inward thumb (replaces the old sphere mitt).</summary>
        static void Hand(Transform el, string side, float lower, float armR, Color skin)
        {
            Transform wr = Joint(el, "JointWrist" + side, new Vector3(0f, -lower, 0f));
            var palm = Sphere(wr, new Vector3(0f, -armR * 0.9f, 0f), armR * 2.2f, skin, "palm" + side);
            palm.transform.localScale = new Vector3(armR * 2.0f, armR * 2.4f, armR * 1.2f);
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * armR * 0.5f;
                Capsule(wr, new Vector3(x, -armR * 2.2f, 0f), new Vector3(armR * 0.42f, armR * 0.6f, armR * 0.42f), skin, "finger" + side);
            }
            // Thumb angles in toward the body.
            float inward = side == "L" ? 1f : -1f;
            var thumb = Capsule(wr, new Vector3(inward * armR * 1.0f, -armR * 1.2f, armR * 0.3f), new Vector3(armR * 0.42f, armR * 0.55f, armR * 0.42f), skin, "thumb" + side);
            thumb.transform.localEulerAngles = new Vector3(0f, 0f, inward * -40f);
        }

        /// <summary>Two-segment leg: hip joint → thigh (shorts), knee joint →
        /// shin (skin) + shoe, so the animator can bend the knee in the run
        /// cycle and tuck the legs in the air.</summary>
        static void Leg(Transform m, string side, Vector3 hipPos, float legLen, float legR, float h, Color skin)
        {
            Transform hip = Joint(m, "JointLeg" + side, hipPos);
            float thigh = legLen * 0.52f, shin = legLen * 0.48f;
            Capsule(hip, new Vector3(0f, -thigh / 2f, 0f), new Vector3(legR * 2f, thigh / 2f, legR * 2f), Shorts, "thigh" + side);
            Transform knee = Joint(hip, "JointKnee" + side, new Vector3(0f, -thigh, 0f));
            Capsule(knee, new Vector3(0f, -shin / 2f, 0f), new Vector3(legR * 1.8f, shin / 2f, legR * 1.8f), skin, "shin" + side);
            Box(knee, new Vector3(0f, -shin + 0.02f * h, 0.03f * h), new Vector3(legR * 2.2f, 0.04f * h, legR * 3.2f), Shoe, "shoe" + side);
        }

        // ---- Characters ----------------------------------------------------

        static void Plumber(Transform m, float h, Color jersey, Color cap, float width = 1f)
        {
            var (hy, hd) = Humanoid(m, h, jersey, Skin, width);
            Sphere(m, new Vector3(0f, hy + hd * 0.32f, -0.04f * h), hd * 1.05f, cap, "cap");
            Box(m, new Vector3(0f, hy + hd * 0.18f, hd * 0.55f), new Vector3(hd * 1.1f, hd * 0.14f, hd * 0.55f), cap, "brim");
            Sphere(m, new Vector3(0f, hy, hd * 0.55f), hd * 0.5f, Skin, "nose");
            Box(m, new Vector3(0f, hy - hd * 0.22f, hd * 0.5f), new Vector3(hd * 0.85f, hd * 0.16f, hd * 0.28f), Dark, "stache");
        }

        static void Baby(Transform m, float h, Color jersey, Color cap)
        {
            // Big head, tiny body.
            var (hy, hd) = Humanoid(m, h * 0.82f, jersey, Skin, 0.95f);
            Sphere(m, new Vector3(0f, hy + hd * 0.1f, 0f), hd * 1.5f, Skin, "bigHead");
            Sphere(m, new Vector3(0f, hy + hd * 0.55f, -0.04f * h), hd * 1.5f, cap, "cap");
            Sphere(m, new Vector3(0f, hy + hd * 0.05f, hd * 0.7f), hd * 0.55f, Skin, "nose");
        }

        static void Princess(Transform m, float h, Color jersey, Color hair)
        {
            float legLen = 0.30f * h;
            // Dress (cone skirt, jersey-tinted) instead of legs.
            Cone(m, new Vector3(0f, legLen, 0f), new Vector3(0.5f * h, 0.42f * h, 0.5f * h), jersey, Vector3.zero, "skirt");
            float torsoY = legLen + 0.34f * h, torsoR = 0.13f * h;
            Capsule(m, new Vector3(0f, torsoY, 0f), new Vector3(torsoR * 2f, 0.13f * h, torsoR * 1.5f), jersey, "torso");
            float armLen = 0.38f * h, armR = 0.055f * h;
            Arm(m, "L", new Vector3(-(torsoR + armR), torsoY + armLen / 2f, 0f), armLen, armR, Skin);
            Arm(m, "R", new Vector3(torsoR + armR, torsoY + armLen / 2f, 0f), armLen, armR, Skin);

            float headDia = 0.24f * h, headY = legLen + 0.5f * h;
            Sphere(m, new Vector3(0f, headY, 0f), headDia, Skin, "head");
            // Hair: back blob + side locks.
            Sphere(m, new Vector3(0f, headY + headDia * 0.1f, -headDia * 0.5f), headDia * 1.1f, hair, "hair");
            Capsule(m, new Vector3(0f, headY - headDia * 0.4f, -headDia * 0.7f), new Vector3(headDia * 0.5f, headDia * 0.7f, headDia * 0.5f), hair, "ponytail");
            // Crown.
            var gold = new Color(1f, 0.84f, 0.1f);
            for (int i = 0; i < 5; i++)
            {
                float a = (i / 5f) * Mathf.PI * 2f;
                Cone(m, new Vector3(Mathf.Cos(a) * headDia * 0.45f, headY + headDia * 0.55f, Mathf.Sin(a) * headDia * 0.45f),
                    new Vector3(headDia * 0.18f, headDia * 0.3f, headDia * 0.18f), gold, Vector3.zero, "crown");
            }
        }

        static void Toadstool(Transform m, float h, Color jersey)
        {
            var (hy, hd) = Humanoid(m, h, jersey, Skin, 1f);
            // Oversized mushroom cap.
            var cap = Sphere(m, new Vector3(0f, hy + hd * 0.45f, 0f), hd * 2.4f, new Color(0.97f, 0.97f, 0.97f), "cap");
            cap.transform.localScale = new Vector3(hd * 2.4f, hd * 1.4f, hd * 2.4f);
            var red = new Color(0.88f, 0.15f, 0.15f);
            Sphere(m, new Vector3(hd * 0.75f, hy + hd * 0.55f, hd * 0.3f), hd * 0.6f, red, "spot");
            Sphere(m, new Vector3(-hd * 0.7f, hy + hd * 0.55f, -hd * 0.3f), hd * 0.55f, red, "spot");
            Sphere(m, new Vector3(0f, hy + hd * 0.7f, hd * 0.8f), hd * 0.5f, red, "spot");
        }

        static void Ape(Transform m, float h, Color jersey, bool big, Color? cap = null)
        {
            var fur = new Color(0.32f, 0.2f, 0.12f);
            var (hy, hd) = Humanoid(m, h, jersey, fur, big ? 1.25f : 0.95f);
            // Muzzle.
            Box(m, new Vector3(0f, hy - hd * 0.15f, hd * 0.5f), new Vector3(hd * 0.8f, hd * 0.55f, hd * 0.5f), new Color(0.78f, 0.62f, 0.45f), "muzzle");
            // Ears.
            Sphere(m, new Vector3(-hd * 0.55f, hy + hd * 0.2f, 0f), hd * 0.45f, fur, "earL");
            Sphere(m, new Vector3(hd * 0.55f, hy + hd * 0.2f, 0f), hd * 0.45f, fur, "earR");
            if (cap.HasValue)
                Sphere(m, new Vector3(0f, hy + hd * 0.35f, -0.02f * h), hd * 1.0f, cap.Value, "cap");
        }

        static void Yoshi(Transform m, float h, Color jersey)
        {
            var green = new Color(0.25f, 0.72f, 0.27f);
            var (hy, hd) = Humanoid(m, h, green, green, 1.05f);
            // White belly + jersey saddle.
            Sphere(m, new Vector3(0f, hy - hd * 1.3f, hd * 0.55f), hd * 1.0f, new Color(0.97f, 0.95f, 0.85f), "belly");
            Box(m, new Vector3(0f, hy - hd * 1.0f, -hd * 0.6f), new Vector3(hd * 1.6f, hd * 0.5f, hd * 0.4f), jersey, "saddle");
            // Snout + nostrils.
            Capsule(m, new Vector3(0f, hy - hd * 0.1f, hd * 0.7f), new Vector3(hd * 0.7f, hd * 0.4f, hd * 0.7f), green, "snout");
            // Tail.
            Cone(m, new Vector3(0f, hd * 0.6f, -hd * 0.9f), new Vector3(hd * 0.7f, hd * 1.0f, hd * 0.7f), green, new Vector3(-90f, 0f, 0f), "tail");
        }

        static void Birdo(Transform m, float h, Color jersey)
        {
            var pink = new Color(0.95f, 0.45f, 0.6f);
            var (hy, hd) = Humanoid(m, h, jersey, pink, 1.0f);
            // Big round snout (the signature).
            Sphere(m, new Vector3(0f, hy - hd * 0.1f, hd * 0.7f), hd * 1.1f, pink, "snout");
            Sphere(m, new Vector3(0f, hy - hd * 0.1f, hd * 1.15f), hd * 0.45f, new Color(0.8f, 0.2f, 0.3f), "mouth");
            // Bow on top.
            var red = new Color(0.85f, 0.15f, 0.2f);
            Box(m, new Vector3(-hd * 0.5f, hy + hd * 0.5f, 0f), new Vector3(hd * 0.5f, hd * 0.4f, hd * 0.2f), red, "bowL");
            Box(m, new Vector3(hd * 0.5f, hy + hd * 0.5f, 0f), new Vector3(hd * 0.5f, hd * 0.4f, hd * 0.2f), red, "bowR");
        }

        static void Bowser(Transform m, float h, Color jersey)
        {
            var skin = new Color(0.82f, 0.85f, 0.35f);
            var (hy, hd) = Humanoid(m, h, jersey, skin, 1.35f);
            // Shell on the back.
            var shell = Sphere(m, new Vector3(0f, hy - hd * 1.6f, -0.16f * h), hd * 2.4f, new Color(0.15f, 0.45f, 0.2f), "shell");
            shell.transform.localScale = new Vector3(hd * 2.4f, hd * 2.2f, hd * 1.6f);
            // Shell spikes.
            var cream = new Color(0.92f, 0.86f, 0.7f);
            Cone(m, new Vector3(0f, hy - hd * 1.6f, -0.28f * h), new Vector3(hd * 0.5f, hd * 0.7f, hd * 0.5f), cream, new Vector3(-90f, 0f, 0f), "spike");
            Cone(m, new Vector3(-hd * 0.8f, hy - hd * 1.2f, -0.26f * h), new Vector3(hd * 0.4f, hd * 0.6f, hd * 0.4f), cream, new Vector3(-90f, 0f, 0f), "spike");
            Cone(m, new Vector3(hd * 0.8f, hy - hd * 1.2f, -0.26f * h), new Vector3(hd * 0.4f, hd * 0.6f, hd * 0.4f), cream, new Vector3(-90f, 0f, 0f), "spike");
            // Horns.
            Cone(m, new Vector3(-hd * 0.45f, hy + hd * 0.5f, 0f), new Vector3(hd * 0.3f, hd * 0.6f, hd * 0.3f), cream, Vector3.zero, "hornL");
            Cone(m, new Vector3(hd * 0.45f, hy + hd * 0.5f, 0f), new Vector3(hd * 0.3f, hd * 0.6f, hd * 0.3f), cream, Vector3.zero, "hornR");
            // Snout + red mane.
            Box(m, new Vector3(0f, hy - hd * 0.2f, hd * 0.5f), new Vector3(hd * 0.7f, hd * 0.4f, hd * 0.5f), skin, "snout");
            Sphere(m, new Vector3(0f, hy + hd * 0.1f, -hd * 0.5f), hd * 1.0f, new Color(0.85f, 0.35f, 0.05f), "mane");
        }

        static void Boo(Transform m, float h)
        {
            // Floating round ghost — no legs.
            var white = new Color(0.95f, 0.95f, 0.97f);
            float bodyDia = 0.7f * h;
            var body = Sphere(m, new Vector3(0f, 0.55f * h, 0f), bodyDia, white, "body");
            body.transform.localScale = new Vector3(bodyDia, bodyDia * 1.1f, bodyDia);
            // Little tail wisps.
            Sphere(m, new Vector3(-0.18f * h, 0.28f * h, 0f), 0.18f * h, white, "wisp");
            Sphere(m, new Vector3(0.18f * h, 0.28f * h, 0f), 0.18f * h, white, "wisp");
            // Stubby arms.
            Sphere(m, new Vector3(-0.36f * h, 0.6f * h, 0.1f * h), 0.16f * h, white, "armL");
            Sphere(m, new Vector3(0.36f * h, 0.6f * h, 0.1f * h), 0.16f * h, white, "armR");
            // Big mouth + tongue + eyes.
            Box(m, new Vector3(0f, 0.5f * h, 0.34f * h), new Vector3(0.28f * h, 0.16f * h, 0.05f * h), new Color(0.15f, 0.08f, 0.12f), "mouth");
            Sphere(m, new Vector3(-0.12f * h, 0.66f * h, 0.32f * h), 0.07f * h, Dark, "eyeL");
            Sphere(m, new Vector3(0.12f * h, 0.66f * h, 0.32f * h), 0.07f * h, Dark, "eyeR");
        }

        static void Piranha(Transform m, float h)
        {
            // Warp-pipe base + stem + bulb head with toothy mouth.
            var pipe = new Color(0.2f, 0.7f, 0.55f);
            Cyl(m, new Vector3(0f, 0.12f * h, 0f), new Vector3(0.46f * h, 0.12f * h, 0.46f * h), pipe, "pipe");
            var stem = new Color(0.25f, 0.6f, 0.28f);
            Cyl(m, new Vector3(0f, 0.45f * h, 0f), new Vector3(0.2f * h, 0.32f * h, 0.2f * h), stem, "stem");
            // Leaves.
            Box(m, new Vector3(-0.2f * h, 0.5f * h, 0f), new Vector3(0.28f * h, 0.06f * h, 0.16f * h), stem, "leafL");
            Box(m, new Vector3(0.2f * h, 0.6f * h, 0f), new Vector3(0.28f * h, 0.06f * h, 0.16f * h), stem, "leafR");
            // Bulb head.
            float headDia = 0.5f * h, headY = 0.85f * h;
            Sphere(m, new Vector3(0f, headY, 0f), headDia, new Color(0.85f, 0.12f, 0.12f), "head");
            Sphere(m, new Vector3(headDia * 0.3f, headY + headDia * 0.25f, headDia * 0.25f), headDia * 0.28f, Color.white, "spot");
            Sphere(m, new Vector3(-headDia * 0.35f, headY - headDia * 0.1f, headDia * 0.25f), headDia * 0.28f, Color.white, "spot");
            // Mouth.
            Box(m, new Vector3(0f, headY - headDia * 0.15f, headDia * 0.45f), new Vector3(headDia * 0.7f, headDia * 0.22f, headDia * 0.2f), new Color(0.95f, 0.95f, 0.9f), "mouth");
        }

        static void Mole(Transform m, float h, Color jersey)
        {
            var fur = new Color(0.42f, 0.28f, 0.16f);
            var (hy, hd) = Humanoid(m, h, jersey, fur, 1.0f);
            // Big pink snout + dark shades + claws.
            Sphere(m, new Vector3(0f, hy - hd * 0.1f, hd * 0.6f), hd * 0.7f, new Color(0.95f, 0.6f, 0.6f), "snout");
            Box(m, new Vector3(0f, hy + hd * 0.18f, hd * 0.5f), new Vector3(hd * 1.0f, hd * 0.28f, hd * 0.18f), new Color(0.1f, 0.1f, 0.12f), "shades");
            Sphere(m, new Vector3(-(0.2f * h + 0.07f * h), 0.32f * h, 0.08f * h), 0.1f * h, new Color(0.9f, 0.9f, 0.92f), "clawL");
            Sphere(m, new Vector3(0.2f * h + 0.07f * h, 0.32f * h, 0.08f * h), 0.1f * h, new Color(0.9f, 0.9f, 0.92f), "clawR");
        }

        static void Turtle(Transform m, float h, Color jersey)
        {
            var green = new Color(0.3f, 0.7f, 0.32f);
            var yellow = new Color(0.95f, 0.85f, 0.3f);
            var (hy, hd) = Humanoid(m, h, jersey, green, 1.05f);
            // Yellow belly + green shell on the back.
            Sphere(m, new Vector3(0f, hy - hd * 1.3f, hd * 0.5f), hd * 1.0f, yellow, "belly");
            var shell = Sphere(m, new Vector3(0f, hy - hd * 1.4f, -0.14f * h), hd * 1.9f, new Color(0.55f, 0.4f, 0.15f), "shell");
            shell.transform.localScale = new Vector3(hd * 1.9f, hd * 1.9f, hd * 1.3f);
            // Beak + eyes.
            Cone(m, new Vector3(0f, hy - hd * 0.05f, hd * 0.55f), new Vector3(hd * 0.35f, hd * 0.4f, hd * 0.35f), yellow, new Vector3(90f, 0f, 0f), "beak");
            Sphere(m, new Vector3(-hd * 0.25f, hy + hd * 0.2f, hd * 0.45f), hd * 0.18f, Dark, "eyeL");
            Sphere(m, new Vector3(hd * 0.25f, hy + hd * 0.2f, hd * 0.45f), hd * 0.18f, Dark, "eyeR");
        }

        static void Croc(Transform m, float h, Color jersey)
        {
            var green = new Color(0.3f, 0.62f, 0.32f);
            var (hy, hd) = Humanoid(m, h, jersey, green, 1.2f);
            // Long snout with teeth + brow ridge.
            Box(m, new Vector3(0f, hy - hd * 0.15f, hd * 0.7f), new Vector3(hd * 0.8f, hd * 0.45f, hd * 1.1f), green, "snout");
            Box(m, new Vector3(0f, hy - hd * 0.32f, hd * 0.9f), new Vector3(hd * 0.7f, hd * 0.12f, hd * 0.7f), Color.white, "teeth");
            Box(m, new Vector3(0f, hy + hd * 0.25f, hd * 0.3f), new Vector3(hd * 0.9f, hd * 0.18f, hd * 0.3f), new Color(0.22f, 0.5f, 0.24f), "brow");
            Sphere(m, new Vector3(-hd * 0.3f, hy + hd * 0.4f, hd * 0.25f), hd * 0.22f, Color.white, "eyeL");
            Sphere(m, new Vector3(hd * 0.3f, hy + hd * 0.4f, hd * 0.25f), hd * 0.22f, Color.white, "eyeR");
        }

        static void Shyguy(Transform m, float h, Color jersey)
        {
            // Robe body (jersey) + white mask with eye holes.
            var (hy, hd) = Humanoid(m, h, jersey, jersey, 1.0f);
            var mask = Sphere(m, new Vector3(0f, hy, hd * 0.15f), hd * 1.15f, new Color(0.92f, 0.86f, 0.74f), "mask");
            mask.transform.localScale = new Vector3(hd * 1.1f, hd * 1.2f, hd * 0.9f);
            Sphere(m, new Vector3(-hd * 0.28f, hy + hd * 0.05f, hd * 0.5f), hd * 0.22f, Dark, "eyeL");
            Sphere(m, new Vector3(hd * 0.28f, hy + hd * 0.05f, hd * 0.5f), hd * 0.22f, Dark, "eyeR");
            Box(m, new Vector3(0f, hy - hd * 0.35f, hd * 0.5f), new Vector3(hd * 0.3f, hd * 0.18f, hd * 0.12f), Dark, "mouth");
            // Belt + little hood point.
            Box(m, new Vector3(0f, hy - hd * 1.7f, 0f), new Vector3(hd * 1.7f, hd * 0.2f, hd * 1.5f), new Color(0.85f, 0.7f, 0.2f), "belt");
            Cone(m, new Vector3(0f, hy + hd * 0.7f, -0.02f * h), new Vector3(hd * 0.5f, hd * 0.5f, hd * 0.5f), jersey, Vector3.zero, "hood");
        }

        // ---- Primitive helpers ---------------------------------------------

        static GameObject Prim(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color c, string name)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            var col = g.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            SetColor(g.GetComponent<Renderer>(), c);
            return g;
        }

        static GameObject Sphere(Transform p, Vector3 pos, float dia, Color c, string n) =>
            Prim(p, PrimitiveType.Sphere, pos, Vector3.one * dia, c, n);
        static GameObject Box(Transform p, Vector3 pos, Vector3 s, Color c, string n) =>
            Prim(p, PrimitiveType.Cube, pos, s, c, n);
        static GameObject Capsule(Transform p, Vector3 pos, Vector3 s, Color c, string n) =>
            Prim(p, PrimitiveType.Capsule, pos, s, c, n);
        static GameObject Cyl(Transform p, Vector3 pos, Vector3 s, Color c, string n) =>
            Prim(p, PrimitiveType.Cylinder, pos, s, c, n);

        static GameObject Cone(Transform p, Vector3 pos, Vector3 scale, Color c, Vector3 euler, string n)
        {
            var g = new GameObject(n);
            g.transform.SetParent(p, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.transform.localEulerAngles = euler;
            g.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
            var mr = g.AddComponent<MeshRenderer>();
            mr.material = new Material(DefaultShader);
            SetColor(mr, c);
            return g;
        }

        static void SetColor(Renderer r, Color c)
        {
            if (r == null) return;
            var mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        }

        /// <summary>Shared procedural cone (also used for the hoop nets).</summary>
        public static Mesh ConeMesh()
        {
            if (_cone != null) return _cone;
            const int seg = 12;
            var verts = new List<Vector3> { new Vector3(0f, 1f, 0f) }; // apex
            int baseStart = verts.Count;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
            }
            int centre = verts.Count;
            verts.Add(Vector3.zero);

            var tris = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                int a = baseStart + i;
                int b = baseStart + (i + 1) % seg;
                tris.Add(0); tris.Add(b); tris.Add(a);          // side
                tris.Add(centre); tris.Add(a); tris.Add(b);     // base cap
            }

            _cone = new Mesh { name = "Cone" };
            _cone.SetVertices(verts);
            _cone.SetTriangles(tris, 0);
            _cone.RecalculateNormals();
            return _cone;
        }
    }
}
