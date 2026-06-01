using UnityEngine;
using MarioBasketball.Core;
using MarioBasketball.Gameplay;
using MarioBasketball.CameraControl;
using MarioBasketball.UI;

namespace MarioBasketball.Bootstrap
{
    /// <summary>
    /// Builds a complete, playable court out of primitives at runtime: floor,
    /// two hoops, a player and a ball, plus the camera, <see cref="GameManager"/>
    /// and HUD. This means the entire core loop runs from an empty scene that
    /// contains nothing but one GameObject with this component — no fragile
    /// hand-authored scene or prefab assets to keep in sync.
    ///
    /// Once the game grows, graduate the layout into a real scene/prefabs and
    /// retire this bootstrap.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Court dimensions (metres)")]
        public float courtLength = 28f;
        public float courtWidth = 15f;
        public float rimHeight = 3.05f;

        void Awake()
        {
            BuildLighting();
            BuildCourt();

            Hoop homeHoop = BuildHoop("HomeHoop", new Vector3(0f, 0f, -(courtLength / 2f - 1.6f)), TeamSide.Home, faceZ: 1f);
            Hoop awayHoop = BuildHoop("AwayHoop", new Vector3(0f, 0f, courtLength / 2f - 1.6f), TeamSide.Away, faceZ: -1f);

            PlayerController player = BuildPlayer(new Vector3(0f, 1.1f, -4f));
            BallController ball = BuildBall(player.BallHoldPoint);

            GameManager gm = gameObject.AddComponent<GameManager>();
            gm.ball = ball;
            gm.hoops.Add(homeHoop);
            gm.hoops.Add(awayHoop);

            BuildCamera(player.transform);
            gameObject.AddComponent<DebugHUD>();
        }

        void BuildLighting()
        {
            if (FindFirstObjectByType<Light>() != null) return;
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        void BuildCourt()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Court";
            // A Plane primitive is 10x10 units at scale 1.
            floor.transform.localScale = new Vector3(courtWidth / 10f, 1f, courtLength / 10f);
            Tint(floor, new Color(0.62f, 0.42f, 0.24f)); // hardwood
        }

        Hoop BuildHoop(string label, Vector3 basePos, TeamSide team, float faceZ)
        {
            var root = new GameObject(label);
            root.transform.position = basePos;

            // Backboard.
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Backboard";
            board.transform.SetParent(root.transform);
            board.transform.localPosition = new Vector3(0f, rimHeight + 0.45f, -0.5f * faceZ);
            board.transform.localScale = new Vector3(1.8f, 1.05f, 0.1f);
            Tint(board, Color.white);

            // Rim (a thin disc the players aim at). Visual + aim anchor.
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(root.transform);
            rim.transform.localPosition = new Vector3(0f, rimHeight, 0.1f * faceZ);
            rim.transform.localScale = new Vector3(0.45f, 0.02f, 0.45f);
            Tint(rim, new Color(0.95f, 0.45f, 0.1f));
            // Don't let the aim disc block the ball from dropping through.
            Destroy(rim.GetComponent<Collider>());

            // Score trigger sitting just beneath the rim.
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

        PlayerController BuildPlayer(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.transform.position = pos;
            Tint(go, new Color(0.85f, 0.15f, 0.15f)); // a certain plumber's red
            Destroy(go.GetComponent<CapsuleCollider>()); // CharacterController replaces it

            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0f, 0f);
            cc.height = 2f;
            cc.radius = 0.4f;

            return go.AddComponent<PlayerController>();
        }

        BallController BuildBall(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.24f;
            Tint(go, new Color(0.85f, 0.4f, 0.1f));

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

        /// <summary>
        /// Set an object's colour in a way that works under both the built-in
        /// and Universal render pipelines (different shaders, different
        /// colour property names).
        /// </summary>
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
