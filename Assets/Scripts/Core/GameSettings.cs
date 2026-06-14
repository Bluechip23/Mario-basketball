using UnityEngine;

namespace MarioBasketball.Core
{
    /// <summary>
    /// Player-adjustable settings, persisted via <see cref="PlayerPrefs"/> and
    /// edited on the Settings screen (<c>SettingsMenu</c>). Audio applies
    /// immediately; match rules are read by <c>GameManager</c> when a match
    /// starts, so they take effect on the next game.
    /// </summary>
    public static class GameSettings
    {
        public const int QuarterMinutesMin = 1, QuarterMinutesMax = 12;
        public const int ShotClockMin = 12, ShotClockMax = 24;

        static int _masterVolume = 100;
        static int _quarterMinutes = 4;
        static int _shotClockSeconds = 20;
        static bool _vibration = true;
        static bool _showBoxScore;
        static bool _loaded;

        /// <summary>Master volume, 0-100 (drives <see cref="AudioListener.volume"/>).</summary>
        public static int MasterVolume
        {
            get { Load(); return _masterVolume; }
            set { Load(); _masterVolume = Mathf.Clamp(value, 0, 100); Save(); ApplyAudio(); }
        }

        /// <summary>Minutes per quarter (next match).</summary>
        public static int QuarterMinutes
        {
            get { Load(); return _quarterMinutes; }
            set { Load(); _quarterMinutes = Mathf.Clamp(value, QuarterMinutesMin, QuarterMinutesMax); Save(); }
        }

        /// <summary>Shot clock length in seconds (next match).</summary>
        public static int ShotClockSeconds
        {
            get { Load(); return _shotClockSeconds; }
            set { Load(); _shotClockSeconds = Mathf.Clamp(value, ShotClockMin, ShotClockMax); Save(); }
        }

        /// <summary>Controller rumble on blocks/steals/rebounds etc. (can be off).</summary>
        public static bool Vibration
        {
            get { Load(); return _vibration; }
            set { Load(); _vibration = value; Save(); }
        }

        /// <summary>Show the live on-court box score overlay during a match.</summary>
        public static bool ShowBoxScore
        {
            get { Load(); return _showBoxScore; }
            set { Load(); _showBoxScore = value; Save(); }
        }

        /// <summary>Push the audio settings to the engine (call once at boot).</summary>
        public static void ApplyAudio()
        {
            Load();
            AudioListener.volume = _masterVolume / 100f;
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _masterVolume = PlayerPrefs.GetInt("settings.masterVolume", _masterVolume);
            _quarterMinutes = PlayerPrefs.GetInt("settings.quarterMinutes", _quarterMinutes);
            _shotClockSeconds = PlayerPrefs.GetInt("settings.shotClockSeconds", _shotClockSeconds);
            _vibration = PlayerPrefs.GetInt("settings.vibration", _vibration ? 1 : 0) != 0;
            _showBoxScore = PlayerPrefs.GetInt("settings.showBoxScore", _showBoxScore ? 1 : 0) != 0;
        }

        static void Save()
        {
            PlayerPrefs.SetInt("settings.masterVolume", _masterVolume);
            PlayerPrefs.SetInt("settings.quarterMinutes", _quarterMinutes);
            PlayerPrefs.SetInt("settings.shotClockSeconds", _shotClockSeconds);
            PlayerPrefs.SetInt("settings.vibration", _vibration ? 1 : 0);
            PlayerPrefs.SetInt("settings.showBoxScore", _showBoxScore ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
