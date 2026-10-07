"""Execute the real recap tip renderer and scroll extent with a measured-font boundary.
No Unity fonts, rendering or physical touch delivery is claimed.
"""
from pathlib import Path
import os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(root/'Assets/Scripts/UI/GameUI.RunRecap.cs').read_text()
methods=''.join(member(s,x) for x in ['private float RecapTextHeight(','private float RecapTipHeight(','private float DrawRecapTip(','private float RecapContentHeight('])
assert 'recapGenerationDetailsExpanded=false;' in member(s,'private bool DrawStructuredRunRecap(')
cards=member(s,'private void DrawRecapCards(')
assert cards.index('if(data.HasGenerationFailure)y=DrawRecapTip')<cards.index('data.MechanismEvidence')
shell=r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
namespace UnityEngine{
 public struct Color{public Color(float r,float g,float b,float a=1){}}
 public struct Rect{public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public static class Mathf{public static float Ceil(float a)=>(float)Math.Ceiling(a);public static int RoundToInt(float a)=>(int)Math.Round(a);public static float Max(float a,float b)=>Math.Max(a,b);}
 public enum TextAnchor{MiddleLeft}public class GUIContent{public string text;public GUIContent(string s){text=s;}}
 public class GUIStyle{public int Font;public static float TextScale=1;public float CalcHeight(GUIContent c,float w){int lines=0;foreach(string l in c.text.Split('\n'))lines+=Math.Max(1,(int)Math.Ceiling(l.Length*Font*TextScale/Math.Max(1,w)));return lines*Font*1.5f*TextScale;}}
}
namespace Emberfall{
 public static class MobileControls{public static bool Active;}
 public static class UIIconAtlas{public static object Utility(string s)=>null;}
 public sealed partial class GameUI{
  bool recapGenerationDetailsExpanded,click;float TouchRatio=1;Color jade,pale,muted;Rect card,button;
  struct Label{public Rect Rect;public string Text;public int Font;public Label(Rect r,string t,int f){Rect=r;Text=t;Font=f;}}
  List<Label> labels=new List<Label>();static int checks;
  GUIStyle Style(int font,bool bold,bool wrap)=>new GUIStyle{Font=font};void Fill(Rect r,Color c){card=r;}void DrawIcon(Rect r,object icon,Color c){}
  void Text(Rect r,string t,int font,Color c,bool bold,bool wrap,TextAnchor anchor){labels.Add(new Label(r,t,font));}
  bool Button(Rect r,string text,Color color){button=r;bool result=click;click=false;return result;}
  float RecapGoalHeight(RunRecapLayout l,float u,RunRecapPresentation d)=>0;static float ProgressCardHeight(RunRecapSnapshot d)=>0;
  static void C(bool b,string why){checks++;if(!b)throw new Exception(why);}
  public static void Run(){
   string cause="入口无法到达出生点，需要重新构建本次房间后继续远征";
   string detail=cause+"\n发生位置：印记模型与进度 2\n房间 3 · 守印\n"+new string('详',160);
   var snapshot=new RunRecapSnapshot(false,true,true,15,3,5,12345,0,12,null,0,null,null,null,false,false,failureReason:"GenerationOrPathFailure",mechanismCounts:new[]{3,0,4,0},generationFailureDetail:detail);
   var data=new RunRecapPresentation(snapshot);
   foreach(float w in new[]{220f,320f,488f,960f})foreach(float unit in new[]{.75f,1f,1.5f})foreach(float textScale in new[]{1f,1.5f,2f}){
    var ui=new GameUI();ui.TouchRatio=unit;GUIStyle.TextScale=textScale;MobileControls.Active=true;var layout=new RunRecapLayout(w+80,500,true); // viewport geometry is exercised separately below.
    for(int repeat=0;repeat<8;repeat++){
     bool expanded=ui.recapGenerationDetailsExpanded;ui.labels.Clear();ui.click=true;float expected=ui.RecapTipHeight(data,w,unit,expanded);float end=ui.DrawRecapTip(data,17,w,unit);
     C(Math.Abs(end-17-expected)<.001f&&Math.Abs(ui.card.height-expected*unit)<.001f,"drawn card uses measured content height");
     foreach(var label in ui.labels){float measured=new GUIStyle{Font=label.Font}.CalcHeight(new GUIContent(label.Text),label.Rect.width);C(label.Rect.height+.01f>=measured,"wrapped diagnostic text is never clipped to fixed height");C(label.Rect.x>=ui.card.x&&label.Rect.xMax<=ui.card.xMax+.01f&&label.Rect.y>=ui.card.y&&label.Rect.yMax<=ui.card.yMax+.01f,"text stays inside measured card");}
     C(ui.labels.Count==(expanded?2:1)&&ui.button.height>=44*unit,"only expanded details render and toggle retains touch height");
     C(ui.recapGenerationDetailsExpanded!=expanded&&ReferenceEquals(snapshot,data.Snapshot)&&data.GenerationFailureDetails==detail,"repeated toggles preserve immutable failure evidence");
     ui.recapGenerationDetailsExpanded=false;float collapsed=ui.RecapContentHeight(layout,data);ui.recapGenerationDetailsExpanded=true;float open=ui.RecapContentHeight(layout,data);
     C(Math.Abs((open-collapsed)-(ui.RecapTipHeight(data,layout.ContentWidth,unit,true)-ui.RecapTipHeight(data,layout.ContentWidth,unit,false)))<.01f,"scroll extent tracks expanded details");
     ui.recapGenerationDetailsExpanded=!expanded;
    }
   }
   Console.WriteLine("PASS "+checks+" actual recap measurement/draw/toggle/scroll assertions; font and GUI delivery are managed boundaries");
  }
 }
}
class Program{static void Main(){GameUI.Run();}}
'''
with tempfile.TemporaryDirectory(prefix='room-recap-layout-') as d:
 p=Path(d)
 for name in ['RunRecapPresentation']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/UI'/(name+'.cs')).read_text())
 (p/'Shell.cs').write_text(shell);method=p/'Methods.cs';good='using UnityEngine;namespace Emberfall{public sealed partial class GameUI{'+methods+'}}';method.write_text(good)
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>');cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 build=[dotnet,'build',str(project),'--configfile',str(cfg),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for before,after,oracle in [('(summary-18)*unit','39*unit','wrapped diagnostic text is never clipped to fixed height'),('return result+RecapTipHeight(data,layout.ContentWidth,MobileControls.Active?TouchRatio:1,recapGenerationDetailsExpanded)+6;','return result+56+6;','scroll extent tracks expanded details')]:
  assert before in good;method.write_text(good.replace(before,after));subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL);r=subprocess.run(run,env=env,text=True,capture_output=True);assert r.returncode and 'System.Exception: '+oracle in r.stdout+r.stderr,r.stdout+r.stderr;print('PASS compiled negative:',oracle);method.write_text(good)
