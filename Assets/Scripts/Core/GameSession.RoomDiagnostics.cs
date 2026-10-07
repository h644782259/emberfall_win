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
            RoomGenerationFailureDetail="生成失败 · "+roomGenerationStage+" · "+reason+
                " · 种子 "+runSeed+" / 房间 "+(plan==null?0:plan.Index+1)+
                " / 布局 "+(plan==null?-1:plan.Layout)+" / "+(plan==null||plan.Branch==RoomBranch.None?"主路":plan.Branch==RoomBranch.Seal?"守印":"断供")+(roomGenerationSlot<0?"":" / 敌人 "+(roomGenerationSlot+1));
            if(error!=null)RoomGenerationFailureDetail+=" · "+error.GetType().Name;
            RoomChainRun.Fail(RoomFailureReason.GenerationOrPathFailure);
            LogSystem(RoomGenerationFailureDetail);
            Debug.LogError(RoomGenerationFailureDetail);
            if(error!=null)Debug.LogException(error);
            FinalizeRoomChain();
            Notify(RoomGenerationFailureDetail);
        }
    }
}
