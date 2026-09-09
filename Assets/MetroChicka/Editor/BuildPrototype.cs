using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MetroChicka.Editor
{
    public static class BuildPrototype
    {
        public const string ScenePath="Assets/MetroChicka/Scenes/UnfoldStudy.unity";
        [MenuItem("Metro Chicka/Prepare 3D Prototype")]
        public static void Prepare()
        {
            PlayerSettings.companyName="Metro Chicka";PlayerSettings.productName="Metro Chicka - Unfold Study";
            PlayerSettings.bundleVersion="0.1.0-study";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.metrochicka.unfoldstudy");
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}
            EditorSettings.serializationMode=SerializationMode.ForceText;
            if(!File.Exists("Assets/MetroChicka/Resources/ToyMaterial.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/MetroChicka/Resources/ToyMaterial.mat");
            if(!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Metro Chicka",typeof(Prototype));EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();
            Debug.Log("STUDY_PREPARED");
        }
        [MenuItem("Metro Chicka/Build macOS Study")]
        public static void Mac()=>Build(BuildTarget.StandaloneOSX,"Builds/Study/macOS/Metro Chicka.app");
        [MenuItem("Metro Chicka/Build Windows Study")]
        public static void Windows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Study/Windows/Metro Chicka.exe");
        private static void Build(BuildTarget target,string path)
        {
            Prepare();var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},target=target,locationPathName=path,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception(report.summary.result.ToString());
            Debug.Log("STUDY_BUILD_OK "+path);
        }
    }
}
