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
        private bool DrawVictorySettlement()
        {
            RefreshSettlementChest();
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            float fw=Mathf.Min(width-24*u,1020*u),fh=Mathf.Min(height-24*u,620*u);
            Rect frame=new Rect((width-fw)*.5f,(height-fh)*.5f,fw,fh);
            Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.32f));Box(frame,gold);blockedRects.Add(frame);
            if(PopupCloseButton(new Rect(frame.xMax-48*u,frame.y+6*u,40*u,36*u)))return true;
            Text(new Rect(frame.x+16*u,frame.y+8*u,fw-80*u,32*u),session.LastRunRecap!=null&&!session.LastRunRecap.Won?"挑战结束":"结算与奖励",Mathf.RoundToInt(22*u),gold,true);
            var snapshot=session.LastRunRecap;
            var data=snapshot==null?null:new RunRecapPresentation(snapshot);
            float contentWidth=fw-32*u,top=frame.y+48*u,metricHeight=48*u;
            if(data!=null)
            {
                float cw=(contentWidth-16*u)/5;
                for(int i=0;i<data.Metrics.Length&&i<5;i++)
                {
                    var metric=data.Metrics[i];Rect tile=new Rect(frame.x+16*u+i*(cw+4*u),top,cw,metricHeight);
                    Fill(tile,card);DrawIcon(new Rect(tile.x+5*u,tile.y+6*u,16*u,16*u),UIIconAtlas.Utility(RunRecapPresentation.IconFor(metric.Key)),jade);
                    Text(new Rect(tile.x+23*u,tile.y+2*u,tile.width-26*u,23*u),metric.Value.ToString("N0"),Mathf.RoundToInt(15*u),pale,true,false,TextAnchor.MiddleRight);
                    Text(new Rect(tile.x+3*u,tile.y+26*u,tile.width-6*u,18*u),metric.Key,Mathf.RoundToInt(10*u),muted,false,false,TextAnchor.MiddleCenter);
                }
            }
            top+=metricHeight+6*u;
            long coins=(snapshot==null?0:snapshot.RewardGold)+(settlementChest==null?0:settlementChest.hasCurrencyDeltas?settlementChest.goldDelta:settlementChest.Gold);
            long xp=snapshot==null?0:snapshot.RewardExperience;
            long shards=(snapshot==null?0:snapshot.RewardMaterials)+(settlementChest!=null&&settlementChest.materialKind==RewardMaterialKind.StarAshFragment?settlementChest.materialsDelta:0);
            long stones=snapshot==null?0:snapshot.RewardRefinementStones;
            long[] amounts={coins,xp,shards,stones,settlementChest==null?0:settlementChest.threadsDelta};
            string[] labels={"金币","经验","碎片","洗练石","星纹"},icons={"coin","upgrade","shard","gem","core"};
            float rw=contentWidth/5;
            for(int i=0;i<5;i++)
            {
                Rect r=new Rect(frame.x+16*u+i*rw,top,rw-4*u,32*u);
                DrawIcon(new Rect(r.x,r.y+6*u,20*u,20*u),UIIconAtlas.Utility(icons[i]),i==0?gold:jade);
                Text(new Rect(r.x+24*u,r.y,r.width-24*u,32*u),labels[i]+" +"+amounts[i],Mathf.RoundToInt(11*u),pale,true,false,TextAnchor.MiddleLeft);
            }
            top+=38*u;
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
                    Rect art=new Rect(tile.x+4*u,tile.y,tile.width-8*u,Mathf.Max(36*u,tile.height-44*u));
                    DrawRewardChest(art,false,1,0);
                    if(Button(new Rect(tile.x+4*u,tile.yMax-40*u,tile.width-8*u,36*u),locked==i?"继续开启":"开启",gold,locked<0||locked==i))
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
            float size=Mathf.Min(stage.height,stage.width*.62f);
            Rect art=new Rect(stage.center.x-size*.5f,stage.y,size,size);
            bool animating=chestReceiptId==reward.Id&&!ChestAnimationDone;
            float progress=animating?ChestRevealPresentation.Progress(Time.unscaledTime-chestRevealedAt,ChestDuration):1;
            if(animating)DrawChestRevealTransition(stage,reward,art);else DrawRewardChest(art,true,1,1);
            if(progress<.55f)return;
            var icons=new System.Collections.Generic.List<EntryRewardPreview>();
            if(reward.equipmentIds!=null)foreach(string id in reward.equipmentIds)
            {var item=session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==id);if(item!=null)icons.Add(ActualEquipmentPreview(item));}
            if(reward.Rarity.HasValue&&reward.Slot.HasValue)
                icons.Add(new EntryRewardPreview{Key="fashion:"+reward.Id,Name=reward.Name,Rarity=reward.Rarity.Value,Tint=GameBalance.RarityColor(reward.Rarity.Value),Icon=UIIconAtlas.FashionCardIcon(reward.Slot.Value,(int)reward.Rarity.Value,session.Progression.Profile.heroClass),Description=ProgressionService.FashionBonus(reward.Slot.Value,reward.Rarity.Value)});
            if(reward.gemMechanic!=EquipmentMechanic.None)
                icons.Add(new EntryRewardPreview{Key="gem:"+reward.Id,Name=BuildCatalog.GemName(reward.gemMechanic),Rarity=reward.gemRarity,Tint=GameBalance.RarityColor(reward.gemRarity),Icon=UIIconAtlas.Utility("gem"),Description=BuildCatalog.MechanicDescription(reward.gemMechanic)});
            if(icons.Count==0){DrawChestResourceVisuals(new Rect(art.x,art.center.y,art.width,art.height*.4f),reward);return;}
            int cols=Mathf.Min(3,icons.Count),rows=(icons.Count+cols-1)/cols;
            float cell=Mathf.Min(66*u,Mathf.Min(art.width/(cols+.5f),stage.height/(rows+1))),gap=8*u;
            float scale=Mathf.SmoothStep(.35f,1,Mathf.Clamp01((progress-.55f)/.35f));
            for(int i=0;i<icons.Count;i++)
            {
                float x=stage.center.x-(cols*cell+(cols-1)*gap)*.5f+(i%cols)*(cell+gap);
                float y=stage.center.y-(rows*cell+(rows-1)*gap)*.5f+(i/cols)*(cell+gap)-12*u;
                Rect icon=new Rect(x+(1-scale)*cell*.5f,y+(1-scale)*cell*.5f,cell*scale,cell*scale);
                DrawEntryRewardIcon(icon,icons[i],u);InspectRewardItem(icon,icons[i]);
            }
            if(!animating)Text(new Rect(stage.x,stage.yMax-28*u,stage.width,24*u),"奖励已获得",Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
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
            Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.84f));
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
                    Fill(tile,card);Border(tile,locked==choice?gold:jade);DrawRewardChest(new Rect(tile.x+8*unit,tile.y+4*unit,tile.width-16*unit,76*unit),false,1,0);
                    if(Button(new Rect(tile.x+4*unit,tile.yMax-48*unit,tile.width-8*unit,44*unit),locked==choice?"继续开启":"宝箱 "+(choice+1),gold,locked<0||locked==choice))CollectSettlementRewards(choice);
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
                DrawIcon(new Rect(tile.x+10*unit,tile.y+15*unit,32*unit,32*unit),i==5?UIIconAtlas.Utility("gem"):i==4?UIIconAtlas.Utility("potion"):i==1?UIIconAtlas.Utility("upgrade"):UIIconAtlas.Reward(i==0?0:i-1),i==0?gold:jade);
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
