"""Real practice/session input branch, potion action and mobile latch; engine/save boundaries managed."""
from pathlib import Path
import sys,os,tempfile,subprocess,importlib.util
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
# Reuse the current scene ownership boundary; production methods are added below.
fixture=(root/'Tests/CampPracticeSessionBoundary.cs').read_text().split('public static class CampPracticeSessionTests')[0]
fixture=fixture.replace('public enum KeyCode{H}','public enum KeyCode{H,F}').replace('public static bool GetKeyDown(KeyCode code){return false;}','public static HashSet<KeyCode> Down=new HashSet<KeyCode>();public static bool GetKeyDown(KeyCode code){return Down.Contains(code);}')
fixture=fixture.replace('public int level=10;','public int level=10;public int potions=5;')
fixture=fixture.replace('public int Collections;', 'public int Commits;private bool Fail(string reason){LastError=reason;return false;}private GameProfile Snapshot()=>new GameProfile{potions=Profile.potions};private bool CommitCandidate(GameProfile candidate){Profile=candidate;Commits++;return true;}'+member((root/'Assets/Scripts/Core/ProgressionService.cs').read_text(),'public bool UsePotion()')+'public int Collections;')
fixture=fixture.replace('public void DrinkPotion(){throw new Exception("potion outside this session lifecycle fixture");}','')
# Both the old and new lifecycle boundary can be used while sharing the fixture.
fixture=fixture.replace('public static class MobileControls{public static bool ConsumePotion()=>false;}','')
mobile=(root/'Assets/Scripts/UI/MobileControls.cs').read_text();pointer=member(mobile,'else if (Potion.Contains(point))');pointer=pointer.replace('else if','if',1)
fixture+='namespace Emberfall{public static class MobileControls{public static bool Active=true;private static bool potion;public static int Feedback;private enum Role{Consumed}private class HitBox{public bool Contains(int point)=>true;}private static HitBox Potion=new HitBox();private static void CheckPotionFeedback(){Feedback++;}public static void TouchPotion(){int point=0;Role role;'+pointer+'}'+member(mobile,'public static bool ConsumePotion()')+'}}'
session=(root/'Assets/Scripts/Core/GameSession.cs').read_text();player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text();enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text();expedition=(root/'Assets/Scripts/Core/GameSession.Expedition.cs').read_text()
actual='using System;using UnityEngine;namespace Emberfall{public partial class GameSession{public void OnEnemyInterrupted(EnemyController enemy){}public bool ChallengeRun;private bool TrySpendHealingCharge(){throw new Exception("practice cannot touch run charge");}public void InputUpdate(){'+member(member(session,'private void Update()'),'if(PracticeActive)')+'}'+member(session,'public void DrinkPotion()')+''.join(member(session,k) for k in ['public bool SaveBeforeLeaving()','private bool CanQuitSafely()','private void SaveOnApplicationQuit()','private void OnApplicationPause(bool pause)','private void OnApplicationFocus(bool focus)','private void OnApplicationQuit()','private void OnDestroy()','private void UpdateTimeScale()'])+member(expedition,'public void RecordActualHealing(')+'}public partial class PlayerController{'+member(player,'public void Initialize(GameSession game, HeroClass heroClass)')+member(player,'public void Heal(')+'}public partial class EnemyController{'+''.join(member(enemy,k) for k in ['public bool IsAggro','public bool IsPreparingAttack','private void OnDisable()','private void CancelAttack(','private void ClearWarning()'])+'}}'
program=r'''using System;using UnityEngine;using Emberfall;class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(){GameObject.Roots.Clear();var s=new GameObject("session").AddComponent<GameSession>();s.world=new GameObject("world");s.Player=new GameObject("real hero").AddComponent<PlayerController>();var real=s.Player;var owner=s.Progression;int saved=owner.Saves;float hp=real.Health;C(s.BeginPractice(CampPracticeScenario.Stationary,10),"begin practice");var temp=s.Player;temp.Health=1;Time.deltaTime=.1f;
C(s.StartPractice(),"start");MobileControls.TouchPotion();s.InputUpdate();C(temp.Health==51&&s.Progression.Profile.potions==4,"live mobile input reaches actual practice potion once");s.InputUpdate();C(temp.Health==51&&s.Progression.Profile.potions==4,"mobile latch consumed once");temp.Health=1;Input.Down.Add(KeyCode.F);MobileControls.TouchPotion();s.InputUpdate();Input.Down.Clear();s.InputUpdate();C(temp.Health==51&&s.Progression.Profile.potions==3,"same-frame F and mobile consume once without next-frame spill");temp.Health=1;Input.Down.Add(KeyCode.F);s.InputUpdate();Input.Down.Clear();C(temp.Health==51&&s.Progression.Profile.potions==2,"live keyboard potion");temp.Health=1;s.DrinkPotion();C(temp.Health==51&&s.Progression.Profile.potions==1,"live quickbar remains legal");
s.Paused=true;temp.Health=1;MobileControls.TouchPotion();s.InputUpdate();s.DrinkPotion();s.Paused=false;s.InputUpdate();C(temp.Health==1&&s.Progression.Profile.potions==1,"blocked input discarded and quickbar guarded");
MobileControls.TouchPotion();C(s.RestartPractice(),"restart");temp=s.Player;temp.Health=1;s.DrinkPotion();C(temp.Health==1&&s.Progression.Profile.potions==5,"prepare quickbar denied");MobileControls.TouchPotion();s.InputUpdate();C(temp.Health==1,"prepare consumes no potion");MobileControls.TouchPotion();C(s.StartPractice(),"restart starts");s.InputUpdate();C(temp.Health==1&&s.Progression.Profile.potions==5,"start discards prepare/restart pending latch");
MobileControls.TouchPotion();Time.deltaTime=10;s.InputUpdate();C(!s.PracticeActive&&owner.Profile.potions==5&&real.Health==hp,"terminal frame input cannot reach restored real inventory or HP");C(!MobileControls.ConsumePotion(),"terminal discards mobile latch");
Time.deltaTime=.1f;MobileControls.TouchPotion();C(s.BeginPractice(CampPracticeScenario.Stationary,10),"second begin");temp=s.Player;temp.Health=1;C(s.StartPractice(),"second start");s.InputUpdate();C(temp.Health==1&&s.Progression.Profile.potions==5,"begin never inherits formal pending latch");MobileControls.TouchPotion();s.EndPractice("direct exit");C(!MobileControls.ConsumePotion(),"direct exit discards pending latch");C(s.Progression==owner&&s.Player==real&&owner.Profile.potions==5&&owner.Saves==saved&&real.Health==hp,"formal owner inventory health and save untouched");Console.WriteLine("PASS "+n+" actual practice/mobile/keyboard/potion guards and ownership assertions");}}'''
# Scenario enum is read from production, not copied for this probe.
program=program.replace('CampPracticeScenario.Stationary','CampPracticeScenario.SingleTarget') if 'SingleTarget' in (root/'Assets/Scripts/Core/CampPracticeRecord.cs').read_text() else program
with tempfile.TemporaryDirectory(prefix='practice-potion-') as d:
 p=Path(d);(p/'Fixture.cs').write_text(fixture);(p/'Actual.cs').write_text(actual);production=p/'Practice.cs';original=(root/'Assets/Scripts/Core/GameSession.Practice.cs').read_text();production.write_text(original)
 companion=p/'CompanionBond.cs';companionSource=(root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
 companionMethods=''.join(member(companionSource,k) for k in ['private sealed class BondState','private static BondState State(','public static void OnPerfectDodge(','public static float CommandOpportunityRemaining(','public static bool HasPracticeTimedState('])
 companion.write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public partial class SummonedCompanion{private static readonly Dictionary<PlayerController,BondState> bonds=new Dictionary<PlayerController,BondState>();private static readonly List<PlayerController> staleOwners=new List<PlayerController>();'+companionMethods+'}}')
 sources=[p/'Fixture.cs',p/'Actual.cs',production,companion,root/'Assets/Scripts/Combat/CompanionRules.cs',root/'Assets/Scripts/Combat/CompanionDirective.cs']+[root/'Assets/Scripts/Core'/(f+'.cs') for f in ['ApplicationPauseState','SaveLifecycleGate','CombatImpactBatch','ScheduledTickWindow','CampPracticeRecord']]
 project=cv.write_project(p/'project',sources,program);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 def run(expected=None):
  q=subprocess.run([dotnet,'run','--project',str(project)],env=env,capture_output=True,text=True);print(q.stdout+q.stderr)
  if expected:assert q.returncode and expected in q.stdout+q.stderr
  else:q.check_returncode()
 legacy_tick="""private void TickPractice()
        {
            if(Input.GetKeyDown(KeyCode.H)){EndPractice("主动离开 · 记录提前结束");return;}
            if(PracticeRecord.Finished){EndPractice(PracticeRecord.EndReason);return;}
            if(InputBlocked||!PracticeRecord.Started)return;
            try{PracticeRecord.Advance(Time.deltaTime);if(PracticeRecord.Finished)EndPractice(PracticeRecord.EndReason);}
            catch(Exception exception){Debug.LogException(exception);EndPractice("试招异常中止");}
        }"""
 production.write_text(original.replace(member(original,'private void TickPractice()'),legacy_tick));run('live mobile input reaches actual practice potion once');print('PASS baseline pre-fix TickPractice reproduces lost mobile potion');production.write_text(original)
 run()
 if 'if(potionRequested)DrinkPotion();' in original:
  production.write_text(original.replace('if(potionRequested)DrinkPotion();',''));run('live mobile input reaches actual practice potion once');production.write_text(original)
  body=(p/'Actual.cs').read_text();(p/'Actual.cs').write_text(body.replace('if(PracticeActive&&(InputBlocked||PracticeRecord==null||!PracticeRecord.Started||PracticeRecord.Finished))return;',''));run('blocked input discarded and quickbar guarded');(p/'Actual.cs').write_text(body)
  production.write_text(original.replace('MobileControls.ConsumePotion(); // Discard pending input at practice start.',''));run('start discards prepare/restart pending latch');print('PASS compiled lost routing / quickbar guard / stale start latch negatives rejected')
