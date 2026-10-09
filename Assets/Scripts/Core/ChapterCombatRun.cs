using System;
namespace Emberfall
{
    // Per-attempt combat only; durable unlocks and reward receipts belong to ProgressionService.
    public sealed class ChapterCombatRun
    {
        public ChapterNode Node {get;private set;}
        public ChapterDifficulty Difficulty {get;private set;}
        public int Seed {get;private set;}
        public int RoomIndex {get;private set;}
        public int Epoch {get;private set;}
        public int Seals {get;private set;}
        public float Progress {get{return Objective==RoomObjective.Purify?SealProgress(SealComplete(0)?1:0):progress;}}
        private float progress;
        private readonly float[] sealProgress=new float[2];
        public float SealProgress(int index){return index>=0&&index<2?sealProgress[index]:0;}
        public bool SealComplete(int index){return index>=0&&index<2&&sealProgress[index]>=2f;}
        public bool DoorUnlocked {get;private set;}
        public bool Finished {get;private set;}
        public bool Failed {get;private set;}
        public bool RewardClaimed {get;private set;}
        public RoomObjective Objective {get{return ChapterDefinition.RoomKind(Node,RoomIndex);}}
        public int EnemyCount {get{return Objective==RoomObjective.Rest?0:Objective==RoomObjective.Boss?3:6;}}
        public int LivingRegisteredEnemies {get{return registered-kills;}}
        private int registered,kills;
        private readonly bool[] spawned=new bool[6],defeated=new bool[6];
        public ChapterCombatRun(ChapterNode node,ChapterDifficulty difficulty,int seed)
        {ChapterDefinition.Get(node);if((int)difficulty<0||(int)difficulty>2)throw new ArgumentOutOfRangeException(nameof(difficulty));Node=node;Difficulty=difficulty;Seed=seed;Epoch=-1;}
        public void BindRoom(int epoch)
        {if(Finished)return;Epoch=epoch;registered=kills=Seals=0;progress=0;Array.Clear(sealProgress,0,2);DoorUnlocked=Objective==RoomObjective.Rest;Array.Clear(spawned,0,6);Array.Clear(defeated,0,6);}
        public bool Register(int room,int epoch,int index)
        {if(Finished||Epoch<0||room!=RoomIndex||epoch!=Epoch||index<0||index>=EnemyCount||spawned[index])return false;spawned[index]=true;registered++;return true;}
        public bool Defeat(int room,int epoch,int index)
        {
            if(Finished||Epoch<0||room!=RoomIndex||epoch!=Epoch||index<0||index>=EnemyCount||!spawned[index]||defeated[index])return false;
            defeated[index]=true;kills++;
            if(registered==EnemyCount&&((Objective==RoomObjective.Hunt&&defeated[0])||(Objective==RoomObjective.Boss&&kills==EnemyCount)))DoorUnlocked=true;
            return true;
        }
        private bool CanAdvance(float delta,bool active,bool inside,bool contested)
        {return !Finished&&!DoorUnlocked&&active&&inside&&!contested&&registered==EnemyCount&&!float.IsNaN(delta)&&!float.IsInfinity(delta)&&delta>0;}
        public void AdvanceSeal(int index,float delta,bool active,bool inside,bool contested)
        {
            if(Objective!=RoomObjective.Purify||index<0||index>=2||SealComplete(index)||!CanAdvance(delta,active,inside,contested))return;
            sealProgress[index]=Math.Min(2f,sealProgress[index]+Math.Min(delta,.25f));
            if(sealProgress[index]+.00001f<2f)return;
            sealProgress[index]=2f;Seals++;if(Seals==2)DoorUnlocked=true;
        }
        public void Advance(float delta,bool active,bool inside,bool contested)
        {
            // Legacy hosts retain ordered purification; chapter hosts address either seal explicitly.
            if(Objective==RoomObjective.Purify){AdvanceSeal(SealComplete(0)?1:0,delta,active,inside,contested);return;}
            if(Objective!=RoomObjective.Escape||!CanAdvance(delta,active,inside,contested))return;
            progress=Math.Min(2.5f,progress+Math.Min(delta,.25f));
            if(progress+.00001f>=2.5f){progress=2.5f;Seals=1;DoorUnlocked=true;}
        }
        public bool Exit(bool near,bool blocked)
        {if(Finished||!DoorUnlocked||!near||blocked)return false;if(RoomIndex>=ChapterDefinition.RoomCount(Node)-1){Finished=true;return true;}RoomIndex++;Epoch=-1;DoorUnlocked=false;return true;}
        public bool FinishBoss(){if(Finished||Objective!=RoomObjective.Boss||!DoorUnlocked)return false;Finished=true;return true;}
        public void ClaimReward(){if(Finished&&!Failed)RewardClaimed=true;}
        public void Fail(){if(Finished)return;Finished=Failed=true;DoorUnlocked=false;}
    }
}
