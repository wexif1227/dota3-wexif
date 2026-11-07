using System;
using System.Linq;
namespace Rift.Core
{
    public partial class Match
    {
        string Learn(Unit u,int slot)
        {
            if(slot<0||slot>5||u.skillPoints<=0)return "Нет очков способностей";
            var a=Catalog.Heroes[u.hero].abilities[slot];if(a.name=="—")return "Нет способности";
            int max=slot==5?3:4, required=slot==5?6+u.skills[slot]*6:1+u.skills[slot]*2;
            if(u.skills[slot]>=max||u.level<required)return "Нужен более высокий уровень";u.skills[slot]++;u.skillPoints--;return "";
        }
        string Talent(Unit u,int tier,int side)
        {
            if(tier<0||tier>3||side<0||side>1||u.level<10+tier*5||u.talents[tier]>=0||u.talentPoints<=0)return "Талант пока недоступен";
            u.talents[tier]=side;u.talentPoints--;Stats(u);return "";
        }
        string Cast(Unit u,int slot,V2 p,int targetId)
        {
            if(slot<0||slot>5)return "Неверная способность";var a=Catalog.Heroes[u.hero].abilities[slot];int rank=u.skills[slot];
            if(a.passive)return "Пассивная способность";if(rank==0)return "Сначала изучите способность (+)";if(u.silence>0)return "Безмолвие";if(u.cooldown[slot]>0)return "Способность перезаряжается";if(u.mp<a.mana)return "Недостаточно маны";
            var t=Find(targetId);if(t!=null&&(t.dead||!Visible(t,u.team)))t=null;
            bool targeted=(u.hero==0&&slot==5)||(u.hero==3&&slot==5)||(u.hero==4&&(slot==0||slot==5));
            if(targeted&&(t==null||t.team==u.team))return "Выберите видимую вражескую цель";
            if(targeted&&V2.Distance(t.pos,u.pos)>a.range)return "Цель слишком далеко";
            if(a.range>0&&a.range<100&&u.hero!=1&&V2.Distance(u.pos,p)>a.range+1)p=u.pos+(p-u.pos).Normal*a.range;
            Cancel(u);u.mp-=a.mana;u.cooldown[slot]=a.cooldown*(u.talents[3]==1?.75f:1);u.facing=(p-u.pos).Length>.2f?(p-u.pos).Normal:u.facing;u.invisible=0;
            switch(u.hero)
            {
                case 0:
                    if(slot==0)u.hunger=8+(u.shard?3:0);
                    if(slot==1){var web=Spawn(Kind.Web,u.team,p);web.life=70;web.owner=u.id;Fx("web",p,p,8,u.team);}
                    if(slot==5){Damage(u,t,130+rank*90,false);Fx("poison",u.pos,t.pos,2,u.team);if(t.dead)Summons(u,t.pos,2+rank+(u.aghanim?2:0),0);}
                    break;
                case 1:
                    if(slot<3){V2 center=u.pos+u.facing*a.range;Area(u,center,3.7f,70+rank*70+(u.shard?u.souls*3:0));Fx("raze",center,center,3.7f,u.team);}
                    if(slot==5){float d=120+rank*80+u.souls*10;Area(u,u.pos,13,d,false,3,1);Fx("requiem",u.pos,u.pos,13,u.team);if(u.aghanim){Area(u,u.pos,9,d*.4f);u.hp=Math.Min(u.maxHp,u.hp+d*.25f);}}
                    break;
                case 2:
                    if(slot==0){u.shield=2+rank+(u.aghanim?3:0);Fx("shield",u.pos,u.pos,2,u.team);}
                    if(slot==1){u.invisible=999;u.moving=false;u.target=0;u.path.Clear();}
                    if(slot==5){var traps=Units.Where(x=>x.kind==Kind.Trap&&x.owner==u.id&&!x.dead).ToList();if(traps.Count>=3+rank)traps[0].dead=true;var trap=Spawn(Kind.Trap,u.team,p);trap.owner=u.id;trap.life=120;trap.hp=trap.maxHp=120;Fx("trap",p,p,2,u.team);}
                    break;
                case 3:
                    if(slot==0)
                    {
                        V2 end=u.pos+u.facing*(u.shard?24:19);Unit hit=null;float best=99;
                        foreach(var v in Units){if(v.id==u.id||v.dead||v.kind==Kind.Web||v.kind==Kind.Rune||v.kind==Kind.Tower||v.kind==Kind.Ancient||v.kind==Kind.Barracks)continue;float d=V2.Distance(u.pos,v.pos);if(BattleMap.Segment(v.pos,u.pos,end)<1.3f&&d<best){hit=v;best=d;}}
                        if(hit!=null){end=hit.pos;if(hit.team!=u.team){Damage(u,hit,100+rank*80,false);Control(hit,1,0);}hit.pos=BattleMap.Free(u.pos+u.facing*2);hit.moving=false;hit.path.Clear();}Fx("hook",u.pos,end,1,u.team);
                    }
                    if(slot==1)u.rot=!u.rot;
                    if(slot==5){u.channel=3;u.channelTotal=3;u.channelSpell=5;u.channelTarget=t.id;u.moving=false;Control(t,3,0);}
                    break;
                case 4:
                    if(slot==0){u.channel=10;u.channelTotal=10;u.channelSpell=0;u.channelTarget=t.id;u.moving=false;}
                    if(slot==1){u.haste=6;u.slow=0;Fx("charge",u.pos,u.pos,3,u.team);}
                    if(slot==5){u.pos=BattleMap.Free(t.pos-u.facing*2);Damage(u,t,130+rank*100,false);Control(t,1.5f,0);if(u.aghanim)Area(u,t.pos,6,150,false,0,1.5f);Fx("strike",u.pos,t.pos,4,u.team);}
                    break;
                case 5:
                    if(slot==0){Area(u,p,4,30+rank*20,false,0,1.5f+rank*.4f+(u.shard?1:0));Fx("sprout",p,p,4,u.team);}
                    if(slot==1){u.goal=BattleMap.Free(p);u.channel=2.5f;u.channelTotal=2.5f;u.channelSpell=1;u.moving=false;}
                    if(slot==2)Summons(u,p,1+rank,1);
                    if(slot==5){int n=0;foreach(var v in Units.ToArray().OrderBy(v=>V2.Distance(v.pos,p)))if(!v.dead&&v.team!=u.team&&v.kind!=Kind.Web&&v.kind!=Kind.Rune&&Visible(v,u.team)&&n++<12){Damage(u,v,(90+rank*60)*(1+n*.07f),false);if(u.aghanim)Control(v,1.3f,0);Fx("nature",u.pos,v.pos,2,u.team);}}
                    break;
            }
            return "";
        }
        void Channel(Unit u,float dt)
        {
            if(u.stun>0){Cancel(u);return;}u.channel-=dt;
            if(u.channelSpell==20||(u.hero==5&&u.channelSpell==1))
            {if(u.channel<=0){Fx("teleport",u.pos,u.goal,4,u.team);u.pos=BattleMap.Free(u.goal);Cancel(u);}return;}
            var t=Find(u.channelTarget);if(t==null||t.dead){Cancel(u);return;}
            if(u.hero==3&&u.channelSpell==5){if(V2.Distance(u.pos,t.pos)>6){Cancel(u);return;}float d=(40+u.skills[5]*35)*(u.aghanim?1.6f:1)*dt;Damage(u,t,d,false);u.hp=Math.Min(u.maxHp,u.hp+d);if(u.channel<=0)Cancel(u);}
            if(u.hero==4&&u.channelSpell==0){V2 d=t.pos-u.pos;u.facing=d.Normal;u.pos=V2.Clamp(u.pos+d.Normal*Math.Min(d.Length,(u.shard?30:23)*dt));if(d.Length<2){Damage(u,t,80+u.skills[0]*45,false);Control(t,1+u.skills[0]*.2f,0);Fx("strike",u.pos,t.pos,4,u.team);Cancel(u);}else if(u.channel<=0)Cancel(u);}
        }
        void Summons(Unit u,V2 p,int count,int type)
        {
            int existing=Units.Count(v=>v.kind==Kind.Summon&&v.owner==u.id&&!v.dead);count=Math.Min(count,16-existing);
            for(int i=0;i<count;i++){var s=Spawn(Kind.Summon,u.team,BattleMap.Free(p+new V2(i%3,i/3)));s.owner=u.id;s.hero=type;s.life=45;s.hp=s.maxHp=type==0?160:480;s.damage=type==0?18:35;s.speed=type==0?8:5;s.lane=u.lane;s.waypoint=u.team==0?3:3;}
        }
        public int Price(Unit u,int id)
        {
            var item=Catalog.Item(id);int cost=item.cost;var owned=u.items.Concat(u.stash).ToList();foreach(int component in item.recipe)if(owned.Remove(component))cost-=Catalog.Item(component).cost;return Math.Max(0,cost);
        }
        string Buy(Unit u,int id)
        {
            if(id<=0||id>=Catalog.Items.Count)return "Неверный предмет";if(id==20&&u.shard)return "Shard уже получен";
            int price=Price(u,id);if(u.gold<price)return "Недостаточно золота";
            bool atBase=V2.Distance(u.pos,BattleMap.Bases[u.team])<12&&!u.dead;
            int[] slots=atBase?u.items:u.stash;
            // Reserve space before consuming components; an upgrade may reuse its component slot.
            bool room=Array.IndexOf(slots,0)>=0||Catalog.Item(id).recipe.Any(c=>Array.IndexOf(slots,c)>=0)||id==20;
            if(!room)return atBase?"Инвентарь заполнен":"Тайник заполнен";
            foreach(int component in Catalog.Item(id).recipe){int index=Array.IndexOf(u.items,component);if(index>=0){u.items[index]=0;u.itemCooldown[index]=0;}else{index=Array.IndexOf(u.stash,component);if(index>=0)u.stash[index]=0;}}
            u.gold-=price;if(id==20){u.shard=true;return "";}slots[Array.IndexOf(slots,0)]=id;Stats(u);return "";
        }
        string Sell(Unit u,int slot)
        {
            if(slot<0||slot>5||u.items[slot]==0)return "Пустой слот";if(V2.Distance(u.pos,BattleMap.Bases[u.team])>12)return "Продажа доступна на базе";
            u.gold+=Catalog.Item(u.items[slot]).cost/2;u.items[slot]=0;u.itemCooldown[slot]=0;Stats(u);return "";
        }
        void Deliver(Unit u)
        {for(int i=0;i<6;i++){if(u.stash[i]==0)continue;int slot=Array.IndexOf(u.items,0);if(slot<0)break;u.items[slot]=u.stash[i];u.stash[i]=0;}Stats(u);}
        string UseItem(Unit u,int slot,V2 point,int targetId)
        {
            if(slot<0||slot>5||u.items[slot]==0)return "Пустой слот";var i=Catalog.Item(u.items[slot]);if(i.active=="")return "Пассивный предмет";if(u.itemCooldown[slot]>0)return "Предмет перезаряжается";
            var t=Find(targetId);if(t!=null&&(t.dead||!Visible(t,u.team)||t.team==u.team))t=null;
            string a=i.active;bool enemy=a=="hex"||a=="silence"||a=="cyclone"||a=="dagon"||a=="midas";
            if(enemy&&(t==null||V2.Distance(u.pos,t.pos)>12))return "Выберите врага в радиусе 12 м";
            if(a=="midas"&&t.kind!=Kind.Creep&&t.kind!=Kind.Neutral)return "Нужен крип";
            if(a=="blink"&&u.lastDamage>0)return "Blink заблокирован после урона";
            if(a=="tp"){var building=Units.Where(b=>b.team==u.team&&!b.dead&&(b.kind==Kind.Tower||b.kind==Kind.Ancient)).OrderBy(b=>V2.Distance(b.pos,point)).FirstOrDefault();if(building==null)return "Нет строения для телепорта";u.goal=building.pos;u.channel=3;u.channelTotal=3;u.channelSpell=20;u.moving=false;}
            if(a=="heal"||a=="salve")u.regen=a=="heal"?7.2f:16;
            if(a=="mana"||a=="wand")u.mp=Math.Min(u.maxMp,u.mp+180);
            if(a=="wand")u.hp=Math.Min(u.maxHp,u.hp+160);
            if(a=="ward"||a=="sentry"){var pos=u.pos+(point-u.pos).Normal*Math.Min(8,V2.Distance(u.pos,point));var w=Spawn(Kind.Ward,u.team,pos);w.hero=a=="sentry"?1:0;w.life=a=="sentry"?150:180;w.hp=w.maxHp=150;}
            if(a=="blink"){u.pos=BattleMap.Free(u.pos+(point-u.pos).Normal*Math.Min(14,V2.Distance(u.pos,point)));u.moving=false;Fx("teleport",u.pos,u.pos,3,u.team);}
            if(a=="force"){u.pos=BattleMap.Free(u.pos+u.facing*9);u.moving=false;}
            if(a=="phase")u.haste=4;if(a=="bkb"){u.immune=6;u.silence=u.slow=u.stun=0;}
            if(a=="satanic"){u.hunger=6;u.hp=Math.Min(u.maxHp,u.hp+200);}
            if(a=="reflect")u.reflect=5;
            if(a=="shiva"){Area(u,u.pos,12,200,false,4);Fx("ice",u.pos,u.pos,12,u.team);}
            if(a=="hex"||a=="cyclone")Control(t,a=="hex"?3:2.5f,0);
            if(a=="silence"&&t.immune<=0){t.silence=4;Cancel(t);}
            if(a=="dagon"){Damage(u,t,400,false);Fx("dagon",u.pos,t.pos,2,u.team);}
            if(a=="midas"){Damage(u,t,100000,true);u.gold+=142;GrantXp(u,200);}
            if(a=="mek"||a=="arcane"||a=="pipe")foreach(var friend in Units)if(!friend.dead&&friend.team==u.team&&V2.Distance(u.pos,friend.pos)<12){if(a=="mek")friend.hp=Math.Min(friend.maxHp,friend.hp+300);if(a=="arcane")friend.mp=Math.Min(friend.maxMp,friend.mp+200);if(a=="pipe")friend.immune=4;}
            u.itemCooldown[slot]=i.cooldown;if(i.id<=6){u.items[slot]=0;u.itemCooldown[slot]=0;}return "";
        }
    }
}
