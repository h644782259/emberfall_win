from pathlib import Path
import json,hashlib,ast
base=Path(__file__).resolve().parent;root=base.parents[2]
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for name,value in json.loads((base/'manifest.json').read_text()).items():assert digest(base/name)==value,name
r=json.loads((base/'report.json').read_text());assert r['passed'] and len(r['checks'])==4 and all(c['exitCode']==0 for c in r['checks']) and not r['sourceChangedDuringRun']
for name,value in r['sourceSha256'].items():assert digest(root/name)==value,name
parsed=ast.parse((base/'run.py').read_text());fn=next(n for n in parsed.body if isinstance(n,ast.FunctionDef) and n.name=='inputs');scope={'root':root,'base':base,'digest':digest};exec(compile(ast.Module(body=[fn],type_ignores=[]),'<verified-inputs>','exec'),scope);assert scope['inputs']()==r['sourceSha256']
assert (base/'exit-code.txt').read_text().strip()=='0'
print('PASS frozen source hashes and four checks; no real Unity Editor/build/device claim')
