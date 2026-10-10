using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Emberfall.Editor
{
    [InitializeOnLoad]
    public static class WorldArtVisualReview
    {
        const string Key="Emberfall.WorldArtReview.";
        const string SaveKey="Emberfall.ValidationSaveDirectory";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static int stage=-1,errors;static float next;
        static readonly List<string> rows=new List<string>();
        static string Root=>Path.GetFullPath("Docs/Validation/WorldArt");
        static WorldArtVisualReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Play Mode occupied");
            Directory.CreateDirectory(Root);SessionState.SetString(Key+"Save",SessionState.GetString(SaveKey,""));
            SessionState.SetString(SaveKey,Path.Combine(Root,"IsolatedSave"));SessionState.SetBool(Key+"Active",true);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");EditorApplication.isPlaying=true;
        }
        static void Log(string s,string trace,LogType kind)
        {
            if(trace.Contains("UnityEditor.Search")){rows.Add("EDITOR SEARCH INDEX: "+s);return;}if(SessionState.GetBool(Key+"Active",false)&&(kind==LogType.Error||kind==LogType.Exception)){errors++;rows.Add(s+"\n"+trace);}}
        static void Check(bool ok,string name){rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)errors++;}
        static void Tick()
        {
            if(!SessionState.GetBool(Key+"Active",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();var game=GameSession.Instance;
            if(game==null||game.Progression==null||Time.time<next)return;
            try
            {
                if(stage>=0)Capture(game,stage);
                if(++stage>=18){Finish();return;}
                Setup(game,stage);next=Time.time+.6f;
            }
            catch(Exception e){errors++;rows.Add(e.ToString());Finish();}
        }
        static Type RuntimeType(string name)=>typeof(PlayerController).Assembly.GetType("Emberfall."+name,true);
        static void SetSurfaceTextures(bool enabled)=>RuntimeType("SurfaceTextureLibrary").GetField("Enabled",BindingFlags.Static|BindingFlags.Public).SetValue(null,enabled);
        static Vector3 Location(int n){return n<8?new Vector3(-10,0,-12):n<12?new Vector3(15,0,1):new Vector3(0,0,-10);}
        static void Setup(GameSession game,int n)
        {
            if(n==17)return;
            if(n==16){typeof(PlayerController).GetMethod("CastSkill",Flags).Invoke(game.Player,new object[]{0});return;}
            bool baseline=n%2==0;SetSurfaceTextures(!baseline);WorldTerrain.ReliefEnabled=!baseline;
            UnityEngine.Random.InitState(73413);game.StartNew((HeroClass)((n/2)%4));game.SetPaused(false);
            game.Player.Teleport(WorldTraversal.NearestWalkable(Location(n),.45f));
            foreach(var e in game.Enemies)if(e!=null)e.gameObject.SetActive(false);game.Enemies.Clear();
            typeof(GameSession).GetField("respawnTimer",Flags).SetValue(game,100000f);
            Vector3 enemyAt=WorldTraversal.NearestWalkable(game.Player.transform.position+new Vector3(2.6f,0,2.6f),.65f);
            typeof(GameSession).GetMethod("SpawnEnemy",Flags).Invoke(game,new object[]{EnemyKind.Guardian,1,enemyAt,false});
            var enemy=game.Enemies[game.Enemies.Count-1];enemy.enabled=false;
            if(n%2==1)
            {
                float fall=0;Vector3 start=WorldTraversal.NearestWalkable(new Vector3(15,0,-2),.45f);
                for(int i=0;i<60;i++)start=WorldTraversal.MovePlayer(start,new Vector3(.01f,0,.035f),1f/60,ref fall,.45f);
                Check(Mathf.Abs(start.y-WorldTerrain.Height(start))<.002f,"player follows smooth ramp "+n);
                Vector3 monster=WorldTraversal.Move(enemyAt,new Vector3(.03f,0,.02f),.65f);
                Check(Mathf.Abs(monster.y-WorldTerrain.Height(monster))<.002f,"monster follows ground "+n);
                Vector3 landing=WorldTraversal.ResolveSkillLanding(start,Vector3.forward,2,.45f,22);
                Check(Mathf.Abs(landing.y-WorldTraversal.SurfaceHeight(landing))<.002f,"skill landing stays on surface "+n);
                if(n==9)RuntimeType("EnemyAttackTelegraph").GetMethod("Circle",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{enemyAt,2.6f,enemy.transform});
                if(n==11){RuntimeType("CombatFx").GetMethod("Ring",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{enemyAt,3f,new Color(.4f,.8f,1),1.5f,.1f,false,true});typeof(PlayerController).GetMethod("CastSkill",Flags).Invoke(game.Player,new object[]{0});}
                Check(Resources.Load<Shader>("WorldArt/WeatheredWorld").isSupported,"world shader supported");
                var meadow=Resources.Load<Texture2D>("WorldArt/GroundMeadow");Check(meadow!=null&&meadow.width>=512,"generated meadow texture loaded");
            }
        }
        static void Capture(GameSession game,int n)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)throw new Exception("Graphics required");
            GameObject obj=new GameObject("Review camera");var camera=obj.AddComponent<Camera>();camera.CopyFrom(Camera.main);
            Vector3 focus=game.Player.transform.position+new Vector3(0,.6f,0);
            camera.transform.position=focus+new Vector3(8,11,-13);camera.transform.LookAt(focus);camera.orthographic=true;camera.orthographicSize=n>=12?11:7;
            var rt=new RenderTexture(1280,720,24){antiAliasing=2};var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{camera.aspect=1280f/720;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Root,n.ToString("00")+(n%2==0?"-before":"-after")+".png"),texture.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(obj);}
            rows.Add("CAPTURE "+n+" hero="+game.Player.HeroClass+" height="+game.Player.transform.position.y+" graphics="+SystemInfo.graphicsDeviceType);
        }
        static void Finish()
        {
            File.WriteAllLines(Path.Combine(Root,"review.txt"),rows);SessionState.SetBool(Key+"Active",false);SessionState.SetString(SaveKey,SessionState.GetString(Key+"Save",""));
            SetSurfaceTextures(true);WorldTerrain.ReliefEnabled=true;EditorApplication.isPlaying=false;
            if(Application.isBatchMode)EditorApplication.Exit(errors==0?0:1);
        }
    }
}
