using System;
using System.Linq;
namespace Rift.Core
{
    public partial class Match
    {
        void Bot(Unit u,float dt)
        {
            u.think-=dt;if(u.think>0)return;u.think=Difficulty==0?1.1f:Difficulty==1?.55f:.24f;
            for(int k=0;k<6;k++){int slot=k==3?5:k==5?3:k;if(u.skillPoints>0)Learn(u,slot);}
            for(int tier=0;tier<4;tier++)if(u.talentPoints>0)Talent(u,tier,u.range>4?1:0);
            BotShop(u);float health=u.hp/u.maxHp;
            if(health<(Difficulty==0?.2f:.34f))u.retreating=true;
            if(u.retreating)
            {
                if(health>.78f&&u.mp>u.maxMp*.4f){u.retreating=false;u.moving=false;u.path.Clear();}
                else
                {
                    if(u.rot)Cast(u,1,u.pos,0);CancelAttack(u);u.target=0;u.manualTarget=false;
                    BotItems(u,null);
                    if(u.hero==5&&V2.Distance(u.pos,BattleMap.Bases[u.team])>18&&!Around(u.pos,9).Any(t=>t.team!=u.team&&t.kind==Kind.Hero&&V2.Distance(u.pos,t.pos)<9)&&BotCast(u,1,null,BattleMap.Bases[u.team]))return;
                    Move(u,BattleMap.Bases[u.team]+new V2((u.id%3-1)*2,0));return;
                }
            }
            // Finish an attack's wind-up instead of cancelling it with a new order each think.
            if(u.attackWindup>0)return;
            var nearby=Around(u.pos,18).Where(t=>!t.dead&&V2.Distance(t.pos,u.pos)<18).ToArray();
            var heroes=nearby.Where(t=>t.kind==Kind.Hero&&t.team!=u.team&&Visible(t,u.team)).ToArray();
            int friends=nearby.Count(t=>t.kind==Kind.Hero&&t.team==u.team);
            bool overwhelmed=heroes.Count(t=>V2.Distance(t.pos,u.pos)<10)>friends+1;
            if((overwhelmed||TowerDanger(u,u.pos))&&Difficulty>0)
            {
                CancelAttack(u);u.target=0;u.manualTarget=false;var safe=Units.Where(t=>t.team==u.team&&t.kind==Kind.Tower&&!t.dead).OrderBy(t=>V2.Distance(u.pos,t.pos)).FirstOrDefault();
                var anchor=safe!=null?safe.pos:BattleMap.Bases[u.team];Move(u,BattleMap.Free(anchor+(BattleMap.Bases[u.team]-anchor).Normal*4));return;
            }
            Unit target=null;
            if(Difficulty>0)
            {
                target=nearby.Where(t=>t.kind==Kind.Creep&&t.team!=u.team&&t.team!=2&&t.hp<=LastHitDamage(u,t)*1.05f&&V2.Distance(u.pos,t.pos)<u.range+4).OrderBy(t=>t.hp).FirstOrDefault();
                if(target==null&&Difficulty==2)target=nearby.Where(t=>t.kind==Kind.Creep&&t.team==u.team&&t.hp<Math.Min(t.maxHp*.5f,LastHitDamage(u,t)*.95f)&&V2.Distance(u.pos,t.pos)<u.range+2).OrderBy(t=>t.hp).FirstOrDefault();
            }
            if(target==null)
            {
                var opponent=heroes.Where(t=>V2.Distance(t.pos,u.pos)<12&&!TowerDanger(u,t.pos)).OrderBy(t=>t.hp/t.maxHp+V2.Distance(u.pos,t.pos)*.04f).FirstOrDefault();
                if(opponent!=null&&(opponent.hp<opponent.maxHp*.55f||friends>=heroes.Length&&health>.55f))target=opponent;
            }
            if(target==null)target=Nearest(u,13);
            if(target==null&&Difficulty>0&&Time>45&&health>.65f&&(u.hero==0||u.hero==5)&&heroes.Length==0&&!nearby.Any(t=>t.kind==Kind.Creep&&t.team!=u.team))
                target=nearby.Where(t=>t.kind==Kind.Neutral&&V2.Distance(t.pos,u.pos)<13&&(t.home.x+t.home.y)*(u.team==0?1:-1)<0).OrderBy(t=>V2.Distance(t.pos,u.pos)).FirstOrDefault();
            if(target!=null&&TowerDanger(u,target.pos))target=null;
            if(target!=null)
            {
                u.target=target.id;u.manualTarget=target.team==u.team;
                if(target.team!=u.team&&Difficulty>0)
                {
                    if(BotItems(u,target)||BotAbilities(u,target))return;
                    // Ranged heroes create room between attacks, but keep the next hit in range.
                    if(u.range>4&&target.kind==Kind.Hero&&u.attackTimer>.35f&&V2.Distance(u.pos,target.pos)<u.range*.6f)
                    {u.target=0;Move(u,BattleMap.Free(u.pos+(u.pos-target.pos).Normal*2.5f));return;}
                }
                return;
            }
            u.target=0;u.manualTarget=false;if(u.rot)Cast(u,1,u.pos,0);
            // Use an allied wave as a front line. Offsets give lane partners distinct positions.
            var line=BattleMap.Lanes[u.lane];var front=Units.Where(t=>t.kind==Kind.Creep&&t.team==u.team&&t.lane==u.lane&&!t.dead).OrderByDescending(t=>u.team==0?t.waypoint:6-t.waypoint).ThenBy(t=>V2.Distance(t.pos,BattleMap.Bases[1-u.team])).FirstOrDefault();
            if(front!=null&&V2.Distance(front.pos,u.pos)<24)
            {
                var forward=(BattleMap.Bases[1-u.team]-front.pos).Normal;var point=front.pos-forward*(u.range>4?4:2)+new V2(-forward.y,forward.x)*((u.id%3-1)*2);
                if(!TowerDanger(u,point)&&V2.Distance(point,u.pos)>2)Move(u,point);return;
            }
            if(u.moving)return;int index=0;float nearest=float.MaxValue;for(int i=0;i<line.Length;i++){float distance=V2.Distance(u.pos,line[i]);if(distance<nearest){nearest=distance;index=i;}}
            index=Math.Max(0,Math.Min(line.Length-1,index+(u.team==0?1:-1)));var destination=BattleMap.LanePoint(u.lane,index,u.team,u.id%3);
            if(!TowerDanger(u,destination))Move(u,destination);
        }
        static float LastHitDamage(Unit u,Unit t)=>u.damage*(1-.06f*t.armor/(1+.06f*Math.Abs(t.armor)));
        bool TowerDanger(Unit u,V2 point)
        {
            foreach(var tower in Units)if(tower.kind==Kind.Tower&&tower.team!=u.team&&!tower.dead&&V2.Distance(tower.pos,point)<tower.range+1)
            {
                int shields=Units.Count(t=>!t.dead&&t.team==u.team&&(t.kind==Kind.Creep||t.kind==Kind.Summon)&&V2.Distance(t.pos,tower.pos)<tower.range-.5f);
                if(shields<2||tower.aggroTarget==u.id&&tower.aggroUntil>Time)return true;
            }
            return false;
        }
        void BotShop(Unit u)
        {
            int[] build=u.hero==3||u.hero==4?new[]{8,10,9,14,19,24,18}:u.hero==5?new[]{8,12,15,19,28,24}:new[]{8,11,13,21,18,25};
            foreach(int item in build)
            {
                bool owned=Catalog.Has(u,item)||u.stash.Contains(item)||(item==8&&u.items.Concat(u.stash).Any(id=>id==13||id==14||id==15));
                if(owned)continue;if(u.gold>=Catalog.Item(item).cost)Buy(u,item);break;
            }
            if(u.delivery<=0&&u.stash.Any(i=>i!=0))Command(u.owner,new Order{type="courier"});
        }
        bool BotItems(Unit u,Unit target)
        {
            for(int slot=0;slot<6;slot++)
            {
                if(u.items[slot]==0||u.itemCooldown[slot]>0)continue;string effect=Catalog.Item(u.items[slot]).active;float health=u.hp/u.maxHp;
                bool defensive=(effect=="wand"||effect=="mek"||effect=="satanic")&&health<.65f||effect=="arcane"&&u.mp<u.maxMp*.5f;
                bool fight=target!=null&&target.kind==Kind.Hero&&V2.Distance(u.pos,target.pos)<11;
                bool use=defensive||fight&&((effect=="bkb"||effect=="reflect")&&u.lastDamage>0||effect=="hex"&&target.stun<.3f||effect=="silence"&&target.silence<.3f||effect=="dagon"||effect=="shiva");
                if(use&&UseItem(u,slot,target!=null?target.pos:u.pos,target!=null?target.id:0)=="")return true;
            }
            return false;
        }
        bool BotCast(Unit u,int slot,Unit target,V2? point=null)
        {
            if(u.skills[slot]<=0||u.cooldown[slot]>0||u.mp<Catalog.Heroes[u.hero].abilities[slot].mana)return false;
            return Cast(u,slot,point??(target!=null?target.pos:u.pos),target!=null?target.id:0)=="";
        }
        bool BotAbilities(Unit u,Unit target)
        {
            float distance=V2.Distance(u.pos,target.pos);bool hero=target.kind==Kind.Hero;
            if(u.hero==0)
            {
                if(target.hp<(130+u.skills[5]*90)*.75f&&BotCast(u,5,target))return true;
                if(distance<4&&u.hunger<=0&&BotCast(u,0,target))return true;
                if(!Units.Any(t=>t.kind==Kind.Web&&t.owner==u.id&&!t.dead&&V2.Distance(t.pos,u.pos)<7)&&BotCast(u,1,target,u.pos))return true;
            }
            if(u.hero==1)
            {
                if(hero&&distance<9&&u.souls>4&&BotCast(u,5,target))return true;
                for(int slot=0;slot<3;slot++)if(Math.Abs(distance-Catalog.Heroes[1].abilities[slot].range)<2.6f&&BotCast(u,slot,target))return true;
            }
            if(u.hero==2)
            {
                if(u.shield==0&&BotCast(u,0,target))return true;
                if(hero&&distance<u.range&&u.invisible<=0&&BotCast(u,1,target)){u.target=target.id;return true;}
                if(hero&&!Units.Any(t=>t.kind==Kind.Trap&&t.owner==u.id&&!t.dead&&V2.Distance(t.pos,target.pos)<5)&&BotCast(u,5,target))return true;
            }
            if(u.hero==3)
            {
                if(hero&&distance<3.4f&&target.stun<.4f&&BotCast(u,5,target))return true;
                if((distance<4.2f&&u.hp>u.maxHp*.4f)!=u.rot&&BotCast(u,1,target))return true;
                if(hero&&distance>5&&distance<18&&!Units.Any(t=>t.id!=u.id&&t.id!=target.id&&!t.dead&&!Building(t)&&t.kind!=Kind.Web&&t.kind!=Kind.Rune&&BattleMap.Segment(t.pos,u.pos,target.pos)<1.3f)&&BotCast(u,0,target))return true;
            }
            if(u.hero==4&&hero)
            {
                if(distance<9&&target.stun<.4f&&BotCast(u,5,target))return true;
                if(distance>5&&distance<16&&!TowerDanger(u,target.pos)&&BotCast(u,0,target))return true;
                if(distance<8&&u.haste<=0&&BotCast(u,1,target))return true;
            }
            if(u.hero==5)
            {
                if(hero&&distance<14&&target.stun<.4f&&BotCast(u,0,target))return true;
                if(!Units.Any(t=>t.kind==Kind.Summon&&t.owner==u.id&&!t.dead)&&BotCast(u,2,target,u.pos))return true;
                if(hero&&BotCast(u,5,target))return true;
            }
            return false;
        }
    }
}
