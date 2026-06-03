using System.Collections.Generic;
using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Characters;
using MarioBasketball.Gameplay;
using MarioBasketball.CameraControl;
using MarioBasketball.UI;

namespace MarioBasketball.Bootstrap
{
    /// <summary>
    /// Builds a complete, playable full-court 3v3 game out of primitives at
    /// runtime: floor, perimeter walls (there is no out of bounds — the ball
    /// and players bounce off), painted lane / three-point arcs / centre
    /// circle, two hoops with rims, two five-player rosters (three on court,
    /// two on the bench each), the ball, camera, <see cref="GameManager"/> and
    /// HUD.
    ///
    /// Running from an empty scene with just this component keeps the prototype
    /// free of fragile authored scene/prefab assets. As real content arrives,
    /// this bootstrap shrinks and is retired.
    ///
    /// Every player currently uses Bowser's stat sheet (the only character that
    /// exists); only the home point guard is human, the rest stand until AI is
    /// built.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Court dimensions (metres)")]
        public float courtLength = 28f;
        public float courtWidth = 15f;
        public float rimHeight = 3.05f;
        public float threePointRadius = 6.75f;

        static readonly Color HomeColor = new Color(0.85f, 0.15f, 0.15f);
        static readonly Color AwayColor = new Color(0.15f, 0.35f, 0.9f);
        static readonly Color HumanColor = new Color(1.0f, 0.78f, 0.1f);

        Material _lineMat;

        void Awake()
        {
            BuildLighting();
            BuildCourt();
            BuildWalls();
            BuildMarkings();

            // Teams start on their own half and attack the far basket: home
            // begins on the -z half and attacks the +z hoop, and vice versa.
            Hoop hoopNeg = BuildHoop("HoopNeg", new Vector3(0f, 0f, -(courtLength / 2f - 1.6f)), TeamSide.Away, faceZ: 1f);
            Hoop hoopPos = BuildHoop("HoopPos", new Vector3(0f, 0f, courtLength / 2f - 1.6f), TeamSide.Home, faceZ: -1f);

            var gm = gameObject.AddComponent<GameManager>();
            gm.hoops.Add(hoopNeg);
            gm.hoops.Add(hoopPos);

            // Distinct lineups so the archetypes are on the floor to balance
            // against. Slot 1 (index, middle) is the human-controlled starter.
            var homeRoster = new[]
            {
                CharacterLibrary.Luigi(), CharacterLibrary.Mario(), CharacterLibrary.Peach(),
                CharacterLibrary.Toad(), CharacterLibrary.DiddyKong()
            };
            var awayRoster = new[]
            {
                CharacterLibrary.DonkeyKong(), CharacterLibrary.Bowser(), CharacterLibrary.Waluigi(),
                CharacterLibrary.DiddyKong(), CharacterLibrary.Toad()
            };
            BuildTeam(gm.Home, TeamSide.Home, HomeColor, -1f, true, gm, homeRoster);
            BuildTeam(gm.Away, TeamSide.Away, AwayColor, 1f, false, gm, awayRoster);

            gm.ball = BuildBall(new Vector3(0f, 1.1f, 0f));

            // Substitution anchors: benches sit outside each sideline.
            gm.homeBenchAnchor = new Vector3(-(courtWidth / 2f + 1.5f), 1.1f, -2f);
            gm.awayBenchAnchor = new Vector3(courtWidth / 2f + 1.5f, 1.1f, 2f);
            gm.homeSubEntry = new Vector3(0f, 1.1f, -4f);
            gm.awaySubEntry = new Vector3(0f, 1.1f, 4f);

            BuildCamera(gm.humanPlayer != null ? gm.humanPlayer.transform : null);

            var switcher = gameObject.AddComponent<MarioBasketball.Control.PlayerSwitchManager>();
            switcher.humanSide = TeamSide.Home;
            switcher.initial = gm.humanPlayer;

            gameObject.AddComponent<DebugHUD>();
            gameObject.AddComponent<DebugMatchControls>();
        }

        // ---- Teams ---------------------------------------------------------

