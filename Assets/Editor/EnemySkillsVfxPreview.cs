using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberfall.Editor
{
    // Editor-only actual-cast review, isolated from all player saves.
    [InitializeOnLoad]
    public static class EnemySkillsVfxPreview
    {
        const string Key="Emberfall.EnemySkillsVfx.";
        const string SaveKey="Emberfall.ValidationSaveDirectory";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static IEnumerator routine;
        static float waitUntil;
        static int errors;
        static float previousCaptureDelta;
        static readonly List<string> rows=new List<string>();
        static EnemySkillsVfxPreview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        [MenuItem("Emberfall/预览所有怪物技能特效")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before previewing.");
            if(!Application.isBatchMode&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/Review/EnemySkills-20261011"));
            Directory.CreateDirectory(Path.Combine(output,"IsolatedSave"));
            SessionState.SetString(Key+"Output",output);SessionState.SetString(Key+"PreviousSave",SessionState.GetString(SaveKey,""));
            SessionState.SetFloat(Key+"PreviousEffects",EffectPreferences.EffectsScale);
            SessionState.SetString(SaveKey,Path.Combine(output,"IsolatedSave"));SessionState.SetBool(Key+"Active",true);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity",OpenSceneMode.Single);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
        }
        static void Log(string condition,string stack,LogType type)
        {if(SessionState.GetBool(Key+"Active",false)&&(type==LogType.Exception||type==LogType.Error)){errors++;rows.Add("ERROR "+condition+"\n"+stack);}}
        static void Tick()
        {
            if(!SessionState.GetBool(Key+"Active",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();
            if(GameSession.Instance==null||GameSession.Instance.Progression==null)return;
            try
            {
                if(routine==null){Application.runInBackground=true;previousCaptureDelta=Time.captureDeltaTime;Time.captureDeltaTime=1f/20f;routine=Review();}
                if(Time.time<waitUntil)return;
                if(!routine.MoveNext()){Finish(errors==0?0:1);return;}
                waitUntil=Time.time+(routine.Current is float delay?delay:0);
            }
            catch(Exception error){rows.Add(error.ToString());Debug.LogException(error);Finish(1);}
        }
        static IEnumerator Review()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)throw new InvalidOperationException("Real graphics device required.");
            var game=GameSession.Instance;string output=SessionState.GetString(Key+"Output","");
            string[] labels={"Slime-Melee","Goblin-Melee","Wisp-Bolt","Guardian-Slam","Boss-Slam","Boss-Charge","Boss-Fan","Boss-Sweep"};
            foreach(bool reduced in new[]{false,true})
            foreach(string label in labels)
            {
                EffectPreferences.EffectsScale=reduced?.4f:1;
                UnityEngine.Random.InitState(711);game.StartNew(HeroClass.Vanguard);
                CombatReviewBuildSetup.Apply(game.Progression,new CombatReviewConfiguration{id="enemy-vfx",hero=HeroClass.Vanguard,level=100,skillRanks=new[]{3,3,3,3,3,3,3,3,3,3}},Path.Combine(output,"IsolatedSave"));
                var player=game.Player;player.RefreshStats(true);player.enabled=false;game.SetPaused(false);game.SetUIBlocking(false);var ui=game.GetComponent<GameUI>();if(ui!=null)ui.enabled=false;
                foreach(var old in game.Enemies)if(old!=null){old.gameObject.SetActive(false);UnityEngine.Object.Destroy(old.gameObject);}game.Enemies.Clear();
                typeof(GameSession).GetField("respawnTimer",Private).SetValue(game,100000f);
                player.Teleport(WorldTraversal.NearestWalkable(new Vector3(5,0,5),.45f));typeof(PlayerController).GetProperty("Health").SetValue(player,1000000f);
                bool boss=label.StartsWith("Boss");EnemyKind kind=label.StartsWith("Slime")?EnemyKind.Slime:label.StartsWith("Goblin")?EnemyKind.Goblin:label.StartsWith("Wisp")?EnemyKind.Wisp:EnemyKind.Guardian;
                float distance=label.Contains("Slam")?2f:label.Contains("Melee")?1.3f:5f;
                var at=WorldTraversal.NearestWalkable(player.transform.position+Vector3.forward*distance,.8f);
                typeof(GameSession).GetMethod("SpawnEnemy",Private).Invoke(game,new object[]{kind,100,at,boss});var enemy=game.Enemies[0];enemy.enabled=false;
                typeof(EnemyController).GetProperty("Health").SetValue(enemy,1000000f);
                enemy.transform.rotation=Quaternion.LookRotation(player.transform.position-at);Camera.main.GetComponent<AdventureCamera>().Snap();
                yield return .04f;
                float windup;
                LargeExpeditionBoss sweep=null;
                if(label=="Boss-Sweep")
                {
                    sweep=LargeExpeditionBoss.Configure(enemy,1,711);typeof(EnemyController).GetProperty("Health").SetValue(enemy,enemy.MaxHealth*.6f);
                    enemy.enabled=true;windup=LargeBossPhaseState.WindupSeconds;
                }
                else
                {
                    string attack=label.Substring(label.IndexOf('-')+1);
                    var type=typeof(EnemyController).GetNestedType("AttackType",BindingFlags.NonPublic);
                    bool accepted=(bool)typeof(EnemyController).GetMethod("PrepareAttack",Private).Invoke(enemy,new object[]{Enum.Parse(type,attack),false,player.transform.position});
                    if(!accepted)throw new InvalidOperationException(label+" actual prepare rejected");
                    windup=(float)typeof(EnemyController).GetField("windup",Private).GetValue(enemy);enemy.enabled=true;
                }
                float previous=0;
                foreach(float sample in new[]{.15f,windup*.75f,windup+.05f,windup+.2f,windup+.45f,windup+.7f})
                {
                    yield return Mathf.Max(0,sample-previous);previous=sample;
                    Capture(Camera.main,Path.Combine(output,label+"-"+(reduced?"Reduced":"Normal")+"-"+sample.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                }
                if(sweep!=null&&sweep.State.Phase!=LargeBossPhase.Beam)throw new InvalidOperationException("Sweep did not actually reach Beam: "+sweep.State.Phase);
                if(sweep==null&&(bool)typeof(EnemyController).GetField("preparing",Private).GetValue(enemy))throw new InvalidOperationException("Attack did not release");
                rows.Add(label+" actual controller release; reduced="+reduced+"; windup="+windup);
                var effectType=typeof(PlayerController).Assembly.GetType("Emberfall.SkillImpactDetail");
                foreach(var effect in UnityEngine.Object.FindObjectsByType(effectType,FindObjectsSortMode.None))
                {
                    float before=(float)effectType.GetField("age",Private).GetValue(effect);game.SetPaused(true);effectType.GetMethod("Update",Private).Invoke(effect,null);
                    if((float)effectType.GetField("age",Private).GetValue(effect)!=before)throw new InvalidOperationException("Enemy effect advanced while paused");game.SetPaused(false);
                }
            }
            rows.Add("Actual Unity "+Application.unityVersion+" / "+SystemInfo.graphicsDeviceType+"; 16 actual attack scenarios, normal/reduced. Captures include original warning and real controller release. Not device FPS measurement.");
        }
        static void Capture(Camera camera,string path)
        {
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float oldAspect=camera.aspect;
            var render=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=2};
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{camera.aspect=1280f/720;camera.targetTexture=render;camera.Render();RenderTexture.active=render;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=oldTarget;camera.aspect=oldAspect;RenderTexture.active=oldActive;render.Release();UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(image);}
        }
        static void Finish(int code)
        {
            string output=SessionState.GetString(Key+"Output","");File.WriteAllLines(Path.Combine(output,"actual-casts.txt"),rows);
            SessionState.SetBool(Key+"Active",false);SessionState.SetString(SaveKey,SessionState.GetString(Key+"PreviousSave",""));
            EffectPreferences.EffectsScale=SessionState.GetFloat(Key+"PreviousEffects",1);Time.captureDeltaTime=previousCaptureDelta;EditorApplication.isPlaying=false;
            if(Application.isBatchMode)EditorApplication.Exit(code);
        }
    }
}
