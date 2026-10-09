from pathlib import Path
import importlib.util,tempfile,subprocess,os
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
source=(root/'Assets/Scripts/Core/GameAudio.cs').read_text()
def member(key):
 a=source.index(key);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b].replace('private static','public static')
names=source[source.index('private static readonly string[] MusicNames='):source.index(';',source.index('private static readonly string[] MusicNames='))+1]
methods=''.join(member(k) for k in ['private static AudioClip SynthesizeBackground(','private static AudioClip SynthesizeSkill(','private static double Bell(','private static int CurrentMusicTheme('])
code=r'''using System;using System.Linq;using System.Collections.Generic;
class AudioClip {public float[] Data;public object hideFlags;public static AudioClip Create(string n,int length,int channels,int rate,bool stream){return new AudioClip();}public bool SetData(float[] data,int offset){Data=data;return true;}}
static class HideFlags{public static object DontSave;}
enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}enum ChapterNode{ForestCourt,Redrock,StarPlatform}enum ExpeditionModeKind{HoldPoint,TimedBreakthrough,BossGauntlet}
class Enemy{public bool IsDead,IsBoss;public Obj gameObject=new Obj();}class Obj{public bool activeInHierarchy=true;}class ModeState{public ExpeditionModeKind Mode;}
class GameSession{public static GameSession Instance;public bool HasStarted,InDungeon,InCombat,IsInCamp,ChapterActive;public int CurrentHub;public ChapterNode ActiveChapterNode;public object RoomChainRun;public ModeState ModeRun;public List<Enemy> Enemies=new List<Enemy>();}
class AudioProbe{const int SampleRate=22050;const double Tau=Math.PI*2;
'''+names+methods+r'''}
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static string Check(AudioClip clip){var x=clip.Data;C(x.Length>1000,"real PCM length");C(x.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)&&Math.Abs(v)<=.81),"finite normalized PCM");C(x.Sum(v=>(double)v*v)/x.Length>.001,"audible RMS");C(Math.Abs(x[0])<.001&&Math.Abs(x[x.Length-1])<.002,"quiet clip boundaries");return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(x.AsSpan())));}static void Main(){var themes=new HashSet<string>();for(int i=0;i<12;i++)C(themes.Add(Check(AudioProbe.SynthesizeBackground(i))),"each scene has different PCM");var skills=new HashSet<string>();for(int hero=0;hero<4;hero++)for(int skill=0;skill<10;skill++)C(skills.Add(Check(AudioProbe.SynthesizeSkill((HeroClass)hero,skill))),"each class skill has distinct PCM");C(AudioProbe.CurrentMusicTheme()==0,"menu theme");var game=GameSession.Instance=new GameSession{HasStarted=true,IsInCamp=true};C(AudioProbe.CurrentMusicTheme()==0,"camp theme");game.IsInCamp=false;C(AudioProbe.CurrentMusicTheme()==1,"wilderness theme");game.CurrentHub=1;C(AudioProbe.CurrentMusicTheme()==11,"town theme");game.InDungeon=true;C(AudioProbe.CurrentMusicTheme()==2,"ruins theme");for(int i=0;i<3;i++){game.ModeRun=new ModeState{Mode=(ExpeditionModeKind)i};C(AudioProbe.CurrentMusicTheme()==3+i,"mode-specific music");}game.RoomChainRun=new object();C(AudioProbe.CurrentMusicTheme()==6,"corridor theme");game.ChapterActive=true;for(int i=0;i<3;i++){game.ActiveChapterNode=(ChapterNode)i;C(AudioProbe.CurrentMusicTheme()==7+i,"chapter-specific music");}game.Enemies.Add(new Enemy{IsBoss=true});C(AudioProbe.CurrentMusicTheme()==9,"idle boss does not trigger battle music");game.InCombat=true;C(AudioProbe.CurrentMusicTheme()==10,"actual boss combat music");game.Enemies[0].IsDead=true;C(AudioProbe.CurrentMusicTheme()==9,"dead boss returns to scene theme");Console.WriteLine("PASS "+n+" PCM and scene-selection assertions (not listening acceptance)");}}
'''
with tempfile.TemporaryDirectory(prefix='audio-variety-') as tmp:
 p=Path(tmp);project=cv.write_project(p/'project',[],code);sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet';subprocess.run([sdk,'run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
