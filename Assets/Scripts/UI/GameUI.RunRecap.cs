using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 recapScroll;
        private string recapError;
        private bool recapGenerationDetailsExpanded;
        private RunRecapSnapshot renderedRecap;
        private RunRecapPresentation recapPresentation;

        private ChestReward settlementChest;
        private PlayerController settlementOwner;
        private string settlementSlot;
        private int settlementEpoch=-1;
        private bool SettlementRewardsPending {get{return session.DungeonRewardPending||session.ModeRewardPending||session.ChapterRewardPending||session.Progression.Profile.pendingFashionChest||session.Progression.Profile.pendingChestReveal;}}
        private void RefreshSettlementChest()
        {
            if(settlementOwner!=session.Player||settlementSlot!=session.Progression.CurrentSlotId||settlementEpoch!=(session.Player==null?-1:session.Player.CombatEpoch))
            {settlementChest=null;settlementOwner=session.Player;settlementSlot=session.Progression.CurrentSlotId;settlementEpoch=session.Player==null?-1:session.Player.CombatEpoch;}
            if(session.Progression.Profile.pendingChestReveal)settlementChest=session.Progression.LastChestReward;
        }
        private void CollectSettlementRewards(int selected=-1)
        {
            var p=session.Progression;
            if(!p.Profile.pendingFashionChest&&!p.Profile.pendingChestReveal)
            {
                if(session.ChapterRewardPending&&!session.TrySettleChapterReward())return;
                if(session.DungeonRewardPending&&!session.TrySettleDungeonReward())return;
                if(session.ModeRewardPending&&!session.TrySettleArenaReward())return;
            }
            if(selected<0||!p.Profile.pendingFashionChest||p.Profile.pendingChestReveal)return;
            string result=p.OpenChosenDungeonChest(selected);
            if(result==null)return;
            settlementChest=p.LastChestReward;revealedChest=selected;chestRevealResult=result;
            chestReceiptId=settlementChest.Id;chestRevealedAt=Time.unscaledTime;rewardSoundPlayed=false;
            GameAudio.Play(SoundCue.Cast);
        }
        private void CloseSettlement()
        {
            // A failed receipt acknowledgement keeps the durable receipt available for retry.
            if(session.Progression.Profile.pendingChestReveal)session.Progression.AcknowledgeChestReward();
            session.DismissFinishedResult();
            dismissedChestOwner=session.Progression;dismissedChestSlot=session.Progression.CurrentSlotId;
            dismissedChestId=session.Progression.Profile.pendingChestQualificationId;
            panel=Panel.None;session.SetUIBlocking(false);BlockUITransition();
        }
        private void DrawVictoryTransition()
        {
            float u=MobileControls.Active?TouchRatio:1;
            Rect r=new Rect(width*.5f-160*u,height*.23f,320*u,62*u);
            Text(new Rect(r.x,r.y,r.width,38*u),"首领已击败",Mathf.RoundToInt(24*u),gold,true,false,TextAnchor.MiddleCenter);
            Text(new Rect(r.x,r.y+36*u,r.width,22*u),"通关奖励即将揭晓",Mathf.RoundToInt(12*u),pale,false,false,TextAnchor.MiddleCenter);
            float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*4);
            Fill(new Rect(r.center.x-(60+24*pulse)*u,r.yMax,(120+48*pulse)*u,2*u),new Color(gold.r,gold.g,gold.b,.4f+.4f*pulse));
        }

        private bool DrawVictorySettlement()
        {
            RefreshSettlementChest();
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            float fw=Mathf.Min(width-24*u,1020*u),fh=Mathf.Min(height-24*u,620*u);
            Rect frame=new Rect((width-fw)*.5f,(height-fh)*.5f,fw,fh);
            Box(frame,gold);blockedRects.Add(frame);
            if(PopupCloseButton(new Rect(frame.xMax-48*u,frame.y+6*u,40*u,36*u)))return true;
            Text(new Rect(frame.x+16*u,frame.y+8*u,fw-80*u,32*u),session.LastRunRecap!=null&&!session.LastRunRecap.Won?"挑战结束":"结算与奖励",Mathf.RoundToInt(22*u),gold,true);
            var snapshot=session.LastRunRecap;
            var data=snapshot==null?null:new RunRecapPresentation(snapshot);
            bool spacious=fh>=460*u;
            float contentWidth=fw-32*u,top=frame.y+48*u,metricHeight=(spacious?80:48)*u;
            if(data!=null)
            {
                float cw=(contentWidth-16*u)/5;
                for(int i=0;i<data.Metrics.Length&&i<5;i++)
                {
                    var metric=data.Metrics[i];Rect tile=new Rect(frame.x+16*u+i*(cw+4*u),top,cw,metricHeight);
                    Fill(tile,card);DrawIcon(new Rect(tile.x+5*u,tile.y+6*u,(spacious?24:16)*u,(spacious?24:16)*u),UIIconAtlas.Utility(RunRecapPresentation.IconFor(metric.Key)),jade);
                    Text(new Rect(tile.x+(spacious?34:23)*u,tile.y+2*u,tile.width-(spacious?38:26)*u,(spacious?43:23)*u),metric.Value.ToString("N0"),Mathf.RoundToInt((spacious?24:15)*u),pale,true,false,TextAnchor.MiddleRight);
                    Text(new Rect(tile.x+3*u,tile.y+(spacious?50:26)*u,tile.width-6*u,22*u),metric.Key,Mathf.RoundToInt((spacious?13:10)*u),muted,false,false,TextAnchor.MiddleCenter);
                }
            }
            top+=metricHeight+6*u;
            long coins=(snapshot==null?0:snapshot.RewardGold)+(settlementChest==null?0:settlementChest.hasCurrencyDeltas?settlementChest.goldDelta:settlementChest.Gold);
            long xp=snapshot==null?0:snapshot.RewardExperience;
            long shards=(snapshot==null?0:snapshot.RewardMaterials)+(settlementChest!=null&&settlementChest.rulesRevision<3&&settlementChest.materialKind==RewardMaterialKind.StarAshFragment?settlementChest.materialsDelta:0);
            long stones=snapshot==null?0:snapshot.RewardRefinementStones;
            long[] amounts={coins,xp,shards,stones,settlementChest==null||settlementChest.rulesRevision>=3?0:settlementChest.threadsDelta};
            string[] labels={"金币","经验","碎片","洗练石","星纹"},icons={"coin","upgrade","shard","gem","core"};
            int resourceCount=settlementChest==null||settlementChest.rulesRevision>=3?5:2;
            float rw=contentWidth/resourceCount,resourceHeight=(spacious?68:40)*u;
            for(int i=0;i<resourceCount;i++)
            {
                Rect r=new Rect(frame.x+16*u+i*rw,top,rw-4*u,resourceHeight);
                Fill(r,new Color(.055f,.095f,.12f));
                float iconSize=(spacious?32:20)*u;
                DrawIcon(new Rect(r.x+5*u,r.center.y-iconSize*.5f,iconSize,iconSize),UIIconAtlas.Utility(icons[i]),i==0?gold:i==1?jade:GameBalance.RarityColor(i==3?Rarity.Epic:Rarity.Rare));
                float textX=r.x+iconSize+10*u,textWidth=r.xMax-textX-4*u;
                Text(new Rect(textX,r.y+3*u,textWidth,resourceHeight*.42f),labels[i],Mathf.RoundToInt((spacious?12:9)*u),muted,false,false,TextAnchor.MiddleLeft);
                Text(new Rect(textX,r.y+resourceHeight*.43f,textWidth,resourceHeight*.52f),"+"+amounts[i],Mathf.RoundToInt((spacious?19:12)*u),pale,true,false,TextAnchor.MiddleLeft);
            }
            top+=resourceHeight+10*u;
            Rect stage=new Rect(frame.x+16*u,top,contentWidth,Mathf.Max(80*u,frame.yMax-top-12*u));
            if(p.Profile.pendingChestReveal&&settlementChest!=null)
            {
                if(chestReceiptId!=settlementChest.Id)ResetChestReveal();
                DrawSettlementChestStage(stage,settlementChest,u);
            }
            else if(settlementChest!=null)DrawSettlementChestStage(stage,settlementChest,u);
            else if(p.Profile.pendingFashionChest)
            {
                Text(new Rect(stage.x,stage.y,stage.width,24*u),"选择你的通关宝箱",Mathf.RoundToInt(15*u),gold,true,false,TextAnchor.MiddleCenter);
                float cw=(stage.width-16*u)/3;int locked=p.SelectedRewardChest;
                for(int i=0;i<3;i++)
                {
                    Rect tile=new Rect(stage.x+i*(cw+8*u),stage.y+28*u,cw,stage.height-28*u);
                    Rect art=new Rect(tile.x+4*u,tile.y,tile.width-8*u,Mathf.Max(36*u,tile.height));
                    if(ChestOpenButton(art,u,locked<0||locked==i))
                    {chestRevealOrigin=art;CollectSettlementRewards(i);}
                }
            }
            else if(SettlementRewardsPending)
            {
                Text(new Rect(stage.x,stage.y,stage.width,40*u),"奖励尚未保存，请重试。",Mathf.RoundToInt(14*u),gold,false,true,TextAnchor.MiddleCenter);
                if(Button(new Rect(stage.center.x-90*u,stage.center.y,180*u,40*u),"重试领取",gold))CollectSettlementRewards();
            }
            else Text(stage,"本次奖励已收下",Mathf.RoundToInt(16*u),jade,true,false,TextAnchor.MiddleCenter);
            if(!string.IsNullOrEmpty(p.LastError))Text(new Rect(stage.x,stage.yMax-24*u,stage.width,24*u),"保存暂未完成，可重试或关闭后继续游戏",Mathf.RoundToInt(11*u),gold,false,false,TextAnchor.MiddleCenter);
            return false;
        }
        private void DrawSettlementChestStage(Rect stage,ChestReward reward,float u)
        {
            bool animating=chestReceiptId==reward.Id&&!ChestAnimationDone;
            float progress=animating?ChestRevealPresentation.Progress(Time.unscaledTime-chestRevealedAt,ChestDuration):1;
            float badgeSize=Mathf.Min(180*u,Mathf.Min(stage.height*.8f,stage.width*.24f));
            Rect badge=new Rect(stage.x,stage.center.y-badgeSize*.5f,badgeSize,badgeSize);
            float heroSize=Mathf.Min(stage.height,stage.width*.52f);
            Rect hero=new Rect(stage.center.x-heroSize*.5f,stage.center.y-heroSize*.5f,heroSize,heroSize);
            float dock=Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.45f)/.32f));
            Rect art=new Rect(Mathf.Lerp(hero.x,badge.x,dock),Mathf.Lerp(hero.y,badge.y,dock),Mathf.Lerp(hero.width,badge.width,dock),Mathf.Lerp(hero.height,badge.height,dock));
            if(animating&&progress>.18f&&progress<.8f)
            {GUI.BeginGroup(art);DrawRewardRadiance(new Rect(0,0,art.width,art.height),gold,progress);GUI.EndGroup();}
            DrawRewardChest(art,true,1,progress);
            if(progress<.55f)return;
            Rect rewardsArea=new Rect(badge.xMax+16*u,stage.y+8*u,Mathf.Max(1,stage.xMax-badge.xMax-24*u),stage.height-16*u);
            Fill(new Rect(badge.xMax+6*u,stage.y+stage.height*.15f,u,stage.height*.7f),new Color(gold.r,gold.g,gold.b,.25f));
            var icons=new System.Collections.Generic.List<EntryRewardPreview>();
            if(reward.equipmentIds!=null)foreach(string id in reward.equipmentIds)
            {var item=session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==id);if(item!=null)icons.Add(ActualEquipmentPreview(item));}
            if(reward.Rarity.HasValue&&reward.Slot.HasValue&&!reward.Duplicate)
                icons.Add(new EntryRewardPreview{Key="fashion:"+reward.Id,AppearanceSlot=reward.Slot.Value,Name=reward.Name,Rarity=reward.Rarity.Value,Tint=GameBalance.RarityColor(reward.Rarity.Value),Icon=UIIconAtlas.FashionCardIcon(reward.Slot.Value,(int)reward.Rarity.Value,session.Progression.Profile.heroClass),Description=ProgressionService.FashionBonus(reward.Slot.Value,reward.Rarity.Value)});
            if(reward.gemMechanic!=EquipmentMechanic.None&&!reward.duplicateGem)
                icons.Add(new EntryRewardPreview{Key="gem:"+reward.Id,Name=BuildCatalog.GemName(reward.gemMechanic),Rarity=reward.gemRarity,Tint=GameBalance.RarityColor(reward.gemRarity),Icon=UIIconAtlas.Utility("gem"),Description=GemRewardDescription(reward.gemMechanic,reward.gemRarity)});
            var snapshot=session.LastRunRecap;
            int[] counts={reward.materialsDelta+(reward.rulesRevision>=3||snapshot==null?0:snapshot.RewardMaterials),reward.rulesRevision>=3?reward.refinementStonesDelta:snapshot==null?0:snapshot.RewardRefinementStones,reward.threadsDelta};
            string[] keys={"shard","refinement","thread"},names={"星烬碎片","装备洗练石","星纹"};
            for(int n=0;n<counts.Length;n++)if(counts[n]>0)icons.Add(new EntryRewardPreview{Key=keys[n],Name=names[n],Quantity=counts[n],Description=names[n]+"\n数量  "+counts[n],Icon=n==1?UIIconAtlas.Utility("gem"):UIIconAtlas.Reward(n==0?1:2),Rarity=n==1?Rarity.Epic:Rarity.Rare,Tint=n==1?GameBalance.RarityColor(Rarity.Epic):jade});
            if(icons.Count==0)return;
            float gap=12*u,caption=0,cell=0;int cols=1,rows=icons.Count;
            for(int candidate=1;candidate<=icons.Count;candidate++)
            {
                int candidateRows=(icons.Count+candidate-1)/candidate;
                float size=Mathf.Min(60*u,Mathf.Min((rewardsArea.width-gap*(candidate-1))/candidate,(rewardsArea.height-gap*(candidateRows-1))/candidateRows-caption));
                if(size>cell){cell=size;cols=candidate;rows=candidateRows;}
            }
            cell=Mathf.Max(1,cell);
            float top=rewardsArea.center.y-(rows*(cell+caption)+(rows-1)*gap)*.5f;
            for(int i=0;i<icons.Count;i++)
            {
                float reveal=animating?Mathf.Clamp01((progress-.55f-i*.035f)/.16f):1;
                if(reveal<=0)continue;
                int rowCount=Mathf.Min(cols,icons.Count-(i/cols)*cols);
                float rowLeft=rewardsArea.center.x-(rowCount*cell+(rowCount-1)*gap)*.5f;
                Rect icon=new Rect(rowLeft+(i%cols)*(cell+gap),top+(i/cols)*(cell+caption+gap),cell,cell);
                Color before=GUI.color;GUI.color=new Color(before.r,before.g,before.b,before.a*reveal);
                Fill(icon,card);Border(icon,icons[i].QualityColor,2*u);
                DrawIcon(new Rect(icon.x+5*u,icon.y+5*u,icon.width-10*u,icon.height-10*u),icons[i].Icon,icons[i].QualityColor);
                if(icons[i].Quantity>1)Text(new Rect(icon.x+2*u,icon.yMax-18*u,icon.width-5*u,16*u),icons[i].Quantity.ToString(),Mathf.RoundToInt(11*u),pale,true,false,TextAnchor.MiddleRight);
                if(caption>0)Text(new Rect(icon.x,icon.yMax+4*u,icon.width,caption-4*u),icons[i].Name,Mathf.RoundToInt(12*u),icons[i].QualityColor,true,true,TextAnchor.MiddleCenter);
                GUI.color=before;
                InspectRewardItem(icon,icons[i]);
            }

        }

        // Returns only the explicit primary action. Respawn/navigation stays with the caller.
        private bool DrawStructuredRunRecap(bool death)
        {
            if(!death)return DrawVictorySettlement();
            RefreshSettlementChest();
            bool mobile=MobileControls.Active;
            float unit=mobile?TouchRatio:1f;
            RunRecapLayout layout=new RunRecapLayout(width/unit,height/unit,mobile);
            RunRecapSnapshot snapshot=session.LastRunRecap;
            if(!object.ReferenceEquals(renderedRecap,snapshot))
            {
                renderedRecap=snapshot; recapScroll=Vector2.zero;recapGenerationDetailsExpanded=false;
                recapPresentation=snapshot==null?null:new RunRecapPresentation(snapshot);
            }
            string latestError=string.IsNullOrEmpty(session.Progression.LastError)?null:"保存暂未完成，请重试；已获得的奖励不会重复发放。";
            if(recapError!=latestError)
            {recapError=latestError;if(!string.IsNullOrEmpty(recapError))recapScroll=Vector2.zero;}
            RunRecapPresentation data=recapPresentation;
            Color accent=snapshot!=null&&snapshot.Won?gold:jade;
            Rect frame=RecapRect(layout.Frame,unit),header=RecapRect(layout.Header,unit);

            Box(frame,accent);Fill(new Rect(frame.x,frame.y,4*unit,frame.height),accent);
            if(!death&&PopupCloseButton(new Rect(frame.xMax-48*unit,frame.y+8*unit,40*unit,36*unit)))return true;
            float iconSize=mobile?35:44;
            DrawIcon(new Rect(header.x,header.y+2*unit,iconSize*unit,iconSize*unit),UIIconAtlas.Utility(snapshot!=null&&snapshot.Won?"confirm":"skills"),accent);
            string title=snapshot==null?"战斗复盘":snapshot.Won?(snapshot.ModeName.Length>0?"挑战完成":"遗迹通关"):"本次止步";
            Text(new Rect(header.x+(iconSize+12)*unit,header.y,header.width-(iconSize+126)*unit,32*unit),title,Mathf.RoundToInt((mobile?23:28)*unit),pale,true);
            string location=snapshot==null?"":snapshot.InDungeon?(snapshot.ModeName.Length>0?snapshot.ModeName+"  ·  ":"")+"Lv"+AdventureRewardRules.DungeonLevel(snapshot.Tier)+"  ·  "+snapshot.Wave+" / "+snapshot.TotalWaves+(snapshot.ModeName.Length>0?" 阶段":" 波"):"原野探索";
            Text(new Rect(header.x+(iconSize+12)*unit,header.y+34*unit,header.width-(iconSize+20)*unit,22*unit),location,Mathf.RoundToInt(13*unit),muted);
            Rule(frame.x+16*unit,frame.y+(mobile?61:78)*unit,frame.width-32*unit,accent*.5f);
            Rect viewport=RecapRect(layout.Viewport,unit);
            float statusHeight=RecapStatusHeight(layout,unit);
            float rewardHeight=settlementChest==null?0:DrawChestRewardContents(layout.ContentWidth,unit,settlementChest,null,false,false)+36;
            float pickupHeight=0;
            float choiceHeight=session.Progression.Profile.pendingFashionChest?180:0;
            float contentHeight=RecapContentHeight(layout,data)+statusHeight+rewardHeight+pickupHeight+choiceHeight;
            recapScroll=BeginTouchScroll("recap",viewport,recapScroll,new Rect(0,0,layout.ContentWidth*unit,Mathf.Max(viewport.height,contentHeight*unit)));
            if(statusHeight>0)
            {
                Fill(new Rect(0,0,layout.ContentWidth*unit,(statusHeight-10)*unit),new Color(.17f,.095f,.065f));
                Text(new Rect(12*unit,10*unit,(layout.ContentWidth-24)*unit,(statusHeight-30)*unit),recapError,Mathf.RoundToInt(14*unit),gold,false,true);
            }
            if(data==null)
                Text(new Rect(12*unit,(24+statusHeight)*unit,(layout.ContentWidth-24)*unit,56*unit),"暂无战斗记录",Mathf.RoundToInt(22*unit),muted,true,false,TextAnchor.MiddleCenter);
            else DrawRecapCards(layout,data,unit,statusHeight);
            if(settlementChest!=null)
            {
                float rewardTop=(RecapContentHeight(layout,data)+statusHeight)*unit;
                GUI.BeginGroup(new Rect(0,rewardTop,layout.ContentWidth*unit,rewardHeight*unit));
                DrawChestRewardContents(layout.ContentWidth,unit,settlementChest,null,true,false);
                GUI.EndGroup();
            }
            if(choiceHeight>0)
            {
                float y=(RecapContentHeight(layout,data)+statusHeight+rewardHeight+pickupHeight)*unit;
                Text(new Rect(8*unit,y,(layout.ContentWidth-16)*unit,28*unit),"选择一个通关宝箱",Mathf.RoundToInt(16*unit),gold,true);
                float cell=(layout.ContentWidth-24)/3;
                int locked=session.Progression.SelectedRewardChest;
                for(int choice=0;choice<3;choice++)
                {
                    Rect tile=new Rect((choice*(cell+8)+4)*unit,y+32*unit,cell*unit,136*unit);
                    Fill(tile,card);Border(tile,locked==choice?gold:jade);
                    if(ChestOpenButton(new Rect(tile.x+8*unit,tile.y+4*unit,tile.width-16*unit,tile.height-8*unit),unit,locked<0||locked==choice))CollectSettlementRewards(choice);
                }
            }
            EndTouchScroll();
            Rect primary=RecapRect(layout.Primary,unit);
            if(death)
            {
                float menuWidth=Mathf.Min(152*unit,primary.width*.35f);
                if(NavigationButton(new Rect(primary.x,primary.y,menuWidth,primary.height), "菜单 / 存档", jade))session.SetPaused(true);
                primary.x+=menuWidth+12*unit;primary.width-=menuWidth+12*unit;
            }

            if(!death&&session.InDungeon&&SettlementRewardsPending)
            {
                float claimWidth=primary.width*.48f;
                if(Button(new Rect(primary.x,primary.y,claimWidth,primary.height),session.Progression.Profile.pendingFashionChest?"请选择上方宝箱":string.IsNullOrEmpty(session.Progression.LastError)?"领取奖励":"重试领取",gold,!session.Progression.Profile.pendingFashionChest))CollectSettlementRewards();
                primary.x+=claimWidth+8*unit;primary.width-=claimWidth+8*unit;
            }
            if(death&&session.CanRetryRoomChain)
            {
                float retryWidth=primary.width*.48f;
                if(Button(new Rect(primary.x,primary.y,retryWidth,primary.height),"原条件重试",gold))
                {session.RetryFailedRoomChain();BlockUITransition();return false;}
                primary.x+=retryWidth+8*unit;primary.width-=retryWidth+8*unit;
            }
            return PrimaryButton(primary, death?"回营整备":"继续拾取", accent, true, null, true);
        }

        private static Rect RecapRect(RunRecapLayout.Area area,float unit)
        { return new Rect(area.X*unit,area.Y*unit,area.Width*unit,area.Height*unit); }

        private float RecapStatusHeight(RunRecapLayout layout,float unit)
        {
            return string.IsNullOrEmpty(recapError)?0:
                Mathf.Ceil(Style(Mathf.RoundToInt(14*unit),false,true).CalcHeight(new GUIContent(recapError),(layout.ContentWidth-24)*unit)/unit)+30;
        }
        private void DrawRecapCards(RunRecapLayout layout,RunRecapPresentation data,float unit,float top=0)
        {
            float y=top,w=layout.ContentWidth;
            RunRecapSnapshot snapshot=data.Snapshot;
            if(data.HasFailureBanner)
            {
                Fill(new Rect(0,y*unit,w*unit,62*unit),new Color(.16f,.10f,.075f));
                DrawIcon(new Rect(15*unit,(y+17)*unit,28*unit,28*unit),UIIconAtlas.Utility("pause"),gold);
                Text(new Rect(57*unit,(y+14)*unit,(w-73)*unit,35*unit),data.FailureLabel,Mathf.RoundToInt(23*unit),pale,true,false,TextAnchor.MiddleLeft);
                y+=76;
            }
            if(data.Metrics.Length>0)
            {
                RecapSection("战斗数据",y,w,unit);y+=28;
                for(int i=0;i<data.Metrics.Length;i++)
                {
                    var metric=data.Metrics[i];Rect r=RecapRect(layout.Metric(i,y),unit);
                    Fill(r,card);Fill(new Rect(r.x,r.y,3*unit,r.height),jade*.7f);
                    DrawIcon(new Rect(r.xMax-38*unit,r.y+13*unit,25*unit,25*unit),UIIconAtlas.Utility(RunRecapPresentation.IconFor(metric.Key)),jade);
                    int numberSize=Mathf.RoundToInt(34*unit);
                    string value=metric.Value.ToString("N0");
                    float measured=Style(numberSize,true).CalcSize(new GUIContent(value)).x;
                    if(measured>r.width-48*unit)numberSize=Mathf.Max(Mathf.RoundToInt(20*unit),Mathf.FloorToInt(numberSize*(r.width-48*unit)/measured));
                    Text(new Rect(r.x+14*unit,r.y+6*unit,r.width-48*unit,44*unit),value,numberSize,pale,true);
                    Text(new Rect(r.x+14*unit,r.y+57*unit,r.width-25*unit,22*unit),metric.Key,Mathf.RoundToInt(14*unit),muted);
                }
                y+=layout.MetricRowsHeight(data.Metrics.Length)+18;
            }
            RecapSection("物品获取",y,w,unit);y+=28;
            long chestGold=settlementChest==null?0:settlementChest.hasCurrencyDeltas?settlementChest.goldDelta:settlementChest.Gold;
            long chestMaterials=settlementChest!=null&&settlementChest.hasCurrencyDeltas&&settlementChest.materialKind==RewardMaterialKind.StarAshFragment?settlementChest.materialsDelta:0;
            long threads=settlementChest!=null&&settlementChest.hasCurrencyDeltas?settlementChest.threadsDelta:0;
            long[] amounts={snapshot.RewardGold+chestGold,snapshot.RewardExperience,snapshot.RewardMaterials+chestMaterials,threads,session.RunPickupPotions,snapshot.RewardRefinementStones};
            string[] labels={"金币","经验","星烬碎片","星纹","药品","装备洗练石"};
            for(int i=0;i<(snapshot.RewardRefinementStones>0?6:session.RunPickupPotions>0?5:4);i++)
            {
                float cell=(w-8)*.5f;Rect tile=new Rect((i%2)*(cell+8)*unit,(y+(i/2)*76)*unit,cell*unit,68*unit);
                Fill(tile,card);
                DrawIcon(new Rect(tile.x+10*unit,tile.y+15*unit,32*unit,32*unit),i==5?UIIconAtlas.Utility("gem"):i==4?UIIconAtlas.Utility("potion"):i==1?UIIconAtlas.Utility("upgrade"):UIIconAtlas.Reward(i==0?0:i-1),i==0?gold:i==1?jade:GameBalance.RarityColor(i==5?Rarity.Epic:i==4?Rarity.Common:Rarity.Rare));
                Text(new Rect(tile.x+52*unit,tile.y+7*unit,tile.width-60*unit,22*unit),labels[i],Mathf.RoundToInt(13*unit),muted);
                Text(new Rect(tile.x+52*unit,tile.y+29*unit,tile.width-60*unit,30*unit),"+"+amounts[i].ToString("N0"),Mathf.RoundToInt(21*unit),pale,true);
            }

        }

        private float RecapTextHeight(string text,float width,float unit,int font)
        {return Mathf.Ceil(Style(Mathf.RoundToInt(font*unit),false,true).CalcHeight(new GUIContent(text),Mathf.Max(1,width*unit))/unit);}
        private float RecapTipHeight(RunRecapPresentation data,float width,float unit,bool expanded)
        {
            if(string.IsNullOrEmpty(data.Tip))return 0;
            float height=Mathf.Max(56,RecapTextHeight(data.Tip,width-64,unit,15)+18);
            if(data.GenerationFailureDetails.Length>0)
            {height+=56;if(expanded)height+=RecapTextHeight(data.GenerationFailureDetails,width-24,unit,14)+20;}
            return height;
        }
        private float DrawRecapTip(RunRecapPresentation data,float y,float width,float unit)
        {
            if(string.IsNullOrEmpty(data.Tip))return y;
            bool expanded=recapGenerationDetailsExpanded;
            float summary=Mathf.Max(56,RecapTextHeight(data.Tip,width-64,unit,15)+18);
            float height=RecapTipHeight(data,width,unit,expanded);
            Fill(new Rect(0,y*unit,width*unit,height*unit),new Color(.055f,.11f,.13f));
            DrawIcon(new Rect(13*unit,(y+16)*unit,24*unit,24*unit),UIIconAtlas.Utility("help"),jade);
            Text(new Rect(49*unit,(y+9)*unit,(width-64)*unit,(summary-18)*unit),data.Tip,Mathf.RoundToInt(15*unit),pale,false,true,TextAnchor.MiddleLeft);
            if(data.GenerationFailureDetails.Length>0)
            {
                if(Button(new Rect(12*unit,(y+summary+4)*unit,(width-24)*unit,44*unit),expanded?"收起异常详情":"查看异常详情",jade))
                    recapGenerationDetailsExpanded=!expanded;
                // Use the measured state for this IMGUI event; the next event applies a toggle.
                if(expanded)Text(new Rect(12*unit,(y+summary+66)*unit,(width-24)*unit,(height-summary-76)*unit),data.GenerationFailureDetails,Mathf.RoundToInt(14*unit),muted,false,true,TextAnchor.MiddleLeft);
            }
            return y+height;
        }

        private void RecapSection(string title,float y,float w,float unit)
        { Text(new Rect(0,y*unit,w*unit,23*unit),title,Mathf.RoundToInt(16*unit),pale,true); }
        private float DrawRecapChips(string title,string[] values,float y,float width,float unit,Color accent)
        {
            if(values.Length==0)return y;
            RecapSection(title,y,width,unit);y+=28;
            RunRecapLayout.Area[] chips=RunRecapChipLayout.Pack(values,width);
            for(int i=0;i<chips.Length;i++)
            {
                var chip=chips[i];Rect r=new Rect(chip.X*unit,(y+chip.Y)*unit,chip.Width*unit,chip.Height*unit);
                Fill(r,card);Border(r,new Color(accent.r,accent.g,accent.b,.25f));
                Text(new Rect(r.x+11*unit,r.y,r.width-22*unit,r.height),values[i],Mathf.RoundToInt(14*unit),accent,true,false,TextAnchor.MiddleLeft);
            }
            return y+RunRecapChipLayout.Height(chips)+18;
        }
        private static float ProgressCardHeight(RunRecapSnapshot data)
        { return 24+(data.RewardDetailsUnavailable?64:0)+(data.RewardGold>0||data.RewardExperience>0||data.RewardMaterials>0?72:0)+(data.Materials>0?72:0)+(data.GoldLost>0?34:0)+(data.PendingChest||data.FirstClearChoice?28:0); }
        private float RecapContentHeight(RunRecapLayout layout,RunRecapPresentation data)
        {
            if(data==null)return 110;
            float result=data.HasFailureBanner?76:0;
            result+=28+layout.MetricRowsHeight(data.Metrics.Length)+18;
            result+=28+(session.RunPickupPotions>0||data.Snapshot.RewardRefinementStones>0?228:152)+18;
            return result+6;
        }
    }
}
