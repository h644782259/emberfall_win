using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private ProgressionService.FashionServiceQuote smithFashionQuote;
        private Vector2 smithFashionScroll;
        private CollectionModelPreview smithFashionBefore,smithFashionAfter;
        private void ReleaseFashionSmithPreview()
        {
            if(smithFashionBefore!=null)smithFashionBefore.Dispose();
            if(smithFashionAfter!=null)smithFashionAfter.Dispose();
            smithFashionBefore=smithFashionAfter=null;smithFashionQuote=null;
        }
        private bool DrawFashionServiceAction(Rect rect,bool dismantle,int amount,bool enabled,string reason,float u)
        {
            bool clicked=Button(rect,"",dismantle?new Color(.92f,.38f,.22f):gold,enabled);
            if(dismantle){Text(new Rect(rect.x+4*u,rect.y,rect.width*.48f,rect.height),"分解",Mathf.RoundToInt(13*u),enabled?new Color(1,.64f,.4f):muted,true,false,TextAnchor.MiddleCenter);}
            float center=rect.center.x;
            if(!dismantle)DrawIcon(new Rect(center-30*u,rect.center.y-12*u,20*u,20*u),dismantle?UIIconAtlas.Utility("smith"):UIIconAtlas.EquipmentUpgradeArrow(),enabled?(dismantle?jade:gold):muted);
            DrawIcon(new Rect(center-6*u,rect.center.y-9*u,16*u,16*u),UIIconAtlas.Reward(2),GameBalance.RarityColor(Rarity.Rare));
            Text(new Rect(center+14*u,rect.y,rect.xMax-center-6*u,rect.height),(dismantle?"+":"")+amount,Mathf.RoundToInt(14*u),enabled?pale:muted,true,false,TextAnchor.MiddleLeft);
            if(rect.Contains(Event.current.mousePosition)&&!string.IsNullOrEmpty(reason))GUI.Label(rect,new GUIContent("",reason));
            return clicked;
        }
        private void DrawFashionStatRows(Rect area,FashionData value,FashionData previous,float u)
        {
            string[] rows=ProgressionService.FashionBonus(value).Split(new[]{" · "},System.StringSplitOptions.RemoveEmptyEntries);
            Text(new Rect(area.x,area.y,area.width,20*u),"基础属性 / 升阶加成",Mathf.RoundToInt(11*u),muted,true);
            for(int i=0;i<rows.Length;i++)
            {
                int split=rows[i].LastIndexOf(' ');if(split<0)continue;
                string label=rows[i].Substring(0,split);
                Rect row=new Rect(area.x,area.y+(22+i*28)*u,area.width,26*u);Fill(row,new Color(.055f,.105f,.12f,.95f));
                Text(new Rect(row.x+7*u,row.y,row.width*.5f-7*u,row.height),label,Mathf.RoundToInt(13*u),muted,true);
                int basis=ProgressionService.FashionBaseStat(value,label),rank=ProgressionService.FashionRankStat(value,label);
                float start=row.width*.5f,space=row.width-start-5*u;
                DrawComparedValue(new Rect(row.x+start,row.y,space*.55f,row.height),(label=="暴击几率"?"×":"")+basis+"%",previous==null?0:basis.CompareTo(ProgressionService.FashionBaseStat(previous,label)),u,false);
                DrawComparedValue(new Rect(row.x+start+space*.55f,row.y,space*.45f,row.height),"+"+rank+"%",previous==null?0:rank.CompareTo(ProgressionService.FashionRankStat(previous,label)),u,true);
            }
        }
        private void DrawFashionSmith(Rect body,float u)
        {
            var p=session.Progression;bool enabled=GUI.enabled;GUI.enabled=enabled&&smithFashionQuote==null;
            Rect list=body;
            int columns=body.width/u>=660?3:body.width/u>=420?2:1;float cw=(list.width/u-(columns-1)*12)/columns;
            float rowHeight=278;
            smithFashionScroll=BeginTouchScroll("smith-fashion",list,smithFashionScroll,new Rect(0,0,list.width,Mathf.Max(list.height,((p.Profile.fashions.Count+columns-1)/columns)*rowHeight*u)));
            for(int i=0;i<p.Profile.fashions.Count;i++)
            {
                var fashion=p.Profile.fashions[i];Rect tile=new Rect(i%columns*(cw+12)*u,i/columns*rowHeight*u,cw*u,(rowHeight-12)*u);
                Fill(tile,card);Border(tile,GameBalance.RarityColor(fashion.rarity));
                DrawIcon(new Rect(tile.x+10*u,tile.y+10*u,48*u,48*u),UIIconAtlas.FashionCardIcon(fashion.slot,(int)fashion.VisualRarity,p.Profile.heroClass),GameBalance.RarityColor(fashion.rarity));
                Text(new Rect(tile.x+66*u,tile.y+8*u,tile.width-74*u,44*u),fashion.name+"\n"+fashion.upgradeRank+"阶 / "+ProgressionService.MaximumFashionRank+"阶",Mathf.RoundToInt(12*u),pale,true,true);
                DrawFashionStatRows(new Rect(tile.x+10*u,tile.y+62*u,tile.width-20*u,132*u),fashion,null,u);
                var upgrade=p.PrepareFashionService(fashion.id,false,SmithServiceActive);
                var dismantle=p.PrepareFashionService(fashion.id,true,SmithServiceActive);
                string upgradeReason=p.FashionServiceLock(fashion.id,false,SmithServiceActive),dismantleReason=p.FashionServiceLock(fashion.id,true,SmithServiceActive);
                if(fashion.upgradeRank>=ProgressionService.MaximumFashionRank)
                    DrawSmithMaxBadge(new Rect(tile.x+8*u,tile.y+202*u,(tile.width-24*u)*.5f,48*u),"已满阶",u);
                else if(DrawFashionServiceAction(new Rect(tile.x+8*u,tile.y+202*u,(tile.width-24*u)*.5f,48*u),false,ProgressionService.FashionUpgradeCost(fashion),upgrade!=null,upgradeReason,u))smithFashionQuote=upgrade;
                if(DrawFashionServiceAction(new Rect(tile.center.x+4*u,tile.y+202*u,(tile.width-24*u)*.5f,48*u),true,ProgressionService.FashionDismantleValue(fashion),dismantle!=null,dismantleReason,u))smithFashionQuote=dismantle;
            }
            if(p.Profile.fashions.Count==0)Text(new Rect(10*u,20*u,list.width-20*u,60*u),"暂无时装 · 副本宝箱可获取兵装和羽翼",Mathf.RoundToInt(14*u),muted,false,true);
            EndTouchScroll();GUI.enabled=enabled;
            DrawFashionSmithConfirmation(u);
        }
        private void DrawFashionSmithConfirmation(float u)
        {
            var quote=smithFashionQuote;if(quote==null)return;var p=session.Progression;
            var fashion=p.Profile.fashions.Find(f=>f.id==quote.Id);
            if(fashion==null){smithFashionQuote=null;return;}
            bool fresh=quote.Fingerprint==p.BuildStateFingerprint();
            float w=Mathf.Min(660*u,width-24*u),h=Mathf.Min((quote.Dismantle?250:480)*u,height-24*u);
            Rect box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);blockedRects.Add(box);Fill(box,ink);Border(box,jade);
            Text(new Rect(box.x+12*u,box.y+8*u,w-64*u,30*u),quote.Dismantle?"确认分解 · "+fashion.name:"时装升阶 · "+fashion.name,Mathf.RoundToInt(17*u),gold,true);
            if(PopupCloseButton(new Rect(box.xMax-48*u,box.y+4*u,44*u,36*u))){smithFashionQuote=null;return;}
            if(quote.Dismantle)
                Text(new Rect(box.x+16*u,box.y+52*u,w-32*u,h-112*u),"分解后获得 "+quote.Threads+" 星纹（含已投入升阶材料返还）。\n这件时装将从收藏中移除，收藏属性可能下降。",Mathf.RoundToInt(14*u),pale,false,true);
            else
            {
                var next=p.PreviewFashionUpgrade(fashion.id);
                if(next==null){smithFashionQuote=null;return;}
                if(smithFashionBefore==null)smithFashionBefore=new CollectionModelPreview();
                if(smithFashionAfter==null)smithFashionAfter=new CollectionModelPreview();
                float cw=(w-34*u)*.5f,visualHeight=Mathf.Min(150*u,Mathf.Max(0,h-264*u));
                for(int col=0;col<2;col++)
                {
                    var value=col==0?fashion:next;var model=col==0?smithFashionBefore:smithFashionAfter;
                    Rect visual=new Rect(box.x+12*u+col*(cw+10*u),box.y+64*u,cw,visualHeight);
                    model.SetComposition(value.slot==FashionSlot.Wings?CollectionPreviewComposition.Back:CollectionPreviewComposition.Weapon);
                    model.SetViewport(visual.width*Mathf.Abs(GUI.matrix.m00),visual.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
                    var texture=model.RenderSafe(p.Profile.heroClass,p.Equipped(ItemSlot.Weapon),p.Equipped(ItemSlot.Armor),p.Equipped(ItemSlot.Relic),value.slot==FashionSlot.Wings?value:p.EquippedFashion(FashionSlot.Wings),value.slot==FashionSlot.Weapon?value:p.EquippedFashion(FashionSlot.Weapon));
                    if(texture!=null)GUI.DrawTexture(visual,texture,ScaleMode.ScaleToFit,false);
                    Text(new Rect(visual.x,box.y+40*u,cw,22*u),(col==0?"当前":"升阶后")+" · "+value.upgradeRank+"阶",Mathf.RoundToInt(12*u),col==0?muted:jade,true);
                    DrawFashionStatRows(new Rect(visual.x,visual.yMax+8*u,cw,box.yMax-64*u-visual.yMax-8*u),value,col==0?null:fashion,u);
                }
            }
            string reason=fresh?p.FashionServiceLock(quote.Id,quote.Dismantle,SmithServiceActive):"时装或材料已变化，请重新打开";
            Rect confirm=new Rect(box.x+12*u,box.yMax-48*u,w-24*u,36*u);
            if(DrawFashionServiceAction(confirm,quote.Dismantle,quote.Threads,reason.Length==0,reason,u))
            {bool saved=p.ApplyFashionService(quote,SmithServiceActive);Feedback(saved,quote.Dismantle?"时装已分解":"时装已升阶");if(saved){smithFashionQuote=null;RebuildBagItems();}}
        }
    }
}
