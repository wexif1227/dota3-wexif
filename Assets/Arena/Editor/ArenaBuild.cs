using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rift.Editor
{
    [InitializeOnLoad]
    public static class ArenaBuild
    {
        static double next;
        static ArenaBuild(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;next=EditorApplication.timeSinceStartup+1;
            string file=Path.GetFullPath("artifacts/editor-command.txt");if(!File.Exists(file))return;
            string command=File.ReadAllText(file).Trim();File.Delete(file);
            try
            {
                if(command=="refresh")AssetDatabase.Refresh();
                else if(command=="setup")Setup();
                else if(command=="windows")Windows();
                else if(command=="android")Android();
                else if(command=="art")ArtAssets.Bake();
                else if(command=="dota")DotaArt.Bake();
                else if(command=="world")WorldArt.Bake();
                else if(command=="play"){Setup();EditorApplication.isPlaying=true;}
                else if(command=="stop")EditorApplication.isPlaying=false;
                else if(command=="smoke"&&Rift.Client.ArenaApp.Instance)Rift.Client.ArenaApp.Instance.StartLocal();
                else if(command=="capture")ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/editor-capture.png"));
                File.WriteAllText("artifacts/editor-result.txt","OK "+command);
            }
            catch(Exception e){File.WriteAllText("artifacts/editor-result.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("RIFT/Prepare project")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Arena/Scenes");
            string scenePath="Assets/Arena/Scenes/Arena.unity";
            if(!File.Exists(scenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("RIFT runtime bootstrap — press Play");EditorSceneManager.SaveScene(scene,scenePath);
            }
            else if(!EditorApplication.isPlaying)EditorSceneManager.OpenScene(scenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scenePath,true)};
            PlayerSettings.companyName="Wexif";PlayerSettings.productName="Rift — Dota3 Alpha";PlayerSettings.bundleVersion="0.3.1";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.wexif.riftalpha");
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.runInBackground=true;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel23;PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.forceInternetPermission=true;
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=graphics.FindProperty("m_AlwaysIncludedShaders");
            for(int i=list.arraySize-1;i>=0;i--){var existing=list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;if(existing&&existing.name=="Standard"){list.GetArrayElementAtIndex(i).objectReferenceValue=null;list.DeleteArrayElementAtIndex(i);}}
            foreach(string name in new[]{"Rift/Terrain","Rift/Lit","Rift/Hero","Rift/Dota","Rift/DotaTransparent","Rift/CosmeticParticle","Rift/Atmosphere","Rift/River","Rift/Path","Sprites/Default"})
            {
                var shader=Shader.Find(name);if(!shader)continue;bool found=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){int index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=2;AssetDatabase.SaveAssets();
            Debug.Log("RIFT_SETUP_OK");
        }
        [MenuItem("RIFT/Build Windows")]
        public static void Windows(){Setup();Build(BuildTarget.StandaloneWindows64,"Builds/Windows/Rift.exe");}
        [MenuItem("RIFT/Build Android APK")]
        public static void Android(){Setup();EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Android/Rift.apk");}
        public static void Smoke()
        {
            Setup();Directory.CreateDirectory("artifacts");EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var gameViewType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(gameViewType!=null){var view=EditorWindow.GetWindow(gameViewType);view.position=new Rect(0,0,1600,900);view.Show();}
            EditorApplication.isPlaying=true;double started=EditorApplication.timeSinceStartup;
            EditorApplication.update+=WaitSmoke;
            void WaitSmoke()
            {
                if(EditorApplication.timeSinceStartup-started<24)return;
                var app=Rift.Client.ArenaApp.Instance;
                if(app==null){File.WriteAllText("artifacts/smoke-result.txt","FAIL no app");EditorApplication.Exit(1);return;}
                var world=app.World;var target=new RenderTexture(1600,900,24);world.Camera.targetTexture=target;world.Camera.Render();RenderTexture.active=target;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("artifacts/world.png",tex.EncodeToPNG());RenderTexture.active=null;world.Camera.targetTexture=null;
                string result="time="+(app.State?.time??0)+" units="+(app.State?.units.Count??0)+" screen="+app.ScreenName;File.WriteAllText("artifacts/smoke-result.txt",result);Debug.Log("RIFT_PLAYMODE_OK "+result);EditorApplication.update-=WaitSmoke;EditorApplication.Exit(0);
            }
        }
        static void Build(BuildTarget target,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Arena/Scenes/Arena.unity"},locationPathName=path,target=target,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result+" errors="+report.summary.totalErrors);
            Debug.Log("RIFT_BUILD_OK "+path+" bytes="+report.summary.totalSize);
        }
    }
}
