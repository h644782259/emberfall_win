"""Actual pause drawing with managed text measurement/GUI boundaries; not rendering."""
from pathlib import Path
import tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1]
def method(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text();prefs=(root/'Assets/Scripts/UI/GameUI.ControlPreferences.cs').read_text()
assert '字号预览' not in prefs and '布局为角色' not in prefs
methods=''.join(method(s,k) for k in ['private void DrawMobilePause()','private void DrawMobilePauseBody('])+method(prefs,'private void DrawMobileControlPreferences(')
fixture=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public Vector2 center=>new Vector2{x=x+width*.5f,y=y+height*.5f};public float xMax=>x+width;public float yMax=>y+height;}
 public struct Color{public Color(float a,float b,float c,float d){}}
 public enum TextAnchor{MiddleCenter}
 public class GUIContent{public string text;public GUIContent(string s){text=s;}}
 public class GUIStyle{public int size;public float CalcHeight(GUIContent c,float w)=>(float)Math.Ceiling(c.text.Length*size/w)*(size+3);}
 public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float f)=>(int)Math.Round(f);}
}
namespace Emberfall{
 public static class MobileControls{public static MobileControlLayout Layout;}
 public static class UIIconAtlas{public static object Utility(string s)=>s;}public static class GameAudio{public static bool Muted;}
 public static class EffectPreferences{public static int TouchPosition;public static float TouchOpacity=1,TouchVisualScale=1,InterfaceTextScale=1,CombatTextScale=1,EffectsScale=1;public static bool CameraShake,ReducedEffects;public static void CycleInterfaceTextScale(){InterfaceTextScale=1.2f;}}
 class Progress{public string LastError="";}
 class Session{public Progress Progression=new Progress();public string Notification="";public bool Paused=true;public void SetPaused(bool v){Paused=v;}public void SetUIBlocking(bool v){}}
 partial class GameUI{
 bool mobileBindingEditor;int bindingDragSource,bindingDragFinger;void CancelMobileScroll(){}void DrawMobileBindingEditor(){action="binding";}bool MerchantServiceActive=>true;enum Panel{SaveLocation,Skills}enum ButtonRole{Primary,Danger,Navigation}Panel panel;Session session=new Session();bool saveReturnPause;int mobilePausePage;readonly Vector2[] mobilePauseScroll=new Vector2[3];Color pale,jade,gold,muted;float width=>MobileControls.Layout.Width;float height=>MobileControls.Layout.Height*TouchRatio;float TouchRatio=>MobileControls.Layout.Scale;
 record Draw(Rect R,string Label,bool Scroll);List<Draw> buttons=new List<Draw>();List<Draw> texts=new List<Draw>();bool scrolling;Rect viewport,content;int begin,end;string click="",action="";
 Rect TouchRect(float x,float y,float w,float h)=>new Rect(x*TouchRatio,y*TouchRatio,w*TouchRatio,h*TouchRatio);int TouchFont(float f)=>(int)Math.Round(f*TouchRatio);GUIStyle Style(int s,bool bold=false,bool wrap=false)=>new GUIStyle{size=(int)Math.Round(s*EffectPreferences.InterfaceTextScale)};
 void Box(Rect r,Color c,bool interactive){}bool PopupCloseButton(Rect r)=>Button(r,"关闭设置",jade);bool PrimaryButton(Rect r,string s,Color c)=>Button(r,s,c);bool Button(Rect r,string s,Color c){buttons.Add(new Draw(r,s,scrolling));return click==s;}bool TabButton(Rect r,string s,bool selected)=>Button(r,s,jade);bool PauseSidebarTab(Rect r,string s,bool selected,float u)=>Button(r,s,jade);void DrawIcon(Rect r,object icon,Color c){}bool QuietAction(Rect r,string s,bool enabled,string hint)=>Button(r,hint,jade);bool DangerButton(Rect r,string s)=>Button(r,s,gold);bool NavigationButton(Rect r,string s,Color c)=>Button(r,s,c);bool DrawButton(Rect r,string s,ButtonRole role)=>Button(r,s,jade);
 void Fill(Rect r,Color c){}void Text(Rect r,string s,int f,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleCenter){texts.Add(new Draw(r,s,scrolling));if(s=="设置")C(r.height>=Style(f,bold).CalcHeight(new GUIContent(s),r.width),"measured title fits scaled font");}
 Vector2 BeginTouchScroll(string owner,Rect r,Vector2 p,Rect body){C(!scrolling,"one scroll owner");scrolling=true;begin++;viewport=r;content=body;return p;}void EndTouchScroll(){scrolling=false;end++;}
 void BlockUITransition(){}string ActiveCharacterName()=>"测试角色";string PlatformText(string s)=>s;
 void OpenControls(){action="guide";}void OpenMobileNoticeFromPause(){action="notice";}void RequestManualSave(){action="save";}void OpenSaveSelection(){action="load";}void RequestExit(bool b){action=b?"title":"exit";}void LeaveMobilePauseForCamp(){action="camp";}
 static int checks;static void C(bool v,string why){checks++;if(!v)throw new Exception(why);}
 public static void Main(){
 foreach(var dims in new[]{(568f,320f,163f),(689f,373f,163f),(754f,386f,163f),(2048f,1536f,264f)})foreach(float scale in new[]{1f,1.1f,1.2f})for(int page=0;page<3;page++){
 MobileControls.Layout=new MobileControlLayout(dims.Item1,dims.Item2,dims.Item3);EffectPreferences.InterfaceTextScale=scale;var ui=new GameUI{mobilePausePage=page};ui.DrawMobilePause();float w=ui.width*ui.TouchRatio,h=ui.height;
 C(ui.begin==1&&ui.end==1&&!ui.scrolling,"balanced scrolling on all three pages");C(ui.viewport.x>=0&&ui.viewport.y>=0&&ui.viewport.xMax<=w&&ui.viewport.yMax<=h,"body safe area");C(ui.content.height>=ui.viewport.height,"body scrolls if smaller viewport");
 C(ui.buttons.Count==(page==1?10:9),"compact action counts");foreach(var b in ui.buttons){Rect bounds=b.Scroll?ui.content:new Rect(0,0,w,h);C(b.R.x>=0&&b.R.y>=0&&b.R.xMax<=bounds.xMax+.01&&b.R.yMax<=bounds.yMax+.01,"all buttons reachable");C(b.R.height>=44*ui.TouchRatio,"retained touch height");if(!b.Scroll)C(b.R.yMax<=ui.viewport.y||b.R.y>=ui.viewport.yMax||b.R.xMax<=ui.viewport.x,"navigation fixed outside scrolling body");}
 foreach(string removed in new[]{"前往遗迹","城镇旅行地图","行囊","图鉴 / 待领"})C(!ui.buttons.Exists(b=>b.Label==removed),"no duplicate outer entry");
 C(!ui.texts.Exists(t=>t.Label.Contains("自动保存持续写入")),"no automatic save boilerplate");
 var titleButton=ui.buttons.Find(b=>b.Label=="保存并返回主菜单");var exitButton=ui.buttons.Find(b=>b.Label=="保存并退出");
 C(!ui.buttons.Exists(b=>b.Label=="冒险"),"adventure tab removed");
 C(titleButton.R.xMax<exitButton.R.x&&Math.Abs(titleButton.R.width-exitButton.R.width)<.01&&titleButton.R.y==exitButton.R.y,"save return and exit share equal footer widths in requested order");

 if(page==1)C(!ui.buttons.Exists(b=>b.Label=="操作指南"),"guide belongs only to control tab");
 foreach(var tab in new[]{("存档",0),("声音与画面",1),("按键设置",2)}){var target=new GameUI{mobilePausePage=page,click=tab.Item1};target.DrawMobilePause();C(target.mobilePausePage==tab.Item2&&target.session.Paused,"every tab directly accessible while paused");}
 if(page==0)foreach(var item in new[]{("保存","save"),("读取存档","load"),("保存并返回主菜单","title"),("保存并退出","exit")}){ui.click=item.Item1;ui.DrawMobilePause();C(ui.action==item.Item2,"retained action invokes original handler");}
 }
 var binding=new GameUI{mobilePausePage=2,click="技能按键配置"};binding.DrawMobilePause();C(binding.mobileBindingEditor&&binding.session.Paused,"binding editor opens within pause");binding.DrawMobilePause();C(binding.action=="binding","actual pause dispatches binding editor");var close=new GameUI{click="关闭设置"};close.DrawMobilePause();C(!close.session.Paused&&close.begin==0,"graphical close resumes before scroll");Console.WriteLine("PASS "+checks+" actual pause layout/action assertions with managed text measurement; not Unity rendering");}
 METHODS
 }
}
'''.replace('METHODS',methods).replace('NOTICECOUNT','10' if 'OpenMobileNoticeFromPause();' in methods else '9')
with tempfile.TemporaryDirectory(prefix='mobile-pause-layout-') as tmp:
 p=Path(tmp);(p/'Fixture.cs').write_text(fixture)
 for rel in ['UI/MobileControlLayout','Combat/MobileSkillPolicy']:(p/(Path(rel).name+'.cs')).write_text((root/'Assets/Scripts'/(rel+'.cs')).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
