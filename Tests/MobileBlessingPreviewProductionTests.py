"""Actual blessing draw, chrome, paragraph measurement, Notify/getter with engine shells.
Managed coordinate replay is not Unity font rendering or device touch acceptance.
"""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(file,signature):
 s=(root/'Assets/Scripts'/file).read_text();a=s.index(signature);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
methods='\n'.join(member('UI/GameUI.MobilePanels.cs',sig) for sig in ['private bool DrawMobilePanelChrome(','private float MeasureMobileParagraph(','private float DrawMobileParagraph('])+'\n'+member('UI/GameUI.Expedition.cs','private void ClickBlessing(')
notify=member('Core/GameSession.cs','public void Notify(');notification=member('Core/GameSession.cs','public string Notification')
shell=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Color{public Color(float r,float g,float b,float a=1){}public static Color operator*(Color c,float n)=>c;}
 public struct Vector2{public static Vector2 zero=>new Vector2();}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}public float yMax=>y+height;}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Ceil(float x)=>(float)Math.Ceiling(x);}
 public enum TextAnchor{UpperLeft,MiddleLeft,MiddleCenter}
 public class GUIContent{public string text;public GUIContent(string s){text=s;}public static GUIContent none=new GUIContent("");}
 public class GUIStyle{public int size;public float CalcHeight(GUIContent c,float width)=>size*1.4f*(1+(int)(c.text.Length*size/Math.Max(1,width)));}
 public static class Time{public static float unscaledTime;public static int frameCount;}
 public static class GUI{public static bool enabled=true;public static int Click=-1,Index;public static bool Button(Rect r,GUIContent c,object s)=>Index++==Click;}
}
namespace Emberfall {
 public enum RunBlessing{First,Second,Third}public class PlayerController{public int CombatEpoch=1;}
 public class Profile{public int heroClass;}public class ProgressionStub{public Profile Profile=new Profile();public string LastError="";}
 public class RunChoices{public RunBlessing[] Offer=>new[]{RunBlessing.First,RunBlessing.Second,RunBlessing.Third};public int CompletedWave=1;
 public static string Name(RunBlessing b)=>"祝福"+b;public static string Description(RunBlessing b)=>"既有祝福说明";public static string Association(RunBlessing b,Profile p,bool m)=>"可用";public static int[] UsableRanks(Profile p,bool b)=>new int[10];public static bool IsCompatible(RunBlessing b,int hero,int[] ranks)=>true;}
 public class SessionStub{public PlayerController Player=new PlayerController();public RunChoices RunChoices=new RunChoices();public ProgressionStub Progression=new ProgressionStub();public int Confirmed=-1;public bool Paused;string notification;float notificationUntil;
 NOTIFICATION
 NOTIFY
 public void SetPaused(bool p){Paused=p;}public bool ConfirmBlessing(int i){Confirmed=i;return true;}}
 public sealed partial class GameUI {
 SessionStub session=new SessionStub();int selectedBlessing=-1;float width=568,height=320,TouchRatio=1;Color gold=new Color(),jade=new Color(),pale=new Color(),muted=new Color(),card=new Color();object invisibleButton=new object();string click;
 bool scrolling;Rect scrollViewport,scrollContent;List<Rect> blockedRects=new List<Rect>();List<(string text,Rect rect,bool scroll,bool wrap)> drawn=new List<(string,Rect,bool,bool)>();List<Rect> footer=new List<Rect>();
 float lastBlessingClick=-10;int touchScrollSuppressed=-1;bool UITransitionBlocked=>false;class GestureBoundary{public bool Dragging=>false;}GestureBoundary touchScroll=new GestureBoundary();
 string HubNpcServiceSubtitle(string copy)=>copy;
 string preview="下一房 · 双印净化 · 分开占领，两处印记都完成才能离开";
 string BlessingSubtitle(bool mobile)=>preview;string PlatformText(string t)=>t;int TouchFont(float n)=>(int)Math.Round(n*TouchRatio);
 MobilePanelLayout MobilePanelGeometry()=>new MobilePanelLayout(width/TouchRatio,height/TouchRatio);
 Rect TouchRect(float x,float y,float w,float h)=>new Rect(x*TouchRatio,y*TouchRatio,w*TouchRatio,h*TouchRatio);
 Rect MobilePanelRect(MobilePanelLayout.Area a)=>TouchRect(a.X,a.Y,a.Width,a.Height);
 GUIStyle Style(int n,bool bold,bool wrap,TextAnchor anchor=TextAnchor.UpperLeft)=>new GUIStyle{size=n};
 void Fill(Rect r,Color c){}void Border(Rect r,Color c){}void Rule(float x,float y,float w,Color c){}void ClosePanel(){}void BlockUITransition(){}void CancelMobileScroll(){}
 void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.UpperLeft){drawn.Add((s,r,scrolling,wrap));}
 bool Button(Rect r,string s,Color c,bool enabled=true){if(s=="确认祝福并继续"||s=="暂停 / 存档")footer.Add(r);if(enabled&&click==s){click=null;return true;}return false;}
 bool PopupCloseButton(Rect r,bool enabled=true)=>false;
 
