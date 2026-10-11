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
    public static class ChainLightningVfxPreview
    {
        const string Key="Emberfall.ChainLightningVfx.";
        const string SaveKey="Emberfall.ValidationSaveDirectory";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static IEnumerator routine;
        static float waitUntil;
        static int errors;
        static float previousCaptureDelta;
        static readonly List<string> rows=new List<string>();
        static ChainLightningVfxPreview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        [MenuItem("Emberfall/预览雷霆锁链持续电流")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before previewing.");
            if(!Application.isBatchMode&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            routine=null;waitUntil=0;errors=0;rows.Clear();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/Review/ChainLightning-20261011"));
            Directory.CreateDirectory(Path.Combine(output,"IsolatedSave"));
            SessionState.SetString(Key+"Output",output);SessionState.SetString(Key+"PreviousSave",SessionState.GetString(SaveKey,""));
            SessionState.SetFloat(Key+"PreviousEffects",EffectPreferences.EffectsScale);
            SessionState.SetString(SaveKey,Path.Combine(output,"IsolatedSave"));SessionState.SetBool(Key+"Active",true);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;
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
                if(routine==null){Application.runInBackground=true;previousCaptureDelta=Time.captureDeltaTime;Time.captureDeltaTime=1f/60f;routine=Review();}
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
            foreach(HeroClass hero in new[]{HeroClass.Arcanist})
            {
                EffectPreferences.EffectsScale=reduced?.4f:1;
                UnityEngine.Random.InitState(61453);game.StartNew(hero);
                var config=new CombatReviewConfiguration{id="all-skill-vfx",hero=hero,level=100,skillRanks=new[]{3,3,3,3,3,3,3,3,3,3},specialization=ElementalistSpecialization.None};
                CombatReviewBuildSetup.Apply(game.Progression,config,Path.Combine(output,"IsolatedSave"));
                game.Player.RefreshStats(true);game.SetPaused(false);
                foreach(int skill in new[]{4})
                {
                    var player=game.Player;
                    foreach(var enemy in game.Enemies)if(enemy!=null){enemy.gameObject.SetActive(false);UnityEngine.Object.Destroy(enemy.gameObject);}game.Enemies.Clear();
                    typeof(GameSession).GetField("respawnTimer",Private).SetValue(game,100000f);
                    player.ResetCooldownsForDungeonEntry();player.RefreshStats(true);player.Teleport(FindOpenReviewPoint());
                    player.transform.rotation=Quaternion.identity;
                    var runtime=(SkillRuntime)typeof(PlayerController).GetField("skillRuntime",Private).GetValue(player);runtime.Advance(200);runtime.FillEnergy();
                    for(int i=0;i<3;i++)
                    {
                        var at=WorldTraversal.NearestWalkable(player.transform.position+Vector3.forward*(3+i*1.5f)+Vector3.right*(i-1)*1.2f,.6f);
                        typeof(GameSession).GetMethod("SpawnEnemy",Private).Invoke(game,new object[]{new[]{EnemyKind.Guardian,EnemyKind.Slime,EnemyKind.Wisp}[i],100,at,false});
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
                    int before=(int)typeof(PlayerController).GetField("nextCastId",Private).GetValue(player);
                    typeof(PlayerController).GetMethod("CastSkill",Private).Invoke(player,new object[]{skill});
                    if((int)typeof(PlayerController).GetField("nextCastId",Private).GetValue(player)==before)
                        throw new InvalidOperationException("Chain cast rejected; rank="+game.Progression.Profile.skillRanks[skill]+" energy="+player.Energy+" sight="+WorldTraversal.HasLineOfSight(player.transform.position,game.Enemies[0].transform.position));
                    rows.Add(hero+"/"+skill+" "+GameBalance.SkillName(hero,skill)+" actual release/trigger; reduced="+reduced+"; energy="+player.Energy);
                    player.enabled=false;
                    var visualType=typeof(PlayerController).Assembly.GetType("Emberfall.ChainElectrifiedVisual",true);
                    yield return .08f;
                    foreach(var enemy in game.Enemies)if(enemy.GetComponentInChildren(visualType)==null)throw new InvalidOperationException("Confirmed chain target has no body current.");
                    float[] hitHealth=game.Enemies.ConvertAll(e=>e.Health).ToArray();
                    float previous=.08f;
                    foreach(float sample in new[]{.1f,.2f,.3f,.4f,.5f,.6f,.7f,.8f,.9f,1f,1.1f,1.2f,1.3f,1.4f,1.5f,1.8f})
                    {
                        yield return sample-previous;previous=sample;
                        Capture(Camera.main,Path.Combine(output,hero+"-"+skill+"-"+(reduced?"Reduced":"Normal")+"-"+sample.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                    }
                    for(int n=0;n<game.Enemies.Count;n++)if(game.Enemies[n].Health!=hitHealth[n])throw new InvalidOperationException("Cosmetic current changed health.");
                    foreach(var enemy in game.Enemies)if(enemy.GetComponentInChildren(visualType)!=null)throw new InvalidOperationException("After-current failed to expire.");
                    var victim=game.Enemies[0];
                    var attach=visualType.GetMethod("Attach",BindingFlags.Static|BindingFlags.NonPublic);
                    var ageField=visualType.GetField("age",Private);
                    attach.Invoke(null,new object[]{player,victim});yield return .15f;
                    var effect=victim.GetComponentInChildren(visualType);float age=(float)ageField.GetValue(effect);
                    game.SetPaused(true);for(int n=0;n<12;n++)yield return 0f;
                    if((float)ageField.GetValue(effect)!=age)throw new InvalidOperationException("Current advanced while paused.");
                    game.SetPaused(false);attach.Invoke(null,new object[]{player,victim});
                    if(victim.GetComponentInChildren(visualType)!=effect||(float)ageField.GetValue(effect)!=0)throw new InvalidOperationException("Repeated hit stacked instead of refreshing.");
                    victim.TakeDamage(2000000,Vector3.forward,0,0);yield return .05f;
                    if(victim.GetComponentInChildren(visualType)!=null)throw new InvalidOperationException("Dead target kept body current.");
                    attach.Invoke(null,new object[]{player,game.Enemies[1]});
                    var epochProperty=typeof(PlayerController).GetProperty("CombatEpoch",Private|BindingFlags.Public);
                    epochProperty.SetValue(player,(int)epochProperty.GetValue(player)+1);yield return .05f;
                    if(game.Enemies[1].GetComponentInChildren(visualType)!=null)throw new InvalidOperationException("Old combat generation kept current.");
                    player.enabled=true;
                    rows.Add("PASS expiry / paused clock / same-target refresh / death / epoch teardown / no extra damage; reduced="+reduced);
                }
            }
            rows.Add("Actual Unity "+Application.unityVersion+" graphics="+SystemInfo.graphicsDeviceType+"; 2 real chain casts across guardian/slime/wisp, normal/reduced effects; cosmetic lifecycle checks. Camera captures exclude OnGUI; no measured device FPS.");
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
