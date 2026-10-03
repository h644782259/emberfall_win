"""Real first-trial routing method and real save acknowledgement; renderer is a shell."""
from pathlib import Path
import tempfile,subprocess,os,sys
r=Path(__file__).resolve().parents[1]
def member(sig):
 s=(r/'Assets/Scripts/UI/GameUI.Rewards.cs').read_text();a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
shell=r'''
using System;using System.IO;using Emberfall;
namespace Emberfall{public class TrialSession{public ProgressionService Progression;public object Player=new object();public bool Blocked;public void SetUIBlocking(bool v){Blocked=v;}public void LogSystem(string s){}}
public partial class GameUI{enum Panel{Chests,Fashion}Panel panel=Panel.Chests;TrialSession session;bool ChestAnimationDone=true,mobileFashionPreview;string chestRevealResult="saved";object collectionOwner;CollectionViewingState collectionViewing=new CollectionViewingState();int trials,blocks;
void Feedback(bool ok,string why){}void ResetChestReveal(){chestRevealResult=null;}void TrialFashion(FashionSlot slot,Rarity rarity){trials++;collectionViewing.TryOn(new FashionData{slot=slot,rarity=rarity});}void BlockUITransition(){blocks++;}
CAN
ACCEPT
public static void Verify(string root){var p=new ProgressionService(root);if(!p.CreateNewSlot(HeroClass.Vanguard))throw new Exception("fixture");p.Profile.pendingChestReveal=true;p.Profile.lastChestReward=new ChestReward{id=Guid.NewGuid().ToString("N"),gold=60,rarityIndex=1,slotIndex=0,duplicate=false};p.Save();var ui=new GameUI{session=new TrialSession{Progression=p}};
Directory.CreateDirectory(p.SaveFilePath+".tmp");if(ui.AcceptChestForTrial()||ui.panel!=Panel.Chests||ui.trials!=0||!p.Profile.pendingChestReveal)throw new Exception("failed acknowledgement cannot open trial or discard receipt");Directory.Delete(p.SaveFilePath+".tmp");
if(!ui.AcceptChestForTrial()||ui.panel!=Panel.Fashion||ui.trials!=1||!ui.mobileFashionPreview||!ui.session.Blocked||p.Profile.pendingChestReveal||ui.collectionOwner!=ui.session.Player)throw new Exception("durable receipt opens one owned trial without unblocking gameplay");
if(ui.AcceptChestForTrial()||ui.trials!=1)throw new Exception("repeated button cannot repeat transition");if(!p.LoadSlot(p.CurrentSlotId)||p.Profile.pendingChestReveal)throw new Exception("acknowledgement survives restart");Console.WriteLine("PASS: production first-trial acknowledgement failure, success and repeat routing");}
}}
class Program{static void Main(string[] args){GameUI.Verify(args[0]);}}
'''.replace('CAN',member('private static bool CanTrialChestReward(')).replace('ACCEPT',member('private bool AcceptChestForTrial()'))
core=['GameTypes','ProgressionService','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','CastFirstHitReceipt','TierRewardRules','TierRewardBand','ProgressionGoalState']
with tempfile.TemporaryDirectory(prefix='chest-trial-') as t:
 p=Path(t);(p/'Program.cs').write_text(shell)
 files=[r/'Assets/Scripts/Core'/f'{n}.cs' for n in core]+[r/'Tests/ProgressionTests.cs',r/'Assets/Scripts/UI/CollectionViewingState.cs',r/'Assets/Scripts/UI/CollectionPreviewComposition.cs']
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup><ItemGroup>'+''.join(f'<Compile Include="{f}"/>' for f in files)+'</ItemGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(p/'Test.csproj'),'--',str(p/'save')]
 subprocess.run(cmd,env=env,check=True)
 # Removing the old-safe acknowledgement gate must fail the save-failure oracle.
 (p/'Program.cs').write_text(shell.replace('if(!session.Progression.AcknowledgeChestReward()){Feedback(false,"无法保存奖励确认");return false;}','session.Progression.AcknowledgeChestReward();'))
 result=subprocess.run(cmd,env=env,capture_output=True,text=True)
 assert result.returncode and 'failed acknowledgement cannot open trial' in result.stdout+result.stderr,'early trial mutation must fail expected oracle'
 print('PASS: ignored-save-failure first-trial negative control rejected')
