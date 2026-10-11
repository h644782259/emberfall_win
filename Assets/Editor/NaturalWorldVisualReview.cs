using System;using System.IO;using System.Reflection;using System.Collections.Generic;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace Emberfall.Editor
{
 [InitializeOnLoad] public static class NaturalWorldVisualReview
 {
  const string Key="Emberfall.NaturalWorldReview";const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
  static int stage=-1,errors;static double next;static readonly List<string> rows=new List<string>();
  static string Root=>Path.GetFullPath("Docs/Validation/NaturalWorld");
  static NaturalWorldVisualReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
  public static void Run(){Directory.CreateDirectory(Root);SessionState.SetString("Emberfall.ValidationSaveDirectory",Root+"/Save");SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");EditorApplication.isPlaying=true;}
  static void Log(string message,string trace,LogType type){if(!SessionState.GetBool(Key,false)||trace.Contains("UnityEditor.Search"))return;if(type==LogType.Error||type==LogType.Exception){errors++;rows.Add(message+"\n"+trace);}}
  static void Check(bool ok,string label){rows.Add((ok?"PASS ":"FAIL ")+label);if(!ok)errors++;}
  static void Change(GameSession g){typeof(GameSession).GetMethod("ChangeZone",F).Invoke(g,new object[]{false});}
  static void Tick()
  {
   if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   EditorApplication.QueuePlayerLoopUpdate();var g=GameSession.Instance;if(g==null||g.Progression==null||EditorApplication.timeSinceStartup<next)return;
   try{
    if(stage<0){g.StartNew(HeroClass.Vanguard);g.SetPaused(false);stage=0;next=EditorApplication.timeSinceStartup+2;return;}
    if(stage==0){Capture("camp",new Vector3(4,9,-18),new Vector3(0,.8f,-10),6.5f);Validate(g);g.Player.Teleport(new Vector3(-13,0,-11));}
    if(stage==1){Capture("woodland-hill",new Vector3(-8,6,-18),new Vector3(-13,1.2f,-9),5);Check(Mathf.Abs(g.Player.transform.position.y-WorldTraversal.SurfaceHeight(g.Player.transform.position))<.1f,"hero stays grounded on hill");}
    if(stage==2){Capture("brook",new Vector3(7,6,-8),new Vector3(1,.2f,-1),5);Change(g);Check(g.BeginPractice(CampPracticeScenario.Stationary,10),"practice can start during pending camp population");Check(g.StartPractice(),"practice starts");}
    if(stage==3){Check(g.Enemies.Count==1,"ambient spawn does not enter active practice");g.EndPractice("场景验证结束");g.SetUIBlocking(false);g.SetPaused(false);}
    if(stage==4){Check(g.Enemies.Count==12,"ambient population resumes after practice");typeof(GameSession).GetMethod("ChangeZone",F).Invoke(g,new object[]{true});}
    if(stage==5){Capture("dungeon",new Vector3(8,10,-14),new Vector3(0,.2f,0),10);Validate(g);g.Progression.Profile.unlockedHubMask=7;g.Progression.Profile.currentHub=1;Change(g);Check(g.CurrentHub==1,"quarry review uses correct hub");}
    if(stage==6){Capture("quarry",new Vector3(7,11,-18),new Vector3(0,.5f,-2),10);Validate(g);g.Progression.Profile.currentHub=2;Change(g);Check(g.CurrentHub==2,"observatory review uses correct hub");}
    if(stage==7){Capture("observatory",new Vector3(7,11,-18),new Vector3(0,.5f,-2),10);Validate(g);Finish();return;}
    stage++;next=EditorApplication.timeSinceStartup+1.5;
   }catch(Exception e){errors++;rows.Add(e.ToString());Finish();}
  }
  static void Validate(GameSession g)
  {
   var shader=Resources.Load<Shader>("WorldArt/WeatheredWorld");Check(shader!=null&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"world material compiles on "+SystemInfo.graphicsDeviceType);
   var world=(GameObject)typeof(GameSession).GetField("world",F).GetValue(g);int detail=0;int trees=0;
   foreach(var filter in world.GetComponentsInChildren<MeshFilter>())
   {
    if(filter.name.StartsWith("Merged ")){detail++;Check(filter.sharedMesh.vertexCount<25000,"bounded merged detail vertices");}
    if(filter.name=="Open individual leaf canopy"){trees++;Check(filter.sharedMesh.triangles.Length/3<3000,"bounded shared leaf canopy");}
   }
   rows.Add("geometry hub="+g.CurrentHub+" dungeon="+g.InDungeon+" merged detail renderers="+detail+" tree canopies="+trees);
  }
  static void Capture(string name,Vector3 position,Vector3 target,float size)
  {
   var obj=new GameObject("Natural world review camera");var camera=obj.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.transform.position=position;camera.transform.LookAt(target);camera.orthographic=true;camera.orthographicSize=size;camera.ResetProjectionMatrix();
   var rt=new RenderTexture(1600,1000,24){antiAliasing=4};var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);var old=RenderTexture.active;
   try{camera.targetTexture=rt;camera.aspect=1.6f;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Root+"/"+name+".png",tex.EncodeToPNG());rows.Add("CAPTURE "+name);}
   finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
  }
  static void Finish(){SessionState.SetBool(Key,false);File.WriteAllLines(Root+"/review.txt",rows);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(errors==0?0:1);}
 }
}
