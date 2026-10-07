using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MobControl.Core;

namespace MobControl.UI
{
    /// <summary>
    /// Controls the initial Game Interface, including:
    /// 1. Login / Player Profile Screen (Name entry, guest play)
    /// 2. Level Selection Screen (Level 1, 2, 3... with unlock states and stars)
    /// 3. Currency HUD (Player name, coins, gems)
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject levelSelectPanel;

        [Header("Top Bar / Header HUD")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI gemsText;

        [Header("Login Panel Elements")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button loginSubmitButton;
        [SerializeField] private Button playAsGuestButton;
        [SerializeField] private TextMeshProUGUI welcomeMessageText;

        [Header("Level Select Panel Elements")]
        [SerializeField] private Transform levelButtonsContainer;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private Button backToLoginButton;
        [SerializeField] private string level1SceneName = "q"; // Current runner scene

        [Header("Visual Colors for Level Buttons")]
        [SerializeField] private Color unlockedColor = new Color(0.12f, 0.75f, 0.42f, 1f); // Vibrant Emerald Green
        [SerializeField] private Color lockedColor = new Color(0.40f, 0.45f, 0.50f, 0.6f); // Soft Slate Gray
        [SerializeField] private Color currentLevelColor = new Color(0.95f, 0.65f, 0.15f, 1f); // Gold highlight

        private void Awake()
        {
            if (Instance == null) Instance = this;
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            // Setup button listeners
            if (loginSubmitButton != null) loginSubmitButton.onClick.AddListener(OnLoginSubmitted);
            if (playAsGuestButton != null) playAsGuestButton.onClick.AddListener(OnPlayAsGuest);
            if (backToLoginButton != null) backToLoginButton.onClick.AddListener(ShowLoginPanel);

            UpdateTopBarHUD();

            // Check if player has already entered a name
            string existingName = LevelProgressionManager.Instance != null 
                ? LevelProgressionManager.Instance.PlayerName 
                : PlayerPrefs.GetString("mc_player_name", "");

            if (!string.IsNullOrEmpty(existingName) && existingName != "Survivor")
            {
                // Player already logged in previously -> show Level Select directly
                if (welcomeMessageText != null) welcomeMessageText.text = $"Welcome back, <b>{existingName}</b>!";
                ShowLevelSelectPanel();
            }
            else
            {
                // First-time launch -> show Login / Name entry panel
                ShowLoginPanel();
            }
        }

        public void ShowLoginPanel()
        {
            if (loginPanel != null) loginPanel.SetActive(true);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);

            if (nameInputField != null && LevelProgressionManager.Instance != null)
            {
                nameInputField.text = LevelProgressionManager.Instance.PlayerName;
            }
        }

        public void ShowLevelSelectPanel()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(true);

            UpdateTopBarHUD();
            PopulateLevelButtons();
        }

        private void OnLoginSubmitted()
        {
            string enteredName = nameInputField != null ? nameInputField.text : "Player 1";
            if (string.IsNullOrWhiteSpace(enteredName)) enteredName = "Player 1";

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.LoginPlayer(enteredName);
            }
            else
            {
                PlayerPrefs.SetString("mc_player_name", enteredName);
                PlayerPrefs.Save();
            }

