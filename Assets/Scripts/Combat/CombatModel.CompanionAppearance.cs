using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private Transform companionInsignia,companionRigid;
        private Vector3 companionRestPosition;
        private Quaternion companionRestRotation;
        private float companionPreparation,companionRecallLift;
        private Transform CompanionRigidParent()
        {
            if(companionRigid==null)
            {companionRigid=Joint("Companion rigid body attachments",Vector3.zero);companionRestPosition=body.localPosition;companionRestRotation=body.localRotation;}
            return companionRigid;
        }
        public void SetCompanionAttackPreparation(float value){companionPreparation=Mathf.Clamp01(value);}
        private void ApplyCompanionPose()
        {
            if(companionRigid==null||body==null)return;
            float ready=companionPreparation;
            if(ready>0)
            {
                body.localRotation=body.localRotation*Quaternion.Euler(-ready*12,0,0);
                if(quadruped){transform.localPosition+=new Vector3(0,-ready*.06f,-ready*.04f);if(wolfJaw!=null)wolfJaw.localRotation=Quaternion.Euler(-ready*18,0,0);}
                if(treantCompanion){if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-ready*65,0,-10);if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(-ready*65,0,10);}
            }
            // Match the body's rigid motion about its authored pivot, excluding its soft scale.
            Quaternion rotation=body.localRotation*Quaternion.Inverse(companionRestRotation);
            companionRigid.localRotation=rotation;
            companionRigid.localPosition=body.localPosition-rotation*companionRestPosition;
            companionRigid.localScale=Vector3.one;
            if(companionInsignia!=null)companionInsignia.localPosition=Vector3.up*companionRecallLift;
            if(floating&&decoration!=null)decoration.localPosition=new Vector3(0,1.75f+Mathf.Sin(Time.time*2)*.025f,0);
        }

        private int companionAppearanceKey=-1;
        public void SetCompanionRecall(float progress)
        {companionRecallLift=.04f*Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI);}
        // A stable local structure, independent of health/command VFX and combat stats.
        public void SetCompanionAppearance(SummonedCompanion.Kind form,int rank,bool permanent)
        {
            rank=Mathf.Clamp(rank,0,3);int key=(int)form*16+rank*2+(permanent?1:0);
            if(companionAppearanceKey==key)return;companionAppearanceKey=key;
            if(companionInsignia!=null){companionInsignia.gameObject.SetActive(false);Destroy(companionInsignia.gameObject);}
            companionInsignia=Joint("Companion contract structure",Vector3.zero);
            companionInsignia.SetParent(CompanionRigidParent(),false);
            Color metal=new Color(.56f,.51f,.31f),jade=new Color(.35f,.73f,.58f),wood=new Color(.31f,.24f,.14f);
            if(form==SummonedCompanion.Kind.Wolf)
            {
                if(permanent)
                {
                    Part("Permanent wolf collar",PrimitiveType.Cube,new Vector3(0,.80f,.34f),new Vector3(.55f,.13f,.14f),metal,companionInsignia,VisualSurface.Metal);
                    Part("Permanent wolf contract seal",PrimitiveType.Sphere,new Vector3(0,.74f,.43f),new Vector3(.14f,.19f,.065f),jade,companionInsignia,VisualSurface.Crystal);
                }
                for(int i=0;i<rank;i++)for(int side=-1;side<=1;side+=2)
                    Part("Wolf grown shoulder clasp",PrimitiveType.Cube,new Vector3(side*(.25f+i*.025f),.72f,.12f-i*.16f),new Vector3(.075f,.14f,.10f),metal,companionInsignia,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,side*25);
            }
            else if(form==SummonedCompanion.Kind.Spirit||form==SummonedCompanion.Kind.Wisp)
            {
                if(permanent)
                {
                    Part("Permanent spirit contract band",PrimitiveType.Cube,new Vector3(0,1.04f,.31f),new Vector3(.40f,.10f,.075f),metal,companionInsignia,VisualSurface.Metal);
                    Part("Permanent spirit contract seal",PrimitiveType.Sphere,new Vector3(0,1.08f,.37f),new Vector3(.15f,.19f,.08f),jade,companionInsignia,VisualSurface.Crystal);
                }
                for(int i=0;i<rank;i++)for(int side=-1;side<=1;side+=2)
                    Part("Spirit grown crown vane",PrimitiveType.Cube,new Vector3(side*(.19f+i*.10f),1.73f-i*.10f,-.10f),new Vector3(.085f,.25f,.09f),metal,companionInsignia,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,-side*(20+i*12));
            }
            else
            {
                if(permanent)Part("Permanent treant contract knot",PrimitiveType.Sphere,new Vector3(0,1.2f,.49f),new Vector3(.25f,.29f,.09f),jade,companionInsignia,VisualSurface.Crystal);
                for(int i=0;i<rank;i++)for(int side=-1;side<=1;side+=2)
                    Part("Treant grown shoulder branch",PrimitiveType.Capsule,new Vector3(side*(.60f+i*.10f),1.7f+i*.10f,-.12f),new Vector3(.09f,.30f,.09f),wood,companionInsignia,VisualSurface.Wood).localRotation=Quaternion.Euler(0,0,-side*(25+i*10));
            }
        }
    }
}
