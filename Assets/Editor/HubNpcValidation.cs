using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    // Uses the real UI routes, procedural rigs, Unity clocks and imported voice assets.
    public static class HubNpcValidation
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static object Call(object target,string name,params object[] args)
        {return target.GetType().GetMethod(name,Private).Invoke(target,args);}
        private static string Panel(GameUI ui)
        {return typeof(GameUI).GetField("panel",Private).GetValue(ui).ToString();}
        private static IEnumerator Wait(float seconds)
        {float end=Time.unscaledTime+seconds;while(Time.unscaledTime<end)yield return null;}

        public static IEnumerator Validate(GameSession game,Action<bool,string> check,Action<string> log,string output)
        {
            check(game.Progression.SaveFilePath.StartsWith(Path.Combine(output,"IsolatedSave"),StringComparison.OrdinalIgnoreCase),"NPC: saves isolated before creating a character");
            GameUI ui=game.GetComponent<GameUI>();
            Call(ui,"HandleHubNpcShortcut",true);
            check(!game.HasStarted&&Panel(ui)=="None","NPC: G at title cannot open dialogue");
            game.StartNew(HeroClass.Vanguard);
            game.Progression.GrantExperience(1000000);
            game.Player.enabled=false;
            MobileControls.SimulationEnabled=false;
            foreach(var enemy in game.Enemies)if(enemy!=null){enemy.gameObject.SetActive(false);UnityEngine.Object.Destroy(enemy.gameObject);}
            game.Enemies.Clear();
            game.Notify("");
            check(!GameBalance.IsBindableKey((int)KeyCode.G)&&!game.Progression.SetHotbarKey(0,(int)KeyCode.G),"NPC: G reserved from skill bindings");
            int[] previous=(int[])GameBalance.DefaultHotbarKeys.Clone();previous[0]=(int)KeyCode.G;
            var repaired=(int[])typeof(ProgressionService).GetMethod("RepairHotbarKeys",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{previous});
            check(repaired[0]!=103&&repaired[1]==previous[1]&&new System.Collections.Generic.HashSet<int>(repaired).Count==repaired.Length,"NPC: legacy G binding repaired without duplicate skill keys");

            game.Player.Teleport(new Vector3(0,0,-15));
            Call(ui,"HandleHubNpcShortcut",true);
            check(Panel(ui)=="None","NPC: G outside approach range ignored");
            game.Player.Teleport(GameSession.HubNpcPosition(0)+Vector3.back*1.8f);
            game.SetPaused(true);
            Call(ui,"HandleHubNpcShortcut",true);
            check(Panel(ui)=="None","NPC: pause rejects G");
            game.SetPaused(false);
            Call(ui,"TogglePanel",Enum.Parse(typeof(GameUI).GetNestedType("Panel",BindingFlags.NonPublic),"Skills"));
            Call(ui,"HandleHubNpcShortcut",true);
            check(Panel(ui)=="Skills"&&game.ActiveHubNpc==HubNpcKind.None,"NPC: existing skill panel rejects G");
            Call(ui,"ClosePanel");

            for(int hub=0;hub<3;hub++)
            {
                if(hub>0)check(game.TravelToHub(hub),"NPC: travel to hub "+hub);
                game.Player.enabled=false;
                IEnumerator settle=Wait(.3f);while(settle.MoveNext())yield return settle.Current;
                for(int role=0;role<3;role++)
                {
                    string name=role==0?"Camp Merchant":role==1?"Camp Blacksmith":"Star Exchange Steward";
                    Transform npc=GameObject.Find(name).transform,body=npc.Find("NPC body pivot"),head=body.Find("NPC head pivot");
                    Transform prop=npc.Find(role==0?"Merchant stocked shelf":role==1?"Forged anvil face":"Exchange lectern");
                    Vector3 propPosition=prop.position;Quaternion propRotation=prop.rotation;
                    Quaternion idleFacing=body.rotation;
                    game.Player.Teleport(GameSession.HubNpcPosition(role)+new Vector3(.6f,0,-1.8f));
                    settle=Wait(.15f);while(settle.MoveNext())yield return settle.Current;
                    if(hub==0)Capture(body,Path.Combine(output,"npc-"+role+"-idle.png"));
                    Call(ui,"HandleHubNpcShortcut",false);
                    check(Panel(ui)=="None","NPC: held/non-press event does not start conversation");
                    Call(ui,"HandleHubNpcShortcut",true);
                    check(Panel(ui)=="HubDialogue"&&game.ActiveHubNpc==(HubNpcKind)(role+1),"NPC: G opens visible greeting for hub "+hub+", role "+role);
                    check(game.InputBlocked&&Time.timeScale==0,"NPC: conversation freezes combat");
                    settle=Wait(.75f);while(settle.MoveNext())yield return settle.Current;
                    Vector3 facing=game.Player.transform.position-body.position;facing.y=0;
                    check(Vector3.Dot(body.forward,facing.normalized)>.995f,"NPC: actor faces player while combat clock is paused");
                    check(prop.position==propPosition&&Quaternion.Angle(prop.rotation,propRotation)<.01f,"NPC: working station does not rotate with actor");
                    Quaternion firstNod=head.localRotation;
                    settle=Wait(.22f);while(settle.MoveNext())yield return settle.Current;
                    check(Quaternion.Angle(firstNod,head.localRotation)>.1f,"NPC: conversational nod continues in dialogue");
                    GameAudio owner=UnityEngine.Object.FindFirstObjectByType<GameAudio>();
                    AudioSource speech=(AudioSource)typeof(GameAudio).GetField("npcVoice",Private).GetValue(owner);
                    check(speech!=null&&speech.clip!=null&&speech.isPlaying&&speech.clip.length>1,"NPC: imported Mandarin greeting plays once");
                    if(hub==0)Capture(body,Path.Combine(output,"npc-"+role+"-greeting.png"));
                    if(hub==0&&role==0)
                    {
                        GameAudio.MasterVolume=.25f;
                        check(Mathf.Abs(speech.volume-.175f)<.001f,"NPC: speech follows master volume");
                        game.SetPaused(true);firstNod=head.localRotation;
                        settle=Wait(.2f);while(settle.MoveNext())yield return settle.Current;
                        check(Quaternion.Angle(firstNod,head.localRotation)<.01f&&!speech.isPlaying,"NPC: true pause freezes nod and stops speech");
                        game.SetPaused(false);GameAudio.MasterVolume=1;
                    }
                    Call(ui,"HandleHubNpcShortcut",true);
                    check(Panel(ui)==(role==2?"Chapter":"Inventory")&&game.ActiveHubNpc==(HubNpcKind)(role+1),"NPC: second G enters correct service without losing speaker");
                    Call(ui,"HandleHubNpcShortcut",true);
                    check(Panel(ui)==(role==2?"Chapter":"Inventory"),"NPC: G inside service does not restart or switch panel");
                    settle=Wait(.2f);while(settle.MoveNext())yield return settle.Current;
                    if(role==2){Call(ui,"OpenChapterExchange");check(Panel(ui)=="Camp","NPC: exchange remains reachable after dialogue");}
                    Call(ui,"ClosePanel");
                    check(game.ActiveHubNpc==HubNpcKind.None&&!speech.isPlaying&&!game.InputBlocked,"NPC: close ends speech and restores gameplay");
                    settle=Wait(.8f);while(settle.MoveNext())yield return settle.Current;
                    check(Quaternion.Angle(body.rotation,idleFacing)<.1f,"NPC: actor returns to working orientation");
                }
            }
            game.Player.Teleport(GameSession.HubNpcPosition(0)+Vector3.back*1.8f);
            GameAudio.Muted=true;
            Call(ui,"OpenNearbyHubNpc");
            check(game.ActiveHubNpc==HubNpcKind.Merchant,"NPC: muted greeting still has a visual dialogue");
            var audio=UnityEngine.Object.FindFirstObjectByType<GameAudio>();
            var voice=(AudioSource)typeof(GameAudio).GetField("npcVoice",Private).GetValue(audio);
            check(!voice.isPlaying,"NPC: mute suppresses speech");
            Call(ui,"ClosePanel");
            GameAudio.Muted=false;
            IEnumerator release=Wait(.5f);while(release.MoveNext())yield return release.Current;
            MobileControls.SimulationEnabled=true;
            Call(ui,"HandleHubNpcShortcut",true);
            check(Panel(ui)=="None","NPC: mobile mode ignores desktop G");
            ui.ActivateMobileInteraction();
            check(Panel(ui)=="HubDialogue"&&game.ActiveHubNpc==HubNpcKind.Merchant,"NPC: touch interaction shares greeting route");
            release=Wait(.5f);while(release.MoveNext())yield return release.Current;
            Call(ui,"OpenHubNpcService");
            check(Panel(ui)=="Inventory","NPC: touch service button opens inventory");
            Call(ui,"ClosePanel");MobileControls.SimulationEnabled=false;
            log("NPC validation covered G gating, nine hub/role combinations, real paused-clock animation, fixed props, speech, close, legacy keys and mobile routes. Six Camera.Render captures show NPCs; these captures exclude IMGUI.");
        }

        private static void Capture(Transform body,string path)
        {
            Camera camera=Camera.main;
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            RenderTexture oldTarget=camera.targetTexture,oldActive=RenderTexture.active;
            Renderer[] heroRenderers=GameSession.Instance.Player.GetComponentsInChildren<Renderer>();
            bool[] enabled=new bool[heroRenderers.Length];
            for(int i=0;i<heroRenderers.Length;i++){enabled[i]=heroRenderers[i].enabled;heroRenderers[i].enabled=false;}
            var target=new RenderTexture(640,480,24);
            var pixels=new Texture2D(640,480,TextureFormat.RGB24,false);
            try
            {
                camera.transform.position=body.position+new Vector3(2.6f,2.4f,-4.2f);
                camera.transform.LookAt(body.position+Vector3.up);
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,640,480),0,0);pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
                camera.transform.SetPositionAndRotation(position,rotation);
                for(int i=0;i<heroRenderers.Length;i++)heroRenderers[i].enabled=enabled[i];
                UnityEngine.Object.Destroy(pixels);target.Release();UnityEngine.Object.Destroy(target);
            }
        }
    }
}
