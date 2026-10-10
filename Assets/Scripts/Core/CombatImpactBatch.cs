using System;
using System.Collections.Generic;
namespace Emberfall
{
    // Impact decisions keep their original target-batch boundary. Practice terminal
    // receipts use a separate outer action boundary, never a timer or future frame.
    public static class CombatImpactBatch
    {
        private static int depth, actionDepth, resolving;
        private static readonly List<Action> pending = new List<Action>();
        private static readonly List<Action> afterAction = new List<Action>();
        public static bool InAction { get { return depth != 0 || actionDepth != 0 || resolving != 0; } }
        public static void Begin() { depth++; }
        public static void Resolve(Action action)
        {
            if (action == null) return;
            if (depth == 0) action();
            else if (!pending.Contains(action)) pending.Add(action);
        }
        public static void BeginAction(){actionDepth++;}
        public static void EndAction()
        {
            if(actionDepth<=0)throw new InvalidOperationException("Unbalanced combat action");
            actionDepth--;SettleAction();
        }
        public static void AfterCurrentAction(Action action)
        {
            if(action==null)return;
            if(depth==0&&actionDepth==0&&resolving==0)action();
            else if(!afterAction.Contains(action))afterAction.Add(action);
        }
        private static void SettleAction()
        {
            if(depth!=0||actionDepth!=0||resolving!=0)return;
            var work=afterAction.ToArray();afterAction.Clear();
            foreach(var action in work)action();
        }
        public static void End()
        {
            if (depth <= 0) throw new InvalidOperationException("Unbalanced combat impact batch");
            if (--depth != 0) return;
            resolving++;
            try
            {
                var work=pending.ToArray();pending.Clear();
                foreach(var action in work)action();
            }
            finally {resolving--;SettleAction();}
        }
    }
}
