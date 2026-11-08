using System;
using System.Collections.Generic;
using System.Linq;
namespace Rift.Core
{
    public partial class Match
    {
        readonly List<Unit>[] cells=new List<Unit>[900];
        readonly List<int> usedCells=new List<int>();
        static int Grid(float x)=>Math.Max(0,Math.Min(29,(int)((x+60)/4)));
        void IndexUnits()
        {
            foreach(int cell in usedCells)cells[cell].Clear();usedCells.Clear();
            foreach(var u in Units){if(u.dead)continue;int index=Grid(u.pos.x)+Grid(u.pos.y)*30;if(cells[index]==null)cells[index]=new List<Unit>();if(cells[index].Count==0)usedCells.Add(index);cells[index].Add(u);}
        }
        IEnumerable<Unit> Around(V2 p,float range)
        {
            for(int y=Grid(p.y-range);y<=Grid(p.y+range);y++)for(int x=Grid(p.x-range);x<=Grid(p.x+range);x++){var list=cells[x+y*30];if(list==null)continue;foreach(var u in list)if(!u.dead)yield return u;}
        }
        public static bool Building(Unit u)=>u.kind==Kind.Tower||u.kind==Kind.Barracks||u.kind==Kind.Ancient;
        public static float BodyRadius(Unit u)=>u.kind==Kind.Hero?.9f:u.kind==Kind.Creep?.6f:u.kind==Kind.Neutral?.75f:u.kind==Kind.Summon?(u.hero==0?.4f:.7f):u.kind==Kind.Roshan?1.9f:u.kind==Kind.Tower?1.5f:u.kind==Kind.Barracks?1.8f:u.kind==Kind.Ancient?2.7f:0;
        static float Reach(Unit u,Unit target)=>u.range+(Building(target)?BodyRadius(target):target.kind==Kind.Roshan?1.3f:0);
        bool Protected(Unit target)
        {
            if(target.kind==Kind.Ancient)return Units.Any(t=>t.kind==Kind.Tower&&t.team==target.team&&!t.dead&&t.waypoint==2);
            if(target.kind==Kind.Tower)return Units.Any(t=>t.kind==Kind.Tower&&t.team==target.team&&t.lane==target.lane&&!t.dead&&t.waypoint<target.waypoint);
            if(target.kind==Kind.Barracks)return Units.Any(t=>t.kind==Kind.Tower&&t.team==target.team&&t.lane==target.lane&&!t.dead);return false;
        }
        Unit Nearest(Unit u,float range)
        {
            Unit best=null;float score=float.MaxValue;
            foreach(var t in Around(u.pos,range+3))
            {
                if(t.dead||t.team==u.team||t.kind==Kind.Web||t.kind==Kind.Rune||t.kind==Kind.Trap||t.kind==Kind.Courier||Protected(t))continue;
                float distance=V2.Distance(u.pos,t.pos);if(distance>range+(Building(t)?BodyRadius(t):0)||t.invisible>0&&!Visible(t,u.team))continue;
                if(u.team==2){if(t.team==2||Building(t)||V2.Distance(t.pos,u.home)>11)continue;}
                else if(t.team==2&&(u.kind!=Kind.Hero||distance>2))continue;
                float priority=(t.kind==Kind.Creep||t.kind==Kind.Summon)?-5:Building(t)?9:0;
                if(u.kind==Kind.Hero)priority=Building(t)?6:0;
                float value=distance+priority;if(t.id==u.aggroTarget&&u.aggroUntil>Time)value-=50;
                if(value<score){best=t;score=value;}
            }
            return best;
        }
        void RaiseAggro(Unit source,Unit victim)
        {
            if(source==null||source.dead||source.team==victim.team)return;
            if((victim.kind==Kind.Creep||victim.kind==Kind.Summon)&&(source.kind==Kind.Hero||source.team==2)){if(victim.aggroTarget!=source.id||victim.aggroUntil<Time)victim.aggroOrigin=victim.pos;victim.aggroTarget=source.id;victim.aggroUntil=Time+2.5f;}
            if(victim.team==2)
            {
                foreach(var guard in Around(victim.home,8))if(guard.team==2&&(guard.kind==Kind.Neutral||guard.kind==Kind.Roshan)&&!guard.returning){guard.aggroTarget=source.id;guard.aggroUntil=Time+5;guard.target=source.id;}
                return;
            }
            if(source.kind!=Kind.Hero||victim.kind!=Kind.Hero)return;
            foreach(var guard in Around(victim.pos,12))if(guard.team==victim.team&&(guard.kind==Kind.Tower||guard.kind==Kind.Creep)&&V2.Distance(guard.pos,source.pos)<(guard.kind==Kind.Tower?guard.range:6))
            {if(guard.aggroTarget!=source.id||guard.aggroUntil<Time)guard.aggroOrigin=guard.pos;guard.aggroTarget=source.id;guard.aggroUntil=Time+(guard.kind==Kind.Tower?3:2.5f);}
        }
        void CombatMove(Unit u,float dt)
        {
            if(u.invisible>0&&u.target==0)return;
            if(u.aggroTarget!=0&&(u.kind==Kind.Creep||u.kind==Kind.Summon||u.kind==Kind.Tower)&&(u.aggroUntil<=Time||u.kind!=Kind.Tower&&V2.Distance(u.pos,u.aggroOrigin)>9))
            {
                if(u.target==u.aggroTarget){u.target=0;u.moving=false;u.path.Clear();if(u.kind!=Kind.Tower)u.acquireDelay=.9f;}
                u.aggroTarget=0;u.aggroUntil=0;
            }
            bool neutral=u.kind==Kind.Neutral||u.kind==Kind.Roshan;
            if(neutral&&(V2.Distance(u.pos,u.home)>12||u.returning))
            {
                u.returning=true;u.target=u.aggroTarget=0;CancelAttack(u);u.hp=Math.Min(u.maxHp,u.hp+u.maxHp*.18f*dt);
                if(V2.Distance(u.pos,u.home)<1.5f){u.returning=false;u.hp=u.maxHp;u.moving=false;u.path.Clear();u.aggroUntil=0;return;}
                if(u.repath<=0||!u.moving){Move(u,u.home);u.repath=.5f;}StepMovement(u,dt);return;
            }
            if(u.attackWindup>0)
            {
                var victim=Find(u.attackVictim);
                if(victim==null||victim.dead||!Visible(victim,u.team)||V2.Distance(u.pos,victim.pos)>Reach(u,victim)+1){CancelAttack(u);return;}
                u.facing=(victim.pos-u.pos).Normal;u.attackWindup=Math.Max(0,u.attackWindup-dt);if(u.attackWindup<=0)Attack(u,victim);return;
            }
            if(u.followTarget!=0)
            {
                var friend=Find(u.followTarget);if(friend==null||friend.dead){u.followTarget=0;u.moving=false;u.path.Clear();return;}
                if(V2.Distance(u.pos,friend.pos)>3){if(u.repath<=0&&(!u.moving||V2.Distance(u.goal,friend.pos)>1)){Move(u,friend.pos);u.repath=.35f;}}
                else{u.moving=false;u.path.Clear();u.facing=(friend.pos-u.pos).Normal;}
            }
            if(u.kind==Kind.Summon)
            {
                var owner=Find(u.owner);if(owner!=null&&!owner.dead)
                {
                    var assisted=Find(owner.target);
                    if(owner.retreating){u.target=0;u.followTarget=owner.id;}
                    else{if(u.followTarget==owner.id)u.followTarget=0;if(assisted!=null&&!assisted.dead&&assisted.team!=u.team&&V2.Distance(u.pos,assisted.pos)<18)u.target=assisted.id;}
                }
            }
            var target=Find(u.target);
            if(!u.manualTarget&&u.aggroUntil>Time){var aggro=Find(u.aggroTarget);if(aggro!=null&&!aggro.dead&&V2.Distance(u.pos,aggro.pos)<(u.kind==Kind.Tower?u.range:10))target=aggro;}
            bool laneUnit=u.kind==Kind.Creep||u.kind==Kind.Summon;
            if(target!=null&&(target.dead||!Visible(target,u.team)||!u.manualTarget&&Protected(target)||laneUnit&&(V2.Distance(u.pos,target.pos)>14||u.kind==Kind.Creep&&LaneDistance(target.pos,u.lane)>12)||u.kind==Kind.Tower&&V2.Distance(u.pos,target.pos)>u.range))
            {target=null;u.target=0;u.manualTarget=false;u.moving=false;u.path.Clear();}
            bool canAcquire=u.acquireDelay<=0&&u.followTarget==0&&!u.retreating&&(!u.moving||u.attackMoving||laneUnit||u.kind==Kind.Tower||neutral);
            if(target==null&&canAcquire&&(!neutral||u.aggroUntil>Time||Nearest(u,3)!=null))target=Nearest(u,u.kind==Kind.Tower||u.holdPosition?u.range:laneUnit?9:Math.Max(u.range,8));
            if(target!=null)
            {
                u.target=target.id;float distance=V2.Distance(u.pos,target.pos),reach=Reach(u,target);
                if(distance<=reach){u.moving=false;u.path.Clear();u.facing=(target.pos-u.pos).Normal;if(u.attackTimer<=0)BeginAttack(u,target);return;}
                if(u.kind!=Kind.Tower&&!u.holdPosition&&u.repath<=0){Move(u,BattlePosition(u,target));u.repath=.3f;}
            }
            else if(u.attackMoving&&!u.moving){if(V2.Distance(u.pos,u.attackGoal)>1)Move(u,u.attackGoal);else u.attackMoving=false;}
            if(target==null&&laneUnit)
            {
                var line=BattleMap.Lanes[u.lane];u.waypoint=Math.Max(0,Math.Min(line.Length-1,u.waypoint));int step=u.team==0?1:-1;
                bool reached=V2.Distance(u.pos,line[u.waypoint])<3.8f;
                if(reached&&u.waypoint+step>=0&&u.waypoint+step<line.Length){u.waypoint+=step;u.moving=false;}
                if(!u.moving){var goal=BattleMap.LanePoint(u.lane,u.waypoint,u.team,u.formationSlot);if(reached&&(u.waypoint==0&&u.team==1||u.waypoint==line.Length-1&&u.team==0))goal=BattleMap.Bases[1-u.team];Move(u,goal);}
            }
            StepMovement(u,dt);
        }
        static float LaneDistance(V2 pos,int lane){float distance=float.MaxValue;var line=BattleMap.Lanes[lane];for(int i=1;i<line.Length;i++)distance=Math.Min(distance,BattleMap.Segment(pos,line[i-1],line[i]));return distance;}
        V2 BattlePosition(Unit u,Unit target)
        {
            float radius=Math.Max(BodyRadius(u)+BodyRadius(target)+.1f,Reach(u,target)*(u.range>4?.82f:.85f));
            float angle=(float)Math.Atan2(u.pos.y-target.pos.y,u.pos.x-target.pos.x);V2 best=u.pos;float bestScore=float.MaxValue;
            for(int i=0;i<16;i++)
            {
                float a=angle+(i%2==0?1:-1)*((i+1)/2)*(float)Math.PI/8;var point=target.pos+new V2((float)Math.Cos(a),(float)Math.Sin(a))*radius;if(!BattleMap.Walkable(point))continue;
                float score=V2.Distance(u.pos,point);
                foreach(var other in Around(point,3))if(other.id!=u.id&&other.id!=target.id&&BodyRadius(other)>0){float overlap=BodyRadius(u)+BodyRadius(other)+.15f-V2.Distance(point,other.pos);if(overlap>0)score+=overlap*12;}
                if(score<bestScore){bestScore=score;best=point;}
            }
            return best;
        }
        void StepMovement(Unit u,float dt)
        {
            if(!u.moving||u.path.Count==0)return;
            if(!BattleMap.Walkable(u.pos))u.pos=BattleMap.Free(u.pos);
            var delta=u.path[0]-u.pos;if(delta.Length<.3f){u.path.RemoveAt(0);if(u.path.Count==0)u.moving=false;return;}
            V2 desired=delta.Normal,avoid=new V2();float radius=BodyRadius(u);
            foreach(var other in Around(u.pos,4))
            {
                if(other.id==u.id||BodyRadius(other)<=0)continue;V2 away=u.pos-other.pos;float distance=away.Length,safe=radius+BodyRadius(other);
                if(distance<.001f)away=new V2((u.id&1)==0?1:-1,.3f);
                if(distance<safe+1.1f&&V2.Dot(desired,away.Normal)<.25f){float weight=Math.Max(0,(safe+1.1f-distance)/1.1f);avoid+=away.Normal*weight;if(V2.Dot(desired,away.Normal)<-.65f)avoid+=new V2(desired.y,-desired.x)*weight*.9f;}
            }
            V2 direction=(desired+avoid*.8f).Normal;float speed=u.speed*(u.slow>0?.55f:1)*(u.haste>0?1.5f:1),step=Math.Min(delta.Length,speed*dt);var before=u.pos;
            if(!Offset(u,direction*step)){if(!Offset(u,new V2(-desired.y,desired.x)*step))Offset(u,new V2(desired.y,-desired.x)*step);}
            if(V2.Distance(before,u.pos)>.001f){u.facing=(u.pos-before).Normal;if(u.kind==Kind.Hero)u.invisible=0;}
        }
        static bool Offset(Unit u,V2 displacement)
        {
            var point=V2.Clamp(u.pos+displacement);if(!BattleMap.Walkable(point)||!BattleMap.Clear(u.pos,point))return false;u.pos=point;return true;
        }
        static float Yield(Unit u)=>Building(u)||u.channel>0?0:u.stun>0?.15f:u.kind==Kind.Hero&&!u.moving?0:u.moving?1:.4f;
        void ResolveCrowding(float dt)
        {
            for(int pass=0;pass<3;pass++)
            {
                IndexUnits();foreach(var u in Units)
                {
                    float radius=BodyRadius(u);if(u.dead||radius<=0||Building(u))continue;
                    foreach(var other in Around(u.pos,5))
                    {
                        if(other.id==u.id||other.id<u.id&&!Building(other))continue;float r=BodyRadius(other);if(r<=0)continue;
                        V2 delta=u.pos-other.pos;float length=delta.Length,required=radius+r;if(length>=required-.02f)continue;
                        V2 normal=length>.001f?delta/length:new V2((float)Math.Cos((u.id*17+other.id*31)*.7f),(float)Math.Sin((u.id*17+other.id*31)*.7f));
                        float a=Yield(u),b=Yield(other);if(Building(other)&&u.channel<=0)a=1;if(a+b<=0)continue;
                        float correction=Math.Min(required-length,.18f+dt*3);if(a>0)Offset(u,normal*(correction*a/(a+b)));if(b>0)Offset(other,normal*(-correction*b/(a+b)));
                    }
                }
            }
        }
    }
}
