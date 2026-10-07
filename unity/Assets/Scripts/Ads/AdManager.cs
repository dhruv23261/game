using System;
using UnityEngine;

namespace MobControl.Ads
{
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("Ad Settings")]
        [SerializeField] private bool testMode = true;
        public bool TestMode => testMode;

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

        public void ShowInterstitialAd(Action onCompleted = null)
        {
            Debug.Log("[AdManager] Showing Interstitial Ad...");
            // In editor or test mode, simulate ad display
            onCompleted?.Invoke();
        }

        public void ShowRewardedAd(Action onRewardEarned, Action onAdClosed = null)
        {
            Debug.Log("[AdManager] Showing Rewarded Video Ad (3x Coins)...");
            // Simulate successful watched ad
            onRewardEarned?.Invoke();
            onAdClosed?.Invoke();
        }
    }
}
