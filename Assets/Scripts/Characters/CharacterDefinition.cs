using UnityEngine;

namespace MarioBasketball.Characters
{
    /// <summary>
    /// Editor-authorable container for a character's <see cref="CharacterStats"/>.
    /// Create assets via <b>Assets ▸ Create ▸ Mario Basketball ▸ Character</b>
    /// once the roster is being built in the editor.
    ///
    /// For now the only character (Bowser) is provided in code by
    /// <see cref="CharacterLibrary"/> so the prototype needs no asset wiring;
    /// this type is the path the roster will graduate to.
    /// </summary>
    [CreateAssetMenu(fileName = "Character", menuName = "Mario Basketball/Character", order = 0)]
    public class CharacterDefinition : ScriptableObject
    {
        public CharacterStats stats = new CharacterStats();

        void OnValidate()
        {
            stats?.Validate();
        }
    }
}
