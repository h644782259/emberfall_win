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

        // Returns only the explicit primary action. Respawn/navigation stays with the caller.
        private bool DrawStructuredRunRecap(bool death)
        {
            bool mobile=MobileControls.Active;
            float unit=mobile?TouchRatio:1f;
            RunRecapLayout layout=new RunRecapLayout(width/unit,height/unit,mobile);
            RunRecapSnapshot snapshot=session.LastRunRecap;
            if(!object.ReferenceEquals(renderedRecap,snapshot))
            {
                renderedRecap=snapshot; recapScroll=Vector2.zero;recapGenerationDetailsExpanded=false;
                recapPresentation=snapshot==null?null:new RunRecapPresentation(snapshot);
            }
            string latestError=session.Progression.LastError;
            if(recapError!=latestError)
            {recapError=latestError;if(!string.IsNullOrEmpty(recapError))recapScroll=Vector2.zero;}
            RunRecapPresentation data=recapPresentation;
            Color accent=snapshot!=null&&snapshot.Won?gold:jade;
            Rect frame=RecapRect(layout.Frame,unit),header=RecapRect(layout.Header,unit);
            Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.84f));
            Box(frame,accent);Fill(new Rect(frame.x,frame.y,4*unit,frame.height),accent);
            float iconSize=mobile?35:44;
            DrawIcon(new Rect(header.x,header.y+2*unit,iconSize*unit,iconSize*unit),UIIconAtlas.Utility(snapshot!=null&&snapshot.Won?"confirm":"skills"),accent);
            string title=snapshot==null?"战斗复盘":snapshot.Won?(snapshot.ModeName.Length>0?"挑战完成":"遗迹通关"):"本次止步";
            Text(new Rect(header.x+(iconSize+12)*unit,header.y,header.width-(iconSize+126)*unit,32*unit),title,Mathf.RoundToInt((mobile?23:28)*unit),pale,true);
            string location=snapshot==null?"":snapshot.InDungeon?(snapshot.ModeName.Length>0?snapshot.ModeName+"  ·  ":"")+"第 "+snapshot.Tier+" 阶  ·  "+snapshot.Wave+" / "+snapshot.TotalWaves+(snapshot.ModeName.Length>0?" 阶段":" 波"):"原野探索";
            Text(new Rect(header.x+(iconSize+12)*unit,header.y+34*unit,header.width-(iconSize+20)*unit,22*unit),location,Mathf.RoundToInt(13*unit),muted);
            if(snapshot!=null&&snapshot.InDungeon)
            {
                Rect mode=new Rect(header.xMax-104*unit,header.y+6*unit,104*unit,29*unit);
                Fill(mode,card);Text(mode,snapshot.Challenge?"限疗挑战":"普通模式",Mathf.RoundToInt(13*unit),accent,true,false,TextAnchor.MiddleCenter);
            }
            Rule(frame.x+16*unit,frame.y+(mobile?61:78)*unit,frame.width-32*unit,accent*.5f);
            Rect viewport=RecapRect(layout.Viewport,unit);
            float statusHeight=RecapStatusHeight(layout,unit);
            float contentHeight=RecapContentHeight(layout,data)+statusHeight;
            recapScroll=BeginTouchScroll("recap",viewport,recapScroll,new Rect(0,0,layout.ContentWidth*unit,Mathf.Max(viewport.height,contentHeight*unit)));
            if(statusHeight>0)
            {
                Fill(new Rect(0,0,layout.ContentWidth*unit,(statusHeight-10)*unit),new Color(.17f,.095f,.065f));
                Text(new Rect(12*unit,10*unit,(layout.ContentWidth-24)*unit,(statusHeight-30)*unit),recapError,Mathf.RoundToInt(14*unit),gold,false,true);
            }
            if(data==null)
                Text(new Rect(12*unit,(24+statusHeight)*unit,(layout.ContentWidth-24)*unit,56*unit),"暂无战斗记录",Mathf.RoundToInt(22*unit),muted,true,false,TextAnchor.MiddleCenter);
            else DrawRecapCards(layout,data,unit,statusHeight);
            EndTouchScroll();
            Rect primary=RecapRect(layout.Primary,unit);
            if(death||session.ModeFinished)
            {
                float menuWidth=Mathf.Min(152*unit,primary.width*.35f);
                if(NavigationButton(new Rect(primary.x,primary.y,menuWidth,primary.height), "菜单 / 存档", jade))session.SetPaused(true);
                primary.x+=menuWidth+12*unit;primary.width-=menuWidth+12*unit;
            }
            if(session.CanRetryRoomChain)
            {
                float retryWidth=primary.width*.48f;
                if(Button(new Rect(primary.x,primary.y,retryWidth,primary.height),"原条件重试",gold))
                {session.RetryFailedRoomChain();BlockUITransition();return false;}
                primary.x+=retryWidth+8*unit;primary.width-=retryWidth+8*unit;
            }
            return PrimaryButton(primary, death?"回营整备":session.ModeFinished?session.ModeRewardPending?"结算奖励并回营":"返回营地":"返回冒险", accent, true, null, true);
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
            string goal=CurrentProgressionGoalStatus(data.Snapshot.RewardMaterials);
            float goalHeight=RecapGoalHeight(layout,unit,data);
            Fill(new Rect(0,y*unit,w*unit,(goalHeight-10)*unit),card);
            Text(new Rect(12*unit,(y+8)*unit,(w-24)*unit,(goalHeight-26)*unit),goal,Mathf.RoundToInt(13*unit),jade,false,true);
            y+=goalHeight;
            RunRecapSnapshot snapshot=data.Snapshot;
            if(data.HasFailureBanner)
            {
                Fill(new Rect(0,y*unit,w*unit,62*unit),new Color(.16f,.10f,.075f));
                DrawIcon(new Rect(15*unit,(y+17)*unit,28*unit,28*unit),UIIconAtlas.Utility("pause"),gold);
                Text(new Rect(57*unit,(y+14)*unit,(w-73)*unit,35*unit),data.FailureLabel,Mathf.RoundToInt(23*unit),pale,true,false,TextAnchor.MiddleLeft);
                y+=76;
            }
            if(data.HasGenerationFailure)y=DrawRecapTip(data,y,w,unit);
            if(data.HasDamage)
            {
                Rect cause=new Rect(0,y*unit,w*unit,66*unit);Fill(cause,new Color(.16f,.085f,.08f));
                DrawIcon(new Rect(14*unit,(y+17)*unit,31*unit,31*unit),UIIconAtlas.Utility("attack"),new Color(1f,.58f,.44f));
                Text(new Rect(58*unit,(y+9)*unit,(w-184)*unit,20*unit),"最后受击",Mathf.RoundToInt(12*unit),muted);
                Text(new Rect(58*unit,(y+30)*unit,(w-184)*unit,26*unit),snapshot.LastDamageSource,Mathf.RoundToInt(18*unit),pale,true);
                Text(new Rect((w-116)*unit,(y+14)*unit,100*unit,38*unit),"−"+Money(Mathf.CeilToInt(Mathf.Min(1000000000,snapshot.LastDamageAmount))),Mathf.RoundToInt(27*unit),new Color(1f,.64f,.49f),true,false,TextAnchor.MiddleRight);
                y+=80;
            }
            if(data.Metrics.Length>0)
            {
                RecapSection("关键操作",y,w,unit);y+=28;
                for(int i=0;i<data.Metrics.Length;i++)
                {
                    var metric=data.Metrics[i];Rect r=RecapRect(layout.Metric(i,y),unit);
                    Fill(r,card);Fill(new Rect(r.x,r.y,3*unit,r.height),jade*.7f);
                    DrawIcon(new Rect(r.xMax-38*unit,r.y+13*unit,25*unit,25*unit),UIIconAtlas.Utility(RunRecapPresentation.IconFor(metric.Key)),jade);
                    int numberSize=Mathf.RoundToInt(34*unit*Mathf.Clamp(EffectPreferences.CombatTextScale/1.25f,1f,1.18f));
                    string value=metric.Value.ToString("N0");
                    float measured=Style(numberSize,true).CalcSize(new GUIContent(value)).x;
                    if(measured>r.width-48*unit)numberSize=Mathf.Max(Mathf.RoundToInt(20*unit),Mathf.FloorToInt(numberSize*(r.width-48*unit)/measured));
                    Text(new Rect(r.x+14*unit,r.y+6*unit,r.width-48*unit,44*unit),value,numberSize,pale,true);
                    Text(new Rect(r.x+14*unit,r.y+57*unit,r.width-25*unit,22*unit),metric.Key,Mathf.RoundToInt(14*unit),muted);
                }
                y+=layout.MetricRowsHeight(data.Metrics.Length)+18;
            }
            if(data.HasProgress)
            {
                RecapSection(snapshot.Won?"物资与奖励":"保留与损失",y,w,unit);y+=28;
                float cardHeight=ProgressCardHeight(snapshot);
                Fill(new Rect(0,y*unit,w*unit,cardHeight*unit),card);
                float inner=y+12;
                if(data.Rewards.Length>0)
                {
                    float rewardWidth=(w-28-10*(data.Rewards.Length-1))/data.Rewards.Length;
                    for(int i=0;i<data.Rewards.Length;i++)
                    {
                        float x=14+i*(rewardWidth+10);
                        DrawRewardToken(new Rect(x*unit,inner*unit,rewardWidth*unit,35*unit),data.Rewards[i].Key=="金币"?0:data.Rewards[i].Key=="碎片"?1:3,data.Rewards[i].Value,unit);
                        Text(new Rect(x*unit,(inner+38)*unit,rewardWidth*unit,23*unit),data.Rewards[i].Key,Mathf.RoundToInt(13*unit),muted);
                    }
                    inner+=72;
                }
                if(snapshot.RewardDetailsUnavailable)
                {
                    Text(new Rect(14*unit,inner*unit,(w-28)*unit,56*unit),"通关进度与奖励已保存，可返回营地继续冒险。",Mathf.RoundToInt(13*unit),muted,false,true);
                    inner+=64;
                }
                if(snapshot.Materials>0)
                {
                    Text(new Rect(14*unit,inner*unit,(w-182)*unit,24*unit),"结算碎片",Mathf.RoundToInt(15*unit),pale,true);
                    Text(new Rect((w-164)*unit,(inner-4)*unit,150*unit,30*unit),snapshot.Materials+" / "+snapshot.ExchangeCost,Mathf.RoundToInt(22*unit),gold,true,false,TextAnchor.MiddleRight);
                    Fill(new Rect(14*unit,(inner+32)*unit,(w-28)*unit,5*unit),new Color(.09f,.12f,.16f));
                    Fill(new Rect(14*unit,(inner+32)*unit,(w-28)*unit*data.ExchangeProgress,5*unit),gold);
                    Text(new Rect(14*unit,(inner+43)*unit,(w-28)*unit,21*unit),snapshot.Materials>=snapshot.ExchangeCost?"可兑换机制装备":"距兑换还差 "+(snapshot.ExchangeCost-snapshot.Materials)+" 碎片",Mathf.RoundToInt(13*unit),muted);
                    inner+=72;
                }
                if(snapshot.GoldLost>0)
                {
                    Text(new Rect(14*unit,inner*unit,(w-160)*unit,25*unit),"倒下损失",Mathf.RoundToInt(14*unit),muted);
                    Text(new Rect((w-158)*unit,inner*unit,144*unit,25*unit),"−"+Money(snapshot.GoldLost)+" 金币",Mathf.RoundToInt(18*unit),new Color(1f,.64f,.49f),true,false,TextAnchor.MiddleRight);
                    inner+=34;
                }
                if(snapshot.PendingChest||snapshot.FirstClearChoice)
                {
                    string reward=snapshot.PendingChest?"通关宝箱":"";
                    if(snapshot.FirstClearChoice)reward+=(reward.Length>0?"   ·   ":"")+"首通自选";
                    DrawIcon(new Rect(14*unit,inner*unit,20*unit,20*unit),UIIconAtlas.Utility("inventory"),gold);
                    Text(new Rect(42*unit,inner*unit,(w-56)*unit,24*unit),reward,Mathf.RoundToInt(14*unit),gold,true);
                }
                y+=cardHeight+18;
            }
            y=DrawRecapChips("机制实例 · 生效须实际扣血",data.MechanismEvidence,y,w,unit,jade);
            y=DrawRecapChips("机制装备",data.Mechanics,y,w,unit,gold);
            y=DrawRecapChips("本局祝福",data.Blessings,y,w,unit,jade);
            y=DrawRecapChips("更多操作",data.ExtraActions,y,w,unit,muted);
            if(!data.HasGenerationFailure)DrawRecapTip(data,y,w,unit);
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
        private float RecapGoalHeight(RunRecapLayout layout,float unit,RunRecapPresentation data)
        {return Mathf.Ceil(Style(Mathf.RoundToInt(13*unit),false,true).CalcHeight(new GUIContent(CurrentProgressionGoalStatus(data.Snapshot.RewardMaterials)),(layout.ContentWidth-24)*unit)/unit)+26;}
        private float RecapContentHeight(RunRecapLayout layout,RunRecapPresentation data)
        {
            if(data==null)return 110;
            float result=RecapGoalHeight(layout,MobileControls.Active?TouchRatio:1,data)+(data.HasDamage?80:0)+(data.HasFailureBanner?76:0);
            if(data.Metrics.Length>0)result+=28+layout.MetricRowsHeight(data.Metrics.Length)+18;
            if(data.HasProgress)result+=28+ProgressCardHeight(data.Snapshot)+18;
            foreach(string[] values in new[]{data.MechanismEvidence,data.Mechanics,data.Blessings,data.ExtraActions})
                if(values.Length>0)result+=28+RunRecapChipLayout.Height(RunRecapChipLayout.Pack(values,layout.ContentWidth))+18;
            return result+RecapTipHeight(data,layout.ContentWidth,MobileControls.Active?TouchRatio:1,recapGenerationDetailsExpanded)+6;
        }
    }
}
