using UnityEngine;
using MobControl.Combat;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// Handles collision detection when the player collides with an obstacle (barricade or container).
    /// If the player fails to dodge, stops road movement immediately and triggers death/game-over state.
    /// </summary>
    public class RoadObstacle : MonoBehaviour
    {
        [SerializeField] private bool isContainer = false;
        [SerializeField] private int damage = 1;
        private BoxCollider boxCol;
        private bool hasTriggered = false;
        public bool HasTriggered => hasTriggered;

        public void TriggerObstacleHit(GameObject hitObj) => HandleHit(hitObj);

        public void Initialize(bool isContainer, int damage)
        {
            this.isContainer = isContainer;
            this.damage = damage;
            boxCol = GetComponent<BoxCollider>();
        }

        private void Awake()
        {
            boxCol = GetComponent<BoxCollider>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.gameObject);
        }

        private void OnCollisionStay(Collision collision)
        {
            HandleHit(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            HandleHit(other.gameObject);
        }

        private void Update()
        {
            if (hasTriggered) return;
            if (MobControl.UI.ZombieRunnerUIManager.Instance != null && MobControl.UI.ZombieRunnerUIManager.Instance.CurrentState != MobControl.UI.ZombieRunnerUIManager.ScreenState.Gameplay) return;
            if (PlayerRunnerController.Instance == null || PlayerRunnerController.Instance.IsDead) return;
            if (PlayerRunnerController.Instance.IsWaitingToStart) return;
            if (PlayerRunnerController.Instance.IsObstacleInvulnerable) return;

            // Direct spatial contact check: detects if the player leader or any squad ally intersects this obstacle
            if (boxCol != null)
            {
                // 1. Check Leader
                Vector3 playerPos = PlayerRunnerController.Instance.transform.position;
                Vector3 pCenter = playerPos + Vector3.up * 0.72f;
                Vector3 closest = boxCol.ClosestPoint(pCenter);
                if (Vector3.Distance(closest, pCenter) < 0.40f)
                {
                    HandleHit(PlayerRunnerController.Instance.gameObject);
                    return;
                }

                // 2. Check each active Squad Ally
                var allies = PlayerRunnerController.Instance.SquadAllies;
                if (allies != null)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        if (allies[i] == null) continue;
                        Vector3 aPos = allies[i].transform.position + Vector3.up * 0.72f;
                        Vector3 aClosest = boxCol.ClosestPoint(aPos);
                        if (Vector3.Distance(aClosest, aPos) < 0.40f)
                        {
                            HandleHit(allies[i].gameObject);
                            return;
                        }
                    }
                }
            }
        }

        private void HandleHit(GameObject hitObj)
        {
            if (hasTriggered) return;
            if (hitObj == null) return;

            // Only trigger obstacle hits during active gameplay
            if (MobControl.UI.ZombieRunnerUIManager.Instance != null && MobControl.UI.ZombieRunnerUIManager.Instance.CurrentState != MobControl.UI.ZombieRunnerUIManager.ScreenState.Gameplay) return;

            // Find player or ally
            PlayerRunnerController player = hitObj.GetComponentInParent<PlayerRunnerController>();
            PlayerSquadAlly ally = hitObj.GetComponentInParent<PlayerSquadAlly>();
            if (player == null && ally != null)
            {
                player = ally.Leader;
            }

            // CRITICAL FIX: Only genuine Player or PlayerSquadAlly collisions trigger obstacle damage/death!
            // Stray collisions from bullets, zombies, debris, or road tiles must NEVER harm or kill the player!
            if (player == null && ally == null)
            {
                return;
            }

            if (player == null || player.IsDead) return;
            if (player.IsWaitingToStart) return;
            if (player.IsObstacleInvulnerable) return;

            // If this is a destructible gate container and it has already been breached, do not trigger death
            DestructibleGate dGate = GetComponent<DestructibleGate>();
            if (dGate != null && dGate.IsBroken) return;

            int currentSquad = player.TotalSquadSize;

            Camera mainCam = Camera.main;
            SmoothCameraFollow camFollow = mainCam != null ? mainCam.GetComponent<SmoothCameraFollow>() : null;

            if (currentSquad > 1)
            {
                hasTriggered = true;

                // 1. Deduct 1 squad member automatically!
                player.DeductSquadMember(ally);

                // 2. Camera Shake for impact
                if (camFollow != null) camFollow.TriggerShake(0.045f);

                // 3. Shatter and destroy this barricade obstacle so player can smoothly pass through!
                ShatterObstacle();

                Debug.Log($"[RoadObstacle] Barricade hit with squad of {currentSquad}! 1 member subtracted, {player.TotalSquadSize} remain. Barricade shattered.");
                return;
            }

            // Only 1 player left: TRY AGAIN / GAME OVER!
            hasTriggered = true;

            // 1. Immediately STOP road movement so the environment does not keep moving independently!
            if (EndlessRoadManager.Instance != null)
            {
                EndlessRoadManager.Instance.SetRunning(false);
            }

            // 2. Camera Shake for heavy crash impact
            if (camFollow != null) camFollow.TriggerShake(isContainer ? 0.08f : 0.05f);

            // 3. Trigger Player Death & Game-Over State
            string obstacleName = gameObject.name.Contains("BurnedCar") ? "a wrecked burned car" : (isContainer ? "a heavy shipping container" : "a road barricade");
            player.Die("CRASHED!", $"You crashed into {obstacleName}.");
            Debug.Log($"[RoadObstacle] Player crashed into {obstacleName} with 1 player left! Road stopped, TRY AGAIN triggered.");
        }

        private void ShatterObstacle()
        {
            if (boxCol != null) boxCol.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }

            // Turn children into dynamic flying debris with physics
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
                if (rb == null) rb = child.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

                Vector3 explosionDir = (child.position - transform.position).normalized;
                explosionDir += new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.4f, 0.8f), Random.Range(0.6f, 1.2f));
                rb.AddForce(explosionDir * Random.Range(4.0f, 8.0f), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 15.0f, ForceMode.Impulse);
            }

            CreateSplinterBurstEffect(transform.position + Vector3.up * 0.4f);
            Destroy(gameObject, 0.8f);
        }

        private void CreateSplinterBurstEffect(Vector3 pos)
        {
            GameObject fxRoot = new GameObject("Barricade_SplinterFX");
            fxRoot.transform.position = pos;

            Material woodMat = new Material(Shader.Find("Standard"));
            woodMat.color = new Color(0.95f, 0.65f, 0.15f);

            for (int i = 0; i < 6; i++)
            {
                GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chip.name = "Splinter";
                chip.transform.position = pos + Random.insideUnitSphere * 0.25f;
                chip.transform.localScale = new Vector3(Random.Range(0.08f, 0.16f), Random.Range(0.08f, 0.16f), Random.Range(0.08f, 0.16f));
                EndlessRoadManager.SafeDestroy(chip.GetComponent<Collider>());
                chip.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

                Rigidbody srb = chip.AddComponent<Rigidbody>();
                srb.isKinematic = false;
                srb.useGravity = true;
                Vector3 blastDir = (Random.insideUnitSphere + Vector3.up * 0.8f).normalized;
                srb.AddForce(blastDir * Random.Range(4f, 8f), ForceMode.Impulse);
                Destroy(chip, 0.65f);
            }

            Destroy(fxRoot, 0.7f);
        }
    }
}
