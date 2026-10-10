"""Execute the real chest drawing/cache methods with a narrow GUI/texture shell."""
from pathlib import Path
import tempfile,subprocess,os,sys
r=Path(__file__).resolve().parents[1]
def member(name):
 s=(r/'Assets/Scripts/UI/GameUI.Rewards.cs').read_text();a=s.index(name);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
shell=r'''
using System;using UnityEngine;
namespace UnityEngine {
public struct Vector2{public float x,y;} public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public Vector2 center=>new Vector2{x=x+width/2,y=y+height/2};} public enum EventType{Layout,Repaint} public class Event{public static Event current=new Event();public EventType type;}
public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float alpha){this.r=r;this.g=g;this.b=b;a=alpha;}}
public struct Color32{public byte r,g,b,a;public Color32(byte x,byte y,byte z,byte w){r=x;g=y;b=z;a=w;}}
public enum TextureFormat{RGBA32}public enum HideFlags{HideAndDontSave}public enum FilterMode{Bilinear}public enum TextureWrapMode{Clamp}public enum ScaleMode{ScaleToFit}
public sealed class Texture2D{public static int Allocations,Uploads,Destroyed;public HideFlags hideFlags;public FilterMode filterMode;public TextureWrapMode wrapMode;public Color32 Pixel;public Texture2D(int w,int h,TextureFormat f,bool m){Allocations++;}public void SetPixels32(Color32[] p){Pixel=p[0];Uploads++;}public void Apply(bool m,bool r){}}
public static class Resources{public static Texture2D Asset;public static T Load<T>(string key)where T:class=>Asset as T;}
public static class GUI{public static Color color=new Color(1,1,1,1);public static int Draws;public static Texture2D Last;public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv,bool alpha){if(r.width!=r.height)throw new Exception("chest must stay square");Draws++;Last=t;}public static void DrawTexture(Rect r,Texture2D t,ScaleMode s,bool a){Draws++;Last=t;}}
public static class Mathf{public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(b,n));public static float Clamp01(float n)=>Math.Max(0,Math.Min(1,n));public static int RoundToInt(float n)=>(int)Math.Round(n);public static float Min(float a,float b)=>Math.Min(a,b);public static float SmoothStep(float a,float b,float t)=>a+(b-a)*t*t*(3-2*t);public static int Min(int a,int b)=>Math.Min(a,b);}
}
namespace Emberfall{public partial class GameUI{
private readonly uint[][] rewardChestFrames=new uint[13][];private Texture2D rewardChestClosed,rewardChestComposite;private Color32[] chestCompositePixels;private int chestCompositeKey=-1;static int bakes;private Texture2D rewardChestHD;private bool rewardChestHDLoaded;
static void Destroy(Texture2D t){Texture2D.Destroyed++;}static uint[] BakeRewardChest(float p){bakes++;var data=new uint[256*256];for(int i=0;i<data.Length;i++)data[i]=p<.5f?0xff0000ffu:0xffff0000u;return data;}
ATLAS
DRAW
RELEASE
public static void Verify(){var ui=new GameUI();Event.current.type=EventType.Layout;ui.DrawRewardChest(new Rect(),false,1,0);if(Texture2D.Allocations!=0||GUI.Draws!=0)throw new Exception("layout must not allocate or draw");
Event.current.type=EventType.Repaint;
for(int frame=0;frame<=96;frame++){int previous=GUI.Draws;ui.DrawRewardChest(new Rect(),true,1,.12f+.64f*frame/96);if(GUI.Draws!=previous+1||GUI.Last.Pixel.a!=255)throw new Exception("one opaque composite draw per sample");int uploads=Texture2D.Uploads;ui.DrawRewardChest(new Rect(),true,1,.12f+.64f*frame/96);if(Texture2D.Uploads!=uploads)throw new Exception("unchanged sample reuses upload");}
if(Texture2D.Allocations!=2||bakes!=13)throw new Exception("exactly two GPU outputs and thirteen bounded source frames");ui.ReleaseChestTextures();if(Texture2D.Destroyed!=2||ui.chestCompositePixels!=null||ui.chestCompositeKey!=-1)throw new Exception("release owns outputs and managed buffers");foreach(var f in ui.rewardChestFrames)if(f!=null)throw new Exception("source frames released");
if((ChestCompositeRules.Blend(0xff0000ff,0xffff0000,4)>>24)!=255)throw new Exception("opaque crossfade never leaks background");
Resources.Asset=new Texture2D(2048,1024,TextureFormat.RGBA32,false);var hd=new GameUI();int oldBakes=bakes,oldUploads=Texture2D.Uploads;hd.DrawRewardChest(new Rect(0,0,240,160),false,1,0);hd.DrawRewardChest(new Rect(0,0,160,240),true,1,1);if(GUI.Last!=Resources.Asset||bakes!=oldBakes||Texture2D.Uploads!=oldUploads)throw new Exception("HD atlas replaces baking without runtime uploads");
Console.WriteLine("PASS: 97 real GUI drawing/cache samples, exact ownership and opaque-alpha oracle");}
}}
class Program{static void Main(){Emberfall.GameUI.Verify();}}
'''.replace('ATLAS',member('private void DrawChestAtlasFrame(')).replace('DRAW',member('private void DrawRewardChest(')).replace('RELEASE',member('private void ReleaseChestTextures()'))
with tempfile.TemporaryDirectory(prefix='chest-composite-') as t:
 p=Path(t);(p/'Program.cs').write_text(shell);rules=(r/'Assets/Scripts/UI/ChestCompositeRules.cs').read_text();(p/'Rules.cs').write_text(rules)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet';cmd=[dotnet,'run','--project',str(p/'Test.csproj')]
 subprocess.run(cmd,env=env,check=True)
 # Recreate separate source-over weighted draws: opaque midpoint alpha becomes .75.
 (p/'Rules.cs').write_text(rules.replace('(uint)((weighted+Steps/2)/Steps)<<24','(uint)(aa*left/Steps*(255-ba*step/Steps)/255+ba*step/Steps)<<24'))
 result=subprocess.run(cmd,env=env,capture_output=True,text=True)
 assert result.returncode and 'one opaque composite draw per sample' in result.stdout+result.stderr,'old double-alpha regression must fail exact drawing oracle'
 print('PASS: old double-alpha composition negative control fails the opacity oracle')
