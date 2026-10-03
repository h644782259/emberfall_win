"""Actual enemy factories and actual status component against managed hierarchy/TRS; no Unity GPU claim."""
from pathlib import Path
import sys,os,tempfile,subprocess
root=Path(__file__).resolve().parents[1];sdk=sys.argv[1]
with tempfile.TemporaryDirectory(prefix='status-anchors-') as t:
 out=Path(t);ns={'__file__':str(root/'Tests/IntegratedActorArtProductionTests.py')};args=sys.argv;sys.argv=['integrated',sdk,str(out)]
 exec((root/'Tests/IntegratedActorArtProductionTests.py').read_text().split('\ncmd=',1)[0],ns);sys.argv=args;p=out/'export';extract=ns['extract']
 for name in ['Combat/CombatModel.StatusAnchors','Combat/EnemyStatusVisual','Core/EnemyStatusVisualRules']:(p/(Path(name).name+'.cs')).write_text((root/'Assets/Scripts'/(name+'.cs')).read_text())
 f=p/'Fixture.cs';s=f.read_text().replace('ShadowCastingMode{On}', 'ShadowCastingMode{On,Off}').replace('public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();','public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T GetComponentInChildren<T>()where T:Component=>GetComponentsInChildren<T>(false).FirstOrDefault();')
 s=s.replace('public Status StatusEffects;public class Status{public float AirborneHeight;}','public EnemyStatusEffects StatusEffects=new EnemyStatusEffects();public enum ThreatTier{Normal,Elite}public ThreatTier Tier;')
 s+='''namespace Emberfall{public class EnemyStatusEffects{public float AirborneHeight;public bool IsFrozen,HasFrostMark,IsMarked;internal event System.Action VisualStateChanged;public void Notify(){VisualStateChanged?.Invoke();}}public class GameSession{public static GameSession Instance=new GameSession();public PlayerController Player=new PlayerController();}public class PlayerController{public EnemyController AimTarget;}public static class EffectPreferences{public static bool ReducedEffects;}}''';f.write_text(s)
 m=p/'Motion.cs';s=m.read_text();method=extract((root/'Assets/Scripts/Combat/CombatModel.cs').read_text(),'internal static CombatModel LargeExpedition(');s=s[:-2]+method+'}}';m.write_text(s)
 (p/'Exporter.cs').write_text((root/'Tests/EnemyStatusAnchorProductionTests.cs').read_text())
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1');cmd=[sdk,'run','--project',str(p/'Export.csproj')]
 def run(expected=None):
  result=subprocess.run(cmd,env=env,capture_output=True,text=True);print(result.stdout+result.stderr)
  if expected:assert result.returncode and expected in result.stdout+result.stderr
  else:result.check_returncode()
 run()
 view=p/'EnemyStatusVisual.cs';original=view.read_text();view.write_text(original.replace('if(piece.transform.parent!=anchor)piece.transform.SetParent(anchor,false);',''))
 run('status symbol follows actual body parent');view.write_text(original)
 model=p/'CombatModel.StatusAnchors.cs';old=model.read_text();model.write_text(old.replace('anchor=largeBossRig!=null?transform.Find("Suspended astrolabe chassis"):body;','anchor=transform;'))
 run('status symbol follows actual body parent');print('PASS compiled root-parent and fake-height regressions rejected')
