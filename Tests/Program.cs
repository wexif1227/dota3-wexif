using Rift.Core;
int checks=0;
void Check(bool b,string label){if(!b)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
Match Setup(int size=1,int hero=3){var m=new Match(size,1);m.AddPlayer(1,"Tester",hero,0);m.AddPlayer(2,"Opponent",1,1);m.Start();return m;}
var m=Setup();var h=m.Hero(1);var enemy=m.Hero(2);
Check(m.Units.Count(u=>u.kind==Kind.Hero)==2,"1v1 slots");
Check(Setup(2).Units.Count(u=>u.kind==Kind.Hero)==4,"2v2 slots");
Check(Setup(5).Units.Count(u=>u.kind==Kind.Hero)==10,"5v5 slots");
Check(m.Command(2,new Order{type="start"})!="","host authorization");
Check(m.Command(1,new Order{type="move",x=float.NaN})!="","reject nonfinite movement");
Check(!m.View(1).units.Any(u=>u.id==enemy.id),"server fog hides enemy spawn");
int gold=h.gold;Check(m.Command(1,new Order{type="buy",value=19})!=""&&h.gold==gold,"purchase insufficient gold");
h.gold=10000;Check(m.Command(1,new Order{type="buy",value=8})=="","purchase boots");
Check(m.Command(1,new Order{type="buy",value=13})==""&&!Catalog.Has(h,8)&&Catalog.Has(h,13),"recipe consumes component");
Check(h.gold==8600,"recipe charges difference");
Check(m.Command(1,new Order{type="buy",value=20})==""&&h.shard&&!Catalog.Has(h,20),"shard consumed permanently");
Check(m.Command(1,new Order{type="learn",slot=0})==""&&h.skillPoints==0,"spend skill point");
Check(m.Command(1,new Order{type="learn",slot=5})!="","ultimate level gate");
enemy.pos=h.pos+new V2(8,0);float hp=enemy.hp;
Check(m.Command(1,new Order{type="cast",slot=0,x=enemy.pos.x,y=enemy.pos.y})==""&&enemy.hp<hp,"Pudge hook damage");
Check(V2.Distance(h.pos,enemy.pos)<3,"Pudge hook pull");
Check(m.Command(1,new Order{type="cast",slot=0})!="","cooldown enforced");
var creep=m.Spawn(Kind.Creep,0,h.pos);creep.hp=creep.maxHp=100;
Check(m.Command(1,new Order{type="attack",target=creep.id})!="","deny above threshold rejected");
creep.hp=30;Check(m.Command(1,new Order{type="attack",target=creep.id})=="","deny below threshold accepted");
m.Damage(h,creep,10000,true);Check(h.denies==1,"deny counter");
m.Damage(enemy,h,10000,true);Check(h.dead&&h.respawn>0,"death and respawn timer");
h.gold=5000;Check(m.Command(1,new Order{type="buyback"})==""&&!h.dead&&h.buyback>0,"buyback");
m.Damage(enemy,h,10000,true);Check(m.Command(1,new Order{type="buyback"})!="","buyback cooldown");
for(int hero=0;hero<6;hero++)
{
    var test=Setup(1,hero);var a=test.Hero(1);var b=test.Hero(2);a.level=25;a.skillPoints=30;a.mp=9999;a.maxMp=9999;
    for(int slot=0;slot<6;slot++)for(int rank=0;rank<4;rank++)test.Command(1,new Order{type="learn",slot=slot});
    for(int slot=0;slot<6;slot++){if(Catalog.Heroes[hero].abilities[slot].passive)continue;b.dead=false;b.hp=5000;b.pos=a.pos+new V2(2,0);a.stun=a.silence=0;a.mp=9999;string result=test.Command(1,new Order{type="cast",slot=slot,target=b.id,x=b.pos.x,y=b.pos.y});Check(result=="",Catalog.Heroes[hero].name+" "+slot);}
}
Match Duel(){var game=Setup();foreach(var unit in game.Units.Where(u=>u.kind!=Kind.Hero))unit.dead=true;game.Hero(1).pos=new V2(0,0);game.Hero(1).goal=game.Hero(1).pos;game.Hero(2).pos=new V2(1.5f,0);game.Hero(2).stun=999;return game;}
void Advance(Match game,float seconds){for(int i=0;i<(int)Math.Ceiling(seconds/.05f);i++)game.Tick(.05f);}
var combat=Duel();var attacker=combat.Hero(1);var victim=combat.Hero(2);float before=victim.hp;
combat.Command(1,new Order{type="attack",target=victim.id});combat.Tick(.05f);
Check(attacker.attackSequence==1&&attacker.attackWindup>0&&victim.hp>=before,"attack begins with a visible windup before damage");
int sequence=attacker.attackSequence;float remaining=attacker.attackWindup;
combat.Command(1,new Order{type="attack",target=victim.id});combat.Tick(.05f);
Check(attacker.attackSequence==sequence&&attacker.attackWindup<remaining,"repeated attack command does not restart windup");
combat.Command(1,new Order{type="move",x=-20,y=0});Advance(combat,.45f);
Check(victim.hp>=before&&!attacker.attacking,"movement cancels attack before impact");
attacker.pos=new V2(0,0);combat.Command(1,new Order{type="attack",target=victim.id});Advance(combat,.4f);
Check(victim.hp<before&&combat.Effects.Any(e=>e.type=="hit"&&e.source==attacker.id&&e.target==victim.id),"damage and hit effect happen at the attack point");
float afterImpact=victim.hp;combat.Command(1,new Order{type="move",x=-20,y=0});combat.Command(1,new Order{type="attack",target=victim.id});Advance(combat,.2f);
Check(victim.hp>=afterImpact,"cancelling backswing does not bypass attack cooldown");
var hold=Duel();var holder=hold.Hero(1);var heldEnemy=hold.Hero(2);heldEnemy.pos=new V2(5,0);V2 heldPosition=holder.pos;hold.Command(1,new Order{type="hold"});Advance(hold,1);
Check(V2.Distance(holder.pos,heldPosition)<.01f&&holder.attackSequence==0,"hold position does not chase an out-of-range enemy");
heldEnemy.pos=new V2(1,0);Advance(hold,.45f);Check(holder.attackSequence>0,"hold position attacks enemies in range");
var attackMove=Duel();var mover=attackMove.Hero(1);attackMove.Hero(2).pos=new V2(4,0);attackMove.Command(1,new Order{type="attackmove",x=15,y=0});Advance(attackMove,1.5f);
Check(mover.attackSequence>0&&mover.pos.x<10,"attack move engages enemies on the route");
var followTest=Duel();var follower=followTest.Hero(1);var ally=followTest.Spawn(Kind.Hero,0,new V2(8,0));ally.stun=999;followTest.Hero(2).pos=new V2(50,50);followTest.Command(1,new Order{type="follow",target=ally.id});Advance(followTest,1.5f);
Check(V2.Distance(follower.pos,ally.pos)<4&&follower.attackSequence==0,"right-click follow approaches an ally without attacking");
var ranged=Duel();var archer=ranged.Hero(1);archer.hero=1;ranged.Stats(archer);var rangedVictim=ranged.Hero(2);rangedVictim.pos=new V2(7,0);float rangedHp=rangedVictim.hp;ranged.Command(1,new Order{type="attack",target=rangedVictim.id});Advance(ranged,.6f);
Check(rangedVictim.hp>=rangedHp&&ranged.Effects.Any(e=>e.type=="bolt"&&e.source==archer.id),"ranged attack releases a projectile before applying damage");
ranged.Command(1,new Order{type="move",x=-20,y=0});Advance(ranged,.35f);Check(rangedVictim.hp<rangedHp,"released projectile still hits after movement cancels backswing");
var stopped=Duel();stopped.Command(1,new Order{type="stop"});Advance(stopped,2);Check(stopped.Hero(1).attackSequence==0,"stop does not immediately resume auto attack");
var meld=Duel();var assassin=meld.Hero(1);assassin.hero=2;meld.Stats(assassin);assassin.invisible=999;assassin.skills[1]=1;meld.Command(1,new Order{type="attack",target=meld.Hero(2).id});Advance(meld,.7f);Check(assassin.invisible==0&&meld.Hero(2).hp<meld.Hero(2).maxHp,"Templar Assassin can attack out of Meld");
var stunnedBot=Duel();var bot=stunnedBot.Hero(2);bot.hero=3;bot.bot=true;bot.skills[0]=1;stunnedBot.Stats(bot);float botMana=bot.mp;V2 victimPosition=stunnedBot.Hero(1).pos;Advance(stunnedBot,.1f);Check(bot.mp>=botMana&&V2.Distance(stunnedBot.Hero(1).pos,victimPosition)<.01f,"stunned bot cannot cast a hook or issue combat orders");
var simulation=Setup(5);simulation.Hero(1).bot=true;simulation.Hero(2).bot=true;
var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<12000;i++)simulation.Tick(.05f);
Check(simulation.Units.All(u=>!float.IsNaN(u.pos.x)&&!float.IsNaN(u.hp)),"10 minute simulation finite");
Check(simulation.Units.Count<400,"entity count bounded");
Check(simulation.Time>100,"simulation advances");
var finish=Setup();foreach(var tower in finish.Units.Where(u=>u.kind==Kind.Tower))tower.dead=true;var core=finish.Units.Find(u=>u.kind==Kind.Ancient&&u.team==1);finish.Damage(finish.Hero(1),core,999999,true);Check(finish.Phase=="ended"&&finish.Winner==0,"ancient destruction finishes match");
var courierTest=Setup();var recipient=courierTest.Hero(1);recipient.pos=new V2(-25,-25);recipient.gold=1000;courierTest.Command(1,new Order{type="buy",value=8});Check(!Catalog.Has(recipient,8)&&recipient.stash.Contains(8),"remote purchase enters stash");courierTest.Command(1,new Order{type="courier"});for(int i=0;i<150;i++)courierTest.Tick(.05f);Check(Catalog.Has(recipient,8)&&!recipient.stash.Contains(8),"courier delivers purchase");
Console.WriteLine($"{checks} checks passed; simulated {simulation.Time:F1}s in {watch.Elapsed.TotalSeconds:F2}s; phase={simulation.Phase}, units={simulation.Units.Count}");
