using System;
using Emberfall;
namespace UnityEngine
{
 public class Object{public static int Destroyed;public static void Destroy(Object value){Destroyed++;}}
 public enum TextureFormat{RGBA32}public enum FilterMode{Bilinear}public enum TextureWrapMode{Clamp}public enum HideFlags{HideAndDontSave}
 public class Texture2D:Object{public readonly int width,height;public string name;public FilterMode filterMode;public TextureWrapMode wrapMode;public HideFlags hideFlags;public Color[] CapturedPixels;public bool ReleasedCpu;
 public Texture2D(int w,int h,TextureFormat f,bool m){width=w;height=h;}public void SetPixels(Color[] p){CapturedPixels=p;}public void Apply(bool m,bool release){ReleasedCpu=release;}}
 public struct Color{public float r,g,b,a;public Color(float x,float y,float z,float alpha=1){r=x;g=y;b=z;a=alpha;}public static Color clear=>new Color(0,0,0,0);public static Color white=>new Color(1,1,1);public static Color operator+(Color a,Color b)=>new Color(a.r+b.r,a.g+b.g,a.b+b.b,a.a+b.a);public static Color operator/(Color a,float v)=>new Color(a.r/v,a.g/v,a.b/v,a.a/v);}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public float sqrMagnitude=>x*x+y*y;public Vector2 normalized=>this/(float)Math.Sqrt(sqrMagnitude);public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static Vector2 operator*(Vector2 a,float v)=>new Vector2(a.x*v,a.y*v);public static Vector2 operator/(Vector2 a,float v)=>new Vector2(a.x/v,a.y/v);public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;public static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a-b).sqrMagnitude);}
 public static class Mathf{public const float Deg2Rad=(float)Math.PI/180;public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));public static float Max(float a,float b)=>Math.Max(a,b);public static float Cos(float a)=>(float)Math.Cos(a);public static float Sin(float a)=>(float)Math.Sin(a);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;}
}
public static class SkillIconAtlasTests
{
 public static string Run(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
  for(int hero=0;hero<4;hero++)for(int skill=0;skill<10;skill++)foreach(int size in new[]{24,32,48}){
   var icon=UIIconAtlas.Skill((HeroClass)hero,skill,size);check(icon.width==size&&icon.height==size,"exact requested icon tier");
   check(ReferenceEquals(icon,UIIconAtlas.Skill((HeroClass)hero,skill,size)),"bounded cache reuses identity");
   int visible=0;foreach(var pixel in icon.CapturedPixels){check(pixel.a>=0&&pixel.a<=1,"finite bounded alpha");if(pixel.a>.2f)visible++;}
   check(visible>size&&visible<size*size,"glyph has visible structure and transparent background");check(icon.ReleasedCpu,"finished textures release CPU pixel storage");
   if(size==48&&!GameBalance.IsPassive(skill)){
    var glyph=UIIconAtlas.SkillGlyph((HeroClass)hero,skill,size);check(!ReferenceEquals(glyph,icon)&&ReferenceEquals(glyph,UIIconAtlas.SkillGlyph((HeroClass)hero,skill,size)),"floating glyph has a separate reusable cache");
    for(int i=0;i<glyph.CapturedPixels.Length;i++){var pixel=glyph.CapturedPixels[i];check(pixel.r==1&&pixel.g==1&&pixel.b==1&&pixel.a==icon.CapturedPixels[i].a,"whole glyph is bright white with identical transparent silhouette");}
   }
  }
  check(ReferenceEquals(UIIconAtlas.Skill(HeroClass.Vanguard,0,27),UIIconAtlas.Skill(HeroClass.Vanguard,0,32)),"arbitrary sizes use three bounded tiers");
  var signatures=new System.Collections.Generic.HashSet<uint>();
  for(int kind=0;kind<4;kind++){
   var resource=UIIconAtlas.Reward(kind);check(resource.width==64&&ReferenceEquals(resource,UIIconAtlas.Reward(kind)),"resource pictogram uses stable cache");uint hash=2166136261;int visible=0;
   foreach(var pixel in resource.CapturedPixels){hash=unchecked((hash^(uint)(pixel.a*255))*16777619);if(pixel.a>.2f)visible++;}
   check(visible>64&&visible<4096&&resource.ReleasedCpu,"resource glyph has transparent structure and releases pixels");signatures.Add(hash);
  }
  check(signatures.Count==4,"gold fragment thread and experience pictograms are distinct");
  var disc=UIIconAtlas.ControlDisc();check(disc.CapturedPixels[0].a==0&&disc.CapturedPixels[24*48+24].a>.9f&&ReferenceEquals(disc,UIIconAtlas.ControlDisc()),"battle controls have a transparent circular silhouette and reusable disc");
  var ring=UIIconAtlas.ControlRing();check(ring.width==64&&ring.CapturedPixels[32*64+32].a==0&&ring.CapturedPixels[0].a==0,"continuous rim remains hollow with transparent corners");
  for(int step=0;step<72;step++){double angle=step*Math.PI/36;int x=(int)Math.Round(32+30*Math.Cos(angle)),y=(int)Math.Round(32+30*Math.Sin(angle));float strongest=0;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int sx=Math.Max(0,Math.Min(63,x+dx)),sy=Math.Max(0,Math.Min(63,y+dy));strongest=Math.Max(strongest,ring.CapturedPixels[sy*64+sx].a);}check(strongest>.2f,"rim has connected visible coverage through every angle");}
  check(ReferenceEquals(ring,UIIconAtlas.ControlRing())&&!ReferenceEquals(ring,UIIconAtlas.ControlRing(true)),"thin rim and halo use bounded separate cached textures");
  var page=UIIconAtlas.SkillPageArrow();int pageInk=0;foreach(var pixel in page.CapturedPixels)if(pixel.a>.2f){pageInk++;check(pixel.r==1&&pixel.g==1&&pixel.b==1,"page arrows are white raster graphics without font dependencies");}check(pageInk>48&&pageInk<48*48&&ReferenceEquals(page,UIIconAtlas.SkillPageArrow()),"page switch graphic is visible and cached");
  var masterySignatures=new System.Collections.Generic.HashSet<uint>();
  for(int i=0;i<4;i++){var icon=UIIconAtlas.Mastery((MasteryType)i);check(ReferenceEquals(icon,UIIconAtlas.Mastery((MasteryType)i)),"mastery glyph is cached");uint signature=2166136261;int visible=0;foreach(var pixel in icon.CapturedPixels){signature=unchecked((signature^(uint)(pixel.a*255))*16777619);if(pixel.a>.2f){visible++;check(pixel.r==1&&pixel.g==1&&pixel.b==1,"mastery is white raster silhouette");}}check(visible>64&&visible<4096,"mastery pictogram has actual visible transparent structure");masterySignatures.Add(signature);}
  check(masterySignatures.Count==4,"four mastery branches have distinct silhouettes");
  var toolSignatures=new System.Collections.Generic.HashSet<uint>();foreach(string name in new[]{"save","apply","reset","upgrade","core","lock"}){var icon=UIIconAtlas.Utility(name);check(ReferenceEquals(icon,UIIconAtlas.Utility(name)),"management icon is cached");uint signature=2166136261;foreach(var pixel in icon.CapturedPixels)signature=unchecked((signature^(uint)(pixel.a*255))*16777619);toolSignatures.Add(signature);}check(toolSignatures.Count==6,"management pictograms do not fall back to generic font/skill icon");
  var utility=UIIconAtlas.Utility("potion");check(utility.width==64,"utility cache keeps existing format");int before=UnityEngine.Object.Destroyed;UIIconAtlas.Clear();check(UnityEngine.Object.Destroyed-before==171,"all120skill variants32floating glyphs four resource glyphs disc and utility are released");
  return "PASS: "+n+" production skill icon raster/cache assertions (not rendered readability)";
 }
}
