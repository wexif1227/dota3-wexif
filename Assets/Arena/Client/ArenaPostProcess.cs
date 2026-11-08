using UnityEngine;
namespace Rift.Client
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaPostProcess:MonoBehaviour
    {
        Material material;
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(!material){var shader=Shader.Find("Rift/Atmosphere");if(shader&&shader.isSupported)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};}
            if(!material){Graphics.Blit(source,destination);return;}
            var a=RenderTexture.GetTemporary(Mathf.Max(1,source.width/4),Mathf.Max(1,source.height/4),0,source.format);
            var b=RenderTexture.GetTemporary(a.width,a.height,0,source.format);a.filterMode=b.filterMode=FilterMode.Bilinear;
            Graphics.Blit(source,a,material,0);Graphics.Blit(a,b,material,1);Graphics.Blit(b,a,material,1);
            material.SetTexture("_Bloom",a);Graphics.Blit(source,destination,material,2);RenderTexture.ReleaseTemporary(a);RenderTexture.ReleaseTemporary(b);
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
}
