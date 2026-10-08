"""Execute production layout/input transforms, backdrop and pixel atlas with managed GUI boundaries."""
from pathlib import Path
import os,re,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
ui=(root/'Assets/Scripts/UI/GameUI.cs').read_text();mobile=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
methods='\n'.join(member(ui,k) for k in ['private void RefreshLayout()', 'private Vector2 ScreenToUI('])
methods+='\n'+member(mobile,'private Rect TouchRect(MobileControlLayout.Area')+'\n'+member(mobile,'private int TouchFont(')
mode_source=(root/'Assets/Scripts/UI/GameUI.Modes.cs').read_text()
methods+='\n'+member(mode_source,'private sealed class EntryRewardPreview')+'\n'+member(mode_source,'private System.Collections.Generic.List<EntryRewardPreview> EntryRewardPreviews(')
controls=(root/'Assets/Scripts/UI/MobileControls.cs').read_text()
actual_layout=member(controls,'public static MobileControlLayout Layout')
types=(root/'Assets/Scripts/Core/GameTypes.cs').read_text()
data='using UnityEngine;namespace Emberfall {'+member((root/'Assets/Scripts/Core/ChapterProgression.cs').read_text(),'public enum ChapterNode')+'\n'.join(re.findall(r'public enum (?:HeroClass|ItemSlot|Rarity|FashionSlot|MasteryType|EquipmentMechanic|ChapterNode)\s*\{[^}]*\}',types))+member(types,'public class ItemData')+member(types,'public class FashionData')+'public static class GameBalance{public const int SkillCount=10;'+member(types,'public static Color RarityColor(')+member(types,'public static string SlotName(')+member(types,'public static string RarityName(')+'} public static class BuildCatalog{'+''.join(member(types,k) for k in ['public static EquipmentMechanic[] MechanicsFor(','public static ItemSlot MechanicSlot(','public static string MechanicName(','public static string MechanicDescription('])+'} public static class ProgressionService{public const int MaximumLevel=100,MaximumUpgrade=100;static int Clamp(int x,int a,int b)=>System.Math.Max(a,System.Math.Min(b,x));'+''.join(member((root/'Assets/Scripts/Core/ProgressionService.cs').read_text(),k) for k in ['public static int EquipmentGenerationLevel(','public static string FashionName(FashionSlot slot,Rarity rarity,HeroClass hero)'])+'} }'
boundary=(root/'Tests/SkillIconAtlasTests.cs').read_text().split('public static class SkillIconAtlasTests')[0]
boundary=boundary.replace('public static Color clear','public static Color black=>new Color(0,0,0);public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);public static Color operator*(Color a,float t)=>new Color(a.r*t,a.g*t,a.b*t,a.a*t);public static Color clear')
boundary=boundary.replace('public static class Mathf{','public static class Mathf{public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Abs(float v)=>Math.Abs(v);public const float PI=(float)Math.PI;public static float Exp(float v)=>(float)Math.Exp(v);public static float Repeat(float v,float length)=>v-(float)Math.Floor(v/length)*length;public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int RoundToInt(float v)=>(int)Math.Round(v);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));')
boundary+=r'''
namespace UnityEngine{
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static bool operator!=(Vector3 a,Vector3 b)=>a.x!=b.x||a.y!=b.y||a.z!=b.z;public static bool operator==(Vector3 a,Vector3 b)=>!(a!=b);}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);}
 public struct Matrix4x4{public int marker;public static Matrix4x4 identity=>new Matrix4x4{marker=1};}
 public enum EventType{Layout,Repaint}public class Event{public static Event current=new Event();public EventType type=EventType.Repaint;}
 public static class Screen{public static float width,height,dpi;}
 public static class Time{public static float unscaledDeltaTime=.05f;}
 public static class GUI{public static Matrix4x4 matrix;public static Color color=Color.white;public static System.Collections.Generic.List<Rect> Draws=new System.Collections.Generic.List<Rect>();public static void DrawTexture(Rect r,Texture2D t){Draws.Add(r);}}
}
namespace Emberfall{
 public static class EffectPreferences{public static int TouchPosition;public static float InterfaceTextScale=1;public static bool ReducedEffects;}
 public static class MobileSkillPolicy{public const int ButtonCount=5;}
 public static class MobileControls{
 public static bool Active=true,IPad=true;public static Rect SafeArea;public static bool IsIPad=>IPad;
 private static MobileControlLayout cachedLayout;private static Vector3 cachedLayoutInputs;private static int cachedPosition;private static bool cachedIPad;
 ACTUAL_LAYOUT
 }
 public class SessionBoundary{public bool HasStarted,BackgroundPaused;public ChapterNode SelectedChapterNode;public int SelectedChapterTier=30;public ProgressBoundary Progression=new ProgressBoundary();}public class ProgressBoundary{public ProfileBoundary Profile=new ProfileBoundary();}public class ProfileBoundary{public int level=30;public HeroClass heroClass;}
 public sealed partial class GameUI{
 float scale=1,width,height;Vector2 guiOffset;Rect hotbarBounds;Rect[] hotbarSlots=new Rect[10];SessionBoundary session=new SessionBoundary();Color jade,gold;float TouchRatio=>MobileControls.Layout.Scale/scale;
 void ObserveTouchViewport(Rect r){}static void Destroy(Texture2D t){UnityEngine.Object.Destroy(t);}static void Fill(Rect r,Color c){GUI.Draws.Add(r);}
 METHODS
 static int checks;static void C(bool yes,string why){checks++;if(!yes)throw new Exception(why);}static bool Near(float a,float b)=>Math.Abs(a-b)<.015;
 static void Inside(MobilePanelLayout.Area r,float w,float h,string name){C(r.X>=0&&r.Y>=0&&r.XMax<=w+.02f&&r.YMax<=h+.02f,name);}
 public static void Verify(){
 foreach(var device in new[]{(2266f,1488f,326f),(2048f,1536f,264f),(2360f,1640f,264f),(2388f,1668f,264f),(2732f,2048f,264f),(2752f,2064f,264f)})
 foreach(bool portrait in new[]{false,true})foreach(int position in new[]{-1,0,1})foreach(float inset in new[]{0,24,48}){
  Screen.width=portrait?device.Item2:device.Item1;Screen.height=portrait?device.Item1:device.Item2;Screen.dpi=device.Item3;
  MobileControls.SafeArea=new Rect(inset,inset,Screen.width-2*inset,Screen.height-2*inset);EffectPreferences.TouchPosition=position;
  var u=new GameUI();MobileControls.IPad=false;u.RefreshLayout();float oldScale=u.scale,oldTouch=MobileControls.Layout.Scale;var before=u.TouchRect(MobileControls.Layout.Inventory);int oldFont=u.TouchFont(14);
  MobileControls.IPad=true;u.RefreshLayout();var l=MobileControls.Layout;float expected=Math.Max(1,Math.Min(1.4f,Math.Min(MobileControls.SafeArea.width/(oldTouch*568),MobileControls.SafeArea.height/(oldTouch*320))));C(Near(l.UiZoom,expected),"iPad uses 1.4 factor with minimum-viewport clamp");C(Near(u.scale,oldScale*l.UiZoom)&&Near(l.Scale,oldTouch*l.UiZoom),"both transforms apply factor once");
  var icon=u.TouchRect(l.Inventory);C(Near(icon.width*u.scale,before.width*oldScale*l.UiZoom),"drawn targets grow 40 percent");C(u.TouchFont(14)==oldFont,"font units do not double-scale");
  foreach(var area in new[]{l.Attack,l.Joystick,l.Menu,l.FocusCommand,l.RecallCommand,l.Skills[4],l.SkillPage}){var r=u.TouchRect(area);var pixel=new Vector2(u.guiOffset.x+r.center.x*u.scale,Screen.height-u.guiOffset.y-r.center.y*u.scale);var hit=u.ScreenToUI(pixel);C(Near(hit.x,r.center.x)&&Near(hit.y,r.center.y),"actual input inverse matches rendered centre");}
  C(!l.FocusCommand.Overlaps(l.RecallCommand)&&!l.RecallCommand.Overlaps(l.Skills[4])&&!l.SkillPage.Overlaps(l.Skills[4]),"orders and page and ultimate keep independent hitboxes");
  var p=new MobilePanelLayout(l.Width,l.Height);Inside(p.Body,l.Width,l.Height,"shared body uses available viewport");Inside(p.Footer,l.Width,l.Height,"fixed actions inside safe area");
  var withoutFooter=new MobilePanelLayout(l.Width,l.Height,false);C(withoutFooter.Body.Height==p.Body.Height+52,"mandatory unopened chest reclaims obsolete return footer space");
  var a=new AdventureSelectionLayout(l.Width,l.Height);Inside(a.Frame,l.Width,l.Height,"six entries and fixed entry footer stay safe");C(Near(a.List.Width/(a.List.Width+a.Details.Width),.3f),"list/detail stays 30/70");C(!a.List.Overlaps(a.Details)&&!a.Details.Overlaps(a.Footer),"scroll areas clear entry button");
  var shop=new MerchantServiceLayout(l.Width,l.Height,true);Inside(shop.Body,l.Width,l.Height,"merchant responsive body");C(shop.Columns>=3,"merchant content uses responsive columns");
  var smith=new SmithServiceLayout(l.Width,l.Height,true);Inside(smith.Detail,l.Width,l.Height,"smith scrolling detail");Inside(smith.Primary,l.Width,l.Height,"smith fixed primary");C(smith.Equipment(0).Height<=108,"smith equipment cards avoid stretched empty rows");
  var grid=new InventoryGridGeometry(p.Body.Width);var last=grid.Tile(grid.Columns-1);C(last.XMax<=p.Body.Width+.02f&&p.Body.Width-last.XMax<48,"inventory fills available row, less than one cell of slack");
  foreach(float font in new[]{1f,1.1f,1.2f})C(5*14*font<=a.Entry(0).Width-16,"entry heading accommodates largest preference");
 }
 foreach(bool ipad in new[]{false,true}){
  var small=new MobileControlLayout(568,320,326,0,ipad);C(small.Width>=568&&small.Height>=320,"small viewport clamps enlargement rather than clipping");
 }
 foreach(var phone in new[]{(1334f,750f,326f),(2532f,1170f,460f)}){
  var l=new MobileControlLayout(phone.Item1,phone.Item2,phone.Item3);var explicitPhone=new MobileControlLayout(phone.Item1,phone.Item2,phone.Item3,0,false);C(l.Scale==explicitPhone.Scale&&l.UiZoom==1&&l.Width==explicitPhone.Width,"phone geometry stays byte-for-byte equivalent");
 }
 // The actual renderer covers pixels beyond safe area and never advances under suspension/reduced effects.
 foreach(var size in new[]{(2048f,942f),(2048f,1536f),(1488f,2266f),(1280f,720f)}){
  Screen.width=size.Item1;Screen.height=size.Item2;var u=new GameUI();GUI.matrix=new Matrix4x4{marker=17};GUI.Draws.Clear();u.DrawTitleBackdrop();var first=GUI.Draws.ToArray();C(first[0].x==0&&first[0].y==0&&first[0].width==Screen.width&&first[0].height==Screen.height,"opaque first draw covers full viewport");C(u.titleSky.CapturedPixels.All(p=>p.a==1),"baked backdrop is completely opaque");C(GUI.matrix.marker==17&&first.Length<=34,"matrix restored and draw budget bounded");
  var texture=u.titleSky;u.ReconcileTitleBackdrop();GUI.Draws.Clear();u.DrawTitleBackdrop();C(ReferenceEquals(texture,u.titleSky)&&GUI.Draws.Last().y!=first.Last().y,"real fog/embers animate without reallocating textures");
  u.session.BackgroundPaused=true;float frozen=u.titleClock;u.ReconcileTitleBackdrop();C(u.titleClock==frozen,"background stops while app suspended");u.session.BackgroundPaused=false;EffectPreferences.ReducedEffects=true;GUI.Draws.Clear();u.DrawTitleBackdrop();var staticFrame=GUI.Draws.ToArray();u.ReconcileTitleBackdrop();GUI.Draws.Clear();u.DrawTitleBackdrop();C(u.titleClock==frozen&&GUI.Draws.Last().x==staticFrame.Last().x&&GUI.Draws.Last().y==staticFrame.Last().y,"reduced effects is stable static composition");EffectPreferences.ReducedEffects=false;
  u.session.HasStarted=true;u.ReconcileTitleBackdrop();C(u.titleSky==null&&u.titleMist==null&&u.titleClock==0,"leaving title releases all owned textures and clock");
 }
 // All roll outcomes are reachable previews, including mechanics restricted on ordinary enemies.
 for(int mode=-1;mode<=3;mode++)for(int tier=1;tier<=100;tier++){
  var actual=new System.Collections.Generic.HashSet<Rarity>();for(int roll=0;roll<100;roll++)actual.Add(AdventureRewardRules.EquipmentRarity(mode,tier,roll));C(actual.SetEquals(DropPreviewRules.ClearRarities(mode,tier)),"clear preview exactly equals all real roll outcomes");C(actual.Contains(Rarity.Legendary),"high-quality possible clear remains visible");
  int total=0;for(int slot=0;slot<3;slot++)total+=DropPreviewRules.SlotCount(mode,tier,(ItemSlot)slot);C(total==AdventureRewardRules.EquipmentCount(mode,tier),"tier equipment quantity and slot dedupe preserved");
  foreach(bool boss in new[]{false,true})foreach(bool mechanic in new[]{false,true}){
   actual.Clear();for(int roll=0;roll<100;roll++){var rarity=TierRewardRules.DropRarity(false,tier,roll);if(!mechanic||rarity>=Rarity.Epic)actual.Add(rarity);if(boss)actual.Add(TierRewardRules.DropRarity(true,tier,roll));}C(actual.SetEquals(DropPreviewRules.EnemyRarities(tier,boss,mechanic)),"enemy/mechanic preview exactly follows actual drop restrictions");
  }
 }

 foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(int tier in new[]{1,5,10,20,30,40,70,100})for(int entry=0;entry<8;entry++){
  bool chapter=entry>=5;int mode=chapter?(entry==5?0:entry==6?1:2):entry-1;var u=new GameUI();u.session.Progression.Profile.heroClass=hero;u.session.SelectedChapterNode=(ChapterNode)(chapter?entry-5:0);u.session.SelectedChapterTier=tier;
  var preview=u.EntryRewardPreviews(mode,tier,chapter);C(preview.Select(p=>p.Key).Distinct().Count()==preview.Count,"real preview deduplicates complete identity including rarity");
  for(int slot=0;slot<3;slot++)foreach(var rarity in DropPreviewRules.ClearRarities(mode,tier))C(preview.Any(p=>p.Key=="clear:"+(ItemSlot)slot+":"+rarity)==(DropPreviewRules.SlotCount(mode,tier,(ItemSlot)slot)>0),"actual UI contains each reachable clear slot/rarity and no false slot");
  bool hasBoss=chapter?u.session.SelectedChapterNode==ChapterNode.StarPlatform:mode==-1||mode==2||mode==3;
  foreach(var mechanic in BuildCatalog.MechanicsFor(hero))foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))C(preview.Any(p=>p.Key=="mechanic:"+mechanic+":"+rarity)==DropPreviewRules.EnemyRarities(tier,hasBoss,true).Contains(rarity),"actual UI mechanics show only real reachable quality");
  C(preview.Where(p=>p.Key.StartsWith("fashion:")).All(p=>p.Rarity==Rarity.Legendary),"current fashion quality remains legendary while designs stay separate");
  C(preview.Any(p=>p.Clear)&&preview.Any(p=>!p.Clear),"clear box and probabilistic enemy groups remain distinct");
 }
 var all=new System.Collections.Generic.HashSet<uint>();
 foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))for(int tier=0;tier<=10;tier++)foreach(Rarity rarity in Enum.GetValues(typeof(Rarity))){
  var texture=UIIconAtlas.EquipmentCardIcon(slot,Math.Max(1,tier*10),rarity,hero);C(all.Add(Hash(texture)),"distinct pixels: "+hero+" "+slot+" "+tier+" "+rarity);C(ReferenceEquals(texture,UIIconAtlas.EquipmentCardIcon(slot,Math.Max(1,tier*10),rarity,hero)),"identity cache reuses textures");C(texture.width==64&&texture.ReleasedCpu,"fixed raster and released CPU storage");
 }
 foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(FashionSlot slot in Enum.GetValues(typeof(FashionSlot))){var designs=new System.Collections.Generic.HashSet<uint>();for(int identity=0;identity<4;identity++){var fashion=new FashionData{id="old-"+identity,slot=slot,rarity=Rarity.Legendary,appearanceTier=identity};C(designs.Add(Hash(UIIconAtlas.FashionCardIcon(slot,(int)fashion.AppearanceRarity,hero))),"normalized legendary retains four distinct old designs");C(fashion.id=="old-"+identity&&fashion.appearanceTier==identity,"read-only presentation preserves ownership identity");}}
 C(Hash(UIIconAtlas.CompanionCommand(false))!=Hash(UIIconAtlas.CompanionCommand(true))&&Hash(UIIconAtlas.CompanionCommand(true))!=Hash(UIIconAtlas.CompanionCommand(true,true)),"focus return and resume are different pixel glyphs");
 foreach(int count in new[]{0,1,4})foreach(bool blocked in new[]{false,true})foreach(bool target in new[]{false,true})foreach(bool range in new[]{false,true})foreach(bool focused in new[]{false,true})foreach(bool recalled in new[]{false,true}){
  var state=new CompanionCommandPresentation(count,blocked,target,range,focused,recalled);C(state.FocusEnabled==(count>0&&!blocked&&target&&range),"focus enabled from live roster and legal aim");C(state.RecallEnabled==(count>0&&!blocked),"recall has no invented resource or cooldown");C(state.FocusActive==(count>0&&!blocked&&focused)&&state.RecallActive==(count>0&&!blocked&&recalled),"active marks require actual live order");
 }
 Console.WriteLine("PASS "+checks+" actual scale/input, density, backdrop lifecycle, roll reachability and atlas pixel checks; managed boundaries, no device visual claim");
 }
 static uint Hash(Texture2D t){uint h=2166136261;foreach(var p in t.CapturedPixels){h=unchecked((h^(uint)(p.a*255))*16777619);h=unchecked((h^(uint)(p.r*255))*16777619);}return h;}
 }
}
class Program{static void Main(){Emberfall.GameUI.Verify();}}
'''
boundary='using System.Linq;using UnityEngine;\n'+boundary.replace('ACTUAL_LAYOUT',actual_layout).replace('METHODS',methods)
# Integration contracts complement executable geometry rather than claiming raster/device approval.
assert 'MobileControls.Layout.UiZoom' in member(ui,'private void RefreshLayout()')
assert ui.index('if (!session.HasStarted) DrawTitleBackdrop();')<ui.index('GUI.matrix = Matrix4x4.TRS(guiOffset')
session=(root/'Assets/Scripts/Core/GameSession.cs').read_text()
assert 'WorldBuilder.Build' not in member(session,'private void Awake()')
assert 'WorldBuilder.Build' not in member(session,'public bool QuitToTitle(')
assert '"暂停 / 存档"' not in (root/'Assets/Scripts/UI/GameUI.MobileBlessings.cs').read_text()
assert '"返回"' not in member((root/'Assets/Scripts/UI/GameUI.MobileRewards.cs').read_text(),'private void DrawMobileChests()')
assert ui.index('else if (panel == Panel.Chests) DrawChests();')<ui.index('else if(session.ModeFinished)')
with tempfile.TemporaryDirectory(prefix='unified-ui-') as tmp:
 p=Path(tmp);(p/'Boundary.cs').write_text(boundary);(p/'Types.cs').write_text(data)
 for area,names in [('UI',['UIIconAtlas','SkillIconPresentation','MobileControlLayout','MobilePanelLayout','MobileTitleLayout','InventoryGridGeometry','MerchantServiceLayout','SmithServiceLayout','AdventureSelectionLayout','CompanionCommandPresentation','GameUI.TitleBackdrop']),('Core',['HudLogicalScale','EquipmentAppearance','TierRewardRules','TierRewardBand'])]:
  for name in names:(p/(name+'.cs')).write_text((root/'Assets/Scripts'/area/(name+'.cs')).read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169;0414;0660;0661</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
