#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using MobControl.Crowd;
using MobControl.UI;

namespace MobControl.EditorTools
{
    public static class ZombiePrefabBuilder
    {
        [MenuItem("Zombie Squad/Build Chibi Cartoon Zombie Prefabs (Home Page Match)")]
        public static void BuildAllZombiePrefabs()
        {
            AssetDatabase.Refresh();

            EnsureTexturesAndMaterials();

            BuildChibiZombiePrefab(ZombieType.SmallRunner);
            BuildChibiZombiePrefab(ZombieType.ModerateGreen);
            BuildChibiZombiePrefab(ZombieType.BossBrute);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[Zombie Squad] 3 Chibi Cartoon Zombie Prefabs (Small Runner, Moderate Green, Boss Brute) built successfully to match Home Page Art 1:1!</color>");
        }

        public enum ZombieType
        {
            SmallRunner,
            ModerateGreen,
            BossBrute
        }

        public static void EnsureTexturesAndMaterials()
        {
            string matFolder = "Assets/Models/Generated/Zombies/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Models/Generated"))
                    AssetDatabase.CreateFolder("Assets/Models", "Generated");
                if (!AssetDatabase.IsValidFolder("Assets/Models/Generated/Zombies"))
                    AssetDatabase.CreateFolder("Assets/Models/Generated", "Zombies");
                AssetDatabase.CreateFolder("Assets/Models/Generated/Zombies", "Materials");
            }

            // Create/Ensure Materials
            CreateOrUpdateMaterial(matFolder + "/M_ChibiSkin_Green.mat", new Color(0.48f, 0.80f, 0.14f), "Assets/Models/Generated/Zombies/Textures/Tex_Skin_Green.png", 0f, 0.25f);
            CreateOrUpdateMaterial(matFolder + "/M_ChibiSkin_Red.mat", new Color(0.85f, 0.16f, 0.14f), "Assets/Models/Generated/Zombies/Textures/Tex_Skin_Red.png", 0f, 0.25f);
            CreateOrUpdateMaterial(matFolder + "/M_ChibiSkin_Blue.mat", new Color(0.02f, 0.72f, 0.98f), "Assets/Models/Generated/Zombies/Textures/Tex_Skin_Blue.png", 0f, 0.25f);
            CreateOrUpdateMaterial(matFolder + "/M_ChibiSkin_Boss.mat", new Color(0.32f, 0.58f, 0.13f), "Assets/Models/Generated/Zombies/Textures/Tex_Skin_Boss.png", 0.05f, 0.20f);

            CreateOrUpdateMaterial(matFolder + "/M_ChibiShirt_Brown.mat", new Color(0.57f, 0.30f, 0.18f), "Assets/Models/Generated/Zombies/Textures/Tex_Shirt_Brown.png", 0f, 0.15f);
            CreateOrUpdateMaterial(matFolder + "/M_ChibiPants_Denim.mat", new Color(0.18f, 0.44f, 0.74f), "Assets/Models/Generated/Zombies/Textures/Tex_Pants_Denim.png", 0f, 0.15f);

            // Glowing Eye Material
            CreateOrUpdateEyeMaterial(matFolder + "/M_ChibiEye_Glow.mat", "Assets/Models/Generated/Zombies/Textures/Tex_ChibiEye_Glow.png", new Color(1.3f, 1.15f, 0.25f));
            CreateOrUpdateEyeMaterial(matFolder + "/M_ChibiEye_BossGlow.mat", "Assets/Models/Generated/Zombies/Textures/Tex_ChibiEye_Glow.png", new Color(1.5f, 0.65f, 0.10f));

            CreateOrUpdateMaterial(matFolder + "/M_ChibiMouth.mat", new Color(0.12f, 0.03f, 0.03f), null, 0f, 0.1f);
            CreateOrUpdateMaterial(matFolder + "/M_ChibiTeeth.mat", Color.white, null, 0f, 0.6f);
            CreateOrUpdateMaterial(matFolder + "/M_ChibiBossMetal.mat", new Color(0.35f, 0.38f, 0.42f), null, 0.85f, 0.65f);
        }

        private static Material CreateOrUpdateMaterial(string path, Color color, string texPath, float metallic, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            if (!string.IsNullOrEmpty(texPath))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex != null) mat.mainTexture = tex;
            }
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrUpdateEyeMaterial(string path, string texPath, Color emissionColor)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = new Color(1.0f, 0.90f, 0.15f, 1f); // Vibrant bright golden yellow like reference
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
            mat.SetFloat("_Glossiness", 0.90f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildChibiZombiePrefab(ZombieType type)
        {
            string prefabName = "";
            string poolTag = "";
            float scale = 1.0f;
            float moveSpeed = 3.2f;
            int hp = 6;
            int dmg = 1;
            float animSpeed = 5.2f;
            float legSwing = 36f;

            string skinMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiSkin_Green.mat";
            string eyeMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiEye_Glow.mat";
            string shirtMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiShirt_Brown.mat";
            string pantsMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiPants_Denim.mat";
            string mouthMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiMouth.mat";
            string teethMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiTeeth.mat";
            string metalMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiBossMetal.mat";

            switch (type)
            {
                case ZombieType.SmallRunner:
                    prefabName = "Zombie_Small";
                    poolTag = "Zombie_Small";
                    scale = 0.75f; // Fast red agile runner
                    moveSpeed = 4.8f; // High speed: rushes ahead!
                    hp = 8; // Exactly 8 bullets to kill as specified
                    dmg = 1;
                    animSpeed = 8.5f; // Fast running gait
                    legSwing = 48f;
                    skinMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiSkin_Red.mat";
                    break;

                case ZombieType.ModerateGreen:
                    prefabName = "Zombie_Moderate";
                    poolTag = "Zombie_Moderate";
                    scale = 0.88f; // Classic green zombie
                    moveSpeed = 3.2f; // Normal walking speed
                    hp = 5; // Exactly 5 bullets to kill as specified
                    dmg = 1;
                    animSpeed = 5.2f; // Classic cartoon zombie waddle
                    legSwing = 36f;
                    skinMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiSkin_Green.mat";
                    break;

                case ZombieType.BossBrute:
                    prefabName = "Zombie_Boss";
                    poolTag = "Zombie_Boss";
                    scale = 2.00f; // Giant hulking boss brute (Image 1 match)
                    moveSpeed = 1.4f; // Ground-shaking heavy stomp
                    hp = 60; // Exactly 60 bullets to kill as specified
                    dmg = 3;
                    animSpeed = 3.2f;
                    legSwing = 26f;
                    skinMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiSkin_Boss.mat";
                    eyeMatPath = "Assets/Models/Generated/Zombies/Materials/M_ChibiEye_BossGlow.mat";
                    break;
            }

            Material skinMat = AssetDatabase.LoadAssetAtPath<Material>(skinMatPath);
            Material eyeMat = AssetDatabase.LoadAssetAtPath<Material>(eyeMatPath);
            Material shirtMat = AssetDatabase.LoadAssetAtPath<Material>(shirtMatPath);
            Material pantsMat = AssetDatabase.LoadAssetAtPath<Material>(pantsMatPath);
            Material mouthMat = AssetDatabase.LoadAssetAtPath<Material>(mouthMatPath);
            Material teethMat = AssetDatabase.LoadAssetAtPath<Material>(teethMatPath);
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>(metalMatPath);

            // Root GameObject
            GameObject root = new GameObject(prefabName);
            root.tag = "Untagged";

            // Rigidbody & Collider
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            BoxCollider col = root.AddComponent<BoxCollider>();
            float totalH = 1.45f * scale;
            float totalW = 0.75f * scale;
            col.center = new Vector3(0f, totalH * 0.5f, 0f);
            col.size = new Vector3(totalW, totalH, totalW);

            // MobUnit Component
            MobUnit unit = root.AddComponent<MobUnit>();
            unit.Team = MobTeam.Enemy;
            unit.PoolTag = poolTag;
            unit.SetStats(moveSpeed, hp, dmg);

            // Visual Model Root
            GameObject visual = new GameObject("Visual_ChibiZombie");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * scale;

            // Body Root (sways & bounces)
            GameObject bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(visual.transform, false);
            bodyRoot.transform.localPosition = new Vector3(0f, 0.48f, 0f);

            // 1. Torso (Brown Shirt)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torso.name = "Torso_Shirt";
            torso.transform.SetParent(bodyRoot.transform, false);
            torso.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            torso.transform.localScale = new Vector3(0.46f, 0.22f, 0.38f);
            Object.DestroyImmediate(torso.GetComponent<Collider>());
            if (shirtMat != null) torso.GetComponent<Renderer>().sharedMaterial = shirtMat;

            // 2. Belly (Green skin peeking out under torn shirt)
            GameObject belly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            belly.name = "Belly_Skin";
            belly.transform.SetParent(bodyRoot.transform, false);
            belly.transform.localPosition = new Vector3(0f, 0.03f, 0.08f);
            belly.transform.localScale = new Vector3(0.42f, 0.16f, 0.32f);
            Object.DestroyImmediate(belly.GetComponent<Collider>());
            if (skinMat != null) belly.GetComponent<Renderer>().sharedMaterial = skinMat;

            // 3. Pelvis (Blue Shorts)
            GameObject pelvis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pelvis.name = "Pelvis_Shorts";
            pelvis.transform.SetParent(bodyRoot.transform, false);
            pelvis.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            pelvis.transform.localScale = new Vector3(0.44f, 0.12f, 0.36f);
            Object.DestroyImmediate(pelvis.GetComponent<Collider>());
            if (pantsMat != null) pelvis.GetComponent<Renderer>().sharedMaterial = pantsMat;

            // 4. Head (Big Chibi Bald Head)
            GameObject headObj = new GameObject("Head_Root");
            headObj.transform.SetParent(bodyRoot.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 0.44f, 0f);

            GameObject headSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headSphere.name = "Head_Skull";
            headSphere.transform.SetParent(headObj.transform, false);
            headSphere.transform.localPosition = Vector3.zero;
            headSphere.transform.localScale = new Vector3(0.78f, 0.74f, 0.72f); // Big round cartoon head
            Object.DestroyImmediate(headSphere.GetComponent<Collider>());
            if (skinMat != null) headSphere.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Ears
            CreatePart(headObj.transform, "Ear_L", PrimitiveType.Sphere, new Vector3(-0.39f, 0.02f, 0f), new Vector3(0.12f, 0.15f, 0.10f), skinMat);
            CreatePart(headObj.transform, "Ear_R", PrimitiveType.Sphere, new Vector3(0.39f, 0.02f, 0f), new Vector3(0.12f, 0.15f, 0.10f), skinMat);

            // Big Glowing Eyes (Iconic Home Page Style)
            CreateEye(headObj.transform, "Eye_L", new Vector3(-0.16f, 0.06f, 0.31f), eyeMat);
            CreateEye(headObj.transform, "Eye_R", new Vector3(0.16f, 0.06f, 0.31f), eyeMat);

            // Nose
            CreatePart(headObj.transform, "Nose", PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0.36f), new Vector3(0.08f, 0.07f, 0.08f), skinMat);

            // Open Cartoon Mouth with Teeth
            GameObject mouth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mouth.name = "Mouth_Cavity";
            mouth.transform.SetParent(headObj.transform, false);
            mouth.transform.localPosition = new Vector3(0f, -0.16f, 0.35f);
            mouth.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mouth.transform.localScale = new Vector3(0.26f, 0.03f, 0.16f);
            Object.DestroyImmediate(mouth.GetComponent<Collider>());
            if (mouthMat != null) mouth.GetComponent<Renderer>().sharedMaterial = mouthMat;

            // Teeth (White cartoon teeth protruding forward)
            CreatePart(headObj.transform, "Tooth_T1", PrimitiveType.Cube, new Vector3(-0.06f, -0.12f, 0.37f), new Vector3(0.05f, 0.05f, 0.04f), teethMat);
            CreatePart(headObj.transform, "Tooth_T2", PrimitiveType.Cube, new Vector3(0.06f, -0.12f, 0.37f), new Vector3(0.05f, 0.05f, 0.04f), teethMat);
            CreatePart(headObj.transform, "Tooth_B1", PrimitiveType.Cube, new Vector3(0f, -0.20f, 0.37f), new Vector3(0.05f, 0.05f, 0.04f), teethMat);

            // Boss Armor (Spikes, Heavy Shoulders, Horns)
            if (type == ZombieType.BossBrute)
            {
                // Horns / Iron Skull Plate
                CreatePart(headObj.transform, "BossHorn_L", PrimitiveType.Cylinder, new Vector3(-0.25f, 0.35f, 0.05f), new Vector3(0.10f, 0.18f, 0.10f), metalMat, Quaternion.Euler(-20f, 0f, -30f));
                CreatePart(headObj.transform, "BossHorn_R", PrimitiveType.Cylinder, new Vector3(0.25f, 0.35f, 0.05f), new Vector3(0.10f, 0.18f, 0.10f), metalMat, Quaternion.Euler(-20f, 0f, 30f));

                // Shoulder Pauldrons
                CreatePart(bodyRoot.transform, "SpikePad_L", PrimitiveType.Sphere, new Vector3(-0.35f, 0.28f, 0f), new Vector3(0.22f, 0.18f, 0.22f), metalMat);
                CreatePart(bodyRoot.transform, "Spike_L", PrimitiveType.Cylinder, new Vector3(-0.38f, 0.38f, 0f), new Vector3(0.06f, 0.14f, 0.06f), metalMat, Quaternion.Euler(0f, 0f, 25f));
                CreatePart(bodyRoot.transform, "SpikePad_R", PrimitiveType.Sphere, new Vector3(0.35f, 0.28f, 0f), new Vector3(0.22f, 0.18f, 0.22f), metalMat);
                CreatePart(bodyRoot.transform, "Spike_R", PrimitiveType.Cylinder, new Vector3(0.38f, 0.38f, 0f), new Vector3(0.06f, 0.14f, 0.06f), metalMat, Quaternion.Euler(0f, 0f, -25f));
            }

            // 5. Arms (Reaching Outward in Classic Cartoon Zombie Pose)
            Transform armL = CreateLimb(bodyRoot.transform, "Arm_L", new Vector3(-0.28f, 0.22f, 0.05f), Quaternion.Euler(-78f, -10f, 0f), shirtMat, skinMat);
            Transform armR = CreateLimb(bodyRoot.transform, "Arm_R", new Vector3(0.28f, 0.22f, 0.05f), Quaternion.Euler(-80f, 10f, 0f), shirtMat, skinMat);

            // 6. Legs & Feet
            Transform legL = CreateLeg(bodyRoot.transform, "Leg_L", new Vector3(-0.14f, -0.14f, 0f), pantsMat, skinMat);
            Transform legR = CreateLeg(bodyRoot.transform, "Leg_R", new Vector3(0.14f, -0.14f, 0f), pantsMat, skinMat);

            // Attach ChibiAnimator
            ChibiZombieAnimator anim = root.AddComponent<ChibiZombieAnimator>();
            anim.bodyRoot = bodyRoot.transform;
            anim.head = headObj.transform;
            anim.leftArm = armL;
            anim.rightArm = armR;
            anim.leftLeg = legL;
            anim.rightLeg = legR;
            anim.animSpeed = animSpeed;
            anim.legSwingAngle = legSwing;
            anim.headBobAmount = 0.038f * scale;

            // Attach 3D Health Bar to all zombies (Image 1 match)
            GameObject barObj = new GameObject("HealthBar3D");
            barObj.transform.SetParent(root.transform, false);
            float barY = (type == ZombieType.BossBrute) ? 3.6f : ((type == ZombieType.SmallRunner) ? 1.45f : 1.65f);
            barObj.transform.localPosition = new Vector3(0f, barY, 0f);
            Simple3DHealthBar bar = barObj.AddComponent<Simple3DHealthBar>();

            // Save Prefab
            SavePrefab(root, "Assets/Prefabs/" + prefabName + ".prefab");
            SavePrefab(root, "Assets/Resources/Prefabs/" + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, Quaternion? rot = null)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = rot.HasValue ? rot.Value : Quaternion.identity;
            obj.transform.localScale = scale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private static void CreateEye(Transform parent, string name, Vector3 localPos, Material eyeMat)
        {
            Material socketMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Generated/Zombies/Materials/M_ChibiMouth.mat");

            // 1. Outer dark eye socket (circular rim)
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = name + "_Socket";
            rim.transform.SetParent(parent, false);
            rim.transform.localPosition = localPos;
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rim.transform.localScale = new Vector3(0.24f, 0.02f, 0.24f);
            Object.DestroyImmediate(rim.GetComponent<Collider>());
            if (socketMat != null) rim.GetComponent<Renderer>().sharedMaterial = socketMat;

            // 2. Glowing Golden Yellow Eyeball (Direct child of head, protruding prominently forward)
            GameObject eyeball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeball.name = name + "_Glow";
            eyeball.transform.SetParent(parent, false);
            eyeball.transform.localPosition = localPos + new Vector3(0f, 0f, 0.04f);
            eyeball.transform.localScale = new Vector3(0.20f, 0.20f, 0.16f);
            Object.DestroyImmediate(eyeball.GetComponent<Collider>());
            if (eyeMat != null) eyeball.GetComponent<Renderer>().sharedMaterial = eyeMat;

            // 3. Cute Cartoon Pupil / Amber Center inside eye
            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = name + "_Pupil";
            pupil.transform.SetParent(parent, false);
            pupil.transform.localPosition = localPos + new Vector3(0f, 0f, 0.10f);
            pupil.transform.localScale = new Vector3(0.08f, 0.08f, 0.06f);
            Object.DestroyImmediate(pupil.GetComponent<Collider>());
            if (socketMat != null) pupil.GetComponent<Renderer>().sharedMaterial = socketMat;
        }

        private static Transform CreateLimb(Transform parent, string name, Vector3 pivotPos, Quaternion rotation, Material sleeveMat, Material skinMat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPos;
            pivot.transform.localRotation = rotation;

            // Short Sleeve
            GameObject sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeve.name = "Sleeve";
            sleeve.transform.SetParent(pivot.transform, false);
            sleeve.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            sleeve.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sleeve.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);
            Object.DestroyImmediate(sleeve.GetComponent<Collider>());
            if (sleeveMat != null) sleeve.GetComponent<Renderer>().sharedMaterial = sleeveMat;

            // Forearm & Hand (reaching forward)
            GameObject forearm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            forearm.name = "Forearm";
            forearm.transform.SetParent(pivot.transform, false);
            forearm.transform.localPosition = new Vector3(0f, 0f, 0.28f);
            forearm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            forearm.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
            Object.DestroyImmediate(forearm.GetComponent<Collider>());
            if (skinMat != null) forearm.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Chubby Hand
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = "Hand";
            hand.transform.SetParent(pivot.transform, false);
            hand.transform.localPosition = new Vector3(0f, 0f, 0.44f);
            hand.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            Object.DestroyImmediate(hand.GetComponent<Collider>());
            if (skinMat != null) hand.GetComponent<Renderer>().sharedMaterial = skinMat;

            return pivot.transform;
        }

        private static Transform CreateLeg(Transform parent, string name, Vector3 pivotPos, Material pantsMat, Material skinMat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPos;

            // Blue Ripped Shorts Thigh
            GameObject thigh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thigh.name = "Thigh_Shorts";
            thigh.transform.SetParent(pivot.transform, false);
            thigh.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            thigh.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            Object.DestroyImmediate(thigh.GetComponent<Collider>());
            if (pantsMat != null) thigh.GetComponent<Renderer>().sharedMaterial = pantsMat;

            // Bare Green Calf
            GameObject calf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            calf.name = "Calf_Skin";
            calf.transform.SetParent(pivot.transform, false);
            calf.transform.localPosition = new Vector3(0f, -0.26f, 0f);
            calf.transform.localScale = new Vector3(0.14f, 0.10f, 0.14f);
            Object.DestroyImmediate(calf.GetComponent<Collider>());
            if (skinMat != null) calf.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Bare Foot
            GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            foot.name = "Foot";
            foot.transform.SetParent(pivot.transform, false);
            foot.transform.localPosition = new Vector3(0f, -0.34f, 0.05f);
            foot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foot.transform.localScale = new Vector3(0.14f, 0.12f, 0.12f);
            Object.DestroyImmediate(foot.GetComponent<Collider>());
            if (skinMat != null) foot.GetComponent<Renderer>().sharedMaterial = skinMat;

            return pivot.transform;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
    }
}
#endif
