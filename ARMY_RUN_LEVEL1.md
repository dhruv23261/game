# Level 1 — Army Run → "Cave Spike Roller" (Mob Control reference)

> **Nov 2026 rework:** Level 1 now follows the Mob Control "Cave Spike Roller" video: one elevated road over a misty
> snowy pine forest, a **+1 strip** down the right edge, and the main lane filled with a **red swarm of thousands of
> mobs** (`ArmyRunLevel.Swarm.cs`, GPU-instanced). See section 0 below; older sections describe systems that still
> exist (gates, containers, gunners) but are no longer placed in Level 1.

## 0. Cave Spike Roller rules (current)

| Rule | Value |
|---|---|
| World | `ArmyRunEnvironment.SnowForest = true`: grey concrete road, dark side walls, bluish **+1 strip lane** (x 2.05…3.6), pier into thick mist, snowy pines below. `false` = old sea bridges. |
| Swarm | Red blob mobs fill x −3.2…1.75. 1 HP each (big mobs: 1.6× size, 6 HP, kill 3). Within 30 m they shuffle at 0.6 m/s, within 5.5 m they **run at the army**. Contact = **1 for 1**. |
| Road speed | Slows as the swarm front gets close (full speed at 20 m → 0.7 m/s → stop at 4.5 m). You push forward exactly as fast as you kill. |
| Bullets | Pierce: a bullet carrying N damage kills N mobs in a row, the rest goes into the boss/prop behind. Streams bend into the swarm lane, so hugging the strip still hits the horde. 90 tracers/s max. |
| +1 strip | A plaque every 1.5 m. The army's edge touching it counts. +1 → +2 → orange +5 → +10. Every 15 = weapon LV +1. |
| HELP! cage | Concrete alcove on the left, 40 HP (first) / 60 HP (before the boss). Freed giant (60 HP) walks ahead and punches every mob that reaches him (−0.25 HP each). |
| Spike rollers | Hold ONE half of the road (left/right, alternating), slide ±0.35 m and spin inside it; red danger zone + "DANGER!" when 24 m away. Crush mobs; catching the army costs up to 25 % of the overlapped share. |
| Bullet reach | 12.5 m ahead of the commander = 2/3 of the screen height. The top third belongs to the horde. Road slows from 15 m. |
| Minus signs | Runs of 3 red signs inside the strip: −3 (st.1), −6 (st.2), −10/−25 (st.3), −15/−50 (st.4). Dodging = skipping a few plus signs. |
| Combo | 20 plus signs in a row without a miss → "PERFECT x20 +5" (bonus = combo/4). A miss or a minus resets it. |
| Ally twice | Cage 1 (66 m): giant for 60 s (countdown on his label, then "ALLY LEFT"). Cage 2 (400 m, 60 HP): champion for the Master Tyrant fight. |
| Camera | Portrait: 16 m high, 7.8 m behind the commander, 48° down, FOV 55, x fixed at 0.35 (15 % follow). Squad at 76 % screen height, road 70 % of the width there, fading into mist 35 m ahead. |
| Road pace | 5 m/s (slower when the swarm is close). |
| Start | The squad glides to x 2.1 (beside the strip) when the run starts, so the first plaques teach the strip. Swarm blocks ramp from a thin front to a thick core (`AddSwarmRamp`). |
| Stale scene copies | `q.unity` contains an old baked Level 1 preview (sea bridge + UI) under `ArmyRunLevel_Director`; `PurgeStaleRoots()` deletes every `ArmyRun_Root` that isn't the live one at level start. Safe to delete it from the scene by hand too. |
| Strip values | stage 1 **+1** · stage 2 **+2** · stage 3 **+3 → orange +10** · stage 4 **+5 → orange +20 → +99** (last 6 m). Signs every 1.15 m, leaning back 50° to face the camera; blue under 10, orange from 10. |
| Brute | Yellow, inside the stage-3 swarm. HP = max(1000, 7 s of your full fire) so it is always a real fight. |
| Editor capture | In Play mode a screenshot + stats line every 3 s → `unity/Captures/level1_live_*.png` / `level1_live.txt` (`ArmyRunLevel.EditorCapture`). |
| Master Tyrant | HP = max(16000, 18 s of your fire) at 420 m; his reinforcements are new swarm blocks (140 / 180 mobs). |

