using System;
namespace Emberfall
{
    public enum RoomBranch { None, Seal, Supply }
    public enum RoomFailureReason { None, Death, Timeout, Abandoned, GenerationOrPathFailure }
    public sealed class RoomChainPlan
    {
        public readonly int Index, EnemyCount, Layout, Seed;
        public readonly bool Interlude, Boss;
        public readonly RoomObjective Objective;
        public readonly RoomBranch Branch;
        public RoomChainPlan(int index, int seed = 0, RoomBranch branch = RoomBranch.None)
        {
            Index = index; Seed = seed; Branch = index==2?branch:RoomBranch.None;
            Objective = Branch==RoomBranch.Seal?RoomObjective.Purify:Branch==RoomBranch.Supply?RoomObjective.Hunt:RoomTactics.Objective(seed,index);
            Layout = index < 4 ? 20 + RoomTactics.Terrain(seed,index)*2 + (RoomTactics.Mirror(seed)<0?1:0) : 10+index;
            if(Branch!=RoomBranch.None)Layout=20+(Branch==RoomBranch.Seal?0:4)+(RoomTactics.Mirror(seed)<0?1:0);
            Interlude = false; Boss = index == 4; EnemyCount = Interlude ? 0 : Boss ? 3 : Branch==RoomBranch.Seal?4:6;
        }
    }
    public sealed class RoomChainState
    {
        public RoomChainPlan Room {get;private set;}
        public bool DoorUnlocked {get;private set;}
        public bool Finished {get;private set;}
        public bool Failed {get;private set;}
        public RoomFailureReason Failure {get;private set;}
        public bool RewardClaimed {get;private set;}
        public int Seals {get;private set;}
        private float progress;
        private readonly float[] sealProgress=new float[2];
        public float Progress {get{return Room!=null&&Room.Objective==RoomObjective.Purify?SealProgress(SealComplete(0)?1:0):progress;}}
        public float SealProgress(int index){return index>=0&&index<2?sealProgress[index]:0;}
        public bool SealComplete(int index){return index>=0&&index<2&&sealProgress[index]>=2f;}
        public float CaptureFraction {get{return Room!=null&&Room.Objective==RoomObjective.Purify?(sealProgress[0]+sealProgress[1])/4f:progress/2.5f;}}
        private bool[] spawned, defeated;
        private int spawnedCount, kills;
        public RoomBranch SelectedBranch {get;private set;}
        public bool BranchChoiceOpen {get;private set;}
        public RoomChainState(int seed=0,RoomBranch branch=RoomBranch.None) { SelectedBranch=branch==RoomBranch.Seal||branch==RoomBranch.Supply?branch:RoomBranch.None;SetRoom(new RoomChainPlan(0,seed)); }
        public bool OpenBranchChoice(){if(Finished||Room==null||Room.Index!=1||!DoorUnlocked||SelectedBranch!=RoomBranch.None||BranchChoiceOpen)return false;BranchChoiceOpen=true;return true;}
        public void CancelBranchChoice(){BranchChoiceOpen=false;}
        public bool SelectBranch(RoomBranch branch){if(!BranchChoiceOpen||Finished||(branch!=RoomBranch.Seal&&branch!=RoomBranch.Supply))return false;SelectedBranch=branch;BranchChoiceOpen=false;return true;}
        private void SetRoom(RoomChainPlan plan)
        {
            Room=plan; spawned=new bool[plan.EnemyCount]; defeated=new bool[plan.EnemyCount];
            spawnedCount=kills=Seals=0; progress=0; Array.Clear(sealProgress,0,2); DoorUnlocked=false;
        }
        public bool Register(RoomChainPlan plan,int index)
        { if(Finished||!ReferenceEquals(plan,Room)||index<0||index>=spawned.Length||spawned[index])return false;spawned[index]=true;spawnedCount++;return true; }
        public bool Defeat(RoomChainPlan plan,int index)
        {
            if(Finished||!ReferenceEquals(plan,Room)||index<0||index>=spawned.Length||!spawned[index]||defeated[index])return false;
            defeated[index]=true;kills++;
            if(Room.Boss && kills==Room.EnemyCount && spawnedCount==Room.EnemyCount)Finished=true;
            if(Room.Objective==RoomObjective.Hunt && defeated[0] && spawnedCount==Room.EnemyCount)DoorUnlocked=true;
            if(Room.Index<2&&Room.Branch==RoomBranch.None&&kills==Room.EnemyCount&&spawnedCount==Room.EnemyCount)DoorUnlocked=true;
            return true;
        }
        // No deadline or reinforcements: clearing enemies always leaves a safe way to finish.
        // Leaving a seal pauses progress; it never erases a mobile player's partial capture.
        private bool CanCapture(float delta,bool active,bool inside,bool contested)
        {return active&&!Finished&&!DoorUnlocked&&spawnedCount==Room.EnemyCount&&inside&&!contested&&!float.IsNaN(delta)&&!float.IsInfinity(delta)&&delta>0;}
        public void AdvanceSeal(int index,float delta,bool active,bool inside,bool contested)
        {
            if(Room==null||Room.Objective!=RoomObjective.Purify||index<0||index>=2||SealComplete(index)||!CanCapture(delta,active,inside,contested))return;
            sealProgress[index]=Math.Min(2f,sealProgress[index]+Math.Min(delta,.25f));
            if(sealProgress[index]+.00001f<2f)return;
            sealProgress[index]=2f;Seals++;if(Seals==2)DoorUnlocked=true;
        }
        public void Advance(float delta,bool active,bool inside,bool contested)
        {
            if(Room==null)return;
            // Compatibility only: the live host addresses each physical seal explicitly.
            if(Room.Objective==RoomObjective.Purify){AdvanceSeal(SealComplete(0)?1:0,delta,active,inside,contested);return;}
            if(Room.Objective!=RoomObjective.Escape||!CanCapture(delta,active,inside,contested))return;
            progress=Math.Min(2.5f,progress+Math.Min(delta,.25f));
            if(progress<2.5f)return;
            progress=0;Seals=1;DoorUnlocked=true;
        }
        public bool ChooseInterlude(){if(Finished||!Room.Interlude||DoorUnlocked)return false;DoorUnlocked=true;return true;}
        public bool Next(bool nearDoor,bool blocked)
        {if(Finished||!DoorUnlocked||!nearDoor||blocked||Room.Index>=4||BranchChoiceOpen||Room.Index==1&&SelectedBranch==RoomBranch.None)return false;SetRoom(new RoomChainPlan(Room.Index+1,Room.Seed,SelectedBranch));return true;}
        public bool ClaimReward(bool durable){if(!Finished||Failed||RewardClaimed||!durable)return false;RewardClaimed=true;return true;}
        public void Fail(RoomFailureReason reason=RoomFailureReason.Abandoned){if(Finished)return;Failure=reason==RoomFailureReason.None?RoomFailureReason.Abandoned:reason;Failed=Finished=true;DoorUnlocked=false;BranchChoiceOpen=false;}
        public void Dispose(){if(!Finished)Failure=RoomFailureReason.Abandoned;Failed=Finished=true;DoorUnlocked=false;BranchChoiceOpen=false;Room=null;spawned=defeated=new bool[0];}
    }
}
