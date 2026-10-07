using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MobControl.Core;

namespace MobControl.Crowd
{
    [System.Serializable]
    public class EnemyWaveConfig
    {
        public int waveIndex;
        public float spawnDelay;
        public int count;
        public float spawnInterval;
        public float mobSpeed;
        public int mobHealth;
    }

    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private float spawnSpreadX = 3.0f;
        private List<Coroutine> activeWaveCoroutines = new List<Coroutine>();

        public void StartWaves(List<EnemyWaveConfig> waves)
        {
            StopAllWaves();

            foreach (var wave in waves)
            {
                Coroutine c = StartCoroutine(RunWave(wave));
                activeWaveCoroutines.Add(c);
            }
        }

        public void StopAllWaves()
        {
            foreach (var c in activeWaveCoroutines)
            {
                if (c != null) StopCoroutine(c);
            }
            activeWaveCoroutines.Clear();
        }

        private IEnumerator RunWave(EnemyWaveConfig wave)
        {
            yield return new WaitForSeconds(wave.spawnDelay);

            for (int i = 0; i < wave.count; i++)
            {
                Vector3 spawnPos = transform.position + new Vector3(Random.Range(-spawnSpreadX, spawnSpreadX), 0f, 0f);
                GameObject enemyObj = ObjectPooler.Instance.SpawnFromPool("EnemyMob", spawnPos, Quaternion.Euler(0, 180, 0));

                if (enemyObj != null && enemyObj.TryGetComponent<MobUnit>(out MobUnit unit))
                {
                    unit.SetStats(wave.mobSpeed, wave.mobHealth, 1);
                }

                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }
    }
}
