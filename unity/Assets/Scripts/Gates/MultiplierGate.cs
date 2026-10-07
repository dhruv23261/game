using UnityEngine;
using TMPro;
using MobControl.Core;
using MobControl.Crowd;
using MobControl.Combat;

namespace MobControl.Gates
{
    public enum GateOperation
    {
        Multiply,
        Add,
        Subtract
    }

    public class MultiplierGate : MonoBehaviour
    {
        [Header("Gate Configuration")]
        [SerializeField] private GateOperation operation = GateOperation.Multiply;
        [SerializeField] private int value = 2;
        [SerializeField] private TextMeshPro textLabel;
        [SerializeField] private MeshRenderer gateRenderer;
        [SerializeField] private SpriteRenderer iconRenderer; // Character/Unit thumbnail icon

        [Header("Movement Settings")]
        [SerializeField] private bool isMoving = false;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float moveRange = 2.0f;

        [Header("Materials / Colors")]
        [SerializeField] private Material positiveMaterial;
        [SerializeField] private Material negativeMaterial;

        private static int nextGateId = 1;
        private Vector3 startPosition;
        private int gateInstanceId;

        public int GateInstanceId => gateInstanceId;
        public int Value => value;
        public GateOperation Operation => operation;

        private void Awake()
        {
            gateInstanceId = nextGateId++;
            startPosition = transform.position;
            UpdateVisuals();
        }

        private void OnValidate()
        {
            // Update 3D text and color in real-time while editing in Scene
            UpdateVisuals();
        }

        public void SetupGate(GateOperation op, int val, bool moving = false, float speed = 0f, float range = 0f)
        {
            operation = op;
            value = val;
            isMoving = moving;
            moveSpeed = speed;
            moveRange = range;
            startPosition = transform.position;
            UpdateVisuals();
        }

        private void Update()
        {
            if (isMoving && moveRange > 0f)
            {
                float offset = Mathf.Sin(Time.time * moveSpeed) * moveRange;
                transform.position = new Vector3(startPosition.x + offset, transform.position.y, transform.position.z);
            }
        }

        public void UpdateVisuals()
        {
            string labelStr = "";
            bool isPositive = true;

            switch (operation)
            {
                case GateOperation.Multiply:
                    labelStr = $"x{value}";
                    isPositive = value >= 1;
                    break;
                case GateOperation.Add:
                    labelStr = $"+{value}";
                    isPositive = true;
                    break;
                case GateOperation.Subtract:
                    labelStr = $"-{value}";
                    isPositive = false;
                    break;
            }

            if (textLabel != null)
            {
                textLabel.text = labelStr;
                textLabel.color = isPositive ? new Color(0.2f, 0.9f, 1f, 1f) : new Color(1f, 0.25f, 0.25f, 1f);
            }

            if (gateRenderer != null)
            {
                if (isPositive && positiveMaterial != null)
                    gateRenderer.material = positiveMaterial;
                else if (!isPositive && negativeMaterial != null)
                    gateRenderer.material = negativeMaterial;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 1. Check Player Runner (spawns squad allies)
            PlayerRunnerController runner = other.GetComponentInParent<PlayerRunnerController>();
            if (runner != null && runner == PlayerRunnerController.Instance)
            {
                if (runner.HasPassedGate(gateInstanceId)) return;
                runner.MarkGatePassed(gateInstanceId);

                int currentSquad = runner.TotalSquadSize;
                int targetSquad = (operation == GateOperation.Multiply)
                    ? (currentSquad * value)
                    : (currentSquad + value);
                int alliesToSpawn = Mathf.Max(0, targetSquad - currentSquad);
                runner.SpawnSquadAllies(alliesToSpawn);
                return;
            }

            // 2. Check Player Mobs
            if (other.TryGetComponent<MobUnit>(out MobUnit mob))
            {
                // Only player mobs multiply through player-facing gates
                if (mob.Team != MobTeam.Player) return;

                if (mob.HasPassedGate(gateInstanceId)) return;
                mob.MarkGatePassed(gateInstanceId);

                ApplyMultiplier(mob);
            }
        }

        private void ApplyMultiplier(MobUnit originalMob)
        {
            int extraMobsToSpawn = 0;

            switch (operation)
            {
                case GateOperation.Multiply:
                    extraMobsToSpawn = Mathf.Max(0, value - 1);
                    break;
                case GateOperation.Add:
                    extraMobsToSpawn = value;
                    break;
                case GateOperation.Subtract:
                    originalMob.TakeDamage(value);
                    return;
            }

            for (int i = 0; i < extraMobsToSpawn; i++)
            {
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    0f,
                    Random.Range(0.2f, 0.6f)
                );

                Vector3 spawnPos = originalMob.transform.position + spawnOffset;
                GameObject clone = null;
                if (ObjectPooler.Instance != null && ObjectPooler.Instance.HasPool("PlayerMob"))
                {
                    clone = ObjectPooler.Instance.SpawnFromPool("PlayerMob", spawnPos, originalMob.transform.rotation);
                }
                if (clone == null)
                {
                    clone = Instantiate(originalMob.gameObject, spawnPos, originalMob.transform.rotation);
                }

                if (clone != null && clone.TryGetComponent<MobUnit>(out MobUnit cloneUnit))
                {
                    cloneUnit.MarkGatePassed(gateInstanceId);
                }
            }
        }
    }
}
