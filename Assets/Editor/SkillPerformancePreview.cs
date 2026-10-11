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
    public static class SkillPerformancePreview
    {
        const string Key="Emberfall.SkillPerformance.";
        const string SaveKey="Emberfall.ValidationSaveDirectory";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static IEnumerator routine;
        static float waitUntil;
        static int errors;static double traceTime;
        static float previousCaptureDelta;
        static readonly List<string> rows=new List<string>();
        static SkillPerformancePreview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        [MenuItem("Emberfall/预览所有职业技能特效")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before previewing.");
            if(!Application.isBatchMode&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/Review/SkillPerformance-20261011"));
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
            if(!SessionState.GetBool(Key+"Active",false))return;
            if(EditorApplication.timeSinceStartup-traceTime>10){traceTime=EditorApplication.timeSinceStartup;Debug.Log("Skill review state: playing="+EditorApplication.isPlaying+" paused="+EditorApplication.isPaused+" compiling="+EditorApplication.isCompiling+" time="+Time.time+" scale="+Time.timeScale+" game="+(GameSession.Instance!=null));}
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
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
            foreach(bool reduced in new[]{false,true})
            foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {
                EffectPreferences.EffectsScale=reduced?.4f:1;
                UnityEngine.Random.InitState(61453);game.StartNew(hero);
                var config=new CombatReviewConfiguration{id="all-skill-vfx",hero=hero,level=100,skillRanks=new[]{3,3,3,3,3,3,3,3,3,3},specialization=ElementalistSpecialization.None};
                CombatReviewBuildSetup.Apply(game.Progression,config,Path.Combine(output,"IsolatedSave"));
                game.Player.RefreshStats(true);game.SetPaused(false);game.SetUIBlocking(false);
                var ui=game.GetComponent<GameUI>();if(ui!=null)ui.enabled=false;
                foreach(int skill in new[]{0,1,2,4,5,6,7,9,8,3})
                {
                    var player=game.Player;
                    foreach(var enemy in game.Enemies)if(enemy!=null){enemy.gameObject.SetActive(false);UnityEngine.Object.Destroy(enemy.gameObject);}game.Enemies.Clear();
                    typeof(GameSession).GetField("respawnTimer",Private).SetValue(game,100000f);
                    player.ResetCooldownsForDungeonEntry();player.RefreshStats(true);player.Teleport(FindOpenReviewPoint());
                    player.transform.rotation=hero==HeroClass.Ranger&&skill==4?Quaternion.Euler(0,180,0):Quaternion.identity;
                    var runtime=(SkillRuntime)typeof(PlayerController).GetField("skillRuntime",Private).GetValue(player);runtime.Advance(200);runtime.FillEnergy();
                    for(int i=0;i<3;i++)
                    {
                        var at=WorldTraversal.NearestWalkable(player.transform.position+Vector3.forward*(3+i*1.5f)+Vector3.right*(i-1)*1.2f,.6f);
                        typeof(GameSession).GetMethod("SpawnEnemy",Private).Invoke(game,new object[]{EnemyKind.Guardian,100,at,false});
                        var enemy=game.Enemies[game.Enemies.Count-1];enemy.enabled=false;
                        typeof(EnemyController).GetProperty("Health").SetValue(enemy,1000000f);
                    }
                    typeof(PlayerController).GetProperty("AimTarget").SetValue(player,game.Enemies[0]);
                    typeof(PlayerController).GetField("aimPoint",Private).SetValue(player,game.Enemies[0].transform.position);
                    Camera.main.GetComponent<AdventureCamera>().Snap();
                    yield return .04f;
                    typeof(PlayerController).GetProperty("AimTarget").SetValue(player,game.Enemies[0]);
                    typeof(PlayerController).GetField("aimPoint",Private).SetValue(player,game.Enemies[0].transform.position);
                    runtime.Advance(200);runtime.FillEnergy();
                    if(skill==3)
                    {
                        int before=(int)typeof(PlayerController).GetField("nextCastId",Private).GetValue(player);
                        typeof(PlayerController).GetMethod("CastSkill",Private).Invoke(player,new object[]{skill});
                        if((int)typeof(PlayerController).GetField("nextCastId",Private).GetValue(player)!=before)throw new InvalidOperationException("Stat passive must not cast.");
                        rows.Add(hero+"/3 "+GameBalance.SkillName(hero,3)+" — stat passive, no invented cast; reduced="+reduced);continue;
                    }
                    if(skill==8)
                    {
                        typeof(PlayerController).GetProperty("Health").SetValue(player,player.MaxHealth*.2f);
                        typeof(PlayerController).GetField("passiveCooldown",Private).SetValue(player,0f);
                        typeof(PlayerController).GetMethod("TryDefensePassive",Private).Invoke(player,null);
                        if((float)typeof(PlayerController).GetField("passiveTime",Private).GetValue(player)<=0)throw new InvalidOperationException("Actual defense trigger rejected.");
                    }
                    else
                    {
                        if(hero==HeroClass.Vanguard&&skill==6)typeof(PlayerController).GetProperty("Health").SetValue(player,player.MaxHealth*.45f);
                        int before=(int)typeof(PlayerController).GetField("nextCastId",Private).GetValue(player);
                        typeof(PlayerController).GetMethod("CastSkill",Private).Invoke(player,new object[]{skill});
                        if((int)typeof(PlayerController).GetField("nextCastId",Private).GetValue(player)==before)throw new InvalidOperationException(hero+" skill "+skill+" rejected: "+game.Progression.LastError);
                    }
                    CheckCastIsolation(player);
                    rows.Add(hero+"/"+skill+" "+GameBalance.SkillName(hero,skill)+" actual release/trigger; reduced="+reduced+"; energy="+player.Energy);Debug.Log("Skill review release "+hero+"/"+skill+" reduced="+reduced);
                    float previous=0;
                    foreach(float sample in skill==2||skill==7||skill==9?new[]{.1f,.5f,1.2f,2.5f,4.5f,6.8f,8f}:new[]{.1f,.35f,.8f,1.6f,2.4f})
                    {
                        yield return sample-previous;previous=sample;
                        CheckActiveEffects(game);
                        Capture(Camera.main,Path.Combine(output,hero+"-"+skill+"-"+(reduced?"Reduced":"Normal")+"-"+sample.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                    }
                }
            }
            rows.Add("Actual Unity "+Application.unityVersion+" graphics="+SystemInfo.graphicsDeviceType+"; 64 active releases, 8 actual defense triggers, 8 stat passive no-cast checks; sampled pause, stale-cast isolation and light/particle budgets. Real camera world captures exclude OnGUI. This validates release/trigger/rendering, not human timing acceptance or measured device FPS.");
        }
        static void CheckCastIsolation(PlayerController player)
        {
            var model=player.GetComponentInChildren<CombatModel>();if(model==null)return;
            var remaining=typeof(CombatModel).GetField("performanceRemaining",Private);
            if((float)remaining.GetValue(model)<=0)return;
            var age=typeof(CombatModel).GetField("performanceBeatAge",Private);
            var cast=typeof(CombatModel).GetField("performanceCast",Private);
            float before=(float)age.GetValue(model);
            typeof(CombatModel).GetMethod("SkillPerformanceBeat",Private).Invoke(model,new object[]{-1,(int)cast.GetValue(model)-1});
            if((float)age.GetValue(model)!=before)throw new InvalidOperationException("An older cast moved the current guiding pose.");
        }
        static void CheckActiveEffects(GameSession game)
        {
            Type type=typeof(PlayerController).Assembly.GetType("Emberfall.SkillPerformanceVfx",true);
            if((int)type.GetField("lights",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)>3)throw new InvalidOperationException("Shared spell-light budget exceeded.");
            foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
            {
                if(component.GetType()!=type)continue;
                var age=type.GetField("age",Private);float before=(float)age.GetValue(component);
                game.SetPaused(true);
                try{type.GetMethod("LateUpdate",Private).Invoke(component,null);if((float)age.GetValue(component)!=before)throw new InvalidOperationException("A sustained spell advanced while paused.");}
                finally{game.SetPaused(false);}
                foreach(var particles in component.GetComponentsInChildren<ParticleSystem>())
                    if(particles.main.maxParticles>96)throw new InvalidOperationException("Sustained spell-particle budget exceeded.");
                var at=component.transform.position;
                if(float.IsNaN(at.x)||float.IsNaN(at.y)||float.IsNaN(at.z))throw new InvalidOperationException("Invalid moving spell position.");
            }
        }
        static Vector3 FindOpenReviewPoint()
        {
            for(int x=-14;x<=14;x+=2)for(int z=-14;z<=10;z+=2)
            {
                var origin=new Vector3(x,0,z);if(!WorldTraversal.IsWalkable(origin,.6f))continue;
                bool clear=true;Vector3 previous=origin;
                for(int i=0;i<3;i++)
                {
                    var at=origin+Vector3.forward*(3+i*1.5f)+Vector3.right*(i-1)*1.2f;
                    if(!WorldTraversal.IsWalkable(at,.6f)||!WorldTraversal.HasLineOfSight(origin,at)||!WorldTraversal.HasLineOfSight(previous,at)){clear=false;break;}previous=at;
                }
                if(clear)return origin;
            }
            throw new InvalidOperationException("No clear actual-cast review space found.");
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
