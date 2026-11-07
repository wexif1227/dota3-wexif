using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Rift.Client;
using Rift.Core;
namespace Rift.Editor
{
    // Exercises the same pointer-frame path used by real mouse input, in a running match.
    public static class DesktopPlaytest
    {
        static double started,next;static int stage,checks;static bool sawWalk,sawAttack;static ArenaApp app;static Unit own,enemy;
        static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(string name,object value)=>typeof(ArenaApp).GetField(name,Fields).SetValue(app,value);
        static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Debug.Log("RIFT_CONTROL_PASS "+label);}
        static Vector2 Screen(V2 point,float height=0)=>app.World.Camera.WorldToScreenPoint(WorldView.Position(point,height));
        static Vector2 Body(Unit unit)
        {
            var model=app.World.Models[unit.id];var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();return app.World.Camera.WorldToScreenPoint(skin?skin.bounds.center:model.transform.position+Vector3.up*1.5f);
        }
        static DesktopPointerFrame Frame(Vector2 point)=>new DesktopPointerFrame{position=point,focused=true};
        static void Click(Vector2 point){var f=Frame(point);f.leftPressed=true;app.ProcessDesktopPointer(f);f.leftPressed=false;f.leftReleased=true;app.ProcessDesktopPointer(f);}
        public static void Run()
        {
            ArenaBuild.Setup();EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.position=new Rect(0,0,1600,900);view.Show();
            started=EditorApplication.timeSinceStartup;stage=checks=0;sawWalk=sawAttack=false;EditorApplication.isPlaying=true;EditorApplication.update+=Drive;
        }
        static void Drive()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>55)throw new Exception("Desktop playtest timed out");
                if(app&&own!=null&&app.World.Models.TryGetValue(own.id,out var visual))
                {
                    var motion=visual.GetComponent<ImportedHeroMotion>();if(motion&&motion.player){sawWalk|=motion.player.IsPlaying(motion.run);foreach(var attack in motion.attacks)sawAttack|=motion.player.IsPlaying(attack);}
                }
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.25;
                // A headless editor has a synthetic cursor at its edge; recenter between input cases.
                if(app&&own!=null&&stage>0)app.FocusHero();
                if(stage==0)
                {
                    app=ArenaApp.Instance;if(!app)return;Set("hero",3);Set("size",1);Set("repeatRightClick",true);app.StartLocal();own=app.Local.Hero(1);enemy=app.Local.Units.Find(u=>u.kind==Kind.Hero&&u.team!=own.team);
                    foreach(var unit in app.Local.Units)if(unit.id!=own.id&&unit.id!=enemy.id)unit.dead=true;
                    own.pos=BattleMap.Free(new V2(-5,-7));own.goal=own.pos;enemy.pos=BattleMap.Free(new V2(3,-7));enemy.hp=enemy.maxHp=10000;enemy.stun=999;enemy.bot=false;
                    app.Local.Command(1,new Order{type="stop"});app.State=app.Local.View(1);app.FocusHero();stage++;return;
                }
                if(stage==1)
                {
                    Check(app.SelectedUnitId==own.id,"F1 selects own hero");var viewport=app.World.Camera.WorldToViewportPoint(app.World.Models[own.id].transform.position);Check(Mathf.Abs(viewport.x-.5f)<.04f&&Mathf.Abs(viewport.y-.5f)<.04f,"F1 centers camera at the rendered hero");
                    Check(app.World.PickUnit(Body(own))?.id==own.id,"body picking selects a tall hero");Click(Body(enemy));Check(app.SelectedUnitId==enemy.id,"left click inspects enemy (selected="+app.SelectedUnitId+", expected="+enemy.id+", body="+Body(enemy)+")");app.FocusHero();stage++;return;
                }
                if(stage==2)
                {
                    var empty=Screen(new V2(-15,-12));Click(empty);Check(app.SelectedUnitId==0,"left click on empty ground clears selection");var goal=own.goal;var f=Frame(empty);f.rightPressed=true;app.ProcessDesktopPointer(f);Check(V2.Distance(goal,own.goal)<.01f,"unselected hero receives no mouse orders");app.FocusHero();
                    Vector2 body=Screen(own.pos,1.5f);var down=Frame(body-new Vector2(55,75));down.leftPressed=true;app.ProcessDesktopPointer(down);var up=Frame(body+new Vector2(55,75));up.leftReleased=true;app.ProcessDesktopPointer(up);Check(app.SelectedUnitId==own.id,"drag rectangle selects own hero");stage++;return;
                }
                if(stage==3)
                {
                    var f=Frame(Screen(new V2(-12,-9)));f.rightPressed=f.rightHeld=true;app.ProcessDesktopPointer(f);Check(V2.Distance(own.goal,BattleMap.Free(new V2(-12,-9)))<1,"right button sends movement");stage++;return;
                }
                if(stage==4)
                {
                    var f=Frame(Screen(new V2(-9,-13)));f.rightHeld=true;app.ProcessDesktopPointer(f);Check(V2.Distance(own.goal,BattleMap.Free(new V2(-9,-13)))<1,"held right button updates movement without a second press");ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/controls-walking.png"));stage++;return;
                }
                if(stage==5)
                {
                    var goal=own.goal;Set("shop",true);var f=Frame(Screen(new V2(-18,-10)));f.rightPressed=true;app.ProcessDesktopPointer(f);Check(V2.Distance(goal,own.goal)<.01f,"open shop blocks world orders");Set("shop",false);app.FocusHero();stage++;return;
                }
                if(stage==6)
                {
                    var point=Screen(new V2(15,-10));var f=Frame(point);f.attackPressed=true;app.ProcessDesktopPointer(f);f.attackPressed=false;f.leftPressed=true;app.ProcessDesktopPointer(f);Check(own.attackMoving,"A then left click starts attack move");
                    app.Local.Command(1,new Order{type="stop"});own.pos=BattleMap.Free(new V2(-5,-7));enemy.pos=own.pos+new V2(1.5f,0);app.State=app.Local.View(1);app.FocusHero();stage++;return;
                }
                if(stage==7)
                {
                    var f=Frame(Body(enemy));f.rightPressed=true;app.ProcessDesktopPointer(f);Check(own.target==enemy.id,"right click enemy starts attack");stage++;return;
                }
                if(stage==8){Check(own.attackSequence>0,"running match publishes attack start");ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/controls-attack.png"));next=EditorApplication.timeSinceStartup+1.8;stage++;return;}
                if(stage==9)
                {
                    Check(sawWalk,"imported skeletal run animation played");Check(sawAttack,"imported skeletal attack animation played");
                    app.Local.Command(1,new Order{type="stop"});Check(!own.attacking,"stop cancels current attack pose");
                    File.WriteAllText("artifacts/controls-playtest.txt",checks+" checks passed");Debug.Log("RIFT_CONTROLS_OK "+checks+" checks passed");EditorApplication.update-=Drive;EditorApplication.Exit(0);
                }
            }
            catch(Exception error){Debug.LogException(error);File.WriteAllText("artifacts/controls-playtest.txt",error.ToString());EditorApplication.update-=Drive;EditorApplication.Exit(1);}
        }
    }
}
