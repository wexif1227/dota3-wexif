using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Rift.Client;
using Rift.Core;
namespace Rift.Editor
{
    public static class MapPlaytest
    {
        static double started,next;static int stage;static ArenaApp app;
        public static void Run()
        {
            ArenaBuild.Setup();EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.position=new Rect(0,0,1600,900);view.Show();started=EditorApplication.timeSinceStartup;stage=0;EditorApplication.isPlaying=true;EditorApplication.update+=Drive;
        }
        static void Drive()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>58)throw new Exception("Map playtest timed out");
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.4;
                app=ArenaApp.Instance;if(!app)return;
                if(stage==0)
                {
                    typeof(ArenaApp).GetField("size",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(app,5);app.StartLocal();foreach(var unit in app.Local.Units)if(unit.kind==Kind.Hero)unit.bot=true;
                    for(int i=0;i<1240;i++)app.Local.Tick(.05f);ShowAll();stage++;next=EditorApplication.timeSinceStartup+1;return;
                }
                if(stage==1)
                {
                    ShowAll();Shader.SetGlobalFloat("_RiftFog",0);var camera=app.World.Camera;camera.transform.position=new Vector3(0,135,-90);camera.transform.LookAt(Vector3.zero);camera.orthographicSize=74;
                    Capture(camera,"artifacts/map-overview.png",1536,1536);camera.transform.position=new Vector3(0,43,-30);camera.transform.LookAt(new Vector3(0,0,1));camera.orthographicSize=25;Capture(camera,"artifacts/map-mid.png",1600,1000);
                    var creeps=app.Local.Units.Where(u=>!u.dead&&u.kind==Kind.Creep).ToArray();if(creeps.Length<8)throw new Exception("Missing active waves");
                    if(app.World.Models.Values.Count(g=>g.GetComponent<CreepMotion>())<8)throw new Exception("Missing soldier animation components");
                    Debug.Log("RIFT_MAP_PLAYTEST_OK simulated="+app.Local.Time+" creeps="+creeps.Length+" trees="+BattleMap.Trees.Count+" camps="+BattleMap.Camps.Length+" renderers="+UnityEngine.Object.FindObjectsOfType<Renderer>().Count(r=>r.enabled));
                    var battle=new Match(1);battle.Start();battle.Tick(.05f);battle.Units.Clear();
                    for(int team=0;team<2;team++)for(int i=0;i<4;i++){float sign=team==0?-1:1;var unit=battle.Spawn(Kind.Creep,team,new V2(8*sign+(i-1)*1.2f,8*sign-(i-1)*1.2f));unit.lane=1;unit.waypoint=3;unit.formationSlot=i;unit.hero=i==3?1:0;unit.range=i==3?7:1.8f;unit.hp=unit.maxHp=3000;unit.damage=10;}
                    for(int tick=0;tick<240;tick++)battle.Tick(.05f);
                    foreach(var previous in app.World.Models.Values)previous.SetActive(false);app.World.ClearUnits();
                    var battleView=app.Local.View(1);battleView.units=battle.Units.Where(u=>!u.dead).ToList();app.World.Sync(battleView);Shader.SetGlobalFloat("_RiftFog",0);
                    foreach(var unit in battleView.units){var model=app.World.Models[unit.id];if(unit.facing.Length>.01f)model.transform.rotation=Quaternion.LookRotation(new Vector3(unit.facing.x,0,unit.facing.y));}
                    camera.transform.position=new Vector3(0,23,-16);camera.transform.LookAt(Vector3.zero);camera.orthographicSize=13;Capture(camera,"artifacts/creep-formation.png",1400,1000);
                    EditorApplication.update-=Drive;EditorApplication.Exit(0);
                }
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.update-=Drive;EditorApplication.Exit(1);}
        }
        static void ShowAll()
        {
            var state=app.Local.View(1);state.units=app.Local.Units.Where(u=>!u.dead).ToList();app.World.Sync(state);
        }
        static void Capture(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.aspect=width/(float)height;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();Directory.CreateDirectory("artifacts");File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(image);
        }
    }
}
