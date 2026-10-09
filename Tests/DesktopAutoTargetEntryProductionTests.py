"""Exercise actual desktop target selection and dungeon default selection methods."""
from pathlib import Path
import importlib.util,subprocess,tempfile,os
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def method(file,name):
 s=(root/'Assets/Scripts'/file).read_text();a=s.index(name);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b].replace('private ','public ')
code=r"""using System;using System.Collections.Generic;
struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
class Transform{public Vector3 position;}class Obj{public bool activeInHierarchy=true;}
class EnemyController{public bool IsDead;public float HitFootprintBonus;public Obj gameObject=new Obj();public Transform transform=new Transform();}
enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
static class CombatFx{public static Vector3 Flat(Vector3 a)=>new Vector3(a.x,0,a.z);}
static class CombatSight{public static HashSet<float> Blocked=new HashSet<float>();public static bool Direct(Vector3 a,Vector3 b)=>!Blocked.Contains(b.x);}
class Encounter{public List<EnemyController> Enemies=new List<EnemyController>();}
class TargetProbe{public Encounter session=new Encounter();public HeroClass HeroClass;public Transform transform=new Transform();public EnemyController FocusTarget,AimTarget;
"""+method('Combat/PlayerController.cs','private static bool ValidAimTarget(')+method('Combat/PlayerController.cs','private EnemyController ResolveDesktopBasicTarget(')+r"""}
class Profile{public int level;}
class Progression{public Profile Profile=new Profile();public int[] Best=new int[5];public int UnlockedAdventureTier(int mode)=>Best[mode+1]+1;}
static class AdventureRewardRules{public static int MaximumDungeonIndex(int level)=>Math.Max(1,Math.Min(10,level/10));}
class EntryProbe{public Progression Progression=new Progression();public int[] selectedAdventureTiers=new[]{1,1,1,1,1};
"""+method('Core/GameSession.Expedition.cs','private void ResetAdventureEntryTiers(')+r"""}
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static EnemyController E(float x,float z=0)=>new EnemyController{transform=new Transform{position=new Vector3(x,0,z)}};static void Main(){
 foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){var p=new TargetProbe{HeroClass=hero};EnemyController near=E(-2),far=E(3),wall=E(1),dead=E(.5f),hidden=E(.2f);dead.IsDead=true;hidden.gameObject.activeInHierarchy=false;CombatSight.Blocked.Add(1);p.session.Enemies.AddRange(new[]{wall,dead,hidden,far,near});C(p.ResolveDesktopBasicTarget()==near,"auto lock includes nearby enemy behind hero but ignores dead hidden blocked");p.AimTarget=far;C(p.ResolveDesktopBasicTarget()==far,"mouse target retained");p.FocusTarget=near;C(p.ResolveDesktopBasicTarget()==near,"focus target wins even when mouse target enumerated earlier");p.FocusTarget=wall;p.AimTarget=dead;C(p.ResolveDesktopBasicTarget()==near,"invalid focus and aim safely fall back");p.session.Enemies.Clear();C(p.ResolveDesktopBasicTarget()==null,"empty arena has no fabricated target");p.session.Enemies.Add(E(30));C(p.ResolveDesktopBasicTarget()==null,"no out-of-range target");CombatSight.Blocked.Clear();}
 var entry=new EntryProbe();entry.Progression.Best=new[]{0,2,9,4,7};entry.Progression.Profile.level=60;entry.ResetAdventureEntryTiers();int[] expected={1,3,6,5,6};for(int i=0;i<5;i++)C(entry.selectedAdventureTiers[i]==expected[i],"default combines independent clear and character level");entry.selectedAdventureTiers[2]=1;entry.Progression.Profile.level=100;entry.ResetAdventureEntryTiers();C(entry.selectedAdventureTiers[2]==10,"reopening entry refreshes highest instead of stale lower choice");Console.WriteLine("PASS "+n+" actual targeting and entry-selection assertions");}}
"""
with tempfile.TemporaryDirectory(prefix='target-entry-') as tmp:
 p=Path(tmp);project=cv.write_project(p/'project',[],code);sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet';subprocess.run([sdk,'run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
