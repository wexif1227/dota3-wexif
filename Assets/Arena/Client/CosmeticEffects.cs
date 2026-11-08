using System;
using UnityEngine;
namespace Rift.Client
{
    // Source 2 sprite textures, simulated with Unity emitters. Bone offsets are baked in the idle pose.
    public sealed class CosmeticEffects:MonoBehaviour
    {
        [Serializable]public class Anchor
        {
            public Transform emitter,bone,aimFrom;
            public Vector3 localPoint;
        }
        public Anchor[] anchors=Array.Empty<Anchor>();
        public ParticleSystem[] systems=Array.Empty<ParticleSystem>();
        bool alive=true;
        public void Reposition()
        {
            foreach(var a in anchors)
            {
                if(!a.emitter||!a.bone)continue;
                a.emitter.position=a.bone.TransformPoint(a.localPoint);
                a.emitter.rotation=a.aimFrom?Quaternion.LookRotation((a.bone.position-a.aimFrom.position).normalized):Quaternion.identity;
            }
        }
        void OnEnable(){Reposition();alive=false;SetAlive(true);}
        void LateUpdate()=>Reposition();
        void OnDisable(){foreach(var p in systems)if(p)p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
        public void SetAlive(bool value)
        {
            if(alive==value)return;alive=value;
            foreach(var p in systems)if(p){if(value){p.Clear(false);p.Play(false);}else p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
            foreach(var a in anchors)if(a.emitter){var line=a.emitter.GetComponent<LineRenderer>();if(line)line.enabled=value;var trail=a.emitter.GetComponent<TrailRenderer>();if(trail){trail.Clear();trail.emitting=value;}}
        }
    }
}
