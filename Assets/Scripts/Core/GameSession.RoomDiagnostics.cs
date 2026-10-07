using System;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        public string RoomGenerationFailureDetail {get;private set;}
        private string roomGenerationStage;
        private int roomGenerationSlot=-1;
        private void ResetRoomGenerationDiagnostics()
        {RoomGenerationFailureDetail=null;roomGenerationStage="开始远征";roomGenerationSlot=-1;}
        private void RoomGenerationStage(string stage,int slot=-1)
        {roomGenerationStage=stage;roomGenerationSlot=slot;}
        private void FailRoomGeneration(string reason,Exception error=null)
        {
            if(RoomChainRun==null)return;
            var plan=RoomChainRun.Room;
            string location="房间 "+(plan==null?0:plan.Index+1)+" · "+(plan==null||plan.Branch==RoomBranch.None?"主路":plan.Branch==RoomBranch.Seal?"守印":"断供")+(roomGenerationSlot<0?"":" · 敌人 "+(roomGenerationSlot+1));
            RoomGenerationFailureDetail=reason+"\n发生位置："+roomGenerationStage+"\n"+location;
            string diagnostic="生成失败 · "+roomGenerationStage+" · "+reason+" · 种子 "+runSeed+" / "+location+" / 布局 "+(plan==null?-1:plan.Layout);
            if(error!=null)diagnostic+=" · "+error.GetType().Name;
            RoomChainRun.Fail(RoomFailureReason.GenerationOrPathFailure);
            LogSystem(diagnostic);
            Debug.LogError(diagnostic);
            if(error!=null)Debug.LogException(error);
            FinalizeRoomChain();
            Notify(reason+"；可原条件重试或返回营地。");
        }
    }
}
