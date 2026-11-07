using System;
using System.Collections.Generic;
namespace Rift.Core
{
    public static class BattleMap
    {
        public const float Size=120;
        public static readonly V2[] Bases={new V2(-47,-47),new V2(47,47)};
        public static readonly V2[][] Lanes={
            new[]{new V2(-44,-41),new V2(-46,-20),new V2(-46,10),new V2(-40,39),new V2(-14,46),new V2(16,46),new V2(41,44)},
            new[]{new V2(-41,-41),new V2(-28,-28),new V2(-14,-14),new V2(0,0),new V2(14,14),new V2(28,28),new V2(41,41)},
            new[]{new V2(-41,-44),new V2(-16,-46),new V2(14,-46),new V2(40,-39),new V2(46,-10),new V2(46,20),new V2(44,41)}};
        public static readonly V2[] Camps={new V2(-30,-8),new V2(-16,-34),new V2(-31,22),new V2(30,8),new V2(16,34),new V2(31,-22)};
        public static readonly List<V2> Trees=new List<V2>();
        static readonly bool[,] blocked=new bool[60,60];
        static BattleMap()
        {
            var random=new Random(7419);
            for(int i=0;i<950;i++)
            {
                var p=new V2((float)random.NextDouble()*110-55,(float)random.NextDouble()*110-55);
                if(Math.Abs(p.x+p.y)<7 || V2.Distance(p,Bases[0])<14||V2.Distance(p,Bases[1])<14)continue;
                bool clear=true;
                foreach(var lane in Lanes)for(int j=1;j<lane.Length;j++)if(Segment(p,lane[j-1],lane[j])<5)clear=false;
                foreach(var c in Camps)if(V2.Distance(c,p)<6)clear=false;
                if(V2.Distance(p,new V2(-13,13))<8)clear=false;
                if(!clear)continue;
                Trees.Add(p);blocked[Cell(p.x),Cell(p.y)]=true;
            }
        }
        public static float Segment(V2 p,V2 a,V2 b)
        {V2 d=b-a;float t=Math.Max(0,Math.Min(1,V2.Dot(p-a,d)/Math.Max(.001f,V2.Dot(d,d))));return V2.Distance(p,a+d*t);}
        static int Cell(float a)=>Math.Max(0,Math.Min(59,(int)((a+60)/2)));
        static V2 Point(int id)=>new V2((id%60)*2-59,(id/60)*2-59);
        public static bool Walkable(V2 p)=>!blocked[Cell(p.x),Cell(p.y)];
        public static V2 Free(V2 p)
        {
            p=V2.Clamp(p);if(Walkable(p))return p;
            for(int r=1;r<8;r++)for(int k=0;k<16;k++){var q=p+new V2((float)Math.Cos(k*Math.PI/8),(float)Math.Sin(k*Math.PI/8))*r;if(Walkable(q))return V2.Clamp(q);}
            return p;
        }
        public static List<V2> Path(V2 start,V2 end)
        {
            end=Free(end);var path=new List<V2>();
            bool direct=true;float distance=V2.Distance(start,end);
            for(float t=1;t<distance;t+=1)if(!Walkable(start+(end-start).Normal*t)){direct=false;break;}
            if(direct){path.Add(end);return path;}
            int s=Cell(start.x)+Cell(start.y)*60,g=Cell(end.x)+Cell(end.y)*60;
            var open=new List<int>{s};var parent=new int[3600];var cost=new float[3600];var closed=new bool[3600];
            for(int i=0;i<3600;i++){cost[i]=float.MaxValue;parent[i]=-1;}cost[s]=0;
            int[] dx={1,-1,0,0,1,1,-1,-1},dy={0,0,1,-1,1,-1,1,-1};int visited=0;
            while(open.Count>0&&visited++<3600)
            {
                int bi=0;float best=float.MaxValue;
                for(int i=0;i<open.Count;i++){float f=cost[open[i]]+V2.Distance(Point(open[i]),end);if(f<best){bi=i;best=f;}}
                int n=open[bi];open.RemoveAt(bi);if(n==g){for(int c=g;c!=s&&c>=0;c=parent[c])path.Add(Point(c));path.Reverse();path.Add(end);return path;}
                closed[n]=true;int x=n%60,y=n/60;
                for(int k=0;k<8;k++){int nx=x+dx[k],ny=y+dy[k];if(nx<1||ny<1||nx>58||ny>58||blocked[nx,ny])continue;
                    if(k>=4&&(blocked[nx,y]||blocked[x,ny]))continue;
                    int v=nx+ny*60;if(closed[v])continue;float c=cost[n]+(k<4?2:2.828f);if(c>=cost[v])continue;cost[v]=c;parent[v]=n;if(!open.Contains(v))open.Add(v);}
            }
            return path;
        }
    }
}
