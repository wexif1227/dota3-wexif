using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Rift.Client;
namespace Rift.Editor
{
    public static class CosmeticValidation
    {
        public static void Run()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            int systems=0,live=0;
            for(int hero=0;hero<6;hero++)
            {
                var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("DotaImported/Hero"+hero));var fx=model.GetComponent<CosmeticEffects>();
                if(!fx||fx.systems.Length<3||fx.anchors.Length<2)throw new Exception("Missing cosmetic effects "+hero);
                foreach(var anchor in fx.anchors)if(Vector3.Distance(anchor.emitter.position,model.transform.position)>12)throw new Exception("Cosmetic attachment outside model "+hero);
                var motion=model.GetComponent<ImportedHeroMotion>();var start=fx.anchors.Select(a=>a.emitter.position).ToArray();
                motion.player[motion.run].clip.SampleAnimation(motion.player.gameObject,motion.player[motion.run].length*.4f);fx.Reposition();
                int moving=fx.anchors.Where((a,i)=>Vector3.Distance(a.emitter.position,start[i])>.005f).Count();if(moving==0)throw new Exception("Cosmetics do not follow animation "+hero);
                model.transform.position=new Vector3(2,0,-3);model.transform.rotation=Quaternion.Euler(0,47,0);fx.Reposition();
                foreach(var a in fx.anchors)if(Vector3.Distance(a.emitter.position,a.bone.TransformPoint(a.localPoint))>.001f)throw new Exception("Cosmetic attachment drift "+hero);
                foreach(var ps in fx.systems){ps.Simulate(1.8f,false,true);if(ps.particleCount<1)throw new Exception("Empty particle emitter "+hero+" "+ps.name);live+=ps.particleCount;systems++;}
                fx.SetAlive(false);if(fx.systems.Any(p=>p.isPlaying||p.particleCount>0))throw new Exception("Death leaves cosmetic particles "+hero);
                fx.SetAlive(true);foreach(var ps in fx.systems){ps.Simulate(.8f,false,true);if(ps.particleCount==0)throw new Exception("Respawn loses cosmetics "+hero);}
                Debug.Log("RIFT_COSMETIC_PASS hero="+hero+" systems="+fx.systems.Length+" movingAnchors="+moving+" death/respawn=ok");
                // Keep only sprites/ribbons for the visibility render below.
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.enabled=false;
                model.transform.position=new Vector3((hero%3-1)*6.1f,hero<3?6:0,0);model.transform.rotation=Quaternion.Euler(0,165,0);fx.Reposition();foreach(var ps in fx.systems)ps.Simulate(1.8f,false,true);
            }
            var camera=new GameObject("Cosmetic visibility camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=7.25f;camera.transform.position=new Vector3(0,8,-24);camera.transform.LookAt(new Vector3(0,5.45f,0));
            var target=new RenderTexture(768,512,24);camera.targetTexture=target;
            Shader.SetGlobalTexture("_RiftVision",Texture2D.whiteTexture);Shader.SetGlobalFloat("_RiftFog",1);var visible=Capture(camera,target);int visiblePixels=visible.GetPixels32().Count(p=>p.r>12||p.g>12||p.b>12);if(visiblePixels<100)throw new Exception("Particle shader renders no visible sprites");
            Shader.SetGlobalTexture("_RiftVision",Texture2D.blackTexture);var hidden=Capture(camera,target);int hiddenPixels=hidden.GetPixels32().Count(p=>p.r>12||p.g>12||p.b>12);if(hiddenPixels>2)throw new Exception("Cosmetics leak through fog: "+hiddenPixels);
            Directory.CreateDirectory("artifacts");File.WriteAllBytes("artifacts/cosmetic-particles-only.png",visible.EncodeToPNG());Shader.SetGlobalFloat("_RiftFog",0);
            Debug.Log("RIFT_COSMETIC_VALIDATION_OK systems="+systems+" liveParticles="+live+" visiblePixels="+visiblePixels+" hiddenPixels="+hiddenPixels);
        }
        static Texture2D Capture(Camera camera,RenderTexture target)
        {
            camera.Render();RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=null;return image;
        }
    }
}
