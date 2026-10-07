using UnityEngine;
using MobControl.Network;
using MobControl.UI;
using MobControl.Ads;
using MobControl.Cannon;

namespace MobControl.Core
{
    public enum GameState
    {
        Menu,
        Loading,
        Playing,
        LevelWon,
        LevelLost
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Menu;

        [Header("Scene References")]
        [SerializeField] private CannonController cannon;

        private int activeLevelNumber = 1;
        private int mobsSpawnedCount = 0;

        public GameState CurrentState => currentState;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            currentState = GameState.Loading;

            string savedId = PlayerPrefs.GetString("PlayerId", "");
            ApiClient.Instance.Authenticate(
                savedId,
                onSuccess: (player) =>
                {
                    PlayerPrefs.SetString("PlayerId", player.playerId);
                    PlayerPrefs.Save();

                    UIManager.Instance.UpdatePlayerCurrency(player.coins, player.gems);
                    activeLevelNumber = player.currentLevel;
                    StartLevel(activeLevelNumber);
                },
                onError: (err) =>
                {
                    Debug.LogWarning("[GameManager] Server offline or auth error, using offline defaults: " + err);
                    StartLevel(1);
                }
            );
        }

        public void StartLevel(int levelId)
        {
            activeLevelNumber = levelId;
            mobsSpawnedCount = 0;
            currentState = GameState.Loading;

            ApiClient.Instance.FetchLevel(
                levelId,
                onSuccess: (levelData) =>
                {
                    LevelManager.Instance.LoadLevel(levelData);
                    UIManager.Instance.ShowGameplay(levelData.id, levelData.name);

                    if (cannon != null)
                    {
                        cannon.SetControllable(true);
                        cannon.SetFireRate(levelData.cannonFireRate);
                    }

                    currentState = GameState.Playing;
                },
                onError: (err) =>
                {
                    Debug.LogError("[GameManager] Could not load level data: " + err);
                }
            );
        }

        public void OnLevelWon()
        {
            if (currentState != GameState.Playing) return;
            currentState = GameState.LevelWon;

            if (cannon != null) cannon.SetControllable(false);

            ApiClient.Instance.SubmitLevelCompletion(
                activeLevelNumber,
                isVictory: true,
                mobsSpawned: mobsSpawnedCount,
                onSuccess: (res) =>
                {
                    UIManager.Instance.UpdatePlayerCurrency(res.player.coins, res.player.gems);
                    UIManager.Instance.ShowVictory(activeLevelNumber, res.earnedCoins, res.earnedGems, res.rewardedMultiplierAvailable > 1);

                    if (res.shouldShowInterstitial)
                    {
                        AdManager.Instance.ShowInterstitialAd();
                    }
                },
                onError: (err) =>
                {
                    Debug.LogError("[GameManager] Error submitting victory: " + err);
                    UIManager.Instance.ShowVictory(activeLevelNumber, 50, 1, true);
                }
            );
        }

        public void OnLevelLost()
        {
            if (currentState != GameState.Playing) return;
            currentState = GameState.LevelLost;

            if (cannon != null) cannon.SetControllable(false);

            ApiClient.Instance.SubmitLevelCompletion(
                activeLevelNumber,
                isVictory: false,
                mobsSpawned: mobsSpawnedCount,
                onSuccess: (res) =>
                {
                    UIManager.Instance.ShowDefeat();
                    if (res.shouldShowInterstitial)
                    {
                        AdManager.Instance.ShowInterstitialAd();
                    }
                },
                onError: (err) =>
                {
                    UIManager.Instance.ShowDefeat();
                }
            );
        }

        public void ProceedToNextLevel()
        {
            if (activeLevelNumber < 15)
            {
                activeLevelNumber++;
            }
            StartLevel(activeLevelNumber);
        }

        public void RetryCurrentLevel()
        {
            StartLevel(activeLevelNumber);
        }
    }
}
