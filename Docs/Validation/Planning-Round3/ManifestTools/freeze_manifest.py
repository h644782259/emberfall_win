import pathlib,subprocess,hashlib,json,datetime,sys
root=pathlib.Path(sys.argv[1]); out=pathlib.Path(sys.argv[2]); out.mkdir(parents=True,exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
paths=subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0')
files={p:{'sha256':sha(root/p),'size':(root/p).stat().st_size} for p in paths if p and (root/p).is_file()}
refs=root/'Tools/ReferenceAssemblies'
ref_files={str(p.relative_to(refs)):{'sha256':sha(p),'size':p.stat().st_size} for p in sorted(refs.rglob('*')) if p.is_file()}
report={'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'scope':'All Git tracked files (including every source, runner, fixture, shader, historical document and script); external pinned references recorded separately. Excludes generated ignored outputs.','trackedFiles':files,'referenceFiles':ref_files,'sdk':'/workspace/shared/emberfall-tools/dotnet/dotnet','sdkVersion':subprocess.check_output(['/workspace/shared/emberfall-tools/dotnet/dotnet','--version'],text=True).strip(),'sdkExecutableSha256':sha(pathlib.Path('/workspace/shared/emberfall-tools/dotnet/dotnet'))}
(out/'input-manifest.json').write_text(json.dumps(report,indent=2)+'\n');print(len(files),len(ref_files))
