using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Rift.Client;
namespace Rift.Editor
{
    public static class CosmeticArt
    {
        const string Root="Assets/Arena/Resources/DotaImported/Particles";
        const string Raw="artifacts/particle-raw/materials/particle/";
        static Material flame,spiritFlame,glow,snow,smoke,beam;
        static readonly Color Warm=new Color(1,.8f,.38f),Ice=new Color(.38f,.8f,1),Psi=new Color(1,.68f,.3f);
        public static void Prepare()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var shader=Shader.Find("Rift/CosmeticParticle");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Cosmetic particle shader failed");
            Atlas("Flame",Directory.GetFiles(Raw+"fire_particle_8","fire_particle_8_low_seq0_*.png").OrderBy(p=>int.Parse(Path.GetFileNameWithoutExtension(p).Split('_').Last())).Take(16).ToArray(),4,128);
            Copy("Glow",Raw+"particle_glow_04.png");Copy("Snow",Raw+"snowflake/snowflakes_01_seq0.png");Copy("Smoke",Raw+"smoke3/smoke3b_seq0.png");Copy("Beam",Raw+"particle_glow_04.png");
            flame=Material("Flame",true,2.1f);glow=Material("Glow",true,1.4f);snow=Material("Snow",false,1.2f);smoke=Material("Smoke",false,.65f);beam=Material("Beam",true,2);
            spiritFlame=Material("SpiritFlame",true,2.1f);spiritFlame.mainTexture=flame.mainTexture;spiritFlame.SetFloat("_Monochrome",1);EditorUtility.SetDirty(spiritFlame);
        }
        static void Copy(string name,string source)
        {
            File.Copy(source,Root+"/"+name+".png",true);ImportTexture(Root+"/"+name+".png");
        }
        static void ImportTexture(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        static void Atlas(string name,string[] frames,int cols,int cell)
        {
            if(frames.Length==0)throw new Exception("Missing extracted flame frames");
            var atlas=new Texture2D(cols*cell,cols*cell,TextureFormat.RGBA32,false);atlas.SetPixels(new Color[cols*cols*cell*cell]);
            for(int frame=0;frame<cols*cols;frame++)
            {
                var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(frames[Math.Min(frame,frames.Length-1)]));float scale=Mathf.Min((cell-4f)/image.width,(cell-4f)/image.height);
                int width=Mathf.RoundToInt(image.width*scale),height=Mathf.RoundToInt(image.height*scale),x0=frame%cols*cell+(cell-width)/2,y0=(cols-1-frame/cols)*cell+2;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)atlas.SetPixel(x0+x,y0+y,image.GetPixelBilinear((x+.5f)/width,(y+.5f)/height));UnityEngine.Object.DestroyImmediate(image);
            }
            atlas.Apply();File.WriteAllBytes(Root+"/"+name+".png",atlas.EncodeToPNG());UnityEngine.Object.DestroyImmediate(atlas);ImportTexture(Root+"/"+name+".png");
        }
        static Material Material(string name,bool additive,float gain)
        {
            string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Rift/CosmeticParticle"));AssetDatabase.CreateAsset(mat,path);}
            mat.shader=Shader.Find("Rift/CosmeticParticle");mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+name+".png");mat.SetFloat("_DstBlend",additive?(float)BlendMode.One:(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_Gain",gain);EditorUtility.SetDirty(mat);return mat;
        }
        sealed class Builder
        {
            public readonly GameObject root;public readonly List<CosmeticEffects.Anchor> anchors=new List<CosmeticEffects.Anchor>();public readonly List<ParticleSystem> systems=new List<ParticleSystem>();
            public Builder(GameObject root){this.root=root;}
            public Transform Bone(string name)=>root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==name)??throw new Exception("Missing cosmetic bone: "+name);
            public Transform Anchor(string name,Transform bone,Vector3 point,Transform aim=null)
            {
                var t=new GameObject("Cosmetic / "+name).transform;t.SetParent(root.transform,false);t.position=point;
                anchors.Add(new CosmeticEffects.Anchor{bone=bone,emitter=t,localPoint=bone.InverseTransformPoint(point),aimFrom=aim});return t;
            }
            public ParticleSystem Emit(Transform anchor,string name,Material material,Color color,float rate,float size,float life,float radius,float rise,bool flipbook=false)
            {
                var obj=new GameObject(name);obj.transform.SetParent(anchor,false);var ps=obj.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.duration=3;main.loop=true;main.prewarm=false;main.playOnAwake=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;main.startLifetime=new ParticleSystem.MinMaxCurve(life*.7f,life);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(size*.65f,size);main.startColor=color;main.startRotation=new ParticleSystem.MinMaxCurve(-.25f,.25f);main.maxParticles=Mathf.CeilToInt(rate*life*1.5f)+8;main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;
                var emission=ps.emission;emission.rateOverTime=rate;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=Mathf.Max(.01f,radius);
                var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(-.13f,.13f);velocity.y=new ParticleSystem.MinMaxCurve(rise*.65f,rise);velocity.z=new ParticleSystem.MinMaxCurve(-.13f,.13f);
                var colorLife=ps.colorOverLifetime;colorLife.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.12f),new GradientAlphaKey(.5f,.55f),new GradientAlphaKey(0,1)});colorLife.color=gradient;
                var sizeLife=ps.sizeOverLifetime;sizeLife.enabled=true;sizeLife.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.55f),new Keyframe(.25f,1),new Keyframe(1,.2f)));
                if(flipbook){var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=sheet.numTilesY=4;sheet.animation=ParticleSystemAnimationType.WholeSheet;sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));sheet.cycleCount=1;}
                ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(231+systems.Count*17);var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.maxParticleSize=.6f;
                systems.Add(ps);return ps;
            }
            public void Fire(Transform anchor,float size,float rate=18)
            {
                Emit(anchor,"Animated flame",flame,Warm,rate,size,.65f,size*.12f,size*.8f,true);
                Emit(anchor,"Embers",glow,new Color(1,.25f,.03f,.8f),rate*.28f,.055f,1.2f,size*.2f,size*1.5f);
            }
            public void Frost(Transform anchor,float size)
            {
                Emit(anchor,"Cold mist",smoke,new Color(.5f,.82f,1,.32f),9,size*1.4f,1.1f,size*.15f,-.22f);
                Emit(anchor,"Ice crystals",snow,new Color(.75f,.95f,1,.7f),7,.09f,1.45f,size*.38f,-.4f);
                Emit(anchor,"Ice aura",glow,new Color(.15f,.6f,1,.45f),6,size,.6f,size*.1f,.15f);
            }
            public void Finish()
            {
                var fx=root.AddComponent<CosmeticEffects>();fx.anchors=anchors.ToArray();fx.systems=systems.ToArray();fx.Reposition();
            }
        }
        public static void Decorate(GameObject root,int hero)
        {
            var b=new Builder(root);
            if(hero==0)
            {
                var points=Candles(root);if(points.Count<2)throw new Exception("Could not locate Broodmother candle tips");
                var bones=root.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.bones).Where(t=>t).Distinct().ToArray();
                foreach(var point in points){var bone=bones.OrderBy(t=>(t.position-point).sqrMagnitude).First();b.Fire(b.Anchor("Candle",bone,point+Vector3.up*.06f),.37f,12);}
            }
            if(hero==1)
            {
                var head=b.Bone("Head0_JNT");b.Fire(b.Anchor("Arcana crown",head,head.position+Vector3.up*.48f),1.1f,30);
                var spine=b.Bone("Spine2_JNT");b.Fire(b.Anchor("Arcana core",spine,spine.position+Vector3.forward*.4f),.9f,20);
                foreach(string side in new[]{"Left","Right"}){var wrist=b.Bone(side+"Arm3_JNT");b.Fire(b.Anchor("Burning claws",wrist,wrist.position),.56f,12);}
            }
            if(hero==2)
            {
                foreach(string side in new[]{"L","R"})
                {
                    var wrist=b.Bone("wrist_"+side);var a=b.Anchor("Psi blade",wrist,wrist.position,b.Bone("elbow_"+side));
                    b.Emit(a,"Psi glow",glow,Psi,18,.42f,.55f,.13f,.12f);b.Emit(a,"Psi sparks",glow,new Color(1,.55f,.85f,.8f),12,.065f,.8f,.2f,.4f);
                    var line=a.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;line.SetPositions(new[]{Vector3.zero,Vector3.forward*.7f,Vector3.forward*1.45f});line.widthCurve=new AnimationCurve(new Keyframe(0,.04f),new Keyframe(.3f,.23f),new Keyframe(1,0));line.sharedMaterial=beam;line.startColor=Psi;line.endColor=new Color(1,.85f,.55f,0);line.shadowCastingMode=ShadowCastingMode.Off;
                    var trail=a.gameObject.AddComponent<TrailRenderer>();trail.time=.22f;trail.minVertexDistance=.08f;trail.widthMultiplier=.18f;trail.sharedMaterial=glow;trail.startColor=new Color(1,.65f,.35f,.7f);trail.endColor=new Color(1,.3f,.8f,0);trail.shadowCastingMode=ShadowCastingMode.Off;
                }
            }
            if(hero==3)
            {
                foreach(string side in new[]{"R","L"}){var bone=b.Bone("wrist_"+side);b.Frost(b.Anchor("Frost weapon",bone,Tip(root,"frostivus2018_pudge_walrus_slaughterer_"+(side=="R"?"weapon":"off_hand"),bone.position)),.65f);}
            }
            if(hero==4)
            {
                var head=b.Bone("Head_0");var a=b.Anchor("Spirit mane",head,head.position+Vector3.up*.7f);b.Emit(a,"Blue spirit flame",spiritFlame,new Color(.16f,.75f,1,.8f),26,1.4f,.9f,.2f,1.65f,true);b.Emit(a,"Spirit motes",glow,Ice,12,.07f,1.3f,.35f,.65f);
                var mace=b.Bone("mace_R3C0");b.Frost(b.Anchor("Spirit weapon",mace,mace.position),.7f);
            }
            if(hero==5)
            {
                var staff=b.Bone("staff_1");b.Fire(b.Anchor("Sufferwood staff",staff,Tip(root,"dark_staff",staff.position,true)),.85f,25);
                var spine=b.Bone("Spine_3");b.Emit(b.Anchor("Living mantle",spine,spine.position),glow.name,glow,new Color(.55f,1,.35f,.4f),7,.055f,1.6f,.75f,.35f);
            }
            b.Finish();Debug.Log("RIFT_COSMETIC_BAKED hero="+hero+" anchors="+b.anchors.Count+" systems="+b.systems.Count);
        }
        static Vector3[] Vertices(SkinnedMeshRenderer renderer)
        {
            // Apply bind poses directly: BakeMesh can retain the FBX centimetre scale a second time.
            var mesh=renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
            var matrices=renderer.bones.Select((bone,i)=>bone.localToWorldMatrix*bind[i]).ToArray();
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];var v=vertices[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
            }
            return vertices;
        }
        static Vector3 Tip(GameObject root,string material,Vector3 origin,bool highest=false)
        {
            var renderer=root.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r=>r.sharedMaterials.Any(m=>m&&m.name==material));
            if(!renderer)throw new Exception("Missing cosmetic weapon: "+material);
            return highest?Vertices(renderer).OrderByDescending(p=>p.y).First():Vertices(renderer).OrderByDescending(p=>(p-origin).sqrMagnitude).First();
        }
        static List<Vector3> Candles(GameObject root)
        {
            var points=new List<Vector3>();
            foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!r.enabled)continue;var mesh=r.sharedMesh;var world=Vertices(r);var uv=mesh.uv;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var material=r.sharedMaterials[sub];if(!material||!material.mainTexture)continue;var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(material.mainTexture)));
                    foreach(int i in mesh.GetTriangles(sub).Distinct())
                    {
                        var c=texture.GetPixelBilinear(uv[i].x,uv[i].y);var p=world[i];if(p.y<1.4f||c.r<.55f||c.g<.45f||c.b<.18f||c.g<c.b+.05f)continue;points.Add(p);
                    }
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            var tips=new List<Vector3>();foreach(var point in points.OrderByDescending(p=>p.y)){if(tips.All(p=>new Vector2(p.x-point.x,p.z-point.z).sqrMagnitude>.36f*.36f))tips.Add(point);if(tips.Count>=5)break;}
            return tips;
        }
    }
}
