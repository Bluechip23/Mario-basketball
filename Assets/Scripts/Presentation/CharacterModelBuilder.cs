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
                default: Humanoid(m, h, jersey, Skin); break;
            }
        }

        // ---- Shared humanoid base ------------------------------------------

        /// <summary>Legs + jersey torso + arms + head. Returns the head centre
        /// height and diameter (model space) so callers can add features.</summary>
        static (float headY, float headDia) Humanoid(Transform m, float h, Color jersey, Color skin, float width = 1f)
        {
            float legLen = 0.46f * h, legR = 0.12f * h * width;
            Capsule(m, new Vector3(-0.12f * h, legLen / 2f, 0f), new Vector3(legR * 2f, legLen / 2f, legR * 2f), Shorts, "legL");
            Capsule(m, new Vector3(0.12f * h, legLen / 2f, 0f), new Vector3(legR * 2f, legLen / 2f, legR * 2f), Shorts, "legR");
            Box(m, new Vector3(-0.12f * h, 0.02f * h, 0.04f * h), new Vector3(legR * 2.2f, 0.05f * h, legR * 3f), Shoe, "shoeL");
            Box(m, new Vector3(0.12f * h, 0.02f * h, 0.04f * h), new Vector3(legR * 2.2f, 0.05f * h, legR * 3f), Shoe, "shoeR");

            float torsoLen = 0.32f * h, torsoR = 0.2f * h * width, torsoY = legLen + torsoLen / 2f;
            Capsule(m, new Vector3(0f, torsoY, 0f), new Vector3(torsoR * 2f, torsoLen / 2f + 0.05f * h, torsoR * 1.5f), jersey, "torso");

            float armLen = 0.42f * h, armR = 0.07f * h * width;
            Capsule(m, new Vector3(-(torsoR + armR * 0.8f), torsoY, 0f), new Vector3(armR * 2f, armLen / 2f, armR * 2f), skin, "armL");
            Capsule(m, new Vector3(torsoR + armR * 0.8f, torsoY, 0f), new Vector3(armR * 2f, armLen / 2f, armR * 2f), skin, "armR");

            float headDia = 0.27f * h, headY = legLen + torsoLen + headDia * 0.42f;
            Sphere(m, new Vector3(0f, headY, 0f), headDia, skin, "head");
            return (headY, headDia);
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
            Capsule(m, new Vector3(-(torsoR + armR), torsoY, 0f), new Vector3(armR * 2f, armLen / 2f, armR * 2f), Skin, "armL");
            Capsule(m, new Vector3(torsoR + armR, torsoY, 0f), new Vector3(armR * 2f, armLen / 2f, armR * 2f), Skin, "armR");

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

        static Mesh ConeMesh()
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
