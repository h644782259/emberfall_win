#!/usr/bin/env python3
"""Compile the actual level callback and budget helpers; presentation objects are shells."""
from pathlib import Path
import os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
def member(text,key):
 a=text.index(key);b=text.index('{',a)+1;depth=1
 while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
source=(root/'Assets/Scripts/Core/GameSession.cs').read_text();types=(root/'Assets/Scripts/Core/GameTypes.cs').read_text()
callback=member(source,'private void OnLevelUp(')
budget='\n'.join(member(types,k) for k in ['public static int SkillPointBudget(', 'public static int SkillPointsGainedAtLevel('])
code=r'''
using System;using System.Collections.Generic;
namespace Emberfall {
struct Vector3 { public static Vector3 up=>new Vector3();public static Vector3 operator +(Vector3 a,Vector3 b)=>a;public static Vector3 operator *(Vector3 a,float b)=>a; }
struct Color { public Color(float r,float g,float b){} }
class Transform { public Vector3 position; }
class Hero { public Transform transform=new Transform();public int Heals;public void RefreshStats(bool heal){if(heal)Heals++;} }
enum SoundCue {LevelUp} static class GameAudio {public static int Calls;public static void Play(SoundCue cue){Calls++;} }
static class GameBalance { BUDGET }
class ProgressionService{public string LevelGrowthDescription()=>"base stat growth";}
class GameSession {
 public ProgressionService Progression=new ProgressionService();
 public Hero Player=new Hero();public List<string> Floating=new List<string>(),Notices=new List<string>();
 void SpawnFloatingText(Vector3 position,string text,Color c){Floating.Add(text);}void Notify(string text){Notices.Add(text);}
 CALLBACK
 public void Invoke(int level){OnLevelUp(level);}
}
class Program {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){
  var game=new GameSession();
  foreach(int level in new[]{2,3,4,10,100}) {
   int gain=level==2?0:1;game.Invoke(level);
   Check(game.Floating[game.Floating.Count-1]=="LEVEL "+level+"  +"+gain+" SP","floating level callback must show actual point gain");
   Check(game.Notices[game.Notices.Count-1].Contains("获得 "+gain+" 技能点"),"notice and floating gain agree");
  }
  Check(game.Player.Heals==5&&GameAudio.Calls==5,"presentation change preserves level heal and sound");
  var multi=new GameSession();for(int level=2;level<=5;level++)multi.Invoke(level);
  Check(string.Join("|",multi.Floating)=="LEVEL 2  +0 SP|LEVEL 3  +1 SP|LEVEL 4  +1 SP|LEVEL 5  +1 SP","multi-level callback sequence reports each real delta");
  multi.Player=null;int before=multi.Floating.Count;multi.Invoke(2);
  Check(multi.Floating.Count==before&&multi.Notices[multi.Notices.Count-1].Contains("获得 0 技能点"),"no player still receives correct notice without floating");
  Console.WriteLine("PASS: "+checks+" actual OnLevelUp feedback assertions");
 }
}
}
'''.replace('BUDGET',budget).replace('CALLBACK',callback)
with tempfile.TemporaryDirectory(prefix='level-feedback-') as folder:
 out=Path(folder);path=out/'Replay.cs';project=out/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 old='"LEVEL " + level + "  +" + GameBalance.SkillPointsGainedAtLevel(level) + " SP"';assert old in code
 for mutation in [False,True]:
  path.write_text(code.replace(old,'"LEVEL " + level + "  +1 SP"') if mutation else code)
  build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
  result=subprocess.run([dotnet,str(out/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True)
  if mutation:assert result.returncode!=0 and 'floating level callback must show actual point gain' in result.stderr,result.stdout+result.stderr;print('PASS: compiled legacy hardcoded +1 SP fails precise feedback assertion')
  else:assert result.returncode==0,result.stdout+result.stderr;print(result.stdout,end='')
