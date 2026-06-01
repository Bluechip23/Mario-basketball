using UnityEngine;
using MarioBasketball.Core;

namespace MarioBasketball.Gameplay
{
    /// <summary>
    /// A basket. Exposes the rim centre that shooters aim at and records
    /// which team attacks it. The actual "did it go in" detection lives on a
    /// child <see cref="ScoreZone"/> trigger just below the rim.
    /// </summary>
    public class Hoop : MonoBehaviour
    {
        [Tooltip("The team that scores by putting the ball through THIS hoop.")]
        public TeamSide attackedBy = TeamSide.Home;

        [Tooltip("World point shooters aim at — the middle of the rim.")]
        public Transform rim;

        public Vector3 AimPoint => rim != null ? rim.position : transform.position;
    }
}
