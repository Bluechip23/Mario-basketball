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
    /// Builds the playable full-court 3v3 game out of primitives at runtime. On
    /// <see cref="Awake"/> it lays down the static court (floor, perimeter walls
    /// — there is no out of bounds, things bounce off — painted lane /
    /// three-point arcs / centre circle, two rimmed hoops, camera) and shows the
    /// <see cref="TeamSelectMenu"/>. When the player confirms their rosters,
    /// <see cref="StartMatch"/> spawns the teams and kicks off the game.
    ///
    /// Running from an empty scene with just this component keeps the prototype
    /// free of fragile authored scene/prefab assets. As real content arrives,
    /// this bootstrap shrinks and is retired.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Court dimensions (metres)")]
        public float courtLength = 32f;
        public float courtWidth = 18f;
        public float rimHeight = 3.05f;
        public float threePointRadius = 6.75f;
        [Tooltip("Global scale on every player's size — a bit smaller so the action reads more clearly and bodies crowd the view less.")]
        public float playerHeightScale = 0.85f;

        static readonly Color HomeColor = new Color(0.85f, 0.15f, 0.15f);
        static readonly Color AwayColor = new Color(0.15f, 0.35f, 0.9f);

        // Mario scenery palette.
        static readonly Color PipeGreen     = new Color(0.13f, 0.62f, 0.20f);
        static readonly Color PipeGreenDark = new Color(0.08f, 0.42f, 0.13f);
        static readonly Color QBlockYellow  = new Color(1f, 0.78f, 0.12f);
        static readonly Color QBlockBrown   = new Color(0.50f, 0.28f, 0.10f);
        static readonly Color Grass         = new Color(0.36f, 0.70f, 0.28f);
        static readonly Color HillGreen     = new Color(0.28f, 0.60f, 0.22f);
        static readonly Color CloudWhite    = new Color(0.98f, 0.99f, 1f);

        Material _lineMat;
        Hoop _hoopNeg;
        Hoop _hoopPos;

        void Awake()
        {
            GameSettings.ApplyAudio();
            BuildLighting();
            BuildCourt();
            BuildWalls();
            BuildMarkings();
            BuildSurroundings();

            // Teams start on their own half and attack the far basket: home
            // begins on the -z half and attacks the +z hoop, and vice versa.
            _hoopNeg = BuildHoop("HoopNeg", new Vector3(0f, 0f, -(courtLength / 2f - 1.6f)), TeamSide.Away, faceZ: 1f);
            _hoopPos = BuildHoop("HoopPos", new Vector3(0f, 0f, courtLength / 2f - 1.6f), TeamSide.Home, faceZ: -1f);

            BuildCamera(null); // overview framing until the match starts

            // Menu flow: MainMenu (shown) → TeamSelect (exhibition) or
            // CreatePlayer. The latter two start disabled; MainMenu enables them.
            var teamSelect = gameObject.AddComponent<TeamSelectMenu>();
            teamSelect.bootstrap = this;
            teamSelect.enabled = false;

            var createPlayer = gameObject.AddComponent<CreatePlayerMenu>();
            createPlayer.enabled = false;

            var settings = gameObject.AddComponent<SettingsMenu>();
            settings.enabled = false;

            var main = gameObject.AddComponent<MainMenu>();
            main.teamSelect = teamSelect;
            main.createPlayer = createPlayer;
            main.settings = settings;
            teamSelect.mainMenu = main;
            createPlayer.mainMenu = main;
        }

        /// <summary>
        /// Spawns both teams and starts the match. Each roster is five
        /// characters: the first three start (the <b>first</b> is the
        /// human-controlled player), the last two sit on the bench.
        /// </summary>
        public void StartMatch(CharacterStats[] homeRoster, CharacterStats[] awayRoster)
        {
            var gm = gameObject.AddComponent<GameManager>();
            gm.hoops.Add(_hoopNeg);
            gm.hoops.Add(_hoopPos);

            BuildTeam(gm.Home, TeamSide.Home, HomeColor, -1f, true, gm, homeRoster);
            BuildTeam(gm.Away, TeamSide.Away, AwayColor, 1f, false, gm, awayRoster);

            gm.ball = BuildBall(new Vector3(0f, 1.1f, 0f));

            // Stop the loose ball from pinballing off the players' capsules — it
            // passes through bodies and is resolved by the rebound contest, so it
            // settles into someone's hands instead of bouncing around wildly. The
            // ball still clanks off the rim, backboard and walls.
            IgnoreBallVsPlayers(gm);
            // Players don't physically collide with each other — no climbing or
            // standing on heads; the soft Separation push keeps them spaced out.
            IgnorePlayerVsPlayers(gm);

            // Mario benches: Home perches on warp pipes, Away on floating ? blocks.
            BuildBenches(gm);

            // Substitution anchors: benches sit outside each sideline.
            gm.homeBenchAnchor = new Vector3(-(courtWidth / 2f + 1.5f), 1.1f, -2f);
            gm.awayBenchAnchor = new Vector3(courtWidth / 2f + 1.5f, 1.1f, 2f);
            gm.homeSubEntry = new Vector3(0f, 1.1f, -4f);
            gm.awaySubEntry = new Vector3(0f, 1.1f, 4f);

            // NBA-Street-style sideline camera tracks the ball (the action),
            // not the controlled player — the gold ring marks who you control.
            if (Camera.main != null)
            {
                var rig = Camera.main.GetComponent<CameraRig>();
                if (rig != null && gm.ball != null) rig.target = gm.ball.transform;
            }

            var switcher = gameObject.AddComponent<MarioBasketball.Control.PlayerSwitchManager>();
            switcher.humanSide = TeamSide.Home;
            switcher.initial = gm.humanPlayer;

            gameObject.AddComponent<DebugHUD>();
            gameObject.AddComponent<BoxScoreHUD>();
            gameObject.AddComponent<DebugMatchControls>();
            gameObject.AddComponent<PauseMenu>();
            gameObject.AddComponent<MarioBasketball.Core.Haptics>();
        }

        /// <summary>Disable physical collisions between the ball and every player's
        /// body capsule (both teams, court and bench), so a loose ball doesn't
        /// ricochet off players.</summary>
        void IgnoreBallVsPlayers(GameManager gm)
        {
            var ballCol = gm.ball != null ? gm.ball.GetComponent<Collider>() : null;
            if (ballCol == null) return;
            foreach (var team in new[] { gm.Home, gm.Away })
            {
                IgnoreList(ballCol, team.onCourt);
                IgnoreList(ballCol, team.bench);
            }
        }

        static void IgnoreList(Collider ballCol, List<PlayerController> players)
        {
            foreach (var p in players)
            {
                if (p == null) continue;
                var cc = p.GetComponent<CharacterController>();
                if (cc != null) Physics.IgnoreCollision(ballCol, cc, true);
            }
        }

        void IgnorePlayerVsPlayers(GameManager gm)
        {
            var all = new List<CharacterController>();
            foreach (var team in new[] { gm.Home, gm.Away })
            {
                CollectControllers(all, team.onCourt);
                CollectControllers(all, team.bench);
            }
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                    Physics.IgnoreCollision(all[i], all[j], true);
        }

        static void CollectControllers(List<CharacterController> list, List<PlayerController> players)
        {
            foreach (var p in players)
            {
                if (p == null) continue;
                var cc = p.GetComponent<CharacterController>();
                if (cc != null) list.Add(cc);
            }
        }

        // ---- Teams ---------------------------------------------------------

        void BuildTeam(TeamState team, TeamSide side, Color color, float halfSign, bool hasHuman, GameManager gm, CharacterStats[] roster)
        {
            // Three on-court spots spread across this team's half, two on bench.
            // Slot 0 (centre) is the human's first pick.
            float[] xs = { 0f, -3f, 3f };
            for (int i = 0; i < 3; i++)
            {
                bool isHuman = hasHuman && i == 0; // first pick is the human starter
                var pos = new Vector3(xs[i], 1.1f, 4f * halfSign);
                var pc = BuildPlayer(roster[i], pos, side, color, benched: false);
                team.onCourt.Add(pc);
                if (isHuman) gm.humanPlayer = pc;
            }

            float benchX = (courtWidth / 2f + 1.5f) * halfSign;
            for (int i = 0; i < 2; i++)
            {
                var pos = new Vector3(benchX, 1.1f, -2f + i * 2f);
                var pc = BuildPlayer(roster[3 + i], pos, side, color, benched: true);
                team.bench.Add(pc);
            }
        }

        PlayerController BuildPlayer(CharacterStats stats, Vector3 pos, TeamSide side, Color jersey, bool benched)
        {
            float h = stats.heightMeters * Mathf.Clamp(playerHeightScale, 0.3f, 1.5f);

            // Root carries physics/logic; the visual model is built under it,
            // scaled to the character's height (NBA-Street-style big/small bodies).
            var go = new GameObject($"{side}_{stats.characterName}");
            go.SetActive(false); // configure before Awake/OnEnable run
            pos.y = h / 2f + 0.05f;
            go.transform.position = pos;

            MarioBasketball.Presentation.CharacterModelBuilder.Build(go.transform, stats, h, jersey);

            var cc = go.AddComponent<CharacterController>();
            cc.center = Vector3.zero;
            cc.height = h;
            cc.radius = Mathf.Min(0.18f * h, h / 2f - 0.01f); // slimmer body
            cc.skinWidth = 0.02f;

            var character = go.AddComponent<PlayerCharacter>();
            character.stats = stats;

            go.AddComponent<PostUpController>(); // required by PlayerController
            var pc = go.AddComponent<PlayerController>();
            pc.team = side;
            pc.isHuman = false; // control is assigned at runtime by PlayerSwitchManager
            pc.threePointDistance = threePointRadius;

            // Every player has a brain; it yields on whoever the human controls.
            go.AddComponent<MarioBasketball.AI.PlayerAI>();
            go.AddComponent<MarioBasketball.Presentation.ProceduralAnimator>();
            go.AddComponent<MarioBasketball.Presentation.OnFireFlash>(); // star-power strobe when On Fire

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
            // Out-of-bounds lines are invisible walls: tall enough that even a
            // high lob can't leave the court.
            const float t = 0.5f;
            const float h = 12f;
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
            // Three-point arc: a semicircle around the hoop bulging toward
            // centre court (dir points from centre toward this hoop, so the
            // bulge is opposite it).
            var arcPts = new List<Vector3>();
            int seg = 32;
            for (int i = 0; i <= seg; i++)
            {
                float phi = Mathf.Lerp(-90f, 90f, i / (float)seg) * Mathf.Deg2Rad;
                arcPts.Add(new Vector3(threePointRadius * Mathf.Sin(phi), 0.02f, hoopZ - dir * threePointRadius * Mathf.Cos(phi)));
            }
            Polyline($"{label}Arc", arcPts.ToArray());

            // Lane (paint): FIBA — 4.9 m wide, free-throw line 5.8 m from the baseline.
            float baselineZ = dir < 0f ? -courtLength / 2f : courtLength / 2f;
            float ftZ = baselineZ - dir * 5.8f;
            float laneHalf = 2.45f;
            LoopLine($"{label}Lane", new[]
            {
                new Vector3(-laneHalf, 0.02f, baselineZ),
                new Vector3(laneHalf, 0.02f, baselineZ),
                new Vector3(laneHalf, 0.02f, ftZ),
                new Vector3(-laneHalf, 0.02f, ftZ),
            });
        }

        // ---- Mario scenery & benches ---------------------------------------

        /// <summary>Dress the empty space around the court: a grass field, rolling
        /// hills behind the baselines, roadside bushes, lazy clouds and a few
        /// decorative warp pipes / floating ? blocks. All cosmetic — colliders are
        /// stripped so nothing interferes with play (it all sits outside the walls).</summary>
        void BuildSurroundings()
        {
            var root = new GameObject("Scenery").transform;
            float hw = courtWidth / 2f, hl = courtLength / 2f;

            // Grass field stretching well past the court on every side.
            var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "GrassField";
            grass.transform.SetParent(root);
            grass.transform.position = new Vector3(0f, -0.06f, 0f);
            grass.transform.localScale = new Vector3(9f, 1f, 9f); // ~90 m square
            var grassCol = grass.GetComponent<Collider>();
            if (grassCol != null) Destroy(grassCol);
            Tint(grass, Grass);

            var rng = new System.Random(12345); // stable layout each run
            float Rand(float a, float b) => (float)(a + rng.NextDouble() * (b - a));

            // Rolling hills behind both baselines (flattened spheres).
            for (int i = 0; i < 6; i++)
            {
                float side = (i % 2 == 0) ? 1f : -1f;
                float z = side * (hl + Rand(8f, 18f));
                float s = Rand(7f, 13f);
                Prop(PrimitiveType.Sphere, "Hill", new Vector3(Rand(-20f, 20f), -s * 0.55f, z),
                    new Vector3(s, s * 0.6f, s), HillGreen, root);
            }

            // Bushes hugging the sidelines.
            for (int i = 0; i < 10; i++)
            {
                float side = (i % 2 == 0) ? 1f : -1f;
                float s = Rand(1.2f, 2.4f);
                Prop(PrimitiveType.Sphere, "Bush", new Vector3(side * (hw + Rand(2f, 6f)), s * 0.25f, Rand(-hl, hl)),
                    new Vector3(s, s * 0.7f, s), HillGreen, root);
            }

            // Puffy clouds overhead.
            for (int i = 0; i < 7; i++)
            {
                float s = Rand(2.5f, 4.5f);
                Prop(PrimitiveType.Sphere, "Cloud", new Vector3(Rand(-25f, 25f), Rand(10f, 16f), Rand(-25f, 25f)),
                    new Vector3(s * 1.6f, s * 0.7f, s), CloudWhite, root);
            }

            // A couple of standalone warp pipes and some floating ? blocks in the surrounds.
            BuildPipe(new Vector3(-(hw + 4f), 0f, hl + 6f), Rand(1.6f, 2.6f), root);
            BuildPipe(new Vector3(hw + 4f, 0f, -(hl + 6f)), Rand(1.6f, 2.6f), root);
            for (int i = 0; i < 4; i++)
                BuildQuestionBlock(new Vector3(Rand(-12f, 12f), Rand(4f, 7f), (i % 2 == 0 ? 1f : -1f) * (hl + Rand(3f, 7f))), 1.0f, root);
        }

        /// <summary>Seat each team's bench on Mario furniture: Home on warp pipes,
        /// Away on floating ? blocks (players perched on top, facing the court).</summary>
        void BuildBenches(GameManager gm)
        {
            var root = new GameObject("Benches").transform;
            SeatBench(gm.Home.bench, onPipes: true, root);
            SeatBench(gm.Away.bench, onPipes: false, root);
        }

        void SeatBench(List<PlayerController> bench, bool onPipes, Transform root)
        {
            foreach (var p in bench)
            {
                if (p == null) continue;
                Vector3 at = p.transform.position;       // already at the sideline bench spot
                float h = p.BodyHeight;
                Vector3 face = new Vector3(-Mathf.Sign(at.x), 0f, 0f); // look in toward the court
                if (onPipes)
                {
                    const float pipeH = 1.0f;
                    BuildPipe(new Vector3(at.x, 0f, at.z), pipeH, root);
                    SeatPlayer(p, new Vector3(at.x, pipeH + h * 0.5f, at.z), face);
                }
                else
                {
                    const float floatY = 1.15f, size = 0.95f;
                    BuildQuestionBlock(new Vector3(at.x, floatY, at.z), size, root);
                    SeatPlayer(p, new Vector3(at.x, floatY + size * 0.5f + h * 0.5f, at.z), face);
                }
            }
        }

        static void SeatPlayer(PlayerController p, Vector3 pos, Vector3 face)
        {
            p.Teleport(pos); // benched players are inert (their controller is disabled), so they stay put
            if (face.sqrMagnitude > 0.01f) p.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
        }

        /// <summary>A green warp pipe of the given height, with a wider rim lip.</summary>
        void BuildPipe(Vector3 basePos, float height, Transform root)
        {
            Prop(PrimitiveType.Cylinder, "Pipe", basePos + Vector3.up * (height * 0.5f),
                new Vector3(1.1f, height * 0.5f, 1.1f), PipeGreen, root);   // cylinder is 2 units tall → y-scale is half-height
            Prop(PrimitiveType.Cylinder, "PipeLip", basePos + Vector3.up * height,
                new Vector3(1.35f, 0.14f, 1.35f), PipeGreenDark, root);
        }

        /// <summary>A floating ? block — yellow cube, corner rivets and a hint of a "?".</summary>
        void BuildQuestionBlock(Vector3 centre, float size, Transform root)
        {
            Prop(PrimitiveType.Cube, "QuestionBlock", centre, Vector3.one * size, QBlockYellow, root);
            float r = size * 0.42f;
            foreach (var c in new[] { new Vector3(r, -r, r), new Vector3(-r, -r, r), new Vector3(r, -r, -r), new Vector3(-r, -r, -r) })
                Prop(PrimitiveType.Cube, "Rivet", centre + c, Vector3.one * (size * 0.12f), QBlockBrown, root);
            Prop(PrimitiveType.Cube, "Q", centre + new Vector3(0f, 0f, size * 0.5f + 0.01f),
                new Vector3(size * 0.20f, size * 0.42f, 0.02f), QBlockBrown, root);
        }

        /// <summary>Build a tinted primitive used purely as scenery (collider stripped).</summary>
        GameObject Prop(PrimitiveType type, string label, Vector3 pos, Vector3 scale, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = label;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Tint(go, color);
            return go;
        }

        Hoop BuildHoop(string label, Vector3 basePos, TeamSide team, float faceZ)
        {
            var root = new GameObject(label);
            root.transform.position = basePos;

            // Cosmetic-only piece (collider stripped) parented under the hoop, so the
            // structural dressing never interferes with shot/rim physics.
            GameObject Deco(PrimitiveType type, Vector3 lp, Vector3 ls, Color col, string nm)
            {
                var g = GameObject.CreatePrimitive(type);
                g.name = nm;
                g.transform.SetParent(root.transform);
                g.transform.localPosition = lp;
                g.transform.localScale = ls;
                var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
                Tint(g, col);
                return g;
            }

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Backboard";
            board.transform.SetParent(root.transform);
            board.transform.localPosition = new Vector3(0f, rimHeight + 0.55f, -0.55f * faceZ);
            board.transform.localScale = new Vector3(2.4f, 1.4f, 0.1f);
            Tint(board, Color.white);

            // Rim: slightly oversized for an arcade read (0.7 m). The visual ring
            // has no collider; physics comes from a ring of iron colliders so a
            // near-miss clanks while a clean make drops through.
            const float rimRadius = 0.35f;
            Vector3 rimCentre = new Vector3(0f, rimHeight, 0.18f * faceZ);
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(root.transform);
            rim.transform.localPosition = rimCentre;
            rim.transform.localScale = new Vector3(rimRadius * 2f, 0.03f, rimRadius * 2f);
            Destroy(rim.GetComponent<Collider>());
            // Render the rim as an open ring (a tube of short segments) instead of a
            // solid orange disc — you can see the ball drop through, like a real hoop.
            // The cylinder itself stays (invisible) only as the AimPoint anchor.
            Color rimColor = new Color(0.95f, 0.32f, 0.12f);
            rim.GetComponent<MeshRenderer>().enabled = false;
            const int ringSegments = 24;
            for (int i = 0; i < ringSegments; i++)
            {
                float a = (i + 0.5f) / ringSegments * Mathf.PI * 2f;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.name = "RimRing";
                seg.transform.SetParent(root.transform);
                seg.transform.localPosition = rimCentre + new Vector3(Mathf.Cos(a) * rimRadius, 0f, Mathf.Sin(a) * rimRadius);
                seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)));
                seg.transform.localScale = new Vector3(0.045f, 0.05f, 0.045f);
                var sc = seg.GetComponent<Collider>(); if (sc != null) Destroy(sc);
                Tint(seg, rimColor);
            }

            const int rimSegments = 10;
            for (int i = 0; i < rimSegments; i++)
            {
                float a = i / (float)rimSegments * Mathf.PI * 2f;
                var seg = new GameObject("RimIron");
                seg.transform.SetParent(root.transform);
                seg.transform.localPosition = rimCentre + new Vector3(Mathf.Cos(a) * rimRadius, 0f, Mathf.Sin(a) * rimRadius);
                var segCol = seg.AddComponent<SphereCollider>();
                segCol.radius = 0.035f;
            }

            // Net: a soft inverted cone that snaps on a make (NetSwish).
            var net = new GameObject("Net");
            net.transform.SetParent(root.transform);
            net.transform.localPosition = rimCentre + new Vector3(0f, -0.02f, 0f);
            net.transform.localScale = new Vector3(rimRadius * 1.7f, -0.5f, rimRadius * 1.7f); // apex down
            net.AddComponent<MeshFilter>().sharedMesh = MarioBasketball.Presentation.CharacterModelBuilder.ConeMesh();
            var netMr = net.AddComponent<MeshRenderer>();
            netMr.material = new Material(LineMaterial) { color = new Color(0.95f, 0.95f, 0.95f, 0.85f) };
            net.AddComponent<MarioBasketball.Presentation.NetSwish>();

            // Rim contact trigger (shot-clock "hit the rim" detection).
            var rimTriggerGo = new GameObject("RimTrigger");
            rimTriggerGo.transform.SetParent(root.transform);
            rimTriggerGo.transform.localPosition = rimCentre;
            var rimCol = rimTriggerGo.AddComponent<SphereCollider>();
            rimCol.radius = rimRadius + 0.1f;
            rimCol.isTrigger = true;
            rimTriggerGo.AddComponent<Rim>();

            // Score trigger just beneath the rim — only a clean drop-through
            // counts (misses are aimed at the iron, outside this).
            var zoneGo = new GameObject("ScoreZone");
            zoneGo.transform.SetParent(root.transform);
            zoneGo.transform.localPosition = rimCentre + new Vector3(0f, -0.25f, 0f);
            var zoneCol = zoneGo.AddComponent<SphereCollider>();
            zoneCol.radius = 0.17f;
            zoneCol.isTrigger = true;
            zoneGo.AddComponent<ScoreZone>();

            // ---- Mounting structure (cosmetic) so the hoop reads as a real,
            // pole-mounted backboard instead of a floating board. ----
            Color steel = new Color(0.30f, 0.32f, 0.36f);
            Color frame = new Color(0.16f, 0.16f, 0.18f);
            Color square = new Color(0.85f, 0.20f, 0.12f);
            float boardY = rimHeight + 0.55f;     // backboard centre height
            float faceFront = -0.50f * faceZ;     // court-facing surface of the board

            // Support pole behind the baseline, with two arms reaching out to the board.
            float poleTop = boardY + 0.70f;       // up to the top edge of the board
            Deco(PrimitiveType.Cylinder, new Vector3(0f, poleTop / 2f, -1.05f * faceZ),
                 new Vector3(0.14f, poleTop / 2f, 0.14f), steel, "Pole");
            Deco(PrimitiveType.Cube, new Vector3(0f, boardY + 0.40f, -0.80f * faceZ), new Vector3(0.09f, 0.09f, 0.55f), steel, "MountArm");
            Deco(PrimitiveType.Cube, new Vector3(0f, boardY - 0.40f, -0.80f * faceZ), new Vector3(0.09f, 0.09f, 0.55f), steel, "MountArm");

            // Backboard trim around the edge.
            Deco(PrimitiveType.Cube, new Vector3(0f, boardY + 0.70f, faceFront), new Vector3(2.42f, 0.08f, 0.06f), frame, "BoardFrame");
            Deco(PrimitiveType.Cube, new Vector3(0f, boardY - 0.70f, faceFront), new Vector3(2.42f, 0.08f, 0.06f), frame, "BoardFrame");
            Deco(PrimitiveType.Cube, new Vector3(-1.20f, boardY, faceFront), new Vector3(0.08f, 1.40f, 0.06f), frame, "BoardFrame");
            Deco(PrimitiveType.Cube, new Vector3(1.20f, boardY, faceFront), new Vector3(0.08f, 1.40f, 0.06f), frame, "BoardFrame");

            // Shooter's square above the rim (sits a hair proud of the board face).
            float sqTop = rimHeight + 0.58f, sqBot = rimHeight + 0.13f, sqW = 0.59f, sqMid = (sqTop + sqBot) / 2f;
            float sqFront = -0.49f * faceZ;
            Deco(PrimitiveType.Cube, new Vector3(0f, sqTop, sqFront), new Vector3(sqW + 0.06f, 0.05f, 0.03f), square, "Square");
            Deco(PrimitiveType.Cube, new Vector3(0f, sqBot, sqFront), new Vector3(sqW + 0.06f, 0.05f, 0.03f), square, "Square");
            Deco(PrimitiveType.Cube, new Vector3(-sqW / 2f, sqMid, sqFront), new Vector3(0.05f, sqTop - sqBot, 0.03f), square, "Square");
            Deco(PrimitiveType.Cube, new Vector3(sqW / 2f, sqMid, sqFront), new Vector3(0.05f, sqTop - sqBot, 0.03f), square, "Square");

            // Bracket joining the rim to the board.
            Deco(PrimitiveType.Cube, new Vector3(0f, rimHeight, -0.34f * faceZ), new Vector3(0.14f, 0.06f, 0.36f), rimColor, "RimBracket");

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
            go.transform.localScale = Vector3.one * 0.30f; // arcade-readable
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
            // Sideline overview before a target exists (team select / pre-match).
            cam.transform.position = new Vector3(-(courtWidth / 2f + 6f), 5f, 0f);
            cam.transform.LookAt(new Vector3(0f, 1.6f, 0f));

            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            // NBA-Street chase cam: trail behind the ball down-court, low and
            // close (players read big), looking toward the attacking hoop. A touch
            // zoomed out from the reference shot — tune distanceBehind / fieldOfView.
            rig.distanceBehind = 8.5f;
            rig.height = 3.4f;
            rig.lateralOffset = 0f;
            rig.orbitDegrees = 65f;   // closer to the old sideline view, still trailing the action
            rig.lookAhead = 3f;
            rig.lookHeight = 1.2f;
            rig.fieldOfView = 46f;
            rig.followSmoothing = 5f;
            rig.turnSmoothing = 3f;
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
