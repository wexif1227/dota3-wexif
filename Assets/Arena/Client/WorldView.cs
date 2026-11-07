using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Rift.Core;
namespace Rift.Client
{
    public class WorldView : MonoBehaviour
    {
        public Camera Camera;
        public RenderTexture Portrait;
        public readonly Dictionary<int,GameObject> Models=new Dictionary<int,GameObject>();
        readonly Dictionary<int,Unit> current=new Dictionary<int,Unit>();
        readonly HashSet<int> effects=new HashSet<int>();
        readonly List<GameObject> transient=new List<GameObject>();
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        GameObject terrainRoot,preview;
        Camera portraitCamera;
        int selected=-1;
        Texture2D vision;
        float nextVision;
        readonly HashSet<int> seenUnits=new HashSet<int>();
        public static Vector3 Position(V2 p,float offset=0)=>new Vector3(p.x,Height(p.x,p.y)+offset,p.y);
        public static float Height(float x,float z){float river=Mathf.Abs(x+z)/1.414f;return river<3?-.6f:Mathf.SmoothStep(-.6f,.4f,Mathf.Clamp01((river-3)/3))+.22f*Mathf.Sin(x*.15f)*Mathf.Cos(z*.13f);}
        Material Mat(string hex,bool glow=false)
        {
            string key=hex+(glow?"g":"");if(materials.TryGetValue(key,out var m))return m;
            ColorUtility.TryParseHtmlString("#"+hex,out var color);m=new Material(Shader.Find("Standard")){color=color};m.SetFloat("_Glossiness",.2f);
            if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.7f);}materials[key]=m;return m;
        }
        public void Build()
        {
            foreach(var old in FindObjectsOfType<Camera>())Destroy(old.gameObject);
            foreach(var old in FindObjectsOfType<Light>())Destroy(old.gameObject);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.45f,.53f,.65f);RenderSettings.ambientEquatorColor=new Color(.28f,.35f,.37f);RenderSettings.ambientGroundColor=new Color(.12f,.15f,.2f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.1f,.16f,.2f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.004f;
            var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.88f,.72f);sun.intensity=1.35f;sun.transform.rotation=Quaternion.Euler(48,-34,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;QualitySettings.shadowDistance=100;
            Camera=new GameObject("Battle Camera").AddComponent<Camera>();Camera.clearFlags=CameraClearFlags.SolidColor;Camera.orthographic=true;Camera.orthographicSize=25;Camera.nearClipPlane=.1f;Camera.farClipPlane=250;Camera.backgroundColor=new Color(.035f,.065f,.085f);Camera.transform.rotation=Quaternion.Euler(53,0,0);Camera.transform.position=new Vector3(0,50,-39);Camera.gameObject.AddComponent<AudioListener>();Camera.cullingMask=~(1<<8);
            terrainRoot=new GameObject("The Shattered Vale / terrain");Ground();
            Shape(PrimitiveType.Cube,"Outer wilderness","263C37",new Vector3(0,-1.4f,0),new Vector3(400,1,400),terrainRoot.transform);
            var rimRandom=new System.Random(97);for(int i=0;i<180;i++){float angle=i*Mathf.PI*2/180;float distance=67+(float)rimRandom.NextDouble()*24;var p=new Vector3(Mathf.Cos(angle)*distance,-.9f,Mathf.Sin(angle)*distance);if(Mathf.Abs(p.x)<61&&Mathf.Abs(p.z)<61)continue;Cone(terrainRoot.transform,p,2+(float)rimRandom.NextDouble()*2,5+(float)rimRandom.NextDouble()*6,Mat(i%2==0?"344B46":"415750"),6);}
            var water=Shape(PrimitiveType.Cube,"River","2E7080",new Vector3(0,-.35f,0),new Vector3(6,.08f,169),terrainRoot.transform);water.transform.rotation=Quaternion.Euler(0,-45,0);
            foreach(var lane in BattleMap.Lanes)for(int i=1;i<lane.Length;i++)PathStrip(lane[i-1],lane[i],4.6f,"8E846B");
            var random=new System.Random(718);
            foreach(var p in BattleMap.Trees)
            {
                float s=.8f+(float)random.NextDouble()*.8f;var root=new GameObject("Tree");root.transform.SetParent(terrainRoot.transform);root.transform.position=Position(p);
                bool dire=p.x+p.y>0;Shape(PrimitiveType.Cylinder,"Trunk",dire?"57484B":"5E4F36",new Vector3(0,1.4f*s,0),new Vector3(.35f*s,1.4f*s,.35f*s),root.transform);
                for(int layer=0;layer<3;layer++)Cone(root.transform,new Vector3(0,(2+layer*.9f)*s,0),(2.2f-layer*.4f)*s,2.8f*s,Mat(dire?(layer%2==0?"425B68":"526772"):(layer%2==0?"386C57":"598766")),7);
            }
            foreach(var camp in BattleMap.Camps){Ring(terrainRoot.transform,Position(camp,.04f),5,.14f,Mat("9E9778"));for(int i=0;i<5;i++){float a=i*1.256f;Shape(PrimitiveType.Sphere,"Camp stone","68706E",Position(camp+new V2(Mathf.Cos(a)*5,Mathf.Sin(a)*5)),new Vector3(1,.7f,1.4f),terrainRoot.transform);}}
            for(int team=0;team<2;team++)
            {
                var p=BattleMap.Bases[team];Shape(PrimitiveType.Cylinder,"Base terrace",team==0?"7C9B90":"716D83",Position(p,-.3f),new Vector3(23,.35f,23),terrainRoot.transform);
                Ring(terrainRoot.transform,Position(p,.1f),10,.2f,Mat(team==0?"85D9BC":"DF9688",true));
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var q=p+new V2(Mathf.Cos(a)*11,Mathf.Sin(a)*11);Shape(PrimitiveType.Cube,"Ancient pillar",team==0?"A8B6A1":"777385",Position(q,2.2f),new Vector3(.7f,4.5f,.7f),terrainRoot.transform);Shape(PrimitiveType.Sphere,"Brazier",team==0?"91E8C4":"FF865C",Position(q,4.7f),Vector3.one*.8f,terrainRoot.transform,true);}
            }
            for(int i=0;i<34;i++){float x=-56+i*3.4f;Shape(PrimitiveType.Sphere,"River rock","5A6C74",Position(new V2(x,-x+5.5f),-.15f),new Vector3(2,.8f,1.4f),terrainRoot.transform);}
            Batch(terrainRoot);
            Portrait=new RenderTexture(640,720,16);portraitCamera=new GameObject("Hero Portrait Camera").AddComponent<Camera>();portraitCamera.cullingMask=1<<8;portraitCamera.targetTexture=Portrait;portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=new Color(.045f,.075f,.09f);portraitCamera.orthographic=true;portraitCamera.orthographicSize=4.1f;portraitCamera.transform.position=new Vector3(1000,3.4f,-10);portraitCamera.transform.LookAt(new Vector3(1000,2,0));
            SelectHero(0);
            vision=new Texture2D(96,96,TextureFormat.RGB24,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};Shader.SetGlobalTexture("_RiftVision",vision);Shader.SetGlobalFloat("_RiftFog",0);
        }
        void Ground()
        {
            int n=80;var vertices=new Vector3[(n+1)*(n+1)];var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];var triangles=new int[n*n*6];
            Color light=new Color(.25f,.37f,.28f),dark=new Color(.26f,.29f,.33f),sand=new Color(.34f,.39f,.36f);
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){float px=x*1.5f-60,pz=z*1.5f-60;int i=z*(n+1)+x;vertices[i]=new Vector3(px,Height(px,pz),pz);float noise=Mathf.PerlinNoise(px*.11f+100,pz*.11f+100);var c=px+pz<0?light:dark;colors[i]=Color.Lerp(c*.7f,c*1.25f,noise);if(Mathf.Abs(px+pz)<9)colors[i]=sand;uv[i]=new Vector2(x*.1f,z*.1f);}
            int k=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;triangles[k++]=a;triangles[k++]=a+n+1;triangles[k++]=a+1;triangles[k++]=a+1;triangles[k++]=a+n+1;triangles[k++]=a+n+2;}
            var mesh=new Mesh{name="Vale heightfield",vertices=vertices,triangles=triangles,colors=colors,uv=uv};mesh.RecalculateNormals();var obj=new GameObject("Ground",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));obj.transform.SetParent(terrainRoot.transform);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshCollider>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("Rift/Terrain"));
        }
        public GameObject Shape(PrimitiveType type,string name,string color,Vector3 pos,Vector3 scale,Transform parent,bool glow=false)
        {var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=Mat(color,glow);var c=o.GetComponent<Collider>();if(c)Destroy(c);return o;}
        void Cone(Transform parent,Vector3 pos,float radius,float height,Material material,int count=8)
        {
            var vertices=new List<Vector3>();var indices=new List<int>();for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;int k=vertices.Count;vertices.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));vertices.Add(Vector3.up*height);vertices.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));indices.Add(k);indices.Add(k+1);indices.Add(k+2);}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();var o=new GameObject("Faceted spire",typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        void Limb(Transform root,string color,Vector3 a,Vector3 b,float radius)
        {var o=Shape(PrimitiveType.Cylinder,"Limb",color,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),root);o.transform.up=b-a;}
        void Ring(Transform parent,Vector3 pos,float radius,float thickness,Material mat)
        {
            var go=new GameObject("Ring",typeof(LineRenderer));go.transform.SetParent(parent,false);var lr=go.GetComponent<LineRenderer>();lr.useWorldSpace=false;lr.positionCount=65;lr.widthMultiplier=thickness;lr.sharedMaterial=mat;for(int i=0;i<65;i++){float a=i*Mathf.PI/32;lr.SetPosition(i,pos+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
        }
        void PathStrip(V2 a,V2 b,float width,string color)
        {
            int n=Mathf.CeilToInt(V2.Distance(a,b)/2);V2 dir=(b-a).Normal,side=new V2(-dir.y,dir.x)*width*.5f;
            var v=new Vector3[(n+1)*2];var t=new int[n*6];for(int i=0;i<=n;i++){V2 p=a+(b-a)*(i/(float)n);v[i*2]=Position(p-side,.035f);v[i*2+1]=Position(p+side,.035f);if(i<n){int k=i*6,j=i*2;t[k]=j;t[k+1]=j+1;t[k+2]=j+2;t[k+3]=j+1;t[k+4]=j+3;t[k+5]=j+2;}}
            var mesh=new Mesh{vertices=v,triangles=t};mesh.RecalculateNormals();var obj=new GameObject("Stone lane",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(terrainRoot.transform);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=Mat(color);
        }
        void Batch(GameObject root)
        {
            var groups=root.GetComponentsInChildren<MeshRenderer>().GroupBy(r=>r.sharedMaterial).ToArray();
            foreach(var group in groups){var combines=new List<CombineInstance>();foreach(var r in group){var f=r.GetComponent<MeshFilter>();if(!f)continue;combines.Add(new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix});r.enabled=false;}
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines.ToArray());var go=new GameObject("Static terrain batch",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key;}
        }
        public void SelectHero(int hero)
        {
            if(selected==hero)return;selected=hero;if(preview)Destroy(preview);preview=HeroModel(hero,0);preview.transform.position=new Vector3(1000,0,0);preview.transform.rotation=Quaternion.Euler(0,155,0);SetLayer(preview,8);
        }
        static void SetLayer(GameObject g,int layer){g.layer=layer;foreach(Transform c in g.transform)SetLayer(c.gameObject,layer);}
        GameObject HeroModel(int hero,int team)
        {
            var g=new GameObject(Catalog.Heroes[hero].name);var root=g.transform;string c=Catalog.Heroes[hero].color;
            if(hero==0)
            {
                Shape(PrimitiveType.Sphere,"Carapace",c,new Vector3(0,1.4f,-.6f),new Vector3(2.4f,1.7f,2.9f),root);Shape(PrimitiveType.Sphere,"Head","493747",new Vector3(0,1.1f,1.1f),new Vector3(1.3f,1,1.2f),root);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++){Vector3 a=new Vector3(side*.6f,1.4f,.8f-i*.6f),b=new Vector3(side*(2.2f+Mathf.Sin(i)),1.8f,2-i*1.2f),d=new Vector3(b.x*1.2f,.15f,b.z+.3f);Limb(root,c,a,b,.15f);Limb(root,"4E3A55",b,d,.1f);}for(int i=-1;i<=1;i++)Shape(PrimitiveType.Sphere,"Eye","FE8670",new Vector3(i*.35f,1.3f,1.65f),Vector3.one*.22f,root,true);
            }
            else
            {
                float bulk=hero==3?1.8f:hero==4?1.5f:1;
                Shape(PrimitiveType.Capsule,"Torso",hero==1?"372B34":c,new Vector3(0,1.8f,0),new Vector3(1.3f*bulk,1.4f,bulk),root);
                Shape(PrimitiveType.Sphere,"Head",hero==3?"A2AD7C":c,new Vector3(0,3,0),new Vector3(.8f,.9f,.8f),root);
                for(int s=-1;s<=1;s+=2){Limb(root,"34424A",new Vector3(s*.4f,1.4f,0),new Vector3(s*.5f,.2f,.2f),.23f);Shape(PrimitiveType.Sphere,"Shoulder",c,new Vector3(s*.85f*bulk,2.4f,0),Vector3.one*.7f,root);Limb(root,c,new Vector3(s*.85f*bulk,2.4f,0),new Vector3(s*1.1f*bulk,1.3f,.4f),.25f);Shape(PrimitiveType.Sphere,"Eye",hero==1?"FF9A5A":"D7F7F5",new Vector3(s*.19f,3.1f,.36f),Vector3.one*.13f,root,true);}
                if(hero==1){for(int s=-1;s<=1;s+=2){Cone(root,new Vector3(s*.4f,3.2f,0),.3f,1.5f,Mat("E76C43"),5);Cone(root,new Vector3(s*.9f,2.6f,0),.4f,1,Mat("543C44"),5);}Shape(PrimitiveType.Sphere,"Soul core","FF723D",new Vector3(0,2,.55f),new Vector3(.55f,.75f,.2f),root,true);}
                if(hero==2){Cone(root,new Vector3(0,.4f,-.25f),1,2,Mat("594575"),5);for(int s=-1;s<=1;s+=2){var blade=Shape(PrimitiveType.Cube,"Psionic blade","D5ADF7",new Vector3(s*1.3f,1.5f,1),new Vector3(.12f,.25f,2),root,true);blade.transform.localRotation=Quaternion.Euler(0,s*-15,0);}Shape(PrimitiveType.Cube,"Mask","332D4B",new Vector3(0,3,.39f),new Vector3(.8f,.25f,.2f),root);}
                if(hero==3){Shape(PrimitiveType.Cube,"Apron","5D4A49",new Vector3(0,1.5f,.8f),new Vector3(1.6f,2,.15f),root);Shape(PrimitiveType.Cube,"Cleaver","A8A9A1",new Vector3(-2.1f,1.5f,.8f),new Vector3(.9f,1.4f,.15f),root);Ring(root,new Vector3(2,1.3f,.6f),.5f,.15f,Mat("B6B5A9"));for(int i=0;i<5;i++)Shape(PrimitiveType.Cube,"Stitch","C7B49A",new Vector3(-.5f+i*.25f,2.1f,.91f),new Vector3(.08f,.3f,.08f),root);}
                if(hero==4){for(int s=-1;s<=1;s+=2){Limb(root,"D3C9A6",new Vector3(s*.4f,3.2f,0),new Vector3(s*1.1f,3.7f,.3f),.2f);Cone(root,new Vector3(s*1.1f,3.7f,.3f),.18f,.65f,Mat("D3C9A6"),5);}Shape(PrimitiveType.Sphere,"Mace","7BC7DD",new Vector3(1.8f,1,.7f),Vector3.one*1.1f,root,true);}
                if(hero==5){Cone(root,new Vector3(0,.2f,0),1.1f,2.6f,Mat("456548"),7);Limb(root,"B9A679",new Vector3(1.4f,0,.4f),new Vector3(1.4f,4.1f,.4f),.12f);Shape(PrimitiveType.Sphere,"Staff gem","A4E9A1",new Vector3(1.4f,4.1f,.4f),Vector3.one*.65f,root,true);for(int s=-1;s<=1;s+=2){Limb(root,"B9A679",new Vector3(s*.3f,3.2f,0),new Vector3(s*.8f,4.2f,0),.1f);Limb(root,"B9A679",new Vector3(s*.6f,3.8f,0),new Vector3(s*1.1f,4,.3f),.08f);}}
            }
            return g;
        }
        GameObject Model(Unit u)
        {
            GameObject g=u.kind==Kind.Hero?HeroModel(u.hero,u.team):new GameObject(u.kind.ToString());var r=g.transform;string team=u.team==0?"7BC4AA":u.team==1?"D48275":"C9A56B";
            if(u.kind==Kind.Tower){Shape(PrimitiveType.Cylinder,"Foundation","707B7D",new Vector3(0,.4f,0),new Vector3(4,.4f,4),r);Shape(PrimitiveType.Cube,"Tower",team,new Vector3(0,2,0),new Vector3(2.3f,3.8f,2.3f),r);Cone(r,new Vector3(0,4,0),1.9f,2,Mat("3D515D"),4);Shape(PrimitiveType.Sphere,"Beacon",team,new Vector3(0,5.5f,0),Vector3.one*.9f,r,true);}
            if(u.kind==Kind.Ancient){Shape(PrimitiveType.Cylinder,"Ancient dais","6D7884",new Vector3(0,.5f,0),new Vector3(8,.5f,8),r);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Cone(r,new Vector3(Mathf.Cos(a)*2,1,Mathf.Sin(a)*2),1.3f,3+i%2,Mat(team,true),5);}Cone(r,Vector3.up,2,7,Mat(team,true),6);}
            if(u.kind==Kind.Barracks){Shape(PrimitiveType.Cube,"Barracks",team,new Vector3(0,1.3f,0),new Vector3(4,2.5f,3),r);Cone(r,new Vector3(0,2.5f,0),3,1.5f,Mat("4B5360"),4);}
            if(u.kind==Kind.Creep||u.kind==Kind.Neutral||u.kind==Kind.Summon){Shape(PrimitiveType.Capsule,"Body",u.kind==Kind.Neutral?"A48F73":team,new Vector3(0,.9f,0),new Vector3(.8f,1,1),r);Shape(PrimitiveType.Sphere,"Helmet","57606C",new Vector3(0,1.8f,0),Vector3.one*.65f,r);Limb(r,"C2C7BE",new Vector3(.6f,.4f,0),new Vector3(.6f,2.4f,.3f),.08f);if(u.kind==Kind.Summon&&u.hero==1)Cone(r,new Vector3(0,1,0),1,2,Mat("689467"),6);}
            if(u.kind==Kind.Roshan){Shape(PrimitiveType.Sphere,"Roshan","79748A",new Vector3(0,2,0),new Vector3(4,4,3),r);for(int s=-1;s<=1;s+=2){Limb(r,"60566A",new Vector3(s*1.5f,3,0),new Vector3(s*3,.5f,1),.6f);Cone(r,new Vector3(s,3.5f,0),.5f,2,Mat("C6B9A2"),5);}}
            if(u.kind==Kind.Courier){Shape(PrimitiveType.Sphere,"Courier",team,new Vector3(0,2,0),new Vector3(.8f,.7f,1.3f),r);for(int s=-1;s<=1;s+=2)Shape(PrimitiveType.Cube,"Wing","D3C8A0",new Vector3(s*.9f,2.1f,0),new Vector3(1.4f,.12f,.8f),r);Shape(PrimitiveType.Cube,"Delivery chest","A48557",new Vector3(0,1.5f,0),Vector3.one*.6f,r);}
            if(u.kind==Kind.Ward){Limb(r,"A58D64",Vector3.zero,Vector3.up*2.4f,.15f);Shape(PrimitiveType.Sphere,"Eye",u.hero==1?"87B8EE":"E9C877",Vector3.up*2.4f,new Vector3(1,.7f,.7f),r,true);}
            if(u.kind==Kind.Web){Ring(r,new Vector3(0,.06f,0),8,.06f,Mat("BDB6D5"));for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Limb(r,"BDB6D5",new Vector3(0,.06f,0),new Vector3(Mathf.Cos(a)*8,.06f,Mathf.Sin(a)*8),.035f);}}
            if(u.kind==Kind.Trap||u.kind==Kind.Rune){Ring(r,new Vector3(0,.2f,0),1.2f,.12f,Mat(u.kind==Kind.Trap?"D29DEB":"EBCF72",true));Cone(r,Vector3.up*.3f,.7f,1.3f,Mat("B4A8F5",true),4);}
            if(u.kind!=Kind.Web&&u.kind!=Kind.Rune)Ring(r,new Vector3(0,.08f,0),u.kind==Kind.Hero?1.5f:1,.09f,Mat(team,true));
            return g;
        }
        public void Sync(Snapshot snapshot)
        {
            Shader.SetGlobalFloat("_RiftFog",1);
            if(Time.time>=nextVision)
            {
                nextVision=Time.time+.25f;var player=snapshot.players.Find(p=>p.id==snapshot.you);int team=player!=null?player.team:0;
                var allies=snapshot.units.Where(u=>u.team==team&&!u.dead&&u.kind!=Kind.Trap&&u.kind!=Kind.Web).ToArray();
                var pixels=new Color[96*96];for(int y=0;y<96;y++)for(int x=0;x<96;x++){V2 p=new V2(x/95f*120-60,y/95f*120-60);float vis=0;foreach(var u in allies){float range=u.kind==Kind.Ward?17:u.kind==Kind.Hero?20:12;vis=Mathf.Max(vis,Mathf.Clamp01((range-V2.Distance(p,u.pos))/3));}pixels[y*96+x]=new Color(vis,vis,vis);}vision.SetPixels(pixels);vision.Apply();
            }
            current.Clear();foreach(var u in snapshot.units){current[u.id]=u;if(!Models.ContainsKey(u.id)){var g=Model(u);g.transform.position=Position(u.pos);Models[u.id]=g;}Models[u.id].SetActive(!u.dead);}
            foreach(var pair in Models.ToArray())if(!current.ContainsKey(pair.Key)){Destroy(pair.Value);Models.Remove(pair.Key);}
            foreach(var e in snapshot.effects)if(effects.Add(e.id))ShowEffect(e);
            if(effects.Count>4000)effects.Clear();
        }
        void ShowEffect(Effect e)
        {
            var g=new GameObject("FX "+e.type);string c=e.type=="raze"||e.type=="requiem"?"FF784A":e.type=="ice"?"84DFFF":e.type=="nature"||e.type=="sprout"?"A8E3A2":e.type=="hook"?"C8BDA7":e.team==0?"8BE4D1":"F1A08C";
            if(e.type=="hook"||e.type=="bolt"||e.type=="nature"||e.type=="dagon"||e.type=="poison"){var lr=g.AddComponent<LineRenderer>();lr.positionCount=2;lr.SetPosition(0,Position(e.a,1.6f));lr.SetPosition(1,Position(e.b,1.3f));lr.widthMultiplier=e.type=="hook"?.15f:.09f;lr.sharedMaterial=Mat(c,true);}
            else {g.transform.position=Position(e.a,.15f);Ring(g.transform,Vector3.zero,e.radius,.22f,Mat(c,true));if(e.type=="raze"||e.type=="sprout")for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Cone(g.transform,new Vector3(Mathf.Cos(a)*e.radius*.6f,0,Mathf.Sin(a)*e.radius*.6f),.5f,2.5f,Mat(c,true),5);}}
            Destroy(g,.45f);
        }
        void Update()
        {
            foreach(var pair in current)
            {
                if(!Models.TryGetValue(pair.Key,out var model)||!model.activeSelf)continue;var u=pair.Value;var pos=Position(u.pos);if(u.kind==Kind.Hero||u.kind==Kind.Creep)pos.y+=Mathf.Sin(UnityEngine.Time.time*7+u.id)*(u.moving?.09f:.025f);
                model.transform.position=Vector3.Lerp(model.transform.position,pos,1-Mathf.Exp(-UnityEngine.Time.deltaTime*15));
                if(u.facing.Length>.01f)model.transform.rotation=Quaternion.Slerp(model.transform.rotation,Quaternion.LookRotation(new Vector3(u.facing.x,0,u.facing.y)),UnityEngine.Time.deltaTime*12);
            }
            if(preview)preview.transform.rotation=Quaternion.Euler(0,155+Mathf.Sin(UnityEngine.Time.time*.3f)*12,0);
        }
        public void ClearUnits(){foreach(var g in Models.Values)Destroy(g);Models.Clear();current.Clear();Shader.SetGlobalFloat("_RiftFog",0);}
    }
}
