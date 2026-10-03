using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Attach to the authored moving body, never a world-height approximation or face.
        // Local offsets sit outside its flank; parenting carries squash, flight and knockdown.
        internal bool TryStatusAttachment(out Transform anchor,out Vector3 frost,out Vector3 vulnerability)
        {
            anchor=largeBossRig!=null?transform.Find("Suspended astrolabe chassis"):body;
            frost=vulnerability=Vector3.zero;
            if(anchor==null)return false;
            float flank=largeBossRig!=null?1.02f:.72f;
            float below=largeBossRig!=null?-.12f:slime?-.12f:floating?-.2f:-.16f;
            frost=new Vector3(-flank,below,0);
            vulnerability=new Vector3(flank,below,0);
            return true;
        }
    }
}
