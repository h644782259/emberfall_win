"""H03: real UI partial and model entry, actual progression receipts; managed GUI recording only."""
from pathlib import Path
import os,sys,tempfile,subprocess
r=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(relative,signature):
 s=(r/relative).read_text();a=s.index(signature);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
shell='''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);}
 public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float a)=>(int)Math.Round(a);}
}
namespace Emberfall{
 public static class EquipmentComparisonPresentation{TRIAL RECEIPT}
 public sealed partial class GameUI{
 Color gold=new Color(1,1,0),pale=new Color(1,1,1);string collectionReceiptKey;FashionData lastModel;
 readonly List<Rect> fills=new List<Rect>();readonly List<string> texts=new List<string>();int chests;
 void Fill(Rect r,Color c){fills.Add(r);}void Border(Rect r,Color c){}void Text(Rect r,string s,int font,Color c,bool bold=false,bool wrap=false){texts.Add(s);}
 void DrawRewardChest(Rect r,bool open,float alpha,float progress){chests++;}
 void DrawChestGold(Rect r,Color c){}void SetCollectionAngle(FashionSlot s){}void DrawCollectionModel(Rect r,FashionData f,bool interactive){lastModel=f;}
 MODEL
 public static void CheckCard(Rect r,int choice,GameProfile p,float scale){var ui=new GameUI();ui.DrawSingleChestCard(r,scale);
 if(ui.chests!=1||!ui.texts.Contains("通关宝箱"))throw new Exception("real card uses actual choice detail");
 foreach(var f in ui.fills)if(f.width<=0||f.height<=0||f.x<r.x-.01f||f.y<r.y-.01f||f.x+f.width>r.x+r.width+.01f||f.yMax>r.yMax+.01f)throw new Exception("card foreground bounds");}
 public static string EmblemTrace(int choice){var ui=new GameUI();ui.DrawChestChoiceEmblem(new Rect(0,0,100,100),choice,ui.gold);return string.Join(";",ui.fills.ConvertAll(f=>$"{f.x},{f.y},{f.width},{f.height}"));}
 public static FashionData Model(ChestReward receipt){var ui=new GameUI();ui.DrawChestRewardModel(new Rect(0,0,200,200),receipt);return ui.lastModel;}
 public static string Gold(ChestReward receipt){var ui=new GameUI();ui.DrawChestGoldReward(new Rect(0,0,200,200),receipt,ui.gold);return string.Join("\\n",ui.texts);}
 }}
class Program{static void Main(string[] args){ChestChoicePresentationTests.Run(args[0]);}}
'''.replace('TRIAL',member('Assets/Scripts/UI/EquipmentComparisonPresentation.cs','public static FashionData Trial(')).replace('RECEIPT',member('Assets/Scripts/UI/EquipmentComparisonPresentation.cs','public static FashionData Receipt(')).replace('MODEL',member('Assets/Scripts/UI/GameUI.CollectionPreview.cs','private bool DrawChestRewardModel('))
# Check both production callers use the executed shared drawing and receipt presentation.
for filename in ['GameUI.Rewards.cs','GameUI.MobileRewards.cs']:
 source=(r/'Assets/Scripts/UI'/filename).read_text()
 assert ('DrawSingleChestCard(' in source) and 'ChestRevealPresentation.ChoiceDisclosure' in source
 assert 'ChestRevealPresentation.ResultWithCollection(reward,session.Progression.Profile)' in source
 assert 'DrawChestCommittedReward(' in source
core=['SafeSaveFlow','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','CastFirstHitReceipt','TierRewardRules','TierRewardBand','ProgressionGoalState']
with tempfile.TemporaryDirectory(prefix='h03-chest-ui-') as tmp:
 p=Path(tmp)
 for name in core:(p/(name+'.cs')).write_text((r/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for name in ['ProgressionTests','ChestChoicePresentationTests']:(p/(name+'.cs')).write_text((r/'Tests'/(name+'.cs')).read_text())
 for name in ['ChestRevealPresentation','GameUI.ChestChoices']:(p/(name+'.cs')).write_text((r/'Assets/Scripts/UI'/(name+'.cs')).read_text())
 (p/'Program.cs').write_text(shell)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 cmd=[dotnet,'run','--project',str(p/'Test.csproj'),'--',str(p/'saves')]
 subprocess.run(cmd,env=env,check=True)
 # Exact regressions: list length instead of distinct ranks, legacy supply reinterpretation, identical silhouettes.
 mutations=[('ChestRevealPresentation.cs','return ranks.Count;','return profile==null?0:profile.fashions.Count;','count distinct valid ranks'),
 ('ChestRevealPresentation.cs','reward.rulesRevision==1&&','', 'legacy gold not relabelled supply'),
 ('GameUI.ChestChoices.cs','if(choice==0)','if(false)','three distinct production emblem silhouettes')]
 for filename,old,new,expected in mutations:
  path=p/filename;original=path.read_text();assert old in original;path.write_text(original.replace(old,new))
  result=subprocess.run(cmd,env=env,text=True,capture_output=True)
  print('NEGATIVE CONTROL:',filename,old,flush=True);print(result.stdout+result.stderr,flush=True)
  assert result.returncode and expected in result.stdout+result.stderr
  path.write_text(original)
 print('PASS: H03 three regression controls rejected')
