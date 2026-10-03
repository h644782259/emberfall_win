using System;
using System.Collections.Generic;
namespace Emberfall
{
    // Lifetime state belongs to a cast, never to its numeric ordering. Producers
    // retain this object while they can still deliver an impact.
    public sealed class CastFirstHitReceipt
    {
        internal sealed class Scope { internal bool Active = true; }
        private readonly Scope scope;
        private readonly HashSet<object> interruptTargets = new HashSet<object>();
        private bool coreSeen;
        internal int References {get;private set;}=1;
        public CastFirstHitReceipt Retain(){if(!Active)return null;References++;return this;}
        public void Release(){if(References>0)References--;}
        public const int MaximumTargets = 256;
        public int Id { get; private set; }
        internal CastFirstHitReceipt(int id, Scope scope) { Id=id;this.scope=scope; }
        public bool Active { get { return scope.Active&&References>0; } }
        public bool FirstCoreHit()
        { if(!Active||coreSeen)return false;coreSeen=true;return true; }
        public bool FirstInterruptTarget(object target)
        {
            if(!Active||target==null||interruptTargets.Contains(target)||interruptTargets.Count>=MaximumTargets)return false;
            interruptTargets.Add(target);return true;
        }
    }
    // No LRU eviction: a live producer's receipt is never removed or reconstructed.
    // Dead weak entries are swept on issuance. At capacity new qualification is
    // refused, while existing casts keep their exact receipts and all damage runs.
    public sealed class CastFirstHitRegistry
    {
        public const int MaximumLiveCasts=256;
        private readonly Dictionary<int,WeakReference<CastFirstHitReceipt>> casts=new Dictionary<int,WeakReference<CastFirstHitReceipt>>();
        private readonly List<int> dead=new List<int>(MaximumLiveCasts);
        private CastFirstHitReceipt.Scope scope=new CastFirstHitReceipt.Scope();
        private int epoch=int.MinValue, highestIssued;
        public int Count {get{return casts.Count;}}
        public void SetEpoch(int value)
        {if(epoch==value)return;scope.Active=false;scope=new CastFirstHitReceipt.Scope();casts.Clear();dead.Clear();highestIssued=0;epoch=value;}
        public CastFirstHitReceipt Find(int id)
        {WeakReference<CastFirstHitReceipt> weak;CastFirstHitReceipt receipt;return id>0&&casts.TryGetValue(id,out weak)&&weak.TryGetTarget(out receipt)&&receipt.Active?receipt:null;}
        public CastFirstHitReceipt Issue(int id)
        {
            if(id<=0)return null;
            var existing=Find(id);if(existing!=null)return existing;
            if(id<=highestIssued)return null;highestIssued=id;
            // Issuance is called only for a fresh monotonic actor ID; Find never
            // issues/recreates a missing receipt for an impact from an old producer.
            dead.Clear();foreach(var pair in casts){CastFirstHitReceipt live;if(!pair.Value.TryGetTarget(out live)||!live.Active)dead.Add(pair.Key);}
            foreach(int key in dead)casts.Remove(key);dead.Clear();
            if(casts.Count>=MaximumLiveCasts)return null;
            var receipt=new CastFirstHitReceipt(id,scope);casts.Add(id,new WeakReference<CastFirstHitReceipt>(receipt));return receipt;
        }
    }
    // Compatibility for pure rule callers without producer lifetimes. Kept bounded
    // and fail-closed (no eviction). Actual actors use the weak lifetime registry.
    internal sealed class CastFirstHitHistory
    {
        private readonly Dictionary<int,CastFirstHitReceipt> casts=new Dictionary<int,CastFirstHitReceipt>();
        private CastFirstHitReceipt.Scope scope=new CastFirstHitReceipt.Scope();
        public CastFirstHitReceipt Get(int id)
        {if(id<=0)return null;CastFirstHitReceipt receipt;if(casts.TryGetValue(id,out receipt))return receipt;if(casts.Count>=CastFirstHitRegistry.MaximumLiveCasts)return null;receipt=new CastFirstHitReceipt(id,scope);casts.Add(id,receipt);return receipt;}
        public void Clear(){scope.Active=false;scope=new CastFirstHitReceipt.Scope();casts.Clear();}
    }
}
