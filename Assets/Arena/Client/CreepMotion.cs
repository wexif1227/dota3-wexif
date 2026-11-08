using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public sealed class CreepMotion:MonoBehaviour
    {
        public Transform leftLeg,rightLeg,weaponArm;
        bool moving;int sequence;float impact=-10,point=.3f;
        public void Sync(Unit unit)
        {
            moving=unit.moving;
            if(sequence!=unit.attackSequence){sequence=unit.attackSequence;point=Mathf.Max(.15f,unit.attackPoint);impact=Time.time+point;}
            if(!unit.attacking)impact=-10;
        }
        void LateUpdate()
        {
            float stride=moving?Mathf.Sin(Time.time*10+transform.GetInstanceID()%11)*28:0;
            if(leftLeg)leftLeg.localRotation=Quaternion.Euler(stride,0,0);if(rightLeg)rightLeg.localRotation=Quaternion.Euler(-stride,0,0);
            float time=Time.time-impact;
            float strike=time<0?Mathf.Lerp(0,48,1+time/point):time<.22f?Mathf.Lerp(-75,0,time/.22f):0;
            if(weaponArm)weaponArm.localRotation=Quaternion.Euler(strike-stride*.5f,0,0);
        }
    }
}
