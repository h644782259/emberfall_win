from MobileBindingFixtureSources import include_mobile_binding_sources
#!/usr/bin/env python3
"""Execute actual slot/basic/meter methods against a recording GUI boundary."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
with tempfile.TemporaryDirectory(prefix='mobile-opportunity-h05-') as t:
 p=Path(t)
 for name in ['Core/CombatOpportunityState','UI/MobileCombatPresentation','UI/MobileControlLayout','UI/MobileOpportunityMeter','UI/MobileControls.Feedback','UI/GameUI.MobileFeedback']:(p/(Path(name).name+'.cs')).write_text((root/'Assets/Scripts'/(name+'.cs')).read_text())
 include_mobile_binding_sources(p,root)
 # Stock badge has its own actual-production HUD suite; this fixture records opportunity draws only.
 stock=p/'GameUI.MobileFeedback.cs';body=stock.read_text();a=body.index('        private void DrawSkillStock(');b=body.index('{',a)+1;depth=1
 while depth:depth+=(body[b]=='{')-(body[b]=='}');b+=1
 stock.write_text(body[:a]+'private void DrawSkillStock(Rect r,int skill,float unit){}'+body[b:])
 (p/'Fixture.cs').write_text((root/'Tests/MobileOpportunityPresentationTests.cs').read_text());(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 def run(oracle=None):
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(q.stdout+q.stderr);q.check_returncode()
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(q.stdout+q.stderr)
  if oracle:assert q.returncode and oracle in q.stdout+q.stderr
  else:q.check_returncode()
 run()
 for name,before,after,oracle in [
 ('MobileControls.Feedback.cs','wordWrap=true,clipping=TextClipping.Overflow','wordWrap=false,clipping=TextClipping.Clip','long pinned identity has measured wrapped bounds'),
 ('GameUI.MobileFeedback.cs','if(!session.InputBlocked)meter.Draw','meter.Draw','pause resume same window never replays acquisition emphasis'),
 ('MobileOpportunityMeter.cs','style.normal.textColor=tint;GUI.color=Color.white;','style.normal.textColor=tint;GUI.color=new Color(1,1,1,opacity);','outer clock opacity applied once'),
 ('MobileOpportunityMeter.cs','state.Fraction','state.Remaining/16f','arc uses actual total grant duration'),
 ('MobileOpportunityMeter.cs','changed||!hadWindow||lastKind!=state.Kind','true','pause resume same window never replays acquisition emphasis'),
 ('GameUI.MobileFeedback.cs','            string rejected=', '            if(window.Actionable)Text(caption,window.Caption,TouchFont(10),jade,true,false,TextAnchor.MiddleCenter);\n            string rejected=', 'inner button has only one unavailable reason'),
 ('BindingMethods.cs','key.Y-15','key.Y+5','opportunity only outside button')]:
  f=p/name;original=f.read_text();assert before in original;f.write_text(original.replace(before,after));run(oracle);f.write_text(original);print('PASS compiled negative:',oracle)
