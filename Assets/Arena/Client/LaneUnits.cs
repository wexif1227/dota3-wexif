using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Rift.Core;
namespace Rift.Client
{
    public partial class WorldView
    {
        readonly Dictionary<string,Mesh> soldierMeshes=new Dictionary<string,Mesh>();
        void BuildSoldier(GameObject model,Unit unit,string color)
        {
            var root=model.transform;
            if(unit.kind==Kind.Summon&&unit.hero==0)
            {
                Shape(PrimitiveType.Sphere,"Spider abdomen","653F49",new Vector3(0,.55f,-.23f),new Vector3(.65f,.55f,.8f),root);
                Shape(PrimitiveType.Sphere,"Spider head","3D333E",new Vector3(0,.45f,.35f),new Vector3(.48f,.4f,.45f),root);
                foreach(int side in new[]{-1,1})for(int i=0;i<4;i++){float z=(i-1.5f)*.28f;Limb(root,"64515E",new Vector3(side*.18f,.45f,z),new Vector3(side*.62f,.6f,z-.15f),.045f);Limb(root,"64515E",new Vector3(side*.62f,.6f,z-.15f),new Vector3(side*.8f,.06f,z+.18f),.035f);}
                PaintSoldier(root,"spider");
                return;
            }
            bool neutral=unit.kind==Kind.Neutral,ent=unit.kind==Kind.Summon;
            string body=neutral?"9F8770":ent?"6B7050":color;
            Shape(PrimitiveType.Capsule,"Torso",body,new Vector3(0,1.1f,0),new Vector3(.75f,.65f,.65f),root);
            Shape(PrimitiveType.Sphere,"Head",neutral?"A8997D":ent?"8A9565":"B1AE99",new Vector3(0,1.9f,.06f),Vector3.one*.48f,root);
            if(!neutral&&!ent){Shape(PrimitiveType.Cube,"Shoulder armor","596974",new Vector3(0,1.5f,0),new Vector3(.95f,.22f,.6f),root);Shape(PrimitiveType.Cube,"Tabard",color,new Vector3(0,.84f,.31f),new Vector3(.43f,.7f,.07f),root);Cone(root,new Vector3(0,2.07f,0),.31f,.25f,Mat("626D78"),6);}
            var motion=model.AddComponent<CreepMotion>();
            foreach(int side in new[]{-1,1})
            {
                var leg=new GameObject("Leg").transform;leg.SetParent(root,false);leg.localPosition=new Vector3(side*.22f,.68f,0);
                Shape(PrimitiveType.Capsule,"Greave",neutral?"746754":"4A535B",new Vector3(0,-.27f,0),new Vector3(.25f,.33f,.28f),leg);Shape(PrimitiveType.Cube,"Boot","494E4D",new Vector3(0,-.56f,.11f),new Vector3(.3f,.22f,.48f),leg);
                if(side<0)motion.leftLeg=leg;else motion.rightLeg=leg;
            }
            var arm=new GameObject("Weapon arm").transform;arm.SetParent(root,false);arm.localPosition=new Vector3(.47f,1.47f,0);motion.weaponArm=arm;
            Limb(arm,body,Vector3.zero,new Vector3(.07f,-.45f,.17f),.12f);
            Limb(root,body,new Vector3(-.45f,1.47f,0),new Vector3(-.5f,.95f,.2f),.12f);
            if(unit.range>4){Limb(arm,"897557",new Vector3(.1f,-.55f,.24f),new Vector3(.1f,.55f,.24f),.065f);Shape(PrimitiveType.Sphere,"Ranged focus",unit.team==0?"86D6BC":"EB9A7B",new Vector3(.1f,.6f,.24f),Vector3.one*.26f,arm,true);}
            else if(!ent){Limb(arm,"A3AAA6",new Vector3(.1f,-.4f,.25f),new Vector3(.1f,.45f,.35f),.065f);Shape(PrimitiveType.Cube,"Axe blade","9AA9AA",new Vector3(.2f,.35f,.35f),new Vector3(.4f,.3f,.12f),arm);Shape(PrimitiveType.Cube,"Buckler","697C75",new Vector3(-.56f,1.02f,.28f),new Vector3(.16f,.63f,.58f),root);}
            if(ent)Crown(root,new Vector3(0,1.8f,-.1f),"688252",.75f,.9f);
            if(neutral)foreach(int side in new[]{-1,1})Cone(root,new Vector3(side*.23f,2.03f,0),.12f,.45f,Mat("C4B9A0"),5);
            string key=unit.kind+"/"+unit.team+"/"+unit.hero;
            PaintSoldier(motion.leftLeg,key+"/left");PaintSoldier(motion.rightLeg,key+"/right");PaintSoldier(arm,key+"/arm");PaintSoldier(root,key+"/body",motion.leftLeg,motion.rightLeg,arm);
        }
        void PaintSoldier(Transform group,string key,params Transform[] exclude)
        {
            var filters=group.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled&&!exclude.Any(t=>f.transform.IsChildOf(t))).ToArray();
            if(!soldierMeshes.TryGetValue(key,out var mesh))
            {
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
                foreach(var filter in filters)
                {
                    var source=filter.sharedMesh;var material=filter.GetComponent<MeshRenderer>().sharedMaterial;var color=material.color;color.a=material.GetColor("_EmissionColor").maxColorComponent>.1f?1:0;
                    var matrix=group.worldToLocalMatrix*filter.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;int first=vertices.Count;
                    foreach(var v in source.vertices){vertices.Add(matrix.MultiplyPoint3x4(v));colors.Add(color);}foreach(var n in source.normals)normals.Add(normalMatrix.MultiplyVector(n).normalized);foreach(int index in source.triangles)triangles.Add(first+index);
                }
                mesh=new Mesh{name="Shared soldier "+key,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();soldierMeshes[key]=mesh;
            }
            foreach(var filter in filters)filter.GetComponent<MeshRenderer>().enabled=false;
            group.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;group.gameObject.AddComponent<MeshRenderer>().sharedMaterial=HeroArt.Material;
        }
    }
}
