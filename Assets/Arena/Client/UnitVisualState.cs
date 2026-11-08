using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public sealed class UnitVisualState:MonoBehaviour
    {
        Renderer[] parts;MaterialPropertyBlock properties;float flash=-1;
        public void Sync(Unit unit)
        {
            float value=Mathf.Clamp01(unit.hitFlash/.2f);if(Mathf.Abs(value-flash)<.01f)return;flash=value;
            if(parts==null){parts=GetComponentsInChildren<Renderer>();properties=new MaterialPropertyBlock();}
            foreach(var part in parts)
            {
                if(part is ParticleSystemRenderer||part is LineRenderer||part is TrailRenderer)continue;
                part.GetPropertyBlock(properties);properties.SetFloat("_HitFlash",flash);part.SetPropertyBlock(properties);
            }
        }
    }
}
