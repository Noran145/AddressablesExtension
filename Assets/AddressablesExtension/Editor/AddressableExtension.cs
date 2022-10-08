using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Noran145.AddressablesExtension.Editor
{
    public class AddressableExtension : EditorWindow
    {
        private static string CATALOG_VERSION = "v1.0.0";

        /// <summary>
        /// Rewriting to target group.
        /// </summary>
        private static BuildTargetGroup TargetBuildGroup => BuildTargetGroup.WebGL;
        
        /// <summary>
        /// Rewriting to target platform.
        /// </summary>
        private static BuildTarget TargetBuildPlatform => BuildTarget.WebGL;

        private static BuildTarget CurrentBuildTarget => EditorUserBuildSettings.activeBuildTarget;

        private const string BUILD_PATH_VARIABLE_NAME = "CustomBuildPath";
        private const string LOAD_PATH_VARIABLE_NAME = "CustomLoadPath";
        private static string BUILD_PATH_DEFAULT_VALUE = $"ServerData/{TargetBuildPlatform}";
        private const string LOAD_PATH_DEFAULT_VALUE = "{VrmImporter.Config.AppConfig.RemoteAddressableUrl}";
        
        private static AddressableAssetSettings settings;
        private static AddressableAssetGroup activeSceneGroup;
        private const string DEFAULT_LOCAL_GROUP_NAME = "Default Local Group";
        private const string SCENE_LIST_GROUP_NAME = "SceneList";
        
        private const string SHADER_BUNDLE_NAMING = "custom";
        
        [MenuItem("Noran145/AddressableExtension")]
        private static void ShowWindow()
        {
            var window = GetWindow<AddressableExtension>();
            window.titleContent = new GUIContent("AddressableExtension");
        }

        private void OnGUI()
        {
            if (EditorUserBuildSettings.activeBuildTarget == TargetBuildPlatform)
            {
                GUILayout.Space(20f);
                EditorGUILayout.HelpBox("Set up Addressable Group and Profile Button.", MessageType.Info);
                if (GUILayout.Button($"Initialize {CurrentBuildTarget} AddressableSettings"))
                {
                    SetAddressable();
                }
                
                GUILayout.Space(20f);
                EditorGUILayout.HelpBox("Start Addressable Build Button.", MessageType.Info);
                if (GUILayout.Button("Build Addressable"))
                {
                    StartAddressableBuild();
                
                    EditorUtility.RevealInFinder(BUILD_PATH_DEFAULT_VALUE);

                    var files = Directory.GetFiles(BUILD_PATH_DEFAULT_VALUE);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        Debug.Log($"FileName: {Path.GetFileName(file)}\nFileSize: {fileInfo.Length}");
                    }
                }
            }
            else
            {
                GUILayout.Space(20f);
                EditorGUILayout.HelpBox($"We are using {TargetBuildPlatform} and need to switch platforms to {TargetBuildPlatform}.", MessageType.Info);
                if (GUILayout.Button($"Switch to {TargetBuildPlatform} Platform"))
                {
                    const string path = "Assets/AddressableAssetsData";
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                    
                    SwitchToTargetPlatform();
                }
                
                GUILayout.Space(20f);
                EditorGUILayout.HelpBox($"Sorry, please switch your platform to {TargetBuildPlatform}. Please press 'Switch to {TargetBuildPlatform} Platform' above.", MessageType.Error);
                EditorGUI.BeginDisabledGroup(true);
                if (GUILayout.Button("Initialize AddressableSettings"))
                {
                    SetAddressable();
                }
                EditorGUI.EndDisabledGroup();
                
                GUILayout.Space(20f);
                EditorGUILayout.HelpBox($"Sorry, please switch your platform to {TargetBuildPlatform}. Please press 'Switch to {TargetBuildPlatform} Platform' above.", MessageType.Error);
                EditorGUI.BeginDisabledGroup(true);
                if (GUILayout.Button("Build Addressable"))
                {
                    StartAddressableBuild();
                
                    EditorUtility.RevealInFinder(BUILD_PATH_DEFAULT_VALUE);

                    var files = Directory.GetFiles(BUILD_PATH_DEFAULT_VALUE);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        Debug.Log($"FileName: {Path.GetFileName(file)}\nFileSize: {fileInfo.Length}");
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private static void SwitchToTargetPlatform()
        {
            var isSuccess = EditorUserBuildSettings.SwitchActiveBuildTarget(TargetBuildGroup, TargetBuildPlatform);
            if (isSuccess)
            {
                EditorUtility.DisplayDialog("Success", $"Switch Platform is Success, thank you installing {TargetBuildPlatform} Platform",
                    "Yes");
            }
            else
            {
                EditorUtility.DisplayDialog("Failed", $"Switch Platform is Failed, please install {TargetBuildPlatform} Platform",
                    "Yes");
            }
        }

        private static void SetAddressable()
        {
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
            {
                // AddressableAssetSettingsDefaultObjectがなければ作成する
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                settings.BuildRemoteCatalog = true;
                settings.OverridePlayerVersion = CATALOG_VERSION;
            }
            else
            {
                // // AddressableAssetSettingsDefaultObjectがあれば既存のものを使う
                settings = AddressableAssetSettingsDefaultObject.Settings;
            }

            activeSceneGroup = settings.groups.FirstOrDefault(x => x.Name == SCENE_LIST_GROUP_NAME);
            if (activeSceneGroup == null)
            {
                var groupTemplate = settings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
                activeSceneGroup = settings.CreateGroup(SCENE_LIST_GROUP_NAME, true, false, false, null, groupTemplate.GetTypes());
            }

            SetProfileSettings(settings);

            SetSchemaSettings(activeSceneGroup, settings);
            SetActiveSceneInGroupEntity(activeSceneGroup, settings);
        
            DeleteDefaultGroup(settings);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Set ProfileSettings
        /// </summary>
        /// <param name="settings"></param>
        private static void SetProfileSettings(AddressableAssetSettings settings)
        {
            settings.profileSettings.CreateValue(BUILD_PATH_VARIABLE_NAME, BUILD_PATH_DEFAULT_VALUE);
            settings.profileSettings.CreateValue(LOAD_PATH_VARIABLE_NAME, LOAD_PATH_DEFAULT_VALUE);
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, BUILD_PATH_VARIABLE_NAME);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, LOAD_PATH_VARIABLE_NAME);
            settings.ShaderBundleNaming = ShaderBundleNaming.Custom;
            settings.ShaderBundleCustomNaming = SHADER_BUNDLE_NAMING;
        }

        /// <summary>
        /// Set SchemaSettings
        /// </summary>
        /// <param name="group"></param>
        /// <param name="settings"></param>
        private static void SetSchemaSettings(AddressableAssetGroup group, AddressableAssetSettings settings)
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            schema.BuildPath.SetVariableByName(settings, BUILD_PATH_VARIABLE_NAME);
            schema.LoadPath.SetVariableByName(settings, LOAD_PATH_VARIABLE_NAME);
        }

        /// <summary>
        /// Delete DefaultGroup
        /// </summary>
        /// <param name="settings"></param>
        private static void DeleteDefaultGroup(AddressableAssetSettings settings)
        {
            var defaultGroup = settings.groups.FirstOrDefault(x => x.Name == DEFAULT_LOCAL_GROUP_NAME);
            if (defaultGroup != null) settings.RemoveGroup(defaultGroup);
        }
        
        /// <summary>
        /// Add ActiveScene in SceneListGroup
        /// </summary>
        /// <param name="assetGroup"></param>
        /// <param name="assetSettings"></param>
        private static void SetActiveSceneInGroupEntity(AddressableAssetGroup assetGroup, AddressableAssetSettings assetSettings)
        {
            Debug.Log(SceneManager.GetActiveScene().path);
            var guid = AssetDatabase.GUIDFromAssetPath(SceneManager.GetActiveScene().path).ToString();
            var assetEntry = assetSettings.CreateOrMoveEntry(guid, assetGroup);
            assetEntry.SetAddress(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
        }

        /// <summary>
        /// Start Addressable Build
        /// </summary>
        private static void StartAddressableBuild()
        {
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent();
        }
    }
}
