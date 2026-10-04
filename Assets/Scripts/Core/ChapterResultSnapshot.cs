namespace Emberfall
{
    // Attempt-local evidence; never fabricates durable profile progress on a failed write.
    public sealed class ChapterResultSnapshot
    {
        public readonly int EmberCreated,EmberEffective,FrostCreated,FrostEffective;
        public readonly ChapterNode Node;
        public readonly ChapterDifficulty Difficulty;
        public readonly int Tier,EntryPotions,Room,Seals,KillExperience;
        public int CompletionExperience {get;private set;}
        public readonly bool LimitedHealing,Failed;
        public readonly float FirstSealSeconds,SecondSealSeconds,LastHitAmount;
        public readonly string Failure,LastHit;
        public bool Saved {get;private set;}
        public int Materials {get;private set;}
        public bool RewardDetailsUnavailable {get;private set;}
        public void RecordSavedUnavailable(){if(Saved||Failed)return;RewardDetailsUnavailable=true;Saved=true;}
        public bool FirstCompletion {get;private set;}
        public bool FirstCoreAvailable {get;private set;}
        public int UnlockedNode {get;private set;}=-1;
        public int UnlockedDifficulty {get;private set;}=-1;
        public int SharedBefore {get;private set;}
        public int SharedAfter {get;private set;}
        public ChapterResultSnapshot(ChapterNode node,ChapterDifficulty difficulty,int tier,int potions,bool limited,int room,int seals,float first,float second,bool failed,string failure,string lastHit,float lastHitAmount,int killExperience,int[] mechanismCounts=null)
        {EmberCreated=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[0]:0;EmberEffective=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[1]:0;FrostCreated=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[2]:0;FrostEffective=mechanismCounts!=null&&mechanismCounts.Length==4?mechanismCounts[3]:0;Node=node;Difficulty=difficulty;Tier=tier;EntryPotions=potions;LimitedHealing=limited;Room=room;Seals=seals;FirstSealSeconds=first;SecondSealSeconds=second;Failed=failed;Failure=failure;LastHit=lastHit;LastHitAmount=lastHitAmount;KillExperience=killExperience;}
        public void RecordSaved(int materials,bool first,int nextNode,int nextDifficulty,int sharedBefore,int sharedAfter,int completionExperience,bool firstCoreAvailable=false)
        {if(Saved||Failed)return;Materials=materials;FirstCompletion=first;FirstCoreAvailable=firstCoreAvailable;UnlockedNode=nextNode;UnlockedDifficulty=nextDifficulty;SharedBefore=sharedBefore;SharedAfter=sharedAfter;CompletionExperience=completionExperience;Saved=true;}
    }
}
