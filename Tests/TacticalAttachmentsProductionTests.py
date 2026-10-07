#!/usr/bin/env python3
"""Real TacticalEnemyVisual -> attachment builder -> strict decoder -> committed buffers.
Engine objects are managed substitutes, never Unity rendering acceptance.
"""
import os,tempfile
from pathlib import Path
os.environ.setdefault('DOTNET_CLI_HOME',str(Path(tempfile.gettempdir())/'emberfall-e05-dotnet'))
root=Path(__file__).resolve().parents[1]
source=(root/'Tests/TacticalLiveVisualTests.py').read_text()
start=source.index(" (path/'AttachmentFallback.cs').write_text")
end=source.index(" (path/'Fixture.cs').write_text",start)
source=source[:start]+'''\n for name in ['TacticalAttachmentArt','AuthoredActorMeshes']:(path/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
'''+source[end:]
marker=" (path/'Unity.cs').write_text"
extra=r'''
 unity=unity.replace('public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();','public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>GameObject.All.Where(o=>Under(o.transform,transform)).Select(o=>o.GetComponent<T>()).Where(c=>c!=null).ToArray();static bool Under(Transform t,Transform p){while(t!=null){if(t==p)return true;t=t.parent;}return false;}')
 unity=unity.replace('new T{gameObject=this}', 'new T{gameObject=this,name=name}')
 unity=unity.replace('public Vector3[] vertices;', 'public Vector3[] normals;public Bounds bounds;public Vector3[] vertices;')
 unity=unity.replace('public void RecalculateBounds(){}','public void RecalculateBounds(){bounds=new Bounds{center=new Vector3((vertices.Max(v=>v.x)+vertices.Min(v=>v.x))*.5f,(vertices.Max(v=>v.y)+vertices.Min(v=>v.y))*.5f,(vertices.Max(v=>v.z)+vertices.Min(v=>v.z))*.5f),size=new Vector3(vertices.Max(v=>v.x)-vertices.Min(v=>v.x),vertices.Max(v=>v.y)-vertices.Min(v=>v.y),vertices.Max(v=>v.z)-vertices.Min(v=>v.z))};}')
 unity=unity.replace('public enum PrimitiveType{Sphere}', 'public struct Bounds{public Vector3 size,center;}public enum PrimitiveType{Sphere,Cube,Capsule,Cylinder}')
 unity=unity.replace('public sealed class Shader:Object{', 'public sealed class Shader:Object{public bool isSupported=true;')
 unity=unity.replace('public static class Resources{public static T Load<T>(string name)where T:new()=>new T();}', 'public class TextAsset{public byte[] bytes;}public static class Resources{public static string Root;public static T Load<T>(string name)where T:new(){var p=System.IO.Path.Combine(Root,name+".bytes");return System.IO.File.Exists(p)?(T)(object)new TextAsset{bytes=System.IO.File.ReadAllBytes(p)}:default(T);}}')
'''
source=source.replace(marker,extra+marker)
old=" (path/'Program.cs').write_text('System.Console.WriteLine(TacticalLiveVisualTests.Run());')"
source=source.replace(old," (path/'Program.cs').write_text((root/'Tests/TacticalAttachmentsProductionTests.cs').read_text().replace('RESOURCE_ROOT',(root/'Assets/Resources').as_posix()))" )
# Adapter-specific compiled mutants must fail against the actual loaded chain.
source=source.replace(" visual=path/'TacticalEnemyVisual.cs';", """
 adapter=path/'TacticalAttachmentArt.cs';good=adapter.read_text()
 for old,new,expected in [('part.enabled=Enabled&&active;', 'part.enabled=Enabled;', 'all actual state removals immediate'), ('if(parts[i]!=null)parts[i].enabled=false;', 'if(parts[i]!=null){}', 'same epoch replacement owner hides old fittings')]:
  changed=good.replace(old,new);assert changed!=good;adapter.write_text(changed)
  result=subprocess.run(command,capture_output=True,text=True)
  assert result.returncode and expected in result.stdout+result.stderr,result.stdout+result.stderr
 adapter.write_text(good)
 print('PASS E05 compiled stale-state and failed-owner-hide mutants rejected')
 visual=path/'TacticalEnemyVisual.cs';""")
# Run primary + legacy controls under explicit disabled adapter; extra suite enables actual resources.
exec(compile(source,str(__file__),'exec'))