        void BuildTeam(TeamState team, TeamSide side, Color color, float halfSign, bool hasHuman, GameManager gm, CharacterStats[] roster)
        {
            // Three on-court spots spread across this team's half, two on bench.
            float[] xs = { -3f, 0f, 3f };
            for (int i = 0; i < 3; i++)
            {
                bool isHuman = hasHuman && i == 1; // the middle slot is the human starter
                var pos = new Vector3(xs[i], 1.1f, 4f * halfSign);
                var pc = BuildPlayer(roster[i], pos, side, isHuman ? HumanColor : color, isHuman, benched: false);
                team.onCourt.Add(pc);
                if (isHuman) gm.humanPlayer = pc;
            }

            float benchX = (courtWidth / 2f + 1.5f) * halfSign;
            for (int i = 0; i < 2; i++)
            {
                var pos = new Vector3(benchX, 1.1f, -2f + i * 2f);
                var pc = BuildPlayer(roster[3 + i], pos, side, color, isHuman: false, benched: true);
                team.bench.Add(pc);
            }
        }

        PlayerController BuildPlayer(CharacterStats stats, Vector3 pos, TeamSide side, Color color, bool isHuman, bool benched)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{side}_{stats.characterName}";
            go.SetActive(false); // configure before Awake/OnEnable run
            go.transform.position = pos;
            Tint(go, color);
            Destroy(go.GetComponent<CapsuleCollider>()); // CharacterController replaces it

            var cc = go.AddComponent<CharacterController>();
            cc.center = Vector3.zero;
            cc.height = 2f;
            cc.radius = 0.4f;

            var character = go.AddComponent<PlayerCharacter>();
            character.stats = stats;

            var pc = go.AddComponent<PlayerController>();
            pc.team = side;
            pc.isHuman = false; // control is assigned at runtime by PlayerSwitchManager
            pc.threePointDistance = threePointRadius;

            // Every player has a brain; it yields on whoever the human controls.
            go.AddComponent<MarioBasketball.AI.PlayerAI>();

            go.SetActive(true);

