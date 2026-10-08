using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private ProgressionAttention attention;
        private GameProfile attentionProfile;
        private bool attentionDirty=true,attentionCamp;
        private string reviewedEquipmentOwner;
        private Texture2D attentionDot;
        private readonly System.Collections.Generic.HashSet<string> reviewedEquipment=new System.Collections.Generic.HashSet<string>();
        private ProgressionAttention Attention
        {
            get
            {
                if(attentionDirty||attention==null||attentionProfile!=session.Progression.Profile||attentionCamp!=session.IsInCamp)
                {attention=ProgressionAttention.Evaluate(session.Progression,session.IsInCamp);
                    attentionProfile=session.Progression.Profile;attentionCamp=session.IsInCamp;attentionDirty=false;}
                EnsureReviewedEquipment();
                return attention;
            }
        }
        public void RebindProgressionNotifications(ProgressionService oldService,ProgressionService newService)
        {if(oldService!=null)oldService.Changed-=InvalidateAttention;if(newService!=null)newService.Changed+=InvalidateAttention;attentionProfile=null;attention=null;attentionDirty=true;reviewedEquipment.Clear();reviewedEquipmentOwner=null;ResetBuildPlanSurface();}
        private void InvalidateAttention(){attentionDirty=true;}
        private bool NewEquipmentAttention
        {get{foreach(string id in Attention.HigherScoreItems)if(!reviewedEquipment.Contains(id))return true;return false;}}
        private void Badge(Rect r,bool show)
        {
            if(!show)return;
            if(attentionDot==null)
            {
                const int size=32;attentionDot=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};
                Color[] pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float a=Mathf.Clamp01(15-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16)));pixels[y*size+x]=new Color(1,.19f,.19f,a);}
                attentionDot.SetPixels(pixels);attentionDot.Apply(false,true);
            }
            float sizeUI=MobileControls.Active?10*TouchRatio:10;Color prior=GUI.color;GUI.color=Color.white;
            GUI.DrawTexture(new Rect(r.xMax-sizeUI*.8f,r.y-sizeUI*.2f,sizeUI,sizeUI),attentionDot);GUI.color=prior;
        }
        private void EnsureReviewedEquipment()
        {
            string key="Emberfall.ReviewedEquipment."+session.Progression.CurrentSlotId;
            if(reviewedEquipmentOwner==key)return;
            reviewedEquipmentOwner=key;reviewedEquipment.Clear();
            foreach(string id in PlayerPrefs.GetString(key,"").Split(','))if(!string.IsNullOrEmpty(id))reviewedEquipment.Add(id);
        }
        private bool UnreviewedEquipmentUpgrade(ItemData item)
        {return IsEquipmentUpgrade(item)&&!reviewedEquipment.Contains(item.id);}
        private void ReviewEquipment(ItemData item)
        {
            if(item==null)return;EnsureReviewedEquipment();
            if(reviewedEquipment.Add(item.id))
            {PlayerPrefs.SetString(reviewedEquipmentOwner,string.Join(",",reviewedEquipment));PlayerPrefs.Save();}
        }
    }
}
