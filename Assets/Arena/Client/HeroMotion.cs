using System;
using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public sealed class HeroMotion:MonoBehaviour
    {
        public int hero;
        public Transform body;
        public Transform[] limbs=Array.Empty<Transform>();
        [NonSerialized] public bool moving,channeling;
        [NonSerialized] public float hit;
        Vector3 basePosition;Quaternion[] rotations;float phase,attackElapsed=10,point=.3f,length=1;int sequence;
        void Awake(){Initialize();}
        public void Initialize(){if(body)basePosition=body.localPosition;rotations=new Quaternion[limbs.Length];for(int i=0;i<limbs.Length;i++)rotations[i]=limbs[i].localRotation;}
        public void Strike(){attackElapsed=0;point=.15f;length=.6f;hit=.6f;}
        public void Sync(Unit unit)
        {
            moving=unit.moving;channeling=unit.channel>0;
            if(unit.attackSequence!=sequence){sequence=unit.attackSequence;attackElapsed=0;point=unit.attackPoint;length=unit.attackLength;}
            if(unit.attackSequence>0&&!unit.attacking){attackElapsed=10;hit=0;}
        }
        static float Ease(float value){value=Mathf.Clamp01(value);return value*value*(3-2*value);}
        void Update()
        {
            if(!body)return;if(rotations==null||rotations.Length!=limbs.Length)Initialize();float dt=Time.deltaTime;phase+=dt*(moving?9:1.8f);hit=Mathf.Max(0,hit-dt);attackElapsed+=dt;
            float strike=0,twist=0;
            if(attackElapsed<point){float t=Ease(attackElapsed/Mathf.Max(.05f,point));strike=32*t;twist=-12*t;}
            else if(attackElapsed<point+.09f){float t=Ease((attackElapsed-point)/.09f);strike=Mathf.Lerp(32,-85,t);twist=Mathf.Lerp(-12,18,t);}
            else if(attackElapsed<length){float t=Ease((attackElapsed-point-.09f)/Mathf.Max(.1f,length-point-.09f));strike=Mathf.Lerp(-85,0,t);twist=Mathf.Lerp(18,0,t);}
            bool attacking=attackElapsed<length;
            body.localPosition=basePosition+Vector3.up*(hero==1?.13f*Mathf.Sin(phase):moving&&!attacking?.035f*Mathf.Abs(Mathf.Sin(phase)):.02f*Mathf.Sin(phase))+Vector3.forward*(attacking?Mathf.Max(0,-strike)*.002f:0);
            body.localRotation=Quaternion.Euler((moving&&hero==4?13:0)+(hero==0?strike*.1f:0),twist,hero==1?Mathf.Sin(phase)*2:0);
            for(int i=0;i<limbs.Length;i++)
            {
                if(!limbs[i])continue;bool leg=limbs[i].name.StartsWith("Leg");
                float offset=hero==0?(i%2)*Mathf.PI:(limbs[i].name.EndsWith("-1")?0:Mathf.PI)+(leg?Mathf.PI:0);
                float swing=moving&&!attacking?Mathf.Sin(phase+offset)*(hero==0?17:leg?22:14):Mathf.Sin(phase+i)*1.5f;
                float attack=!leg?strike:hero==0&&i%4==0?strike*.4f:0;
                limbs[i].localRotation=rotations[i]*Quaternion.Euler(swing+attack+(channeling&&!leg?-25:0),hero==0?swing*.45f:0,hero==0?swing*.3f:0);
            }
        }
    }
}
