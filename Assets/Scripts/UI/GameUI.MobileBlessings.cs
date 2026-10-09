using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileBlessingScroll;
        private RunBlessing[] mobileBlessingOffer;
        private PlayerController mobileBlessingOwner;
        private int mobileBlessingEpoch=-1,mobileBlessingWave=-1;
        private void ReconcileBlessingOffer(RunBlessing[] offer)
        {
            bool changed=mobileBlessingOwner!=session.Player||mobileBlessingEpoch!=session.Player.CombatEpoch||
                mobileBlessingWave!=session.RunChoices.CompletedWave||mobileBlessingOffer==null||mobileBlessingOffer.Length!=offer.Length;
            if(!changed)for(int i=0;i<offer.Length;i++)if(offer[i]!=mobileBlessingOffer[i]){changed=true;break;}
            // Offer intentionally returns defensive copies; reference equality would
            // erase the selected card on every IMGUI event and prevent confirmation.
            if(changed)
            {mobileBlessingOffer=offer;mobileBlessingOwner=session.Player;mobileBlessingEpoch=session.Player.CombatEpoch;
             mobileBlessingWave=session.RunChoices.CompletedWave;mobileBlessingScroll=Vector2.zero;selectedBlessing=-1;lastBlessingClick=-10;}
        }
        private void DrawMobileBlessingChoice()
        {
            RunBlessing[] offer=session.RunChoices.Offer;
            ReconcileBlessingOffer(offer);
            var layout=MobilePanelGeometry();int count=offer.Length;if(count==0)return;
            float frameWidth=Mathf.Min(960,layout.Width-24),bodyWidth=frameWidth-32;
            float column=(bodyWidth-16-(count-1)*10)/count;
            string preview=BlessingSubtitle(true);
            float previewHeight=MeasureMobileParagraph(preview,bodyWidth-16,13,true)+8;
            float noticeHeight=0,cardHeight=150;
            for(int i=0;i<count;i++)
            {
                float name=MeasureMobileParagraph(RunChoices.Name(offer[i]),column-20,17,true);
                float detail=MeasureMobileParagraph(RunChoices.Description(offer[i]),column-20,14);
                float association=MeasureMobileParagraph(RunChoices.Association(offer[i],session.Progression.Profile,true),column-20,12);
                cardHeight=Mathf.Max(cardHeight,name+detail+association+82);
            }
            float frameHeight=Mathf.Min(layout.Height-24,70+previewHeight+cardHeight+76);
            float frameX=(layout.Width-frameWidth)*.5f,frameY=(layout.Height-frameHeight)*.5f;
            Fill(new Rect(0,0,width,height),new Color(.008f,.018f,.03f,.86f));blockedRects.Add(new Rect(0,0,width,height));
            Box(TouchRect(frameX,frameY,frameWidth,frameHeight),jade,false);
            Text(TouchRect(frameX+16,frameY+12,frameWidth-76,30),"星烬祝福 · 选择一项",TouchFont(22),pale,true);
            if(PopupCloseButton(TouchRect(frameX+frameWidth-52,frameY+8,44,44))){session.SetPaused(true);return;}
            DrawMobileParagraph(frameX+16,frameY+54,bodyWidth-16,preview,13,gold,true);
            Rect viewport=TouchRect(frameX+16,frameY+54+previewHeight,bodyWidth,frameHeight-previewHeight-122);
            mobileBlessingScroll=BeginTouchScroll("mobile-blessings",viewport,mobileBlessingScroll,
                new Rect(0,0,(bodyWidth-16)*TouchRatio,(cardHeight+8)*TouchRatio));
            for(int i=0;i<count;i++)
            {
                float x=i*(column+10);Rect cardRect=TouchRect(x,noticeHeight+4,column,cardHeight);bool chosen=selectedBlessing==i;
                Fill(cardRect,chosen?new Color(.11f,.21f,.22f):card);Border(cardRect,chosen?gold:jade*.45f);
                float y=noticeHeight+14;y+=DrawMobileParagraph(x+10,y,column-20,RunChoices.Name(offer[i]),17,chosen?gold:pale,true)+8;
                bool compatible=RunChoices.IsCompatible(offer[i],session.Progression.Profile.heroClass,RunChoices.UsableRanks(session.Progression.Profile,true));
                y+=DrawMobileParagraph(x+10,y,column-20,RunChoices.Association(offer[i],session.Progression.Profile,true),12,compatible?jade:muted)+12;
                DrawMobileParagraph(x+10,y,column-20,RunChoices.Description(offer[i]),14,pale);
                Text(TouchRect(x+10,noticeHeight+cardHeight-22,column-20,20),chosen?"已选择 ✓":"轻触选择",TouchFont(13),chosen?gold:muted,true,false,TextAnchor.MiddleCenter);
                if(touchScrollSuppressed==Time.frameCount||touchScroll.Dragging)lastBlessingClick=-10;
                if(GUI.Button(cardRect,GUIContent.none,invisibleButton))ClickBlessing(i);
            }
            EndTouchScroll();
            if(PrimaryButton(TouchRect(frameX+16,frameY+frameHeight-60,bodyWidth,44), "确认祝福并继续", gold, selectedBlessing>=0&&selectedBlessing<count))
            {
                if(session.ConfirmBlessing(selectedBlessing)){selectedBlessing=-1;CancelMobileScroll();BlockUITransition();}
            }
        }
    }
}
