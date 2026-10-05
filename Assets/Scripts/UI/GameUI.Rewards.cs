using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly uint[][] rewardChestFrames = new uint[13][];
        private Texture2D rewardChestClosed,rewardChestComposite;
        private Color32[] chestCompositePixels;
        private int chestCompositeKey=-1;
        private Rect chestRevealOrigin;
        private bool chestDetails, chestOpening;
        private int revealedChest = -1;
        private string chestRevealResult;
        private float chestRevealedAt;
        private bool rewardSoundPlayed;
        private string chestReceiptId;
        private float ChestDuration { get { var reward=session.Progression.LastChestReward; return EffectPreferences.ReducedEffects ? .15f : reward != null && reward.Duplicate ? .7f : reward == null || !reward.Rarity.HasValue ? .95f : 1.1f + (int)reward.Rarity.Value * .28f; } }
        private bool ChestAnimationDone { get { return chestRevealResult != null && Time.unscaledTime-chestRevealedAt >= ChestDuration; } }

        private void ResetChestReveal()
        {
            chestDetails = false;
            chestOpening = false;
            revealedChest = -1;
            chestRevealResult = null;
            chestReceiptId = null;
            rewardSoundPlayed = false;
            desktopChestResultScroll=Vector2.zero;
            if (session.Progression.Profile.pendingChestReveal && session.Progression.LastChestReward != null)
            {
                var reward=session.Progression.LastChestReward;
                revealedChest=Mathf.Clamp(reward.choice,0,2); chestRevealResult=reward.summary; chestReceiptId=reward.Id;
                chestRevealedAt=Time.unscaledTime-ChestDuration; rewardSoundPlayed=true;
            }
        }

        private void ReleaseChestTextures()
        {
            if(rewardChestClosed!=null)Destroy(rewardChestClosed);if(rewardChestComposite!=null)Destroy(rewardChestComposite);
            rewardChestClosed=rewardChestComposite=null;chestCompositePixels=null;chestCompositeKey=-1;
            for(int i=0;i<rewardChestFrames.Length;i++)rewardChestFrames[i]=null;
        }

        private Vector2 desktopChestResultScroll;
        private void DrawChests()
        {
            if(MobileControls.Active){DrawMobileChests();return;}
            var progression=session.Progression;var savedReward=progression.LastChestReward;
            if(progression.Profile.pendingChestReveal&&savedReward!=null&&chestReceiptId!=savedReward.Id)ResetChestReveal();
            if(!progression.Profile.pendingChestReveal&&chestRevealResult!=null)ResetChestReveal();
            bool revealed=chestRevealResult!=null&&progression.Profile.pendingChestReveal,complete=revealed&&ChestAnimationDone;
            var reward=revealed?savedReward:null;
            Color accent=reward!=null&&reward.Rarity.HasValue?GameBalance.RarityColor(reward.Rarity.Value):gold;
            if(revealed&&complete&&!rewardSoundPlayed)
            {rewardSoundPlayed=true;GameAudio.Play(reward==null||!reward.Rarity.HasValue||reward.Duplicate?SoundCue.UI:reward.Rarity.Value==Rarity.Legendary?SoundCue.Victory:reward.Rarity.Value==Rarity.Epic?SoundCue.LevelUp:reward.Rarity.Value==Rarity.Rare?SoundCue.Loot:SoundCue.Cast);}
            Fill(new Rect(0,0,width,height),new Color(.018f,.025f,.045f,.9f));
            float ww=Mathf.Min(860,width-32),wh=Mathf.Min(550,height-24);
            Rect w=new Rect((width-ww)*.5f,(height-wh)*.5f,ww,wh);
            Fill(w,new Color(.045f,.064f,.095f,.99f));Border(w,new Color(.52f,.60f,.67f,.3f));
            Text(new Rect(w.x+28,w.y+20,w.width-56,18),"F A L L E N   S T A R",10,gold,true);
            Text(new Rect(w.x+28,w.y+45,w.width-248,42),revealed?(complete?"宝箱奖励":"开启宝箱"):"遗迹馈赠",28,pale,true);
            Text(new Rect(w.x+28,w.y+92,w.width-56,24),revealed?(complete?ChestRevealPresentation.Outcome(reward):"已保存奖励 · 可以跳过揭晓动画"):ChestRevealPresentation.ChoiceDisclosure,14,muted);
            if(Button(new Rect(w.xMax-200,w.y+43,78,36),"菜单",jade)){session.SetPaused(true);BlockUITransition();return;}
            if(Button(new Rect(w.xMax-110,w.y+43,82,36),chestDetails?"收起规则":"奖励规则",muted))chestDetails=!chestDetails;
            Rect body=new Rect(w.x+28,w.y+132,w.width-56,w.height-208);
            if(chestDetails)DrawDesktopChestRules(body);
            else if(complete)DrawDesktopChestResult(body,reward,accent);
            else if(revealed)DrawChestRevealTransition(body,reward,new Rect(body.x,body.y,ChestRevealPresentation.DesktopArtSize(body.height),ChestRevealPresentation.DesktopArtSize(body.height)));
            else
            {
                Rect r=new Rect(body.x,body.y,body.width,body.height);
                DrawSingleChestCard(r,1);
                if(Button(new Rect(r.x+12,r.yMax-54,r.width-24,42),progression.ChestOpenCaption,gold,!chestOpening&&progression.Profile.pendingFashionChest&&!progression.Profile.pendingChestReveal))
                {
                    chestOpening=true;string result=progression.OpenDungeonChest();
                    if(result==null){chestOpening=false;Feedback(false,"宝箱暂时无法开启");}
                    else {chestRevealOrigin=ChestChoiceArt(r,1);revealedChest=0;chestRevealResult=result;chestRevealedAt=Time.unscaledTime;chestDetails=false;rewardSoundPlayed=false;chestReceiptId=progression.LastChestReward.Id;desktopChestResultScroll=Vector2.zero;GameAudio.Play(SoundCue.Cast);}
                    BlockUITransition();return;
                }
            }

            if(revealed)
            {
                Text(new Rect(w.x+28,w.yMax-55,w.width-430,36),complete?"奖励已保存":"正在揭晓已保存的奖励",13,muted,false,true);
                if(complete&&CanTrialChestReward(reward)&&Button(new Rect(w.xMax-396,w.yMax-58,180,42),"收下并试穿",jade)){AcceptChestForTrial();return;}
                if(Button(new Rect(w.xMax-208,w.yMax-58,180,42),complete?"收下":"跳过动画",jade,!chestDetails,null,true))
                {if(!complete)chestRevealedAt=Time.unscaledTime-ChestDuration;else FinishChestReveal();BlockUITransition();}
            }
            else Text(new Rect(w.x+28,w.yMax-50,w.width-56,36),string.IsNullOrEmpty(progression.LastError)?"开启后奖励先保存，再展示结果":progression.LastError,13,muted,false,true);
        }
        private void DrawDesktopChestRules(Rect r)
        {
            int minimum=TierRewardRules.ChestGoldMinimum(session.Progression.Profile.pendingChestTier);
            string rules=ProgressionService.DungeonChestRules(session.Progression.Profile.pendingChestTier, session.Progression.Profile.pendingChestReveal ? session.Progression.LastChestReward : null);
            Text(new Rect(r.x+10,r.y+8,r.width-20,r.height-16),rules,15,pale,false,true);
        }
        private void DrawDesktopChestResult(Rect r,ChestReward reward,Color accent)
        {
            float size=ChestRevealPresentation.DesktopArtSize(r.height);
            Rect art=new Rect(r.x,r.y,size,size);Fill(art,new Color(.025f,.045f,.07f));Border(art,accent);
            DrawChestCommittedReward(art,reward,accent);
            Rect details=new Rect(art.xMax+24,r.y,r.width-size-24,r.height);
            string result=ChestRevealPresentation.ResultWithCollection(reward,session.Progression.Profile);
            string error=session.Progression.LastError;
            string copy=(string.IsNullOrEmpty(error)?"":error+"\n\n")+result;
            float total=Mathf.Max(details.height,Style(18,true,true).CalcHeight(new GUIContent(copy),details.width-26)+20);
            desktopChestResultScroll=BeginTouchScroll("desktop-chest-result",details,desktopChestResultScroll,new Rect(0,0,details.width-16,total));
            Text(new Rect(4,8,details.width-26,total-16),copy,18,accent,true,true);EndTouchScroll();
        }
        private void DrawChestRevealTransition(Rect r,ChestReward reward,Rect destination)
        {
            float progress=ChestRevealPresentation.Progress(Time.unscaledTime-chestRevealedAt,ChestDuration);
            Color accent=reward!=null&&reward.Rarity.HasValue?GameBalance.RarityColor(reward.Rarity.Value):gold;
            float travel=ChestRevealPresentation.Travel(progress);
            Rect origin=chestRevealOrigin.width>0?chestRevealOrigin:destination;
            Rect moving=new Rect(Mathf.Lerp(origin.x,destination.x,travel),Mathf.Lerp(origin.y,destination.y,travel),Mathf.Lerp(origin.width,destination.width,travel),Mathf.Lerp(origin.height,destination.height,travel));
            DrawRewardChest(moving,true,1,progress);
            if(reward!=null&&reward.Duplicate&&reward.hasCurrencyDeltas)DrawChestResourceVisuals(moving,reward);
            if(progress>.35f){GUI.BeginGroup(moving);DrawRewardRadiance(new Rect(0,0,moving.width,moving.height),accent,progress);GUI.EndGroup();}
        }

        private void DrawChestGold(Rect area,Color accent)
        {
            float size=Mathf.Min(area.width,area.height),unit=size/200f;
            for(int i=0;i<3;i++)
            {Rect bar=new Rect(area.center.x-52*unit+(i-1)*8*unit,area.center.y+(1-i)*24*unit,104*unit,28*unit);Fill(bar,accent*(.65f+i*.12f));Border(bar,gold);}
            Text(new Rect(area.x,area.yMax-38*unit,area.width,28*unit),"金币已入账",Mathf.RoundToInt(16*unit),accent,true,false,TextAnchor.MiddleCenter);
        }

        private static bool CanTrialChestReward(ChestReward reward)
        {return reward!=null&&reward.Rarity.HasValue&&!reward.Duplicate&&reward.Slot.HasValue;}
        private bool AcceptChestForTrial()
        {
            var reward=session.Progression.LastChestReward;
            if(!session.Progression.Profile.pendingChestReveal||!ChestAnimationDone||!CanTrialChestReward(reward))return false;
            if(!session.Progression.AcknowledgeChestReward()){Feedback(false,"无法保存奖励确认");return false;}
            session.LogSystem(chestRevealResult);ResetChestReveal();panel=Panel.Fashion;session.SetUIBlocking(true);
            collectionOwner=session.Player;collectionViewing.Reset();TrialFashion(reward.Slot.Value,reward.Rarity.Value);mobileFashionPreview=true;BlockUITransition();return true;
        }

        private void FinishChestReveal()
        {
            if (!session.Progression.AcknowledgeChestReward()) { Feedback(false,"无法保存奖励确认"); return; }
            session.LogSystem(chestRevealResult);
            panel=Panel.None; session.SetUIBlocking(false); ResetChestReveal();
        }

        private void DrawRewardRadiance(Rect r, Color tint, float progress)
        {
            float strength=Mathf.Sin(Mathf.Clamp01((progress-.35f)/.65f)*Mathf.PI)*EffectPreferences.EffectsScale;
            int count=EffectPreferences.ReducedEffects?4:12;
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                float distance=23+progress*45;
                Vector2 p=r.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;
                Color c=new Color(tint.r,tint.g,tint.b,strength*.72f);
                Fill(new Rect(p.x-1,p.y-4,2,8),c);Fill(new Rect(p.x-4,p.y-1,8,2),c);
            }

        }

        private void DrawRewardChest(Rect r,bool opened,float opacity,float progress)
        {
            if(Event.current.type!=EventType.Repaint)return;
            int key=opened?Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01((progress-.12f)/.64f)*96),0,96):0;
            Texture2D texture=key==0?rewardChestClosed:rewardChestComposite;
            bool created=texture==null;
            if(created)
            {
                texture=new Texture2D(256,256,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                if(key==0)rewardChestClosed=texture;else rewardChestComposite=texture;
            }
            if(created || key==0&&rewardChestFrames[0]==null || key!=0&&chestCompositeKey!=key)
            {
                int lower=key/8,upper=Mathf.Min(12,lower+1),blend=key%8;
                if(rewardChestFrames[lower]==null)rewardChestFrames[lower]=BakeRewardChest(lower/12f);
                if(rewardChestFrames[upper]==null)rewardChestFrames[upper]=BakeRewardChest(upper/12f);
                if(chestCompositePixels==null)chestCompositePixels=new Color32[256*256];
                for(int i=0;i<chestCompositePixels.Length;i++)
                {uint c=ChestCompositeRules.Blend(rewardChestFrames[lower][i],rewardChestFrames[upper][i],blend);chestCompositePixels[i]=new Color32((byte)c,(byte)(c>>8),(byte)(c>>16),(byte)(c>>24));}
                texture.SetPixels32(chestCompositePixels);texture.Apply(false,false);if(key!=0)chestCompositeKey=key;
            }
            Color before=GUI.color;GUI.color=new Color(1,1,1,opacity);GUI.DrawTexture(r,texture,ScaleMode.ScaleToFit,true);GUI.color=before;
        }

        private static uint[] BakeRewardChest(float opening)
        {
            const int size = 256;
            bool opened = opening > .2f;
            Color[] pixels = new Color[size * size];
            // Soft radial light, rather than a flashing full-screen effect.
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size * .5f) / (size * .43f);
                float dy = (y - size * .52f) / (size * .45f);
                float a = Mathf.Pow(Mathf.Max(0, 1 - dx * dx - dy * dy), 3) * (opened ? .32f : .18f);
                pixels[y * size + x] = new Color(opened ? 1f : .25f, opened ? .71f : .74f, opened ? .31f : .78f, a);
            }
            Color trim = new Color(.91f, .72f, .39f), light = new Color(1, .88f, .59f);
            Color front = new Color(.12f, .25f, .29f), side = new Color(.07f, .15f, .20f), top = new Color(.19f, .35f, .37f);
            ChestPolygon(pixels, size, side, new Vector2(27, 85), new Vector2(79, 97), new Vector2(103, 81), new Vector2(51, 70));
            ChestPolygon(pixels, size, front, new Vector2(27, 63), new Vector2(79, 76), new Vector2(79, 99), new Vector2(27, 85));
            ChestPolygon(pixels, size, side, new Vector2(79, 76), new Vector2(103, 61), new Vector2(103, 84), new Vector2(79, 99));
            ChestPolygon(pixels, size, new Color(.045f, .07f, .09f), new Vector2(28, 61), new Vector2(54, 47), new Vector2(102, 60), new Vector2(79, 75));
            float lift = Mathf.SmoothStep(0,1,opening) * 24;
            ChestPolygon(pixels, size, top, new Vector2(25, 59 - lift), new Vector2(50, 43 - lift), new Vector2(105, 57 - lift), new Vector2(79, 73 - lift));
            ChestPolygon(pixels, size, front, new Vector2(25, 59 - lift), new Vector2(79, 73 - lift), new Vector2(79, 81 - lift), new Vector2(25, 67 - lift));
            ChestPolygon(pixels, size, side, new Vector2(79, 73 - lift), new Vector2(105, 57 - lift), new Vector2(105, 65 - lift), new Vector2(79, 81 - lift));
            CrestStroke(pixels, size, 25, 59 - lift, 79, 73 - lift, light, 1.5f);
            CrestStroke(pixels, size, 79, 73 - lift, 105, 57 - lift, trim, 1.5f);
            CrestStroke(pixels, size, 25, 67 - lift, 79, 81 - lift, trim, 1.2f);
            CrestStroke(pixels, size, 79, 81 - lift, 105, 65 - lift, trim, 1.2f);
            CrestStroke(pixels, size, 27, 85, 79, 99, trim, 1.2f);
            CrestStroke(pixels, size, 79, 99, 103, 84, trim, 1.2f);
            CrestStroke(pixels, size, 34, 67, 34, 85, trim, 2.8f);
            CrestStroke(pixels, size, 71, 77, 71, 95, trim, 2.8f);
            CrestStroke(pixels, size, 95, 69, 95, 86, trim, 2.6f);
            CrestStroke(pixels, size, 37, 56 - lift, 91, 69 - lift, trim, 2.1f);
            CrestStroke(pixels, size, 45, 50 - lift, 99, 63 - lift, trim, 2.1f);
            if(opening < .5f)
            {
                float shift=opening*14;
                ChestPolygon(pixels, size, trim, new Vector2(49,72+shift),new Vector2(59,75+shift),new Vector2(59,87+shift),new Vector2(49,84+shift));
                ChestPolygon(pixels,size,new Color(.43f,1,.84f),new Vector2(54,75+shift),new Vector2(57,81+shift),new Vector2(54,85+shift),new Vector2(51,79+shift));
            }
            if (opened)
            {
                for (int i = 0; i < 9; i++)
                {
                    float x = 33 + (i * 17 % 62), y = 35 + (i * 19 % 29);
                    CrestStroke(pixels, size, x - 1, y, x + 1, y, light, 1);
                    CrestStroke(pixels, size, x, y - 2, x, y + 2, light, .8f);
                }
            }
            var result=new uint[pixels.Length];
            for(int i=0;i<pixels.Length;i++){Color32 c=pixels[i];result[i]=(uint)c.r|((uint)c.g<<8)|((uint)c.b<<16)|((uint)c.a<<24);}
            return result;
        }

        private static void ChestPolygon(Color[] pixels, int size, Color color, params Vector2[] points)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + .5f) * 128 / size, (y + .5f) * 128 / size);
                bool inside = false;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    if ((points[i].y > p.y) != (points[j].y > p.y) && p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[i].x) inside = !inside;
                if (inside) pixels[(size - y - 1) * size + x] = color;
            }
        }
    }
}
