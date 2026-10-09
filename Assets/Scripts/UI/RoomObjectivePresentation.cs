using System.Globalization;
namespace Emberfall
{
    // Shared, compact objective wording. No Unity or input side effects.
    public sealed class RoomObjectivePresentation
    {
        public readonly string Title, ProgressText, Hint;
        public readonly float Fraction;
        public string SupportHint {get;private set;}
        private RoomObjectivePresentation(string title,string progress,string hint,float fraction)
        {Title=title;ProgressText=progress;Hint=hint;Fraction=fraction;}
        public static RoomObjectivePresentation Create(RoomChainState run,bool inside,bool contested,bool paused,bool supportActive)
        {
            // Compatibility for callers migrating from a supplier-alive boolean:
            // it cannot tell whether any individual enemy is actually receiving aid.
            var result=Build(run,inside,contested?1:0,paused);
            result.SupportHint=run.Room.Index<3&&!run.Finished?(supportActive?"供能者存活 · 6米可见才受援":"供能者已倒 · 护援已断"):"";
            return result;
        }
        public static RoomObjectivePresentation Create(RoomChainState run,bool inside,int contestants,bool paused,RoomSupportSnapshot support)
        {
            var result=Build(run,inside,contestants,paused);
            result.SupportHint=run.Room.Index<3&&!run.Finished?support.Hint:"";
            return result;
        }
        private static RoomObjectivePresentation Build(RoomChainState run,bool inside,int contestants,bool paused)
        {
            string title=RoomTactics.Name(run.Room.Objective)+" "+(run.Room.Index+1)+"/5";
            if(run.Finished)return new RoomObjectivePresentation(title,run.Failed?"远征失败":"远征完成",run.Failed?"返回营地再挑战":"领取结算后返回",run.Failed?0:1);
            if(run.DoorUnlocked)return new RoomObjectivePresentation(title,"北门已开","留下清场或前往北门",1);
            if(run.Room.Interlude)return new RoomObjectivePresentation(title,"选择一项祝福","确认后北门开启",0);
            if(run.Room.Boss)return new RoomObjectivePresentation(title,"击败首领与护卫","观察首领预警与锚点",0);
            if(run.Room.Objective==RoomObjective.Hunt)return new RoomObjectivePresentation(title,"击败金环魔灵","6米护援 · 遮挡可切断",0);
            bool purify=run.Room.Objective==RoomObjective.Purify;
            string progress=(purify?"双印 "+run.Seals+"/2 · ":"占领 ")+(purify?run.SealProgress(0)+run.SealProgress(1):run.Progress).ToString("0.0",CultureInfo.InvariantCulture)+(purify?"/4秒":"/2.5秒");
            string hint=paused?"已暂停 · 保留进度":contestants>0?"争夺"+contestants+"敌·双环标记·保留进度":inside?"正在累积 · 保持站位":purify?"任选A/B · 各累计2秒":"站入北门金环 · 累计2.5秒";
            float fraction=run.CaptureFraction;
            return new RoomObjectivePresentation(title,progress,hint,fraction);
        }
    }
}
