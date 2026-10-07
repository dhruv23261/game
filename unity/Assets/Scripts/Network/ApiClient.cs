using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MobControl.Network
{
    [System.Serializable]
    public class PlayerUpgrades
    {
        public int cannonFireRateLevel = 1;
        public int startingMobCountLevel = 1;
        public int mobSpeedLevel = 1;
        public int mobPowerLevel = 1;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string playerId;
        public int currentLevel = 1;
        public int maxLevelReached = 1;
        public int coins = 200;
        public int gems = 10;
        public PlayerUpgrades upgrades;
    }

    [System.Serializable]
    public class LevelGateData
    {
        public string id;
        public string operation; // "multiply", "add", "subtract"
        public int value;
        public Vector3 position;
        public float width;
        public bool isMoving;
        public float moveSpeed;
        public float moveRange;
    }

    [System.Serializable]
    public class LevelWaveData
    {
        public int waveIndex;
        public float spawnDelay;
        public int count;
        public float spawnInterval;
        public float mobSpeed;
        public int mobHealth;
    }

    [System.Serializable]
    public class LevelRewards
    {
        public int coins;
        public int gems;
    }

    [System.Serializable]
    public class LevelAdConfig
    {
        public bool showInterstitialOnFinish;
        public int rewardedMultiplier;
    }

    [System.Serializable]
    public class LevelData
    {
        public int id;
        public string name;
        public string difficulty;
        public int enemyBaseHealth;
        public int playerBaseHealth;
        public float cannonFireRate;
        public LevelGateData[] gates;
        public LevelWaveData[] enemyWaves;
        public LevelRewards rewards;
        public LevelAdConfig adConfig;
    }

    [System.Serializable]
    public class LevelResponse
    {
        public bool success;
        public LevelData level;
    }

    [System.Serializable]
    public class AuthResponse
    {
        public bool success;
        public PlayerData player;
    }

    [System.Serializable]
    public class LevelCompleteResponse
    {
        public bool success;
        public string result;
        public int earnedCoins;
        public int earnedGems;
        public bool shouldShowInterstitial;
        public int rewardedMultiplierAvailable;
        public PlayerData player;
    }

    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        [Header("Backend Configuration")]
        [SerializeField] private string baseUrl = "http://localhost:4000/api";

        public PlayerData CurrentPlayer { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Authenticate(string savedPlayerId, Action<PlayerData> onSuccess, Action<string> onError)
        {
            StartCoroutine(AuthRoutine(savedPlayerId, onSuccess, onError));
        }

        private IEnumerator AuthRoutine(string savedPlayerId, Action<PlayerData> onSuccess, Action<string> onError)
        {
            string url = $"{baseUrl}/player/auth";
            string jsonBody = "{\"playerId\":\"" + (savedPlayerId ?? "") + "\"}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AuthResponse res = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    if (res != null && res.success)
                    {
                        CurrentPlayer = res.player;
                        onSuccess?.Invoke(CurrentPlayer);
                    }
                    else
                    {
                        onError?.Invoke("Failed to parse player response");
                    }
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        public void FetchLevel(int levelId, Action<LevelData> onSuccess, Action<string> onError)
        {
            StartCoroutine(FetchLevelRoutine(levelId, onSuccess, onError));
        }

        private IEnumerator FetchLevelRoutine(int levelId, Action<LevelData> onSuccess, Action<string> onError)
        {
            string url = $"{baseUrl}/levels/{levelId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    LevelResponse res = JsonUtility.FromJson<LevelResponse>(request.downloadHandler.text);
                    if (res != null && res.success)
                    {
                        onSuccess?.Invoke(res.level);
                    }
                    else
                    {
                        onError?.Invoke($"Failed to get level {levelId}");
                    }
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        public void SubmitLevelCompletion(int levelId, bool isVictory, int mobsSpawned, Action<LevelCompleteResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SubmitLevelRoutine(levelId, isVictory, mobsSpawned, onSuccess, onError));
        }

        private IEnumerator SubmitLevelRoutine(int levelId, bool isVictory, int mobsSpawned, Action<LevelCompleteResponse> onSuccess, Action<string> onError)
        {
            string url = $"{baseUrl}/levels/complete";
            string jsonBody = $"{{\"playerId\":\"{CurrentPlayer?.playerId}\",\"levelId\":{levelId},\"isVictory\":{isVictory.ToString().ToLower()},\"mobsSpawned\":{mobsSpawned}}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    LevelCompleteResponse res = JsonUtility.FromJson<LevelCompleteResponse>(request.downloadHandler.text);
                    if (res != null && res.success)
                    {
                        CurrentPlayer = res.player;
                        onSuccess?.Invoke(res);
                    }
                    else
                    {
                        onError?.Invoke("Failed to process level completion");
                    }
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        public void ClaimRewardedMultiplier(int levelId, Action<PlayerData> onSuccess, Action<string> onError)
        {
            StartCoroutine(ClaimMultiplierRoutine(levelId, onSuccess, onError));
        }

        private IEnumerator ClaimMultiplierRoutine(int levelId, Action<PlayerData> onSuccess, Action<string> onError)
        {
            string url = $"{baseUrl}/ads/claim-multiplier";
            string jsonBody = $"{{\"playerId\":\"{CurrentPlayer?.playerId}\",\"levelId\":{levelId}}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AuthResponse res = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    if (res != null && res.success)
                    {
                        CurrentPlayer = res.player;
                        onSuccess?.Invoke(res.player);
                    }
                    else
                    {
                        onError?.Invoke("Reward verification error");
                    }
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }
    }
}
