# 🎮 Mob Control Style Game - Unity & Backend Setup Guide

## 🏗️ 1. Project Structure Overview

```
gameproject1/
├── server/                    # Node.js Backend Server
│   ├── src/
│   │   ├── data/
│   │   │   ├── levels.json    # 15 Complete Level Configurations & Ad Rules
│   │   │   └── store.js       # Player Data & Progression Store
│   │   ├── controllers/       # Player, Level & Ad Verification Controllers
│   │   ├── routes/            # REST API Endpoints
│   │   └── server.js          # Express Server Entry Point (Port 4000)
│   └── package.json
│
└── unity/
    └── Assets/
        └── Scripts/           # Ready-to-use C# Unity Scripts
            ├── Core/          # GameManager.cs, LevelManager.cs, ObjectPooler.cs
            ├── Cannon/        # CannonController.cs (Drag-to-aim & Fire)
            ├── Crowd/         # MobUnit.cs, EnemySpawner.cs (Clash & Path)
            ├── Gates/         # MultiplierGate.cs (Math Gates: x2, x3, +5, -3)
            ├── Base/          # BaseTarget.cs (Health Bars & Destruction)
            ├── Network/       # ApiClient.cs (REST Backend Sync)
            ├── Ads/           # AdManager.cs (Interstitial & Rewarded 3x)
            └── UI/            # UIManager.cs (HUD, Screens, Claim Buttons)
```

---

## 🚀 2. Running the Node.js Backend Server

1. Open terminal inside the `server/` directory:
   ```bash
   cd /Users/savvy/Downloads/gameproject1/server
   npm run dev
   ```
2. The server runs at `http://localhost:4000`.
3. Test endpoints:
   - **All 15 Levels:** `http://localhost:4000/api/levels`
   - **Level 1 Details:** `http://localhost:4000/api/levels/1`
   - **Ad Settings:** `http://localhost:4000/api/ads/settings`

---

## 🎯 3. Unity Scene Setup (Step-by-Step)

### A. Camera & Lighting
- **Main Camera:** Position `(0, 14, -6)`, Rotation `(55, 0, 0)`.
- **Directional Light:** Rotation `(50, -30, 0)` with soft shadows.

### B. Environment / Arena
- Create a 3D Plane or Quad for the track: Scale `(1, 1, 3)` so it extends from `Z = -2` to `Z = 20`.

### C. Prefabs Setup
1. **PlayerMob Prefab:**
   - Capsule / 3D Character (Blue Material).
   - Add `Rigidbody` (Is Kinematic = `true` or Freeze Rotation).
   - Add `BoxCollider` (Is Trigger = `true`).
   - Attach `MobUnit.cs` (Team = `Player`, Speed = `4`).
2. **EnemyMob Prefab:**
   - Capsule / 3D Character (Red Material).
   - Add `Rigidbody` & `BoxCollider` (Is Trigger = `true`).
   - Attach `MobUnit.cs` (Team = `Enemy`, Speed = `3.5`).
3. **MultiplierGate Prefab:**
   - 3D Cube / Arch frame with TextMeshPro in center.
   - Add `BoxCollider` (Is Trigger = `true`).
   - Attach `MultiplierGate.cs`.

### D. Hierarchy GameObjects
- **`[Managers]` GameObject:**
  - Attach `GameManager`, `LevelManager`, `ObjectPooler`, `ApiClient`, `AdManager`, `UIManager`.
  - In `ObjectPooler`, configure 2 pools:
    - Tag: `PlayerMob`, Prefab: `PlayerMob`, Size: `150`
    - Tag: `EnemyMob`, Prefab: `EnemyMob`, Size: `100`
- **`Cannon` GameObject:**
  - Position `(0, 0.5, 0)`.
  - Attach `CannonController.cs`.
- **`EnemyBase` & `PlayerBase`:**
  - `PlayerBase` at `Z = -2`, `EnemyBase` at `Z = 16`.
  - Attach `BaseTarget.cs` to each.

---

## 🤖 4. Meshy AI 3D Asset Generation Pipeline

Use these prompts in [Meshy.ai](https://www.meshy.ai/) to generate stylized low-poly assets:

1. **Player Blue Mob Runner:**
   > `Low poly stylized friendly blue stickman runner character, clean mobile hypercasual game style, T-pose, vibrant blue color, smooth shading`
2. **Enemy Red Mob Runner:**
   > `Low poly stylized menacing red runner mob character, hypercasual arcade style, T-pose, vibrant crimson red, clean geometry`
3. **Boss Titan Mob (Level 15):**
   > `Giant armored titan golem, stylized low-poly mobile game boss, menacing stance, glowing red accents`
4. **Multiplier Gate Arch:**
   > `Futuristic holographic energy portal gate, glowing blue borders, low poly hypercasual arcade style, clean modular arch`
5. **Cannon Shooter:**
   > `Stylized futuristic double-barrel turret blaster, vibrant blue and silver accents, low poly hypercasual mobile game`

---

## 💰 5. Ads & Monetization Flow

1. **Interstitial Ads:** Automatically triggered upon match completion based on server configuration in `levels.json` (Level 3 onwards).
2. **Rewarded Ads (3x Multiplier):** Players can watch a rewarded ad on the Victory screen to triple their earned coins.
