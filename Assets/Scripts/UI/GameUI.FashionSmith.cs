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
        private void DrawFashionSmith(Rect body,float u)
        {
            var p=session.Progression;bool enabled=GUI.enabled;GUI.enabled=enabled&&smithFashionQuote==null;
            Text(new Rect(body.x,body.y,body.width,26*u),"星纹 "+p.Profile.fashionThreads+" · 分解闲置时装获取星纹，升阶强化外观与属性",Mathf.RoundToInt(12*u),gold,true);
            Rect list=new Rect(body.x,body.y+32*u,body.width,body.height-32*u);
            int columns=body.width/u>=540?3:2;float cw=(list.width/u-(columns-1)*12)/columns;
            float rowHeight=238;
            smithFashionScroll=BeginTouchScroll("smith-fashion",list,smithFashionScroll,new Rect(0,0,list.width,Mathf.Max(list.height,((p.Profile.fashions.Count+columns-1)/columns)*rowHeight*u)));
            for(int i=0;i<p.Profile.fashions.Count;i++)
            {
                var fashion=p.Profile.fashions[i];Rect tile=new Rect(i%columns*(cw+12)*u,i/columns*rowHeight*u,cw*u,(rowHeight-12)*u);
                Fill(tile,card);Border(tile,GameBalance.RarityColor(fashion.rarity));
                DrawIcon(new Rect(tile.x+10*u,tile.y+10*u,42*u,42*u),UIIconAtlas.FashionCardIcon(fashion.slot,(int)fashion.VisualRarity,p.Profile.heroClass),GameBalance.RarityColor(fashion.VisualRarity));
                Text(new Rect(tile.x+60*u,tile.y+8*u,tile.width-68*u,44*u),fashion.name+"\n"+fashion.upgradeRank+"阶 / "+ProgressionService.MaximumFashionRank+"阶",Mathf.RoundToInt(12*u),pale,true,true);
                Text(new Rect(tile.x+10*u,tile.y+58*u,tile.width-20*u,94*u),ProgressionService.FashionBonus(fashion).Replace(" · ","\n"),Mathf.RoundToInt(11*u),jade,false,true);
                var upgrade=p.PrepareFashionService(fashion.id,false,SmithServiceActive);
                var dismantle=p.PrepareFashionService(fashion.id,true,SmithServiceActive);
                string upgradeReason=p.FashionServiceLock(fashion.id,false,SmithServiceActive),dismantleReason=p.FashionServiceLock(fashion.id,true,SmithServiceActive);
                if(Button(new Rect(tile.x+8*u,tile.y+158*u,tile.width-16*u,28*u),upgradeReason.Length==0?"升阶 · "+ProgressionService.FashionUpgradeCost(fashion)+"星纹":upgradeReason,gold,upgrade!=null))smithFashionQuote=upgrade;
                if(Button(new Rect(tile.x+8*u,tile.y+192*u,tile.width-16*u,28*u),dismantleReason.Length==0?"分解 · +"+ProgressionService.FashionDismantleValue(fashion)+"星纹":dismantleReason,jade,dismantle!=null))smithFashionQuote=dismantle;
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
                float cw=(w-34*u)*.5f,visualHeight=Mathf.Min(150*u,h*.35f);
                for(int col=0;col<2;col++)
                {
                    var value=col==0?fashion:next;var model=col==0?smithFashionBefore:smithFashionAfter;
                    Rect visual=new Rect(box.x+12*u+col*(cw+10*u),box.y+64*u,cw,visualHeight);
                    model.SetComposition(value.slot==FashionSlot.Wings?CollectionPreviewComposition.Back:CollectionPreviewComposition.Weapon);
                    model.SetViewport(visual.width*Mathf.Abs(GUI.matrix.m00),visual.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
                    var texture=model.RenderSafe(p.Profile.heroClass,p.Equipped(ItemSlot.Weapon),p.Equipped(ItemSlot.Armor),p.Equipped(ItemSlot.Relic),value.slot==FashionSlot.Wings?value:p.EquippedFashion(FashionSlot.Wings),value.slot==FashionSlot.Weapon?value:p.EquippedFashion(FashionSlot.Weapon));
                    if(texture!=null)GUI.DrawTexture(visual,texture,ScaleMode.ScaleToFit,false);
                    Text(new Rect(visual.x,box.y+40*u,cw,22*u),(col==0?"当前":"升阶后")+" · "+value.upgradeRank+"阶",Mathf.RoundToInt(12*u),col==0?muted:jade,true);
                    Text(new Rect(visual.x,visual.yMax+8*u,cw,Mathf.Max(30*u,box.yMax-64*u-visual.yMax-8*u)),ProgressionService.FashionBonus(value).Replace(" · ","\n"),Mathf.RoundToInt(12*u),col==0?pale:jade,false,true);
                }
            }
            string reason=fresh?p.FashionServiceLock(quote.Id,quote.Dismantle,SmithServiceActive):"时装或材料已变化，请重新打开";
            if(PrimaryButton(new Rect(box.x+12*u,box.yMax-48*u,w-24*u,36*u),reason.Length>0?reason:quote.Dismantle?"确认分解 · +"+quote.Threads+"星纹":"确认升阶 · "+quote.Threads+"星纹",gold,reason.Length==0))
            {bool saved=p.ApplyFashionService(quote,SmithServiceActive);Feedback(saved,quote.Dismantle?"时装已分解":"时装已升阶");if(saved){smithFashionQuote=null;RebuildBagItems();}}
        }
    }
}
