"""Exercise production wave transitions and mobile page hints with a managed host boundary."""
from pathlib import Path
import importlib.util, os, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
def member(path, signature):
    text=(root/path).read_text();start=text.index(signature);end=text.index('{',start)+1;depth=1
    while depth:
        depth+=(text[end]=='{')-(text[end]=='}');end+=1
    return text[start:end]
wave=member('Assets/Scripts/Core/GameSession.cs','private IEnumerator NextWave()')
arena=member('Assets/Scripts/Core/GameSession.Modes.cs','private void TickArenaRun()')
hint=member('Assets/Scripts/UI/GameUI.Mobile.cs','private bool OtherMobilePageReady()')
visible=member('Assets/Scripts/UI/GameUI.Mobile.cs','public bool MobileSkillVisible(int skill)')
combo=member('Assets/Scripts/Core/GameSession.Expedition.cs','public int ComboHitCount')+'\n'+member('Assets/Scripts/Core/GameSession.Expedition.cs','public void RecordComboHit(')
combo+='\n'+member('Assets/Scripts/Core/GameSession.Expedition.cs','public void RecordOutgoingDamage(')+'\n'+member('Assets/Scripts/Core/GameSession.Expedition.cs','public bool InCombat')+'\n'+member('Assets/Scripts/Core/GameSession.Expedition.cs','public void RecordCombatEngagement()')
float_scale=member('Assets/Scripts/Combat/FloatingNumber.cs','private float DamageFloatScale')
fixture=r'''
using System;using System.Collections;using System.Collections.Generic;using Emberfall;using PlayerController=Hero;
class Mathf{public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float Clamp01(float t)=>Math.Max(0,Math.Min(1,t));public static int Min(int a,int b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
class Time{public static float deltaTime=.016f,time;}
class Point{public float sqrMagnitude;}
class Transform{public Point position=new Point();}
class GameObject{public bool activeInHierarchy=true;}
class Hero{public int CombatEpoch;public float MaxHealth=100,Healing;public Transform transform=new Transform();public HashSet<int> Ready=new HashSet<int>();public void Heal(float amount){Healing+=amount;}public bool IsSkillAvailable(int i)=>Ready.Contains(i);}
class Enemy{public bool IsDead,IsPreparingAttack;public bool isActiveAndEnabled=true;public GameObject gameObject=new GameObject();public Transform transform=new Transform();public float NavigationRadius;}
class CombatFx{public static Point Flat(Point p)=>p;}
enum SoundCue{Victory}class GameAudio{public static void Play(SoundCue s){}}
enum ExpeditionModeStatus{Active,AwaitingSpawn,Won}enum ExpeditionModeFailure{SpawnBlocked}
class ExpeditionModeState{
 public ExpeditionModeStatus Status=ExpeditionModeStatus.AwaitingSpawn;public bool IsTerminal;public int PendingSpawnCount,AliveEnemies;
 public static bool ContestsHoldPoint(float p,float r)=>false;public static bool InsideHoldPoint(float p)=>true;
 public void Advance(float t,bool input,bool inside,int pressure,bool alive){}public void Fail(ExpeditionModeFailure f){}
}
class Hazards{public void Advance(float t){}}
class Session{
 PlayerController combatStateOwner;int combatStateEpoch=-1;float combatStateUntil=-1;
 int comboHits,comboEpoch;public int runMaximumCombo;public double runDamageTotal;public float runMaximumDamage;public bool CombatEnded;float comboLastHit=-100;PlayerController comboOwner;public bool HasStarted=true;
 COMBO
 public Hero Player=new Hero();public bool ChallengeRun,InDungeon=true,IsDead,InputBlocked,changingZone,DungeonCleared;
 public int DungeonWave=1,TotalWaves=3,HealingCharges,Spawns,Settlements,Begins;public object waveRoutine;public string LastRunSummary;
 public ExpeditionModeState ModeRun;public List<Enemy> Enemies=new List<Enemy>();bool arenaAwaitingBlessing;Hazards arenaHazards;float arenaSpawnDelay,arenaSpawnBlocked;
 void SpawnDungeonWave(){Spawns++;}void Notify(string s){}void UpdateTimeScale(){}void QueueDungeonCompletion(){Settlements++;}bool TrySettleDungeonReward()=>true;string BuildRunSummary(bool b)=>"";
 void BeginArenaPhase(){Begins++;ModeRun.Status=ExpeditionModeStatus.Active;}void SpawnArenaEnemies(){}void FinalizeArenaResult(){}
 WAVE
 ARENA
 public IEnumerator Wave()=>NextWave();public void Tick()=>TickArenaRun();
}
class UI{
 public Session session=new Session();public int mobileSkillPage;public int[] bindings=MobileSkillPolicy.DefaultBindings();
 int BoundMobileSkill(int button,int page)=>bindings[MobileSkillPolicy.BindingIndex(button,page)];
 VISIBLE
 HINT
 public bool Hint()=>OtherMobilePageReady();
}
class FloatCaption{public float life,Duration=1.5f;FLOAT_SCALE public float Scale=>DamageFloatScale;}
class Program{
 static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
 static void Main(){
 var combo=new Session();Time.time=0;for(int hit=0;hit<100;hit++)combo.RecordComboHit(1);Check(combo.ComboHitCount==100,"100 real hits display 100");combo.RecordComboHit(0);combo.RecordComboHit(float.NaN);Check(combo.ComboHitCount==100,"invalid damage never increments");Time.time=3.1f;Check(combo.ComboHitCount==0,"inactivity resets streak");combo.RecordComboHit(1);Check(combo.ComboHitCount==1,"new streak starts at one");combo.Player.CombatEpoch++;Check(combo.ComboHitCount==0,"world transition resets streak");combo.RecordComboHit(1);combo.IsDead=true;Check(combo.ComboHitCount==0,"death hides streak");
 Check(combo.runMaximumCombo==100,"peak survives combo timeout and death");
 var damage=new Session();damage.RecordOutgoingDamage(10);damage.RecordOutgoingDamage(30);damage.RecordOutgoingDamage(5);damage.RecordOutgoingDamage(float.NaN);damage.RecordOutgoingDamage(float.PositiveInfinity);damage.RecordOutgoingDamage(-1);Check(damage.runDamageTotal==45&&damage.runMaximumDamage==30,"actual losses aggregate, invalid values ignored");damage.CombatEnded=true;damage.RecordOutgoingDamage(100);Check(damage.runDamageTotal==45,"settled totals frozen");damage.CombatEnded=false;damage.InDungeon=false;damage.RecordOutgoingDamage(100);Check(damage.runDamageTotal==45,"camp damage excluded");
 var combat=new Session();var passive=new Enemy();combat.Enemies.Add(passive);Time.time=20;Check(!combat.InCombat,"idle nearby enemy never blocks operations");passive.IsPreparingAttack=true;Check(combat.InCombat,"actual attack preparation enters combat");passive.IsPreparingAttack=false;combat.RecordCombatEngagement();Time.time=23.99f;Check(combat.InCombat,"recent hit retains combat grace");Time.time=24;Check(!combat.InCombat,"four seconds without combat releases operations");combat.RecordCombatEngagement();combat.Player.CombatEpoch++;Check(!combat.InCombat,"world transition clears old combat state");combat.RecordCombatEngagement();combat.CombatEnded=true;Check(!combat.InCombat,"completed dungeon exits immediately");
 var caption=new FloatCaption();float last=caption.Scale;Check(last>1,"new damage starts enlarged");for(int i=1;i<=15;i++){caption.life=i*.1f;Check(caption.Scale<last,"older damage is strictly smaller");last=caption.Scale;}Check(Math.Abs(last-.55f)<.001f,"old damage shrinks before retirement");
 var s=new Session();var w=s.Wave();Check(w.MoveNext()&&s.Spawns==0,"finishing damage frame must complete first");s.InputBlocked=true;Check(w.MoveNext()&&s.Spawns==0,"pause blocks wave transition");s.InputBlocked=false;Check(!w.MoveNext()&&s.Spawns==1&&s.DungeonWave==2&&s.Player.Healing==25,"auto advance with one heal");
 w=s.Wave();w.MoveNext();Check(!w.MoveNext()&&s.DungeonWave==3&&s.Spawns==2,"boss wave auto advances");w=s.Wave();Check(!w.MoveNext()&&s.DungeonCleared&&s.Settlements==1&&s.Spawns==2,"final wave settles instead of respawning");
 foreach(int reason in new[]{0,1,2,3,4}){s=new Session();w=s.Wave();w.MoveNext();if(reason==0)s.IsDead=true;if(reason==1)s.InDungeon=false;if(reason==2)s.Player.CombatEpoch++;if(reason==3)s.Player=new Hero();if(reason==4)s.changingZone=true;Check(!w.MoveNext()&&s.Spawns==0,"stale or dead run never spawns "+reason);}
 for(int mode=0;mode<3;mode++){s=new Session{ModeRun=new ExpeditionModeState()};s.Tick();Check(s.Begins==1&&s.Player.Healing==20,"arena automatically starts next phase with heal");s.Tick();Check(s.Begins==1&&s.Player.Healing==20,"no duplicate phase or heal");s.ModeRun.Status=ExpeditionModeStatus.AwaitingSpawn;s.Tick();Check(s.Begins==2&&s.Player.Healing==40,"second transition remains automatic");s.ModeRun.Status=ExpeditionModeStatus.Won;s.Tick();Check(s.Begins==2,"terminal run does not restart");}
 var ui=new UI();Check(!ui.Hint(),"all unavailable hides badge");ui.session.Player.Ready.Add(9);Check(!ui.Hint(),"shared ultimate must not light badge");ui.session.Player.Ready.Add(0);Check(!ui.Hint(),"current-page ready skill must not light badge");ui.session.Player.Ready.Add(5);Check(ui.Hint(),"hidden ready skill lights badge");ui.session.Player.Ready.Remove(0);ui.mobileSkillPage=1;Check(!ui.Hint(),"switch recomputes hidden page");ui.session.Player.Ready.Add(2);Check(ui.Hint(),"reverse page works");ui.bindings=MobileSkillPolicy.DefaultBindings(true);ui.mobileSkillPage=0;ui.session.Player.Ready.Clear();ui.session.Player.Ready.Add(2);Check(ui.Hint(),"summoner bindings respected");ui.session.Player=null;Check(!ui.Hint(),"no player hides badge");
 foreach(var size in new[]{new[]{2048f,1536f,264f},new[]{2388f,1668f,264f},new[]{2732f,2048f,264f},new[]{2266f,1488f,326f}}){var l=new MobileControlLayout(size[0],size[1],size[2],0,true);Check(Math.Abs(l.Attack.X-(l.Width-223.9f))<.02f,"iPad attack shifts right 12 units");Check(l.Dodge.X-l.Attack.X-l.Attack.Width>=30,"right controls retain gap");Check(l.Attack.X>0&&l.Attack.Y>0&&l.Attack.X+l.Attack.Width<l.Width&&l.Attack.Y+l.Attack.Height<l.Height,"attack stays inside safe area");foreach(var skill in l.Skills)Check(!l.Attack.Overlaps(skill),"attack does not overlap skill arc");}
 Console.WriteLine("PASS "+n+" production transition/page-hint assertions (managed host, not Unity execution)");
 }
}
'''.replace('WAVE',wave).replace('ARENA',arena).replace('VISIBLE',visible).replace('HINT',hint).replace('COMBO',combo).replace('FLOAT_SCALE',float_scale)
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='adventure-pacing-') as tmp:
    p=Path(tmp);project=cv.write_project(p/'project',[root/'Assets/Scripts/Combat/MobileSkillPolicy.cs',root/'Assets/Scripts/UI/MobileControlLayout.cs'],fixture)
    env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
    sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
    subprocess.run([sdk,'run','--project',str(project)],env=env,check=True)
