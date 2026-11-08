using Rift.Core;
public static class BehaviorChecks
{
    static Match Arena(int difficulty=2){var m=new Match(1,difficulty);m.Start();m.Tick(.05f);m.Units.Clear();return m;}
    static void Advance(Match m,float seconds){for(int i=0;i<(int)Math.Ceiling(seconds/.05f);i++)m.Tick(.05f);}
    static Unit Hero(Match m,int team,V2 pos,int hero=3,bool bot=false)
    {
        var h=m.Spawn(Kind.Hero,team,pos);h.hero=hero;h.owner=team+1;h.bot=bot;h.skillPoints=0;m.Stats(h);h.hp=h.maxHp;h.mp=h.maxMp;return h;
    }
    public static void Run(Action<bool,string> check)
    {
        foreach(var camp in BattleMap.Camps)check(BattleMap.Path(BattleMap.Bases[0],camp).Count>0&&BattleMap.Path(BattleMap.Bases[1],camp).Count>0,"both teams can reach camp "+camp.x+","+camp.y);
        foreach(var line in BattleMap.Lanes)for(int i=1;i<line.Length;i++)check(BattleMap.Path(line[i-1],line[i]).Count>0,"lane segment is connected");
        check(BattleMap.Path(BattleMap.Bases[0],BattleMap.Roshan).Count>0&&BattleMap.Path(BattleMap.Bases[1],BattleMap.Roshan).Count>0,"Roshan pit has a reachable entrance");
        foreach(var route in BattleMap.JungleRoutes)for(int i=1;i<route.Length;i++)check(BattleMap.Path(route[i-1],route[i]).Count>0,"jungle route is connected");
        var stack=Arena();for(int i=0;i<12;i++){var c=stack.Spawn(Kind.Creep,0,new V2(0,0));c.lane=1;c.waypoint=3;c.formationSlot=i%4;c.speed=0;c.hp=c.maxHp=10000;}
        Advance(stack,3);float min=999;foreach(var a in stack.Units)foreach(var b in stack.Units)if(a.id<b.id)min=Math.Min(min,V2.Distance(a.pos,b.pos));
        check(min>1.05f,"twelve coincident creeps separate into distinct bodies (min="+min.ToString("F2")+")");
        var waves=Arena();for(int team=0;team<2;team++)for(int i=0;i<4;i++)
        {
            float sign=team==0?-1:1;var c=waves.Spawn(Kind.Creep,team,new V2(8*sign+(i-1)*1.2f,8*sign-(i-1)*1.2f));c.lane=1;c.waypoint=3;c.formationSlot=i;c.range=i==3?7:1.8f;c.hp=c.maxHp=10000;c.damage=5;
        }
        Advance(waves,12);var fighters=waves.Units.Where(u=>u.kind==Kind.Creep).ToArray();min=999;foreach(var a in fighters)foreach(var b in fighters)if(a.id<b.id)min=Math.Min(min,V2.Distance(a.pos,b.pos));
        check(min>1.03f&&fighters.Count(c=>c.attackSequence>0)>=6,"opposing waves fight without merging (min="+min.ToString("F2")+")");
        check(fighters.Where(c=>c.range>4).All(c=>fighters.Where(e=>e.team!=c.team).Min(e=>V2.Distance(c.pos,e.pos))>3),"ranged creeps remain behind melee fighters");
        check(fighters.All(c=>BattleMap.Walkable(c.pos)),"combat separation respects map obstacles");
        var block=Arena();var player=Hero(block,0,new V2(0,0));player.stun=999;var walker=block.Spawn(Kind.Creep,0,new V2(-9,-9));walker.lane=1;walker.waypoint=4;walker.formationSlot=1;
        Advance(block,7);check(V2.Distance(player.pos,new V2(0,0))<.01f&&walker.pos.x+walker.pos.y>6,"creep walks around a stationary player without pushing them");
        var leash=Arena();var neutral=leash.Spawn(Kind.Neutral,2,new V2(-15,-8));neutral.home=new V2(-30,-8);neutral.hp=100;neutral.maxHp=450;neutral.target=999;
        Advance(leash,.1f);check(neutral.returning&&neutral.target==0,"neutral abandons pursuit outside its camp leash");Advance(leash,8);check(!neutral.returning&&V2.Distance(neutral.pos,neutral.home)<2&&neutral.hp>400,"neutral returns home and recovers");
        var retreat=Arena();var bot=Hero(retreat,0,new V2(-20,-20),3,true);bot.hp=100;retreat.Tick(.05f);check(bot.retreating&&V2.Distance(bot.goal,BattleMap.Bases[0])<4,"low-health bot retreats to the fountain");
        bot.hp=bot.maxHp*.6f;Advance(retreat,.4f);check(bot.retreating,"bot keeps retreating instead of oscillating at its health threshold");bot.hp=bot.maxHp;bot.mp=bot.maxMp;Advance(retreat,.4f);check(!bot.retreating,"healed bot returns to lane duty");
        var deny=Arena();var farmer=Hero(deny,0,new V2(0,0),3,true);var allied=deny.Spawn(Kind.Creep,0,new V2(1.7f,0));allied.hp=20;allied.maxHp=330;allied.speed=0;allied.lane=1;allied.waypoint=3;
        Advance(deny,.8f);check(farmer.denies==1,"hard bot times a deny and finishes its attack");
        var kite=Arena();var archer=Hero(kite,0,new V2(0,0),1,true);archer.attackTimer=.9f;var pursuer=Hero(kite,1,new V2(3,0));pursuer.hp=pursuer.maxHp*.4f;pursuer.stun=999;kite.Tick(.05f);
        check(V2.Distance(archer.pos,pursuer.pos)>3,"ranged bot kites a nearby hero between attacks");
        var towerTest=Arena();var raider=Hero(towerTest,0,new V2(2,0));var protectedHero=Hero(towerTest,1,new V2(0,0));protectedHero.stun=999;
        var tower=towerTest.Spawn(Kind.Tower,1,new V2(0,8));tower.range=11;tower.damage=10;tower.hp=tower.maxHp=10000;
        var tank=towerTest.Spawn(Kind.Creep,0,new V2(2,7));tank.speed=0;tank.hp=tank.maxHp=10000;tank.lane=1;tank.waypoint=3;
        towerTest.Command(1,new Order{type="stop"});towerTest.Tick(.05f);check(tower.attackVictim==tank.id,"tower lets allied creeps tank before targeting a nearby hero");towerTest.Command(1,new Order{type="attack",target=protectedHero.id});Advance(towerTest,1.5f);
        check(tower.aggroTarget==raider.id&&tower.target==raider.id,"tower switches aggro to a hero who attacks its ally");
        var unsafePush=Arena();var cautious=Hero(unsafePush,0,new V2(0,0),1,true);var danger=unsafePush.Spawn(Kind.Tower,1,new V2(7,0));danger.range=11;unsafePush.Tick(.05f);
        check(cautious.target==0&&cautious.goal.x<0,"bot refuses an unsupported tower dive");
        var assist=Arena();var summoner=Hero(assist,0,new V2(-28,-8),5);var guardian=assist.Spawn(Kind.Neutral,2,new V2(-30,-8));guardian.hp=guardian.maxHp=10000;guardian.speed=0;
        var minion=assist.Spawn(Kind.Summon,0,new V2(-27,-8));minion.owner=summoner.id;minion.lane=1;minion.range=2;summoner.target=guardian.id;assist.Tick(.05f);
        check(minion.target==guardian.id,"summon assists its owner against a neutral camp");
        Console.WriteLine("BEHAVIOR_METRICS trees="+BattleMap.Trees.Count+" camps="+BattleMap.Camps.Length+" waveMinSpacing="+min.ToString("F2"));
    }
}
