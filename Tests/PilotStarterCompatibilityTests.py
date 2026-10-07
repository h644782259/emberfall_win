#!/usr/bin/env python3
"""Real new-character progression items through the actual pilot compatibility and equipment gate.
Unity transport is the existing managed test double; this is not an engine import/render test.
"""
from pathlib import Path
import importlib.util, os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(text,key):
 a=text.index(key);b=text.index('{',a)+1;depth=1
 while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
helper=member((root/'Assets/Scripts/Combat/CombatModel.BlenderPilot.cs').read_text(),'private static bool PilotStarterCompatible(')
model=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text();apply=member(model,'public void ApplyEquipment(')
a=apply.index('pilotHasGear =');b=apply.index(';',a)
gate=apply[a:b].replace('pilotHasGear =','return')+';'
program=r'''
using System;using System.IO;using Emberfall;using UnityEngine;
class Program {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 HELPER
 static bool Unsupported(ItemData weapon,ItemData armor,ItemData relic){GATE}
 static ItemData Copy(ItemData item)=>JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item,true));
 static void Main(string[] args){
  var p=new ProgressionService(Path.Combine(args[0],"actual-starter"));p.NewGame(HeroClass.Vanguard);
  var weapon=p.Equipped(ItemSlot.Weapon);var armor=p.Equipped(ItemSlot.Armor);var relic=p.Equipped(ItemSlot.Relic);
  Check(weapon!=null&&armor!=null&&relic!=null,"actual CreateProfile supplies all three starter slots");
  string before=JsonUtility.ToJson(p.Profile,true);
  Check(!Unsupported(weapon,armor,relic),"actual new-character kit enables imported pilot");
  Check(!Unsupported(null,null,null),"null diagnostic outfit remains supported");
  foreach(ItemSlot slot in new[]{ItemSlot.Weapon,ItemSlot.Armor,ItemSlot.Relic}) {
   var actual=p.Equipped(slot);Check(PilotStarterCompatible(actual,slot),"actual starter slot accepted");
   foreach(Action<ItemData> change in new Action<ItemData>[] {
    x=>x.name="other common drop",x=>x.level=2,x=>x.rarity=Rarity.Rare,x=>x.upgradeLevel=1,
    x=>x.mechanic=EquipmentMechanic.ReturningBlade,x=>x.mechanicVariantUnlocked=true,
    x=>x.slot=slot==ItemSlot.Weapon?ItemSlot.Armor:ItemSlot.Weapon}) {
    var item=Copy(actual);change(item);
    Check(!PilotStarterCompatible(item,slot),"nonstarter appearance must fall back");
    Check(Unsupported(slot==ItemSlot.Weapon?item:weapon,slot==ItemSlot.Armor?item:armor,slot==ItemSlot.Relic?item:relic),"actual equipment gate rejects each unsupported slot");
   }
  }
  Check(before==JsonUtility.ToJson(p.Profile,true),"appearance admission never mutates real equipment stats or save state");
  Check(p.Load()&&!Unsupported(p.Equipped(ItemSlot.Weapon),p.Equipped(ItemSlot.Armor),p.Equipped(ItemSlot.Relic)),"actual saved starter survives reload admission");
  Console.WriteLine("PASS: "+checks+" actual starter creation and pilot compatibility assertions");
 }
}
'''.replace('HELPER',helper).replace('GATE',gate)
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
with tempfile.TemporaryDirectory(prefix='pilot-starter-') as folder:
 out=Path(folder);sources=[root/'Assets/Scripts/Core'/(x+'.cs') for x in core]+[root/'Tests/ProgressionTests.cs']
 code=out/'Replay.cs';code.write_text(program);project=cv.write_project(out/'project',sources+[code],program='')
 config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 variants=[('current',program,None),('legacy-all-gear-fallback',program.replace(gate,'return weapon!=null||armor!=null||relic!=null;'),'actual new-character kit enables imported pilot'),('broad-starter-fallback',program.replace('item.mechanic==EquipmentMechanic.None','true'),'nonstarter appearance must fall back')]
 for name,text,expected in variants:
  code.write_text(text);build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
  run=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(out/name)],env=env,capture_output=True,text=True)
  if expected:assert run.returncode!=0 and expected in run.stdout+run.stderr,run.stdout+run.stderr;print('PASS: compiled '+name+' fails exact admission assertion')
  else:assert run.returncode==0,run.stdout+run.stderr;print(run.stdout,end='')
