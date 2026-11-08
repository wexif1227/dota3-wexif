using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public struct DesktopPointerFrame
    {
        public Vector2 position;
        public bool rightPressed,rightHeld,leftPressed,leftReleased,attackPressed,focused;
    }
    public partial class ArenaApp
    {
        int selectedUnit=-1,hoveredUnit,lastRightTarget;
        bool attackMode,selecting,repeatRightClick=true;
        Vector2 selectionStart,lastMiddleMouse,currentPointer;
        float nextRightOrder;
        string lastRightType="";
        public int SelectedUnitId=>selectedUnit;
        public int HoveredUnitId=>hoveredUnit;
        public Vector3 CameraFocus=>cameraFocus;
        bool CanOrderHero=>mobile||Me!=null&&selectedUnit==Me.id;
        void ResetDesktop(){selectedUnit=-1;hoveredUnit=0;selecting=attackMode=false;lastRightType="";nextRightOrder=0;MinifyCursors.Reset();}
        public void FocusHero()
        {
            if(Me==null)return;selectedUnit=Me.id;armed=itemArmed=-1;attackMode=selecting=false;follow=false;
            cameraFocus=World.Models.TryGetValue(Me.id,out var model)?model.transform.position:WorldView.Position(Me.pos);
            cameraFocus.y=0;World.Camera.transform.position=cameraFocus+new Vector3(0,42,-31.65f);
        }
        void SelectUnit(Unit unit){selectedUnit=unit?.id??0;armed=itemArmed=-1;attackMode=false;}
        Rect SelectionRect()
        {
            Vector2 end=currentPointer;return Rect.MinMaxRect(Mathf.Min(selectionStart.x,end.x),Mathf.Min(selectionStart.y,end.y),Mathf.Max(selectionStart.x,end.x),Mathf.Max(selectionStart.y,end.y));
        }
        void MouseControl()
        {
            Vector2 mouse=Input.mousePosition;
            bool inside=mouse.x>=0&&mouse.y>=0&&mouse.x<=Screen.width&&mouse.y<=Screen.height;
            bool cameraHeld=Input.GetKey(KeyCode.F1)||Input.GetKey(KeyCode.Space);
            if(Application.isFocused&&inside&&!cameraHeld&&!Input.GetMouseButton(2))
            {
                float dx=mouse.x<7?-1:mouse.x>Screen.width-7?1:0,dz=mouse.y>Screen.height-7?1:mouse.y<7?-1:0;
                if(dx!=0||dz!=0){follow=false;cameraFocus+=new Vector3(dx,0,dz)*Time.deltaTime*cameraSpeed;}
            }
            if(Input.GetMouseButtonDown(2))lastMiddleMouse=mouse;
            if(Input.GetMouseButton(2)&&inside&&!OverUI(mouse))
            {
                Vector2 delta=mouse-lastMiddleMouse;float units=2*World.Camera.orthographicSize/Screen.height;
                cameraFocus-=new Vector3(delta.x*units,0,delta.y*units/.7986f);follow=false;
            }
            lastMiddleMouse=mouse;
            ProcessDesktopPointer(new DesktopPointerFrame{position=mouse,focused=Application.isFocused,rightPressed=Input.GetMouseButtonDown(1),rightHeld=Input.GetMouseButton(1),leftPressed=Input.GetMouseButtonDown(0),leftReleased=Input.GetMouseButtonUp(0),attackPressed=Input.GetKeyDown(KeyCode.A)});
        }
        public void ProcessDesktopPointer(DesktopPointerFrame frame)
        {
            Vector2 mouse=frame.position;currentPointer=mouse;
            bool inside=mouse.x>=0&&mouse.y>=0&&mouse.x<=Screen.width&&mouse.y<=Screen.height;
            var hover=inside&&!OverUI(mouse)?World.PickUnit(mouse):null;hoveredUnit=hover?.id??0;
            if(CanOrderHero&&frame.attackPressed){armed=itemArmed=-1;attackMode=true;}
            bool ui=inside&&OverUI(mouse);string relation=hover==null||Me==null?"":hover.team==Me.team?"_team":"_enemy";
            MinifyCursors.Set(!frame.focused||!inside||ui?"default":armed>=0||itemArmed>=0?"spell"+(relation==""?"_default":relation):attackMode?"attack"+(relation==""?"_default":relation):frame.rightHeld&&hover==null?"move":"default"+relation);
            if(!frame.focused||!inside){selecting=false;return;}
            if(frame.leftReleased&&selecting)
            {
                selecting=false;
                if(Vector2.Distance(selectionStart,mouse)>7)
                {
                    var box=SelectionRect();var own=World.Camera.WorldToScreenPoint(WorldView.Position(Me.pos,2));
                    SelectUnit(box.Contains(own)?Me:null);
                }
                else if(!OverUI(mouse))SelectUnit(hover);
            }
            if(OverUI(mouse))return;
            V2 point=Point(mouse);
            if(frame.leftPressed)
            {
                if(armed>=0||itemArmed>=0){CastPoint(point,hover?.id??0);return;}
                if(attackMode&&CanOrderHero)
                {
                    IssuePointerOrder(point,hover,true,true);attackMode=false;return;
                }
                selectionStart=mouse;selecting=true;
            }
            bool rightDown=frame.rightPressed;
            if(rightDown){armed=itemArmed=-1;attackMode=false;selecting=false;lastRightType="";nextRightOrder=0;}
            if(CanOrderHero&&(rightDown||repeatRightClick&&frame.rightHeld&&Time.unscaledTime>=nextRightOrder))
                IssuePointerOrder(point,hover,false,rightDown);
        }
        // A held right button updates movement at 10 Hz without restarting an attack on the same target.
        void IssuePointerOrder(V2 point,Unit target,bool forceAttack,bool first)
        {
            if(Me==null)return;
            string type=forceAttack?"attackmove":"move";int id=0;
            if(target!=null&&target.id!=Me.id)
            {
                bool deny=target.team==Me.team&&target.kind==Kind.Creep&&target.hp<=target.maxHp*.5f;
                type=target.team!=Me.team||deny||forceAttack?"attack":"follow";id=target.id;
            }
            nextRightOrder=Time.unscaledTime+.1f;
            if(!first&&id!=0&&lastRightType==type&&lastRightTarget==id)return;
            Send(new Order{type=type,target=id,x=point.x,y=point.y});lastRightType=type;lastRightTarget=id;
            if(first)World.CommandMarker(target?.pos??point,type=="attack"||type=="attackmove");
        }
        void DrawDesktopOverlay()
        {
            if(mobile||Me==null)return;
            if(selecting&&Vector2.Distance(selectionStart,currentPointer)>7)
            {
                var rect=SelectionRect();float x=rect.x/scale,y=(Screen.height-rect.yMax)/scale,w=rect.width/scale,h=rect.height/scale;
                Box(x,y,w,h,new Color(.2f,.8f,.5f,.12f));Line(x,y,w,mint);Line(x,y+h,w,mint);Box(x,y,1,h,mint);Box(x+w,y,1,h,mint);
            }
            if(attackMode)Text("АТАКА • ЛКМ по цели или земле • ПКМ — отмена",485,uiHeight-245,700,32,17,gold,TextAnchor.MiddleCenter);
            if(selectedUnit!=Me.id)
            {
                var target=State.units.Find(u=>u.id==selectedUnit);string title=target==null?"ГЕРОЙ НЕ ВЫДЕЛЕН":target.name+"  •  "+Mathf.CeilToInt(target.hp)+" / "+Mathf.CeilToInt(target.maxHp)+" HP";
                Text(title+"    [F1 — свой герой]",420,105,850,35,18,target!=null&&target.team!=Me.team?new Color(1,.45f,.35f):mint,TextAnchor.MiddleCenter);
            }
        }
    }
}
