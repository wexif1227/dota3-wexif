using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Rift.Client;
namespace Rift.Editor
{
    public sealed class DotaModelImporter:AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.Contains("/DotaImported/"))return;var importer=(ModelImporter)assetImporter;
            importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.animationCompression=ModelImporterAnimationCompression.Optimal;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/DotaImported/"))return;var importer=(TextureImporter)assetImporter;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.mipmapEnabled=true;
            if(assetPath.Contains("/Cursors/")){importer.textureType=TextureImporterType.Cursor;importer.isReadable=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;return;}
            if(assetPath.Contains("normal_")){importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;}
        }
    }
    public static class DotaArt
    {
        [Serializable]class MaterialSource{public string name,albedo,normal,alphaMode;public bool hidden,additive;public float cutoff;}
        [Serializable]class Source{public int hero;public string name;public string[] animations;public MaterialSource[] materials;}
        const string Root="Assets/Arena/Resources/DotaImported";
        public static void Review(){Bake();Validate();CosmeticValidation.Run();ArtAssets.Gallery();}
        public static void BuildWindows(){Validate();ArenaBuild.Windows();}
        public static void Validate()
        {
            for(int hero=0;hero<6;hero++)
            {
                var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("DotaImported/Hero"+hero));var motion=model.GetComponent<ImportedHeroMotion>();
                if(!motion||!motion.player)throw new Exception("Missing skeletal animation player "+hero);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)if(!material||!material.mainTexture||!material.shader||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Invalid local hero material "+hero);
                foreach(string name in new[]{motion.run}.Concat(motion.attacks))
                {
                    var clip=motion.player[name].clip;clip.SampleAnimation(motion.player.gameObject,0);var bones=model.GetComponentsInChildren<Transform>();var rotations=bones.Select(t=>t.localRotation).ToArray();var positions=bones.Select(t=>t.localPosition).ToArray();clip.SampleAnimation(motion.player.gameObject,clip.length*.4f);
                    int changed=0;for(int i=0;i<bones.Length;i++)if(Quaternion.Angle(rotations[i],bones[i].localRotation)>.5f||Vector3.Distance(positions[i],bones[i].localPosition)>.001f)changed++;
                    if(changed<3)throw new Exception("Animation does not move the skeleton: "+hero+" "+name);
                    Debug.Log("RIFT_SKELETON_OK hero="+hero+" clip="+name+" animatedBones="+changed);
                }
                UnityEngine.Object.DestroyImmediate(model);
            }
            Debug.Log("RIFT_DOTA_VALIDATION_OK");
        }
        [MenuItem("RIFT/Import local Dota hero exports")]
        public static void Bake()
        {
            AssetDatabase.Refresh();
            CosmeticArt.Prepare();
            foreach(string name in new[]{"Rift/Dota","Rift/DotaTransparent"}){var shader=Shader.Find(name);if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Hero shader failed: "+name);}
            for(int hero=0;hero<6;hero++)
            {
                string folder=Root+"/Hero"+hero,path=folder+"/Hero"+hero+".fbx";
                if(!File.Exists(path))throw new Exception("Missing local export: "+path);
                var source=JsonUtility.FromJson<Source>(File.ReadAllText(folder+"/source.json"));
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                var definitions=importer.defaultClipAnimations;
                foreach(var definition in definitions){string name=definition.name.ToLowerInvariant();definition.loopTime=name.Contains("idle")||name.Contains("run")||name.Contains("walk")||name.Contains("channel");definition.loopPose=definition.loopTime;}
                importer.clipAnimations=definitions;importer.SaveAndReimport();
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.name="Visual";
                var materials=new Dictionary<string,Material>();
                foreach(var entry in source.materials)
                {
                    string materialPath=folder+"/"+entry.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(!material){material=new Material(Shader.Find("Rift/Dota"));AssetDatabase.CreateAsset(material,materialPath);}
                    material.shader=Shader.Find(entry.alphaMode=="BLEND"?"Rift/DotaTransparent":"Rift/Dota");material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+entry.albedo);
                    if(!string.IsNullOrEmpty(entry.normal))material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+entry.normal));
                    material.SetFloat("_Glow",entry.additive?1:hero==1?.15f:0);material.SetFloat("_Hidden",entry.hidden?1:0);material.SetFloat("_Cutoff",entry.alphaMode=="MASK"?entry.cutoff:0);
                    if(entry.alphaMode=="BLEND")material.SetFloat("_DstBlend",entry.additive?(float)UnityEngine.Rendering.BlendMode.One:(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    EditorUtility.SetDirty(material);materials[entry.name]=material;
                }
                foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m&&materials.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();
                    renderer.enabled=renderer.sharedMaterials.Any(m=>m&&(!m.HasProperty("_Hidden")||m.GetFloat("_Hidden")<.5f));
                    if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
                }
                var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                string Find(string term)=>clips.OrderBy(c=>c.name.Length).FirstOrDefault(c=>c.name.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)?.name??"";
                var root=new GameObject("Dota "+source.name);var placement=new GameObject("Model scale").transform;placement.SetParent(root.transform,false);model.transform.SetParent(placement,false);
                var motion=root.AddComponent<ImportedHeroMotion>();motion.player=model.GetComponent<Animation>();if(!motion.player)motion.player=model.AddComponent<Animation>();
                foreach(var clip in clips){clip.legacy=true;if(motion.player[clip.name]==null)motion.player.AddClip(clip,clip.name);}
                motion.idle=Find("idle");motion.run=hero==3?Find("walk"):Find("run");if(motion.run=="")motion.run=Find("walk");motion.channel=Find("channel");motion.death=Find("death");
                motion.attacks=clips.Where(c=>c.name.IndexOf("attack",StringComparison.OrdinalIgnoreCase)>=0).Select(c=>c.name).Take(3).ToArray();
                if(motion.idle==""||motion.run==""||motion.attacks.Length==0)throw new Exception("Incomplete gameplay animations: "+hero);
                var idle=clips.First(c=>c.name==motion.idle);idle.SampleAnimation(model,0);
                Bounds bounds=new Bounds();bool first=true;
                foreach(var renderer in model.GetComponentsInChildren<Renderer>()){if(!renderer.enabled)continue;if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                if(first||bounds.size.y<.001f)throw new Exception("Empty local hero model "+hero);
                float scale=(hero==0?2.9f:hero==1?5.1f:hero==3?4.3f:4.8f)/bounds.size.y;
                placement.localScale=Vector3.one*scale;placement.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                CosmeticArt.Decorate(root,hero);
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Hero"+hero+".prefab");
                Debug.Log("RIFT_DOTA_HERO_OK hero="+hero+" clips="+clips.Length+" idle="+motion.idle+" run="+motion.run+" scale="+scale+" bounds="+bounds);
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("RIFT_DOTA_ART_OK");
        }
    }
}
