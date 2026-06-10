using System.Collections.Generic;
using UnityEngine;

namespace MarioBasketball.Characters
{
    /// <summary>One player the user built in Create-a-Player.</summary>
    [System.Serializable]
    public class CreatedPlayer
    {
        public CharacterStats stats = new CharacterStats();
        /// <summary>Journey characters earn stats through story mode; standard
        /// players have unlimited stats but are exhibition-only.</summary>
        public bool journey;
    }

    [System.Serializable]
    class CreatedPlayerSave
    {
        public List<CreatedPlayer> players = new List<CreatedPlayer>();
    }

    /// <summary>
    /// Stores user-created players, persisted with <see cref="PlayerPrefs"/> so
    /// they survive across sessions. Team select adds these to the roster pool.
    /// </summary>
    public static class CreatedPlayerStore
    {
        const string Key = "mb_created_players_v1";
        static CreatedPlayerSave _save;

        static void EnsureLoaded()
        {
            if (_save != null) return;
            string json = PlayerPrefs.GetString(Key, "");
            _save = string.IsNullOrEmpty(json)
                ? new CreatedPlayerSave()
                : (JsonUtility.FromJson<CreatedPlayerSave>(json) ?? new CreatedPlayerSave());
        }

        public static IReadOnlyList<CreatedPlayer> All()
        {
            EnsureLoaded();
            return _save.players;
        }

        public static void Add(CharacterStats stats, bool journey)
        {
            EnsureLoaded();
            _save.players.Add(new CreatedPlayer { stats = stats, journey = journey });
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_save));
            PlayerPrefs.Save();
        }
    }
}
