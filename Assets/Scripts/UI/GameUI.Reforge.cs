using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private ProgressionService reforgeOwner;
        private string reforgeCharacter,reforgeItem,reforgeNotice;
        private ReforgeChoice[] reforgeChoices;
        private ReforgeQuote reforgeSelected;
        private Vector2 reforgeScroll;
        private void OpenReforgeSurface(string itemId)
        {
            var p=session.Progression;if(panel!=Panel.Camp||!session.IsInCamp)return;
            var choices=p.ReforgeChoices(itemId);if(choices.Length==0)return;
            reforgeOwner=p;reforgeCharacter=p.CurrentSlotId;reforgeItem=itemId;reforgeChoices=choices;reforgeSelected=choices[0].Quote;
            reforgeNotice=null;reforgeScroll=Vector2.zero;CancelMobileScroll();BlockUITransition();
        }
        private void ReconcileReforgeSurface()
        {if(reforgeOwner!=null&&(panel!=Panel.Camp||!session.IsInCamp||reforgeOwner!=session.Progression||reforgeCharacter!=session.Progression.CurrentSlotId))ClearReforgeSurface();}
        private void ClearReforgeSurface()
        {reforgeOwner=null;reforgeCharacter=reforgeItem=reforgeNotice=null;reforgeChoices=null;reforgeSelected=null;reforgeScroll=Vector2.zero;}
        private bool CloseReforgeSurface()
        {if(reforgeOwner==null)return false;ClearReforgeSurface();CancelMobileScroll();BlockUITransition();return true;}
        private bool ExecuteReforgeSelection(bool track)
        {
            ReconcileReforgeSurface();if(reforgeOwner==null||reforgeSelected==null)return false;
            bool saved=track?reforgeOwner.SelectProgressionGoal(ProgressionGoalKind.Reforge,reforgeItem,0,reforgeSelected.TargetLevel):reforgeOwner.ReforgeMechanic(reforgeSelected,session.IsInCamp);
            reforgeNotice=saved?(track?"已追踪固定目标等级":"重铸已保存"):reforgeOwner.LastError;
            if(saved&&!track)CloseReforgeSurface();BlockUITransition();return saved;
        }
        private bool DrawReforgeSurface()
        {
            ReconcileReforgeSurface();if(reforgeOwner==null)return false;
            float u=MobileControls.Active?TouchRatio:1;var l=new MobileDialogLayout(width/u,height/u);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,1));blockedRects.Add(new Rect(0,0,width,height));Box(BuildPlanRect(l.Frame,u),jade,false);
            Text(BuildPlanRect(l.Header,u),"重铸 · 选择固定目标",Mathf.RoundToInt(21*u),pale,true);
            float bodyWidth=l.Body.Width-18;float h=DrawReforgeOptions(bodyWidth,u,false);
            reforgeScroll=BeginTouchScroll("reforge-options",BuildPlanRect(l.Body,u),reforgeScroll,new Rect(0,0,bodyWidth*u,Mathf.Max(l.Body.Height,h)*u));
            DrawReforgeOptions(bodyWidth,u,true);EndTouchScroll();
            var preview=reforgeOwner.PreviewReforge(reforgeSelected);string reason=reforgeOwner.ReforgeLockReason(reforgeSelected,session.IsInCamp);
            if(Button(BuildPlanRect(l.FooterButton(0,3),u),"追踪此目标",jade,preview!=null))ExecuteReforgeSelection(true);
            Rect reforgeAction=BuildPlanRect(l.FooterButton(1,3),u);
            if(Button(reforgeAction,"",gold,reason.Length==0,reason))ExecuteReforgeSelection(false);
            Text(new Rect(reforgeAction.x+8*u,reforgeAction.y,reforgeAction.width-92*u,reforgeAction.height),"重铸",Mathf.RoundToInt(13*u),gold,true);
            DrawPrice(new Rect(reforgeAction.xMax-84*u,reforgeAction.y+4*u,76*u,reforgeAction.height-8*u),reforgeSelected==null?0:reforgeSelected.GoldCost,false,u);
            if(NavigationButton(BuildPlanRect(l.FooterButton(2,3),u), "返回工坊", jade))CloseReforgeSurface();return true;
        }
        private float DrawReforgeOptions(float w,float u,bool draw)
        {
            float y=4;
            foreach(var choice in reforgeChoices)
            {
                bool selected=reforgeSelected==choice.Quote;
                string label=choice.Label+" → "+choice.Quote.TargetLevel+"级"+(selected?" ✓":"");
                if(draw)
                {
                    Rect option=new Rect(4*u,y*u,(w-8)*u,44*u);
                    if(TabButton(option,"",selected)){reforgeSelected=choice.Quote;reforgeNotice=null;}
                    Text(new Rect(option.x+8*u,option.y,option.width-110*u,option.height),label,Mathf.RoundToInt(13*u),selected?gold:pale,true);
                    DrawPrice(new Rect(option.xMax-100*u,option.y+8*u,92*u,28*u),choice.Quote.GoldCost,false,u);
                }
                y+=50;
            }
            var before=reforgeOwner.Profile.inventory.Find(x=>x.id==reforgeItem);var after=reforgeOwner.PreviewReforge(reforgeSelected);
            string text=after==null?"报价已失效，请返回重新选择。":"固定 "+reforgeSelected.FromLevel+" → "+reforgeSelected.TargetLevel+"级\n"+
                "攻击 "+before.attack+" → "+after.attack+" · 防御 "+before.defense+" → "+after.defense+" · 生命 "+before.health+" → "+after.health+"\n"+
                (reforgeOwner.Profile.gold>=reforgeSelected.GoldCost?"执行后余金 "+(reforgeOwner.Profile.gold-reforgeSelected.GoldCost):"金币不足 · 仍缺 "+(reforgeSelected.GoldCost-reforgeOwner.Profile.gold))+"\n保留身份、品质、机制变体与部位强化；分段总价相同。";
            if(!string.IsNullOrEmpty(reforgeNotice))text+="\n"+reforgeNotice;
            GoalParagraph(ref y,w,u,text,13,pale,false,draw);return y+8;
        }
    }
}