            if (benched)
            {
                character.IsBenched = true;
                pc.enabled = false;
            }
            return pc;
        }

        // ---- Court geometry ------------------------------------------------

        void BuildCourt()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Court";
            floor.transform.localScale = new Vector3(courtWidth / 10f, 1f, courtLength / 10f);
            Tint(floor, new Color(0.62f, 0.42f, 0.24f)); // hardwood
        }

        void BuildWalls()
        {
            const float t = 0.5f;
            const float h = 3f;
            float hw = courtWidth / 2f;
            float hl = courtLength / 2f;

            CreateWall("WallRight", new Vector3(hw + t / 2f, h / 2f, 0f), new Vector3(t, h, courtLength + t * 2f));
            CreateWall("WallLeft", new Vector3(-hw - t / 2f, h / 2f, 0f), new Vector3(t, h, courtLength + t * 2f));
            CreateWall("WallFar", new Vector3(0f, h / 2f, hl + t / 2f), new Vector3(courtWidth, h, t));
            CreateWall("WallNear", new Vector3(0f, h / 2f, -hl - t / 2f), new Vector3(courtWidth, h, t));
        }

        void CreateWall(string label, Vector3 centre, Vector3 size)
        {
            var go = new GameObject(label);
            go.transform.position = centre;
            var col = go.AddComponent<BoxCollider>();
            col.size = size; // invisible collider: ball and players bounce/stop
        }

        void BuildMarkings()
        {
            float hw = courtWidth / 2f;
            float hl = courtLength / 2f;

            Polyline("CentreLine", new Vector3(-hw, 0.02f, 0f), new Vector3(hw, 0.02f, 0f));
            LoopLine("CentreCircle", Arc(Vector3.zero, 1.8f, 0f, 360f, 48));

            BuildHalfMarkings(-(courtLength / 2f - 1.6f), -1f, "Home");
            BuildHalfMarkings(courtLength / 2f - 1.6f, 1f, "Away");
        }

        void BuildHalfMarkings(float hoopZ, float dir, string label)
        {
            // Three-point arc: a semicircle around the hoop bulging toward centre.
            var arcPts = new List<Vector3>();
            int seg = 32;
            for (int i = 0; i <= seg; i++)
            {
                float phi = Mathf.Lerp(-90f, 90f, i / (float)seg) * Mathf.Deg2Rad;
                arcPts.Add(new Vector3(threePointRadius * Mathf.Sin(phi), 0.02f, hoopZ + dir * threePointRadius * Mathf.Cos(phi)));
            }
            Polyline($"{label}Arc", arcPts.ToArray());

            // Lane (paint): baseline to free-throw line.
            float baselineZ = dir < 0f ? -courtLength / 2f : courtLength / 2f;
            float ftZ = baselineZ - dir * 5.8f;
            float laneHalf = 2.4f;
            LoopLine($"{label}Lane", new[]
            {
                new Vector3(-laneHalf, 0.02f, baselineZ),
                new Vector3(laneHalf, 0.02f, baselineZ),
                new Vector3(laneHalf, 0.02f, ftZ),
                new Vector3(-laneHalf, 0.02f, ftZ),
            });
        }

        Hoop BuildHoop(string label, Vector3 basePos, TeamSide team, float faceZ)
        {
            var root = new GameObject(label);
            root.transform.position = basePos;

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Backboard";
            board.transform.SetParent(root.transform);
            board.transform.localPosition = new Vector3(0f, rimHeight + 0.45f, -0.5f * faceZ);
            board.transform.localScale = new Vector3(1.8f, 1.05f, 0.1f);
            Tint(board, Color.white);

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(root.transform);
            rim.transform.localPosition = new Vector3(0f, rimHeight, 0.1f * faceZ);
            rim.transform.localScale = new Vector3(0.45f, 0.02f, 0.45f);
            Tint(rim, new Color(0.95f, 0.45f, 0.1f));
            Destroy(rim.GetComponent<Collider>()); // visual only; let the ball pass

            // Rim contact trigger (shot-clock "hit the rim" detection).
            var rimTriggerGo = new GameObject("RimTrigger");
            rimTriggerGo.transform.SetParent(root.transform);
            rimTriggerGo.transform.localPosition = new Vector3(0f, rimHeight, 0.1f * faceZ);
            var rimCol = rimTriggerGo.AddComponent<SphereCollider>();
            rimCol.radius = 0.5f;
            rimCol.isTrigger = true;
            rimTriggerGo.AddComponent<Rim>();

            // Score trigger just beneath the rim.
            var zoneGo = new GameObject("ScoreZone");
            zoneGo.transform.SetParent(root.transform);
            zoneGo.transform.localPosition = new Vector3(0f, rimHeight - 0.25f, 0.1f * faceZ);
            var zoneCol = zoneGo.AddComponent<SphereCollider>();
            zoneCol.radius = 0.28f;
            zoneCol.isTrigger = true;
            zoneGo.AddComponent<ScoreZone>();

            var hoop = root.AddComponent<Hoop>();
            hoop.attackedBy = team;
            hoop.rim = rim.transform;
            return hoop;
        }

        BallController BuildBall(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.24f;
            Tint(go, new Color(0.85f, 0.4f, 0.1f));

            var col = go.GetComponent<SphereCollider>();
            col.material = new PhysicsMaterial("Ball")
            {
                bounciness = 0.55f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            rb.linearDamping = 0.05f;

            return go.AddComponent<BallController>();
        }

        void BuildCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.target = target;
        }

        // ---- Lighting / line / colour helpers ------------------------------

        void BuildLighting()
        {
            if (FindFirstObjectByType<Light>() != null) return;
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        Material LineMaterial => _lineMat ??= new Material(Shader.Find("Sprites/Default"));

        LineRenderer NewLine(string label)
        {
            var go = new GameObject(label);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = LineMaterial;
            lr.widthMultiplier = 0.08f;
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.startColor = lr.endColor = Color.white;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        void Polyline(string label, params Vector3[] points)
        {
            var lr = NewLine(label);
            lr.loop = false;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
        }

        void LoopLine(string label, Vector3[] points)
        {
            var lr = NewLine(label);
            lr.loop = true;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
        }

        static Vector3[] Arc(Vector3 centre, float radius, float fromDeg, float toDeg, int segments)
        {
            var pts = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)segments) * Mathf.Deg2Rad;
                pts[i] = centre + new Vector3(Mathf.Cos(a) * radius, 0.02f, Mathf.Sin(a) * radius);
            }
            return pts;
        }

        static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var mat = renderer.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }
    }
}