 bool NavigationButton(Rect r,string s,Color c,bool enabled=true)=>Button(r,s,c,enabled);bool PrimaryButton(Rect r,string s,Color c,bool enabled=true)=>Button(r,s,c,enabled);
 Vector2 BeginTouchScroll(string key,Rect viewport,Vector2 p,Rect content){scrolling=true;scrollViewport=viewport;scrollContent=content;return p;}void EndTouchScroll(){scrolling=false;}
 METHODS
 public static int Verify(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
 foreach(float logicalWidth in new[]{568,667,800,1024})foreach(float ratio in new[]{1f,1.8f,3f})foreach(int phase in new[]{0,1,2}){
  var ui=new GameUI{width=logicalWidth*ratio,height=320*ratio,TouchRatio=ratio};Time.unscaledTime=0;ui.session.Notify("首房完成 · 选择本局打法");
  if(phase==1)Time.unscaledTime=7;if(phase==2){Time.unscaledTime=110;ui.session.Progression.LastError="保存失败：存储空间不足，请清理空间后重试。此提示必须保留，不能覆盖下一房预告。";}
  GUI.Click=1;GUI.Index=0;ui.DrawMobileBlessingChoice();
  var shown=ui.drawn.FindAll(x=>x.text==ui.preview&&!x.scroll&&x.wrap);
  check(shown.Count==1,"room preview must remain visible in its own measured fixed row despite Notify or persistent save error");
  check(shown[0].rect.y>=68*ratio&&shown[0].rect.yMax<=ui.scrollViewport.y,"preview ends before reduced list viewport begins");
  check(ui.scrollViewport.height>=48*ratio&&ui.scrollViewport.yMax<=ui.height-56*ratio,"list remains usable without overlapping fixed footer");
  string notice=ui.session.Notification;if(notice.Length>0)check(ui.drawn.Exists(x=>x.text==notice&&x.scroll&&x.wrap),"full notification/error remains separately measured in list");
  check(ui.footer.Count==1&&ui.footer.TrueForAll(r=>r.height==48*ratio&&r.y>=ui.scrollViewport.yMax),"mandatory confirmation is the sole fixed 48-unit footer action");
  check(ui.selectedBlessing==1,"card tap selects intended offer while preview and notification coexist");
  GUI.Click=-1;GUI.Index=0;ui.click="确认祝福并继续";ui.DrawMobileBlessingChoice();
  check(ui.session.Confirmed==1&&ui.selectedBlessing==-1,"subsequent confirmation survives defensive offer copies");
 }
 return n;
 }
 }
}
class Program{static void Main(){Console.WriteLine("PASS: "+Emberfall.GameUI.Verify()+" production mobile blessing/chrome/notification assertions");}}
'''.replace('NOTIFICATION',notification).replace('NOTIFY',notify).replace('METHODS',methods)
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
source=(root/'Assets/Scripts/UI/GameUI.MobileBlessings.cs').read_text()
with tempfile.TemporaryDirectory(prefix='blessing-preview-') as folder:
 out=Path(folder);config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
 for legacy in [False,True]:
  area=out/('legacy' if legacy else 'positive');area.mkdir();code=source
  if legacy:
   code=code.replace('"星烬祝福","选择一项 · 仅本局生效",false,true','"星烬祝福",BlessingSubtitle(true),false,true')
   code=code.replace('float previewHeight=DrawMobileParagraph(layout.Body.X+8,layout.Body.Y,layout.Body.Width-16,preview,13,gold,true)+8;','float previewHeight=0;')
  (area/'Blessings.cs').write_text(code)
  p=cv.write_project(area/'project',[area/'Blessings.cs',root/'Assets/Scripts/UI/MobilePanelLayout.cs'],program=shell)
  subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True)
  command=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')]
  if not legacy:subprocess.run(command,env=env,check=True)
  else:
   result=subprocess.run(command,env=env,capture_output=True,text=True);error=result.stdout+result.stderr
   expected='Unhandled exception. System.Exception: room preview must remain visible in its own measured fixed row despite Notify or persistent save error'
   if result.returncode==0 or not error.splitlines() or error.splitlines()[0]!=expected:raise RuntimeError('wrong old-header failure: '+error)
   print('PASS: legacy header-only preview compiled then failed exact notification-occlusion assertion')
