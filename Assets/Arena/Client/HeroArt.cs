using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rift.Client
{
    // Authored parametric meshes. Each animated body part is merged into one mesh.
    public static class HeroArt
    {
        static Material material;
        public static Material Material
        {
            get
            {
                if(!material)material=new Material(Shader.Find("Rift/Hero")){name="Hero painted metal and skin"};
                return material;
            }
        }
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static Color C(string hex,bool glow=false){ColorUtility.TryParseHtmlString("#"+hex,out var c);c.a=glow?1:0;return c;}
        static Transform Joint(Transform parent,string name,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        public static GameObject Create(int hero)
        {
            var root=new GameObject(Rift.Core.Catalog.Heroes[hero].name+" • sculpted");
            var motion=root.AddComponent<HeroMotion>();motion.hero=hero;
            var body=Joint(root.transform,"Body",Vector3.zero);motion.body=body;
            var limbs=new List<Transform>();
            if(hero==0)Spider(body,limbs);
            else if(hero==1)Fiend(body,limbs);
            else if(hero==2)Assassin(body,limbs);
            else if(hero==3)Butcher(body,limbs);
            else if(hero==4)Breaker(body,limbs);
            else Prophet(body,limbs);
            motion.limbs=limbs.ToArray();motion.Initialize();return root;
        }
        static void Spider(Transform root,List<Transform> limbs)
        {
            var m=new Sculpt();Color shell=C("26151E"),edge=C("59222D"),red=C("FF3426",true);
            m.Oval(V(0,1.65f,-.7f),V(1.22f,.95f,1.48f),shell);
            m.Oval(V(0,1.3f,.9f),V(.76f,.58f,.91f),C("24171D"));
            // Layered shell seams and a red hourglass on the abdomen.
            for(int s=-1;s<=1;s+=2)
            {
                m.Tube(new[]{V(s*.1f,2.56f,-1.8f),V(s*.62f,2.48f,-1.05f),V(s*.32f,2.56f,-.65f),V(s*.65f,2.35f,.05f)},new[]{.03f,.09f,.07f,.01f},red,7);
                m.Tube(new[]{V(s*.5f,1.08f,1.35f),V(s*.82f,.9f,1.85f),V(s*.4f,.6f,2.03f),V(s*.28f,.72f,1.77f)},new[]{.22f,.17f,.1f,.005f},edge);
                for(int i=0;i<3;i++)m.Oval(V(s*(.18f+i*.19f),1.56f-i*.09f,1.61f-i*.055f),Vector3.one*(.095f-i*.013f),red,12,8);
                for(int i=0;i<4;i++)
                {
                    var joint=Joint(root,"Leg "+s+" "+i,V(s*.62f,1.47f,1-i*.55f));limbs.Add(joint);
                    var leg=new Sculpt();float back=1.6f-i*1.03f;
                    var points=new[]{Vector3.zero,V(s*.8f,.48f,back*.5f),V(s*1.8f,.7f,back),V(s*2.14f,-.35f,back+.25f),V(s*2.32f,-1.43f,back+.48f)};
                    leg.Tube(points,new[]{.2f,.19f,.15f,.1f,.008f},shell,10);
                    leg.Oval(points[2],V(.19f,.19f,.21f),edge,12,8);
                    leg.Tube(new[]{points[0]+V(0,.08f,0),points[1]+V(0,.08f,0),points[2]+V(0,.06f,0)},new[]{.035f,.045f,.01f},C("9E4A4B"),6);
                    leg.Finish(joint);
                }
            }
            // Wax candles echo the supplied Broodmother loadout.
            foreach(var p in new[]{V(-.7f,2.28f,-.9f),V(.66f,2.32f,-1.06f),V(.24f,2.48f,-.32f)})
            {
                m.Tube(new[]{p,p+V(.02f,.7f,.01f)},new[]{.15f,.13f},C("D8C88B"),12);
                m.Oval(p+V(0,.05f,0),V(.25f,.08f,.24f),C("B6A66C"));
                m.Tube(new[]{p+V(0,.68f,0),p+V(.06f,.93f,0),p+V(0,1.12f,.02f)},new[]{.07f,.12f,.001f},C("FFAA35",true),10);
            }
            m.Finish(root);
        }
        static void Fiend(Transform root,List<Transform> limbs)
        {
            var m=new Sculpt();Color black=C("221F29"),plate=C("3C2830"),lava=C("FF5B12",true),gold=C("FFBC37",true);
            m.Oval(V(0,2.6f,0),V(.82f,1.13f,.52f),black);
            m.Oval(V(0,2.72f,.44f),V(.58f,.78f,.13f),lava);
            for(int s=-1;s<=1;s+=2)
            {
                for(int i=0;i<4;i++)m.Tube(new[]{V(s*.02f,3.22f-i*.34f,.57f),V(s*.5f,3.12f-i*.29f,.54f),V(s*.75f,3.45f-i*.37f,.12f)},new[]{.08f,.16f,.025f},plate,8);
                m.Blade(new[]{V(s*.75f,3.25f,.02f),V(s*1.55f,3.6f,-.08f),V(s*1.3f,4.11f,-.18f),V(s*1.02f,3.83f,.1f),V(s*.64f,3.71f,.2f)},.15f,black);
                for(int i=0;i<3;i++)m.Tube(new[]{V(s*(.73f+i*.19f),3.58f,0),V(s*(1.15f+i*.18f),3.97f+i*.08f,-.1f),V(s*(1.4f+i*.17f),4.4f+i*.1f,-.32f)},new[]{.18f,.13f,.002f},plate,8);
                var arm=Joint(root,"Claw "+s,V(s*.95f,3.15f,0));limbs.Add(arm);var a=new Sculpt();
                a.Tube(new[]{Vector3.zero,V(s*.35f,-.7f,.07f),V(s*.47f,-1.22f,.45f)},new[]{.3f,.22f,.2f},black,12);
                a.Oval(V(s*.52f,-1.4f,.48f),V(.31f,.37f,.21f),plate);
                for(int j=0;j<4;j++)a.Tube(new[]{V(s*(.3f+j*.14f),-1.48f,.53f),V(s*(.36f+j*.19f),-1.96f,.77f),V(s*(.18f+j*.17f),-2.13f,1.04f)},new[]{.09f,.06f,.001f},j%2==0?black:plate,7);
                a.Tube(new[]{V(s*.27f,-.3f,.24f),V(s*.42f,-.85f,.24f),V(s*.44f,-1.2f,.6f)},new[]{.04f,.06f,.015f},lava,6);a.Finish(arm);
            }
            m.Oval(V(0,3.75f,.07f),V(.46f,.62f,.37f),black);
            for(int s=-1;s<=1;s+=2)
            {
                m.Tube(new[]{V(s*.23f,4,.03f),V(s*.5f,4.44f,-.07f),V(s*.46f,4.87f,-.2f)},new[]{.18f,.13f,.001f},plate,9);
                m.Tube(new[]{V(s*.05f,3.88f,.4f),V(s*.28f,3.99f,.34f),V(s*.41f,4.13f,.18f)},new[]{.055f,.085f,.008f},gold,7);
                m.Blade(new[]{V(s*.03f,3.56f,.4f),V(s*.22f,3.72f,.39f),V(s*.3f,3.51f,.35f),V(s*.08f,3.2f,.46f)},.1f,plate);
            }
            m.Tube(new[]{V(0,.25f,0),V(.22f,.8f,0),V(-.12f,1.3f,0),V(0,1.95f,0)},new[]{.08f,.37f,.42f,.56f},lava,16);
            for(int i=0;i<8;i++){float angle=i*2.4f;var p=V(Mathf.Sin(angle)*(.22f+i*.04f),.6f+i*.16f,Mathf.Cos(angle)*.32f);m.Oval(p,V(.28f,.1f,.21f),black,10,6);}
            m.Finish(root);
        }
        static void Face(Sculpt m,Vector3 p,Color skin,Color eyes,bool mask=false)
        {
            m.Oval(p,V(.29f,.39f,.25f),skin,20,14);
            m.Oval(p+V(0,-.11f,.19f),V(.19f,.18f,.09f),mask?C("313842"):skin,16,10);
            m.Oval(p+V(0,.01f,.24f),V(.065f,.1f,.075f),skin,12,8);
            for(int s=-1;s<=1;s+=2)
            {
                m.Tube(new[]{p+V(s*.055f,.12f,.22f),p+V(s*.2f,.16f,.19f)},new[]{.035f,.018f},C("574044"),7);
                m.Oval(p+V(s*.12f,.08f,.241f),V(.07f,.027f,.023f),eyes,12,8);
            }
        }
        static void Assassin(Transform root,List<Transform> limbs)
        {
            var m=new Sculpt();Color suit=C("173B43"),gold=C("D1A548"),skin=C("B8BDDA"),pink=C("F16C85",true);
            m.Tube(new[]{V(0,1.75f,0),V(0,2.12f,0),V(0,2.62f,0),V(0,2.96f,0)},new[]{.44f,.33f,.52f,.28f},suit,20,.62f);
            for(int s=-1;s<=1;s+=2)
            {
                m.Oval(V(s*.22f,2.64f,.22f),V(.26f,.26f,.15f),suit);
                m.Tube(new[]{V(s*.47f,2.81f,.12f),V(s*.2f,2.38f,.34f),V(0,2.15f,.26f)},new[]{.043f,.045f,.025f},gold,7);
                m.Oval(V(s*.61f,2.91f,0),V(.33f,.17f,.33f),C("55454F"));
                for(int j=0;j<3;j++)m.Blade(new[]{V(s*(.42f+j*.13f),2.98f,-.03f),V(s*(.65f+j*.24f),3.32f,-.12f),V(s*(.9f+j*.15f),3.02f,.13f),V(s*.56f,2.85f,.33f)},.09f,j==1?pink:C("813D57"));
                var arm=Joint(root,"Arm "+s,V(s*.57f,2.75f,0));limbs.Add(arm);var a=new Sculpt();
                a.Tube(new[]{Vector3.zero,V(s*.31f,-.4f,.02f),V(s*.47f,-.75f,.4f)},new[]{.15f,.11f,.095f},skin,12);
                a.Tube(new[]{V(s*.34f,-.45f,.12f),V(s*.47f,-.72f,.37f)},new[]{.16f,.13f},suit,12);
                a.Tube(new[]{V(s*.34f,-.45f,.12f),V(s*.36f,-.49f,.16f)},new[]{.17f,.17f},gold,12);
                a.Oval(V(s*.49f,-.8f,.49f),V(.12f,.12f,.2f),skin);
                a.Blade(new[]{V(s*.43f,-.71f,.4f),V(s*.63f,-.65f,.73f),V(s*.65f,-.58f,1.85f),V(s*.38f,-.78f,.92f)},.055f,C("FFD78A",true));
                a.Tube(new[]{V(s*.48f,-.7f,.6f),V(s*.62f,-.61f,1.65f)},new[]{.025f,.002f},C("FFF9DF",true),6);a.Finish(arm);
                var leg=Joint(root,"Leg "+s,V(s*.24f,1.73f,0));limbs.Add(leg);var l=new Sculpt();
                l.Tube(new[]{Vector3.zero,V(s*.12f,-.73f,.08f),V(s*.17f,-1.43f,-.03f)},new[]{.24f,.15f,.105f},suit,16);
                l.Oval(V(s*.17f,-1.53f,.14f),V(.16f,.13f,.32f),suit);
                l.Tube(new[]{V(s*.12f,-.7f,.08f),V(s*.13f,-.77f,.08f)},new[]{.17f,.17f},gold,12);l.Finish(leg);
            }
            Face(m,V(0,3.38f,.03f),skin,C("FFE99C",true));
            m.Tube(new[]{V(-.45f,2.83f,-.18f),V(-.41f,3.48f,-.19f),V(0,3.87f,-.13f),V(.41f,3.48f,-.19f),V(.45f,2.83f,-.18f)},new[]{.16f,.22f,.25f,.22f,.16f},C("E4D7BE"),12);
            m.Blade(new[]{V(-.34f,3.65f,.13f),V(-.17f,4.05f,.02f),V(0,3.84f,.22f),V(.17f,4.05f,.02f),V(.34f,3.65f,.13f),V(0,3.58f,.36f)},.08f,gold);
            m.Oval(V(0,3.73f,.35f),V(.085f,.16f,.055f),C("FFF2AE",true),10,8);
            m.Blade(new[]{V(-.4f,1.99f,.28f),V(.4f,1.99f,.28f),V(.25f,1.23f,.37f),V(0,1.04f,.43f),V(-.25f,1.23f,.37f)},.03f,suit);
            m.Tube(new[]{V(-.4f,1.99f,.32f),V(-.25f,1.23f,.41f),V(0,1.04f,.47f),V(.25f,1.23f,.41f),V(.4f,1.99f,.32f)},new[]{.035f,.035f,.04f,.035f,.035f},gold,6);m.Finish(root);
        }
        static void Butcher(Transform root,List<Transform> limbs)
        {
            var m=new Sculpt();Color skin=C("A29272"),shadow=C("6F615C"),leather=C("3E494C"),iron=C("8B9FA5");
            m.Oval(V(0,1.87f,.03f),V(1.09f,1.17f,.77f),skin,28,18);
            m.Oval(V(-.42f,2.57f,.38f),V(.6f,.42f,.47f),C("AE9B80"));m.Oval(V(.51f,2.48f,.37f),V(.58f,.43f,.46f),skin);
            // Curved apron follows the belly rather than a flat cube.
            m.Cloth(.94f,2.65f,1.28f,1.02f,-65,65,leather);
            for(int s=-1;s<=1;s+=2)
            {
                m.Tube(new[]{V(s*.65f,2.91f,-.2f),V(s*.68f,2.79f,.51f),V(s*.56f,2.1f,.86f),V(s*.55f,1.42f,.86f)},new[]{.095f,.1f,.09f,.085f},C("4E5759"),8);
                m.Oval(V(s*.57f,2.08f,.91f),Vector3.one*.11f,iron,12,8);
                var arm=Joint(root,"Arm "+s,V(s*.98f,2.68f,0));limbs.Add(arm);var a=new Sculpt();
                a.Tube(new[]{Vector3.zero,V(s*.5f,-.42f,.05f),V(s*.64f,-.84f,.4f)},new[]{.48f,.38f,.29f},skin,18);
                a.Oval(V(s*.63f,-.97f,.48f),V(.31f,.34f,.3f),C("827B64"));
                for(int j=0;j<4;j++)a.Oval(V(s*(.42f+j*.13f),-1.11f,.64f),V(.09f,.22f,.11f),skin,10,8);
                a.Tube(new[]{V(s*.5f,-.46f,.07f),V(s*.58f,-.67f,.24f)},new[]{.4f,.35f},C("594C43"),16);
                if(s<0)
                {
                    a.Tube(new[]{V(s*.6f,-1.16f,.5f),V(s*.62f,-1.16f,1.05f)},new[]{.1f,.1f},C("4F3428"),10);
                    a.Blade(new[]{V(-.59f,-1.04f,.9f),V(-1.2f,-.98f,1.02f),V(-1.42f,-1.56f,1.06f),V(-.61f,-1.61f,.9f)},.11f,iron);
                    a.Tube(new[]{V(-1.42f,-1.56f,1.07f),V(-.61f,-1.61f,.91f)},new[]{.045f,.045f},C("C1CFCC"),6);
                }
                else
                {
                    a.Tube(new[]{V(.65f,-1.14f,.56f),V(.98f,-1.32f,.86f),V(1.37f,-1.02f,1.04f),V(1.2f,-.6f,1.1f),V(.95f,-.72f,1.1f)},new[]{.12f,.15f,.13f,.08f,.002f},iron,12);
                    for(int j=0;j<5;j++)a.Loop(V(.7f,-1.15f-j*.15f,.5f),.115f,.033f,j%2==0?Vector3.forward:Vector3.right,iron);
                }
                a.Finish(arm);
                var leg=Joint(root,"Leg "+s,V(s*.52f,1.15f,0));limbs.Add(leg);var l=new Sculpt();
                l.Tube(new[]{Vector3.zero,V(s*.08f,-.48f,.03f),V(s*.18f,-.88f,.09f)},new[]{.36f,.3f,.23f},C("354147"),16);
                l.Oval(V(s*.18f,-.98f,.26f),V(.35f,.24f,.53f),C("73513C"));l.Finish(leg);
            }
            m.Oval(V(0,3.04f,.08f),V(.53f,.56f,.49f),skin,24,18);
            m.Oval(V(0,2.89f,.53f),V(.43f,.25f,.13f),C("292927"));
            for(int s=-1;s<=1;s+=2)m.Oval(V(s*.21f,3.22f,.5f),V(.1f,.07f,.04f),C("BFE852",true));
            m.Oval(V(0,3.08f,.57f),V(.13f,.15f,.13f),skin);
            for(int j=0;j<7;j++)m.Tube(new[]{V(-.31f+j*.1f,2.97f,.65f),V(-.29f+j*.1f,2.81f,.68f)},new[]{.055f,.03f},C("D8C998"),7);
            m.Tube(new[]{V(-.44f,3.14f,.3f),V(-.26f,3.55f,.24f),V(.18f,3.57f,.25f),V(.43f,3.15f,.33f)},new[]{.04f,.04f,.04f,.04f},shadow,7);
            for(int j=0;j<6;j++)m.Tube(new[]{V(-.4f+j*.14f,3.46f,.38f),V(-.38f+j*.14f,3.56f,.36f)},new[]{.018f,.018f},C("362D28"),5);
            m.Oval(V(0,1.17f,.87f),V(.28f,.22f,.07f),C("B19048"),12,8);m.Finish(root);
        }
        static void Breaker(Transform root,List<Transform> limbs)
        {
            var m=new Sculpt();Color skin=C("4B8FA9"),fur=C("86CADD"),armor=C("4B4546"),bronze=C("99856B"),ivory=C("D1C7A1");
            m.Tube(new[]{V(0,1.45f,0),V(0,2.07f,0),V(0,2.84f,0),V(0,3.17f,-.05f)},new[]{.62f,.72f,1.03f,.62f},skin,22,.68f);
            m.Oval(V(0,3.08f,-.27f),V(.8f,.7f,.5f),fur);
            m.Cloth(.89f,1.77f,.83f,.59f,-180,180,C("272E35"));
            for(int s=-1;s<=1;s+=2)
            {
                m.Oval(V(s*.92f,2.94f,0),V(.65f,.57f,.63f),armor,20,14);
                m.Loop(V(s*.93f,2.93f,.5f),.38f,.06f,Vector3.forward,bronze);
                for(int j=0;j<3;j++)m.Oval(V(s*(.55f+j*.28f),3.18f,.43f),Vector3.one*.09f,bronze,10,8);
                var arm=Joint(root,"Arm "+s,V(s*1.03f,2.7f,.02f));limbs.Add(arm);var a=new Sculpt();
                a.Tube(new[]{Vector3.zero,V(s*.39f,-.58f,.05f),V(s*.43f,-1.1f,.42f)},new[]{.38f,.3f,.25f},skin,16);
                a.Tube(new[]{V(s*.39f,-.62f,.08f),V(s*.43f,-.93f,.3f)},new[]{.35f,.3f},armor,14);
                a.Oval(V(s*.44f,-1.24f,.51f),V(.32f,.35f,.28f),skin);
                for(int j=0;j<3;j++)a.Tube(new[]{V(s*(.28f+j*.16f),-1.35f,.66f),V(s*(.28f+j*.16f),-1.5f,.56f)},new[]{.09f,.08f},fur,8);
                if(s<0){for(int j=0;j<7;j++)a.Loop(V(-.5f,-1.44f-j*.14f,.54f),.12f,.033f,j%2==0?Vector3.forward:Vector3.right,bronze);a.Oval(V(-.5f,-2.45f,.54f),V(.43f,.46f,.43f),armor);for(int j=0;j<5;j++){float angle=j*Mathf.PI*2/5;a.Tube(new[]{V(-.5f,-2.45f,.54f),V(-.5f+Mathf.Sin(angle)*.72f,-2.42f,.54f+Mathf.Cos(angle)*.72f)},new[]{.24f,.001f},C("91E8ED",true),8);}}
                a.Finish(arm);
                var leg=Joint(root,"Leg "+s,V(s*.42f,1.34f,0));limbs.Add(leg);var l=new Sculpt();l.Tube(new[]{Vector3.zero,V(s*.16f,-.55f,-.08f),V(s*.23f,-1.03f,.14f)},new[]{.32f,.25f,.19f},skin,16);l.Oval(V(s*.24f,-1.16f,.29f),V(.32f,.21f,.39f),C("333A3E"));l.Finish(leg);
            }
            m.Oval(V(0,3.27f,.35f),V(.55f,.6f,.45f),skin,24,16);
            m.Oval(V(0,3.03f,.74f),V(.38f,.25f,.25f),C("445B70"));
            m.Loop(V(0,2.96f,.99f),.17f,.045f,Vector3.forward,C("C9A459"));
            for(int s=-1;s<=1;s+=2)
            {
                m.Tube(new[]{V(s*.4f,3.62f,.11f),V(s*.79f,3.73f,.05f),V(s*.99f,4.02f,.04f),V(s*.78f,4.36f,.16f)},new[]{.25f,.22f,.14f,.003f},ivory,12);
                m.Oval(V(s*.22f,3.4f,.73f),V(.095f,.06f,.04f),C("FF652B",true));
                m.Tube(new[]{V(s*.42f,3.14f,.6f),V(s*.42f,2.79f,.72f),V(s*.24f,2.44f,.63f)},new[]{.23f,.21f,.001f},fur,10);
                m.Tube(new[]{V(s*.25f,2.86f,.9f),V(s*.28f,3.11f,.99f)},new[]{.08f,.001f},ivory,8);
            }
            for(int i=0;i<5;i++){float x=(i-2)*.15f;m.Tube(new[]{V(x,3.69f,.06f),V(x*.8f,4.15f,-.11f),V(x*.2f,4.63f,-.2f)},new[]{.14f,.1f,.001f},C(i%2==0?"5BCDE2":"A6F4EF",true),9);}m.Finish(root);
        }
        static void Prophet(Transform root,List<Transform> limbs)
        {
            var m=new Sculpt();Color green=C("7C9D6E"),cloth=C("304B37"),wood=C("675139"),gold=C("B9A85A");
            m.Tube(new[]{V(0,1.62f,0),V(0,2.18f,0),V(0,2.83f,0)},new[]{.6f,.44f,.68f},green,20,.62f);
            m.Cloth(.48f,1.83f,.69f,.55f,-180,180,C("484C36"));
            for(int s=-1;s<=1;s+=2)
            {
                for(int j=0;j<4;j++)m.Blade(new[]{V(s*.05f,1.87f,.45f),V(s*(.3f+j*.14f),1.62f,.49f-j*.08f),V(s*(.24f+j*.18f),.44f+j*.13f,.57f-j*.1f)},.035f,j%2==0?cloth:C("597148"));
                m.Oval(V(s*.67f,2.83f,0),V(.47f,.28f,.43f),wood);
                m.Tube(new[]{V(s*.42f,2.83f,.32f),V(s*.75f,3.03f,.35f),V(s*.97f,2.77f,.26f)},new[]{.05f,.055f,.01f},gold,8);
                var arm=Joint(root,"Arm "+s,V(s*.64f,2.72f,0));limbs.Add(arm);var a=new Sculpt();
                a.Tube(new[]{Vector3.zero,V(s*.3f,-.5f,.06f),V(s*.49f,-.83f,.47f)},new[]{.22f,.17f,.12f},green,14);
                a.Tube(new[]{V(s*.32f,-.52f,.1f),V(s*.46f,-.75f,.4f)},new[]{.21f,.18f},wood,12);
                a.Oval(V(s*.5f,-.89f,.52f),V(.16f,.19f,.18f),green);
                if(s==1)
                {
                    a.Tube(new[]{V(.6f,-2.5f,.58f),V(.59f,-1.2f,.58f),V(.69f,.05f,.5f),V(.81f,1.2f,.52f),V(.5f,1.65f,.58f)},new[]{.075f,.1f,.12f,.16f,.06f},wood,12);
                    a.Loop(V(.73f,1.15f,.55f),.38f,.065f,Vector3.forward,gold);
                    a.Oval(V(.73f,1.15f,.55f),V(.2f,.31f,.17f),C("79DCE4",true),12,10);
                }
                else for(int j=0;j<5;j++)a.Blade(new[]{V(-.3f,-.48f,.2f),V(-.9f-j*.06f,-.58f-j*.12f,.13f),V(-.55f,-1.3f-j*.08f,.32f)},.025f,j%2==0?C("608E48"):C("446C38"));a.Finish(arm);
                var leg=Joint(root,"Leg "+s,V(s*.32f,1.27f,0));limbs.Add(leg);var l=new Sculpt();l.Tube(new[]{Vector3.zero,V(s*.04f,-.58f,.02f),V(s*.1f,-1.02f,.08f)},new[]{.23f,.18f,.14f},cloth,14);l.Oval(V(s*.11f,-1.1f,.25f),V(.22f,.17f,.36f),wood);l.Finish(leg);
            }
            Face(m,V(0,3.27f,.08f),green,C("F9D57A",true));
            for(int i=-2;i<=2;i++)m.Tube(new[]{V(i*.11f,3.17f,.31f),V(i*.16f,2.86f,.48f),V(i*.13f,2.48f,.45f),V(i*.09f,2.17f,.29f)},new[]{.15f,.16f,.12f,.001f},C(i%2==0?"6C8161":"C4C19A"),10);
            for(int s=-1;s<=1;s+=2)
            {
                var antler=new[]{V(s*.23f,3.56f,0),V(s*.68f,3.91f,-.01f),V(s*.95f,4.4f,-.07f),V(s*.86f,4.89f,-.04f),V(s*.57f,5.1f,.1f)};
                m.Tube(antler,new[]{.17f,.14f,.1f,.06f,.001f},gold,10);
                for(int j=0;j<3;j++)m.Tube(new[]{antler[j+1],antler[j+1]+V(-s*.14f,.35f,.16f),antler[j+1]+V(-s*.2f,.64f,.21f)},new[]{.09f,.05f,.001f},gold,8);
            }
            m.Finish(root);
        }

        // Geometry carries painted vertex colours; no per-decoration draw calls.
        sealed class Sculpt
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<int> triangles=new List<int>();
            int Add(Vector3 p,Color c){vertices.Add(p);float variation=.97f+.03f*Mathf.Sin(p.x*2.1f+p.y*1.3f+p.z*1.9f);colors.Add(new Color(c.r*variation,c.g*variation,c.b*variation,c.a));return vertices.Count-1;}
            void Quad(int a,int b,int c,int d){triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(c);triangles.Add(b);triangles.Add(d);}
            public void Oval(Vector3 center,Vector3 radius,Color color,int segments=16,int rings=12)
            {
                int start=vertices.Count;
                for(int y=0;y<=rings;y++){float t=y*Mathf.PI/rings;for(int x=0;x<=segments;x++){float a=x*Mathf.PI*2/segments;Add(center+Vector3.Scale(radius,new Vector3(Mathf.Sin(t)*Mathf.Cos(a),Mathf.Cos(t),Mathf.Sin(t)*Mathf.Sin(a))),color);}}
                for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int a=start+y*(segments+1)+x;Quad(a,a+1,a+segments+1,a+segments+2);}
            }
            public void Tube(Vector3[] points,float[] radius,Color color,int sides=10,float depth=1)
            {
                int start=vertices.Count,steps=(points.Length-1)*5;
                for(int i=0;i<=steps;i++)
                {
                    float t=i/(float)steps*(points.Length-1);int k=Mathf.Min((int)t,points.Length-2);float f=t-k;
                    Vector3 p0=points[Mathf.Max(0,k-1)],p1=points[k],p2=points[k+1],p3=points[Mathf.Min(points.Length-1,k+2)];
                    Vector3 p=.5f*((2*p1)+(-p0+p2)*f+(2*p0-5*p1+4*p2-p3)*f*f+(-p0+3*p1-3*p2+p3)*f*f*f);
                    Vector3 dir=(p2-p1).normalized,side=Vector3.Cross(dir,Mathf.Abs(dir.z)<.8f?Vector3.forward:Vector3.up).normalized,up=Vector3.Cross(side,dir).normalized;float r=Mathf.Lerp(radius[k],radius[k+1],f);
                    for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;Add(p+(side*Mathf.Cos(a)+up*Mathf.Sin(a)*depth)*r,color);}
                }
                for(int i=0;i<steps;i++)for(int j=0;j<sides;j++){int a=start+i*(sides+1)+j;Quad(a,a+sides+1,a+1,a+sides+2);}
                // Closed end caps make horns and limbs solid from every camera angle.
                int first=Add(points[0],color),last=Add(points[points.Length-1],color);
                for(int j=0;j<sides;j++){triangles.Add(first);triangles.Add(start+j+1);triangles.Add(start+j);triangles.Add(last);triangles.Add(start+steps*(sides+1)+j);triangles.Add(start+steps*(sides+1)+j+1);}
            }
            public void Blade(Vector3[] points,float thickness,Color color)
            {
                int start=vertices.Count;var center=Vector3.zero;foreach(var p in points)center+=p;center/=points.Length;
                foreach(var p in points)Add(p+Vector3.forward*thickness*.5f,color);int front=Add(center+Vector3.forward*thickness,color);
                int back=vertices.Count;foreach(var p in points)Add(p-Vector3.forward*thickness*.5f,color);int rear=Add(center-Vector3.forward*thickness,color);
                for(int i=0;i<points.Length;i++){int j=(i+1)%points.Length;triangles.Add(front);triangles.Add(start+i);triangles.Add(start+j);triangles.Add(rear);triangles.Add(back+j);triangles.Add(back+i);Quad(start+i,back+i,start+j,back+j);}
            }
            public void Loop(Vector3 center,float radius,float width,Vector3 normal,Color color)
            {var p=new Vector3[17];var r=new float[p.Length];var rotation=Quaternion.FromToRotation(Vector3.forward,normal);for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/16;p[i]=center+rotation*V(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);r[i]=width;}Tube(p,r,color,6);}
            public void Cloth(float bottom,float top,float width,float depth,float angleFrom,float angleTo,Color color)
            {
                int start=vertices.Count,steps=24;
                for(int y=0;y<=8;y++)for(int x=0;x<=steps;x++){float f=y/8f,a=Mathf.Lerp(angleFrom,angleTo,x/(float)steps)*Mathf.Deg2Rad,pleat=1+.055f*Mathf.Cos(a*12);Add(V(Mathf.Sin(a)*width*(1-f*.16f)*pleat,Mathf.Lerp(bottom,top,f),Mathf.Cos(a)*depth*pleat),color);}
                for(int y=0;y<8;y++)for(int x=0;x<steps;x++){int a=start+y*(steps+1)+x;Quad(a,a+1,a+steps+1,a+steps+2);}
            }
            public void Finish(Transform parent)
            {
                var mesh=new Mesh{name=parent.name+" sculpt",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                parent.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=parent.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=Material;
            }
        }
    }

}
