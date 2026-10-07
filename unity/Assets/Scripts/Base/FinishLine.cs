using System.Collections;
using UnityEngine;
using TMPro;
using MobControl.Core;
using MobControl.Crowd;

namespace MobControl.Base
{
    public class FinishLine : MonoBehaviour
    {
        public static FinishLine Instance { get; private set; }

        [Header("Finish Line Configuration")]
        [SerializeField] private int requiredMobsToWin = 10;
        [SerializeField] private TextMeshPro mobCounterText;
        [SerializeField] private ParticleSystem confettiVfx;

        private int mobsCrossedCount = 0;
        private bool isFinished = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void InitializeFinish(int requiredMobs)
        {
            requiredMobsToWin = requiredMobs;
            mobsCrossedCount = 0;
            isFinished = false;
            UpdateDisplay();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isFinished) return;

            if (other.TryGetComponent<MobUnit>(out MobUnit mob))
            {
                if (mob.Team == MobTeam.Player)
                {
                    mobsCrossedCount++;
                    UpdateDisplay();

                    // Return mob to pool after crossing finish
                    ObjectPooler.Instance.ReturnToPool("PlayerMob", mob.gameObject);

                    if (!isFinished && mobsCrossedCount >= requiredMobsToWin)
                    {
                        isFinished = true;
                        StartCoroutine(TriggerVictorySequence());
                    }
                }
            }
        }

        private void UpdateDisplay()
        {
            if (mobCounterText != null)
            {
                mobCounterText.text = $"{mobsCrossedCount}/{requiredMobsToWin}";
            }
        }

        private IEnumerator TriggerVictorySequence()
        {
            if (confettiVfx != null)
            {
                confettiVfx.Play();
            }

            yield return new WaitForSeconds(1.0f);
            GameManager.Instance.OnLevelWon();
        }
    }
}
