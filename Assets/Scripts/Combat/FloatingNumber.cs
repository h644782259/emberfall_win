using UnityEngine;
using System.Collections.Generic;
namespace Emberfall
{
    /// <summary>Measured screen-space text with separate bounded mechanism admission.</summary>
    public sealed class FloatingNumber : MonoBehaviour
    {
        public static int ActiveCount { get; private set; }
        public static int MechanismCount { get; private set; }
        private static Font sharedFont;
        private static int fontRevision;
        private static bool watchingFont;
        private CombatTextMetrics metrics;
        private static void WatchFont()
        {if(watchingFont)return;Font.textureRebuilt+=OnFontTextureRebuilt;watchingFont=true;}
        private static void UnwatchFont()
        {if(!watchingFont)return;Font.textureRebuilt-=OnFontTextureRebuilt;watchingFont=false;}
        private static void OnFontTextureRebuilt(Font font)
        {if(font==sharedFont)unchecked{fontRevision++;}}
        private static void SyncSharedFont()
        {
            Font resolved=GameFont.Shared;
            if(object.ReferenceEquals(sharedFont,resolved))return;
            sharedFont=resolved;unchecked{fontRevision++;}
            foreach(var number in visible)if(number!=null)number.RefreshFont();
        }
        private void RefreshFont()
        {
            if(sharedFont==null)return;
            if(textMesh!=null){textMesh.font=sharedFont;textRenderer.sharedMaterial=sharedFont.material;}
            foreach(var edge in outline)if(edge!=null){edge.font=sharedFont;edge.GetComponent<MeshRenderer>().sharedMaterial=sharedFont.material;}
        }
        private static readonly List<FloatingNumber> visible=new List<FloatingNumber>(40);
        private static int reflowFrame=-1;
        private Vector3 originAtSpawn;
        private int lane;
        private TextMesh textMesh;
        private MeshRenderer textRenderer;
        private readonly TextMesh[] outline=new TextMesh[4];
        private float life;
        private Color color;
        private bool critical,mechanism,counted;
        private string display;
        private CombatTextLayout.Box bounds;
        private float Duration {get{return display!=null&&display.StartsWith("等级提升")?4.5f:mechanism?1.8f:critical?1.8f:1.5f;}}
        private static float Density {get{return MobileControls.Active?MobileControls.Layout.Scale:Mathf.Clamp(Screen.dpi>=120?Screen.dpi/163f:Screen.height/1080f,1,3);}}

