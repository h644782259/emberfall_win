using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool MobilePanelOwnsNotification
        {
            get
            {
                return !session.Paused && (panel==Panel.Inventory || panel==Panel.Skills || panel==Panel.Fashion ||
                    panel==Panel.Chests || panel==Panel.HubUtility || panel==Panel.SaveLocation ||
                    panel==Panel.None && session.RunChoices.AwaitingChoice);
            }
        }
        private MobilePanelLayout mobilePanelLayout;
        private Vector2 mobilePanelSize;
        private MobilePanelLayout MobilePanelGeometry()
        {
            var battle=MobileControls.Layout;var size=new Vector2(battle.Width,Mathf.Min(battle.Height,height/TouchRatio));
            if(mobilePanelLayout==null||size!=mobilePanelSize){mobilePanelLayout=new MobilePanelLayout(size.x,size.y);mobilePanelSize=size;}
            return mobilePanelLayout;
        }
        private Rect MobilePanelRect(MobilePanelLayout.Area area)
        {float u=TouchRatio;return new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);}
        private MobileDialogLayout DrawMobileDialogChrome(string title,Color accent)
        {
            var controls=MobileControls.Layout;
            var layout=new MobileDialogLayout(controls.Width,controls.Height);

            blockedRects.Add(new Rect(0,0,width,height));
            Box(MobilePanelRect(layout.Frame),accent,false);
            Text(MobilePanelRect(layout.Header),title,TouchFont(21),pale,true);
            return layout;
        }
        // Returns true only when the existing close/navigation lifecycle was used.
        private bool DrawMobilePanelChrome(MobilePanelLayout layout,string title,string subtitle,bool canClose=true,bool pauseInstead=false,bool showNotice=true,float headerRightReserve=0,bool showClose=true)
        {

            blockedRects.Add(new Rect(0,0,width,height));
            Box(TouchRect(8,4,layout.Width-16,layout.Height-8),jade,false);
            Fill(TouchRect(16,8,layout.Width-32,48),new Color(.018f,.032f,.050f,.98f));
            Fill(TouchRect(16,12,3,28),gold);
            Rect titleRect=TouchRect(layout.Header.X+10,layout.Header.Y,Mathf.Max(1,layout.Header.Width-headerRightReserve-10),30);
            int titleFont=TouchFont(22);
            while(titleFont>1&&(Style(titleFont,true).CalcSize(new GUIContent(title)).x>titleRect.width||Style(titleFont,true).CalcHeight(new GUIContent(title),titleRect.width)>titleRect.height))titleFont--;
            Text(titleRect,title,titleFont,pale,true,false,TextAnchor.MiddleLeft);
            // Keep transient feedback inside a reserved header row; full failures
            // are repeated in measured body content, never over active tabs.
            string notice=showNotice?session.Notification:null;
            Text(TouchRect(layout.Header.X,layout.Header.Y+31,Mathf.Max(1,layout.Header.Width-headerRightReserve),17),
                string.IsNullOrEmpty(notice)?HubNpcServiceSubtitle(subtitle):PlatformText(notice),TouchFont(12),string.IsNullOrEmpty(notice)?muted:gold,false,false,TextAnchor.MiddleLeft);
            Rule(16*TouchRatio,60*TouchRatio,(layout.Width-32)*TouchRatio,jade);
            if(showClose&&NavigationButton(MobilePanelRect(layout.Close), pauseInstead?"菜单":"×", jade, canClose||pauseInstead))
            {if(pauseInstead)session.SetPaused(true);else ClosePanel();BlockUITransition();return true;}
            return false;
        }
        private float MeasureMobileParagraph(string value,float availableWidth,int fontSize=14,bool bold=false)
        {
            if(string.IsNullOrEmpty(value))return 0;
            float u=TouchRatio;
            float measured=Style(TouchFont(fontSize),bold,true,TextAnchor.UpperLeft).CalcHeight(new GUIContent(value),Mathf.Max(1,availableWidth*u))/u;
            return Mathf.Ceil(Mathf.Max(fontSize*1.35f,measured))+2;
        }
        private float DrawMobileParagraph(float x,float y,float availableWidth,string value,int size,Color color,bool bold=false)
        {
            float h=MeasureMobileParagraph(value,availableWidth,size,bold);
            if(h>0)Text(TouchRect(x,y,availableWidth,h),value,TouchFont(size),color,bold,true);
            return h;
        }
        private void MobileDialogParagraph(ref float y,float width,string value,int size,Color color,bool draw,bool bold=false)
        {
            if(string.IsNullOrEmpty(value))return;
            y+=draw?DrawMobileParagraph(8,y,width-16,value,size,color,bold):MeasureMobileParagraph(value,width-16,size,bold);
            y+=10;
        }
    }
}