            ShowLevelSelectPanel();
        }

        private void OnPlayAsGuest()
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.LoginPlayer("Guest_" + Random.Range(100, 999));
            }
            ShowLevelSelectPanel();
        }

        public void UpdateTopBarHUD()
        {
            if (LevelProgressionManager.Instance != null)
            {
                if (playerNameText != null) playerNameText.text = LevelProgressionManager.Instance.PlayerName;
                if (coinsText != null) coinsText.text = LevelProgressionManager.Instance.Coins.ToString("N0");
                if (gemsText != null) gemsText.text = LevelProgressionManager.Instance.Gems.ToString("N0");
            }
            else
            {
                if (playerNameText != null) playerNameText.text = PlayerPrefs.GetString("mc_player_name", "Survivor");
                if (coinsText != null) coinsText.text = "100";
                if (gemsText != null) gemsText.text = "10";
            }
        }

        /// <summary>
        /// Populates level grid (Level 1, 2, 3...) with dynamic locks and star ratings.
        /// </summary>
        private void PopulateLevelButtons()
        {
            if (levelButtonsContainer == null) return;

            int totalLevels = LevelProgressionManager.Instance != null ? LevelProgressionManager.Instance.TotalLevels : 6;
            int highestUnlocked = LevelProgressionManager.Instance != null ? LevelProgressionManager.Instance.HighestUnlockedLevel : 1;

            // If manual buttons already exist inside container, configure them
            int childCount = levelButtonsContainer.childCount;
            if (childCount > 0 && levelButtonPrefab == null)
            {
                for (int i = 0; i < childCount; i++)
                {
                    int levelNumber = i + 1;
                    Transform btnTransform = levelButtonsContainer.GetChild(i);
                    ConfigureLevelButton(btnTransform.gameObject, levelNumber, levelNumber <= highestUnlocked, levelNumber == highestUnlocked);
                }
                return;
            }

            // Otherwise, dynamically spawn level buttons from prefab
            if (levelButtonPrefab != null)
            {
                // Clear existing
                foreach (Transform child in levelButtonsContainer)
                {
                    Destroy(child.gameObject);
                }

                for (int i = 1; i <= totalLevels; i++)
                {
                    int levelNumber = i;
                    GameObject btnObj = Instantiate(levelButtonPrefab, levelButtonsContainer);
                    ConfigureLevelButton(btnObj, levelNumber, levelNumber <= highestUnlocked, levelNumber == highestUnlocked);
                }
            }
        }

        private void ConfigureLevelButton(GameObject btnObj, int levelNumber, bool isUnlocked, bool isCurrent)
        {
            Button btn = btnObj.GetComponent<Button>();
            Image bgImage = btnObj.GetComponent<Image>();
            TextMeshProUGUI label = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Find optional Lock icon and Stars container if present
            Transform lockIcon = btnObj.transform.Find("LockIcon");
            Transform starsContainer = btnObj.transform.Find("StarsContainer");

            if (label != null)
            {
                label.text = isUnlocked ? $"LEVEL {levelNumber}" : $"<color=#888888>LOCKED</color>";
            }

            if (bgImage != null)
            {
                if (!isUnlocked) bgImage.color = lockedColor;
                else if (isCurrent) bgImage.color = currentLevelColor;
                else bgImage.color = unlockedColor;
            }

            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(!isUnlocked);
            }

            if (starsContainer != null)
            {
                starsContainer.gameObject.SetActive(isUnlocked);
                int stars = LevelProgressionManager.Instance != null ? LevelProgressionManager.Instance.GetLevelStars(levelNumber) : 0;
                // Update star icons inside container (e.g. 1st, 2nd, 3rd star)
                for (int s = 0; s < starsContainer.childCount; s++)
                {
                    starsContainer.GetChild(s).gameObject.SetActive(s < stars);
                }
            }

            if (btn != null)
            {
                btn.interactable = isUnlocked;
                btn.onClick.RemoveAllListeners();
                if (isUnlocked)
                {
                    btn.onClick.AddListener(() => LaunchLevel(levelNumber));
                }
            }
        }

        public void LaunchLevel(int levelNumber)
        {
            Debug.Log($"[MainMenu] Launching Level {levelNumber}...");
            PlayerPrefs.SetInt("ActivePlayingLevel", levelNumber);
            PlayerPrefs.Save();

            // If Level 1, load scene "q"
            if (levelNumber == 1)
            {
                SceneManager.LoadScene(level1SceneName);
            }
            else
            {
                // If specific scene exists e.g. "Level2", load it; otherwise load "q" with difficulty parameter
                string targetScene = $"Level{levelNumber}";
                if (Application.CanStreamedLevelBeLoaded(targetScene))
                {
                    SceneManager.LoadScene(targetScene);
                }
                else
                {
                    // Fallback to primary runner track
                    SceneManager.LoadScene(level1SceneName);
                }
            }
        }
    }
}