        public static bool CanSpawn(Vector3 origin,bool isCritical=false)
        {
            FloatingNumber replacement;int selectedLane;Camera camera;CombatTextLayout.Box box;CombatTextMetrics measured;
            return TrySelectAdmission(origin,"1",isCritical,false,out replacement,out selectedLane,out camera,out box,out measured);
        }
        public static FloatingNumber Spawn(Vector3 origin,string value,Color tint,bool isCritical=false,string objectName="Combat Text",bool isMechanism=false)
        {
            string shown=Display(value,isCritical);
            FloatingNumber replacement;int selectedLane;Camera camera;CombatTextLayout.Box box;CombatTextMetrics measured;
            if(!TrySelectAdmission(origin,shown,isCritical,isMechanism,out replacement,out selectedLane,out camera,out box,out measured))return null;
            GameObject root=new GameObject(objectName);root.transform.position=origin;
            FloatingNumber number=root.AddComponent<FloatingNumber>();
            number.InitializeAdmitted(shown,tint,isCritical,isMechanism,replacement,selectedLane,camera,box,measured);
            return number;
        }
        private static string Display(string value,bool critical)
        {
            string shown=value??"";
            // Combat captions are short; detailed explanations stay in the HUD/log.
            if(shown.Length>24)shown=shown.Substring(0,23)+"…";
            return critical?shown.TrimEnd('!','！'):shown;
        }
        private static void FontReady(string value,bool critical)
        {
            SyncSharedFont();
            if(sharedFont!=null)sharedFont.RequestCharactersInTexture(value,64,critical?FontStyle.BoldAndItalic:FontStyle.Bold);
        }
        private static float MeasureAspect(string value,bool critical)
        {
            FontReady(value,critical);float advance=0,min=0,max=0,bottom=float.MaxValue,top=float.MinValue;
            foreach(char c in value)
            {
                CharacterInfo info;
                if(sharedFont!=null&&sharedFont.GetCharacterInfo(c,out info,64,critical?FontStyle.BoldAndItalic:FontStyle.Bold))
                {min=Mathf.Min(min,advance+info.minX);max=Mathf.Max(max,advance+info.maxX);bottom=Mathf.Min(bottom,info.minY);top=Mathf.Max(top,info.maxY);advance+=info.advance;}
                else {advance+=64;max=advance;bottom=Mathf.Min(bottom,0);top=Mathf.Max(top,64);}
            }
            return Mathf.Max(1,max-min)/Mathf.Max(1,top-bottom)+(critical?2:0);
        }
        private static Vector2 ScaledSize(float aspect,bool critical)
        {
            float pixels=CombatTextLayout.PixelHeight(EffectPreferences.CombatTextScale,Density,critical);
            float w=aspect*pixels;
            // Reserve the full screen-space envelope of the tilted critical glyph.
            return critical?new Vector2(w*.99027f+pixels*.13918f,pixels*.99027f+w*.13918f):new Vector2(w,pixels);
        }
        private static bool TrySelectAdmission(Vector3 origin,string value,bool isCritical,bool isMechanism,out FloatingNumber replacement,
            out int selectedLane,out Camera camera,out CombatTextLayout.Box box,out CombatTextMetrics measured)
        {
            replacement=null;selectedLane=0;camera=Camera.main;box=default(CombatTextLayout.Box);measured=default(CombatTextMetrics);
            int globalLimit=MobileControls.Active?20:36;
            if(isMechanism)
            {
                if(MechanismCount>=CombatTextLayout.MechanismLimit)return false;
            }
            else if(ActiveCount-MechanismCount>=globalLimit)
            {
                if(!isCritical)return false;
                foreach(var number in visible)if(number!=null&&!number.critical&&!number.mechanism){replacement=number;break;}
                if(replacement==null)return false;
            }
            Vector3 point=camera!=null?camera.WorldToScreenPoint(origin):Vector3.zero;
            if(camera!=null&&(point.z<=0||point.x<0||point.y<0||point.x>camera.pixelWidth||point.y>camera.pixelHeight))return false;
            int nearby=0;
            foreach(var number in visible)
            {
                if(number==null||number==replacement||number.mechanism!=isMechanism)continue;
                bool close=camera!=null?Vector2.Distance(point,camera.WorldToScreenPoint(number.originAtSpawn))<110*Density:Vector3.Distance(origin,number.originAtSpawn)<2f;
                if(close)nearby++;
            }
            if(nearby>=(isMechanism?4:isCritical?6:4))return false;
            float aspect=MeasureAspect(value,isCritical);measured=new CombatTextMetrics(aspect,fontRevision);
            Vector2 size=ScaledSize(aspect,isCritical)*(isMechanism?1f:1.22f);
            if(Place(point,size,replacement,null,camera,out selectedLane,out box))return true;
            if(isMechanism)
                foreach(var number in visible)
                    if(number!=null&&!number.mechanism&&Place(point,size,number,null,camera,out selectedLane,out box))
                    {replacement=number;return true;}
            return false;
        }
        private static bool Place(Vector3 point,Vector2 size,FloatingNumber replacement,FloatingNumber self,Camera camera,out int slot,out CombatTextLayout.Box box)
        {
            box=default(CombatTextLayout.Box);
            for(slot=0;slot<CombatTextLayout.CandidateCount;slot++)
            {
                box=CombatTextLayout.Candidate(point.x,point.y,size.x,size.y,slot);
                if(camera!=null&&!CombatTextLayout.Fits(box,camera.pixelWidth,camera.pixelHeight))continue;
                bool blocked=false;
                foreach(var number in visible)
                {
                    if(number==null||number==replacement||number==self||!number.counted)continue;
                    if(box.Overlaps(number.bounds)){blocked=true;break;}
                }
                if(!blocked)return true;
            }
            return false;
        }
        public void Initialize(string value,Color tint,bool isCritical=false)
        {
            if(counted)return;
            string shown=Display(value,isCritical);FloatingNumber replacement;Camera camera;int selectedLane;CombatTextLayout.Box box;CombatTextMetrics measured;
            if(!TrySelectAdmission(transform.position,shown,isCritical,false,out replacement,out selectedLane,out camera,out box,out measured)){Destroy(gameObject);return;}
            InitializeAdmitted(shown,tint,isCritical,false,replacement,selectedLane,camera,box,measured);
        }
        private void InitializeAdmitted(string value,Color tint,bool isCritical,bool isMechanism,FloatingNumber replacement,int selectedLane,Camera camera,CombatTextLayout.Box box,CombatTextMetrics measured)
        {
            if(replacement!=null)replacement.Retire();
            critical=isCritical;mechanism=isMechanism;originAtSpawn=transform.position;lane=selectedLane;display=value;bounds=box;metrics=measured;
            ActiveCount++;if(mechanism)MechanismCount++;counted=true;visible.Add(this);WatchFont();
            color=critical?new Color(1f,.15f,.18f):tint;
            for(int i=0;i<outline.Length;i++)
            {
                var edge=new GameObject("Combat text outline");edge.transform.SetParent(transform,false);
                float edgeWidth=critical?.026f:.018f;
                edge.transform.localPosition=new Vector3(i%2==0?-edgeWidth:edgeWidth,i<2?-edgeWidth:edgeWidth,.012f);
                outline[i]=Configure(edge,value,new Color(.045f,.025f,.035f,1));edge.GetComponent<MeshRenderer>().sortingOrder=100;edge.GetComponent<MeshRenderer>().enabled=!critical;
            }
            textMesh=Configure(gameObject,value,color);textRenderer=GetComponent<MeshRenderer>();textRenderer.sortingOrder=101;textRenderer.enabled=!critical;
            ApplyBox(camera);
        }
        private TextMesh Configure(GameObject obj,string value,Color tint)
        {
            TextMesh mesh=obj.AddComponent<TextMesh>();mesh.text=value;mesh.fontSize=64;mesh.characterSize=.085f;
            mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.fontStyle=critical||display=="连击兑现"?FontStyle.BoldAndItalic:FontStyle.Bold;mesh.color=tint;
            if(sharedFont!=null){mesh.font=sharedFont;obj.GetComponent<MeshRenderer>().sharedMaterial=sharedFont.material;}
            return mesh;
        }
        private GUIStyle criticalNumberStyle;
        private void OnGUI()
        {
            if(!critical||!counted||Event.current.type!=EventType.Repaint)return;
            Matrix4x4 priorMatrix=GUI.matrix;Color priorColor=GUI.color;int priorDepth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=1;
            float alpha=Mathf.Clamp01((Duration-life)/.45f);
            GUI.color=new Color(1,1,1,alpha);
            Rect burst=new Rect(bounds.X,Screen.height-bounds.Y-bounds.Height,bounds.Width,bounds.Height);
            GUI.DrawTexture(burst,UIIconAtlas.Utility("critical",true));
            if(criticalNumberStyle==null)criticalNumberStyle=new GUIStyle{alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.BoldAndItalic,wordWrap=false,padding=new RectOffset()};
            criticalNumberStyle.font=sharedFont;
            criticalNumberStyle.fontSize=Mathf.Max(1,Mathf.RoundToInt(burst.height*.55f));
            Rect label=new Rect(burst.x+burst.width*.20f,burst.y+burst.height*.18f,burst.width*.60f,burst.height*.64f);
            GUIContent caption=new GUIContent(display);
            while(criticalNumberStyle.fontSize>1&&(criticalNumberStyle.CalcSize(caption).x>label.width||criticalNumberStyle.CalcSize(caption).y>label.height))criticalNumberStyle.fontSize--;
            criticalNumberStyle.normal.textColor=new Color(.98f,.13f,.18f);
            GUI.Label(label,caption,criticalNumberStyle);
            GUI.matrix=priorMatrix;GUI.color=priorColor;GUI.depth=priorDepth;
        }
        private void Update()
        {
            life+=Time.deltaTime;float alpha=Mathf.Clamp01((Duration-life)/(display!=null&&display.StartsWith("等级提升")?1f:mechanism?.35f:.45f));
            Color face=color;
            if(textMesh!=null)textMesh.color=new Color(face.r,face.g,face.b,alpha);
            foreach(TextMesh edge in outline)if(edge!=null)edge.color=critical?new Color(.24f,.055f,.008f,alpha*.98f):new Color(.045f,.025f,.035f,alpha*.98f);
            if(life>Duration)Retire();
        }
        private void LateUpdate()
        {
            if(reflowFrame==Time.frameCount)return;reflowFrame=Time.frameCount;Camera camera=Camera.main;if(camera==null)return;SyncSharedFont();
            // Reproject all text together after camera motion. Old bounds remain
            // reserved until replaced, so no pair can overlap during reflow.
            for(int i=visible.Count-1;i>=0;i--)
            {
                var number=visible[i];if(number==null)continue;
                Vector3 point=camera.WorldToScreenPoint(number.originAtSpawn);point.y+=number.life*(number.mechanism?18:30)*Density*EffectPreferences.EffectsScale;
                float aspect;
                if(!number.metrics.TryGet(fontRevision,out aspect))
                {aspect=MeasureAspect(number.display,number.critical);number.metrics=new CombatTextMetrics(aspect,fontRevision);}
                if(number.textRenderer!=null)
                {
                    Vector3 actual=number.textRenderer.localBounds.size;
                    if(actual.y>.001f)aspect=Mathf.Max(aspect,actual.x/actual.y+(number.critical?2:0));
                }
                Vector2 size=ScaledSize(aspect,number.critical)*(number.mechanism?1f:number.DamageFloatScale);
                int slot;CombatTextLayout.Box box;
                if(point.z<=0||!Place(point,size,null,number,camera,out slot,out box)){number.Retire();continue;}
                number.lane=slot;number.bounds=box;number.ApplyBox(camera);
            }
        }
        private float DamageFloatScale {get{return Mathf.Lerp(1.22f,.55f,Mathf.Clamp01(life/Duration));}}
        private void ApplyBox(Camera camera)
        {
            if(camera==null||textRenderer==null)return;
            float depth=Mathf.Max(1,Vector3.Dot(originAtSpawn-camera.transform.position,camera.transform.forward));
            transform.position=camera.ScreenToWorldPoint(new Vector3(bounds.X+bounds.Width*.5f,bounds.Y+bounds.Height*.5f,depth));
            transform.rotation=camera.transform.rotation*(critical?Quaternion.Euler(0,0,8f):Quaternion.identity);
            float glyphHeight=textRenderer.localBounds.size.y;if(glyphHeight<=.001f||camera.pixelHeight<=0)return;
            float visibleHeight=camera.orthographic?camera.orthographicSize*2:depth*2*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
            float pixels=21*EffectPreferences.CombatTextScale*Density*(critical?1.28f:1);
            float edgeOffset=glyphHeight/Mathf.Max(1,pixels)*(critical?1.25f:1f);
            for(int i=0;i<outline.Length;i++)if(outline[i]!=null)outline[i].transform.localPosition=new Vector3(i%2==0?-edgeOffset:edgeOffset,i<2?-edgeOffset:edgeOffset,.001f);
            float pop=mechanism?(critical?1+Mathf.Max(0,1-life/.16f)*.14f*EffectPreferences.EffectsScale:1):DamageFloatScale;
            transform.localScale=Vector3.one*(pixels*visibleHeight/(camera.pixelHeight*glyphHeight)*pop);
        }
        private void ReleaseCount(){if(!counted)return;counted=false;ActiveCount=Mathf.Max(0,ActiveCount-1);if(mechanism)MechanismCount=Mathf.Max(0,MechanismCount-1);visible.Remove(this);if(ActiveCount==0)UnwatchFont();}
        private void Retire(){ReleaseCount();gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDisable(){ReleaseCount();}
        private void OnDestroy(){ReleaseCount();}
    }
}
