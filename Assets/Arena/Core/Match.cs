using System;
using System.Collections.Generic;
using System.Linq;
namespace Rift.Core
{
    public partial class Match
    {
        public readonly List<Unit> Units=new List<Unit>();
        public readonly List<Player> Players=new List<Player>();
        public readonly List<Effect> Effects=new List<Effect>();
        public readonly List<string> Feed=new List<string>();
        public string Phase="lobby",Room="LOCAL";
        public int Size=5,Difficulty=1,Host=1,Winner=-1;
        public float Time;
        int nextId=1,nextEffect=1,wave=-1,runeWave=-1,campWave=-1;
        readonly Random random=new Random(123);
        sealed class FlyingAttack{public Unit source,target;public float damage,remaining;}
        readonly List<FlyingAttack> flyingAttacks=new List<FlyingAttack>();
        public Match(int size=5,int difficulty=1){Size=size==1||size==2?size:5;Difficulty=Math.Max(0,Math.Min(2,difficulty));}
        public Player AddPlayer(int id,string name,int hero,int team)
        {
            if(Phase!="lobby"||Players.Count>=Size*2)return null;
            team=team==1?1:0;if(Players.Count(p=>p.team==team)>=Size)team=1-team;
            name=(name??"").Replace("<","").Replace(">","").Replace("\n"," ").Replace("\r"," ");
            var p=new Player{id=id,name=string.IsNullOrWhiteSpace(name)?"Player":name.Substring(0,Math.Min(20,name.Length)),hero=Math.Max(0,Math.Min(5,hero)),team=team};Players.Add(p);return p;
        }
        public Unit Find(int id)=>Units.Find(u=>u.id==id);
        public Unit Hero(int player)=>Units.Find(u=>u.owner==player&&u.kind==Kind.Hero);
        public void Start()
        {
            if(Phase!="lobby")return;Phase="playing";
            for(int team=0;team<2;team++)
            {
                for(int i=0;i<Size;i++)
                {
                    var ps=Players.Where(p=>p.team==team).ToList();Player p=i<ps.Count?ps[i]:null;
                    int h=p!=null?p.hero:(team*3+i)%6;
                    var u=Spawn(Kind.Hero,team,BattleMap.Bases[team]+new V2(i*2-4,0));u.hero=h;u.owner=p!=null?p.id:-(team*Size+i+1);u.name=p!=null?p.name:Catalog.Heroes[h].name;u.bot=p==null;u.gold=650;u.lane=Size==1?1:i%3;
                    u.skillPoints=1;Stats(u);u.hp=u.maxHp;u.mp=u.maxMp;if(p!=null)p.unit=u.id;
                }
                var core=Spawn(Kind.Ancient,team,BattleMap.Bases[team]);core.name="Ancient";core.hp=core.maxHp=4500;core.armor=12;
                for(int lane=0;lane<3;lane++)
                {
                    if(Size==1&&lane!=1)continue;
                    var points=BattleMap.Lanes[lane];
                    for(int tier=0;tier<3;tier++)
                    {int index=team==0?(tier==0?2:tier==1?1:0):(tier==0?4:tier==1?5:6);var t=Spawn(Kind.Tower,team,points[index]);t.lane=lane;t.waypoint=tier;t.name="Tower "+(tier+1);t.hp=t.maxHp=1100+tier*250;t.damage=80+tier*15;t.range=11;t.armor=10;}
                    var b=Spawn(Kind.Barracks,team,points[team==0?0:6]+new V2(lane==2?-3:3,lane==0?-3:3));b.lane=lane;b.hp=b.maxHp=1400;b.armor=8;b.name="Barracks";
                }
            }
            var roshan=Spawn(Kind.Roshan,2,BattleMap.Roshan);roshan.name="Roshan";roshan.hp=roshan.maxHp=6500;roshan.damage=120;roshan.armor=16;roshan.range=3;roshan.speed=4;
            Log("Битва началась. Защитите свой Древний.");
        }
        public Unit Spawn(Kind kind,int team,V2 pos)
        {var u=new Unit{id=nextId++,kind=kind,team=team,pos=pos,goal=pos,home=pos,hp=300,maxHp=300,damage=24,speed=5,range=2,skillPoints=0};Units.Add(u);return u;}
        public void Stats(Unit u)
        {
            if(u.kind!=Kind.Hero)return;var h=Catalog.Heroes[u.hero];float old=u.maxHp;
            u.maxHp=h.hp+(u.level-1)*65+(u.hero==3?u.souls*40:0);u.maxMp=h.mp+(u.level-1)*28;u.damage=h.damage+(u.level-1)*3+(u.hero==1?u.souls*2:0);u.speed=h.speed;u.armor=2+(u.level-1)*.45f;u.range=h.range;
            foreach(int id in u.items){var i=Catalog.Item(id);u.maxHp+=i.hp;u.maxMp+=i.mp;u.damage+=i.damage;u.armor+=i.armor;}
            float boots=0;foreach(int id in u.items)boots=Math.Max(boots,Catalog.Item(id).speed);u.speed+=boots;
            for(int tier=0;tier<4;tier++)if(u.talents[tier]>=0){int side=u.talents[tier];if(tier==0){if(side==0)u.maxHp+=180;else u.maxMp+=160;}if(tier==1){if(side==0)u.damage+=18;else u.armor+=5;}if(tier==2){if(side==0)u.speed*=1.12f;else u.damage+=30;}if(tier==3&&side==0)u.maxHp+=350;}
            u.aghanim=Catalog.Has(u,19);u.hp=Math.Min(u.hp,u.maxHp);u.mp=Math.Min(u.mp,u.maxMp);
        }
        public string Command(int player,Order o)
        {
            if(o==null)return "Пустая команда";
            if(o.type=="start"){if(player!=Host)return "Только лидер комнаты может начать матч";Start();return "";}
            if(Phase!="playing")return "Матч ещё не начат";
            var u=Hero(player);if(u==null)return "Герой не найден";
            if(float.IsNaN(o.x)||float.IsInfinity(o.x)||float.IsNaN(o.y)||float.IsInfinity(o.y))return "Неверная точка";
            if(o.type=="buy")return Buy(u,o.value);
            if(o.type=="sell")return Sell(u,o.slot);
            if(o.type=="learn")return Learn(u,o.slot);
            if(o.type=="talent")return Talent(u,o.slot,o.value);
            if(o.type=="buyback") {int price=200+u.level*80;if(!u.dead||u.buyback>0||u.gold<price)return "Выкуп недоступен";u.gold-=price;u.buyback=180;Revive(u);return "";}
            if(o.type=="courier")
            {if(u.delivery>0)return "Курьер уже в пути";if(!u.stash.Any(i=>i!=0))return "Тайник пуст";var c=Spawn(Kind.Courier,u.team,BattleMap.Bases[u.team]);c.owner=u.id;c.speed=12;c.hp=c.maxHp=180;c.life=90;u.delivery=V2.Distance(c.pos,u.pos)/c.speed+1;return "";}
            if(u.dead)return "Герой погиб";
            if(u.stun>0)return "Герой оглушён";
            if(o.type=="move"||o.type=="attackmove")
            {Cancel(u);u.target=0;u.followTarget=0;u.acquireDelay=0;u.holdPosition=false;u.manualTarget=false;u.retreating=false;u.attackMoving=o.type=="attackmove";u.attackGoal=BattleMap.Free(o.Point);u.invisible=0;Move(u,o.Point);return "";}
            if(o.type=="stop"||o.type=="hold")
            {Cancel(u);u.moving=false;u.path.Clear();u.target=u.followTarget=0;u.manualTarget=false;u.attackMoving=false;u.holdPosition=o.type=="hold";u.acquireDelay=o.type=="stop"?float.MaxValue:0;return "";}
            if(o.type=="follow")
            {var friend=Find(o.target);if(friend==null||friend.dead||friend.id==u.id||friend.team!=u.team)return "Нельзя следовать за целью";Cancel(u);u.target=0;u.followTarget=friend.id;u.acquireDelay=0;u.attackMoving=u.holdPosition=false;return "";}
            if(o.type=="attack")
            {var t=Find(o.target);if(t==null||t.dead||t.id==u.id||t.kind==Kind.Web||t.kind==Kind.Rune||!Visible(t,u.team))return "Цель не видна";if(t.team==u.team&&(t.kind!=Kind.Creep||t.hp>t.maxHp*.5f))return "Добивание доступно ниже 50% HP";if(u.target!=t.id)Cancel(u);u.retreating=false;u.target=t.id;u.manualTarget=true;u.followTarget=0;u.acquireDelay=0;u.attackMoving=u.holdPosition=false;u.moving=false;u.path.Clear();return "";}
            if(o.type=="cast")return Cast(u,o.slot,o.Point,o.target);
            if(o.type=="item")return UseItem(u,o.slot,o.Point,o.target);
            return "Неизвестная команда";
        }
        void Cancel(Unit u){u.channel=0;u.channelSpell=-1;u.channelTarget=0;CancelAttack(u);}
        void CancelAttack(Unit u){if(u.attackWindup>0)u.attackTimer=0;u.attackWindup=0;u.attackVictim=0;u.attacking=false;}
        public void Move(Unit u,V2 p){p=BattleMap.Free(p);if(u.moving&&u.path.Count>0&&V2.Distance(u.goal,p)<.35f)return;u.goal=p;u.path=BattleMap.Path(u.pos,u.goal);u.moving=u.path.Count>0;}
        public void Tick(float dt)
        {
            if(Phase!="playing")return;dt=Math.Max(0,Math.Min(.1f,dt));Time+=dt;
            foreach(var e in Effects)e.time-=dt;Effects.RemoveAll(e=>e.time<=0);
            int w=(int)(Time/30);if(w>wave){wave=w;SpawnWave();}
            int rw=(int)(Time/120);if(rw>runeWave){runeWave=rw;SpawnRunes();}
            int cw=(int)(Time/60);if(cw>campWave){campWave=cw;SpawnCamps();}
            IndexUnits();
            foreach(var u in Units.ToArray())
            {
                Dec(ref u.repath,dt);Dec(ref u.attackTimer,dt);Dec(ref u.acquireDelay,dt);Dec(ref u.stun,dt);Dec(ref u.slow,dt);Dec(ref u.haste,dt);Dec(ref u.immune,dt);Dec(ref u.hitFlash,dt);Dec(ref u.buyback,dt);Dec(ref u.silence,dt);Dec(ref u.reflect,dt);Dec(ref u.hunger,dt);Dec(ref u.regen,dt);Dec(ref u.lastDamage,dt);
                if(u.attackTimer<=0&&u.attackWindup<=0)u.attacking=false;
                for(int i=0;i<6;i++){Dec(ref u.cooldown[i],dt);Dec(ref u.itemCooldown[i],dt);}
                if(u.kind==Kind.Courier&&!u.dead)
                {
                    var owner=Find(u.owner);if(owner==null){u.dead=true;continue;}
                    V2 destination=u.delivered?BattleMap.Bases[u.team]:owner.pos;V2 delta=destination-u.pos;
                    u.facing=delta.Normal;u.pos+=delta.Normal*Math.Min(delta.Length,u.speed*dt);owner.delivery=u.delivered?0:delta.Length/u.speed;
                    if(delta.Length<1.5f){if(u.delivered)u.dead=true;else{Deliver(owner);owner.delivery=0;u.delivered=true;}}continue;
                }
                if(u.dead){if(u.kind==Kind.Hero){u.respawn-=dt;if(u.respawn<=0)Revive(u);}else if(u.kind==Kind.Roshan){u.respawn-=dt;if(u.respawn<=0){u.dead=false;u.hp=u.maxHp;u.pos=u.home;}}continue;}
                if(u.life>0){u.life-=dt;if(u.life<=0){u.dead=true;continue;}}
                if(u.kind==Kind.Hero)
                {
                    Stats(u);u.hp=Math.Min(u.maxHp,u.hp+dt*(1.6f+u.level*.15f+(u.regen>0?25:0)+(Catalog.Has(u,24)?u.maxHp*.012f:0)));u.mp=Math.Min(u.maxMp,u.mp+dt*(1.2f+u.level*.08f));
                    if(V2.Distance(u.pos,BattleMap.Bases[u.team])<10){u.hp=Math.Min(u.maxHp,u.hp+dt*u.maxHp*.16f);u.mp=Math.Min(u.maxMp,u.mp+dt*u.maxMp*.2f);Deliver(u);}
                    if((int)(Time*2)!=(int)((Time-dt)*2))u.gold++;
                    if(u.bot&&u.stun<=0&&u.channel<=0)Bot(u,dt);
                    if(u.rot){Area(u,u.pos,u.aghanim?7:4,(22+u.skills[1]*18)*dt,false,1);u.hp=Math.Max(1,u.hp-20*dt);}
                    if(Catalog.Has(u,34))Area(u,u.pos,6,45*dt,false);
                    foreach(var web in Units)if(web.kind==Kind.Web&&!web.dead&&web.team==u.team&&V2.Distance(u.pos,web.pos)<8){u.haste=Math.Max(u.haste,.15f);u.hp=Math.Min(u.maxHp,u.hp+12*dt);}
                }
                if(u.channel>0){CancelAttack(u);Channel(u,dt);continue;}
                if(u.kind==Kind.Trap){var enemy=Nearest(u,5);if(enemy!=null){var owner=Find(u.owner);if(owner!=null)Area(owner,u.pos,6,150+owner.level*6,false,4,owner.shard?1.5f:0);Fx("trap",u.pos,u.pos,6,u.team);u.dead=true;}continue;}
                if(u.kind==Kind.Rune||u.kind==Kind.Web||u.kind==Kind.Ward)continue;
                if(u.stun>0){CancelAttack(u);u.moving=false;continue;}
                if(u.kind==Kind.Hero||u.kind==Kind.Creep||u.kind==Kind.Summon||u.kind==Kind.Neutral||u.kind==Kind.Roshan||u.kind==Kind.Tower)CombatMove(u,dt);
            }
            ResolveCrowding(dt);
            foreach(var rune in Units.Where(u=>u.kind==Kind.Rune&&!u.dead).ToArray())
            foreach(var h in Units.Where(u=>u.kind==Kind.Hero&&!u.dead))if(V2.Distance(h.pos,rune.pos)<2){if(rune.hero==0){h.gold+=100;h.haste=20;}else{h.hp=Math.Min(h.maxHp,h.hp+350);h.mp=Math.Min(h.maxMp,h.mp+250);}rune.dead=true;Fx("rune",rune.pos,rune.pos,3,h.team);break;}
            foreach(var projectile in flyingAttacks.ToArray())
            {
                projectile.remaining-=dt;if(projectile.remaining>0)continue;
                if(!projectile.target.dead)ResolveAttack(projectile.source,projectile.target,projectile.damage);
                flyingAttacks.Remove(projectile);
            }
            Units.RemoveAll(u=>u.dead&&u.kind!=Kind.Hero&&u.kind!=Kind.Roshan&&u.kind!=Kind.Tower&&u.kind!=Kind.Barracks&&u.kind!=Kind.Ancient);
        }
        static void Dec(ref float x,float dt){if(x>0)x=Math.Max(0,x-dt);}
        void Revive(Unit u){u.dead=false;u.hp=u.maxHp;u.mp=u.maxMp;u.pos=BattleMap.Bases[u.team];u.goal=u.pos;u.moving=false;u.target=u.followTarget=0;u.attackMoving=u.holdPosition=u.manualTarget=u.retreating=false;u.path.Clear();u.stun=u.slow=u.silence=u.invisible=0;Cancel(u);u.rot=false;}
        public bool Visible(Unit u,int team)
        {
            if(u.team==team)return true;if(u.dead)return false;
            foreach(var ally in Units){if(ally.team!=team||ally.dead||ally.kind==Kind.Trap||ally.kind==Kind.Web||ally.kind==Kind.Rune)continue;float sight=ally.kind==Kind.Ward?17:ally.kind==Kind.Hero?20:12;
                if(V2.Distance(u.pos,ally.pos)<sight){if(u.invisible<=0&&!(u.kind==Kind.Trap))return true;if(ally.kind==Kind.Tower||(ally.kind==Kind.Ward&&ally.hero==1))return true;}}
            return false;
        }
        public static float AttackPeriod(Unit u)=>u.kind==Kind.Hero?Math.Max(.45f,1.1f-u.level*.015f):1.25f;
        public static float AttackPoint(Unit u)=>Math.Min(AttackPeriod(u)*.65f,u.kind!=Kind.Hero?.3f:new[]{.35f,.5f,.3f,.3f,.55f,.4f}[Math.Max(0,Math.Min(5,u.hero))]);
        void BeginAttack(Unit u,Unit t)
        {
            u.attackTimer=u.attackLength=AttackPeriod(u);u.attackPoint=u.attackWindup=AttackPoint(u);u.attackStarted=Time;u.attackVictim=t.id;u.attackSequence++;u.attacking=true;RaiseAggro(u,t);
        }
        void Attack(Unit u,Unit t)
        {
            if(t.team==u.team&&(t.kind!=Kind.Creep||t.hp>t.maxHp*.5f)){u.target=0;return;}
            float damage=u.damage;u.stashCount++;if(u.kind==Kind.Hero){if(u.hunger>0)damage*=1.55f;if(u.hero==2&&u.shield>0)damage+=25+u.skills[0]*10;if(u.hero==2&&u.invisible>0){damage+=80+u.skills[1]*35;u.invisible=0;}if(Catalog.Has(u,22)&&u.stashCount%4==0)damage*=2;}
            bool ranged=u.range>4;float flight=ranged?Math.Max(.12f,Math.Min(.65f,V2.Distance(u.pos,t.pos)/25)):0;
            if(ranged)flyingAttacks.Add(new FlyingAttack{source=u,target=t,damage=damage,remaining=flight});else ResolveAttack(u,t,damage);
            Fx(ranged?"bolt":"hit",u.pos,t.pos,1,u.team);var hit=Effects[Effects.Count-1];hit.source=u.id;hit.target=t.id;hit.duration=flight;if(ranged)hit.time=flight+.1f;
        }
        void ResolveAttack(Unit u,Unit t,float damage)
        {
            Damage(u,t,damage,true);
            if(u.kind==Kind.Hero)
            {
                if(u.hunger>0||Catalog.Has(u,25))u.hp=Math.Min(u.maxHp,u.hp+damage*(u.hunger>0?.65f:.2f));
                if(u.hero==0&&u.skills[2]>0){t.slow=2;if(u.shard)Damage(u,t,25,false);}
                if(u.hero==4&&u.skills[2]>0&&u.stashCount%4==0){Control(t,.6f+.2f*u.skills[2],0);Damage(u,t,35+u.skills[2]*20,false);}
                if(u.hero==2&&u.skills[2]>0){V2 dir=(t.pos-u.pos).Normal;foreach(var other in Units.ToArray())if(other.id!=t.id&&other.team!=u.team&&!other.dead&&BattleMap.Segment(other.pos,t.pos,t.pos+dir*8)<1.8f)Damage(u,other,damage*.6f,true);}
            }
        }
        public void Damage(Unit source,Unit target,float amount,bool physical,bool reflected=false)
        {
            if(target==null||target.dead||amount<=0)return;
            if(target.kind==Kind.Ancient&&Units.Any(t=>t.kind==Kind.Tower&&t.team==target.team&&!t.dead&&t.waypoint==2))return;
            if(target.kind==Kind.Tower&&Units.Any(t=>t.kind==Kind.Tower&&t.team==target.team&&t.lane==target.lane&&!t.dead&&t.waypoint<target.waypoint))return;
            if(target.kind==Kind.Barracks&&Units.Any(t=>t.kind==Kind.Tower&&t.team==target.team&&t.lane==target.lane&&!t.dead))return;
            if(!physical&&target.immune>0)return;
            if(target.shield>0&&amount>5){target.shield--;Fx("shield",target.pos,target.pos,2,target.team);return;}
            float armor=target.armor;if(source!=null&&Catalog.Has(source,21))armor-=6;
            if(Units.Any(h=>h.kind==Kind.Hero&&h.hero==1&&h.team!=target.team&&!h.dead&&h.skills[4]>0&&V2.Distance(h.pos,target.pos)<12))armor-=4;
            if(Units.Any(h=>h.team==target.team&&!h.dead&&Catalog.Has(h,35)&&V2.Distance(h.pos,target.pos)<12))armor+=3;
            if(physical)amount*=1-.06f*armor/(1+.06f*Math.Abs(armor));else amount*=.75f;
            RaiseAggro(source,target);
            target.hp-=amount;target.hitFlash=.2f;target.lastDamage=3;
            if(target.channelSpell==20)Cancel(target);
            if(target.reflect>0&&!reflected&&source!=null)Damage(target,source,amount*.7f,false,true);
            if(target.hp>0)return;
            target.hp=0;target.dead=true;target.moving=false;target.target=0;Cancel(target);Fx("death",target.pos,target.pos,target.kind==Kind.Hero?4:2,target.team);
            if(target.kind==Kind.Courier){var courierOwner=Find(target.owner);if(courierOwner!=null)courierOwner.delivery=0;}
            if(target.kind==Kind.Ancient){Phase="ended";Winner=1-target.team;Log(Winner==0?"Победа Сил Света":"Победа Сил Тьмы");}
            Unit killer=source;if(killer!=null&&killer.kind==Kind.Summon)killer=Find(killer.owner);
            if(target.kind==Kind.Hero)
            {
                target.deaths++;target.respawn=8+target.level*2;target.gold=Math.Max(0,target.gold-50);target.rot=false;
                if(target.aegis){target.aegis=false;target.respawn=5;}
                if(target.hero==1)target.souls/=2;
                if(killer!=null&&killer.team!=target.team&&killer.kind==Kind.Hero){killer.kills++;killer.gold+=200+target.level*20;GrantXp(killer,200+target.level*35);if(killer.hero==3)killer.souls++;Log(killer.name+" → "+target.name);}
            }
            else if(target.kind==Kind.Roshan){target.respawn=240;if(killer!=null){killer.aegis=true;killer.gold+=400;}Log("Рошан повержен. Эгида получена.");}
            else if(killer!=null&&killer.kind==Kind.Hero)
            {
                bool deny=killer.team==target.team;if(deny)killer.denies++;else{int gold=target.kind==Kind.Tower?250:target.kind==Kind.Barracks?200:target.kind==Kind.Neutral?65:38;killer.gold+=gold;killer.lastHits++;if(killer.hero==1)killer.souls=Math.Min(killer.aghanim?36:24,killer.souls+1);}
            }
            if(target.kind==Kind.Creep||target.kind==Kind.Neutral||target.kind==Kind.Summon)
            {var nearby=Units.Where(h=>h.kind==Kind.Hero&&!h.dead&&h.team!=target.team&&V2.Distance(h.pos,target.pos)<22).ToList();float xp=killer!=null&&killer.team==target.team?24:60;foreach(var h in nearby)GrantXp(h,(int)(xp/Math.Max(1,nearby.Count)));}
        }
        public void GrantXp(Unit u,int xp)
        {
            u.xp+=xp;while(u.level<30&&u.xp>=u.level*110){u.xp-=u.level*110;u.level++;u.skillPoints++;if(u.level==10||u.level==15||u.level==20||u.level==25)u.talentPoints++;u.hp+=65;u.mp+=28;Stats(u);Fx("level",u.pos,u.pos,3,u.team);}
        }
        void Control(Unit t,float stun,float slow){if(t.immune>0)return;if(t.hero==4&&t.haste>0)stun*=.4f;t.stun=Math.Max(t.stun,stun);t.slow=Math.Max(t.slow,slow);if(stun>0)Cancel(t);}
        void Area(Unit u,V2 p,float r,float damage,bool physical=false,float slow=0,float stun=0)
        {foreach(var t in Units.ToArray())if(!t.dead&&t.team!=u.team&&t.kind!=Kind.Rune&&t.kind!=Kind.Web&&V2.Distance(p,t.pos)<r){Damage(u,t,damage,physical);Control(t,stun,slow);}}
        void Fx(string type,V2 a,V2 b,float r,int team){Effects.Add(new Effect{id=nextEffect++,type=type,a=a,b=b,radius=r,team=team,time=type=="hook"?.6f:.45f});}
        void Log(string s){Feed.Insert(0,s);if(Feed.Count>6)Feed.RemoveAt(6);}
        void SpawnWave()
        {
            for(int team=0;team<2;team++)for(int lane=0;lane<3;lane++){if(Size==1&&lane!=1)continue;bool mega=Units.Any(b=>b.kind==Kind.Barracks&&b.team!=team&&b.lane==lane&&b.dead);
                for(int i=0;i<4;i++){var u=Spawn(Kind.Creep,team,BattleMap.Lanes[lane][team==0?0:6]+new V2(i*.6f,0));u.lane=lane;u.formationSlot=i;u.pos=u.goal=u.home=BattleMap.LanePoint(lane,team==0?0:6,team,i);u.waypoint=team==0?1:5;u.hp=u.maxHp=(mega?600:330)+wave*8;u.damage=(mega?45:22)+wave;u.range=i==3?7:1.8f;u.hero=i==3?1:0;}}
        }
        void SpawnCamps(){foreach(var c in BattleMap.Camps){if(Units.Any(u=>u.kind==Kind.Neutral&&!u.dead&&V2.Distance(u.home,c)<4))continue;for(int i=0;i<2;i++){var n=Spawn(Kind.Neutral,2,c+new V2(i*2,0));n.home=c;n.formationSlot=i;n.hp=n.maxHp=450;n.damage=30;n.name="Forest guardian";}}}
        void SpawnRunes(){foreach(var u in Units)if(u.kind==Kind.Rune)u.dead=true;for(int i=0;i<2;i++){var r=Spawn(Kind.Rune,2,BattleMap.Runes[i]);r.hero=(runeWave+i)%2;r.name=r.hero==0?"Bounty / Haste":"Regeneration";}}
        public Snapshot View(int player)
        {
            var p=Players.Find(x=>x.id==player);int team=p!=null?p.team:0;
            return new Snapshot{room=Room,phase=Phase,you=player,host=Host,size=Size,difficulty=Difficulty,winner=Winner,time=Time,players=Players.ToList(),units=Units.Where(u=>Visible(u,team)).ToList(),effects=Effects.Where(e=>e.team==team||Units.Any(u=>u.team==team&&!u.dead&&V2.Distance(u.pos,e.a)<20)).ToList(),feed=Feed.ToList(),radiantKills=Units.Where(u=>u.kind==Kind.Hero&&u.team==0).Sum(u=>u.kills),direKills=Units.Where(u=>u.kind==Kind.Hero&&u.team==1).Sum(u=>u.kills)};
        }
    }
}
