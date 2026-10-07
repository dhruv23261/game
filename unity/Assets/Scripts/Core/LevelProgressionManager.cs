using UnityEngine;

namespace MobControl.Core
{
    /// <summary>
    /// Manages player progression, unlocked levels, stars, and currency using persistent PlayerPrefs.
    /// Accessible globally across all scenes (MainMenu, Level 1, Level 2, etc.).
    /// </summary>
    public class LevelProgressionManager : MonoBehaviour
    {
        public static LevelProgressionManager Instance { get; private set; }

        private const string KEY_PLAYER_NAME = "mc_player_name";
        private const string KEY_UNLOCKED_LEVEL = "mc_unlocked_level";
        private const string KEY_COINS = "mc_player_coins";
        private const string KEY_GEMS = "mc_player_gems";
        private const string KEY_LEVEL_STARS_PREFIX = "mc_stars_lvl_";

        [Header("Default Settings")]
        [SerializeField] private int totalLevels = 10;
        [SerializeField] private int startingCoins = 100;
        [SerializeField] private int startingGems = 10;

        public int TotalLevels => totalLevels;

        public string PlayerName
        {
            get => PlayerPrefs.GetString(KEY_PLAYER_NAME, "Survivor");
            set
            {
                PlayerPrefs.SetString(KEY_PLAYER_NAME, value);
                PlayerPrefs.Save();
            }
        }

        public int HighestUnlockedLevel
        {
            get => PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1);
            private set
            {
                PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, value);
                PlayerPrefs.Save();
            }
        }

        public int Coins
        {
            get => PlayerPrefs.GetInt(KEY_COINS, startingCoins);
            set
            {
                PlayerPrefs.SetInt(KEY_COINS, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public int Gems
        {
            get => PlayerPrefs.GetInt(KEY_GEMS, startingGems);
            set
            {
                PlayerPrefs.SetInt(KEY_GEMS, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public bool IsLoggedIn => PlayerPrefs.HasKey(KEY_PLAYER_NAME);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Registers or updates the player profile / login credentials.
        /// </summary>
        public void LoginPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "Survivor";

            PlayerName = playerName.Trim();
            Debug.Log($"[LevelProgression] Player logged in as: {PlayerName}");
        }

        /// <summary>
        /// Unlocks the next level upon completing the current level.
        /// </summary>
        public void CompleteLevel(int completedLevel, int starsEarned = 3, int rewardCoins = 50)
        {
            // Save stars for this level (keep highest)
            int currentStars = GetLevelStars(completedLevel);
            if (starsEarned > currentStars)
            {
                PlayerPrefs.SetInt(KEY_LEVEL_STARS_PREFIX + completedLevel, Mathf.Clamp(starsEarned, 1, 3));
            }

            // Reward currency
            Coins += rewardCoins;

            // Unlock next level
            int nextLevel = completedLevel + 1;
            if (nextLevel > HighestUnlockedLevel && nextLevel <= totalLevels)
            {
                HighestUnlockedLevel = nextLevel;
                Debug.Log($"[LevelProgression] Level {nextLevel} unlocked!");
            }

            PlayerPrefs.Save();
        }

        public bool IsLevelUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true;
            return levelNumber <= HighestUnlockedLevel;
        }

        public int GetLevelStars(int levelNumber)
        {
            return PlayerPrefs.GetInt(KEY_LEVEL_STARS_PREFIX + levelNumber, 0);
        }

        /// <summary>
        /// Reset all progress (useful for testing).
        /// </summary>
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(KEY_UNLOCKED_LEVEL);
            PlayerPrefs.DeleteKey(KEY_PLAYER_NAME);
            PlayerPrefs.DeleteKey(KEY_COINS);
            PlayerPrefs.DeleteKey(KEY_GEMS);
            for (int i = 1; i <= totalLevels; i++)
            {
                PlayerPrefs.DeleteKey(KEY_LEVEL_STARS_PREFIX + i);
            }
            PlayerPrefs.Save();
            Debug.Log("[LevelProgression] All player data reset to default.");
        }
    }
}
