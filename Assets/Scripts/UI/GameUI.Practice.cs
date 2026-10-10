using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Panel practiceReturnPanel;
        private bool practicePanelCaptured;
        internal void EnterPracticePanel(){if(practicePanelCaptured)return;practiceReturnPanel=panel;practicePanelCaptured=true;panel=Panel.None;CancelMobileScroll();CancelMobileCast();CancelHotbarPointer();}
        internal void LeavePracticePanel(){if(!practicePanelCaptured)return;panel=practiceReturnPanel;practicePanelCaptured=false;}
        private int practiceSeconds=10;
        private bool practiceShowConfiguration;
        private bool practiceChoicesOpen;
        private void DrawPracticeChoices(ref float y,float width,float unit,bool draw,bool enabled,ProgressionService.BuildDraft draft)
        {
            // Disclosure changes presentation only; the session owns results and the pinned baseline.
            DraftButton(ref y,width,unit,practiceChoicesOpen?"收起营地试招":"展开营地试招 · 准备 / 结果",true,draw,()=>
            {practiceChoicesOpen=!practiceChoicesOpen;CancelMobileScroll();BlockUITransition();});
            if(!practiceChoicesOpen)return;
            BuildPlanParagraph(ref y,width,unit,"试招的基准 A / 本轮 B 是结果对比。折叠不会清除结果或固定基准。",muted,draw);
            BuildPlanParagraph(ref y,width,unit,"营地试招 · 临时角色，无奖励。准备后开始计时；旧三场为百万生命持续训练靶；新增受压场景使用当前等级真实生命并实际攻击。所有场景保留护甲与控制规则。",muted,draw);
            DraftButton(ref y,width,unit,"计时上限："+practiceSeconds+"秒 · 点击切换10/60秒",enabled,draw,()=>practiceSeconds=practiceSeconds==10?60:10);
            for(int i=0;i<5;i++){int index=i;DraftButton(ref y,width,unit,"准备 · "+CampPracticeRecord.ScenarioLabel((CampPracticeScenario)i),enabled,draw,()=>session.BeginPractice((CampPracticeScenario)index,practiceSeconds,draft));}
            var record=session.PracticeRecord;if(record==null||!record.Finished)return;
            var baseline=session.PreviousPracticeRecord;
            BuildPlanParagraph(ref y,width,unit,baseline==null?"尚未固定基准 A":record.Comparison(baseline),gold,draw,true);
            BuildPlanParagraph(ref y,width,unit,"A 固定，后续更新 B。造成扣血的施法按整次计数，多段不重复。治疗看有效治疗；伙伴伤害计入总伤害，触发看机制次数。此比例不判断治疗或召唤是否生效，不是箭矢命中率。",muted,draw);
            DrawPracticeResultRow(ref y,width,unit,new PracticeResultPresentation.Row("实测项目","固定基准 A","本轮 B"),draw,true);
            foreach(var row in new PracticeResultPresentation(baseline,record).Rows)DrawPracticeResultRow(ref y,width,unit,row,draw,false);
            DraftButton(ref y,width,unit,"将本轮结果固定为基准 A",!session.PracticeActive,draw,()=>session.PinPracticeBaseline());
            DraftButton(ref y,width,unit,practiceShowConfiguration?"收起 A / B 冻结配装摘要":"展开 A / B 冻结配装摘要",true,draw,()=>practiceShowConfiguration=!practiceShowConfiguration);
            if(practiceShowConfiguration)
            {
                if(baseline!=null)BuildPlanParagraph(ref y,width,unit,"基准 A · "+baseline.ConfigurationSummary,muted,draw);
                BuildPlanParagraph(ref y,width,unit,"本轮 B · "+record.ConfigurationSummary,muted,draw);
            }
        }
        private void DrawPracticeResultRow(ref float y,float width,float unit,PracticeResultPresentation.Row row,bool draw,bool heading)
        {
            float labelWidth=(width-32)*.38f,valueWidth=(width-32)*.31f;
            int size=Mathf.RoundToInt(13*unit);
            float h=Mathf.Max(28,Mathf.Max(Style(size,heading,true).CalcHeight(new GUIContent(row.Label),labelWidth*unit),Mathf.Max(Style(size,heading,true).CalcHeight(new GUIContent(row.A),valueWidth*unit),Style(size,heading,true).CalcHeight(new GUIContent(row.B),valueWidth*unit)))/unit+8);
            if(draw)
            {
                Text(new Rect(8*unit,y*unit,labelWidth*unit,h*unit),row.Label,size,heading?gold:muted,heading,true);
                Text(new Rect((16+labelWidth)*unit,y*unit,valueWidth*unit,h*unit),row.A,size,pale,heading,true);
                Text(new Rect((24+labelWidth+valueWidth)*unit,y*unit,valueWidth*unit,h*unit),row.B,size,pale,heading,true);
            }
            y+=h+4;
        }
        private void DrawPracticeCombatHUD()
        {
            DrawComboCounter();
            if(MobileControls.Active)DrawMobileHotbar();else DrawHotbar();
            DrawCompanionCommands();DrawChargeProgress();DrawTargetingHint();
        }
        private void DrawPracticeOverlay()
        {
            var r=session.PracticeRecord;float unit=MobileControls.Active?TouchRatio:1;
            var layout=new PracticeHudLayout(width/unit,unit*scale);
            string header=CampPracticeRecord.ScenarioLabel(r.Scenario)+"\n"+(r.Started?"剩余 "+Mathf.Max(0,r.Duration-r.Elapsed).ToString("0.0")+"秒":"准备 · "+r.Duration+"秒上限");
            Text(BuildPlanRect(layout.Header,unit),header,Mathf.RoundToInt(13*unit),pale,true,true);
            string side="HP "+session.Player.Health.ToString("0")+" / "+session.Player.MaxHealth.ToString("0")+" · 能量 "+session.Player.Energy.ToString("0")+"\n伤害 "+r.ActualDamage.ToString("0")+" · DPS "+r.DamagePerSecond.ToString("0.0")+"\n耗能 "+r.EnergySpent.ToString("0.0")+" / 回复 "+r.EnergyRestored.ToString("0.0")+"\n受伤 "+r.DamageTaken.ToString("0")+" / 治疗 "+r.EffectiveHealing.ToString("0");
            Text(BuildPlanRect(layout.Sidebar,unit),side,Mathf.RoundToInt(11*unit),pale,false,true);
            Rect primary=BuildPlanRect(layout.Primary,unit),leave=BuildPlanRect(layout.Leave,unit);blockedRects.Add(primary);blockedRects.Add(leave);
            if(PrimaryButton(primary, r.Started?"重开":"开始", jade)){if(r.Started)session.RestartPractice();else session.StartPractice();}
            if(Button(leave,"结束",gold))session.EndPractice("主动离开 · 记录提前结束");
        }
    }
}
