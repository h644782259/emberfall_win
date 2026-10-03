#!/usr/bin/env python3
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('v',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory(prefix='practice-session-') as directory:
 p=Path(directory);production=p/'Session.cs';production.write_text((root/'Assets/Scripts/Core/GameSession.Practice.cs').read_text());original=production.read_text()
 player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text();a=player.index('public void Initialize(GameSession game, HeroClass heroClass)');b=player.index('{',a)+1;depth=1
 while depth:depth+=(player[b]=='{')-(player[b]=='}');b+=1
 actual=p/'Initialize.cs';actual.write_text('using UnityEngine;namespace Emberfall{public partial class PlayerController{'+player[a:b]+'}}')
 enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
 def member(s,key):
  a=s.index(key);b=s.index('{',a)+1;n=1
  while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
  return s[a:b]
 lifecycle=p/'EnemyLifecycle.cs';lifecycle.write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{'+''.join(member(enemy,k) for k in ['public bool IsAggro','public bool IsPreparingAttack','private void OnDisable()','private void CancelAttack(','private void ClearWarning()'])+'}}')
 session=(root/'Assets/Scripts/Core/GameSession.cs').read_text();shutdown=p/'Shutdown.cs';shutdown.write_text('using System;using UnityEngine;namespace Emberfall{public sealed partial class GameSession{'+''.join(member(session,k) for k in ['public bool SaveBeforeLeaving()','private bool CanQuitSafely()','private void SaveOnApplicationQuit()','private void OnApplicationPause(bool pause)','private void OnApplicationFocus(bool focus)','private void OnApplicationQuit()','private void OnDestroy()','private void UpdateTimeScale()','public bool TryCollectGroundLoot(string itemId'])+'}}')
 expedition=(root/'Assets/Scripts/Core/GameSession.Expedition.cs').read_text()
 telemetry=p/'Telemetry.cs';telemetry.write_text('using System;using UnityEngine;namespace Emberfall{public sealed partial class GameSession{'+member(expedition,'public void RecordIncomingDamage(')+member(expedition,'public void RecordActualHealing(')+member(expedition,'public void OnEnemyInterrupted(').split('            if(!enemy.IsLargeBossCounterWindow)')[0]+'throw new Exception("ordinary interrupt outside fixture");}public void TestKill(EnemyController enemy){'+member(member(session,'public void OnEnemyKilled('),'if(PracticeActive)')+'throw new Exception("ordinary kill outside fixture");}public void TestDeath(){'+member(member(session,'public void OnPlayerDied()'),'if(PracticeActive)')+'throw new Exception("ordinary death outside fixture");}}public partial class PlayerController{'+member(player,'public void Heal(')+'}}')
 project=m.write_project(p/'project',[production,actual,lifecycle,shutdown,telemetry,root/'Assets/Scripts/Core/ApplicationPauseState.cs',root/'Assets/Scripts/Core/SaveLifecycleGate.cs',root/'Assets/Scripts/Core/CombatImpactBatch.cs',root/'Assets/Scripts/Core/CampPracticeRecord.cs',root/'Tests/CampPracticeSessionBoundary.cs'],'using System;class Program{static void Main(){Console.WriteLine(CampPracticeSessionTests.Run());}}')
 config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 def run(args):
  q=subprocess.run([dotnet]+args,env=env,capture_output=True,text=True);print(q.stdout+q.stderr);return q
 assert run(['restore',str(project),'--configfile',str(config),'-v:q']).returncode==0
 assert run(['run','--project',str(project),'--no-restore']).returncode==0
 originalTelemetry=telemetry.read_text()
 for old,new,expected in [('if(PracticeActive){PracticeRecord.IncomingDamage(amount);return;}','','actual clipped Heal and damage telemetry stay out of original run'),('if(PracticeActive){PracticeRecord.Healing(amount);return;}','','actual clipped Heal and damage telemetry stay out of original run')]:
  assert old in originalTelemetry;telemetry.write_text(originalTelemetry.replace(old,new));assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
  q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll')]);assert q.returncode!=0 and expected in q.stdout+q.stderr;print('PASS compiled telemetry isolation negative:',expected);telemetry.write_text(originalTelemetry)
 originalShutdown=shutdown.read_text();old='if(PracticeActive)\n            {try{EndPractice("保存或离开 · 试招结束，恢复原角色");}\n             catch(System.Exception exception){Debug.LogException(exception);Notify("试招收尾出现异常，请重试保存退出。");return false;}}';assert old in originalShutdown
 shutdown.write_text(originalShutdown.replace(old,'if(PracticeActive)return false;'));assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
 q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll')]);assert q.returncode!=0 and 'actual CanQuitSafely restores owner then saves once' in q.stdout+q.stderr;print('PASS compiled negative control: original OS-close veto');shutdown.write_text(originalShutdown)
 for old,new,message in [('PreviousPracticeRecord==null&&PracticeRecord.Started','PracticeRecord.Started','baseline A is a separate frozen first result across repeated B'),('PreviousPracticeRecord=PracticeRecord.FrozenCopy()','PreviousPracticeRecord=PracticeRecord','baseline A is a separate frozen first result across repeated B'),('PracticeRecord.Prepare();','','prepare freezes real session before timer and attacks'),('&&!practiceSupplier.IsDead','','actual kill removes supply once and records order'),('if(!PracticeEntrySafe())return false;','if(false)return false;','unsafe entry refused before real enemy OnDisable'),('Player=practiceOriginalPlayer','Player=Player','timed finish restores exact references'),('practiceRandom=UnityEngine.Random.state','practiceRandom=default(UnityEngine.Random.State)','random state restored')]:
  assert old in original;production.write_text(original.replace(old,new));assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
  q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll')]);assert q.returncode!=0 and message in q.stdout+q.stderr;print('PASS compiled negative control:',message);production.write_text(original)
