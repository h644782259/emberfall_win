using UnityEngine;

namespace Emberfall
{
    // Large, camera-readable shapes supplement the smaller particle details.
    internal sealed class ElementalFieldVisual : MonoBehaviour
    {
        private static int activeCount;
        private bool counted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount(){activeCount=0;}
        private ElementalCombatVfx.Element element;
        private float radius;
        private bool onBody;
        private LineRenderer[] strands;
        private Transform[] bubbles;
        private Material material;
        private Vector3[] placements;
        private float[] placementScales;
        private bool[] placed;
        private Vector3 placementPosition,placementScale;
        private Quaternion placementRotation;
        private int placementRevision=-1;
        private bool placementReady;
        private System.Func<float,float,float,bool> clearPlacement;
        private bool ClearPlacement(float x,float z,float extent)
        {
            Vector3 point=transform.TransformPoint(new Vector3(x,0,z));
            float scale=Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
            return CombatSight.Area(transform.position,point)&&CombatSight.VisualFootprint(point,point,extent*scale);
        }

        public static ElementalFieldVisual Spawn(Transform parent, ElementalCombatVfx.Element type, float size, bool body,CombatVisualPriority priority=CombatVisualPriority.SustainedBackground)
        {
            if(activeCount>=(MobileControls.Active?12:20))return null;
            GameObject obj = new GameObject(type + " Silhouette");
            obj.transform.SetParent(parent, false);
            if(CombatVisualLease.Attach(obj,priority)==null)return null;
            ElementalFieldVisual effect = obj.AddComponent<ElementalFieldVisual>();
            effect.Initialize(type, size, body);
            return effect;
        }

