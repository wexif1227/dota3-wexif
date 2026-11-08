using System;
using System.Collections.Generic;

namespace Rift.Core
{
    [Serializable] public struct V2
    {
        public float x, y;
        public V2(float x, float y) { this.x = x; this.y = y; }
        public float Length => (float)Math.Sqrt(x*x+y*y);
        public V2 Normal => Length > .001f ? this / Length : new V2();
        public static V2 operator +(V2 a,V2 b)=>new V2(a.x+b.x,a.y+b.y);
        public static V2 operator -(V2 a,V2 b)=>new V2(a.x-b.x,a.y-b.y);
        public static V2 operator *(V2 a,float n)=>new V2(a.x*n,a.y*n);
        public static V2 operator /(V2 a,float n)=>new V2(a.x/n,a.y/n);
        public static float Distance(V2 a,V2 b)=>(a-b).Length;
        public static float Dot(V2 a,V2 b)=>a.x*b.x+a.y*b.y;
        public static V2 Clamp(V2 p)=>new V2(Math.Max(-57,Math.Min(57,p.x)),Math.Max(-57,Math.Min(57,p.y)));
    }
    public enum Kind { Hero, Creep, Tower, Barracks, Ancient, Neutral, Roshan, Summon, Ward, Trap, Web, Rune, Courier }
    [Serializable] public class Unit
    {
        public int id, team, hero, owner, lane, waypoint, level=1, gold, xp, kills, deaths, assists, lastHits, denies, souls, shield;
        public Kind kind;
        public string name="";
        public V2 pos, goal, facing=new V2(1,1), home;
        public float hp,maxHp,mp,maxMp,damage,armor,speed,range,attackTimer,respawn,stun,slow,haste,immune,invisible,regen,life,channel,channelTotal,buyback,delivery,hitFlash,silence,reflect,hunger,lastDamage,think;
        public int target, channelTarget, channelSpell=-1, skillPoints=1, talentPoints, stashCount;
        public int attackSequence,attackVictim,followTarget;
        public float attackStarted,attackPoint,attackLength,attackWindup,acquireDelay;
        public V2 attackGoal,aggroOrigin;
        public int formationSlot,aggroTarget;
        public float repath,aggroUntil;
        public bool retreating,returning,manualTarget;
        public bool bot, moving, rot, aghanim, shard, dead, attacking, delivered, aegis,attackMoving,holdPosition;
        public int[] skills=new int[6], items=new int[6], stash=new int[6], talents=new int[]{-1,-1,-1,-1};
        public float[] cooldown=new float[6], itemCooldown=new float[6];
        public List<V2> path=new List<V2>();
    }
    [Serializable] public class Player
    {
        public int id,team,hero,unit;
        public string name="Player";
        public bool connected=true;
    }
    [Serializable] public class Order
    {
        public string type="";
        public int slot,target,value;
        public float x,y;
        public V2 Point=>new V2(x,y);
    }
    [Serializable] public class Effect
    {
        public int id,team,source,target;
        public string type="";
        public V2 a,b;
        public float radius,time=.5f,duration;
    }
    [Serializable] public class Snapshot
    {
        public string type="state", room="",phase="lobby",message="";
        public int you,host,size=5,difficulty=1,winner=-1, radiantKills,direKills;
        public float time;
        public List<Player> players=new List<Player>();
        public List<Unit> units=new List<Unit>();
        public List<Effect> effects=new List<Effect>();
        public List<string> feed=new List<string>();
    }
    [Serializable] public class Wire
    {
        public string type="",room="",name="",token="",message="";
        public int hero,team,size=5,difficulty=1,player;
        public Order order;
    }
    public class Ability
    {
        public string name,description;
        public float mana,cooldown,range;
        public bool passive;
        public Ability(string n,string d,float m,float cd,float r=12,bool p=false) {name=n;description=d;mana=m;cooldown=cd;range=r;passive=p;}
    }
    public class HeroDef
    {
        public string name,title,role,description,color;
        public float hp,mp,damage,range,speed;
        public Ability[] abilities;
    }
    public class ItemDef
    {
        public int id,cost;
        public string name,description,active="",color="D7BD7C";
        public float hp,mp,damage,armor,speed,cooldown;
        public int[] recipe=new int[0];
    }
}
