#if UNITY_EDITOR
using System;
using BenchFlow.XR;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.Hands.OpenXR;

namespace BenchFlow.XR.Editor
{
    /// <summary>Creates the Android OpenXR loader configuration and the reusable runtime rig prefab.</summary>
    [InitializeOnLoad]
    internal static class QuestXRProjectSetup
    {
        const string RigPrefabPath = "Assets/Resources/BenchFlow/QuestXRPlayerRig.prefab";
        const string XrSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

        static QuestXRProjectSetup()
        {
            EditorApplication.delayCall += ConfigureProject;
        }

        static void ConfigureProject()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ConfigureProject;
                return;
            }
            CreateRigPrefab();
            ConfigureAndroidLoader();
            EnableRequiredOpenXRFeatures();
        }

        static void CreateRigPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath) != null) return;
            GameObject rig = new GameObject("Quest XR Player Rig");
            rig.AddComponent<QuestXRPlayerRig>();
            rig.AddComponent<QuestXRHandInteraction>();
            rig.AddComponent<OpenXRHandSubsystemManager>();
            PrefabUtility.SaveAsPrefabAsset(rig, RigPrefabPath);
            UnityEngine.Object.DestroyImmediate(rig);
        }

        static void ConfigureAndroidLoader()
        {
            XRGeneralSettingsPerBuildTarget perTarget = EditorBuildSettings.TryGetConfigObject(
                XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget configured)
                ? configured
                : AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XrSettingsPath);

            if (perTarget == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/XR"))
                    AssetDatabase.CreateFolder("Assets", "XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, XrSettingsPath);
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);

            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            XRGeneralSettings androidSettings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            androidSettings.InitManagerOnStart = true;
            bool assigned = XRPackageMetadataStore.AssignLoader(
                perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android),
                "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
            if (!assigned)
                Debug.LogError("BenchFlow XR: Could not assign the OpenXR loader for Android.");

            EditorUtility.SetDirty(perTarget);
            EditorUtility.SetDirty(androidSettings);
            AssetDatabase.SaveAssets();
        }

        static void EnableRequiredOpenXRFeatures()
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                Debug.LogWarning("BenchFlow XR: OpenXR settings are not available yet. Open Project Settings > XR Plug-in Management > OpenXR once to generate them.");
                return;
            }

            bool metaQuestFeatureFound = false;
            bool handTrackingFeatureFound = false;
            foreach (OpenXRFeature feature in settings.GetFeatures())
            {
                string typeName = feature.GetType().Name;
                string featureName = feature.name;
                bool isQuest = typeName.IndexOf("MetaQuest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               featureName.IndexOf("Meta Quest Support", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isHandTracking = typeName.IndexOf("HandTracking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      featureName.IndexOf("Hand Tracking", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isQuest)
                {
                    feature.enabled = true;
                    metaQuestFeatureFound = true;
                }
                if (isHandTracking)
                {
                    feature.enabled = true;
                    handTrackingFeatureFound = true;
                }
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            if (!metaQuestFeatureFound || !handTrackingFeatureFound)
                Debug.LogWarning("BenchFlow XR: Review the Android OpenXR feature list; Quest Support and Hand Tracking must be enabled.");
        }
    }
}
#endif
