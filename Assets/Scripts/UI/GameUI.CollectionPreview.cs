using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private CollectionModelPreview collectionModel;
        private readonly CollectionViewingState collectionViewing=new CollectionViewingState();
        private PlayerController collectionOwner;
        private string collectionNotice,collectionReceiptKey;
        private float collectionPreviewYaw=20;
        private void ReconcileCollectionPreview()
        {
            if(session!=null&&(session.BackgroundPaused||session.Paused)){ReleaseCollectionModel();return;}
            if(session==null || !session.HasStarted || session.IsDead || session.ModeFinished || session.DungeonSelectionOpen || session.RunChoices.AwaitingChoice || (panel!=Panel.Fashion&&panel!=Panel.Chests&&panel!=Panel.Inventory))ReleaseCollectionPreview();
            else if(collectionOwner!=session.Player){ReleaseCollectionPreview();collectionOwner=session.Player;}
        }
        private void OnDisable(){ReleaseCollectionModel();ClearRewardMoment();}
        private void OnApplicationFocus(bool focused){if(!focused)ClearRewardMoment();if(focused){if(collectionModel!=null)collectionModel.Invalidate();if(wearModel!=null)wearModel.Invalidate();}}
        private void OnApplicationPause(bool paused){if(paused)ClearRewardMoment();if(!paused){if(collectionModel!=null)collectionModel.Invalidate();if(wearModel!=null)wearModel.Invalidate();}}
        private void ReleaseCollectionModel(){if(wearModel!=null)wearModel.Dispose();wearModel=null;if(collectionModel!=null)collectionModel.Dispose();collectionModel=null;}
        private void ReleaseCollectionPreview()
        {ReleaseCollectionModel();collectionViewing.Reset();collectionOwner=null;collectionNotice=null;collectionReceiptKey=null;collectionPreviewYaw=20;}
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
            Texture image=collectionModel.RenderSafe(progression.Profile.heroClass,progression.Equipped(ItemSlot.Weapon),progression.Equipped(ItemSlot.Armor),progression.Equipped(ItemSlot.Relic),wings,weapon);
            Fill(area,new Color(.035f,.06f,.09f));if(image!=null)GUI.DrawTexture(viewport,image,ScaleMode.ScaleToFit,false);
            if(image==null&&collectionModel.LastError!=null)Text(viewport,"预览暂不可用，其他操作可继续",14,muted,false,true);
            if(controls!=null)DrawCollectionControls(controls,Vector2.zero,1);
        }
        private static Rect PreviewControlRect(MobilePanelLayout.Area area,Vector2 origin,float scale)
        {return new Rect((area.X-origin.x)*scale,(area.Y-origin.y)*scale,area.Width*scale,area.Height*scale);}
        private void DrawCollectionControls(CollectionPreviewLayout layout,Vector2 origin,float scale)
        {
            for(int i=0;i<3;i++)if(TabButton(PreviewControlRect(layout.Button(0,i),origin,scale), i==0?"全身":i==1?"武器":"后背", (int)collectionViewing.Mode==i)){collectionViewing.View((CollectionPreviewComposition)i);BlockUITransition();}
            for(int i=0;i<3;i++)if(TabButton(PreviewControlRect(layout.Button(1,i),origin,scale), i==0?"待机":i==1?"攻击":"施法", (int)collectionModel.PreviewAction==i))collectionModel.Play((CollectionPreviewAction)i);
            if(Button(PreviewControlRect(layout.Button(2,0),origin,scale),"左转",jade))collectionViewing.Rotate(-45);
            if(Button(PreviewControlRect(layout.Button(2,1),origin,scale),"右转",jade))collectionViewing.Rotate(45);
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

        private bool DrawChestRewardModel(Rect area,ChestReward reward)
        {
            var trial=EquipmentComparisonPresentation.Receipt(reward);if(trial==null)return false;
            if(collectionReceiptKey!=reward.Id){collectionReceiptKey=reward.Id;SetCollectionAngle(trial.slot);}
            DrawCollectionModel(area,trial,false);return true;
        }
    }
}
