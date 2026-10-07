from pathlib import Path
import json,hashlib,ast,subprocess
base=Path(__file__).resolve().parent;root=base.parents[2]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for name,value in json.loads((base/'manifest.json').read_text()).items():assert sha(base/name)==value,name
r=json.loads((base/'report.json').read_text());assert r['passed'] and len(r['checks'])==16 and all(c['exitCode']==0 for c in r['checks']) and not r['sourceChangedDuringRun']
for name,value in r['sourceSha256'].items():assert sha(root/name)==value,name
parsed=ast.parse((base/'run.py').read_text());fn=next(n for n in parsed.body if isinstance(n,ast.FunctionDef) and n.name=='inputs');scope={'root':root,'base':base,'sha':sha};exec(compile(ast.Module(body=[fn],type_ignores=[]),'<verified-inputs>','exec'),scope);assert scope['inputs']()==r['sourceSha256']
assert (base/'exit-code.txt').read_text().strip()=='0'
assert not subprocess.check_output(['git','diff',r['baseline'],'--name-only','--','Packages','ProjectSettings','Assets/Resources','Assets/Editor/GroundLootValidation.cs'],cwd=root,text=True)
print('PASS 16 frozen checks, all current source hashes and preserved platform/assets/prior Editor fix; no Unity/device claim')
