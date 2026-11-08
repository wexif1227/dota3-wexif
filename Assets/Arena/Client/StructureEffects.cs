using UnityEngine;
using UnityEngine.Rendering;
namespace Rift.Client
{
    public static class StructureEffects
    {
        public static void Attach(GameObject root,string key,float height)
        {
            bool ancient=key.StartsWith("Ancient"),tower=key.StartsWith("Tower"),ward=key=="Ward";
            if(!ancient&&!tower&&!ward)return;
            var material=Resources.Load<Material>("DotaImported/Particles/Glow");if(!material)return;
            bool radiant=key.EndsWith("0");Color color=ward?new Color(.4f,.8f,1):radiant?new Color(.38f,.83f,1):new Color(1,.25f,.06f);
            var emitter=new GameObject("Ambient energy");emitter.transform.SetParent(root.transform,false);emitter.transform.localPosition=new Vector3(0,height*(ancient?.52f:tower?.72f:.82f),0);
            var ps=emitter.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.playOnAwake=true;main.duration=4;main.startLifetime=new ParticleSystem.MinMaxCurve(1,2.2f);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.55f);main.startSize=new ParticleSystem.MinMaxCurve(ancient?.4f:.12f,ancient?1.1f:tower?.55f:.35f);main.startColor=color;main.maxParticles=64;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;
            var emission=ps.emission;emission.rateOverTime=ancient?15:tower?8:5;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=15;shape.radius=ancient?1.1f:tower?.35f:.15f;shape.rotation=new Vector3(-90,0,0);
            var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(color,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.2f),new GradientAlphaKey(0,1)});colors.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(.3f,1),new Keyframe(1,0)));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var fx=root.AddComponent<CosmeticEffects>();fx.systems=new[]{ps};ps.Play();
        }
    }
}
