using UnityEngine;
using UnityEngine.InputSystem;
using MarioBasketball.Core;
using MarioBasketball.Gameplay;
using MarioBasketball.CameraControl;

namespace MarioBasketball.Control
{
    /// <summary>
    /// Decides which of the human side's players the person is controlling, and
    /// keeps exactly one player human-controlled at a time:
    /// <list type="bullet">
    ///   <item><b>On offense</b> control automatically follows the ball — you
    ///   always drive whoever has it.</item>
    ///   <item><b>On defense / loose ball</b> the Switch button (Q / left
    ///   shoulder) hands control to the on-court teammate nearest the ball.</item>
    /// </list>
    /// It also moves the camera to the controlled player, keeps
    /// <see cref="GameManager.humanPlayer"/> pointed at them (so the HUD
    /// follows), recovers control if the current player is subbed out, and
    /// shows a marker at their feet.
    /// </summary>
    public class PlayerSwitchManager : MonoBehaviour
    {
        public TeamSide humanSide = TeamSide.Home;
        public PlayerController initial;

        CameraRig _camera;
        PlayerController _current;
        InputAction _switch;
        GameObject _marker;

        void OnEnable()
        {
            _switch = new InputAction("Switch", InputActionType.Button, "<Keyboard>/q");
            _switch.AddBinding("<Gamepad>/leftShoulder");
            _switch.AddBinding("<Gamepad>/buttonSouth"); // A also switches (only acts on defense)
            _switch.performed += OnSwitchPressed;
            _switch.Enable();
        }

        void OnDisable()
        {
            if (_switch == null) return;
            _switch.performed -= OnSwitchPressed;
            _switch.Disable();
            _switch = null;
        }

        void Start()
        {
            var cam = Camera.main;
            if (cam != null) _camera = cam.GetComponent<CameraRig>();
            _marker = CreateMarker();
            if (initial != null) SetControl(initial);
        }

        void Update()
        {
            if (MatchPause.IsPaused) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Offense: control follows the ball handler.
            var ball = gm.ball;
            if (ball != null && ball.Holder != null && ball.Holder.team == humanSide && IsOnCourt(gm, ball.Holder))
                SetControl(ball.Holder);

            // Recover if our player left the floor (substituted out).
            if (_current == null || !_current.enabled || !IsOnCourt(gm, _current))
                SetControl(PickFallback(gm));

            if (_marker != null && _current != null)
            {
                _marker.SetActive(true);
                Vector3 p = _current.transform.position;
                _marker.transform.position = new Vector3(p.x, 0.05f, p.z);
            }
            else if (_marker != null)
            {
                _marker.SetActive(false);
            }
        }

        void OnSwitchPressed(InputAction.CallbackContext ctx)
        {
            if (MatchPause.IsPaused) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            // On offense control is locked to the ball handler.
            var ball = gm.ball;
            if (ball != null && ball.Holder != null && ball.Holder.team == humanSide) return;

            var target = NearestToBall(gm, exclude: _current);
            if (target != null) SetControl(target);
        }

        void SetControl(PlayerController p)
        {
            if (p == null || p == _current) return;
            if (_current != null) _current.SetHumanControlled(false);
            _current = p;
            p.SetHumanControlled(true);
            if (_camera != null) _camera.target = p.transform;
            if (GameManager.Instance != null) GameManager.Instance.humanPlayer = p;
        }

        bool IsOnCourt(GameManager gm, PlayerController p) =>
            p != null && gm.TeamFor(humanSide).onCourt.Contains(p);

        PlayerController PickFallback(GameManager gm)
        {
            foreach (var p in gm.TeamFor(humanSide).onCourt)
                if (p != null && p.enabled) return p;
            return null;
        }

        PlayerController NearestToBall(GameManager gm, PlayerController exclude)
        {
            var ball = gm.ball;
            Vector3 reference = ball != null ? ball.transform.position : Vector3.zero;
            PlayerController best = null;
            float bestD = Mathf.Infinity;
            foreach (var p in gm.TeamFor(humanSide).onCourt)
            {
                if (p == null || !p.enabled || p == exclude) continue;
                float d = Vector3.Distance(p.transform.position, reference);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        GameObject CreateMarker()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ControlMarker";
            go.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = renderer.material;
                var gold = new Color(1f, 0.82f, 0.1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", gold);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", gold);
            }
            return go;
        }
    }
}
