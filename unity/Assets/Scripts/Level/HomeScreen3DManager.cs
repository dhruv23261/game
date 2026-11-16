using System.Collections.Generic;
using UnityEngine;
using MobControl.Core;
using MobControl.Crowd;
using MobControl.Combat;
using MobControl.UI;

namespace MobControl.Level
{
    /// <summary>
    /// Coordinates home screen state and gameplay camera/entity transitions.
    /// The authentic Zombie Squad home screen is presented via the ultra-high resolution
    /// master artwork canvas in ZombieRunnerUIManager.
    /// </summary>
    public class HomeScreen3DManager : MonoBehaviour
    {
        public static HomeScreen3DManager Instance { get; private set; }

        public const string ROOT_NAME = "[HomeScreen_3D_Scene]";

        [Header("State")]
        [SerializeField] private bool isHomeActive = false;

        private Camera mainCamera;

        // Gameplay Elevated Top Camera (matches Level 1 reference view)
        private readonly Vector3 gameplayCamPos = new Vector3(0f, 5.80f, -6.80f);
        private readonly Vector3 gameplayCamRot = new Vector3(29.0f, 0f, 0f);

        public static HomeScreen3DManager EnsureInstance()
        {
            if (Instance == null)
            {
                HomeScreen3DManager existing = FindAnyObjectByType<HomeScreen3DManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[HomeScreen3DManager]");
                    Instance = go.AddComponent<HomeScreen3DManager>();
                }
            }
            Instance.CleanStaleRoots();
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                SafeDestroy(gameObject);
                return;
            }

            mainCamera = Camera.main;
            CleanStaleRoots();
        }

        public void CleanStaleRoots()
        {
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r != null && r.name.Contains(ROOT_NAME))
                {
                    SafeDestroy(r);
                }
            }
        }

        public void SetHomeMode(bool active)
        {
            isHomeActive = active;
            CleanStaleRoots();

            if (!active)
            {
                if (!MobControl.Level.ArmyRunLevel.Enabled)
                {
                    // Restore gameplay elements
                    var players = FindObjectsByType<PlayerRunnerController>(FindObjectsInactive.Include);
                    foreach (var p in players)
                    {
                        if (p == null) continue;
                        p.gameObject.SetActive(true);
                        p.enabled = true;
                        foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                        {
                            r.enabled = true;
                        }
                    }

                    var roads = FindObjectsByType<EndlessRoadManager>(FindObjectsInactive.Include);
                    foreach (var r in roads)
                    {
                        if (r != null) r.gameObject.SetActive(true);
                    }

                    var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (var go in roots)
                    {
                        if (go == null || go.name.Contains(ROOT_NAME)) continue;

                        if (go.name.Contains("Player_Hero") || go.name.Contains("Level1_StylizedRoad_Root"))
                        {
                            go.SetActive(true);
                            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                            {
                                r.enabled = true;
                            }
                        }
                    }

                    var pooler = ObjectPooler.Instance;
                    if (pooler != null)
                    {
                        pooler.transform.position = Vector3.zero;
                        foreach (var r in pooler.GetComponentsInChildren<Renderer>(true))
                        {
                            r.enabled = true;
                        }
                    }
                }
            }

            if (mainCamera == null) mainCamera = Camera.main;

            if (mainCamera != null && !MobControl.Level.ArmyRunLevel.Enabled)
            {
                SmoothCameraFollow follow = mainCamera.GetComponent<SmoothCameraFollow>();
                if (follow != null)
                {
                    follow.enabled = !active;
                }

                if (!active)
                {
                    mainCamera.transform.position = gameplayCamPos;
                    mainCamera.transform.rotation = Quaternion.Euler(gameplayCamRot);
                    mainCamera.fieldOfView = 58f;
                }
            }
        }

        public void BuildOrVerifyScene(bool forceRebuild = false)
        {
            CleanStaleRoots();
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
