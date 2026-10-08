// Executes the actual goal UI partial and real progression service. GUI/layout shells
// deliver an explicit click; they do not emulate Unity rendering or touch routing.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public struct Rect{public float x,y,width,height;public float xMax=>x+width;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
 public struct Vector2{public static Vector2 zero=>new Vector2();}
 public static class Mathf{public static int RoundToInt(float n)=>(int)Math.Round(n);public static float Ceil(float n)=>(float)Math.Ceiling(n);public static float Max(float a,float b)=>Math.Max(a,b);}
 public class GUIContent{public string text;public GUIContent(string value){text=value;}}
 public class GUIStyle{public float CalcHeight(GUIContent c,float width)=>20*(1+c.text.Length/Math.Max(1,(int)(width/10)));}
}
namespace Emberfall
{
 public static class MobileControls{public static bool Active=true;}
 public sealed partial class GameUI
 {
  bool MerchantServiceActive=>true;enum Panel{None,Skills,Camp,Inventory} Panel panel=Panel.Camp;
  sealed class Context{public ProgressionService Progression;public bool IsInCamp=true,Blocked;public void SetUIBlocking(bool v){Blocked=v;}}
  Context session;float TouchRatio=1,width=568,height=320;Color jade,pale,gold,muted;
  List<Rect> blockedRects=new List<Rect>();List<string> shown=new List<string>();string click;int clickReforgeOption=-1;bool clickReforgeExecute;int presetOpened;
  void DrawPrice(Rect r,int amount,bool materials,float u){shown.Add(amount.ToString());}void CancelMobileScroll(){}void BlockUITransition(){}void Fill(Rect r,Color c){}void Box(Rect r,Color c,bool b){}
  void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false){shown.Add(s);}
  GUIStyle Style(int size,bool bold,bool wrap)=>new GUIStyle();
  Rect BuildPlanRect(MobilePanelLayout.Area r,float u)=>new Rect(r.X*u,r.Y*u,r.Width*u,r.Height*u);
  Vector2 BeginTouchScroll(string key,Rect r,Vector2 scroll,Rect content)=>scroll;void EndTouchScroll(){}
  bool Button(Rect r,string s,Color c,bool enabled=true,string hint=null){shown.Add(s);if(s==""&&enabled){if(clickReforgeOption>=0&&r.x==4&&r.y==4+50*clickReforgeOption&&r.height==44){clickReforgeOption=-1;return true;}if(clickReforgeExecute&&r.height==48){clickReforgeExecute=false;return true;}}if(enabled&&click!=null&&s.StartsWith(click)){click=null;return true;}return false;}
  int buildPlanDetails,skillSection,merchantNavigations,smithNavigations;void NavigateMerchantExchange(){merchantNavigations++;}void NavigateSmithAttachment(ProgressionGoalState g){smithNavigations++;}
  bool DrawAutomaticGrowthSurface()=>throw new Exception("Automatic surface is outside this manual-goal fixture");
  bool NavigationButton(Rect r,string s,Color c,bool enabled=true,string hint=null)=>Button(r,s,c,enabled,hint);
  bool PrimaryButton(Rect r,string s,Color c,bool enabled=true,string hint=null)=>Button(r,s,c,enabled,hint);
  bool TabButton(Rect r,string s,bool selected,bool enabled=true,string hint=null)=>Button(r,s,jade,enabled,hint);
  void Feedback(bool ok,string text){if(!ok)throw new Exception(session.Progression.LastError);}void OpenBuildPlans(){presetOpened++;}
private void DrawCombatTrialGoal(ref float y,float width,float unit,bool draw)
        {
            var p=session.Progression;
            GoalNode(ref y,width,unit,"实战试炼 · 目标的一种","进度沿用已有存档；达成职业练习的成长奖励只发放一次。",draw);
            string[] actions={"普攻命中，回复能量","躲过一次即将命中的预警攻击",p.ClassTutorialText,"在行囊换上一件装备"};
            for(int i=0;i<actions.Length;i++)
            {
                bool done=i==2?p.Profile.classTutorialCompleted:(p.Profile.tutorialMask&(1<<i))!=0;
                GoalParagraph(ref y,width,unit,(done?"✓ ":"○ ")+actions[i],14,done?jade:pale,false,draw);
            }
        }
private void PerformGoalAction(ProgressionGoalState goal)
        {
            if(goal.Action==ProgressionGoalAction.ClaimCore||goal.Action==ProgressionGoalAction.ExchangeCore)
            {NavigateMerchantExchange();return;}
            if(goal.Action==ProgressionGoalAction.UnlockVariant||goal.Action==ProgressionGoalAction.Ascend||goal.Action==ProgressionGoalAction.UpgradeAttachment)
            {NavigateSmithAttachment(goal);return;}
            if(goal.Action==ProgressionGoalAction.OpenPresets){progressionGoalsOpen=false;panel=Panel.Skills;skillSection=1;OpenBuildPlans();buildPlanDetails=1;return;}
            Feedback(session.Progression.ExecuteProgressionGoal(goal.ActionIdentity,session.IsInCamp),"目标操作已保存");
        }
  public static string VerifyMilestones(string root){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   var p=new ProgressionService(Path.Combine(root,"milestones"));check(p.CreateNewSlot(HeroClass.Arcanist),"create persisted character");p.Profile.highestAdventureTier=40;
   check(p.SelectCoreGoal(EquipmentMechanic.CinderTrail),"initial explicit core");var ui=new GameUI{session=new Context{Progression=p}};
   string profile=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);int changed=0;p.Changed+=()=>changed++;
   ui.OpenProgressionGoals();ui.DrawProgressionGoalSurface();ui.CloseProgressionGoalSurface();check(ui.panel==Panel.None&&!ui.session.Blocked,"goal X resumes adventure");ui.OpenProgressionGoals();ui.DrawProgressionGoalSurface();
   check(profile==JsonUtility.ToJson(p.Profile,true)&&disk==File.ReadAllText(p.SaveFilePath)&&changed==0,"opening drawing and reopening make no writes or replacements");
   check(!p.SelectedProgressionGoal(true).Done,"tier40 does not invent selected core ownership");
   foreach(string label in new[]{"10阶节点","20阶节点","40阶节点"})check(ui.shown.Exists(x=>x.StartsWith(label)),"all optional reasons visible independent of current tier");
   ui.click="保存第二套配装";ui.DrawProgressionGoalOptions(520,1,true);
   check(changed==1&&p.Profile.progressionGoal==ProgressionGoalKind.SecondPreset,"only clicked candidate commits selected goal");
   var second=p.SelectedProgressionGoal(true);check(!second.Done&&second.Action==ProgressionGoalAction.OpenPresets,"tier40 never substitutes for both actual presets");
   ui.click=second.ActionLabel;ui.DrawProgressionGoalSurface();check(ui.presetOpened==1&&changed==1,"existing shared action opens presets without claiming completion");
   ui.click="自愿挑战 · 通关第40阶";ui.DrawProgressionGoalOptions(520,1,true);
   check(changed==2&&p.SelectedProgressionGoal(true).Done&&p.Profile.progressionGoalTier==40,"explicit tier goal uses actual tier completion");
   p.Profile.highestAdventureTier=20;check(!p.SelectedProgressionGoal(true).Done,"tier progress below target remains unfinished");
   var item=p.CreateMechanicItem(EquipmentMechanic.FrostEcho);check(p.CollectLoot(item),"collect actual variant candidate");p.Profile.mechanicMaterials=0;
   ui.click="解锁变体 · ";ui.DrawProgressionGoalOptions(520,1,true);var target=p.SelectedProgressionGoal(true);
   check(target.ItemId==item.id&&!target.Done&&!target.CanAct&&target.MaterialCost==ProgressionService.VariantCost,"clicked item keeps stable identity and real material eligibility");
   int before=changed;ui.OpenProgressionGoals();ui.click=target.ActionLabel;ui.DrawProgressionGoalSurface();check(changed==before&&!p.Profile.inventory.Find(x=>x.id==item.id).mechanicVariantUnlocked,"disabled action cannot execute or fabricate reward");
   p.Profile.mechanicMaterials=20;p.Save();string owned=JsonUtility.ToJson(p.Profile,true);
   ui.OpenProgressionGoals();ui.click=p.SelectedProgressionGoal(true).ActionLabel;ui.DrawProgressionGoalSurface();
   check(ui.smithNavigations==1&&JsonUtility.ToJson(p.Profile,true)==owned,"variant goal guides to smith without bypassing material transaction");
   check(p.SelectCoreGoal(EquipmentMechanic.CinderTrail),"select unowned merchant goal");owned=JsonUtility.ToJson(p.Profile,true);
   ui.OpenProgressionGoals();ui.click=p.SelectedProgressionGoal(true).ActionLabel;ui.DrawProgressionGoalSurface();
   check(ui.merchantNavigations==1&&JsonUtility.ToJson(p.Profile,true)==owned,"core goal guides to merchant without directly granting or charging");
   return "PASS: "+n+" actual milestone goal surface/persistence assertions";
  }
 }
}
