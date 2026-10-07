#!/usr/bin/env python3
"""Real combined actor adapters/action poses against freshly executed F6 protection geometry."""
from pathlib import Path
import sys,os,tempfile,subprocess,json,shutil
root=Path(__file__).resolve().parents[1];sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';owned=len(sys.argv)<3
out=Path(sys.argv[2]).resolve() if not owned else Path(tempfile.mkdtemp(prefix='final-envelope-'));out.mkdir(parents=True,exist_ok=True)
ns={'__file__':str(root/'Tests/IntegratedActorArtProductionTests.py')};sys.argv=['test',sdk,str(out)]
exec((root/'Tests/IntegratedActorArtProductionTests.py').read_text().split('\nsubprocess.run(cmd,env=env,check=True)',1)[0],ns)
p=out/'export';m=p/'Motion.cs';s=m.read_text();s=s[:-2]+(root/'Tests/FinalBodyEnvelopeProductionTests.Pose.cs').read_text()+'}}';m.write_text(s)
(p/'Exporter.cs').write_text((root/'Tests/FinalBodyEnvelopeProductionTests.Program.cs').read_text().replace('OUTPUT_PATH',(out/'actors.json').as_posix()))
cmd=[sdk,'run','--project',str(p/'Export.csproj')];env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1');subprocess.run(cmd,env=env,check=True)
for file,old,new,oracle in [('ActorSilhouetteF1.cs','if(!Enabled)return;','return;','actual F1 body loaded'),('CombatModel.WeaponArt.cs','if(swordRig!=null)','if(false)','actual F3 weapon loaded'),('Motion.cs','ApplyAuthoredVanguardPose(acting,t,hurt);','','actual Vanguard layer changes contact')]:
 target=p/file;original=target.read_text();assert old in original;target.write_text(original.replace(old,new,1));r=subprocess.run(cmd,env=env,capture_output=True,text=True);target.write_text(original);(out/('negative-'+file+'.log')).write_text(r.stdout+r.stderr);assert r.returncode!=0 and oracle in r.stdout+r.stderr,(oracle,r.stdout,r.stderr);print('PASS compiled layer control:',oracle)
# Execute the existing actual F6 guard/passive -> AdvancedSkillVfx -> FilledSkillVfx resource pipeline.
# Use its explicit optional output argument; default runs never touch archived evidence.
code=(root/'Tests/DefenseIdentityProductionTests.py').read_text()
# Snapshot real production transforms at birth and three later ages, never scale exported data.
sample=(root/'Tests/DefenseIdentityProductionTests.cs').read_text()
sample=sample.replace('var fx=Child(anchor);Time.deltaTime=.75f;fx.gameObject.Call("Update");','var fx=Child(anchor);float[] deltas={0,.09f,.09f,.57f};float[] ages={0,.09f,.18f,.75f};for(int snapshot=0;snapshot<4;snapshot++){Time.deltaTime=deltas[snapshot];fx.gameObject.Call("Update");')
sample=sample.replace('opacity=o.GetComponent<MeshRenderer>().Opacity','opacity=o.GetComponent<MeshRenderer>().Opacity*o.GetComponent<MeshRenderer>().TintAlpha')
sample=sample.replace('phase=label,objects','phase=label+" / age="+ages[snapshot],objects').replace('samples.Add(new {kind="ProtectionCage",phase=label+" / age="+ages[snapshot],objects});','samples.Add(new {kind="ProtectionCage",phase=label+" / age="+ages[snapshot],objects});}')
code=code.replace("(root/'Tests/DefenseIdentityProductionTests.cs').read_text()","sample")
code=code.replace("('stale-state','if(stateActive!=null&&!stateActive())','if(false)','state cancellation hides hierarchy before deferred destruction')", "('stale-state','if(stateActive!=null&&!stateActive())','if(false)','state cancellation hides hierarchy before deferred destruction'),('old-persistent-motion','protectionEnvelope&&identity==4?20:18','18','persistent protection has body envelope')")
code=code.replace("path=p/'AdvancedSkillVfx.cs'", "path=p/('FilledSkillVfx.cs' if name=='old-persistent-motion' else 'AdvancedSkillVfx.cs')")
code=code.replace("print('PASS compiled negative control',name)","(out/('negative-F6-'+name+'.log')).write_text(result.stdout+result.stderr);print('PASS compiled negative control',name)")
# Last mutation exports the actual old-height pipeline and proves that the joint geometry oracle rejects it.
code+='\n'  # generated driver runs inside the original temporary project lifetime
code+="""
 original=(p/'FilledSkillVfx.cs').read_text();assert 'height=Mathf.Max(height,2.6f)' in original
 (p/'FilledSkillVfx.cs').write_text(original.replace('height=Mathf.Max(height,2.6f)','height=Mathf.Max(height,2.2f)'))
 oldcmd=cmd[:-1]+[str(out/'old-height-protection.json')]
 subprocess.run(oldcmd,env=env,check=True)
 (p/'FilledSkillVfx.cs').write_text(original)
 r=subprocess.run([sys.executable,str(root/'ArtSource/FinalBodyEnvelope/measure.py'),(out/'actors.json').as_posix(),str(out/'old-height-protection.json'),str(out/'old-height-clearance.json')],capture_output=True,text=True)
 (out/'negative-old-height.log').write_text(r.stdout+r.stderr)
 assert r.returncode!=0 and 'actual protection triangles cross assembled body' in r.stdout+r.stderr,r.stdout+r.stderr
 print('PASS compiled old persistent height rejected by actual body/effect triangle intersections',flush=True)
"""
ns={'__file__':str(root/'Tests/DefenseIdentityProductionTests.py'),'sample':sample,'out':out};sys.argv=['defense',sdk,str(out/'protection.json')];exec(compile(code,str(root/'Tests/DefenseIdentityProductionTests.py'),'exec'),ns)
subprocess.run([sys.executable,str(root/'ArtSource/FinalBodyEnvelope/measure.py'),(out/'actors.json').as_posix(),str(out/'protection.json'),str(out/'clearance.json')],check=True)
if owned:shutil.rmtree(out)
