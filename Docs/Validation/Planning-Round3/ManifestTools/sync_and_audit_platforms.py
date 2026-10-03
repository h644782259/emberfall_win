"""Copy only committed shared changes; independently retain every platform override."""
from pathlib import Path
import subprocess,hashlib,json,sys,re
win,ios,android,out=map(Path,sys.argv[1:5]);out.mkdir(parents=True,exist_ok=True)
base='fce5efc4361614aed3eca070ed2574b7d7204a8e';ibase='fd3fe241fd65d3d6ca1a577bef296412094c5af9'
def git(root,*args):return subprocess.check_output(['git',*args],cwd=root)
def names(root,*args):return [p for p in git(root,*args).decode().split('\0') if p]
def sha(data):return hashlib.sha256(data).hexdigest()
def blob(root,rev,path):return git(root,'show',rev+':'+path)
def private(root):return {str(p.relative_to(root)):sha(p.read_bytes()) for name in ('ProjectSettings','Packages','Assets/Editor') for p in (root/name).rglob('*') if p.is_file()}
assert not (android/'.git').exists()
changes=names(win,'diff','--name-only','-z',base,'HEAD')
assert not any(p.startswith(('ProjectSettings/','Packages/','Assets/Editor/')) for p in changes)
before=private(android);old_android={}
paths=names(win,'ls-files','-z','Assets/Scripts','Assets/Resources','Tests','ArtSource','Tools')
for rel in paths:
 p=android/rel
 if p.is_file():old_android[rel]=sha(p.read_bytes())
current=set(names(win,'ls-files','-z'));copied={}
for rel in changes:
 dest=android/rel
 if rel not in current:
  if dest.is_file():dest.unlink()
  copied[rel]=None;continue
 data=blob(win,'HEAD',rel);assert data==blob(ios,'HEAD',rel),rel
 dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data);copied[rel]=sha(data)
assert before==private(android)
scopes={};differences={}
for rel in paths:
 data=blob(win,'HEAD',rel)
 for label,root in (('ios',ios),('android',android)):
  peer=blob(root,'HEAD',rel) if label=='ios' else (root/rel).read_bytes()
  if peer==data:continue
  prior=sha(blob(ios,ibase,rel)) if label=='ios' else old_android.get(rel)
  assert sha(peer)==prior,(label,rel,'changed platform override')
  assert rel not in changes,(label,rel,'changed shared file differs')
  differences[label+':'+rel]={'sha256':sha(peer),'baselineSha256':prior,'unchanged':True}
metas=names(win,'ls-files','-z','*.meta');old=set(names(win,'ls-tree','-r','--name-only','-z',base));new={};seen={}
for rel in metas:
 content=blob(win,'HEAD',rel).decode();m=re.search(r'^guid: (.+)$',content,re.M);guid=m.group(1) if m else None
 if guid:assert guid not in seen,(rel,seen.get(guid));seen[guid]=rel
 if rel in old:
  previous=re.search(r'^guid: (.+)$',blob(win,base,rel).decode(),re.M)
  assert guid==(previous.group(1) if previous else None),rel
 else:new[rel]=guid
for rel in current:
 if rel.startswith('Assets/Scripts/') and rel.endswith('.cs'):assert rel+'.meta' in current,rel
oldjson=[p for p in changes if p.endswith('.json') and p in old];assert not oldjson,oldjson
report={'windowsSourceHead':git(win,'rev-parse','HEAD').decode().strip(),'iosSourceHead':git(ios,'rev-parse','HEAD').decode().strip(),'androidPath':str(android),'androidPlatformBase':'8654c803eb29b87dea7d69f09678ec21e1955f6d','method':'Committed shared blobs only; no Android repository/login/archive. Existing platform files preserved.','changedSharedFiles':copied,'preservedAndroidPlatformFiles':before,'platformFilesUnchanged':True,'completeSharedScopeFileCount':len(paths),'unchangedPlatformOverrides':differences,'guidCount':len(metas),'newMetas':new,'existingGuidChanges':[],'historicalTrackedJsonChanges':oldjson}
(out/'platform-sync-and-guid-audit.json').write_text(json.dumps(report,indent=2)+'\n');print('PASS',len(changes),'changed files;',len(paths),'complete shared files;',len(metas),'stable metadata;',len(differences),'unchanged platform overrides')