        private void Initialize(ElementalCombatVfx.Element type, float size, bool body,CombatVisualPriority priority=CombatVisualPriority.SustainedBackground)
        {
            activeCount++;counted=true;
            element = type;
            radius = size;
            onBody = body;
            int count = onBody ? 3 : MobileControls.Active ? 5 : 8;
            if(EffectPreferences.ReducedEffects)count=onBody?2:3;
            placements=new Vector3[count];placementScales=new float[count];placed=new bool[count];clearPlacement=ClearPlacement;
            if (type == ElementalCombatVfx.Element.Poison)
            {
                material = CombatFx.NewGlow();
                material.color = new Color(.47f, 1f, .2f,.36f);
                bubbles = new Transform[count];
                for (int i = 0; i < count; i++)
                {
                    GameObject bubble = ProceduralVisuals.Create("Venom Bubble",PrimitiveType.Sphere,material);
                    bubble.transform.SetParent(transform,false);
                    bubble.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    bubbles[i] = bubble.transform;
                }
            }
            else
            {
                material = CombatFx.NewGlow();
                strands = new LineRenderer[count];
                for (int i = 0; i < count; i++)
                {
                    GameObject strand = new GameObject(type == ElementalCombatVfx.Element.Fire ? "Flame Tongue" : "Electric Branch");
                    strand.transform.SetParent(transform, false);
                    LineRenderer line = strand.AddComponent<LineRenderer>();
                    line.useWorldSpace = false;
                    line.positionCount = 4;
                    line.sharedMaterial = material;
                    line.widthMultiplier = onBody ? .11f : type == ElementalCombatVfx.Element.Fire ? .15f : .065f;
                    line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.55f, .78f), new Keyframe(1f, 0f));
                    line.startColor = type == ElementalCombatVfx.Element.Fire ? new Color(1f, .85f, .2f,.58f) : new Color(.78f, .97f, 1f,.56f);
                    line.endColor = type == ElementalCombatVfx.Element.Fire ? new Color(1f, .25f, .06f,0) : new Color(.3f, .58f, 1f,.08f);
                    strands[i] = line;
                }
            }
            RefreshPlacement();
            Draw(Time.time); // Initial visible-side placement is applied before the first render.
        }

        private void RefreshPlacement()
        {
            if(onBody)return;
            if(placementReady&&placementRevision==WorldTraversal.Revision&&placementPosition.Equals(transform.position)&&
                placementRotation.Equals(transform.rotation)&&placementScale.Equals(transform.lossyScale))return;
            placementReady=true;placementRevision=WorldTraversal.Revision;placementPosition=transform.position;
            placementRotation=transform.rotation;placementScale=transform.lossyScale;
            float extent=element==ElementalCombatVfx.Element.Poison?.14f:element==ElementalCombatVfx.Element.Fire?.44f:.45f;
            for(int i=0;i<placements.Length;i++)
            {
                // Retain a still-valid envelope when unrelated cover changes or opens.
                if(placed[i]&&ClearPlacement(placements[i].x,placements[i].z,extent*placementScales[i]))continue;
                float angle=i*2.39996f,distance=radius*(.28f+(i%4)*.19f),x,z,fit;
                placed[i]=FilledVfxPlacement.TryPlace(Mathf.Cos(angle)*distance,Mathf.Sin(angle)*distance,radius,extent,clearPlacement,out x,out z,out fit);
                placements[i]=new Vector3(x,0,z);placementScales[i]=fit;
                if(bubbles!=null)bubbles[i].gameObject.SetActive(placed[i]);
                else{strands[i].enabled=placed[i];strands[i].widthMultiplier=(element==ElementalCombatVfx.Element.Fire?.15f:.065f)*fit;}
            }
        }
        private void Update(){RefreshPlacement();Draw(Time.time);}
        private void Draw(float time)
        {
            int count = bubbles != null ? bubbles.Length : strands.Length;
            for (int i = 0; i < count; i++)
            {
                if(!onBody&&!placed[i])continue;
                float angle=i*2.39996f+time*.45f;
                Vector3 origin=onBody?new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*.75f:placements[i];
                float fit=onBody?1:placementScales[i];
                if (bubbles != null)
                {
                    float rise = Mathf.Repeat(time * .65f + i * .173f, 1f);
                    float height = onBody ? .6f : .75f;
                    bubbles[i].localPosition = origin + Vector3.up * ((onBody ? -.35f : .1f) + rise * height);
                    float pulse = .7f + .45f * Mathf.Sin(rise * Mathf.PI);
                    float size = (onBody ? .16f : .19f) * pulse * (1f + .12f * Mathf.Sin(time * 7f + i));
                    bubbles[i].localScale = Vector3.one * size * fit;
                }
                else if (element == ElementalCombatVfx.Element.Fire)
                {
                    float height = (onBody ? .75f : .9f) * (.68f + .32f * Mathf.Sin(time * 8f + i * 1.7f));
                    float glow = .48f + .18f * Mathf.Sin(time * 9f + i * 2.1f);
                    strands[i].startColor = new Color(1f, .83f, .18f, glow);
                    float bottom = onBody ? -.42f : .08f;
                    Vector3 sway = new Vector3(Mathf.Sin(time * 6f + i * 2f), 0f, Mathf.Cos(time * 5f + i)) * .18f * fit;
                    strands[i].SetPosition(0, origin + Vector3.up * bottom);
                    strands[i].SetPosition(1, origin + sway * .4f + Vector3.up * (bottom + height * .3f));
                    strands[i].SetPosition(2, origin + sway + Vector3.up * (bottom + height * .7f));
                    strands[i].SetPosition(3, origin + sway * 1.3f + Vector3.up * (bottom + height));
                }
                else
                {
                    float height = onBody ? .8f : 1.05f;
                    float spark = .24f + .42f * Mathf.Pow(.5f + .5f * Mathf.Sin(time * 38f + i * 3.7f), 4f);
                    strands[i].startColor = new Color(.79f, .96f, 1f, spark);
                    Vector3 top = (onBody?origin*.25f:origin) + Vector3.up * height;
                    strands[i].SetPosition(0, top);
                    strands[i].SetPosition(1, Vector3.Lerp(top, origin, .33f) + new Vector3(Mathf.Sin(time * 39f + i), 0f, Mathf.Cos(time * 31f + i)) * .28f * fit);
                    strands[i].SetPosition(2, Vector3.Lerp(top, origin, .68f) + new Vector3(Mathf.Cos(time * 37f + i), 0f, Mathf.Sin(time * 29f + i)) * .25f * fit);
                    strands[i].SetPosition(3, origin + Vector3.up * .08f);
                }
            }
        }

        private void Release(){if(!counted)return;counted=false;activeCount=Mathf.Max(0,activeCount-1);}
        private void OnDisable(){Release();}
        private void OnEnable(){if(!gameObject.activeInHierarchy||material==null||counted)return;if(activeCount>=(MobileControls.Active?12:20)){gameObject.SetActive(false);return;}activeCount++;counted=true;}
        private void OnDestroy() { Release();if (material != null) Destroy(material); }
    }
}
