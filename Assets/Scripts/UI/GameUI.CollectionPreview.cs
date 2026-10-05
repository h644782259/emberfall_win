using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private CollectionModelPreview collectionModel;
        private FashionData collectionTrial;
        private readonly CollectionViewingState collectionViewing=new CollectionViewingState();
        private PlayerController collectionOwner;
        private int collectionSlot;
        private bool mobileFashionPreview=true;
        private Vector2 mobilePreviewTextScroll;
        private string collectionNotice,collectionReceiptKey;
        private float collectionPreviewYaw=20;
        private void ReconcileCollectionPreview()
        {
            if(session!=null&&(session.BackgroundPaused||session.Paused)){ReleaseCollectionModel();return;}
            if(session==null || !session.HasStarted || session.IsDead || session.ModeFinished || session.DungeonSelectionOpen || session.RunChoices.AwaitingChoice || (panel!=Panel.Fashion&&panel!=Panel.Chests&&!(panel==Panel.Inventory&&equipmentAppearanceOpen)))ReleaseCollectionPreview();
            else if(collectionOwner!=session.Player){ReleaseCollectionPreview();collectionOwner=session.Player;}
        }
        private void OnDisable(){ReleaseCollectionModel();}
        private void OnApplicationFocus(bool focused){if(focused&&collectionModel!=null)collectionModel.Invalidate();}
        private void OnApplicationPause(bool paused){if(!paused&&collectionModel!=null)collectionModel.Invalidate();}
        private void ReleaseCollectionModel(){if(collectionModel!=null)collectionModel.Dispose();collectionModel=null;}
        private void ReleaseCollectionPreview()
        {ReleaseCollectionModel();collectionViewing.Reset();collectionTrial=null;collectionOwner=null;collectionNotice=null;collectionReceiptKey=null;collectionPreviewYaw=20;mobileFashionPreview=true;equipmentAppearanceOpen=false;equipmentAppearanceCandidate=false;equipmentAppearanceItem=null;}
        private void TrialFashion(FashionSlot slot,Rarity rarity)
        {collectionTrial=EquipmentComparisonPresentation.Trial(slot,rarity);collectionNotice=null;mobileFashionStatus=null;mobileFashionFailed=false;collectionViewing.TryOn(collectionTrial);}
        private void SetCollectionAngle(FashionSlot slot)
        {collectionPreviewYaw=slot==FashionSlot.Wings?160:20;if(collectionModel!=null)collectionModel.SetYaw(collectionPreviewYaw);}
        private void DrawCollectionModel(Rect area,FashionData trial,bool rotate)
        {
            var progression=session.Progression;
            if(collectionModel==null){collectionModel=new CollectionModelPreview();collectionModel.SetYaw(collectionPreviewYaw);}
            FashionData wings=progression.EquippedFashion(FashionSlot.Wings),weapon=progression.EquippedFashion(FashionSlot.Weapon);
            if(rotate){wings=collectionViewing.Trial(FashionSlot.Wings)??wings;weapon=collectionViewing.Trial(FashionSlot.Weapon)??weapon;}
            else if(trial!=null){if(trial.slot==FashionSlot.Wings)wings=trial;else weapon=trial;}
            CollectionPreviewComposition composition=rotate?collectionViewing.Mode:trial==null?CollectionPreviewComposition.Full:trial.slot==FashionSlot.Wings?CollectionPreviewComposition.Back:CollectionPreviewComposition.Weapon;
            collectionModel.SetEquipmentFraming(false,false);
            collectionModel.SetComposition(composition);
            collectionModel.SetYaw(rotate?collectionViewing.Yaw:collectionPreviewYaw);
            CollectionPreviewLayout controls=rotate&&!MobileControls.Active?CollectionPreviewLayout.Desktop(new MobilePanelLayout.Area(area.x,area.y,area.width,area.height)):null;
            Rect viewport=controls==null?area:PreviewControlRect(controls.Model,Vector2.zero,1);
            collectionModel.SetViewport(viewport.width*Mathf.Abs(GUI.matrix.m00),viewport.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
            Texture image=collectionModel.Render(progression.Profile.heroClass,progression.Equipped(ItemSlot.Weapon),progression.Equipped(ItemSlot.Armor),progression.Equipped(ItemSlot.Relic),wings,weapon);
            Fill(area,new Color(.035f,.06f,.09f));if(image!=null)GUI.DrawTexture(viewport,image,ScaleMode.ScaleToFit,false);
            if(controls!=null)DrawCollectionControls(controls,Vector2.zero,1);
        }
        private static Rect PreviewControlRect(MobilePanelLayout.Area area,Vector2 origin,float scale)
        {return new Rect((area.X-origin.x)*scale,(area.Y-origin.y)*scale,area.Width*scale,area.Height*scale);}
        private void DrawCollectionControls(CollectionPreviewLayout layout,Vector2 origin,float scale)
        {
            for(int i=0;i<3;i++)if(Button(PreviewControlRect(layout.Button(0,i),origin,scale),i==0?"全身":i==1?"武器":"后背",(int)collectionViewing.Mode==i?gold:jade)){collectionViewing.View((CollectionPreviewComposition)i);BlockUITransition();}
            for(int i=0;i<3;i++)if(Button(PreviewControlRect(layout.Button(1,i),origin,scale),i==0?"待机":i==1?"攻击":"施法",(int)collectionModel.PreviewAction==i?gold:jade))collectionModel.Play((CollectionPreviewAction)i);
            if(Button(PreviewControlRect(layout.Button(2,0),origin,scale),"左转",jade))collectionViewing.Rotate(-45);
            if(Button(PreviewControlRect(layout.Button(2,1),origin,scale),"右转",jade))collectionViewing.Rotate(45);
        }
        private string CollectionTrialTitle
        {get{return collectionTrial==null?"当前外观":ProgressionService.FashionName(collectionTrial.slot,collectionTrial.rarity)+" · 试穿";}}
        private string CollectionTrialState
        {
            get
            {
                if(collectionTrial==null)return "预览不会改变穿戴或属性";
                var p=session.Progression;var worn=p.EquippedFashion(collectionTrial.slot);var strongest=p.StrongestFashion(collectionTrial.slot);
                return (collectionViewing.Trial(FashionSlot.Wings)!=null&&collectionViewing.Trial(FashionSlot.Weapon)!=null?"两部位试穿均保留 · ":"")+EquipmentComparisonPresentation.CollectionState(p.Profile.fashions.Exists(x=>x!=null&&x.id==collectionTrial.id),worn!=null&&worn.id==collectionTrial.id,strongest!=null&&strongest.id==collectionTrial.id);
            }
        }
        private float threadExchangeNextClick;
        private void ExchangeThreadMaterial()
        {
            if(Time.unscaledTime<threadExchangeNextClick)return;
            threadExchangeNextClick=Time.unscaledTime+.35f;
            var p=session.Progression;bool saved=p.ExchangeThreadsForMaterial(p.QuoteThreadMaterialExchange(),session.IsInCamp);
            collectionNotice=saved?"星纹 −6 · 星烬碎片 +1":p.LastError;
            if(MobileControls.Active)MobileFashionResult(saved,collectionNotice);else Feedback(saved,collectionNotice);
            BlockUITransition();
        }

        private void DrawDesktopCollection()
        {
            var p=session.Progression;var profile=p.Profile;
            Rect w=Modal(940,638,"时装收藏","试穿与穿戴独立 · 属性来自每部位最高收藏");
            if(Button(new Rect(w.xMax-69,w.y+20,44,32),"×",jade)){panel=Panel.Inventory;return;}
            Rect stage=new Rect(w.x+24,w.y+110,270,365);DrawCollectionModel(stage,collectionTrial,true);
            Text(new Rect(stage.x,stage.yMax+10,270,26),CollectionTrialTitle,16,pale,true,true);
            Text(new Rect(stage.x,stage.yMax+40,270,36),CollectionTrialState,12,jade,false,true);
            if(Button(new Rect(stage.x,stage.yMax+83,270,36),"恢复当前外观",muted)){collectionTrial=null;collectionViewing.Restore();collectionNotice=null;}
            float x=w.x+314,width=602;
            for(int i=0;i<2;i++)if(Button(new Rect(x+i*305,w.y+110,297,34),i==0?"翅膀":"武器外观",collectionSlot==i?gold:jade))collectionSlot=i;
            var slot=(FashionSlot)collectionSlot;var worn=p.EquippedFashion(slot);var strongest=p.StrongestFashion(slot);
            Text(new Rect(x,w.y+153,width,20),"穿戴 · "+(worn==null?"无":worn.name),13,pale,true);
            Text(new Rect(x,w.y+177,width,32),"属性来源 · "+(strongest==null?"尚无收藏":strongest.name+" · "+ProgressionService.FashionBonus(slot,strongest.rarity)),12,jade,false,true);
            for(int rank=0;rank<4;rank++)
            {
                Rarity rarity=(Rarity)rank;string id="fashion-"+collectionSlot+"-"+rank;
                bool owned=profile.fashions.Exists(f=>f!=null&&f.id==id),equipped=worn!=null&&worn.id==id,source=strongest!=null&&strongest.id==id;
                Rect row=new Rect(x,w.y+213+rank*80,width,72);Fill(row,card);Color accent=GameBalance.RarityColor(rarity);
                Fill(new Rect(row.x,row.y,3,row.height),accent);
                Text(new Rect(x+12,row.y+8,350,22),ProgressionService.FashionName(slot,rarity),16,accent,true);
                Text(new Rect(x+12,row.y+32,350,17),ProgressionService.FashionBonus(slot,rarity),12,pale);
                Text(new Rect(x+12,row.y+51,350,17),EquipmentComparisonPresentation.CollectionState(owned,equipped,source),11,owned?jade:muted);
                if(Button(new Rect(row.xMax-212,row.y+17,96,38),"试穿",jade))TrialFashion(slot,rarity);
                if(Button(new Rect(row.xMax-108,row.y+17,96,38),equipped?"已穿戴":"穿戴",accent,owned&&!equipped))
                {bool accepted=p.EquipFashion(id);collectionNotice=accepted?"外观已穿戴 · 属性来源保持最高收藏":p.LastError;Feedback(accepted,collectionNotice);}
            }
            if(Button(new Rect(x,w.y+547,124,33),"卸下外观",muted,worn!=null)){bool accepted=p.UnequipFashion(slot);collectionNotice=accepted?"已卸下 · 收藏属性保留":p.LastError;Feedback(accepted,collectionNotice);}
            Text(new Rect(x+136,w.y+549,width-136,36),collectionNotice??("星纹 "+profile.fashionThreads+" · 未获得的外观也可试穿"),12,muted,false,true);
            if(Button(new Rect(stage.x,stage.yMax+122,270,36),"6星纹 → 1星烬碎片",jade,string.IsNullOrEmpty(p.ThreadMaterialExchangeLockReason(session.IsInCamp))))ExchangeThreadMaterial();
            bool ownedLegendary=profile.fashions.Exists(f=>f!=null&&f.slot==slot&&f.rarity==Rarity.Legendary);
            if(Button(new Rect(x,w.y+592,382,30),"30星纹 · 自选传说"+(slot==FashionSlot.Wings?"翅膀":"兵装"),gold,session.IsInCamp&&!ownedLegendary&&profile.fashionThreads>=ProgressionService.FashionChoiceCost))
            {bool accepted=p.ChooseLegendaryFashion(slot,session.IsInCamp);collectionNotice=accepted?"传说收藏已解锁":p.LastError;Feedback(accepted,collectionNotice);}
            if(Button(new Rect(x+394,w.y+592,208,30),"返回行囊",jade))panel=Panel.Inventory;
        }
        private void DrawMobileCollectionPreview(MobilePanelLayout layout)
        {
            var progression=session.Progression;
            var previewLayout=CollectionPreviewLayout.Mobile(layout.BodyLeft,layout.BodyRight);
            DrawCollectionModel(MobilePanelRect(previewLayout.Model),collectionTrial,true);
            string source="";
            for(int i=0;i<2;i++)
            {
                var slot=(FashionSlot)i;var best=progression.StrongestFashion(slot);
                source+=(i==0?"翅膀":"武器")+"属性来源 · "+(best==null?"无":best.name+"\n"+ProgressionService.FashionBonus(slot,best.rarity))+"\n";
            }
            string detail=(string.IsNullOrEmpty(mobileFashionStatus)?"":mobileFashionStatus+"\n\n")+CollectionTrialTitle+"\n"+CollectionTrialState+"\n\n"+source;
            float width=layout.BodyRight.Width-26,detailTop=previewLayout.ControlsBottom-layout.BodyRight.Y+16;
            float total=detailTop+MeasureMobileParagraph(detail,width,14)+76;
            mobilePreviewTextScroll=BeginTouchScroll("mobile-fashion-preview",MobilePanelRect(layout.BodyRight),mobilePreviewTextScroll,
                new Rect(0,0,(width+10)*TouchRatio,Mathf.Max(layout.BodyRight.Height,total)*TouchRatio));
            DrawCollectionControls(previewLayout,new Vector2(layout.BodyRight.X,layout.BodyRight.Y),TouchRatio);
            float textEnd=DrawMobileParagraph(8,detailTop,width,detail,14,pale)+16;
            if(Button(TouchRect(8,textEnd,width,48),"恢复当前外观",muted)){collectionTrial=null;collectionViewing.Restore();collectionNotice=null;BlockUITransition();}
            EndTouchScroll();
            if(Button(MobilePanelRect(layout.FooterButton(0,3)),"浏览收藏",jade)){mobileFashionPreview=false;BlockUITransition();return;}
            bool owned=collectionTrial!=null&&progression.Profile.fashions.Exists(f=>f!=null&&f.id==collectionTrial.id);
            var worn=collectionTrial==null?null:progression.EquippedFashion(collectionTrial.slot);
            bool current=worn!=null&&worn.id==collectionTrial.id;
            if(Button(MobilePanelRect(layout.FooterButton(1,3)),current?"已穿戴":owned?"穿戴试穿外观":"仅试穿 · 未获得",gold,owned&&!current))
            {MobileFashionResult(progression.EquipFashion(collectionTrial.id),"外观已穿戴");BlockUITransition();return;}
            if(Button(MobilePanelRect(layout.FooterButton(2,3)),"返回行囊",jade)){panel=Panel.Inventory;BlockUITransition();}
        }
        private bool DrawChestRewardModel(Rect area,ChestReward reward)
        {
            var trial=EquipmentComparisonPresentation.Receipt(reward);if(trial==null)return false;
            if(collectionReceiptKey!=reward.Id){collectionReceiptKey=reward.Id;SetCollectionAngle(trial.slot);}
            DrawCollectionModel(area,trial,false);return true;
        }
    }
}
