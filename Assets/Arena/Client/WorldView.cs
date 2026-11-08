using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Rift.Core;
namespace Rift.Client
{
    public partial class WorldView : MonoBehaviour
    {
        public Camera Camera;
        public RenderTexture Portrait;
        public Action<Effect> OnEffect;
        public readonly Dictionary<int,GameObject> Models=new Dictionary<int,GameObject>();
        readonly Dictionary<int,Unit> current=new Dictionary<int,Unit>();
        readonly HashSet<int> effects=new HashSet<int>();
        readonly List<GameObject> transient=new List<GameObject>();
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        GameObject terrainRoot,preview;
        GameObject aim;
        LineRenderer aimLine,aimCircle,selectionRing,hoverRing;
        int selectedUnitId,hoverUnitId,viewTeam;
        Camera portraitCamera;
        int selected=-1;
        Texture2D vision;
        float nextVision;
        readonly HashSet<int> seenUnits=new HashSet<int>();
        public static Vector3 Position(V2 p,float offset=0)=>new Vector3(p.x,Height(p.x,p.y)+offset,p.y);
        public static float Height(float x,float z)=>BattleMap.Height(x,z);
        Material Mat(string hex,bool glow=false)
        {
            string key=hex+(glow?"g":"");if(materials.TryGetValue(key,out var m))return m;
            ColorUtility.TryParseHtmlString("#"+hex,out var color);m=new Material(Shader.Find("Rift/Lit")){color=color};
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
            BuildArchitecture();
            Batch(terrainRoot);
            Portrait=new RenderTexture(768,864,24);portraitCamera=new GameObject("Hero Portrait Camera").AddComponent<Camera>();portraitCamera.cullingMask=1<<8;portraitCamera.targetTexture=Portrait;portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=new Color(.045f,.065f,.08f);portraitCamera.orthographic=true;portraitCamera.orthographicSize=3.15f;portraitCamera.transform.position=new Vector3(1000,3.8f,-10);portraitCamera.transform.LookAt(new Vector3(1000,2.35f,0));
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
        void PathStrip(V2 a,V2 b,float width,string color,float lift=.04f)
        {
            int n=Mathf.CeilToInt(V2.Distance(a,b)/2);V2 dir=(b-a).Normal,side=new V2(-dir.y,dir.x)*width*.5f;
            var v=new Vector3[(n+1)*2];var t=new int[n*6];for(int i=0;i<=n;i++){V2 p=a+(b-a)*(i/(float)n);v[i*2]=Position(p-side,lift);v[i*2+1]=Position(p+side,lift);if(i<n){int k=i*6,j=i*2;t[k]=j;t[k+1]=j+1;t[k+2]=j+2;t[k+3]=j+1;t[k+4]=j+3;t[k+5]=j+2;}}
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
            // The spider's wide leg span needs more room than the upright heroes.
            portraitCamera.orthographicSize=hero==0?4.15f:3.15f;
        }
        public void ShowAim(V2 from,V2 to,bool visible)
        {
            if(!aim){aim=new GameObject("Spell targeting");aimLine=aim.AddComponent<LineRenderer>();aimLine.positionCount=2;aimLine.widthMultiplier=.08f;aimLine.sharedMaterial=Mat("A4E8D0",true);var circle=new GameObject("Target");circle.transform.SetParent(aim.transform);aimCircle=circle.AddComponent<LineRenderer>();aimCircle.positionCount=49;aimCircle.widthMultiplier=.1f;aimCircle.sharedMaterial=Mat("A4E8D0",true);}
            aim.SetActive(visible);if(!visible)return;aimLine.SetPosition(0,Position(from,.2f));aimLine.SetPosition(1,Position(to,.2f));for(int i=0;i<49;i++){float angle=i*Mathf.PI/24;aimCircle.SetPosition(i,Position(to+new V2(Mathf.Cos(angle),Mathf.Sin(angle))*2,.2f));}
        }
        public Unit PickUnit(Vector2 screen)
        {
            var ray=Camera.ScreenPointToRay(screen);Unit best=null;float bestScore=float.MaxValue;
            foreach(var pair in current)
            {
                var unit=pair.Value;if(unit.dead||unit.kind==Kind.Web||unit.kind==Kind.Rune||!Models.TryGetValue(unit.id,out var model)||!model.activeSelf)continue;
                var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds(model.transform.position+Vector3.up,Vector3.one);
                bool found=false;foreach(var renderer in renderers){if(renderer is LineRenderer||renderer is TrailRenderer||renderer is ParticleSystemRenderer||!renderer.enabled)continue;if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
                if(!found)continue;
                bounds.Expand(.15f);
                if(bounds.IntersectRay(ray,out float distance))
                {
                    // In a melee cluster bounding boxes overlap. Prefer the body under the cursor,
                    // rather than the front edge of a neighbour's weapon or shoulder box.
                    var body=renderers.FirstOrDefault(r=>r is SkinnedMeshRenderer);var anchor=Camera.WorldToScreenPoint(body?body.bounds.center:bounds.center);
                    float score=((Vector2)anchor-screen).sqrMagnitude+distance*.01f;
                    if(anchor.z>0&&score<bestScore){bestScore=score;best=unit;}
                }
                // Health bars remain clickable even when the cursor is just above the mesh.
                var bar=Camera.WorldToScreenPoint(Position(unit.pos,unit.kind==Kind.Hero?4.8f:unit.kind==Kind.Tower?7:3));
                float width=(unit.kind==Kind.Hero?84:unit.kind==Kind.Creep?28:unit.kind==Kind.Tower?64:36)*Screen.width/1600f;
                if(bar.z>0&&Mathf.Abs(bar.x-screen.x)<width*.5f&&Mathf.Abs(bar.y-screen.y)<12){float score=-10000+Mathf.Abs(bar.x-screen.x);if(score<bestScore){bestScore=score;best=unit;}}
            }
            return best;
        }
        public void SetSelection(int selectedId,int hoverId,int team){selectedUnitId=selectedId;hoverUnitId=hoverId;viewTeam=team;}
        LineRenderer SelectionLine(string title)
        {
            var line=new GameObject(title).AddComponent<LineRenderer>();line.loop=true;line.useWorldSpace=true;line.positionCount=64;line.widthMultiplier=.12f;line.numCornerVertices=2;line.sharedMaterial=Mat("80F4A4",true);return line;
        }
        void UpdateSelection(ref LineRenderer line,int id,bool hover)
        {
            if(!line)line=SelectionLine(hover?"Hovered unit":"Selected unit");
            bool visible=current.TryGetValue(id,out var unit)&&!unit.dead&&Models.TryGetValue(id,out var model)&&model.activeSelf;
            line.gameObject.SetActive(visible);if(!visible)return;
            var center=Models[id].transform.position;float radius=unit.kind==Kind.Hero?(unit.hero==0?2.35f:1.55f):unit.kind==Kind.Tower?2.2f:1;
            line.widthMultiplier=hover?.075f:.16f;line.sharedMaterial=Mat(unit.team==viewTeam?(hover?"B2F7C7":"53EB8B"):(hover?"FF6F5F":"EDAA50"),true);
            for(int i=0;i<64;i++){float a=i*Mathf.PI/32;float x=center.x+Mathf.Cos(a)*radius,z=center.z+Mathf.Sin(a)*radius;line.SetPosition(i,new Vector3(x,Height(x,z)+.12f,z));}
        }
        public void CommandMarker(V2 point,bool attack)
        {
            var g=new GameObject(attack?"Attack order":"Move order");g.transform.position=Position(point,.18f);Ring(g.transform,Vector3.zero,.65f,.1f,Mat(attack?"FF7663":"62F293",true));g.AddComponent<CommandFeedback>();
        }
        static void SetLayer(GameObject g,int layer){g.layer=layer;foreach(Transform c in g.transform)SetLayer(c.gameObject,layer);}
        GameObject HeroModel(int hero,int team)
        {
            var prefab=Resources.Load<GameObject>("DotaImported/Hero"+hero);if(!prefab)prefab=Resources.Load<GameObject>("Heroes/Hero"+hero);return prefab?Instantiate(prefab):HeroArt.Create(hero);
        }
        GameObject Model(Unit u)
        {
            GameObject g=u.kind==Kind.Hero?HeroModel(u.hero,u.team):new GameObject(u.kind.ToString());var r=g.transform;string team=u.team==0?"7BC4AA":u.team==1?"D48275":"C9A56B";
            if(u.kind==Kind.Tower){Shape(PrimitiveType.Cylinder,"Foundation","707B7D",new Vector3(0,.4f,0),new Vector3(4,.4f,4),r);Shape(PrimitiveType.Cube,"Tower",team,new Vector3(0,2,0),new Vector3(2.3f,3.8f,2.3f),r);Cone(r,new Vector3(0,4,0),1.9f,2,Mat("3D515D"),4);Shape(PrimitiveType.Sphere,"Beacon",team,new Vector3(0,5.5f,0),Vector3.one*.9f,r,true);}
            if(u.kind==Kind.Ancient){Shape(PrimitiveType.Cylinder,"Ancient dais","6D7884",new Vector3(0,.5f,0),new Vector3(8,.5f,8),r);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Cone(r,new Vector3(Mathf.Cos(a)*2,1,Mathf.Sin(a)*2),1.3f,3+i%2,Mat(team,true),5);}Cone(r,Vector3.up,2,7,Mat(team,true),6);}
            if(u.kind==Kind.Barracks){Shape(PrimitiveType.Cube,"Barracks",team,new Vector3(0,1.3f,0),new Vector3(4,2.5f,3),r);Cone(r,new Vector3(0,2.5f,0),3,1.5f,Mat("4B5360"),4);}
            if(u.kind==Kind.Creep||u.kind==Kind.Neutral||u.kind==Kind.Summon)BuildSoldier(g,u,team);
            if(u.kind==Kind.Roshan){Shape(PrimitiveType.Sphere,"Roshan","79748A",new Vector3(0,2,0),new Vector3(4,4,3),r);for(int s=-1;s<=1;s+=2){Limb(r,"60566A",new Vector3(s*1.5f,3,0),new Vector3(s*3,.5f,1),.6f);Cone(r,new Vector3(s,3.5f,0),.5f,2,Mat("C6B9A2"),5);}}
            if(u.kind==Kind.Courier){Shape(PrimitiveType.Sphere,"Courier",team,new Vector3(0,2,0),new Vector3(.8f,.7f,1.3f),r);for(int s=-1;s<=1;s+=2)Shape(PrimitiveType.Cube,"Wing","D3C8A0",new Vector3(s*.9f,2.1f,0),new Vector3(1.4f,.12f,.8f),r);Shape(PrimitiveType.Cube,"Delivery chest","A48557",new Vector3(0,1.5f,0),Vector3.one*.6f,r);}
            if(u.kind==Kind.Ward){Limb(r,"A58D64",Vector3.zero,Vector3.up*2.4f,.15f);Shape(PrimitiveType.Sphere,"Eye",u.hero==1?"87B8EE":"E9C877",Vector3.up*2.4f,new Vector3(1,.7f,.7f),r,true);}
            if(u.kind==Kind.Web){Ring(r,new Vector3(0,.06f,0),8,.06f,Mat("BDB6D5"));for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Limb(r,"BDB6D5",new Vector3(0,.06f,0),new Vector3(Mathf.Cos(a)*8,.06f,Mathf.Sin(a)*8),.035f);}}
            if(u.kind==Kind.Trap||u.kind==Kind.Rune){Ring(r,new Vector3(0,.2f,0),1.2f,.12f,Mat(u.kind==Kind.Trap?"D29DEB":"EBCF72",true));Cone(r,Vector3.up*.3f,.7f,1.3f,Mat("B4A8F5",true),4);}
            if(u.kind!=Kind.Web&&u.kind!=Kind.Rune)Ring(r,new Vector3(0,.08f,0),u.kind==Kind.Hero?1.15f:u.kind==Kind.Creep?.68f:1,.09f,Mat(team,true));
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
            current.Clear();foreach(var u in snapshot.units){current[u.id]=u;if(!Models.ContainsKey(u.id)){var g=Model(u);g.transform.position=Position(u.pos);Models[u.id]=g;}var model=Models[u.id];var imported=model.GetComponent<ImportedHeroMotion>();model.SetActive(!u.dead||imported&&imported.VisibleWhileDead);}
            foreach(var pair in Models.ToArray())if(!current.ContainsKey(pair.Key)){Destroy(pair.Value);Models.Remove(pair.Key);}
            foreach(var e in snapshot.effects)if(effects.Add(e.id))ShowEffect(e);
            if(effects.Count>4000)effects.Clear();
        }
        void ShowEffect(Effect e)
        {
            OnEffect?.Invoke(e);
            // Attack poses are driven by authoritative attack starts, before damage is applied.
            if(e.source==0&&e.type!="death"&&e.type!="shield")foreach(var pair in current)if(pair.Value.kind==Kind.Hero&&V2.Distance(pair.Value.pos,e.a)<1.6f&&Models.TryGetValue(pair.Key,out var model))model.GetComponent<HeroMotion>()?.Strike();
            var g=new GameObject("FX "+e.type);string c=e.type=="raze"||e.type=="requiem"?"FF784A":e.type=="ice"?"84DFFF":e.type=="nature"||e.type=="sprout"?"A8E3A2":e.type=="hook"?"C8BDA7":e.team==0?"8BE4D1":"F1A08C";
            if(e.type=="bolt")
            {
                var flight=g.AddComponent<AttackProjectile>();flight.start=Position(e.a,2.1f);flight.end=Position(e.b,1.8f);flight.duration=e.duration>0?e.duration:.3f;
                if(Models.TryGetValue(e.target,out var victim))flight.target=victim.transform;
                string color=current.TryGetValue(e.source,out var caster)?caster.hero==1?"FF7F35":caster.hero==2?"EDACFF":caster.hero==5?"91F875":c:c;
                Shape(PrimitiveType.Sphere,"Attack projectile",color,Vector3.zero,Vector3.one*.28f,g.transform,true);
                var trail=g.AddComponent<LineRenderer>();trail.positionCount=2;trail.widthMultiplier=.11f;trail.sharedMaterial=Mat(color,true);flight.trail=trail;g.transform.position=flight.start;return;
            }
            if(e.type=="hook"||e.type=="bolt"||e.type=="nature"||e.type=="dagon"||e.type=="poison"){var lr=g.AddComponent<LineRenderer>();lr.positionCount=2;lr.SetPosition(0,Position(e.a,1.6f));lr.SetPosition(1,Position(e.b,1.3f));lr.widthMultiplier=e.type=="hook"?.15f:.09f;lr.sharedMaterial=Mat(c,true);}
            else {g.transform.position=Position(e.a,.15f);Ring(g.transform,Vector3.zero,e.radius,.22f,Mat(c,true));if(e.type=="raze"||e.type=="sprout")for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Cone(g.transform,new Vector3(Mathf.Cos(a)*e.radius*.6f,0,Mathf.Sin(a)*e.radius*.6f),.5f,2.5f,Mat(c,true),5);}}
            Destroy(g,.45f);
        }
        void Update()
        {
            foreach(var pair in current)
            {
                if(!Models.TryGetValue(pair.Key,out var model)||!model.activeSelf)continue;var u=pair.Value;var pos=Position(u.pos);if(u.kind==Kind.Creep)pos.y+=Mathf.Sin(UnityEngine.Time.time*7+u.id)*(u.moving?.09f:.025f);
                model.transform.position=Vector3.Lerp(model.transform.position,pos,1-Mathf.Exp(-UnityEngine.Time.deltaTime*15));
                var soldier=model.GetComponent<CreepMotion>();if(soldier)soldier.Sync(u);
                var motion=model.GetComponent<HeroMotion>();if(motion)motion.Sync(u);
                var imported=model.GetComponent<ImportedHeroMotion>();if(imported)imported.Sync(u);
                var cosmetics=model.GetComponent<CosmeticEffects>();if(cosmetics)cosmetics.SetAlive(!u.dead);
                if(u.facing.Length>.01f)model.transform.rotation=Quaternion.Slerp(model.transform.rotation,Quaternion.LookRotation(new Vector3(u.facing.x,0,u.facing.y)),UnityEngine.Time.deltaTime*12);
            }
            if(preview)preview.transform.rotation=Quaternion.Euler(0,155+Mathf.Sin(UnityEngine.Time.time*.3f)*12,0);
            UpdateSelection(ref selectionRing,selectedUnitId,false);UpdateSelection(ref hoverRing,hoverUnitId==selectedUnitId?0:hoverUnitId,true);
        }
        public void ClearUnits(){foreach(var g in Models.Values)Destroy(g);Models.Clear();current.Clear();effects.Clear();selectedUnitId=hoverUnitId=0;if(selectionRing)selectionRing.gameObject.SetActive(false);if(hoverRing)hoverRing.gameObject.SetActive(false);if(aim)aim.SetActive(false);Shader.SetGlobalFloat("_RiftFog",0);}
    }
}
