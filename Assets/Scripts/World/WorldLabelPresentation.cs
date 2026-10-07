using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Floating labels share one bounded projection/overlap pass. Higher-priority
    // objective labels win; equal priorities retain creation order without flicker.
    public sealed class WorldLabelPresentation:MonoBehaviour
    {
        private static readonly List<WorldLabelPresentation> labels=new List<WorldLabelPresentation>(WorldLabelReadability.MaximumLabels);
        private static int evaluatedFrame=-1,nextOrder;
        private TextMesh label;private Renderer visual;private Vector3 originalScale;private int priority,order;
        private Rect projected;private bool shown;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){labels.Clear();evaluatedFrame=-1;nextOrder=0;}
        public void Initialize(TextMesh text,int priority=0)
        {
            label=text;if(label.font==null)GameFont.Apply(label);visual=GetComponent<Renderer>();originalScale=transform.localScale;this.priority=priority;
            Register();labels.Sort(Compare);evaluatedFrame=-1;
        }
        private static int Compare(WorldLabelPresentation a,WorldLabelPresentation b)
        {int value=b.priority.CompareTo(a.priority);return value!=0?value:a.order.CompareTo(b.order);}
        private void Register()
        {
            if(label==null||labels.Contains(this))return;
            if(labels.Count>=WorldLabelReadability.MaximumLabels){if(visual!=null)visual.enabled=false;return;}
            order=nextOrder++;labels.Add(this);labels.Sort(Compare);evaluatedFrame=-1;
        }
        private void OnEnable(){Register();}
        private void OnDisable(){labels.Remove(this);if(visual!=null)visual.enabled=false;evaluatedFrame=-1;}
        private void OnDestroy(){labels.Remove(this);}
        private void LateUpdate()
        {
            if(evaluatedFrame==Time.frameCount)return;evaluatedFrame=Time.frameCount;
            Camera camera=Camera.main;
            Rect safe=Screen.safeArea;float density=HudLogicalScale.For(safe.width,safe.height);
            for(int i=0;i<labels.Count;i++)
            {
                var item=labels[i];if(item==null||item.visual==null||item.label==null)continue;
                item.shown=false;item.visual.enabled=false;
                if(camera==null)continue;
                item.transform.rotation=camera.transform.rotation;item.transform.localScale=item.originalScale;
                Vector3 point=camera.WorldToScreenPoint(item.transform.position);
                if(!WorldLabelReadability.Visible(point.z,(item.transform.position-camera.transform.position).sqrMagnitude))continue;
                int lines=1;foreach(char c in item.label.text)if(c=='\n')lines++;
                // Actual font bounds, including current CJK glyphs; no guessed character width.
                float height=item.visual.localBounds.size.y*item.transform.lossyScale.y;
                Vector3 top=camera.WorldToScreenPoint(item.transform.position+camera.transform.up*height);
                float pixels=Mathf.Abs(top.y-point.y);float factor=WorldLabelReadability.Scale(pixels,lines,density);
                item.transform.localScale=item.originalScale*factor;
                float width=item.visual.localBounds.size.x*item.transform.lossyScale.x;
                Vector3 right=camera.WorldToScreenPoint(item.transform.position+camera.transform.right*width);
                float screenWidth=Mathf.Abs(right.x-point.x),screenHeight=pixels*factor;
                Vector3 center=camera.WorldToScreenPoint(item.transform.TransformPoint(item.visual.localBounds.center));
                item.projected=new Rect(center.x-screenWidth*.5f-3,center.y-screenHeight*.5f-2,screenWidth+6,screenHeight+4);
                if(!item.projected.Overlaps(safe))continue;
                bool overlap=false;
                for(int j=0;j<i;j++)if(labels[j]!=null&&labels[j].shown&&item.projected.Overlaps(labels[j].projected)){overlap=true;break;}
                if(overlap)continue;item.shown=true;item.visual.enabled=true;
            }
        }
    }
}
