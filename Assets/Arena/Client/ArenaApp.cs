using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public class ArenaApp : MonoBehaviour
    {
        public static ArenaApp Instance;
        public WorldView World;
        public Snapshot State;
        public Match Local;
        public string ScreenName="menu";
        Connection connection;
        string playerName="Игрок",endpoint="wss://dota3-wexif.onrender.com/ws",roomCode="",notice="",search="";
        int hero,size=5,difficulty=1,team,armed=-1,itemArmed=-1,menuTab;
        float accumulator,nextPing,noticeUntil,lastState,reconnectAt;
        int reconnects;
        bool shop,talents,settings,help,follow=true,mobile;
        Vector2 shopScroll,joystickStart,joystick;
        int joystickFinger=-1;
        float nextMove;
        Vector3 cameraFocus;
        Font font;
        GUIStyle label,button,field;
        Texture2D white,minimap;
        float scale,uiHeight;
        readonly Color bg=new Color(.035f,.059f,.072f,.97f),panel=new Color(.065f,.094f,.108f,.97f),muted=new Color(.56f,.65f,.66f),ink=new Color(.9f,.92f,.86f),gold=new Color(.83f,.71f,.43f),mint=new Color(.38f,.76f,.64f);
        AudioSource sound;
        AudioClip click;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!FindObjectOfType<ArenaApp>())new GameObject("RIFT • Application").AddComponent<ArenaApp>();}
        void Awake()
        {
            Instance=this;Application.targetFrameRate=60;Application.runInBackground=true;Screen.orientation=ScreenOrientation.LandscapeLeft;
            mobile=Application.isMobilePlatform;playerName=PlayerPrefs.GetString("playerName","Игрок");endpoint=PlayerPrefs.GetString("endpoint",endpoint);AudioListener.volume=PlayerPrefs.GetFloat("volume",.35f);
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");white=Texture2D.whiteTexture;
            World=gameObject.AddComponent<WorldView>();World.Build();BuildMinimap();
            sound=gameObject.AddComponent<AudioSource>();click=AudioClip.Create("UI chime",2205,1,22050,false);var samples=new float[2205];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*660/22050)*.13f*(1-i/(float)samples.Length);click.SetData(samples,0);
            if(Environment.GetCommandLineArgs().Contains("-riftSmoke")){StartLocal();Invoke(nameof(CaptureSmoke),12);}
            if(Environment.GetCommandLineArgs().Contains("-riftMenu"))Invoke(nameof(CaptureSmoke),5);
        }
        void CaptureSmoke()
        {
            string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../artifacts"));System.IO.Directory.CreateDirectory(path);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(path,ScreenName=="menu"?"menu.png":"match.png"));Debug.Log("RIFT_SMOKE time="+(State?.time??0)+" units="+(State?.units.Count??0));
        }
        public void StartLocal()
        {
            Leave();Local=new Match(size,difficulty);Local.AddPlayer(1,playerName,hero,team);Local.Start();State=Local.View(1);ScreenName="game";cameraFocus=WorldView.Position(BattleMap.Bases[team]);follow=true;
        }
        void Network(bool create)
        {
            Leave();PlayerPrefs.SetString("playerName",playerName);PlayerPrefs.SetString("endpoint",endpoint);connection=new Connection();connection.Connect(endpoint,new Wire{type=create?"create":"join",name=playerName,hero=hero,team=team,size=size,difficulty=difficulty,room=roomCode});ScreenName="connecting";lastState=Time.realtimeSinceStartup;reconnects=0;
        }
        void Leave()
        {connection?.Dispose();connection=null;Local=null;State=null;World?.ClearUnits();shop=talents=false;armed=itemArmed=-1;}
        void OnDestroy(){connection?.Dispose();}
        Unit Me=>State?.units.Find(u=>u.kind==Kind.Hero&&u.owner==State.you);
        void Send(Order order)
        {if(Local!=null){var result=Local.Command(State.you,order);if(result!="")Notify(result);State=Local.View(State.you);}else connection?.Send(new Wire{type="order",order=order});}
        public void Notify(string text){notice=text;noticeUntil=Time.realtimeSinceStartup+4;}
        void Update()
        {
            if(Local!=null){accumulator+=Mathf.Min(Time.deltaTime,.2f);while(accumulator>=.05f){Local.Tick(.05f);accumulator-=.05f;}State=Local.View(1);}
            if(connection!=null)
            {
                while(connection.Poll(out var message))
                {
                    var header=JsonUtility.FromJson<Wire>(message);
                    if(header.type=="state"){State=JsonUtility.FromJson<Snapshot>(message);lastState=Time.realtimeSinceStartup;reconnects=0;ScreenName=State.phase=="lobby"?"lobby":"game";}
                    if(header.type=="welcome"){connection.Player=header.player;connection.Room=header.room;connection.Token=header.token;}
                    if(header.type=="error")Notify(header.message);
                }
                if(Time.realtimeSinceStartup>nextPing){nextPing=Time.realtimeSinceStartup+10;connection.Send(new Wire{type="ping"});}
                if(!connection.Connected&&connection.Token!=""&&Time.realtimeSinceStartup>reconnectAt&&reconnects<5)
                {
                    string token=connection.Token,room=connection.Room;connection.Dispose();connection=new Connection{Token=token,Room=room};connection.Connect(endpoint,new Wire{type="resume",token=token,room=room});reconnectAt=Time.realtimeSinceStartup+Mathf.Pow(2,++reconnects)+5;Notify("Восстанавливаю соединение…");
                }
            }
            if(State!=null){World.Sync(State);if(State.phase=="ended")ScreenName="game";}
            if(ScreenName!="game"||Me==null){World.Camera.transform.position=Vector3.Lerp(World.Camera.transform.position,new Vector3(0,58,-43),Time.deltaTime*2);return;}
            var me=Me;
            if(Input.GetKeyDown(KeyCode.Escape)){if(shop)shop=false;else if(talents)talents=false;else if(armed>=0||itemArmed>=0){armed=itemArmed=-1;}else settings=!settings;}
            if(Input.GetKeyDown(KeyCode.B))shop=!shop;if(Input.GetKeyDown(KeyCode.T))talents=!talents;
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.F1)){follow=true;cameraFocus=WorldView.Position(me.pos);}
            if(!shop&&!settings&&!talents)
            {
                if(Input.GetKeyDown(KeyCode.S))Send(new Order{type="stop"});
                KeyCode[] keys={KeyCode.Q,KeyCode.W,KeyCode.E,KeyCode.D,KeyCode.F,KeyCode.R};
                for(int i=0;i<6;i++){if(Input.GetKeyDown(keys[i])){if(Input.GetKey(KeyCode.LeftControl))Send(new Order{type="learn",slot=i});else Arm(i,false);}if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))Arm(i,true);}
                if(!mobile)MouseControl();else TouchControl();
            }
            if(follow)cameraFocus=Vector3.Lerp(cameraFocus,WorldView.Position(me.pos),Time.deltaTime*7);
            Vector3 pan=new Vector3(Input.GetKey(KeyCode.RightArrow)?1:Input.GetKey(KeyCode.LeftArrow)?-1:0,0,Input.GetKey(KeyCode.UpArrow)?1:Input.GetKey(KeyCode.DownArrow)?-1:0);
            if(pan!=Vector3.zero){follow=false;cameraFocus+=pan*Time.deltaTime*30;}
            if(!mobile&&!shop)World.Camera.orthographicSize=Mathf.Clamp(World.Camera.orthographicSize-Input.mouseScrollDelta.y*2,17,42);
            cameraFocus.x=Mathf.Clamp(cameraFocus.x,-32,32);cameraFocus.z=Mathf.Clamp(cameraFocus.z,-40,40);World.Camera.transform.position=cameraFocus+new Vector3(0,42,-32);
        }
        bool OverUI(Vector2 p){float s=Screen.width/1600f;return p.y<200*s||p.y>Screen.height-78*s||(mobile&&p.x>Screen.width*.75f&&p.y<350*s)||shop||settings||talents;}
        V2 Point(Vector2 screen)
        {var ray=World.Camera.ScreenPointToRay(screen);var plane=new Plane(Vector3.up,Vector3.zero);plane.Raycast(ray,out float distance);var p=ray.GetPoint(distance);return V2.Clamp(new V2(p.x,p.z));}
        Unit Target(V2 p,bool enemyOnly=false)
        {Unit best=null;float d=3;foreach(var u in State.units){if(u.dead||u.kind==Kind.Web||u.kind==Kind.Rune||u.id==Me.id||enemyOnly&&u.team==Me.team)continue;float distance=V2.Distance(u.pos,p);if(distance<d){best=u;d=distance;}}return best;}
        void MouseControl()
        {
            if(OverUI(Input.mousePosition))return;V2 p=Point(Input.mousePosition);var target=Target(p);
            if(Input.GetMouseButtonDown(1)){armed=itemArmed=-1;Send(target!=null?new Order{type="attack",target=target.id}:new Order{type="move",x=p.x,y=p.y});}
            if(Input.GetMouseButtonDown(0)&&(armed>=0||itemArmed>=0))CastPoint(p,target?.id??0);
            if(Input.GetKeyDown(KeyCode.A)){var enemy=State.units.Where(u=>!u.dead&&u.team!=Me.team&&u.kind!=Kind.Web&&u.kind!=Kind.Rune).OrderBy(u=>V2.Distance(u.pos,Me.pos)).FirstOrDefault();if(enemy!=null)Send(new Order{type="attack",target=enemy.id});}
        }
        void TouchControl()
        {
            foreach(var touch in Input.touches)
            {
                if(touch.phase==TouchPhase.Began&&touch.position.x<Screen.width*.28f&&touch.position.y<Screen.height*.5f&&touch.position.y>Screen.height*.12f){joystickFinger=touch.fingerId;joystickStart=touch.position;}
                if(touch.fingerId==joystickFinger){joystick=Vector2.ClampMagnitude((touch.position-joystickStart)/(Screen.width*.065f),1);if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled){joystickFinger=-1;joystick=Vector2.zero;Send(new Order{type="stop"});}continue;}
                if(touch.phase==TouchPhase.Ended&&!OverUI(touch.position)){var p=Point(touch.position);if(armed>=0||itemArmed>=0)CastPoint(p,Target(p,true)?.id??0);else Send(new Order{type="move",x=p.x,y=p.y});}
            }
            if(joystick.sqrMagnitude>.08f&&Time.time>nextMove){nextMove=Time.time+.12f;V2 p=Me.pos+new V2(joystick.x,joystick.y)*5;Send(new Order{type="move",x=p.x,y=p.y});}
        }
        void Arm(int slot,bool item)
        {
            if(Me==null)return;
            if(item){if(Me.items[slot]==0)return;var a=Catalog.Item(Me.items[slot]).active;if(a==""){Notify(Catalog.Item(Me.items[slot]).description);return;}if(new[]{"heal","salve","mana","wand","phase","bkb","satanic","reflect","shiva","mek","arcane","pipe","force"}.Contains(a)){Send(new Order{type="item",slot=slot});return;}itemArmed=slot;armed=-1;}
            else {var a=Catalog.Heroes[Me.hero].abilities[slot];if(a.passive){Notify(a.description);return;}if(a.range==0){Send(new Order{type="cast",slot=slot});return;}armed=slot;itemArmed=-1;}
            Notify(mobile?"Коснитесь цели на карте":"Выберите цель левой кнопкой мыши");
        }
        void CastPoint(V2 p,int target){Send(new Order{type=armed>=0?"cast":"item",slot=armed>=0?armed:itemArmed,x=p.x,y=p.y,target=target});armed=itemArmed=-1;}
        void Styles()
        {
            if(label!=null)return;
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true,richText=true};label.normal.textColor=ink;
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=18,alignment=TextAnchor.MiddleCenter,wordWrap=true,border=new RectOffset(0,0,0,0),padding=new RectOffset(10,10,4,4)};button.normal.background=white;button.hover.background=white;button.active.background=white;button.normal.textColor=ink;button.hover.textColor=Color.white;button.active.textColor=Color.white;
            field=new GUIStyle(GUI.skin.textField){font=font,fontSize=20,padding=new RectOffset(12,12,10,6)};field.normal.textColor=ink;field.focused.textColor=Color.white;
        }
        void Box(float x,float y,float w,float h,Color color){GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),white);GUI.color=Color.white;}
        void Text(string value,float x,float y,float w,float h=30,int fontSize=18,Color? color=null,TextAnchor align=TextAnchor.UpperLeft)
        {label.fontSize=fontSize;label.normal.textColor=color??ink;label.alignment=align;GUI.Label(new Rect(x,y,w,h),value,label);}
        bool Button(string text,float x,float y,float w,float h=44,bool active=false)
        {GUI.backgroundColor=active?new Color(.2f,.4f,.36f):new Color(.12f,.17f,.19f);bool result=GUI.Button(new Rect(x,y,w,h),text,button);GUI.backgroundColor=Color.white;if(result)sound.PlayOneShot(click);return result;}
        void Line(float x,float y,float w,Color? c=null)=>Box(x,y,w,1,c??new Color(.2f,.28f,.29f));
        void OnGUI()
        {
            if(World==null)return;Styles();scale=Screen.width/1600f;uiHeight=Screen.height/scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            if(ScreenName=="menu")Menu();else if(ScreenName=="join"||ScreenName=="joinModal")Join();else if(ScreenName=="connecting")Connecting();else if(ScreenName=="lobby")Lobby();else if(State!=null)Hud();
            if(settings)Settings();if(help)Help();
            if(noticeUntil>Time.realtimeSinceStartup){Box(470,uiHeight-245,660,44,new Color(.04f,.07f,.08f,.94f));Text(notice,485,uiHeight-236,630,34,18,gold,TextAnchor.MiddleCenter);}
            if(!string.IsNullOrEmpty(GUI.tooltip)){var p=Event.current.mousePosition;Box(Mathf.Min(p.x+15,1230),Mathf.Clamp(p.y-95,5,uiHeight-120),350,90,bg);Text(GUI.tooltip,Mathf.Min(p.x+25,1240),Mathf.Clamp(p.y-85,15,uiHeight-110),330,80,16);}
        }
        void Menu()
        {
            Box(0,0,1600,uiHeight,new Color(.02f,.04f,.05f,.74f));Box(0,0,250,uiHeight,bg);
            Text("R I F T",38,42,200,70,42,gold);Text("THE SHATTERED VALE",40,107,190,30,12,muted);Line(40,155,170);
            Text("DOTA 3 / COMMUNITY ALPHA",40,uiHeight-90,190,40,11,muted);Text("BUILD 0.1.0   •   UNITY",40,uiHeight-48,190,30,11,muted);
            if(Button("ИГРАТЬ",28,200,194,52,menuTab==0))menuTab=0;
            if(Button("ГЕРОИ",28,264,194,52,menuTab==1))menuTab=1;
            if(Button("КАК ИГРАТЬ",28,328,194,52))help=true;
            if(Button("НАСТРОЙКИ",28,392,194,52))settings=true;
            if(Button("ВЫХОД",28,456,194,52))Application.Quit();
            Text("ВЫБЕРИТЕ СВОЮ СТОРОНУ",290,42,730,30,14,gold);Text(menuTab==0?"Битва начинается с тебя.":"Шесть героев. Шесть стилей.",290,78,760,68,38);Text("Три линии. Две крепости. Один Древний.",292,146,730,34,18,muted);
            for(int i=0;i<6;i++)
            {
                int col=i%3,row=i/3;float x=290+col*245,y=(uiHeight<820?190:210)+row*(uiHeight<820?95:105);var h=Catalog.Heroes[i];ColorUtility.TryParseHtmlString("#"+h.color,out var color);Box(x,y,228,90,i==hero?new Color(.16f,.25f,.26f):panel);Box(x,y,4,90,color);
                if(Button(h.name,x+10,y+10,208,42,i==hero)){hero=i;World.SelectHero(i);}Text(h.role,x+18,y+59,200,24,13,muted);
            }
            Box(1080,35,485,uiHeight-70,bg);GUI.DrawTexture(new Rect(1095,110,455,Mathf.Min(475,uiHeight-365)),World.Portrait,ScaleMode.ScaleToFit);
            var selected=Catalog.Heroes[hero];Text(selected.title,1110,61,420,30,14,gold);Text(selected.name,1110,uiHeight-238,420,55,30);Text(selected.description,1110,uiHeight-178,410,80,18,muted);
            if(menuTab==1){float y=uiHeight<820?390:450;foreach(int slot in new[]{0,1,2,5}){var a=selected.abilities[slot];Text(a.name,292,y,220,30,18,gold);Text(a.description,520,y,500,60,16,muted);y+=72;}return;}
            float by=uiHeight<820?390:440;
            Text("ФОРМАТ",292,by,160,25,12,muted);int[] modes={1,2,5};for(int i=0;i<3;i++)if(Button(modes[i]+" × "+modes[i],292+i*116,by+30,105,42,size==modes[i]))size=modes[i];
            Text("БОТЫ",690,by,300,25,12,muted);string[] levels={"Лёгкие","Средние","Сложные"};for(int i=0;i<3;i++)if(Button(levels[i],690+i*110,by+30,102,42,difficulty==i))difficulty=i;
            Text("ИМЯ",292,by+90,200,24,12,muted);playerName=GUI.TextField(new Rect(292,by+118,340,43),playerName,20,field);
            if(Button(team==0?"СИЛЫ СВЕТА":"СИЛЫ ТЬМЫ",690,by+118,322,43,true))team=1-team;
            float bottom=uiHeight<820?uiHeight-80:uiHeight-105;
            if(Button("ТРЕНИРОВКА С БОТАМИ",292,bottom,340,58,true))StartLocal();
            if(Button("ОНЛАЙН • СОЗДАТЬ КОМНАТУ",650,bottom,362,58))Network(true);
            if(Button("ВОЙТИ ПО КОДУ",650,bottom-55,362,42))ScreenName="join";
            Text("Без регистрации • Windows + Android",292,uiHeight-41,730,25,13,muted);
        }
        void Connecting()
        {
            Box(0,0,1600,uiHeight,new Color(.02f,.04f,.05f,.9f));Text("СОЕДИНЕНИЕ",480,uiHeight*.32f,640,60,36,gold,TextAnchor.MiddleCenter);Text(connection?.Status??"Подключение…",450,uiHeight*.43f,700,100,21,ink,TextAnchor.MiddleCenter);Text("Первый запуск бесплатного сервера может занять около минуты.",430,uiHeight*.58f,740,60,16,muted,TextAnchor.MiddleCenter);if(Button("НАЗАД",620,uiHeight*.7f,360,50)){Leave();ScreenName="menu";}
        }
        void Join()
        {
            Box(0,0,1600,uiHeight,new Color(.02f,.04f,.05f,.92f));Text("ВОЙТИ В КОМНАТУ",450,170,700,70,36,gold);Text("Код от друга",450,270,700,30,18,muted);roomCode=GUI.TextField(new Rect(450,315,700,55),roomCode,6,field).ToUpperInvariant();Text("Сервер",450,395,700,30,18,muted);endpoint=GUI.TextField(new Rect(450,440,700,50),endpoint,field);if(Button("ПОДКЛЮЧИТЬСЯ",810,540,340,60,true))Network(false);if(Button("НАЗАД",450,540,320,60))ScreenName="menu";
        }
        void Lobby()
        {
            Box(0,0,1600,uiHeight,new Color(.02f,.04f,.05f,.92f));Text("КОМНАТА  /  "+State.room,160,70,1000,70,40,gold);Text("Отправьте код друзьям. Свободные места займут боты.",162,155,1100,40,21,muted);
            if(Button("КОПИРОВАТЬ КОД",1140,80,280,50))GUIUtility.systemCopyBuffer=State.room;
            for(int side=0;side<2;side++){float x=160+side*680;Text(side==0?"СИЛЫ СВЕТА":"СИЛЫ ТЬМЫ",x,230,600,50,24,side==0?mint:gold);var ps=State.players.Where(p=>p.team==side).ToArray();for(int i=0;i<State.size;i++){float y=295+i*66;Box(x,y,600,55,panel);Text(i<ps.Length?ps[i].name+"   /   "+Catalog.Heroes[ps[i].hero].name:"Бот • свободное место",x+18,y+16,565,30,18,i<ps.Length?ink:muted);}}
            if(State.you==State.host){if(Button("НАЧАТЬ МАТЧ",1050,uiHeight-110,390,58,true))Send(new Order{type="start"});}else Text("Ожидаем начала матча от лидера комнаты…",820,uiHeight-100,620,50,20,gold);
            if(Button("ПОКИНУТЬ КОМНАТУ",160,uiHeight-110,360,58)){Leave();ScreenName="menu";}
        }
        void Hud()
        {
            var me=Me;if(me==null)return;
            DrawHealthBars();Box(0,0,1600,60,bg);Text("R I F T",24,17,180,30,20,gold);
            Text(State.radiantKills+"     —     "+State.direKills,620,9,360,38,29,ink,TextAnchor.MiddleCenter);Text(((int)State.time/60).ToString("00")+":"+((int)State.time%60).ToString("00"),730,43,140,25,13,muted,TextAnchor.MiddleCenter);
            Text(Local!=null?"ТРЕНИРОВКА":State.room+"  •  "+(connection.Connected?"ONLINE":"RECONNECT"),200,20,320,28,13,muted);
            if(Button("?",1424,9,44,40))help=true;if(Button("II",1480,9,44,40))settings=true;
            Text("K / D / A   "+me.kills+" / "+me.deaths+" / "+me.assists+"       LH / DN   "+me.lastHits+" / "+me.denies,1060,73,500,26,13,muted);
            for(int i=0;i<State.feed.Count;i++)Text(State.feed[i],25,85+i*23,530,26,13,i==0?ink:muted);
            float y=uiHeight-181;Box(0,y,1600,181,bg);Line(0,y,1600,new Color(.35f,.4f,.32f));
            MiniMap(10,uiHeight-201,195);
            Text(Catalog.Heroes[me.hero].name,230,y+14,300,35,23);Text("УРОВЕНЬ "+me.level,230,y+54,280,25,13,gold);
            Bar(230,y+88,265,18,me.hp/me.maxHp,new Color(.3f,.67f,.46f));Text(((int)me.hp)+" / "+((int)me.maxHp),230,y+86,265,25,12,Color.white,TextAnchor.MiddleCenter);
            Bar(230,y+115,265,14,me.mp/me.maxMp,new Color(.25f,.52f,.71f));Text(((int)me.mp)+" / "+((int)me.maxMp),230,y+111,265,24,11,Color.white,TextAnchor.MiddleCenter);
            Bar(230,y+142,265,4,me.xp/(float)(me.level*110),gold);Text("Урон "+(int)me.damage+"    Броня "+me.armor.ToString("0.0")+"    Скор. "+me.speed.ToString("0.0"),230,y+151,290,24,12,muted);
            string[] keys={"Q","W","E","D","F","R"};
            for(int i=0;i<6;i++)
            {
                var a=Catalog.Heroes[me.hero].abilities[i];if(a.name=="—")continue;float x=532+i*83;
                if(me.skillPoints>0&&Button("+",x,y-23,74,25))Send(new Order{type="learn",slot=i});
                string title=AbilityCode(me.hero,i);if(Button(new GUIContent(title,a.name+"\n"+a.description),x,y+23,74,72,armed==i))Arm(i,false);Text(keys[i],x+6,y+26,20,20,11,gold);
                if(me.cooldown[i]>0){Box(x,y+23,74,72,new Color(.02f,.03f,.04f,.72f));Text(Mathf.CeilToInt(me.cooldown[i]).ToString(),x,y+40,74,45,28,gold,TextAnchor.MiddleCenter);}
                for(int n=0;n<(i==5?3:4);n++)Box(x+6+n*16,y+103,12,4,me.skills[i]>n?gold:new Color(.2f,.25f,.27f));
                Text(a.passive?"ПАССИВ":a.mana.ToString("0"),x,y+116,74,24,11,muted,TextAnchor.MiddleCenter);
            }
            if(Button("ТАЛАНТЫ"+(me.talentPoints>0?" +":""),532,y+145,230,28))talents=!talents;
            if(Button("КУРЬЕР"+(me.delivery>0?" "+(int)me.delivery:""),778,y+145,234,28))Send(new Order{type="courier"});
            for(int i=0;i<6;i++){float x=1050+(i%3)*72,iy=y+16+(i/3)*67;var item=Catalog.Item(me.items[i]);if(Button(new GUIContent(me.items[i]==0?"—":ItemCode(item.name),item.name+"\n"+item.description),x,iy,65,58,itemArmed==i))Arm(i,true);Text((i+1).ToString(),x+4,iy+2,20,20,10,gold);if(me.itemCooldown[i]>0)Text(Mathf.CeilToInt(me.itemCooldown[i]).ToString(),x,iy+19,65,30,23,gold,TextAnchor.MiddleCenter);}
            Text(me.gold+" ЗОЛОТА",1300,y+24,270,40,25,gold);if(Button("МАГАЗИН  [B]",1300,y+77,270,49,true))shop=!shop;Text("Тайник: "+me.stash.Count(i=>i!=0)+" / 6",1300,y+140,270,26,13,muted);
            if(armed>=0||itemArmed>=0){Text("ВЫБЕРИТЕ ЦЕЛЬ  •  ESC — отмена",530,uiHeight-239,550,35,18,gold,TextAnchor.MiddleCenter);}
            if(me.dead){Box(520,180,560,140,bg);Text("ВОЗРОЖДЕНИЕ ЧЕРЕЗ "+Mathf.CeilToInt(me.respawn),540,198,520,40,24,gold,TextAnchor.MiddleCenter);if(Button("ВЫКУП  /  "+(200+me.level*80)+" G",590,258,420,44))Send(new Order{type="buyback"});}
            if(mobile)MobileHud();if(shop)Shop();if(talents)Talents();
            if(State.phase=="ended"){Box(350,uiHeight*.25f,900,310,bg);Text(State.winner==me.team?"ПОБЕДА":"ПОРАЖЕНИЕ",400,uiHeight*.25f+45,800,75,52,State.winner==me.team?mint:gold,TextAnchor.MiddleCenter);Text("Древний пал. Битва окончена.",400,uiHeight*.25f+135,800,50,23,muted,TextAnchor.MiddleCenter);if(Button("В ГЛАВНОЕ МЕНЮ",600,uiHeight*.25f+215,400,54)){Leave();ScreenName="menu";}}
        }
        bool Button(GUIContent content,float x,float y,float w,float h,bool active=false)
        {GUI.backgroundColor=active?new Color(.2f,.4f,.36f):new Color(.12f,.17f,.19f);bool result=GUI.Button(new Rect(x,y,w,h),content,button);GUI.backgroundColor=Color.white;if(result)sound.PlayOneShot(click);return result;}
        static string Short(string text){var parts=text.Split(' ');return parts.Length>1?parts[0]+"\n"+parts[1]:text;}
        static string ItemCode(string text){var words=text.Split(' ');return words.Length>1?string.Concat(words.Take(3).Select(w=>w[0])).ToUpperInvariant():text.Substring(0,Math.Min(3,text.Length)).ToUpperInvariant();}
        static string AbilityCode(int hero,int slot){string[][] codes={new[]{"FANG","WEB","BITE","","","EGGS"},new[]{"I","II","III","SOUL","AURA","RQM"},new[]{"REF","MELD","PSI","","","TRAP"},new[]{"HOOK","ROT","HEAP","","","MAW"},new[]{"RUN","BULL","BASH","","","VOID"},new[]{"ROOT","TP","TREE","","","WRATH"}};return codes[hero][slot];}
        void Bar(float x,float y,float w,float h,float fraction,Color color){Box(x,y,w,h,new Color(.11f,.15f,.16f));Box(x,y,w*Mathf.Clamp01(fraction),h,color);}
        void DrawHealthBars()
        {
            foreach(var u in State.units){if(u.dead||u.kind==Kind.Web||u.kind==Kind.Rune||u.kind==Kind.Trap)continue;var p=World.Camera.WorldToScreenPoint(WorldView.Position(u.pos,u.kind==Kind.Hero?4.8f:u.kind==Kind.Tower?7:3));if(p.z<0)continue;float x=p.x/scale,y=(Screen.height-p.y)/scale;if(y<65||y>uiHeight-185)continue;float w=u.kind==Kind.Hero?84:48;Bar(x-w/2,y,w,6,u.hp/u.maxHp,u.team==Me.team?mint:new Color(.8f,.35f,.3f));if(u.kind==Kind.Hero){Text(u.name,x-80,y-25,160,24,12,ink,TextAnchor.MiddleCenter);if(u.channel>0)Bar(x-w/2,y+9,w,3,u.channel/u.channelTotal,gold);}}
        }
        void BuildMinimap()
        {
            minimap=new Texture2D(128,128,TextureFormat.RGB24,false);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float px=x/127f*120-60,py=y/127f*120-60;var color=px+py<0?new Color(.12f,.24f,.2f):new Color(.21f,.2f,.25f);if(Mathf.Abs(px+py)<7)color=new Color(.18f,.34f,.39f);foreach(var lane in BattleMap.Lanes)for(int i=1;i<lane.Length;i++)if(BattleMap.Segment(new V2(px,py),lane[i-1],lane[i])<2.5f)color=new Color(.45f,.43f,.33f);minimap.SetPixel(x,y,color);}minimap.Apply();
        }
        void MiniMap(float x,float y,float w)
        {
            Box(x-3,y-3,w+6,w+6,new Color(.35f,.4f,.32f));GUI.DrawTexture(new Rect(x,y,w,w),minimap);
            foreach(var u in State.units){if(u.dead||u.kind==Kind.Web)continue;float px=x+(u.pos.x+60)/120*w,py=y+w-(u.pos.y+60)/120*w;float s=u.kind==Kind.Hero?6:u.kind==Kind.Tower?5:3;Box(px-s/2,py-s/2,s,s,u.id==Me.id?Color.white:u.team==Me.team?mint:u.team==2?gold:new Color(.9f,.35f,.29f));}
            var e=Event.current;if(e.type==EventType.MouseDown&&new Rect(x,y,w,w).Contains(e.mousePosition)){V2 p=new V2((e.mousePosition.x-x)/w*120-60,(1-(e.mousePosition.y-y)/w)*120-60);if(armed>=0||itemArmed>=0)CastPoint(p,0);else if(e.button==1)Send(new Order{type="move",x=p.x,y=p.y});else{follow=false;cameraFocus=WorldView.Position(p);}e.Use();}
        }
        void MobileHud()
        {
            float x=180,y=uiHeight-315;Box(x-65,y-65,130,130,new Color(.15f,.24f,.24f,.5f));Box(x-22+joystick.x*45,y-22-joystick.y*45,44,44,new Color(.6f,.8f,.72f,.7f));
            if(Button("АТАКА",1380,uiHeight-345,150,130,true)){var target=State.units.Where(u=>u.team!=Me.team&&!u.dead&&u.kind!=Kind.Rune&&u.kind!=Kind.Web).OrderBy(u=>V2.Distance(Me.pos,u.pos)).FirstOrDefault();if(target!=null)Send(new Order{type="attack",target=target.id});}
            if(Button("КАМЕРА",1210,uiHeight-290,140,65))follow=true;
        }
        void Shop()
        {
            Box(300,95,1000,uiHeight-310,bg);Text("ЛАВКА ПРЕДМЕТОВ",330,118,600,45,28,gold);if(Button("ЗАКРЫТЬ",1115,115,155,42))shop=false;
            search=GUI.TextField(new Rect(330,175,940,40),search,field);Text("На базе → инвентарь. Вне базы → тайник. Кнопка «Курьер» доставляет покупки.",330,225,940,36,15,muted);
            var items=Catalog.Items.Where(i=>i.id>0&&(search==""||i.name.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();float height=uiHeight-585;
            shopScroll=GUI.BeginScrollView(new Rect(325,270,955,height),shopScroll,new Rect(0,0,925,Mathf.Ceil(items.Length/3f)*110));
            for(int j=0;j<items.Length;j++){var item=items[j];float x=j%3*305,y=j/3*110;Box(x,y,292,100,panel);Text(item.name,x+12,y+8,270,29,17,ink);Text(item.cost+" G",x+12,y+39,90,27,16,gold);if(Button(new GUIContent("КУПИТЬ",item.description+"\nЦена уменьшается на стоимость имеющихся компонентов."),x+130,y+42,147,44)){Send(new Order{type="buy",value=item.id});}}
            GUI.EndScrollView();Text("Продажа на базе: выберите слот ниже (возврат 50%).",330,uiHeight-291,940,25,14,muted);
            for(int i=0;i<6;i++)if(Button(Me.items[i]==0?"—":Catalog.Item(Me.items[i]).name,330+i*157,uiHeight-255,148,36))Send(new Order{type="sell",slot=i});
        }
        void Talents()
        {
            Box(380,130,840,500,bg);Text("ДРЕВО ТАЛАНТОВ",420,160,700,55,32,gold);if(Button("×",1140,150,50,40))talents=false;
            for(int tier=3;tier>=0;tier--){float y=250+(3-tier)*82;Text((10+tier*5).ToString(),770,y+10,60,35,24,gold,TextAnchor.MiddleCenter);for(int side=0;side<2;side++)if(Button(Catalog.Talent(Me.hero,tier,side),side==0?420:850,y,300,60,Me.talents[tier]==side))Send(new Order{type="talent",slot=tier,value=side});}
        }
        void Settings()
        {
            Box(340,90,920,uiHeight-180,bg);Text("НАСТРОЙКИ",390,130,700,55,34,gold);if(Button("×",1165,120,50,42))settings=false;
            Text("Адрес сервера",390,205,800,30,18);endpoint=GUI.TextField(new Rect(390,245,820,45),endpoint,field);
            Text("Громкость",390,320,800,30,18);AudioListener.volume=GUI.HorizontalSlider(new Rect(390,365,820,26),AudioListener.volume,0,1);
            if(Button(mobile?"УПРАВЛЕНИЕ: СЕНСОРНОЕ":"УПРАВЛЕНИЕ: МЫШЬ + КЛАВИАТУРА",390,425,820,50))mobile=!mobile;
            if(Button("СОХРАНИТЬ",390,uiHeight-220,390,50,true)){PlayerPrefs.SetString("endpoint",endpoint);PlayerPrefs.SetFloat("volume",AudioListener.volume);PlayerPrefs.Save();settings=false;}
            if(Button("ГЛАВНОЕ МЕНЮ",820,uiHeight-220,390,50)){Leave();ScreenName="menu";settings=false;}
        }
        void Help()
        {
            Box(300,75,1000,uiHeight-150,bg);Text("ПОЛЕВОЕ РУКОВОДСТВО",345,115,830,60,34,gold);if(Button("×",1210,105,50,42))help=false;
            Text("ПКМ — движение / атака / добивание союзного крипа ниже 50% HP.\nQ W E D F R — способности. Для большинства героев: Q W E R.\nCtrl + клавиша или «+» — изучить способность. Ультимейт: 6 / 12 / 18.\n1–6 — предметы. B — магазин. T — таланты. S — остановиться.\nПробел / F1 — камера на героя. Стрелки — перемещение камеры.\nКолесо — масштаб. Миникарта — обзор; ПКМ по ней — движение.\nНа телефоне: левый джойстик, кнопка атаки справа, способность → цель.\n\nПобеда: разрушьте башни по порядку и вражеский Древний.\nПокупки вне базы отправляются в тайник; вызовите курьера.\nЗолото за крипов получает добивший. Опыт — ближайшие союзники.\nВыкуп работает после смерти и имеет собственную перезарядку.\nВраги скрываются вне обзора. Sentry обнаруживает невидимость.\nРошан даёт Эгиду; руны появляются на реке каждые две минуты.\n\nЭто самостоятельная ранняя версия: числа и ряд эффектов отличаются от Dota 2.",345,195,910,uiHeight-300,20,ink);
        }
        void LateUpdate()
        {
            // Join is kept as a compact modal over the hero-selection menu.
            if(ScreenName=="join")ScreenName="joinModal";
        }
        void OnApplicationQuit(){PlayerPrefs.SetString("playerName",playerName);}
    }
}