| Stage | z | Content |
|---|---|---|
| 1 · THE HORDE | 0–128 | +1 strip · swarm 2.2→2.6 /m² from 34 m · HELP! cage at 66 m |
| 2 · SPIKE ROLLER | 130–210 | +2 strip · rollers at 142 and 180 m · swarm 3.0 /m² |
| 3 · THE BRUTE | 214–300 | +3 → orange +10 strip · swarm 3.4 /m² · Brute at 262 m |
| 4 · FINAL PUSH | 304–408 | +5 → +20 → +99 strip · swarm 3.8 /m² · 2 rollers |
| 5 · MASTER TYRANT | 420 | final boss |

**Free CC0 models (current look) — Quaternius via Poly Pizza, public domain:**

| Model | Used for | Clips baked |
|---|---|---|
| Zombie (1.7k tris after decimation) | the horde, GPU-instanced, 0.85 m | Walk ×8, Run ×8 |
| Orc (with spiked club) | Brute (green, 2.5×) and Master Tyrant (red tint, 4×) | Idle, Walk, Punch, Weapon (= slam), Death |
| Blue Demon (with club) | giant ally (cage waves HELP!) | Idle, Walk, Punch, Weapon, Death, Wave |
| Snowy Pine | forest tiles (36 per 40 m) | static |

Pipeline: `python3 scripts/bake_polypizza.py [--preview]` downloads the .glb files to `polypizza_downloads/`, skins every
animation frame on the CPU, groups colours, decimates the horde zombie, and writes `Resources/ArmyRun/Baked/*.bytes`
(+ `CREDITS.md`). Unity loads them with `ArmyRunBakedModels` (no glTF package) and draws them with the
`ArmyRun/VertexColorLit` shader (vertex colours, instancing, fog). `ArmyBakedRig` plays clips by swapping meshes.
Priority for big characters: Meshy file → baked CC0 model → hand-built `ArmyRunCharacters`.

**Meshy models (optional, picked up automatically when present in `Assets/Resources/ArmyRun/`):**
`MobRed` (static red blob, instanced), `BruteYellow`, `AllyGiant`, plus the older `EnemySoldier` / `BossTyrant`.
Generate with `python3 scripts/meshy_armyrun_generate.py mob brute giant` (needs Meshy credits).
Without them: hand-built characters from `ArmyRunCharacters.cs` (no Meshy needed): red blob mobs with angry eyes,
the **yellow Brute** (red war paint, mohawk, 2.5× soldier height), the **blue Giant** (white spiky hair, waves HELP! in the cage)
and the **red Master Tyrant** (gold crown, horns, 4×). Gorilla proportions — huge shoulders, tiny head, giant fists — animated by
`ArmyBruteRig`: heavy walk, breathing idle, alternating punches, two-handed ground slam timed to the red circle, fall-over death.

---


Every rule, number and file for Level 1 in one place.

---

## 1. Files

