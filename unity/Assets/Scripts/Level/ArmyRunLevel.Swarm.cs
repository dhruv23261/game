using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MobControl.Level
{
    /// <summary>
    /// THE RED SWARM (Mob Control "Cave Spike Roller" reference).
    ///
    ///  • Thousands of small red blob mobs fill the main lane like a carpet (AddSwarm). They are plain data
    ///    (no GameObjects) and are drawn with GPU instancing, so 3000+ on screen is cheap.
    ///  • Within 30 m they shuffle toward you. Within 5.5 m of the army they RUN at it.
    ///  • A mob that reaches the army trades 1-for-1 (a big mob kills 3).
    ///  • Bullets PIERCE: a bullet carrying N damage kills N mobs in a row, so a big army mows lanes through.
    ///  • The road slows to a crawl when the swarm front is close: you only push forward as fast as you kill.
    ///  • Rollers crush every mob they sweep over; the freed giant punches mobs that reach him.
    ///
    ///  Model: Resources/ArmyRun/MobRed (Meshy, static) when present, else a procedural red blob with eyes.
    /// </summary>
    public partial class ArmyRunLevel
    {
        private const int SWARM_CAP = 9000;
        private const float MOB_HEIGHT = 0.62f;           // ~half a soldier: a carpet of small mobs, as in the reference
        private const float MOB_HALF = 0.27f;             // hit half-width
        private const float MOB_WALK = 0.6f;              // whole swarm shuffles toward you (m/s)
        private const float MOB_WAKE_Z = 30f;
        private const float MOB_CHARGE_Z = 5.5f;
        private const float MOB_CHARGE_SPEED = 3.4f;
        private const float SWARM_CRAWL = 0.7f;           // road speed (m/s) with the swarm in your face
        private const float SWARM_SLOW_FROM = 15f;        // front distance where the road starts slowing (just past bullet reach)
        private const float SWARM_STOP_AT = 4f;
        private const int BIG_MOB_HP = 6, BIG_MOB_KILLS = 3;

        private const int NB = 72;                         // 1 m buckets from dz = -4 … +68 (bullet lookup)
        private const float B0 = -4f;

        private struct Mob
        {
            public float x, z, y;          // track-local x/z (y only while dying)
            public float hp;
            public byte state;             // 0 waiting · 1 walking · 2 charging · 3 dying · 4 gone
            public bool big;
            public float phase, t, vx, vy, vz, yaw;
        }

        private readonly Mob[] mobs = new Mob[SWARM_CAP];
        private int mobCount;
        private readonly int[] mobBucket = new int[SWARM_CAP];
        private readonly int[] bucketStart = new int[NB + 1];
        private readonly int[] bucketFill = new int[NB];
        private readonly int[] bucketItems = new int[SWARM_CAP];

        private float swarmFront = float.PositiveInfinity;   // nearest live mob ahead (m)
        // auto-targeting: the nearest live mobs in reach (world x, world z), nearest first
        private const int AIM_K = 12;
        private readonly Vector2[] aimTargets = new Vector2[AIM_K];
        private int aimCount;
        private float swarmSpeedFactor = 1f;
        private bool swarmAimValid;
        private float swarmAimMin, swarmAimMax;
        private int swarmPendingKills, swarmKilledTotal, swarmCoinBank;
        private float swarmSfxTimer, swarmPuffTimer, swarmCoinTimer;
        private Vector3 swarmHitPos;

        // rendering
        private static Mesh s_mobMesh;
        private static Material[] s_mobMats;
        private static Matrix4x4 s_mobFix = Matrix4x4.identity;
        private readonly Matrix4x4[] mobBatch = new Matrix4x4[1023];

        // ═════════════════════════════════════ Building ══

        /// <summary>
        /// Fills z0…z1 (metres run) × xMin…xMax with mobs at 'density' per m². bigChance of them are big
        /// (1.6× size, 6 HP, kill 3 on contact).
        /// </summary>
        private void AddSwarm(float z0, float z1, float xMin, float xMax, float density, float bigChance = 0f)
        {
            float area = (z1 - z0) * (xMax - xMin);
            int n = Mathf.RoundToInt(area * density);
            float cell = Mathf.Sqrt(1f / Mathf.Max(0.05f, density));
            int cols = Mathf.Max(1, Mathf.RoundToInt((xMax - xMin) / cell));
            float cw = (xMax - xMin) / cols;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)cols));
            float rh = (z1 - z0) / rows;
            for (int i = 0; i < n && mobCount < SWARM_CAP; i++)
            {
                int col = i % cols, row = i / cols;
                float x = xMin + (col + 0.5f + Random.Range(-0.3f, 0.3f)) * cw;
                float z = z0 + (row + 0.5f + Random.Range(-0.35f, 0.35f)) * rh;
                bool big = Random.value < bigChance;
                mobs[mobCount++] = new Mob
                {
                    x = Mathf.Clamp(x, xMin, xMax), z = z, hp = big ? BIG_MOB_HP : 1f, big = big,
                    phase = Random.value * 10f, yaw = Random.Range(-25f, 25f)
                };
            }
        }

        /// <summary>Swarm whose density ramps from d0 to d1 (6 m slices): thin front, thick core.</summary>
        private void AddSwarmRamp(float z0, float z1, float xMin, float xMax, float d0, float d1, float bigChance = 0f)
        {
            const float SLICE = 6f;
            for (float z = z0; z < z1 - 0.01f; z += SLICE)
            {
                float ze = Mathf.Min(z1, z + SLICE);
                float k = ((z + ze) * 0.5f - z0) / Mathf.Max(0.01f, z1 - z0);
                AddSwarm(z, ze, xMin, xMax, Mathf.Lerp(d0, d1, k), bigChance);
            }
        }

        private void ResetSwarm()
        {
            mobCount = 0;
            swarmFront = float.PositiveInfinity;
            swarmSpeedFactor = 1f;
            swarmAimValid = false;
            swarmPendingKills = swarmKilledTotal = swarmCoinBank = 0;
        }

        // ═════════════════════════════════════ Per frame ══

        /// <summary>Moves / charges / contacts every mob, rebuilds the bullet lookup and the road speed.</summary>
        private void UpdateSwarm(float dt)
        {
            if (mobCount == 0) { swarmSpeedFactor = 1f; return; }
            Vector3 lp = LeaderPos;
            float trZ = trackRoot.position.z, trX = trackRoot.position.x;
            float R = CrowdRadius;
            float front = float.PositiveInfinity;
            float aMin = float.PositiveInfinity, aMax = float.NegativeInfinity;
            bool allyOn = ally != null && ally.tr != null && ally.deadT < 0f;
            Vector3 ap = allyOn ? ally.tr.position : Vector3.zero;
            int allyPunches = 0;

            for (int b = 0; b < NB; b++) bucketFill[b] = 0;

            for (int i = 0; i < mobCount; i++)
            {
                ref Mob m = ref mobs[i];
                mobBucket[i] = -1;
                if (m.state >= 3)
                {
                    if (m.state == 3)
                    {
                        m.t += dt;
                        m.vy -= 22f * dt;
                        m.x += m.vx * dt; m.y += m.vy * dt; m.z += m.vz * dt;
                        if (m.t > 0.5f || m.y < -1f) m.state = 4;
                    }
                    continue;
                }

                float wz = m.z + trZ, wx = m.x + trX;
                float dz = wz - lp.z;
                if (dz > MOB_WAKE_Z + 40f) continue;            // far away: nothing to do yet

                if (m.state == 0 && dz < MOB_WAKE_Z) m.state = 1;
                float dx = lp.x - wx;
                if (m.state == 1)
                {
                    m.z -= MOB_WALK * dt;
                    if (dz < MOB_CHARGE_Z + R && Mathf.Abs(dx) < R + 3.2f) m.state = 2;
                }
                else if (m.state == 2)
                {
                    float sp = MOB_CHARGE_SPEED * (m.big ? 0.75f : 1f) * dt;
                    m.z -= sp;
                    m.x += Mathf.Clamp(dx, -sp * 0.9f, sp * 0.9f);
                }
                wz = m.z + trZ; wx = m.x + trX; dz = wz - lp.z; dx = lp.x - wx;

                // giant ally punches whatever reaches him
                if (allyOn && Mathf.Abs(wx - ap.x) < ALLY_REACH && wz - ap.z < ALLY_REACH && wz - ap.z > -1.2f)
                {
                    KillMob(i, new Vector3(0f, 7f, 5f), true);
                    allyPunches++;
                    continue;
                }

                // contact with the army: 1-for-1
                if (dz < R * 0.75f && dz > -R && Mathf.Abs(dx) < R + 0.3f)
                {
                    m.state = 4;
                    swarmPendingKills += m.big ? BIG_MOB_KILLS : 1;
                    swarmHitPos = new Vector3(wx, 0f, wz);
                    if (swarmPuffTimer <= 0f) { swarmPuffTimer = 0.06f; fx?.EnemyDeath(swarmHitPos + Vector3.up * 0.4f); }
                    Clash(swarmHitPos);
                    continue;
                }
                if (dz < -5f) { m.state = 4; continue; }   // slipped past (rare)

                if (dz > -0.5f && dz < front) front = dz;
                if (dz > 0f && dz < 25f) { if (wx < aMin) aMin = wx; if (wx > aMax) aMax = wx; }

                int bk = Mathf.FloorToInt(dz - B0);
                if (bk >= 0 && bk < NB) { mobBucket[i] = bk; bucketFill[bk]++; }
            }

            // counting sort → bucketItems
            bucketStart[0] = 0;
            for (int b = 0; b < NB; b++) { bucketStart[b + 1] = bucketStart[b] + bucketFill[b]; bucketFill[b] = 0; }
            for (int i = 0; i < mobCount; i++)
            {
                int bk = mobBucket[i];
                if (bk < 0) continue;
                bucketItems[bucketStart[bk] + bucketFill[bk]++] = i;
            }

            // nearest mobs first: walk the buckets from the army outward (they are already sorted by distance)
            aimCount = 0;
            int bStart = Mathf.Max(0, Mathf.FloorToInt(-B0));
            int bEnd = Mathf.Min(NB - 1, Mathf.FloorToInt(BULLET_RANGE - B0));
            for (int bk = bStart; bk <= bEnd && aimCount < AIM_K; bk++)
                for (int k = bucketStart[bk], e = bucketStart[bk + 1]; k < e && aimCount < AIM_K; k++)
                {
                    ref Mob am = ref mobs[bucketItems[k]];
                    aimTargets[aimCount++] = new Vector2(am.x + trX, am.z + trZ);
                }

            swarmFront = front;
            swarmAimValid = aMax >= aMin;
            if (swarmAimValid) { swarmAimMin = aMin; swarmAimMax = aMax; }

            float targetFactor = 1f;
            if (front < SWARM_SLOW_FROM)
                targetFactor = Mathf.Lerp(front <= SWARM_STOP_AT ? 0f : SWARM_CRAWL / RUN_SPEED, 1f,
                                          Mathf.InverseLerp(SWARM_STOP_AT, SWARM_SLOW_FROM, front));
            swarmSpeedFactor = Mathf.MoveTowards(swarmSpeedFactor, targetFactor, dt * 2.5f);

            // apply contact losses once per frame
            if (swarmPendingKills > 0)
            {
                int k = swarmPendingKills;
                swarmPendingKills = 0;
                Snd?.PlayZombieAttack(swarmHitPos);
                ChangeSoldiers(soldierCount - k, swarmHitPos);
            }

            if (allyPunches > 0 && ally != null)
            {
                ally.hp -= allyPunches * 0.25f;
                PunchAnim();
            }

            swarmSfxTimer -= dt; swarmPuffTimer -= dt; swarmCoinTimer -= dt;
            if (swarmCoinTimer <= 0f && swarmCoinBank >= 5)
            {
                swarmCoinTimer = 0.45f;
                int c = swarmCoinBank / 5;
                swarmCoinBank -= c * 5;
                AddCoins(c, new Vector3(lp.x, 1f, lp.z + Mathf.Min(front, 12f)));
            }
        }

        /// <summary>
        /// A bullet travelling from z0 to z1 (world) at x. It kills mobs in order while it still has damage.
        /// Returns true when the bullet is used up.
        /// </summary>
        private bool SwarmBulletHits(ref float damage, float x, float z0, float z1, float y)
        {
            if (mobCount == 0) return false;
            float trZ = trackRoot.position.z, trX = trackRoot.position.x;
            float lz = LeaderPos.z;
            int b0 = Mathf.Clamp(Mathf.FloorToInt(z0 - 0.4f - lz - B0), 0, NB - 1);
            int b1 = Mathf.Clamp(Mathf.FloorToInt(z1 - lz - B0), 0, NB - 1);
            if (z1 - lz - B0 < 0f || z0 - lz - B0 >= NB) return false;
            float zCur = z0 - 0.4f;
            for (int guard = 0; guard < 40; guard++)
            {
                int best = -1;
                float bestZ = float.PositiveInfinity;
                for (int bk = b0; bk <= b1; bk++)
                {
                    for (int k = bucketStart[bk], e = bucketStart[bk + 1]; k < e; k++)
                    {
                        int i = bucketItems[k];
                        ref Mob m = ref mobs[i];
                        if (m.state >= 3) continue;
                        float wz = m.z + trZ;
                        if (wz < zCur || wz > z1 || wz >= bestZ) continue;
                        float half = m.big ? MOB_HALF * 1.6f : MOB_HALF;
                        if (Mathf.Abs(m.x + trX - x) > half) continue;
                        best = i; bestZ = wz;
                    }
                    if (best >= 0 && bk < b1 && bestZ < bk + 1 + lz + B0) break;   // nearest found in this bucket
                }
                if (best < 0) return false;
                ref Mob hit = ref mobs[best];
                float take = Mathf.Min(hit.hp, damage);
                hit.hp -= take;
                damage -= take;
                if (hit.hp <= 0.01f)
                {
                    KillMob(best, new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(4f, 6.5f), Random.Range(3f, 6f)), false);
                    if (swarmSfxTimer <= 0f) { swarmSfxTimer = 0.09f; Snd?.PlayBulletHit(new Vector3(x, 0.5f, bestZ)); }
                }
                else fx?.BulletHit(new Vector3(x, y, bestZ - 0.3f), new Color(1f, 0.45f, 0.3f));
                if (damage < 0.35f) return true;
                zCur = bestZ + 0.01f;
            }
            return damage < 0.35f;
        }

        /// <summary>Mob dies: flies off spinning and shrinking, red puff, coins banked.</summary>
        private void KillMob(int i, Vector3 vel, bool punched)
        {
            ref Mob m = ref mobs[i];
            if (m.state >= 3) return;
            m.state = 3; m.t = 0f; m.y = 0f;
            m.vx = vel.x; m.vy = vel.y; m.vz = vel.z;
            swarmKilledTotal++;
            swarmCoinBank += m.big ? 3 : 1;
            Vector3 wp = new Vector3(m.x + trackRoot.position.x, 0.4f, m.z + trackRoot.position.z);
            if (swarmPuffTimer <= 0f || m.big)
            {
                swarmPuffTimer = 0.035f;
                if (punched) fx?.BulletHit(wp + Vector3.up * 0.4f, new Color(0.6f, 0.85f, 1f));
                fx?.EnemyDeath(wp);
            }
        }

        /// <summary>Spiked roller: crushes every mob it sweeps over.</summary>
        private void SwarmCrush(float xMin, float xMax, float zWorld, float depth)
        {
            float trZ = trackRoot.position.z, trX = trackRoot.position.x;
            for (int i = 0; i < mobCount; i++)
            {
                ref Mob m = ref mobs[i];
                if (m.state >= 3) continue;
                float wz = m.z + trZ;
                if (Mathf.Abs(wz - zWorld) > depth) continue;
                float wx = m.x + trX;
                if (wx < xMin || wx > xMax) continue;
                KillMob(i, new Vector3(Random.Range(-4f, 4f), Random.Range(6f, 9f), Random.Range(-2f, 2f)), false);
            }
        }

        // ═════════════════════════════════════ Drawing (GPU instancing) ══

        private void DrawSwarm()
        {
            if (mobCount == 0 || trackRoot == null) return;
            EnsureMobMesh();
            if (s_mobMesh == null || s_mobMats == null) return;

            Vector3 lp = LeaderPos;
            float trZ = trackRoot.position.z, trX = trackRoot.position.x;
            float time = Time.time;
            int n = 0;
            for (int i = 0; i < mobCount; i++)
            {
                ref Mob m = ref mobs[i];
                if (m.state == 4) continue;
                float wz = m.z + trZ;
                float dz = wz - lp.z;
                if (dz < -6f || dz > 45f) continue;           // the camera sees ~35 m ahead, then mist
                float wx = m.x + trX;
                float s = m.big ? 1.6f : 1f;
                Quaternion rot;
                Vector3 scl;
                float y = 0f;
                if (m.state == 3)
                {
                    y = m.y;
                    rot = Quaternion.Euler(m.t * -700f, 180f + m.yaw + m.t * 300f, m.t * 200f);
                    float k = 1f - Mathf.Clamp01((m.t - 0.2f) / 0.3f);
                    scl = Vector3.one * (s * Mathf.Max(0.05f, k));
                }
                else
                {
                    float speed = m.state == 2 ? 16f : (m.state == 1 ? 11f : 4f);
                    float hop = Mathf.Abs(Mathf.Sin(time * speed + m.phase));
                    y = m.state == 0 ? 0f : hop * 0.09f * s;
                    float yaw = m.state == 2 ? Mathf.Atan2(lp.x - wx, lp.z - wz) * Mathf.Rad2Deg : 180f + m.yaw * 0.4f + Mathf.Sin(time * 2f + m.phase) * 8f;
                    rot = Quaternion.Euler(0f, yaw, Mathf.Sin(time * speed * 0.5f + m.phase) * (m.state == 0 ? 2f : 6f));
                    float sq = (m.state == 0 ? Mathf.Sin(time * 3f + m.phase) * 0.03f : (hop - 0.5f) * 0.12f);
                    scl = new Vector3(s * (1f - sq * 0.5f), s * (1f + sq), s * (1f - sq * 0.5f));
                }
                mobBatch[n++] = Matrix4x4.TRS(new Vector3(wx, y, wz), rot, scl) * s_mobFix;
                if (n == mobBatch.Length) { FlushMobBatch(n); n = 0; }
            }
            if (n > 0) FlushMobBatch(n);
        }

        private void FlushMobBatch(int count)
        {
            int subs = Mathf.Min(s_mobMesh.subMeshCount, s_mobMats.Length);
            for (int sm = 0; sm < subs; sm++)
                Graphics.DrawMeshInstanced(s_mobMesh, sm, s_mobMats[sm], mobBatch, count, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
        }

        /// <summary>Meshy "MobRed" if generated, otherwise a procedural red blob (body, feet, arms, eyes, brows).</summary>
        private static void EnsureMobMesh()
        {
            if (s_mobMesh != null && s_mobMats != null) return;
            if (TryLoadGeneratedMob()) return;

            var temp = new GameObject("MobTemp").transform;
            Material red = InstancedMat(new Color(0.95f, 0.13f, 0.12f), 0.55f);
            Material white = InstancedMat(Color.white, 0.4f);
            Material black = InstancedMat(new Color(0.06f, 0.05f, 0.07f), 0.2f);
            var groups = new List<List<CombineInstance>> { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };
            Mesh sphere = PrimitiveMesh(PrimitiveType.Sphere), cube = PrimitiveMesh(PrimitiveType.Cube);
            void Add(int g, Mesh mesh, Vector3 p, Vector3 sc, Vector3 euler)
                => groups[g].Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(p, Quaternion.Euler(euler), sc) });

            Add(0, sphere, new Vector3(0f, 0.36f, 0f), new Vector3(0.62f, 0.6f, 0.56f), Vector3.zero);       // body
            Add(0, sphere, new Vector3(0f, 0.64f, -0.02f), new Vector3(0.16f, 0.2f, 0.16f), Vector3.zero);    // little tuft
            Add(0, sphere, new Vector3(-0.15f, 0.07f, 0.02f), new Vector3(0.2f, 0.15f, 0.26f), Vector3.zero);  // feet
            Add(0, sphere, new Vector3(0.15f, 0.07f, 0.02f), new Vector3(0.2f, 0.15f, 0.26f), Vector3.zero);
            Add(0, sphere, new Vector3(-0.33f, 0.33f, 0.04f), new Vector3(0.16f, 0.22f, 0.16f), new Vector3(0f, 0f, 25f));  // arms
            Add(0, sphere, new Vector3(0.33f, 0.33f, 0.04f), new Vector3(0.16f, 0.22f, 0.16f), new Vector3(0f, 0f, -25f));
            Add(1, sphere, new Vector3(-0.11f, 0.44f, 0.24f), new Vector3(0.17f, 0.2f, 0.1f), Vector3.zero);  // eyes
            Add(1, sphere, new Vector3(0.11f, 0.44f, 0.24f), new Vector3(0.17f, 0.2f, 0.1f), Vector3.zero);
            Add(2, sphere, new Vector3(-0.1f, 0.43f, 0.285f), new Vector3(0.08f, 0.1f, 0.04f), Vector3.zero); // pupils
            Add(2, sphere, new Vector3(0.1f, 0.43f, 0.285f), new Vector3(0.08f, 0.1f, 0.04f), Vector3.zero);
            Add(2, cube, new Vector3(-0.12f, 0.56f, 0.25f), new Vector3(0.16f, 0.035f, 0.04f), new Vector3(0f, 0f, -22f)); // angry brows
            Add(2, cube, new Vector3(0.12f, 0.56f, 0.25f), new Vector3(0.16f, 0.035f, 0.04f), new Vector3(0f, 0f, 22f));
            Object.Destroy(temp.gameObject);

            var parts = new CombineInstance[3];
            for (int g = 0; g < 3; g++)
            {
                var pm = new Mesh();
                pm.CombineMeshes(groups[g].ToArray(), true, true);
                parts[g] = new CombineInstance { mesh = pm, transform = Matrix4x4.identity };
            }
            var mesh = new Mesh { name = "MobBlob" };
            mesh.CombineMeshes(parts, false, false);       // 3 sub-meshes: red / white / black
            mesh.RecalculateBounds();
            s_mobMesh = mesh;
            s_mobMats = new[] { red, white, black };
            s_mobFix = Matrix4x4.Scale(Vector3.one * (MOB_HEIGHT / 0.74f));
        }

        private static bool TryLoadGeneratedMob()
        {
            var prefab = Resources.Load<GameObject>(ArmyRunModels.RES_FOLDER + "MobRed");
            if (prefab == null) return false;
            var mf = prefab.GetComponentInChildren<MeshFilter>(true);
            var mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
            if (mf == null || mf.sharedMesh == null || mr == null) return false;

            Matrix4x4 local = prefab.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Bounds mb = mf.sharedMesh.bounds;
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                Vector3 p = local.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, p); max = Vector3.Max(max, p);
            }
            float h = max.y - min.y;
            if (h < 1e-4f) return false;
            float s = MOB_HEIGHT / h;
            Vector3 centre = (min + max) * 0.5f;
            s_mobFix = Matrix4x4.TRS(new Vector3(-centre.x * s, -min.y * s, -centre.z * s), Quaternion.Euler(0f, ArmyRunModels.GENERATED_MODEL_YAW, 0f), Vector3.one * s) * local;

            Texture2D baseTex = Resources.Load<Texture2D>(ArmyRunModels.RES_FOLDER + "MobRed_BaseColor");
            var src = mr.sharedMaterials;
            s_mobMats = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var m = InstancedMat(Color.white, 0.5f);
                Texture tex = src[i] != null && src[i].mainTexture != null ? src[i].mainTexture : baseTex;
                if (tex != null) m.mainTexture = tex; else m.color = new Color(0.95f, 0.13f, 0.12f);
                s_mobMats[i] = m;
            }
            s_mobMesh = mf.sharedMesh;
            return true;
        }

        private static Material InstancedMat(Color c, float gloss)
        {
            Shader sh = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
            var m = new Material(sh) { color = c, name = "Mob_" + ColorUtility.ToHtmlStringRGB(c), enableInstancing = true };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
            return m;
        }

        private static readonly Dictionary<PrimitiveType, Mesh> s_primMeshes = new Dictionary<PrimitiveType, Mesh>();
        private static Mesh PrimitiveMesh(PrimitiveType t)
        {
            if (s_primMeshes.TryGetValue(t, out var m) && m != null) return m;
            var go = GameObject.CreatePrimitive(t);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            s_primMeshes[t] = m;
            return m;
        }
    }
}
