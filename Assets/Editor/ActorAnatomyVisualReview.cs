using System;using System.IO;using System.Reflection;using System.Collections.Generic;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace Emberfall.Editor
{
 [InitializeOnLoad] public static class ActorAnatomyVisualReview
 {
  const string Key="Emberfall.ActorAnatomyReview";const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
  static int stage=-1,errors,frames;static double next;static GameObject root;static CombatModel[] models;static readonly List<string> rows=new List<string>();
  static string Root=>Path.GetFullPath("Docs/Validation/ActorAnatomy");
  static ActorAnatomyVisualReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
  public static void Run(){Directory.CreateDirectory(Root);SessionState.SetString("Emberfall.ValidationSaveDirectory",Root+"/Save");SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");EditorApplication.isPlaying=true;}
  static void Log(string message,string trace,LogType type){if(!SessionState.GetBool(Key,false))return;if(trace.Contains("UnityEditor.Search"))return;if(type==LogType.Error||type==LogType.Exception){errors++;rows.Add(message+"\n"+trace);}}
  static void Enable(bool enabled){foreach(string name in new[]{"ActorAnatomy","ActorSurfaceMaterial"})typeof(CombatModel).Assembly.GetType("Emberfall."+name).GetField("Enabled",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,enabled);}
  static void Check(bool ok,string label){rows.Add((ok?"PASS ":"FAIL ")+label);if(!ok)errors++;}
  static void Tick()
  {
   if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   EditorApplication.QueuePlayerLoopUpdate();var game=GameSession.Instance;if(game==null||game.Progression==null)return;
   if(EditorApplication.timeSinceStartup<next)return;
   try {
    if(stage<0){game.StartNew(HeroClass.Vanguard);game.SetPaused(false);stage=0;Setup();next=EditorApplication.timeSinceStartup+.8;return;}
    Sample();Capture(stage);if(stage==0||stage==1||stage==4||stage==5)Capture(stage+20);Validate();
    if(stage==9){Finish();return;}stage++;Setup();next=EditorApplication.timeSinceStartup+.4;
   } catch(Exception e){errors++;rows.Add(e.ToString());Finish();}
  }
  static void Setup()
  {
   if(root!=null)UnityEngine.Object.DestroyImmediate(root);Enable(stage!=0&&stage!=4);
   root=new GameObject("Isolated actor review");root.transform.position=new Vector3(60,0,0);models=new CombatModel[4];
   bool enemy=stage>=4;for(int i=0;i<4;i++){
    var owner=new GameObject("Review actor "+i);owner.transform.SetParent(root.transform,false);owner.transform.localPosition=new Vector3((i-1.5f)*2.9f,0,0);
    models[i]=enemy?CombatModel.Enemy(owner.transform,(EnemyKind)i,false):CombatModel.Hero(owner.transform,(HeroClass)i);
    if(!enemy)models[i].ConfigurePreview();
   }
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localPosition=new Vector3(0,-.10f,0);floor.transform.localScale=new Vector3(14,.20f,6);UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
   var material=new Material(Shader.Find("Standard")){color=new Color(.12f,.17f,.18f)};floor.GetComponent<Renderer>().sharedMaterial=material;
   root.AddComponent<ReviewMaterialOwner>().Value=material;
   var fill=new GameObject("Portrait fill");fill.transform.SetParent(root.transform,false);fill.transform.rotation=Quaternion.Euler(30,180,0);var light=fill.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.45f;light.color=new Color(1,.90f,.78f);light.shadows=LightShadows.None;
  }
  static void Sample()
  {
   bool enemy=stage>=4;float t=stage==6?.75f:stage==7?0:stage==8?.25f:stage==9?.65f:.38f;
   foreach(var model in models){
    if(!enemy){if(stage==2)for(int n=0;n<30;n++)model.SetLocomotion(new Vector3(0,0,.04f),1f/60,3,true);model.SamplePreview(1.5f,stage==2?CollectionPreviewAction.Move:stage==3?CollectionPreviewAction.Attack:CollectionPreviewAction.Idle,t);}
    else{if(stage>=8)for(int n=0;n<30;n++)model.SetLocomotion(new Vector3(0,0,.03f),1f/60,3,true);model.SetEnemyAttackPose(stage==6?EnemyPosePhase.Windup:stage==7?EnemyPosePhase.Recovery:EnemyPosePhase.Idle,t);model.Animate(stage>=8?.8f:0,stage==7?1:0,false);}
   }
  }
  static void Validate()
  {
   if(stage==0||stage==4)return;
   Check(Resources.Load<Shader>("WorldArt/ActorSurface").isSupported,"actor shader supported stage "+stage);
   int skins=0;foreach(var model in models)foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
    skins++;Check(renderer.bones.Length==2&&renderer.sharedMesh.vertexCount<=500,"bounded two-bone continuous limb");
    foreach(var weight in renderer.sharedMesh.boneWeights)if(Mathf.Abs(weight.weight0+weight.weight1-1)>.0001f)throw new Exception("Unnormalized skin weight");
    var baked=new Mesh();renderer.BakeMesh(baked);foreach(var v in baked.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))throw new Exception("Invalid deformed vertex");UnityEngine.Object.DestroyImmediate(baked);
   }
   Check(skins==(stage<4?16:8),"continuous limbs on all humanoids stage "+stage+" count="+skins);
  }
  static void Capture(int n)
  {
   if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)throw new Exception("Graphics required");
   int selected=stage<4?0:3;
   if(n>=20)for(int i=0;i<models.Length;i++)if(i!=selected)models[i].transform.parent.gameObject.SetActive(false);
   var obj=new GameObject("Actor review camera");var camera=obj.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.transform.position=new Vector3(60,3.3f,10);camera.transform.LookAt(new Vector3(60,1.2f,0));camera.orthographic=true;camera.orthographicSize=3.2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.065f);
   if(n>=20){Vector3 at=models[selected].transform.parent.position;camera.transform.position=at+new Vector3(.7f,2.8f,7);camera.transform.LookAt(at+new Vector3(0,1.3f,0));camera.orthographicSize=stage<4?1.5f:2.0f;}
   var rt=new RenderTexture(1600,900,24){antiAliasing=2};var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var old=RenderTexture.active;
   try{camera.targetTexture=rt;camera.aspect=1600f/900;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Root+"/"+n.ToString("00")+".png",image.EncodeToPNG());rows.Add("CAPTURE "+n+" graphics="+SystemInfo.graphicsDeviceType);}
   finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);if(n>=20)for(int i=0;i<models.Length;i++)models[i].transform.parent.gameObject.SetActive(true);}
  }
  static void Finish(){Enable(true);SessionState.SetBool(Key,false);File.WriteAllLines(Root+"/review.txt",rows);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(errors==0?0:1);}
 }
 public sealed class ReviewMaterialOwner:MonoBehaviour{public Material Value;void OnDestroy(){if(Value!=null)Destroy(Value);}}
}
