using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MobControl.Core;
using MobControl.Network;
using MobControl.Ads;

namespace MobControl.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI gemsText;

        [Header("Screens")]
        [SerializeField] private GameObject menuScreen;
        [SerializeField] private GameObject gameplayScreen;
        [SerializeField] private GameObject victoryScreen;
        [SerializeField] private GameObject defeatScreen;

        [Header("Victory Screen Elements")]
        [SerializeField] private TextMeshProUGUI victoryRewardText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button claim3xAdButton;

        private int pendingRewardedLevelId;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            if (claim3xAdButton != null) claim3xAdButton.onClick.AddListener(OnClaim3xClicked);
        }

        public void UpdatePlayerCurrency(int coins, int gems)
        {
            if (coinsText != null) coinsText.text = coins.ToString();
            if (gemsText != null) gemsText.text = gems.ToString();
        }

        public void ShowMenu()
        {
            menuScreen.SetActive(true);
            gameplayScreen.SetActive(false);
            victoryScreen.SetActive(false);
            defeatScreen.SetActive(false);
        }

        public void ShowGameplay(int levelNumber, string levelName)
        {
            menuScreen.SetActive(false);
            gameplayScreen.SetActive(true);
            victoryScreen.SetActive(false);
            defeatScreen.SetActive(false);

            if (levelTitleText != null)
            {
                levelTitleText.text = $"Level {levelNumber}: {levelName}";
            }
        }

        public void ShowVictory(int levelId, int earnedCoins, int earnedGems, bool adMultiplierAvailable)
        {
            pendingRewardedLevelId = levelId;
            gameplayScreen.SetActive(false);
            victoryScreen.SetActive(true);

            if (victoryRewardText != null)
            {
                victoryRewardText.text = $"+{earnedCoins} Coins  +{earnedGems} Gems";
            }

            if (claim3xAdButton != null)
            {
                claim3xAdButton.gameObject.SetActive(adMultiplierAvailable);
            }
        }

        public void ShowDefeat()
        {
            gameplayScreen.SetActive(false);
            defeatScreen.SetActive(true);
        }

        private void OnNextLevelClicked()
        {
            GameManager.Instance.ProceedToNextLevel();
        }

        private void OnClaim3xClicked()
        {
            AdManager.Instance.ShowRewardedAd(
                onRewardEarned: () =>
                {
                    ApiClient.Instance.ClaimRewardedMultiplier(
                        pendingRewardedLevelId,
                        onSuccess: (updatedPlayer) =>
                        {
                            UpdatePlayerCurrency(updatedPlayer.coins, updatedPlayer.gems);
                            if (claim3xAdButton != null) claim3xAdButton.gameObject.SetActive(false);
                        },
                        onError: (err) => Debug.LogError("[UIManager] Error claiming multiplier: " + err)
                    );
                }
            );
        }
    }
}
