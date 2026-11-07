using UnityEngine;
namespace Rift.Client
{
    public sealed class CommandFeedback:MonoBehaviour
    {
        float age;
        void Update(){age+=Time.deltaTime;transform.localScale=Vector3.one*Mathf.Lerp(1.5f,.45f,age/.5f);if(age>.5f)Destroy(gameObject);}
    }
}
