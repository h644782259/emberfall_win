"""Actual four settlement methods and persistence; scene completion shells, not Unity.
Optional --source-ref freezes production inputs for a reproducible prior implementation.
"""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
ref=sys.argv[sys.argv.index('--source-ref')+1] if '--source-ref' in sys.argv else None
def read(path):return subprocess.check_output(['git','show',ref+':'+path],cwd=root,text=True) if ref else (root/path).read_text()
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
mode=read('Assets/Scripts/Core/GameSession.Modes.cs')
methods=member(mode,'private static long TotalEarnedExperience(')+member(mode,'public bool TrySettleArenaReward(')
for path,key in [('DungeonRewards','public bool TrySettleDungeonReward('),('RoomChain','private bool TrySettleRoomReward('),('Chapter','public bool TrySettleChapterReward(')]:methods+=member(read('Assets/Scripts/Core/GameSession.'+path+'.cs'),key)
for key in ['private void ApplyRewardPresentation(','private bool ApplyRewardPresentation(','private string RewardPresentationText(']:
 if key in mode:methods+=member(mode,key)
host='''using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static int RoundToInt(float x)=>(int)Math.Round(x);}}
namespace Emberfall{public enum RunBlessing{RiskContract}
// Only terminal scene/run state is supplied; reward methods, receipts and saves are production.
public class CompletedRoomShell{public bool Finished=true,Failed,RewardClaimed;public void ClaimReward(bool saved){RewardClaimed=saved;}}
public class CompletedChapterShell{public bool RewardClaimed;public void ClaimReward(){RewardClaimed=true;}}
public sealed partial class GameSession{
public bool HasStarted=true,ChapterActive;public ProgressionService Progression;public int DungeonTier=1;
public ExpeditionModeState ModeRun;public CompletedRoomShell RoomChainRun;public CompletedChapterShell ChapterRun;
public ChapterNode ActiveChapterNode;public ChapterResultSnapshot ChapterResult;public ChapterRunReceipt chapterReceipt;
public string modeReceipt,pendingDungeonRewardId,LastRunSummary;public int pendingDungeonRewardTier=1,pendingDungeonRewardGold=150,pendingDungeonRewardExperience=120;
public bool DungeonRewardPending=>pendingDungeonRewardId!=null;public bool ChapterRewardPending=>ChapterRun!=null&&!ChapterRun.RewardClaimed;
public int modeGoldReward,modeXpReward,modeMaterialReward;public bool modeRewardDetailsUnavailable;
public List<string> Logs=new List<string>(),Notifications=new List<string>();bool HasBlessing(RunBlessing b)=>false;
void Notify(string s){Notifications.Add(s);}void LogSystem(string s){Logs.Add(s);}string BuildRunSummary(bool win)=>modeGoldReward+"/"+modeXpReward+"/"+modeMaterialReward;
ChapterResultSnapshot CaptureChapterResult(bool failed,string reason)=>new ChapterResultSnapshot(ActiveChapterNode,ChapterDifficulty.Normal,1,0,false,0,0,0,0,failed,reason,null,0,0);
public bool Settle(string kind)=>kind=="dungeon"?TrySettleDungeonReward():kind=="room"?TrySettleRoomReward():kind=="arena"?TrySettleArenaReward():TrySettleChapterReward();
public static long Earned(GameProfile p)=>TotalEarnedExperience(p);
'''+methods+('public string ReadDisplay()=>RewardPresentationText("回执");' if 'private string RewardPresentationText(' in mode else 'public string ReadDisplay()=>LastRunSummary;')+'}}'
core=['SafeSaveFlow','GameTypes','ProgressionService','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','ChapterResultSnapshot','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','CastFirstHitReceipt','TierRewardRules','TierRewardBand','ProgressionGoalState','ExpeditionModeState']
with tempfile.TemporaryDirectory(prefix='reward-presentation-') as folder:
 p=Path(folder)
 for name in core:(p/(name+'.cs')).write_text(read('Assets/Scripts/Core/'+name+'.cs'))
 (p/'ChapterEntryPresentation.cs').write_text(read('Assets/Scripts/UI/ChapterEntryPresentation.cs'))
 (p/'ProgressionTests.cs').write_text(read('Tests/ProgressionTests.cs'))
 (p/'Cases.cs').write_text((root/'Tests/RewardPresentationExceptionCases.cs').read_text())
 (p/'Host.cs').write_text(host)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 print('PRODUCTION SOURCE',ref or 'working tree',flush=True)
 subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll'),str(p/'saves')],env=env)
 sys.exit(q.returncode)
