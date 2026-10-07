using System.Collections.Generic;
using UnityEngine;

namespace MobControl.Core
{
    public class ObjectPooler : MonoBehaviour
    {
        private static ObjectPooler _instance;
        public static ObjectPooler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ObjectPooler>();
                    if (_instance == null)
                    {
                        GameObject poolerObj = new GameObject("[ObjectPooler_Auto]");
                        _instance = poolerObj.AddComponent<ObjectPooler>();
                    }
                }
                return _instance;
            }
        }

        [System.Serializable]
        public class PoolItem
        {
            public string tag;
            public GameObject prefab;
            public int size = 100;
        }

        [Header("Pool Configurations")]
        [SerializeField] private List<PoolItem> poolItems = new List<PoolItem>();

        private Dictionary<string, Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializePools();
        }

        private void InitializePools()
        {
            if (poolDictionary == null)
                poolDictionary = new Dictionary<string, Queue<GameObject>>();

            foreach (var item in poolItems)
            {
                if (item.prefab == null || poolDictionary.ContainsKey(item.tag)) continue;

                Queue<GameObject> objectPool = new Queue<GameObject>();
                GameObject parentGroup = new GameObject($"Pool_{item.tag}");
                parentGroup.transform.SetParent(transform);

                for (int i = 0; i < item.size; i++)
                {
                    GameObject obj = Instantiate(item.prefab, parentGroup.transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(item.tag, objectPool);
            }
        }

        public bool HasPool(string tag)
        {
            if (poolDictionary == null) InitializePools();
            return poolDictionary != null && poolDictionary.ContainsKey(tag);
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (poolDictionary == null) InitializePools();

            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPooler] Pool with tag '{tag}' doesn't exist.");
                return null;
            }

            Queue<GameObject> queue = poolDictionary[tag];
            GameObject objToSpawn = null;

            while (queue.Count > 0 && objToSpawn == null)
            {
                objToSpawn = queue.Dequeue();
            }

            if (objToSpawn == null)
            {
                // Expand pool dynamically if depleted or destroyed
                var itemConfig = poolItems.Find(p => p.tag == tag);
                if (itemConfig != null && itemConfig.prefab != null)
                {
                    objToSpawn = Instantiate(itemConfig.prefab, transform);
                }
                else
                {
                    return null;
                }
            }

            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;
            objToSpawn.SetActive(true);

            return objToSpawn;
        }

        public void ReturnToPool(string tag, GameObject obj)
        {
            if (obj == null) return;

            if (poolDictionary == null || !poolDictionary.ContainsKey(tag))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            poolDictionary[tag].Enqueue(obj);
        }

        /// <summary>Dynamically register a pool at runtime (e.g. from PlayerRunnerController or Bullet).</summary>
        public void RegisterPool(string tag, GameObject prefab, int size)
        {
            if (prefab == null) return;
            if (poolDictionary == null) InitializePools();

            if (poolDictionary.ContainsKey(tag)) return; // Already registered

            if (poolItems == null) poolItems = new List<PoolItem>();
            poolItems.Add(new PoolItem { tag = tag, prefab = prefab, size = size });

            Queue<GameObject> objectPool = new Queue<GameObject>();
            GameObject parentGroup = new GameObject($"Pool_{tag}");
            parentGroup.transform.SetParent(transform);

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, parentGroup.transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(tag, objectPool);
            Debug.Log($"[ObjectPooler] Registered pool '{tag}' with {size} objects.");
        }
    }
}