| File | What it does |
|---|---|
| `Scripts/Level/ArmyRunLevel.cs` | **GameManager / LevelManager:** hooks, setup, frame loop, win/lose. Tuning numbers at the top. |
| `Scripts/Level/ArmyRunLevel.Crowd.cs` | **CrowdController + CrowdFormation:** army, sunflower formation, pooled soldiers, Idle/Run/Attack/Die animations, camera follow. |
| `Scripts/Level/ArmyRunLevel.Combat.cs` | **CombatSystem:** army fire, bullet hits, damage, deaths, rewards, explosions, hit flash, container cracking. |
| `Scripts/Level/ArmyRunLevel.Enemies.cs` | **EnemyController:** charging soldiers, squad counters, gunners, parade. |
| `Scripts/Level/ArmyRunLevel.Gates.cs` | **GateController + ObstacleController:** gates, rollers, crashing into props. |
| `Scripts/Level/ArmyRunLevel.Build.cs` | Materials, primitives, labels, pooled floating text. |
| `Scripts/Level/ArmyRunLevel.Ally.cs` | **Giant ally** from the HELP! cage. |
| `Scripts/Level/ArmyRunLevel.Swarm.cs` | **Red swarm:** thousands of instanced mobs, piercing bullets, road slow-down, roller crush. |
| `Scripts/Editor/ArmyRunAnimatorBuilder.cs` | Builds `Resources/ArmyRun/ArmyAnimator.controller` automatically from the player's own clips (Firing Rifle, Fist Fight, Death). |
| `Scripts/Level/ArmyRunLevel.Track.cs` | **The level layout:** 6 stages, plus a builder for every object (gate, barrel, cage, ice, horde, gunner, roller, parade). |
| `Scripts/Level/ArmyRunLevel.Boss.cs` | **The Brute mini-boss** and the **Master Tyrant** final boss. |
| `Scripts/Level/ArmyRunLevel.UI.cs` | Stage banners and the boss HP bar. Hides the zombie-mode HP pill. |
| `Scripts/Level/ArmyRunEnvironment.cs` | Two sea bridges, suspension towers, lamps, buoys, ships, islands, palms, lighthouses, sky, clouds, lighting. |
| `Scripts/Level/ArmyRunFX.cs` | Particle effects: muzzle flash, hit sparks, explosions, smoke, shockwaves, sparkles, debris, blob shadows. |
| `Scripts/Level/ArmyRunModels.cs` | Loads Meshy models when present, plus the low-poly parade soldier. |
| `scripts/meshy_armyrun_generate.py` | Generates the red soldier and the boss with Meshy. |

Hooks in old code: `ZombieRunnerUIManager.StartMission` / `StartGameplayFromWaiting`, `EndlessRoadManager` and `ZombieRoadWaveSpawner` skip level 1, and `PlayerRunnerController` has `ExternalShootingControl`, `SetHorizontalLimit` and `ClearSquadAlliesForArmyMode`.
To bring back the old Level 1: `ArmyRunLevel.Enabled = false;`

---

## 2. World and camera (Mob Control framing)

* Main road **7.2 m wide** (x −3.6 … +3.6) on a sea bridge; the commander steers ±3.0.
* Camera like the Mob Control video: **close and steep**. Height 12.5, 9 m behind, **47°** down, FOV 50. It follows the army sideways (60 %). The army sits in the lower third and the road fills the screen.
* The Dark Legion parade marches on the neighbouring bridge (scenery).

---

## 3. Army

| Rule | Value |
|---|---|
| Start | 1 commander + Soldier Count upgrade |
| Drawn | up to 55 soldiers. The number above the army is the real count. |
| Fire | 1.8 shots/soldier/s × fire bonus. Bullets fly straight ahead, 38 m range, and hit the first thing in their line. |
| Control | swipe left/right only. Aiming = moving. |
| Size changes | only in `ChangeSoldiers()`, which also triggers defeat at 0 |

---

## 4. Everything on the road

| Object | Rule |
|---|---|
| **Container** (blue / red / yellow, number = HP) | A "3" takes exactly 3 hits. Cracks in 4 steps (darker, tilts, chunks fly off) and bursts at 0. Gives coins. Crash into it: lose ceil(HP/2). |
| **Gate** `+5 +10 +20 +40 +60` / `x2 x3 x5` (blue), `-N` (red) | Applies when it reaches the commander, if his x is inside it. Pairs and triples: only one counts. Some gates **slide left and right**. |
| **Wooden barrel** (number = HP) | Shoot it open: **+N soldiers** (green mark) or **FIRE UP ×1.35** (orange mark). Crash into it: lose ceil(HP/4). |
| **Explosive barrel** (red, yellow bands) | Shoot it: explosion of **30 damage within 3.6 m**. Chains into other barrels and wipes enemy squads. Crash into it: lose 3 + 15 % of the army. |
| **Prisoner cage** | Shoot it open: **FREED +N** soldiers. Crash into it: lose ceil(HP/4). |
| **Ice block** | Locks the gate behind it. Break it to unlock the gate. Crash into it: lose ceil(HP/2). |
| **Red soldier** | 2–4 HP, charges and drifts toward you. Flashes white when hit. Contact = **1 for 1**. When close, the front of your army rushes in and punches (Fist Fight clip). |
| **Squad counter** | A red number above every squad counts down as members die. At 0: "CLEARED!" + bonus coins. |
| **Heavy red soldier** (bigger) | 3× HP. Contact kills **2**. |
| **Gunner** (behind sandbags) | Within 34 m, **shoots one of your soldiers every 1.5 s**. Kill gunners first. |
| **Spiked roller** (warning stripes on the road) | Sweeps left and right and can't be shot. When it crosses your line, the share of the army it overlaps loses up to 45 %. Clean dodge = "DODGED!". |
| **Parade** (left bridge) | The huge red army. Scenery only. |
| **+1 strip** (blue +1/+2, orange +5/+10 plaques) | A column of small plaques every 2.4 m along one edge. Each one the commander passes adds its value. **Every 15 plaques = weapon LV +1** (fire rate ×1.12, gold ring + "LV +1"). |
| **Red river** | Enemies scattered along a lane at 0.45–1.0 per metre, walking slowly (0.7×). A constant stream to shoot while you collect plaques. |
| **HELP! cage** (yellow bars, giant inside) | Shoot it to 0 HP → **giant ally** (your soldier at 2.2× size) joins, walks 3 m ahead of the army, **punches every red soldier that reaches him** (−1 HP each, heavy −2) and **punches bosses for 30 damage every 0.25 s**. 40 HP; at 0 he plays the Death clip and sinks. |

