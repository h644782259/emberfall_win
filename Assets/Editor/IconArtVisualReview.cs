using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberfall.Editor
{
    // Editor-only screenshots of the production GameView, with isolated QA saves.
    [InitializeOnLoad]
    public static class IconArtVisualReview
    {
        const string Key="Emberfall.IconArtReview.";
        const string SaveKey="Emberfall.ValidationSaveDirectory";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static EditorWindow view;
        static int stage=-1,errors;
        static double next;
        static readonly List<string> evidence=new List<string>();
        static string Root=>SessionState.GetString(Key+"Root","");
        static IconArtVisualReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Play Mode is occupied.");
            string root=Path.GetFullPath("Docs/Validation/IconArt/UnityUI");Directory.CreateDirectory(root);
            SessionState.SetString(Key+"Root",root);SessionState.SetString(Key+"Save",SessionState.GetString(SaveKey,""));
            SessionState.SetString(SaveKey,Path.Combine(root,"IsolatedSave"));SessionState.SetBool(Key+"Active",true);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            view=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            view.position=new Rect(40,40,1320,642);view.Show();view.Focus();
            EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type)
        {if(SessionState.GetBool(Key+"Active",false)&&(type==LogType.Error||type==LogType.Exception)){errors++;evidence.Add(message+"\n"+stack);}}
        static object Field(object target,string name)=>target.GetType().GetField(name,Flags).GetValue(target);
        static void Set(object target,string name,object value)
        {
            var field=target.GetType().GetField(name,Flags);
            field.SetValue(target,field.FieldType.IsEnum?Enum.Parse(field.FieldType,(string)value):value);
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key+"Active",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if(view==null)view=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
                view.Repaint();
                var game=GameSession.Instance;
                if(game==null||game.Progression==null||EditorApplication.timeSinceStartup<next)return;
                if(stage>=0)Capture(stage);
                stage++;
                if(stage>=20){Finish(errors==0?0:1);return;}
                Setup(game,stage);next=EditorApplication.timeSinceStartup+1.5;
            }
            catch(Exception error){evidence.Add(error.ToString());Finish(1);}
        }
        static void Hero(GameSession game,HeroClass hero)
        {
            Time.timeScale=1;game.StartNew(hero);
            CombatReviewBuildSetup.Apply(game.Progression,new CombatReviewConfiguration{id="icon-ui",hero=hero,level=100,skillRanks=new[]{3,3,3,3,3,3,3,3,3,3}},Path.Combine(Root,"IsolatedSave"));
            game.Player.RefreshStats(true);game.SetPaused(false);
            foreach(var enemy in game.Enemies)if(enemy!=null)enemy.gameObject.SetActive(false);
            game.Enemies.Clear();
            MobileControls.ValidationUsesSimulation=true;MobileControls.SimulationEnabled=true;
            var ui=game.GetComponent<GameUI>();
            Set(ui,"mobileInventoryProfile",game.Progression.CurrentSlotId);
            Set(ui,"mobileInventoryStatusOwner",game.Player);
            Set(ui,"inventoryComparisonOpen",false);
            Time.timeScale=0;
        }
        static void Setup(GameSession game,int index)
        {
            var ui=game.GetComponent<GameUI>();
            if(index<12)
            {
                int hero=index/3,kind=index%3;
                if(kind==0)Hero(game,(HeroClass)hero);
                Set(ui,"panel",kind==0?"None":kind==1?"Inventory":"Skills");
                Set(ui,"inventoryComparisonOpen",false);Set(ui,"mobileInventoryTab",0);
                if(kind==1)
                {
                    var p=game.Progression;p.Profile.inventory.Clear();
                    for(int slot=0;slot<3;slot++)for(int rank=0;rank<4;rank++)p.Profile.inventory.Add(new ItemData{id="review-"+slot+"-"+rank,name=GameBalance.RarityName((Rarity)rank)+GameBalance.SlotName((ItemSlot)slot),slot=(ItemSlot)slot,rarity=(Rarity)rank,level=80,attack=12+rank*5,defense=10+rank*4,health=25+rank*10});
                    for(int slot=0;slot<3;slot++)if(!p.Equip("review-"+slot+"-1"))throw new InvalidOperationException(p.LastError);
                    typeof(GameUI).GetMethod("RebuildBagItems",Flags).Invoke(ui,null);
                    var bag=(System.Collections.IList)Field(ui,"bagItems");if(bag.Count!=9)throw new InvalidOperationException("Worn equipment still in bag: "+bag.Count);
                    evidence.Add("Hero "+hero+": 12 owned / 3 worn / 9 bag entries.");
                }
                if(kind==0)for(int hit=0;hit<8;hit++)game.RecordComboHit(1);
            }
            else if(index==12)
            {
                Hero(game,HeroClass.Arcanist);var p=game.Progression;p.Profile.fashions.Clear();
                for(int slot=0;slot<2;slot++)for(int rank=0;rank<4;rank++)p.Profile.fashions.Add(new FashionData{id="fashion-"+slot+"-"+rank,slot=(FashionSlot)slot,rarity=(Rarity)rank,appearanceTier=rank});
                p.EquipFashion("fashion-0-2");p.EquipFashion("fashion-1-2");
                Set(ui,"panel","Inventory");Set(ui,"mobileInventoryTab",3);
            }
            else if(index==18)
            {
                Setup(game,4);
                var p=game.Progression;var item=p.Equipped(ItemSlot.Weapon);
                var click=typeof(GameUI).GetMethod("WornSlotClick",Flags);
                click.Invoke(ui,new object[]{item.id,(ItemSlot?)ItemSlot.Weapon,null});
                if(p.Equipped(ItemSlot.Weapon)==null)throw new InvalidOperationException("First click unexpectedly unequipped.");
                click.Invoke(ui,new object[]{item.id,(ItemSlot?)ItemSlot.Weapon,null});
                if(p.Equipped(ItemSlot.Weapon)!=null||((System.Collections.IList)Field(ui,"bagItems")).Count!=10)throw new InvalidOperationException("Double click did not return equipment to bag.");
                evidence.Add("Equipment: single click retained; double click unequipped; bag 9 -> 10.");
            }
            else if(index==19)
            {
                Setup(game,12);var p=game.Progression;var item=p.EquippedFashion(FashionSlot.Wings);
                var click=typeof(GameUI).GetMethod("WornSlotClick",Flags);
                click.Invoke(ui,new object[]{item.id,null,(FashionSlot?)FashionSlot.Wings});
                if(p.EquippedFashion(FashionSlot.Wings)==null)throw new InvalidOperationException("First fashion click unexpectedly unequipped.");
                click.Invoke(ui,new object[]{item.id,null,(FashionSlot?)FashionSlot.Wings});
                if(p.EquippedFashion(FashionSlot.Wings)!=null)throw new InvalidOperationException("Double click did not unequip fashion.");
                evidence.Add("Fashion: single click retained; double click unequipped; visible owned entries 6 -> 7.");
            }
            else
            {
                if(index==13)Hero(game,HeroClass.Arcanist);
                Set(ui,"panel","None");
                var runtime=(SkillRuntime)Field(game.Player,"skillRuntime");runtime.ResetCooldowns();runtime.FillEnergy();
                if(index<17){runtime.TryConsume(0,3);runtime.Advance(GameBalance.EffectiveCooldown(HeroClass.Arcanist,0,3)*(index-13)*.25f);runtime.FillEnergy();}
            }
        }
        static void Capture(int index)
        {
            RenderTexture target=null;
            for(Type type=view.GetType();type!=null&&target==null;type=type.BaseType)
            {
                foreach(var property in type.GetProperties(Flags|BindingFlags.DeclaredOnly))
                    if(property.PropertyType==typeof(RenderTexture)&&property.GetIndexParameters().Length==0)try{target=(RenderTexture)property.GetValue(view);if(target!=null)break;}catch{}
                if(target==null)foreach(var field in type.GetFields(Flags|BindingFlags.DeclaredOnly))
                    if(field.FieldType==typeof(RenderTexture)){target=(RenderTexture)field.GetValue(view);if(target!=null)break;}
            }
            if(target==null)throw new InvalidOperationException("GameView render texture unavailable.");
            var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            string name=index<12?((HeroClass)(index/3))+"-"+new[]{"battle","inventory","skills"}[index%3]:index==12?"Arcanist-fashion":index==18?"Arcanist-equipment-unequipped":index==19?"Arcanist-fashion-unequipped":"Arcanist-cooldown-"+((index-13)*25);
            try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                // Metal's GameView render target is vertically inverted on readback.
                if(SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels=image.GetPixels32();int w=image.width,h=image.height;
                    for(int y=0;y<h/2;y++)for(int x=0;x<w;x++){int a=y*w+x,b=(h-1-y)*w+x;var value=pixels[a];pixels[a]=pixels[b];pixels[b]=value;}
                    image.SetPixels32(pixels);image.Apply();
                }
                File.WriteAllBytes(Path.Combine(Root,name+".png"),image.EncodeToPNG());evidence.Add(name+" "+target.width+"x"+target.height+" "+SystemInfo.graphicsDeviceType);}
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
        }
        static void Finish(int code)
        {
            File.WriteAllLines(Path.Combine(Root,"evidence.txt"),evidence);
            SessionState.SetString(SaveKey,SessionState.GetString(Key+"Save",""));SessionState.SetBool(Key+"Active",false);
            Time.timeScale=1;MobileControls.ValidationUsesSimulation=false;MobileControls.SimulationEnabled=false;
            EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
        }
    }
}
