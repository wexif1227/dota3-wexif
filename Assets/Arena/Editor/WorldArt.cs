using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Rift.Client;
namespace Rift.Editor
{
    public static class WorldArt
    {
        const string Root="Assets/Arena/Resources/DotaImported/World";
        [Serializable] class Source {public string key;public float height;public MaterialSource[] materials;}
        [Serializable] class MaterialSource {public string name,albedo,normal,alphaMode;public float cutoff;public bool hidden;}
        public static void Review(){Bake();Gallery();}
        public static void BuildWindows(){Validate();DotaArt.Validate();ArenaBuild.Windows();}
        public static void Gallery()
        {
            Shader.SetGlobalFloat("_RiftFog",0);RenderSettings.ambientLight=new Color(.5f,.55f,.6f);
            var root=new GameObject("Local asset review");var sun=new GameObject("Review sun").AddComponent<Light>();sun.transform.SetParent(root.transform);sun.type=LightType.Directional;sun.intensity=1.3f;sun.transform.rotation=Quaternion.Euler(48,-30,0);
            string[] keys={"Ancient0","Ancient1","Tower0","Tower1","Roshan","Barracks0","Barracks1","Courier","Ward","Creep0M","Creep0R","Creep1M","Creep1R"};
            for(int i=0;i<keys.Length;i++)
            {
                var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("DotaImported/World/"+keys[i]),root.transform);model.transform.position=new Vector3((i%5-2)*10,0,10-i/5*10);model.transform.rotation=Quaternion.Euler(0,155,0);
                var motion=model.GetComponent<ImportedHeroMotion>();if(motion&&!string.IsNullOrEmpty(motion.idle))motion.player[motion.idle].clip.SampleAnimation(motion.player.gameObject,0);
            }
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.SetParent(root.transform);floor.transform.localScale=new Vector3(6,1,4);floor.transform.position=Vector3.down*.03f;var mat=new Material(Shader.Find("Rift/Lit")){color=new Color(.15f,.2f,.22f)};floor.GetComponent<Renderer>().sharedMaterial=mat;
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.position=new Vector3(0,32,-43);camera.transform.LookAt(new Vector3(0,1,0));camera.orthographic=true;camera.orthographicSize=19;camera.backgroundColor=new Color(.04f,.06f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;
            var target=new RenderTexture(2000,1200,24);camera.targetTexture=target;var old=RenderTexture.active;camera.Render();RenderTexture.active=target;var texture=new Texture2D(2000,1200,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,2000,1200),0,0);texture.Apply();Directory.CreateDirectory("artifacts");File.WriteAllBytes("artifacts/minify-world-gallery.png",texture.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(mat);Debug.Log("RIFT_WORLD_GALLERY_OK");
        }
        [MenuItem("RIFT/Import local Minify world exports")]
        public static void Bake()
        {
            AssetDatabase.Refresh();
            foreach(string folder in Directory.GetDirectories(Root))
            {
                var source=JsonUtility.FromJson<Source>(File.ReadAllText(folder+"/source.json"));string path=folder+"/Model.fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);var definitions=importer.defaultClipAnimations;
                foreach(var clip in definitions){string n=clip.name.ToLowerInvariant();clip.loopTime=n.Contains("idle")||n.Contains("run")||n.Contains("walk");clip.loopPose=clip.loopTime;}
                importer.clipAnimations=definitions;importer.SaveAndReimport();
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.name="Visual";
                var materials=new Dictionary<string,Material>();
                foreach(var entry in source.materials)
                {
                    string materialPath=folder+"/"+entry.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(!mat){mat=new Material(Shader.Find("Rift/Dota"));AssetDatabase.CreateAsset(mat,materialPath);}
                    mat.shader=Shader.Find(entry.alphaMode=="BLEND"?"Rift/DotaTransparent":"Rift/Dota");mat.mainTexture=entry.hidden?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+entry.albedo);
                    if(!string.IsNullOrEmpty(entry.normal))mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+entry.normal));
                    mat.SetFloat("_Hidden",entry.hidden?1:0);mat.SetFloat("_Cutoff",entry.alphaMode=="MASK"?entry.cutoff:0);mat.SetFloat("_Glow",0);
                    mat.SetFloat("_Wind",source.key.StartsWith("Tree")?1:0);
                    if(entry.alphaMode=="BLEND")mat.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    EditorUtility.SetDirty(mat);materials[entry.name]=mat;
                }
                foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m&&materials.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();
                    renderer.enabled=renderer.sharedMaterials.Any(m=>m&&(!m.HasProperty("_Hidden")||m.GetFloat("_Hidden")<.5f));
                    if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
                }
                var root=new GameObject(source.key);var placement=new GameObject("Model scale").transform;placement.SetParent(root.transform,false);model.transform.SetParent(placement,false);
                var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                string Find(string term)=>clips.OrderBy(c=>c.name.Length).FirstOrDefault(c=>c.name.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)?.name??"";
                if(clips.Length>0)
                {
                    var motion=root.AddComponent<ImportedHeroMotion>();motion.player=model.GetComponent<Animation>()??model.AddComponent<Animation>();
                    foreach(var clip in clips){clip.legacy=true;if(motion.player[clip.name]==null)motion.player.AddClip(clip,clip.name);}
                    motion.idle=Find("idle");motion.run=Find("run");if(motion.run=="")motion.run=Find("walk");if(motion.run=="")motion.run=motion.idle;
                    motion.death=Find("death");motion.attacks=clips.Where(c=>c.name.IndexOf("attack",StringComparison.OrdinalIgnoreCase)>=0).Select(c=>c.name).ToArray();
                    if(motion.idle!="")clips.First(c=>c.name==motion.idle).SampleAnimation(model,0);
                }
                var renderers=model.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(renderers.Length==0)throw new Exception("Empty world model "+source.key);
                Bounds bounds=GeometryBounds(model);
                if(bounds.size.y<.001f)throw new Exception("Invalid bounds "+source.key);
                float scale=source.height/bounds.size.y;
                // Wide static bases must stay inside their gameplay footprint.
                float width=source.key.StartsWith("Ancient")?10:source.key.StartsWith("Barracks")?4.6f:source.key.StartsWith("Tower")?4.2f:source.key.StartsWith("Tree")?4.5f:float.PositiveInfinity;
                scale=Mathf.Min(scale,width/Mathf.Max(bounds.size.x,bounds.size.z));
                if(source.key.StartsWith("Tree"))
                {
                    int index=0;foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
                    {
                        var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;var colors=new Color[vertices.Length];
                        for(int i=0;i<vertices.Length;i++){float h=Mathf.Clamp01((filter.transform.TransformPoint(vertices[i]).y-bounds.min.y)/bounds.size.y);colors[i]=new Color(1,1,1,h*h);}
                        mesh.colors=colors;string meshPath=folder+"/WindMesh"+(index++)+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}
                        filter.sharedMesh=saved;
                    }
                }
                placement.localScale=Vector3.one*scale;placement.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                StructureEffects.Attach(root,source.key,bounds.size.y*scale);
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+source.key+".prefab");UnityEngine.Object.DestroyImmediate(root);
                Debug.Log("RIFT_WORLD_ASSET_OK "+source.key+" clips="+clips.Length+" height="+(bounds.size.y*scale));
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static Bounds GeometryBounds(GameObject model)
        {
            var bounds=new Bounds();bool first=true;
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if(!renderer.enabled)continue;Mesh mesh;var skin=renderer as SkinnedMeshRenderer;
                if(skin)mesh=skin.sharedMesh;else{var filter=renderer.GetComponent<MeshFilter>();if(!filter)continue;mesh=filter.sharedMesh;}
                var vertices=mesh.vertices;var weights=mesh.boneWeights;var poses=mesh.bindposes;var bones=skin?skin.bones:null;
                var visible=new HashSet<int>();for(int sub=0;sub<mesh.subMeshCount;sub++){var mat=renderer.sharedMaterials[sub];if(mat&&mat.HasProperty("_Hidden")&&mat.GetFloat("_Hidden")>.5f)continue;foreach(int i in mesh.GetTriangles(sub))visible.Add(i);}
                foreach(int i in visible)
                {
                    Vector3 point;
                    if(skin&&weights.Length==vertices.Length&&bones.Length>0)
                    {
                        var w=weights[i];point=Vector3.zero;
                        void Add(int bone,float weight){if(weight>0&&bones[bone])point+=(bones[bone].localToWorldMatrix*poses[bone]).MultiplyPoint3x4(vertices[i])*weight;}
                        Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);
                    }
                    else point=renderer.transform.TransformPoint(vertices[i]);
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }
        public static void Validate()
        {
            foreach(string name in new[]{"Rift/Dota","Rift/Terrain","Rift/Atmosphere","Rift/River","Rift/Path"}){var shader=Shader.Find(name);if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Broken visual shader "+name);}
            foreach(string key in new[]{"Tower0","Tower1","Ancient0","Ancient1","Barracks0","Barracks1","Creep0M","Creep0R","Creep1M","Creep1R","Roshan","Courier","Ward","Tree0A","Tree0B","Tree1A","Tree1B"})
            {
                var prefab=Resources.Load<GameObject>("DotaImported/World/"+key);if(!prefab)throw new Exception("Missing "+key);
                foreach(var r in prefab.GetComponentsInChildren<Renderer>())foreach(var mat in r.sharedMaterials)if(!mat||!mat.shader||ShaderUtil.ShaderHasError(mat.shader)||(!mat.mainTexture&&(!mat.HasProperty("_Hidden")||mat.GetFloat("_Hidden")<.5f)))throw new Exception("Broken material "+key+" "+(mat?mat.name:"null"));
                if(key.StartsWith("Creep"))
                {
                    var instance=UnityEngine.Object.Instantiate(prefab);var motion=instance.GetComponent<ImportedHeroMotion>();
                    if(!motion||motion.run==motion.idle||motion.attacks.Length==0)throw new Exception("Missing creep gameplay clips "+key);
                    foreach(string name in new[]{motion.run,motion.attacks[0]})
                    {
                        var clip=motion.player[name].clip;clip.SampleAnimation(motion.player.gameObject,0);var bones=instance.GetComponentsInChildren<Transform>();var rotations=bones.Select(t=>t.localRotation).ToArray();clip.SampleAnimation(motion.player.gameObject,clip.length*.4f);
                        if(bones.Where((t,i)=>Quaternion.Angle(rotations[i],t.localRotation)>.5f).Count()<3)throw new Exception("Frozen creep clip "+key+" "+name);
                    }
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
            foreach(string name in new[]{"default","default_enemy","default_team","attack_default","attack_enemy","attack_team","spell_default","spell_enemy","spell_team","move","buy"})
            {var t=Resources.Load<Texture2D>("DotaImported/Cursors/"+name);if(!t||!t.isReadable)throw new Exception("Missing readable cursor "+name);}
            Debug.Log("RIFT_WORLD_VALIDATION_OK models=17 animatedCreeps=4 cursors=11");
        }
    }
}