---

## 5. The 6 stages (z = metres run). Dense: something to do every second

| Stage | z | Content |
|---|---|---|
| **1 · BOOT CAMP** | 0–98 | **+1 strip** on the right (20 plaques) · **red river** on the left · container 5 · gates `+10`/`x2` · squad of 10 |
| **2 · RESCUE** | 98–186 | **giant ally in a HELP! cage** (30 HP) · red river · +1 strip · gunner · triple `x3`/`+20`/`-10` |
| **3 · SPIKE ROLLER** | 186–288 | roller · **+2 strip switches to the LEFT** while the river is on the right · 2 explosives in the river · fast roller · `x3` locked behind container 60 / `+25` |
| **4 · THE BRUTE** | 288–382 | squad · **BRUTE** · orange **+5 strip** · FIRE UP barrel · cage +15 · `x2`/`+40` |
| **5 · LEGION RIVER** | 382–478 | **full-width red flood** (12 % heavies) · orange **+10 strip** inside the flood · 2 gunners · triple `x2`/`+60`/`-40` |
| **6 · MASTER TYRANT** | 478 | final boss |

---|---|---|
| **1 · BOOT CAMP** | 0–110 | gates `+5`/`x2` · containers **3** and **5** · barrel +8 & cage +6 · container **10** · squad of 8 · triple gate `x3`/`+12`/`-6` · **explosive barrel in front of a squad** |
| **2 · AMBUSH** | 112–230 | gunner + squad · **sliding `x3` gate** · 2 gunners + 16 (3 heavies) · FIRE UP barrel, cage +12, explosive · guarded `+20` vs `-15` |
| **3 · ICE BRIDGE** | 234–350 | spiked roller · ice 60 → `x2`, ice 30 → `+10` · 24 enemies (4 heavies) with 2 explosives · faster roller · containers 20 + 20 · `x3`/`+30` |
| **4 · THE BRUTE** | 352–440 | escort of 10 · **BRUTE mini-boss** (see 6) · treasure: +15, cage +30, FIRE UP · **`x5` locked behind a 150 container** / `+40` |
| **5 · LEGION CHARGE** | 444–545 | 40 enemies (6 heavies) · 3 gunners + 2 explosives · fast roller · `x2`/`+60`/`-40` |
| **6 · MASTER TYRANT** | 548 | final boss (see 6) |

Each stage shows a banner: "STAGE n / 6", the title and a hint.

---

## 6. Bosses

**THE BRUTE** (2× size, 900 HP, red HP bar + number over his head)
* At 18 m the road stops (arena) and the HP bar appears.
* **Ground slam** every 3.2 s: a red circle appears under your army and fills up for 1 s. Everything still inside dies (up to 22 %). **Move out of the circle.**
* Contact stomp: −2 soldiers per second.
* When he dies: big explosion, "GO!", and the road runs again.

