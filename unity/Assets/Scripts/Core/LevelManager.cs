using System.Collections.Generic;
using UnityEngine;
using MobControl.Network;
using MobControl.Gates;
using MobControl.Base;
using MobControl.Crowd;
using MobControl.Cannon;

namespace MobControl.Core
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private GameObject gatePrefab;
        [SerializeField] private Transform gateContainer;
        [SerializeField] private FinishLine finishLine;
        [SerializeField] private BaseTarget playerBase;
        [SerializeField] private BaseTarget enemyBase;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private CannonController cannonController;

        private List<GameObject> activeGates = new List<GameObject>();
        public LevelData CurrentLevelData { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void LoadLevel(LevelData levelData)
        {
            CurrentLevelData = levelData;
            ClearActiveLevel();

            // 1. Initialize Bases / Finish Line
            if (finishLine != null) finishLine.InitializeFinish(levelData.enemyBaseHealth > 0 ? levelData.enemyBaseHealth : 20);
            if (playerBase != null) playerBase.InitializeBase(levelData.playerBaseHealth);
            if (enemyBase != null) enemyBase.InitializeBase(levelData.enemyBaseHealth);

            // 2. Setup Cannon Fire Rate
            if (cannonController != null)
            {
                cannonController.SetFireRate(levelData.cannonFireRate);
            }

            // 3. Instantiate Gates
            if (levelData.gates != null && gatePrefab != null)
            {
                foreach (var gateData in levelData.gates)
                {
                    GameObject gateObj = Instantiate(gatePrefab, gateData.position, Quaternion.identity, gateContainer);
                    if (gateObj.TryGetComponent<MultiplierGate>(out MultiplierGate gate))
                    {
                        GateOperation op = GateOperation.Multiply;
                        if (gateData.operation == "add") op = GateOperation.Add;
                        else if (gateData.operation == "subtract") op = GateOperation.Subtract;

                        gate.SetupGate(op, gateData.value, gateData.isMoving, gateData.moveSpeed, gateData.moveRange);
                    }
                    activeGates.Add(gateObj);
                }
            }

            // 4. Start Enemy Waves
            if (enemySpawner != null && levelData.enemyWaves != null)
            {
                List<EnemyWaveConfig> waveConfigs = new List<EnemyWaveConfig>();
                foreach (var w in levelData.enemyWaves)
                {
                    waveConfigs.Add(new EnemyWaveConfig
                    {
                        waveIndex = w.waveIndex,
                        spawnDelay = w.spawnDelay,
                        count = w.count,
                        spawnInterval = w.spawnInterval,
                        mobSpeed = w.mobSpeed,
                        mobHealth = w.mobHealth
                    });
                }
                enemySpawner.StartWaves(waveConfigs);
            }
        }

        public void ClearActiveLevel()
        {
            foreach (var g in activeGates)
            {
                if (g != null) Destroy(g);
            }
            activeGates.Clear();

            if (enemySpawner != null)
            {
                enemySpawner.StopAllWaves();
            }
        }
    }
}
