namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private readonly CastFirstHitRegistry castReceipts=new CastFirstHitRegistry();
        private int receiptEpoch=int.MinValue;
        // Synchronous casting and guard-triggered future fields are producers too.
        private CastFirstHitReceipt latestIssuedReceipt, skillCastReceipt, guardCastReceipt;
        private void EnsureCastReceiptEpoch()
        {
            if(receiptEpoch==CombatEpoch)return;
            castReceipts.SetEpoch(CombatEpoch);receiptEpoch=CombatEpoch;
            latestIssuedReceipt=skillCastReceipt=guardCastReceipt=null;
        }
        internal CastFirstHitReceipt CaptureCastReceipt(int castId)
        {EnsureCastReceiptEpoch();return castReceipts.Find(castId);}
        internal CastFirstHitReceipt RetainCastReceipt(int castId)
        {return CaptureCastReceipt(castId)?.Retain();}
        private void HoldCastReceipt(ref CastFirstHitReceipt field,int castId)
        {field?.Release();field=RetainCastReceipt(castId);}
        private int IssueCastId()
        {
            EnsureCastReceiptEpoch();
            if(nextCastId==int.MaxValue)return 0;
            int id=++nextCastId;latestIssuedReceipt?.Release();latestIssuedReceipt=castReceipts.Issue(id);return id;
        }
    }
}