**MASTER TYRANT** (4× size, 12000 HP, red HP bar + number over his head)
* **Entrance:** the road stops, he **drops from the sky**, and the landing makes a shockwave and dust and knocks out a few soldiers. Roar, HP bar.
* **Phase 1** (100–66 %): stands 8 m away and slams every 3.4 s (circle 3.3 m, up to 30 %).
* **66 %:** "REINFORCEMENTS!" with 14 soldiers.
* **33 %:** "MASTER ENRAGED!": he glows red, summons 18, walks into your army, and slams every 2 s with a bigger circle (4 m, up to 35 %).
* **Death:** slow motion, a chain of explosions, he falls backwards, then a gold burst, "VICTORY!" and the victory screen.

---

## 7. Win / lose

* **Win:** Master Tyrant dead. Stars: **≥60 soldiers = 3**, **≥20 = 2**, otherwise 1.
  Coins = 80 + soldiers (bonus capped at +300) + **coins collected during the run**. The victory screen shows **"FINAL ARMY: N SOLDIERS"**, with Next Level / Retry.
* **Coins during the run:** red soldier 1 (heavy 3), gunner 4, container 3, barrel / cage 5, squad cleared = half its size, Brute 50, Master 200. Gold coins fly into the coin pill.
* **Pause:** pause button or level pill → Resume / **Restart** / Home (existing pause menu).
* **Lose:** army reaches 0.

---

## 8. Effects and sound

| Event | Effect | Sound |
|---|---|---|
| Shooting | yellow tracers + muzzle flashes | gunfire (5/s) |
| Bullet hits an enemy | flash + sparks | flesh hit |
| Red soldier dies | thrown back spinning, then a red puff | death |
| Your soldier dies | blue puff | hurt |
| Barrel / cage / ice breaks | debris chunks + small blast, reward text | explosion + reward jingle |
| Explosive barrel | fireball, smoke, sparks, shockwave ring, shake | explosion |
| Gate | panel shatters, sparkles, big "x2" text | gate power-up |
| Boss slam | shockwave, dust, blast, shake | explosion |

All effects are 7 world-space particle systems built at runtime (`ArmyRunFX`), so no prefabs are needed.

---

## 9. HUD

* Existing top bar: pause · level · progress bar (boss skull at the end).
* **Avatar + ARMY count** pill (top-left, your soldier portrait), and the **coin** pill (top-right). Both pulse when they change.
* Stage banners in the middle; boss HP bar under the top bar during boss fights.
* **Red danger vignette**: the screen edges pulse red while enemies are on your army, a slam circle is active, or ≤3 soldiers are left.
* Thick glowing **yellow bullet stream** (up to 60 tracers/s, additive glow).
* The zombie-mode "HP" pill is hidden in Level 1 (the army size is your life).

## 10. Animations (player character untouched)

| Clip (already in project) | Used for |
|---|---|
| Player_FiringRifle | Run (and Idle, slowed down) |
| Player_FistFight | Attack: front soldiers punch in close combat |
| Player_Death | Die: fallen soldiers fall over, lie on the road, then puff away |
| Player_Jogging | not used |

The player himself keeps his own original animator. Only the copies in the army use the new one.

**Missing assets (not in the project). Without them, the closest available clip is used:**
1. **Rifle Idle** clip for this rig (e.g. Mixamo "Rifle Aiming Idle"). Today Idle = Firing Rifle slowed down.
2. **Hit Reaction** clip (e.g. Mixamo "Hit Reaction"). Today hits = white flash + punch scale.
3. **Red enemy soldier model** and **boss model**. Today: your soldier recoloured red / scaled 4×. Generate them with the Meshy script below.

## 11. Meshy characters (optional, recommended)

```bash
cd ~/Downloads/gameproject1
python3 scripts/meshy_armyrun_generate.py
```
This creates `Assets/Resources/ArmyRun/EnemySoldier.fbx` and `BossTyrant.fbx`, rigged and animated. Level 1 uses them automatically, scaled to soldier height and 4× for the boss. Without them, the enemies are your soldier recoloured red.

---

## 12. Frame order (`ArmyRunLevel.Update`)

1. Scroll the bridge, scenery and track (stopped during boss arenas)
2. Stage banners
3. Army fires → bullets move → damage → deaths / rewards / explosions
4. Red soldiers charge → gunners fire → enemy shots land
5. Gates → rollers → crashes
6. Brute / Master (arena, walk, stomp, slam, phases)
7. Visuals: parade, formation, flung bodies, floating text, punches, boss death, UI
