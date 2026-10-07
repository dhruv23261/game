# 🎮 Mob Control Game

Mob Control style 3D hypercasual game built in **Unity** with a **Node.js** backend server.

---

## 📁 Project Structure

- **`unity/`**: Unity 3D Project (Assets, C# Scripts, 3D Models, Prefabs, Scenes, Materials).
- **`server/`**: Express / Node.js backend managing progression, levels (15 levels), and rewarded ad settings.
- **`UNITY_SETUP_GUIDE.md`**: Detailed step-by-step scene and gameplay setup guide.

---

## 🚀 How to Run for Collaborators / Friends

### 1. Unity Client
1. Install **Unity Hub** and Unity Editor (Recommended: 2022.3 LTS or compatible version).
2. Click **Add > Add project from disk** in Unity Hub.
3. Select the `unity` directory inside this repository.
4. *Note:* When opening the first time, Unity will automatically regenerate the `Library` cache folder. This takes 2–5 minutes.
5. Open the main scene located in `Assets/Scenes/`.

### 2. Node.js Backend Server
1. Navigate to the server folder:
   ```bash
   cd server
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the server:
   ```bash
   npm run dev
   ```
   The API will run on `http://localhost:4000`.
