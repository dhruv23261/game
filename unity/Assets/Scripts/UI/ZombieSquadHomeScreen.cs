using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MobControl.UI
{
    /// <summary>
    /// Manages the Zombie Squad Home Screen UI:
    /// Handles Play button (transitions to Level Map or gameplay scene),
    /// Shop, Characters, Weapons, Missions, Daily, Upgrade, and Settings buttons.
    /// Includes juicy punch-scale click animations on tap!
    /// </summary>
    public class ZombieSquadHomeScreen : MonoBehaviour
    {
        [Header("Main Buttons (Transparent Overlays)")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private Button weaponsButton;
        [SerializeField] private Button missionsButton;
        [SerializeField] private Button dailyButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button settingsButton;

        [Header("Character Hero Preview (Blue Toy Soldier Showcase)")]
        [Tooltip("RawImage or Image displaying the hero character preview")]
        [SerializeField] private RawImage heroRawImage;
        [Tooltip("Optional direct 3D character in the scene")]
        [SerializeField] private GameObject hero3DModel;
        [Tooltip("Animator playing Fist Fight A / combat idle")]
        [SerializeField] private Animator heroAnimator;

        [Header("Navigation Target Panels / Scenes")]
        [Tooltip("Optional: If you have a Level Map panel in the same canvas, assign it here.")]
        [SerializeField] private GameObject levelMapPanel;
        [Tooltip("Optional: If you have a Characters panel in the same canvas, assign it here.")]
        [SerializeField] private GameObject charactersPanel;
        [Tooltip("Optional: If you have an Upgrade panel in the same canvas, assign it here.")]
        [SerializeField] private GameObject upgradePanel;
        [Tooltip("Optional: If you have a Weapons panel in the same canvas, assign it here.")]
        [SerializeField] private GameObject weaponsPanel;
        [Tooltip("Optional: If you have a Shop panel in the same canvas, assign it here.")]
        [SerializeField] private GameObject shopPanel;
        [Tooltip("Optional: If you have a Missions panel in the same canvas, assign it here.")]
        [SerializeField] private GameObject missionsPanel;
        [Tooltip("Optional: If you have a Daily panel in the same canvas, assign it here.")]
        [SerializeField] private GameObject dailyPanel;
        [Tooltip("Scene name to load when PLAY is tapped if no LevelMap panel is assigned.")]
        [SerializeField] private string gameplaySceneName = "q";

        private void Awake()
        {
            // Auto-wire hero tap reaction if RawImage has button or can be clicked
            if (heroRawImage != null)
            {
                Button heroBtn = heroRawImage.GetComponent<Button>();
                if (heroBtn == null) heroBtn = heroRawImage.gameObject.AddComponent<Button>();
                heroBtn.transition = Selectable.Transition.None;
                heroBtn.onClick.AddListener(OnHeroClicked);
            }
            // Auto-wire buttons if assigned
            if (playButton != null) playButton.onClick.AddListener(() => OnButtonClicked(playButton, OnPlayClicked));
            if (shopButton != null) shopButton.onClick.AddListener(() => OnButtonClicked(shopButton, OnShopClicked));
            if (charactersButton != null) charactersButton.onClick.AddListener(() => OnButtonClicked(charactersButton, OnCharactersClicked));
            if (weaponsButton != null) weaponsButton.onClick.AddListener(() => OnButtonClicked(weaponsButton, OnWeaponsClicked));
            if (missionsButton != null) missionsButton.onClick.AddListener(() => OnButtonClicked(missionsButton, OnMissionsClicked));
            if (dailyButton != null) dailyButton.onClick.AddListener(() => OnButtonClicked(dailyButton, OnDailyClicked));
            if (upgradeButton != null) upgradeButton.onClick.AddListener(() => OnButtonClicked(upgradeButton, OnUpgradeClicked));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => OnButtonClicked(settingsButton, OnSettingsClicked));
        }

        private void OnButtonClicked(Button btn, System.Action action)
        {
            // Zero-delay immediate trigger!
            action?.Invoke();
            // Juicy click punch bounce
            StartCoroutine(PunchScale(btn.transform, 0.12f, null));
        }

        private IEnumerator PunchScale(Transform tr, float duration, System.Action onComplete)
        {
            Vector3 originalScale = tr.localScale;
            Vector3 punchScale = originalScale * 0.92f;
            float elapsed = 0f;

            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                tr.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / (duration * 0.5f));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                tr.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / (duration * 0.5f));
                yield return null;
            }

            tr.localScale = originalScale;
            onComplete?.Invoke();
        }

        public void OnPlayClicked()
        {
            Debug.Log("[ZombieSquad] PLAY Clicked!");

            if (levelMapPanel != null)
            {
                // Open Level Map screen
                gameObject.SetActive(false);
                levelMapPanel.SetActive(true);
            }
            else
            {
                // Load runner scene directly
                SceneManager.LoadScene(gameplaySceneName);
            }
        }

        public void OnShopClicked()
        {
            Debug.Log("[ZombieSquad] Shop Clicked!");
            if (shopPanel != null)
            {
                gameObject.SetActive(false);
                shopPanel.SetActive(true);
            }
            else
            {
                ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                    ? ZombieRunnerUIManager.Instance 
                    : ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.ShowZombieSquadShopScreen();
                }
            }
        }

        public void OnCharactersClicked()
        {
            Debug.Log("[ZombieSquad] Characters Clicked!");
            if (charactersPanel != null)
            {
                gameObject.SetActive(false);
                charactersPanel.SetActive(true);
            }
            else
            {
                ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                    ? ZombieRunnerUIManager.Instance 
                    : ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.ShowZombieSquadCharactersScreen();
                }
            }
        }

        public void OnWeaponsClicked()
        {
            Debug.Log("[ZombieSquad] Weapons Clicked!");
            if (weaponsPanel != null)
            {
                gameObject.SetActive(false);
                weaponsPanel.SetActive(true);
            }
            else
            {
                ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                    ? ZombieRunnerUIManager.Instance 
                    : ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.ShowZombieSquadWeaponsScreen();
                }
            }
        }

        public void OnMissionsClicked()
        {
            Debug.Log("[ZombieSquad] Missions Clicked!");
            if (missionsPanel != null)
            {
                gameObject.SetActive(false);
                missionsPanel.SetActive(true);
            }
            else
            {
                ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                    ? ZombieRunnerUIManager.Instance 
                    : ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.ShowZombieSquadMissionsScreen();
                }
            }
        }

        public void OnDailyClicked()
        {
            Debug.Log("[ZombieSquad] Daily Rewards Clicked!");
            if (dailyPanel != null)
            {
                gameObject.SetActive(false);
                dailyPanel.SetActive(true);
            }
            else
            {
                ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                    ? ZombieRunnerUIManager.Instance 
                    : ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.ShowZombieSquadDailyScreen();
                }
            }
        }

        public void OnUpgradeClicked()
        {
            Debug.Log("[ZombieSquad] Upgrade Clicked!");
            if (upgradePanel != null)
            {
                gameObject.SetActive(false);
                upgradePanel.SetActive(true);
            }
            else
            {
                ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                    ? ZombieRunnerUIManager.Instance 
                    : ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.ShowZombieSquadUpgradeScreen();
                }
            }
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[ZombieSquad] Settings Clicked!");
        }

        public void OnHeroClicked()
        {
            Debug.Log("[ZombieSquad] Hero Soldier tapped!");
            if (heroRawImage != null)
            {
                StartCoroutine(PunchScale(heroRawImage.transform, 0.15f, null));
            }
            if (heroAnimator != null)
            {
                heroAnimator.SetTrigger("attackTrigger");
            }
        }
    }
}
