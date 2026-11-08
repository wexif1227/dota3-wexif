using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public partial class WorldView
    {
        void BuildArchitecture()
        {
            var water=Shape(PrimitiveType.Cube,"Shallow river","356D78",new Vector3(0,-.74f,0),new Vector3(6.8f,.06f,169),terrainRoot.transform);water.transform.rotation=Quaternion.Euler(0,-45,0);
            water.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Rift/River"));
            foreach(var route in BattleMap.JungleRoutes)for(int i=1;i<route.Length;i++){PathStrip(route[i-1],route[i],3.4f,"555B49",.045f);PathStrip(route[i-1],route[i],2.35f,"77745D",.075f);}
            foreach(var lane in BattleMap.Lanes)
            {
                for(int i=1;i<lane.Length;i++)
                {
                    V2 a=lane[i-1],b=lane[i],dir=(b-a).Normal,side=new V2(-dir.y,dir.x);
                    PathStrip(a,b,7.2f,"4E554B",.05f);PathStrip(a,b,5.4f,"817E6B",.09f);PathStrip(a,b,3.8f,"969383",.13f);
                    int count=Mathf.CeilToInt(V2.Distance(a,b)/2.8f);
                    for(int k=0;k<count;k++)for(int s=-1;s<=1;s+=2)
                    {
                        V2 p=a+(b-a)*((k+.5f)/count)+side*(2.45f+s*.1f)*s;
                        var stone=Shape(PrimitiveType.Cube,"Lane curb",k%3==0?"6D7774":"A1A292",Position(p,.1f),new Vector3(.45f,.18f,1.1f),terrainRoot.transform);stone.transform.rotation=Quaternion.LookRotation(new Vector3(dir.x,0,dir.y));
                    }
                }
            }
            BuildBanks();BuildForest();BuildCamps();BuildBases();BuildLandmarks();
        }
        void BuildBanks()
        {
            for(int i=0;i<69;i++)for(int side=-1;side<=1;side+=2)
            {
                float t=-56+i*1.65f;var p=new V2(t,-t)+new V2(.707f,.707f)*(side*4.65f);if(BattleMap.Ramp(p)||BattleMap.Clearing(p))continue;
                var wall=Shape(PrimitiveType.Cube,"River escarpment",side<0?"646E68":"626571",new Vector3(p.x,-.1f,p.y),new Vector3(2.6f,1.65f,2.4f),terrainRoot.transform);wall.transform.rotation=Quaternion.Euler(0,-45+(i%3-1)*7,0);
                if(i%2==0)Shape(PrimitiveType.Sphere,"Bank boulder","79857C",new Vector3(p.x,.65f,p.y),new Vector3(1.8f,.75f,1.3f),terrainRoot.transform);
            }
            foreach(var crossing in BattleMap.Crossings)
            {
                V2 normal=new V2(.707f,.707f),side=new V2(-.707f,.707f);
                // Stone stairs follow the same continuous height function used by units.
                for(int k=-4;k<=4;k++)
                {
                    V2 p=crossing+normal*(k*1.35f);var tread=Shape(PrimitiveType.Cube,"River stair","90988D",Position(p,.055f),new Vector3(6.8f,.13f,.85f),terrainRoot.transform);tread.transform.rotation=Quaternion.Euler(0,45,0);
                }
                foreach(int bank in new[]{-1,1})foreach(int edge in new[]{-1,1})
                {
                    var p=crossing+normal*(bank*7)+side*(edge*4.1f);var root=new GameObject("Crossing marker").transform;root.SetParent(terrainRoot.transform);root.position=Position(p);
                    Shape(PrimitiveType.Cube,"Stone plinth","596862",new Vector3(0,.3f,0),new Vector3(.95f,.6f,.95f),root);
                    Shape(PrimitiveType.Cylinder,"Waystone","87958A",new Vector3(0,1.2f,0),new Vector3(.45f,.9f,.45f),root);
                    Shape(PrimitiveType.Sphere,"Crossing lantern",bank<0?"D6B875":"86BAD0",new Vector3(0,2.15f,0),Vector3.one*.42f,root,true);
                }
            }
        }
        void BuildForest()
        {
            var random=new System.Random(718);
            foreach(var point in BattleMap.Trees)
            {
                bool dire=point.x+point.y>0;float size=.8f+(float)random.NextDouble()*.65f;
                var prefab=Resources.Load<GameObject>("DotaImported/World/Tree"+(dire?1:0)+(random.Next(2)==0?"A":"B"));
                if(prefab){var imported=Instantiate(prefab,terrainRoot.transform);imported.transform.position=Position(point);imported.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);imported.transform.localScale=Vector3.one*size;continue;}
                var tree=new GameObject(dire?"Dire grove":"Radiant grove").transform;tree.SetParent(terrainRoot.transform);tree.position=Position(point);tree.localScale=Vector3.one*size;
                Shape(PrimitiveType.Cylinder,"Bark",dire?"524B50":"64543F",new Vector3(0,1.5f,0),new Vector3(.3f,1.5f,.3f),tree);
                for(int j=0;j<3;j++)
                {
                    float a=j*Mathf.PI*2/3+(float)random.NextDouble();var branch=new Vector3(Mathf.Cos(a)*.85f,2.5f+j*.4f,Mathf.Sin(a)*.85f);
                    Limb(tree,dire?"68606A":"716349",new Vector3(0,1.7f,0),branch,.13f);
                    Crown(tree,branch,dire?j%2==0?"475F66":"57646D":j%2==0?"3F705B":"668968",1.35f-j*.12f,1.8f);
                }
                if(random.NextDouble()<.24)Shape(PrimitiveType.Sphere,"Mossy root","58694C",new Vector3(.4f,.12f,.1f),new Vector3(1.1f,.3f,.9f),tree);
            }
        }
        void Crown(Transform parent,Vector3 position,string color,float radius,float height)
        {
            const int count=7;var v=new Vector3[count*3+2];var t=new int[count*18];int at=0;
            for(int ring=0;ring<3;ring++)for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count+ring*.18f;float r=radius*(ring==0?.65f:ring==1?1:.55f);v[ring*count+i]=new Vector3(Mathf.Cos(angle)*r,height*ring*.36f,Mathf.Sin(angle)*r);}
            v[count*3]=Vector3.down*.15f;v[count*3+1]=Vector3.up*height;
            for(int i=0;i<count;i++){int n=(i+1)%count;t[at++]=count*3;t[at++]=n;t[at++]=i;for(int ring=0;ring<2;ring++){int a=ring*count+i,b=ring*count+n,c=(ring+1)*count+i,d=(ring+1)*count+n;t[at++]=a;t[at++]=b;t[at++]=c;t[at++]=c;t[at++]=b;t[at++]=d;}t[at++]=count*2+i;t[at++]=count*2+n;t[at++]=count*3+1;}
            for(int i=0;i<t.Length;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
            var mesh=new Mesh{vertices=v,triangles=t};mesh.RecalculateNormals();var go=new GameObject("Irregular canopy",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Mat(color);
        }
        void BuildCamps()
        {
            for(int camp=0;camp<BattleMap.Camps.Length;camp++)
            {
                V2 p=BattleMap.Camps[camp];var root=new GameObject("Jungle camp "+(camp+1)).transform;root.SetParent(terrainRoot.transform);root.position=Position(p);
                Shape(PrimitiveType.Cylinder,"Camp clearing","69664D",new Vector3(0,-.04f,0),new Vector3(9,.08f,9),root);
                for(int k=0;k<6;k++){float angle=k*Mathf.PI/3;var offset=new Vector3(Mathf.Cos(angle)*4.7f,0,Mathf.Sin(angle)*4.7f);Shape(PrimitiveType.Sphere,"Camp stones","707870",offset+Vector3.up*.2f,new Vector3(1.1f,.65f,.85f),root);}
                Shape(PrimitiveType.Cube,"Supply chest","69513C",new Vector3(-3,.5f,2.4f),new Vector3(1.5f,.95f,1.1f),root);
                Shape(PrimitiveType.Cube,"Chest band","B59B65",new Vector3(-3,1,2.4f),new Vector3(1.55f,.12f,1.15f),root);
                Limb(root,"625343",new Vector3(2.8f,0,2.8f),new Vector3(2.8f,3,2.8f),.12f);
                Shape(PrimitiveType.Cube,"Camp banner",p.x+p.y<0?"769583":"927A80",new Vector3(2.8f,2.4f,2.8f),new Vector3(1.1f,.8f,.08f),root);
            }
        }
        void BuildBases()
        {
            for(int team=0;team<2;team++)
            {
                var p=BattleMap.Bases[team];string stone=team==0?"98A798":"777D8A",cloth=team==0?"679A87":"9B676F";float sign=team==0?1:-1;
                var root=new GameObject(team==0?"Radiant citadel":"Dire citadel").transform;root.SetParent(terrainRoot.transform);root.position=Position(p);
                Shape(PrimitiveType.Cylinder,"Citadel plaza",team==0?"788B7C":"696F7D",new Vector3(0,-.12f,0),new Vector3(28,.2f,28),root);
                Ring(root,new Vector3(0,.13f,0),10,.18f,Mat(stone));
                for(int i=0;i<7;i++){float a=(i*15+135)*Mathf.Deg2Rad;var q=new Vector3(Mathf.Cos(a)*13,0,Mathf.Sin(a)*13)*sign;Shape(PrimitiveType.Cube,"Citadel buttress",stone,q+Vector3.up*2,new Vector3(2,4,1.6f),root);Cone(root,q+Vector3.up*4,1.4f,1.5f,Mat(cloth),4);}
                for(int gate=0;gate<3;gate++)
                {
                    V2 direction=gate==0?new V2(.05f,1):gate==1?new V2(.707f,.707f):new V2(1,.05f);direction*=sign;var center=p+direction*14;var side=new V2(-direction.y,direction.x);
                    foreach(int edge in new[]{-1,1}){var q=center+side*(edge*4);Shape(PrimitiveType.Cube,"Lane gate pier",stone,Position(q,2.1f),new Vector3(1.1f,4.2f,1.1f),terrainRoot.transform);Shape(PrimitiveType.Cube,"Gate pennant",cloth,Position(q,3.1f),new Vector3(1.4f,1.4f,.14f),terrainRoot.transform);}
                    var arch=Shape(PrimitiveType.Cube,"Lane gate lintel",stone,Position(center,4.6f),new Vector3(8.7f,.75f,.8f),terrainRoot.transform);arch.transform.rotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.y));
                }
                var fountain=new Vector3(-4*sign,.1f,-4*sign);Shape(PrimitiveType.Cylinder,"Fountain basin",stone,fountain,new Vector3(6,.4f,6),root);Shape(PrimitiveType.Cylinder,"Fountain water",team==0?"6ABFA9":"9B93BF",fountain+Vector3.up*.45f,new Vector3(5,.07f,5),root,true);Cone(root,fountain+Vector3.up*.5f,.9f,3.2f,Mat(stone),6);
                var shop=new Vector3(-5*sign,0,3*sign);Shape(PrimitiveType.Cube,"Shop counter","846E4C",shop+Vector3.up*.6f,new Vector3(3.3f,1.2f,1.5f),root);Shape(PrimitiveType.Cube,"Shop canopy",cloth,shop+Vector3.up*2.8f,new Vector3(4,.3f,2.6f),root);
                foreach(int edge in new[]{-1,1})Limb(root,"B3A581",shop+new Vector3(edge*1.7f,0,0),shop+new Vector3(edge*1.7f,2.8f,0),.11f);
            }
        }
        void BuildLandmarks()
        {
            foreach(var rock in BattleMap.Rocks){var o=Shape(PrimitiveType.Sphere,"Roshan pit wall","777181",Position(rock.pos,1.2f),new Vector3(rock.radius*2.3f,3.8f,rock.radius*2),terrainRoot.transform);o.transform.rotation=Quaternion.Euler(12,rock.pos.x*13,10);}
            var pit=BattleMap.Roshan;Shape(PrimitiveType.Cylinder,"Roshan arena","635E68",Position(pit,-.06f),new Vector3(11,.1f,11),terrainRoot.transform);Ring(terrainRoot.transform,Position(pit,.04f),5.2f,.16f,Mat("9C8596"));
            foreach(var rune in BattleMap.Runes){Shape(PrimitiveType.Cylinder,"Rune shrine","8E917C",Position(rune,.04f),new Vector3(4,.18f,4),terrainRoot.transform);Ring(terrainRoot.transform,Position(rune,.16f),1.65f,.14f,Mat("D9BA69",true));}
            foreach(var p in new[]{new V2(-26,-15),new V2(26,15)})
            {
                var root=new GameObject("Jungle watch ruin").transform;root.SetParent(terrainRoot.transform);root.position=Position(p);
                Shape(PrimitiveType.Cylinder,"Ruin base","666F66",new Vector3(0,.1f,0),new Vector3(3,.2f,3),root);
                for(int k=0;k<3;k++){float a=k*Mathf.PI*2/3;Shape(PrimitiveType.Cube,"Broken column","8C968B",new Vector3(Mathf.Cos(a)*1.4f,1.1f+k*.35f,Mathf.Sin(a)*1.4f),new Vector3(.65f,2.2f+k*.7f,.65f),root);}
            }
        }
    }
}
