using UnityEngine;
namespace Rift.Client
{
    public sealed class AttackProjectile:MonoBehaviour
    {
        public Vector3 start,end;
        public float duration=.3f;
        public Transform target;
        public LineRenderer trail;
        float age;
        void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/Mathf.Max(.08f,duration));
            if(target)end=target.position+Vector3.up*1.8f;
            Vector3 pos=Vector3.Lerp(start,end,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.35f;
            transform.position=pos;
            if(trail){trail.SetPosition(0,pos);trail.SetPosition(1,Vector3.Lerp(start,end,Mathf.Max(0,t-.15f))+Vector3.up*Mathf.Sin(Mathf.Max(0,t-.15f)*Mathf.PI)*.35f);}
            if(t>=1)Destroy(gameObject);
        }
    }
}
