using System;using System.Reflection;using Emberfall;using UnityEngine;
public static class MobileFloatingJoystickTests {
 static int checks;static void C(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static object F(object c,string n)=>c.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(c);
 static bool Shown(MobileControls c)=>(bool)F(c,"hasJoystickOrigin");
 static bool Zero()=>MobileControls.Move.magnitude<.0001f;
 public static string Run(){
 foreach(var size in new[]{(568f,320f,160f),(1440f,650f,320f),(2048f,1536f,264f)})foreach(int preset in new[]{-1,0,1}){
  Screen.width=size.Item1;Screen.height=size.Item2;MobileControls.SafeArea=new Rect(20,10,Screen.width-40,Screen.height-20);MobileControls.Layout=new MobileControlLayout(Screen.width-40,Screen.height-20,size.Item3,preset);
  var s=new GameSession();var hero=new PlayerController(s);var ui=new GameUI();var c=new MobileControls(s,ui);MobileControls.ResetInput();
  var start=new Vector2(Screen.width*.24f,Screen.height*.87f); // well above the former fixed bottom joystick.
  C(c.ProcessPointer(11,TouchPhase.Began,start)&&Zero()&&Shown(c),"floating origin starts anywhere in left third without movement");var origin=(Vector2)F(c,"joystickOrigin");
  c.ProcessPointer(11,TouchPhase.Moved,new Vector2(start.x+3*MobileControls.Layout.Scale,start.y));C(Zero(),"logical dead zone suppresses tiny drags");
  c.ProcessPointer(11,TouchPhase.Moved,new Vector2(Screen.width*.8f,start.y));C(MobileControls.Move.x>.99f&&MobileControls.Move.magnitude<=1.001f,"captured movement continues outside third and clamps magnitude");C(((Vector2)F(c,"joystickOrigin")-origin).magnitude==0,"origin stable for entire gesture");
  c.ProcessPointer(12,TouchPhase.Began,new Vector2(Screen.width*.2f,start.y));c.ProcessPointer(12,TouchPhase.Ended,start);C(MobileControls.Move.x>.99f,"second left finger cannot steal or release movement");
  c.ProcessPointer(13,TouchPhase.Began,c.Control(MobileControls.Layout.Attack));C(MobileControls.AttackHeld&&MobileControls.Move.x>.99f,"right action and movement coexist");c.ProcessPointer(13,TouchPhase.Ended,c.Control(MobileControls.Layout.Attack));
  c.ProcessPointer(11,TouchPhase.Ended,start);C(Zero()&&!Shown(c)&&hero.MobilePinnedTarget==null,"release hides and zeros without world tap");
  c.ProcessPointer(11,TouchPhase.Began,start);c.ProcessPointer(11,TouchPhase.Ended,start);C(Zero()&&!Shown(c),"light tap leaves no movement");
  c.ProcessPointer(11,TouchPhase.Began,start);c.ProcessPointer(11,TouchPhase.Moved,new Vector2(start.x+90,start.y));c.ProcessPointer(11,TouchPhase.Canceled,start);C(Zero()&&!Shown(c),"system cancel zeros and hides");
  ui.Overlay=true;c.ProcessPointer(11,TouchPhase.Began,start);c.ProcessPointer(11,TouchPhase.Moved,new Vector2(start.x+90,start.y));C(Zero()&&!Shown(c),"HUD wins over movement admission");ui.Overlay=false;
  c.ProcessPointer(11,TouchPhase.Began,new Vector2(5,start.y));C(!Shown(c),"unsafe inset rejects origin");
  c.ProcessPointer(11,TouchPhase.Began,start);s.InputBlocked=true;c.Tick();s.InputBlocked=false;C(Zero()&&!Shown(c),"pause zeros and hides");c.ProcessPointer(11,TouchPhase.Moved,start);C(!Shown(c),"resume requires fresh down");
  c.ProcessPointer(11,TouchPhase.Began,start);ui.LifecycleTouchBlocked=true;c.ProcessPointer(11,TouchPhase.Moved,start);C(Zero()&&!Shown(c),"viewport/background lifecycle cancels origin");ui.LifecycleTouchBlocked=false;
  c.ProcessPointer(11,TouchPhase.Began,start);MobileControls.ResetInput();C(Zero()&&!Shown(c),"shared focus/scene reset clears capture");
 }
 Screen.width=568;Screen.height=320;MobileControls.SafeArea=new Rect(0,0,568,320);MobileControls.Layout=new MobileControlLayout(568,320,160);MobileControls.ResetInput();
 return "PASS "+checks+" floating joystick production pointer assertions; managed input delivery boundary";
 }
}
