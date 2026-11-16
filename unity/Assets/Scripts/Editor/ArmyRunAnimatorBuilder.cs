#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MobControl.EditorTools
{
    /// <summary>
    /// Builds Assets/Resources/ArmyRun/ArmyAnimator.controller from the PLAYER'S OWN animation clips
    /// (Models/Characters/Player_*.fbx). Used only by the army clones and the red fallback enemies —
    /// the player character and its MobAnimator are never touched.
    ///
    ///   States:  Idle   (Firing Rifle clip, slowed to a breathing aim — there is no idle clip in the project)
    ///            Run    (Firing Rifle)          ← Moving = true
    ///            Attack (Fist Fight)            ← trigger "Attack", returns to Run
    ///            Die    (Death, plays once)     ← trigger "Die"
    ///
    /// Runs automatically once when the controller is missing; rebuild any time via
    /// Tools ▸ Army Run ▸ Rebuild Army Animator.
    /// </summary>
    [InitializeOnLoad]
    public static class ArmyRunAnimatorBuilder
    {
        private const string FOLDER = "Assets/Resources/ArmyRun";
        private const string PATH = FOLDER + "/ArmyAnimator.controller";
        private const string CHAR = "Assets/Models/Characters/";

        static ArmyRunAnimatorBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PATH) == null) Build();
            };
        }

        [MenuItem("Tools/Army Run/Rebuild Army Animator")]
        public static void Build()
        {
            AnimationClip run = Clip(CHAR + "Player_FiringRifle.fbx") ?? Clip("Assets/Resources/Characters/Player_FiringRifle.fbx");
            AnimationClip fight = Clip(CHAR + "Player_FistFight.fbx");
            AnimationClip death = Clip(CHAR + "Player_Death.fbx");
            if (run == null)
            {
                Debug.LogWarning("[ArmyRun] Player_FiringRifle clip not found — Army animator not built.");
                return;
            }

            if (!Directory.Exists(FOLDER)) Directory.CreateDirectory(FOLDER);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PATH) != null) AssetDatabase.DeleteAsset(PATH);

            var c = AnimatorController.CreateAnimatorControllerAtPath(PATH);
            c.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            var sm = c.layers[0].stateMachine;

            var idle = sm.AddState("Idle", new Vector3(250, 0, 0));
            idle.motion = run;
            idle.speed = 0.12f;
            var runS = sm.AddState("Run", new Vector3(250, 120, 0));
            runS.motion = run;
            sm.defaultState = idle;

            var toRun = idle.AddTransition(runS);
            toRun.hasExitTime = false; toRun.duration = 0.15f;
            toRun.AddCondition(AnimatorConditionMode.If, 0, "Moving");
            var toIdle = runS.AddTransition(idle);
            toIdle.hasExitTime = false; toIdle.duration = 0.2f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Moving");

            if (fight != null)
            {
                var atk = sm.AddState("Attack", new Vector3(500, 120, 0));
                atk.motion = fight;
                atk.speed = 1.4f;
                var toAtk = sm.AddAnyStateTransition(atk);
                toAtk.hasExitTime = false; toAtk.duration = 0.08f; toAtk.canTransitionToSelf = false;
                toAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                var back = atk.AddTransition(runS);
                back.hasExitTime = true; back.exitTime = 0.6f; back.duration = 0.12f;
            }

            if (death != null)
            {
                var die = sm.AddState("Die", new Vector3(500, 0, 0));
                die.motion = death;
                die.speed = 1.5f;
                var toDie = sm.AddAnyStateTransition(die);
                toDie.hasExitTime = false; toDie.duration = 0.05f; toDie.canTransitionToSelf = false;
                toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ArmyRun] Army animator built: Idle/Run{(fight != null ? "/Attack" : "")}{(death != null ? "/Die" : "")} → {PATH}");
        }

        private static AnimationClip Clip(string fbxPath)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (o is AnimationClip a && !a.name.StartsWith("__preview__")) return a;
            return null;
        }
    }
}
#endif
