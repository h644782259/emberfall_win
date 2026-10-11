using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private Transform leftFoot,rightFoot;
        private void RefineHumanoid()
        {
            if(!ActorAnatomy.Enabled||treantCompanion||headRig==null)return;
            // A six-head silhouette keeps class headgear readable without a toy head.
            headRig.localScale=new Vector3(.84f,.86f,.84f);
            Color face=isHero?new Color(.94f,.76f,.59f):enemyOwner!=null&&enemyOwner.Kind==EnemyKind.Guardian?new Color(.57f,.39f,.31f):new Color(.4f,.62f,.28f);
            foreach(var renderer in headRig.GetComponentsInChildren<MeshRenderer>())if(renderer.name=="Head"){face=renderer.sharedMaterial.color;break;}
            Part("Neck skin",PrimitiveType.Capsule,new Vector3(0,-.005f,0),new Vector3(.19f,.12f,.18f),face,headRig,VisualSurface.Skin);
            for(int side=-1;side<=1;side+=2)
            {
                var pupil=headRig.Find(side<0?"Eyes L":"Eyes R");if(pupil!=null){pupil.localScale=new Vector3(.035f,.047f,.02f);pupil.localPosition+=new Vector3(0,0,.021f);}
                Part("Eye sclera",PrimitiveType.Sphere,new Vector3(side*.105f,.2f,.233f),new Vector3(.074f,.045f,.018f),new Color(.86f,.84f,.73f),headRig,VisualSurface.Skin);
                Part("Upper eyelid",PrimitiveType.Sphere,new Vector3(side*.105f,.228f,.233f),new Vector3(.087f,.012f,.027f),face*.88f,headRig,VisualSurface.Skin);
            }
            Part("Lower lip",PrimitiveType.Sphere,new Vector3(0,.085f,.235f),new Vector3(.092f,.023f,.036f),face*.76f,headRig,VisualSurface.Skin);
            leftFoot=FootJoint(leftKnee);rightFoot=FootJoint(rightKnee);
        }
        private void RefineArmorGeometry()
        {
            if(!ActorAnatomy.Enabled)return;
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
                if(filter.name=="Guardian layered pauldron"||filter.name=="Guardian chest plate")ActorAnatomy.Apply(filter.gameObject,filter.name,false);
            if(slime&&body!=null){var material=body.GetComponent<Renderer>().sharedMaterial;material.SetFloat("_Glossiness",.78f);material.SetFloat("_Metallic",.035f);}
        }
        private Transform FootJoint(Transform knee)
        {
            if(knee==null)return null;Transform boot=knee.Find("Boot");if(boot==null)return null;
            var foot=NewJoint("Ankle",knee,new Vector3(0,-.27f,.015f));boot.SetParent(foot,true);return foot;
        }
        private void ApplyAnatomicalGait(bool action)
        {
            if(!ActorAnatomy.Enabled)return;
            float step=locomotion.Phase,motion=locomotion.Speed;
            float l=Mathf.Sin(step),r=-l;
            if(leftFoot!=null)leftFoot.localRotation=Quaternion.Euler((Mathf.Max(0,l)*-12+Mathf.Max(0,-l)*20)*motion,0,0);
            if(rightFoot!=null)rightFoot.localRotation=Quaternion.Euler((Mathf.Max(0,r)*-12+Mathf.Max(0,-r)*20)*motion,0,0);
            if(!action&&leftElbow!=null&&rightElbow!=null)
            {
                leftElbow.localRotation=Quaternion.Euler(-16-Mathf.Max(0,-l)*18*motion,0,-motion*3);
                rightElbow.localRotation=Quaternion.Euler(-18-Mathf.Max(0,-r)*18*motion,0,motion*3);
                float breath=Mathf.Sin((isolatedPreview?previewTime:Time.time)*2.1f+phase)*(1-motion);
                leftArm.localRotation=Quaternion.Euler(-l*locomotion.Forward*27+breath*1.8f,0,-8-locomotion.Side*3-breath*.7f);
                rightArm.localRotation=Quaternion.Euler(l*locomotion.Forward*24-breath*1.8f,0,8-locomotion.Side*3+breath*.7f);
            }
        }
    }
}
