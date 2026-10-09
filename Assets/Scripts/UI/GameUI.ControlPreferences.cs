using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool mobileBindingEditor;
        private string mobileBindingOwner;
        private int[] mobileBindings;
        private Rect MobileVisualRect(Rect hit)
        {float k=EffectPreferences.TouchVisualScale;return new Rect(hit.center.x-hit.width*k*.5f,hit.center.y-hit.height*k*.5f,hit.width*k,hit.height*k);}
        private void EnsureMobileBindings()
        {
            string key="Emberfall.TouchBindings."+session.Progression.CurrentSlotId+"."+(int)session.Progression.Profile.heroClass;
            if(mobileBindings!=null&&mobileBindingOwner==key)return;
            mobileBindingOwner=key;mobileBindings=MobileSkillPolicy.DefaultBindings(session.Progression.Profile.heroClass==HeroClass.Summoner);
            string[] saved=PlayerPrefs.GetString(key,"").Split(',');
            int[] parsed=new int[saved.Length];bool valid=true;
            for(int i=0;i<saved.Length;i++)if(!int.TryParse(saved[i],out parsed[i]))valid=false;
            if(valid&&MobileSkillPolicy.ValidBindings(parsed))mobileBindings=parsed;
            bindingDragSource=-1;bindingDragFinger=-1000;CancelMobileCast();
        }
        private int BoundMobileSkill(int button,int page)
        {
            EnsureMobileBindings();int index=MobileSkillPolicy.BindingIndex(button,page);
            return index<0?-1:mobileBindings[index];
        }
        public int MobileKeyboardSkill(int key)
        {
            int page,button;
            if(key>=(int)KeyCode.Alpha1&&key<=(int)KeyCode.Alpha5){page=0;button=key-(int)KeyCode.Alpha1;}
            else
            {
                page=1;button=key==(int)KeyCode.Z?0:key==(int)KeyCode.X?1:key==(int)KeyCode.C?2:key==(int)KeyCode.V?3:key==(int)KeyCode.B?4:-1;
            }
            if(button<0)return -1;
            mobileSkillPage=page;return BoundMobileSkill(button,page);
        }
        public MobileControlLayout.Area MobileOpportunityArea(int skill)
        {
            var layout=MobileControls.Layout;
            for(int button=0;button<5;button++)if(BoundMobileSkill(button,mobileSkillPage)==skill)
            {
                var key=layout.Skills[button];
                return new MobileControlLayout.Area(key.X,button==0?key.Y-14:button==3&&key.Y-15<140?key.Y+key.Height+1:key.Y-15,key.Width,14);
            }
            return new MobileControlLayout.Area(-10000,-10000,0,0);
        }
        private int bindingDragSource=-1,bindingDragFinger=-1000,bindingTouchFrame=-1;
        private Vector2 bindingDragOrigin,bindingDragPoint;
        private void DrawMobileControlPreferences(float x,float y,float width)
        {
            if(NavigationButton(TouchRect(x,y,width,48),"技能按键配置",jade))
            {mobileBindingEditor=true;bindingDragSource=-1;bindingDragFinger=-1000;CancelMobileScroll();BlockUITransition();}
            if(Button(TouchRect(x,y+58,width,48),"界面字号："+Mathf.RoundToInt(EffectPreferences.InterfaceTextScale*100)+"%",jade))EffectPreferences.CycleInterfaceTextScale();
        }
        private void FinishBindingDrag(Rect[] slots,Vector2 point,bool cancelled)
        {
            int source=bindingDragSource;bindingDragSource=-1;bindingDragFinger=-1000;
            if(cancelled||source<0||(point-bindingDragOrigin).sqrMagnitude<36*TouchRatio*TouchRatio)return;
            for(int target=0;target<slots.Length;target++)if(slots[target].Contains(point))
            {
                if(target!=source&&MobileSkillPolicy.SwapBinding(mobileBindings,target,mobileBindings[source]))
                {PlayerPrefs.SetString(mobileBindingOwner,string.Join(",",mobileBindings));PlayerPrefs.Save();CancelMobileCast();GameAudio.Play(SoundCue.UI);}
                return;
            }
        }
        private Rect BindingPreviewRect(MobileControlLayout.Area area,float minX,float minY,float x,float y,float zoom)
        {return TouchRect(x+(area.X-minX)*zoom,y+(area.Y-minY)*zoom,area.Width*zoom,area.Height*zoom);}
        private void DrawMobileBindingEditor()
        {
            EnsureMobileBindings();var layout=MobileControls.Layout;float u=TouchRatio;
            Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.97f));
            float panelWidth=Mathf.Min(880,layout.Width-24),x=(layout.Width-panelWidth)*.5f;
            Text(TouchRect(x,8,panelWidth-100,34),"技能按键配置",TouchFont(21),pale,true);
            if(QuietAction(TouchRect(x+panelWidth-96,8,96,40),"返回设置"))
            {mobileBindingEditor=false;bindingDragSource=-1;bindingDragFinger=-1000;BlockUITransition();return;}
            Text(TouchRect(x,46,panelWidth,28),"拖动技能交换位置，可跨页拖动；大招、普攻等固定按键不可更改。",TouchFont(12),muted);
            Rect[] slots=new Rect[8];float pageWidth=(panelWidth-16)*.5f;
            for(int page=0;page<2;page++)
            {
                float left=x+page*(pageWidth+16),top=80;
                Fill(TouchRect(left,top,pageWidth,layout.Height-top-12),new Color(.025f,.055f,.075f,.9f));
                Text(TouchRect(left+12,top+4,pageWidth-24,26),page==0?"第一页":"第二页",TouchFont(14),jade,true);
                // Reuse the live combat geometry so preview positions cannot drift from gameplay.
                float minX=layout.Skills[4].X,minY=layout.SkillPage.Y,maxX=layout.Dodge.X+layout.Dodge.Width,maxY=layout.Dodge.Y+layout.Dodge.Height;
                foreach(var skillArea in layout.Skills){minX=Mathf.Min(minX,skillArea.X);minY=Mathf.Min(minY,skillArea.Y);maxY=Mathf.Max(maxY,skillArea.Y+skillArea.Height);}
                float previewScale=Mathf.Min((pageWidth-24)/(maxX-minX),(layout.Height-top-60)/(maxY-minY));
                float originX=left+(pageWidth-(maxX-minX)*previewScale)*.5f;
                float originY=top+42+(layout.Height-top-60-(maxY-minY)*previewScale)*.5f;
                for(int button=0;button<4;button++)
                {
                    int index=page*4+button,skill=mobileBindings[index];
                    Rect hit=slots[index]=BindingPreviewRect(layout.Skills[button],minX,minY,originX,originY,previewScale);
                    bool target=bindingDragSource>=0&&hit.Contains(bindingDragPoint);
                    DrawMobileControlSurface(hit,true,target);
                    if(skill>=0)
                    {
                        float size=hit.width*.76f;
                        DrawIcon(new Rect(hit.center.x-size*.5f,hit.center.y-size*.5f,size,size),UIIconAtlas.SkillGlyph(session.Progression.Profile.heroClass,skill,48),index==bindingDragSource?muted:UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,skill));
                        if(hit.Contains(Mouse))tooltip=GameBalance.SkillName(session.Progression.Profile.heroClass,skill);
                    }
                    else Text(hit,"空位",TouchFont(11),muted,false,false,TextAnchor.MiddleCenter);
                }
                var fixedAreas=new[]{layout.Skills[4],layout.Attack,layout.Dodge,layout.Jump};
                string[] captions={"大招","普攻","闪现","跳跃"};string[] icons={"","attack","blink","jump"};
                for(int i=0;i<fixedAreas.Length;i++)
                {
                    Rect hit=BindingPreviewRect(fixedAreas[i],minX,minY,originX,originY,previewScale);
                    DrawMobileControlSurface(hit,false,false);float size=hit.width*.64f;
                    DrawIcon(new Rect(hit.center.x-size*.5f,hit.center.y-size*.5f,size,size),i==0?UIIconAtlas.SkillGlyph(session.Progression.Profile.heroClass,9,48):UIIconAtlas.Utility(icons[i]),i==0?UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,9):muted);
                    if(hit.Contains(Mouse))tooltip=captions[i]+" · 固定按键";
                    DrawIcon(new Rect(hit.xMax-10*u,hit.y,10*u,10*u),UIIconAtlas.EquipmentLock(true),muted);
                }
                Rect pageArrow=BindingPreviewRect(layout.SkillPage,minX,minY,originX,originY,previewScale);
                float arrowSize=24*u*previewScale;
                DrawIcon(new Rect(pageArrow.center.x-arrowSize*.5f,pageArrow.center.y-arrowSize*.5f,arrowSize,arrowSize),UIIconAtlas.SkillPageArrow(),Color.white);

            }
            if(Input.touchCount>0)
            {
                if(bindingTouchFrame!=Time.frameCount)
                {
                    bindingTouchFrame=Time.frameCount;bool found=false;
                    for(int i=0;i<Input.touchCount;i++)
                    {
                        var touch=Input.GetTouch(i);Vector2 point=ScreenToUI(touch.position);
                        if(touch.phase==TouchPhase.Began&&bindingDragSource<0&&!UITransitionBlocked)
                            for(int slot=0;slot<slots.Length;slot++)if(slots[slot].Contains(point)&&mobileBindings[slot]>=0)
                            {bindingDragSource=slot;bindingDragFinger=touch.fingerId;bindingDragOrigin=point;break;}
                        if(bindingDragFinger!=touch.fingerId)continue;
                        found=true;bindingDragPoint=point;
                        if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)
                            FinishBindingDrag(slots,point,touch.phase==TouchPhase.Canceled);
                    }
                    if(!found){bindingDragSource=-1;bindingDragFinger=-1000;}
                }
            }
            else
            {
                if(bindingDragFinger>=0){bindingDragSource=-1;bindingDragFinger=-1000;}
                var e=Event.current;
                if(e.type==EventType.MouseDown&&e.button==0&&!UITransitionBlocked)
                    for(int slot=0;slot<slots.Length;slot++)if(slots[slot].Contains(Mouse)&&mobileBindings[slot]>=0)
                    {bindingDragSource=slot;bindingDragOrigin=bindingDragPoint=Mouse;e.Use();break;}
                if(bindingDragSource>=0&&e.type==EventType.MouseDrag){bindingDragPoint=Mouse;e.Use();}
                if(bindingDragSource>=0&&e.type==EventType.MouseUp){FinishBindingDrag(slots,Mouse,false);e.Use();}
            }
            if(bindingDragSource>=0)
                DrawIcon(new Rect(bindingDragPoint.x-22*u,bindingDragPoint.y-22*u,44*u,44*u),UIIconAtlas.SkillGlyph(session.Progression.Profile.heroClass,mobileBindings[bindingDragSource],48),UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,mobileBindings[bindingDragSource]));
        }
    }
}
