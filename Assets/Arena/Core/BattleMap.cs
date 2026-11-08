using System;
using System.Collections.Generic;
namespace Rift.Core
{
    public static class BattleMap
    {
        public const float Size=120;
        public static readonly V2[] Bases={new V2(-47,-47),new V2(47,47)};
        public static V2 BaseStructure(int team,int lane,bool barracks)
        {
            V2 p=lane==0?new V2(-46,barracks?-37:-32):lane==2?new V2(barracks?-37:-32,-46):new V2(barracks?-38:-34,barracks?-38:-34);
            return team==0?p:p*-1;
        }
        public static readonly V2 Roshan=new V2(-13,13);
        public static readonly V2[] Runes={new V2(-22,22),new V2(22,-22)};
        public static readonly V2[] Crossings={new V2(-40,40),new V2(-22,22),new V2(0,0),new V2(22,-22),new V2(40,-40)};
        public static readonly V2[][] Lanes={
            new[]{new V2(-44,-41),new V2(-46,-20),new V2(-46,10),new V2(-40,39),new V2(-14,46),new V2(16,46),new V2(41,44)},
            new[]{new V2(-41,-41),new V2(-28,-28),new V2(-14,-14),new V2(0,0),new V2(14,14),new V2(28,28),new V2(41,41)},
            new[]{new V2(-41,-44),new V2(-16,-46),new V2(14,-46),new V2(40,-39),new V2(46,-10),new V2(46,20),new V2(44,41)}};
        public static readonly V2[] Camps={new V2(-30,-8),new V2(-16,-34),new V2(-34,19),new V2(-7,-20),new V2(30,8),new V2(16,34),new V2(34,-19),new V2(7,20)};
        public static readonly List<V2[]> JungleRoutes=new List<V2[]>();
        public struct Rock{public V2 pos;public float radius;public Rock(V2 p,float r){pos=p;radius=r;}}
        public static readonly List<Rock> Rocks=new List<Rock>();
        public static readonly List<V2> Trees=new List<V2>();
        static readonly bool[,] blocked=new bool[60,60];
        static BattleMap()
        {
            var routes=new[]{
                new[]{new V2(-41,-35),new V2(-28,-24),new V2(-16,-34),new V2(-7,-20),new V2(-14,-14)},
                new[]{new V2(-28,-24),new V2(-30,-8),new V2(-44,8)},
                new[]{new V2(-30,-8),new V2(-18,6),new V2(-22,22)},
                new[]{new V2(-44,8),new V2(-34,19),new V2(-40,40)},
                new[]{new V2(-34,19),new V2(-22,22)},
                new[]{new V2(-7,-20),new V2(0,-10),new V2(0,0)}};
            foreach(var route in routes){JungleRoutes.Add(route);var mirror=new V2[route.Length];for(int i=0;i<route.Length;i++)mirror[i]=route[i]*-1;JungleRoutes.Add(mirror);}
            for(int x=0;x<60;x++)for(int y=0;y<60;y++){var p=new V2(x*2-59,y*2-59);float river=Math.Abs(p.x+p.y)*.7071068f;blocked[x,y]=river>3.1f&&river<6.2f&&!Ramp(p)&&!Clearing(p);}
            var random=new Random(7419);
            for(int i=0;i<780;i++)
            {
                var p=new V2((float)random.NextDouble()*106-53,(float)random.NextDouble()*106-53);
                if(p.x+p.y>0||Corridor(p)||!Walkable(p))continue;
                if(Trees.Exists(t=>V2.Distance(t,p)<2.2f))continue;
                Trees.Add(p);Trees.Add(p*-1);blocked[Cell(p.x),Cell(p.y)]=true;blocked[Cell(-p.x),Cell(-p.y)]=true;
            }
            // A crescent pit with an entrance facing mid.
            for(int i=0;i<14;i++){float angle=i*(float)Math.PI*2/14;var dir=new V2((float)Math.Cos(angle),(float)Math.Sin(angle));if(V2.Dot(dir,new V2(.707f,-.707f))>.25f)continue;Rocks.Add(new Rock(Roshan+dir*7.2f,1.25f));}
            foreach(var rock in Rocks)for(int x=0;x<60;x++)for(int y=0;y<60;y++)if(V2.Distance(new V2(x*2-59,y*2-59),rock.pos)<rock.radius+1)blocked[x,y]=true;
        }
        static bool Corridor(V2 p)
        {
            if(Math.Abs(p.x+p.y)<10||V2.Distance(p,Bases[0])<16||V2.Distance(p,Bases[1])<16||V2.Distance(p,Roshan)<10)return true;
            foreach(var lane in Lanes)for(int j=1;j<lane.Length;j++)if(Segment(p,lane[j-1],lane[j])<5.8f)return true;
            foreach(var route in JungleRoutes)for(int j=1;j<route.Length;j++)if(Segment(p,route[j-1],route[j])<3.3f)return true;
            foreach(var c in Camps)if(V2.Distance(c,p)<6.6f)return true;return false;
        }
        public static bool Ramp(V2 p){foreach(var c in Crossings)if(Math.Abs((p.x-p.y-c.x+c.y)*.7071068f)<7)return true;return false;}
        public static bool Clearing(V2 p){foreach(var camp in Camps)if(V2.Distance(p,camp)<5.8f)return true;return false;}
        static float Smooth(float a,float b,float v){float t=Math.Max(0,Math.Min(1,(v-a)/(b-a)));return t*t*(3-2*t);}
        public static float Height(float x,float z)
        {
            var p=new V2(x,z);float raw=RawHeight(x,z);
            foreach(var camp in Camps){float distance=V2.Distance(p,camp);if(distance<6.5f){float blend=Smooth(4.8f,6.5f,distance);return RawHeight(camp.x,camp.y)*(1-blend)+raw*blend;}}
            foreach(var center in Bases){float distance=V2.Distance(p,center);if(distance<15.5f){float blend=Smooth(13.5f,15.5f,distance);return RawHeight(center.x,center.y)*(1-blend)+raw*blend;}}
            float pit=V2.Distance(p,Roshan);if(pit<8.5f){float blend=Smooth(5.8f,8.5f,pit);return .35f*(1-blend)+raw*blend;}return raw;
        }
        static float RawHeight(float x,float z)
        {
            var p=new V2(x,z);float river=Math.Abs(x+z)*.7071068f;
            float height=-.95f+1.75f*Smooth(Ramp(p)?2.2f:3.1f,Ramp(p)?8:4.2f,river);
            height+=.35f*Smooth(16,32,river)+.12f*(float)(Math.Sin(x*.16f)*Math.Cos(z*.13f))*Smooth(9,16,river);return height;
        }
        public static float Segment(V2 p,V2 a,V2 b){V2 d=b-a;float t=Math.Max(0,Math.Min(1,V2.Dot(p-a,d)/Math.Max(.001f,V2.Dot(d,d))));return V2.Distance(p,a+d*t);}
        static int Cell(float a)=>Math.Max(0,Math.Min(59,(int)((a+60)/2)));
        static V2 Point(int id)=>new V2((id%60)*2-59,(id/60)*2-59);
        public static bool Walkable(V2 p)=>Math.Abs(p.x)<=57&&Math.Abs(p.y)<=57&&!blocked[Cell(p.x),Cell(p.y)];
        public static V2 Free(V2 p)
        {
            p=V2.Clamp(p);if(Walkable(p))return p;
            for(int r=1;r<12;r++)for(int k=0;k<24;k++){var q=V2.Clamp(p+new V2((float)Math.Cos(k*Math.PI/12),(float)Math.Sin(k*Math.PI/12))*r);if(Walkable(q))return q;}return p;
        }
        public static bool Clear(V2 a,V2 b)
        {
            float length=V2.Distance(a,b);int steps=Math.Max(1,(int)Math.Ceiling(length/.7f));for(int i=1;i<=steps;i++)if(!Walkable(a+(b-a)*(i/(float)steps)))return false;return true;
        }
        public static V2 LanePoint(int lane,int waypoint,int team,int slot)
        {
            var line=Lanes[Math.Max(0,Math.Min(2,lane))];waypoint=Math.Max(0,Math.Min(line.Length-1,waypoint));V2 direction=(line[Math.Min(line.Length-1,waypoint+1)]-line[Math.Max(0,waypoint-1)]).Normal*(team==0?1:-1);
            float lateral=slot==3?0:(slot-1)*1.4f;return Free(line[waypoint]+new V2(-direction.y,direction.x)*lateral-direction*(slot==3?2.5f:0));
        }
        public static List<V2> Path(V2 start,V2 end)
        {
            start=Free(start);end=Free(end);var path=new List<V2>();if(Clear(start,end)){path.Add(end);return path;}
            int s=Cell(start.x)+Cell(start.y)*60,g=Cell(end.x)+Cell(end.y)*60;var open=new List<int>{s};var parent=new int[3600];var cost=new float[3600];var closed=new bool[3600];
            for(int i=0;i<3600;i++){cost[i]=float.MaxValue;parent[i]=-1;}cost[s]=0;
            int[] dx={1,-1,0,0,1,1,-1,-1},dy={0,0,1,-1,1,-1,1,-1};int visited=0;
            while(open.Count>0&&visited++<3600)
            {
                int bi=0;float best=float.MaxValue;for(int i=0;i<open.Count;i++){float f=cost[open[i]]+V2.Distance(Point(open[i]),end);if(f<best){bi=i;best=f;}}
                int n=open[bi];open.RemoveAt(bi);if(n==g){for(int c=g;c!=s&&c>=0;c=parent[c])path.Add(Point(c));path.Reverse();path.Add(end);return Simplify(start,path);}
                closed[n]=true;int x=n%60,y=n/60;
                for(int k=0;k<8;k++){int nx=x+dx[k],ny=y+dy[k];if(nx<1||ny<1||nx>58||ny>58||blocked[nx,ny])continue;if(k>=4&&(blocked[nx,y]||blocked[x,ny]))continue;int v=nx+ny*60;if(closed[v])continue;float c=cost[n]+(k<4?2:2.828f);if(c>=cost[v])continue;cost[v]=c;parent[v]=n;if(!open.Contains(v))open.Add(v);}
            }
            return path;
        }
        static List<V2> Simplify(V2 start,List<V2> raw)
        {
            var result=new List<V2>();int index=0;while(index<raw.Count){int far=index;while(far+1<raw.Count&&Clear(start,raw[far+1]))far++;result.Add(raw[far]);start=raw[far];index=far+1;}return result;
        }
    }
}
