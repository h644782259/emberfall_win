import pathlib,json,hashlib,subprocess,sys,datetime
root=pathlib.Path(sys.argv[1]);out=pathlib.Path(sys.argv[2]);manifest=json.loads((out/'input-manifest.json').read_text())
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest() if p.is_file() else None
changes=[p for p,v in manifest['trackedFiles'].items() if digest(root/p)!=v['sha256']]
current=set(subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().strip('\0').split('\0'))
refs=root/'Tools/ReferenceAssemblies';refChanges=[p for p,v in manifest['referenceFiles'].items() if digest(refs/p)!=v['sha256']]
report={'checkedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'trackedInputCount':len(manifest['trackedFiles']),'trackedChanges':changes,'trackedInventoryChanges':sorted(current.symmetric_difference(manifest['trackedFiles'])),'referenceChanges':refChanges,'sdkExecutableUnchanged':digest(pathlib.Path(manifest['sdk']))==manifest['sdkExecutableSha256']}
report['sourceHeadUnchanged']=report['sourceHead']==manifest['sourceHead']
report['referenceInventoryChanges']=sorted(set(str(p.relative_to(refs)) for p in refs.rglob('*') if p.is_file()).symmetric_difference(manifest['referenceFiles']))
report['passed']=not any([changes,report['trackedInventoryChanges'],refChanges,report['referenceInventoryChanges']]) and report['sdkExecutableUnchanged'] and report['sourceHeadUnchanged']
(out/'input-stability.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
