using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Rift.Client;
namespace Rift.Editor
{
    public class ArtTextureImporter:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            bool items=assetPath.EndsWith("/Art/Items.png"),abilities=assetPath.EndsWith("/Art/Abilities.png");if(!items&&!abilities)return;
            var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);int cols=items?6:4,rows=items?6:7;
            var sprites=new SpriteMetaData[cols*rows];for(int i=0;i<sprites.Length;i++)sprites[i]=new SpriteMetaData{name=(items?"Item_":"Ability_")+i.ToString("00"),alignment=9,pivot=new Vector2(.5f,.5f),rect=new Rect(i%cols*width/(float)cols+1,height-(i/cols+1)*height/(float)rows+1,width/(float)cols-2,height/(float)rows-2)};
            importer.spritesheet=sprites;
        }
    }
    public static class ArtAssets
    {
        [MenuItem("RIFT/Rebuild hero art")]
        public static void Bake()
        {
            string folder="Assets/Arena/Resources/Heroes";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string materialPath=folder+"/Hero.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(HeroArt.Material);AssetDatabase.CreateAsset(material,materialPath);}
            for(int hero=0;hero<6;hero++)
            {
                var model=HeroArt.Create(hero);var filters=model.GetComponentsInChildren<MeshFilter>();
                string meshPath=folder+"/Hero"+hero+".asset";
                // Rebuilding replaces only our generated mesh asset; existing prefab GUIDs are retained.
                if(AssetDatabase.LoadMainAssetAtPath(meshPath))AssetDatabase.DeleteAsset(meshPath);
                for(int i=0;i<filters.Length;i++){var mesh=filters[i].sharedMesh;if(i==0)AssetDatabase.CreateAsset(mesh,meshPath);else AssetDatabase.AddObjectToAsset(mesh,meshPath);}
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=material;
                PrefabUtility.SaveAsPrefabAsset(model,folder+"/Hero"+hero+".prefab");
                Debug.Log("RIFT_ART hero="+hero+" triangles="+filters.Sum(f=>f.sharedMesh.triangles.Length/3)+" parts="+filters.Length);UnityEngine.Object.DestroyImmediate(model);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        public static void Validate()
        {
            for(int hero=0;hero<6;hero++)
            {
                var prefab=Resources.Load<GameObject>("Heroes/Hero"+hero);if(!prefab||!prefab.GetComponent<HeroMotion>())throw new Exception("Missing hero prefab/animation: "+hero);
                foreach(var mesh in prefab.GetComponentsInChildren<MeshFilter>()){if(mesh.sharedMesh.vertexCount<3)throw new Exception("Empty mesh");foreach(var p in mesh.sharedMesh.vertices)if(float.IsNaN(p.x+p.y+p.z)||float.IsInfinity(p.x+p.y+p.z))throw new Exception("Invalid vertex");}
            }
            if(Resources.LoadAll<Sprite>("Art/Items").Length!=36||Resources.LoadAll<Sprite>("Art/Abilities").Length!=28)throw new Exception("Incomplete icon atlas import");
            Debug.Log("RIFT_ART_OK: 6 animated hero prefabs; 36 item sprites; 28 ability/upgrade sprites");
        }
        public static void Gallery()
        {
            Validate();UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            Shader.SetGlobalFloat("_RiftFog",0);RenderSettings.ambientLight=new Color(.42f,.47f,.57f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var light=new GameObject("Key light").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.86f,.69f);light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,-35,0);
            var fill=new GameObject("Cool fill").AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.48f,.72f,1);fill.intensity=.65f;fill.transform.rotation=Quaternion.Euler(15,145,0);
            for(int i=0;i<6;i++){var prefab=Resources.Load<GameObject>("DotaImported/Hero"+i);if(!prefab)prefab=Resources.Load<GameObject>("Heroes/Hero"+i);var g=UnityEngine.Object.Instantiate(prefab);g.transform.position=new Vector3((i%3-1)*6.1f,i<3?6:0,0);g.transform.rotation=Quaternion.Euler(0,165,0);var fx=g.GetComponent<CosmeticEffects>();if(fx){fx.Reposition();foreach(var ps in fx.systems)ps.Simulate(2.1f,false,true);}}
            var camera=new GameObject("Art review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.043f,.06f);camera.orthographic=true;camera.orthographicSize=7.25f;camera.transform.position=new Vector3(0,8,-24);camera.transform.LookAt(new Vector3(0,5.45f,0));
            var target=new RenderTexture(2048,1440,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var texture=new Texture2D(2048,1440,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,2048,1440),0,0);texture.Apply();Directory.CreateDirectory("artifacts");File.WriteAllBytes("artifacts/hero-gallery.png",texture.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Debug.Log("RIFT_GALLERY_OK");
        }
    }
}
