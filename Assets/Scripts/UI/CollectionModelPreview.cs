using System;
using UnityEngine;
namespace Emberfall
{
    // One real isolated mannequin. No player/controller, equipment or save mutation.
    public sealed class CollectionModelPreview : IDisposable
    {
        const int PreviewLayer=31;
        private static int nextStage;
        GameObject stage,avatar;
        Transform turnRing;
        Camera camera;
        CombatModel model;
        RenderTexture texture;
        Material shadowMaterial,ringMaterial;
        Texture2D shadowTexture;
        Mesh contactMesh;
        float retryAt;
        bool failureReported;
        public string LastError {get;private set;}
        Renderer[] renderers;
        RendererGroupCache rendererGroup;
        int rendererRevision=-1;
        Light[] sceneLights;
        int[] sceneMasks;
        CollectionPreviewState state=new CollectionPreviewState();
        CollectionPreviewMotion motion=new CollectionPreviewMotion();
        CollectionPreviewComposition composition;
        CollectionPreviewSurface requested=new CollectionPreviewSurface(384,480,false),allocated;
        int surfaceFrame=-1;
        bool framingDirty=true;
        bool equipmentFraming,equipmentDetail;
        bool centerOnAvatar;
        public void SetCenterOnAvatar(bool enabled)
        {if(centerOnAvatar==enabled)return;centerOnAvatar=enabled;framingDirty=true;state.Invalidate();}
        int equipmentHighlightSlot=-1;
        readonly MaterialPropertyBlock highlightBlock=new MaterialPropertyBlock();
        MaterialPropertyBlock[] savedHighlightBlocks;
        bool[] highlighted;
        public void SetEquipmentHighlight(int slot)
        {slot=slot>=0&&slot<=2?slot:-1;if(equipmentHighlightSlot==slot)return;equipmentHighlightSlot=slot;state.Invalidate();}
        // Explicit equipment subtrees only. Skin, wings and unrelated parts never flash.
        internal static bool EquipmentPart(string name,int slot)
        {
            return slot==0?(name=="Equipped Weapon"||name=="Vanguard_Sword"):
                slot==1?(name=="Equipped Armor"||name=="Equipped class costume"||name=="Equipped Shoulder"||name=="Equipped class shoulder"||name=="Equipped contract crown forks"):
                slot==2&&name=="Equipped Relic";
        }
        bool HighlightPart(Transform part)
        {for(var t=part;t!=null&&t!=avatar.transform;t=t.parent)if(EquipmentPart(t.name,equipmentHighlightSlot))return true;return false;}
        void BeginEquipmentHighlight()
        {
            if(!equipmentFraming||equipmentHighlightSlot<0)return;
            if(savedHighlightBlocks==null||savedHighlightBlocks.Length!=renderers.Length)
            {savedHighlightBlocks=new MaterialPropertyBlock[renderers.Length];highlighted=new bool[renderers.Length];}
            for(int i=0;i<renderers.Length;i++)
            {
                var renderer=renderers[i];if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy||!HighlightPart(renderer.transform))continue;
                if(savedHighlightBlocks[i]==null)savedHighlightBlocks[i]=new MaterialPropertyBlock();
                renderer.GetPropertyBlock(savedHighlightBlocks[i]);renderer.GetPropertyBlock(highlightBlock);
                highlightBlock.SetColor("_Color",new Color(1f,.78f,.32f));highlightBlock.SetColor("_BaseColor",new Color(1f,.78f,.32f));
                highlighted[i]=true;renderer.SetPropertyBlock(highlightBlock);
            }
        }
        void EndEquipmentHighlight()
        {
            if(highlighted==null)return;
            for(int i=0;i<highlighted.Length;i++)if(highlighted[i])
            {if(renderers[i]!=null)renderers[i].SetPropertyBlock(savedHighlightBlocks[i]);highlighted[i]=false;}
        }
        public void SetEquipmentFraming(bool enabled,bool detail)
        {if(equipmentFraming==enabled&&equipmentDetail==detail)return;equipmentFraming=enabled;equipmentDetail=detail;framingDirty=true;state.Invalidate();}
        public CollectionPreviewAction PreviewAction {get{return motion.Action;}}
        public void Play(CollectionPreviewAction action){motion.Play(action);state.Invalidate();}

        public void Rotate(float degrees){SetYaw(state.Yaw+degrees);}
        public void SetYaw(float degrees){float old=state.Yaw;state.SetYaw(degrees);if(old!=state.Yaw)framingDirty=true;}
        public void SetComposition(CollectionPreviewComposition value)
        {if(((int)value<0||(int)value>2)||composition==value)return;composition=value;framingDirty=true;state.SetYaw(CollectionPreviewFraming.DefaultYaw(value));state.Invalidate();}
        public void SetViewport(float pixelWidth,float pixelHeight,bool mobile)
        {requested=new CollectionPreviewSurface(pixelWidth,pixelHeight,mobile);}
        public void Invalidate(){retryAt=0;state.Invalidate();sceneLights=null;if(rendererGroup!=null)rendererGroup.Invalidate();}
        // Keep a failed visual preview from aborting the surrounding inventory/fashion UI.
        // Strict Render remains available to diagnostics; failures are logged once until recovery.
        public Texture RenderSafe(HeroClass hero,ItemData weapon,ItemData armor,ItemData relic,FashionData wings,FashionData fashionWeapon)
        {
            if(Time.unscaledTime<retryAt)return null;
            try
            {
                var image=Render(hero,weapon,armor,relic,wings,fashionWeapon);
                if(image!=null){LastError=null;failureReported=false;}
                return image;
            }
            catch(Exception error)
            {
                float yaw=state.Yaw;var mode=composition;var surface=requested;
                Dispose();state.SetYaw(yaw);composition=mode;requested=surface;
                retryAt=Time.unscaledTime+2f;LastError=error.Message;
                if(!failureReported){Debug.LogException(error);failureReported=true;}
                return null;
            }
        }
        public Texture Render(HeroClass hero,ItemData weapon,ItemData armor,ItemData relic,FashionData wings,FashionData fashionWeapon)
        {
            if(Event.current==null||Event.current.type!=EventType.Repaint)return texture!=null&&texture.IsCreated()?texture:null;
            if(stage==null||camera==null)
            {
                float yaw=state.Yaw;var mode=composition;var surface=requested;var localMotion=motion;
                Dispose();composition=mode;requested=surface;motion=localMotion;state.SetYaw(yaw);
                // A failed contact/material allocation must not cache a partial stage.
                // Preserve the selected angle, and let the original failure reach the caller.
                try {CreateStage();}
                catch {Dispose();state.SetYaw(yaw);throw;}
            }
            state.Observe(new CollectionPreviewAppearance(hero,weapon,armor,relic,wings,fashionWeapon));
            if((texture==null||!allocated.Equals(requested))&&surfaceFrame!=Time.frameCount)CreateTexture();
            if(texture==null)return null;
            if(!texture.IsCreated()){texture.Create();state.InvalidateTexture();}
            if(!texture.IsCreated())return null;
            if(motion.Advance(Time.unscaledDeltaTime,Time.frameCount))state.Invalidate();
            if(rendererGroup!=null&&rendererGroup.Dirty){state.Invalidate();framingDirty=true;}
            if(!state.ShouldRender(true,Time.frameCount))return texture;
            if(state.NeedsModel)
            {
                if(rendererGroup!=null){rendererGroup.Dispose();rendererGroup=null;}
                if(avatar!=null){avatar.SetActive(false);UnityEngine.Object.Destroy(avatar);}
                avatar=new GameObject("Preview mannequin");avatar.transform.SetParent(stage.transform,false);
                var random=UnityEngine.Random.state;
                try {model=CombatModel.Hero(avatar.transform,hero);model.ApplyEquipment(weapon,armor,relic);model.ApplyFashion(wings,fashionWeapon);model.ConfigurePreview();model.SamplePreview(0,CollectionPreviewAction.Idle,1);}
                finally {UnityEngine.Random.state=random;}
                foreach(var t in avatar.GetComponentsInChildren<Transform>(true))t.gameObject.layer=PreviewLayer;
                foreach(var c in avatar.GetComponentsInChildren<Collider>(true))c.enabled=false;
                foreach(var behaviour in avatar.GetComponentsInChildren<MonoBehaviour>(true))if(!(behaviour is RendererGroupObserver))behaviour.enabled=false;
                rendererGroup=new RendererGroupCache(avatar.transform);rendererRevision=-1;
                framingDirty=true;state.ModelReady();
            }
            RefreshRendererGroup();
            avatar.transform.localRotation=Quaternion.Euler(0,state.Yaw,0);
            avatar.transform.localScale=Vector3.one;
            if(framingDirty){FrameModel();framingDirty=false;}
            model.SamplePreview(motion.Time,motion.Action,motion.Progress,motion.OrbitYaw);

            turnRing.localRotation=Quaternion.Euler(0,motion.RingYaw,0);
            RenderIsolated();state.Rendered(Time.frameCount);
            return texture;
        }
        void RefreshRendererGroup()
        {
            renderers=rendererGroup.Read();if(rendererRevision==rendererGroup.Revision)return;
            rendererRevision=rendererGroup.Revision;framingDirty=true;
            // A late authored part must obey the same isolation contract as the model.
            foreach(var t in avatar.GetComponentsInChildren<Transform>(true))t.gameObject.layer=PreviewLayer;
            foreach(var c in avatar.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var behaviour in avatar.GetComponentsInChildren<MonoBehaviour>(true))if(!(behaviour is RendererGroupObserver))behaviour.enabled=false;
            foreach(var renderer in renderers)if(renderer!=null){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
        }
        bool InComposition(Transform part)
        {
            if(composition==CollectionPreviewComposition.Full)return true;
            for(var t=part;t!=null&&t!=avatar.transform;t=t.parent)
            {
                // Explicit authored FBX part groups; their renderer.bounds below remain
                // authoritative, including animated skinned-mesh bounds.
                if(composition==CollectionPreviewComposition.Back&&(t.name=="Fashion Wings"||t.name=="Vanguard_Back"))return true;
                if(composition==CollectionPreviewComposition.Weapon&&(t.name=="Sword Wrist"||t.name=="Staff Wrist"||t.name=="Bow"||t.name=="Equipped Weapon"||t.name=="Fashion Weapon"||t.name=="Vanguard_Sword"))return true;
            }
            return false;
        }
        bool IsBodyRenderer(Transform part)
        {
            for(var t=part;t!=null&&t!=avatar.transform;t=t.parent)
                if(t.name=="Sword Wrist"||t.name=="Staff Wrist"||t.name=="Bow"||t.name=="Equipped Weapon"||t.name=="Fashion Weapon"||t.name=="Vanguard_Sword"||t.name=="Fashion Wings"||t.name=="Vanguard_Back")return false;
            return true;
        }
        void FrameModel()
        {
            if(equipmentFraming)
            {
                // Fixed game-unit fixture: neither candidate silhouette nor a pose changes framing.
                camera.aspect=(float)texture.width/texture.height;
                camera.orthographic=equipmentDetail;
                camera.fieldOfView=48f; // Same as GameSession's real default camera.
                camera.orthographicSize=2.1f*Mathf.Max(1,1/camera.aspect);
                Vector3 target=stage.transform.position+Vector3.up*(equipmentDetail?1.25f:.7f);
                // Default live pitch48.36646°, distance19×1.2041595; fixed framing,
                // no bounds fitting or following the moving preview mannequin.
                camera.transform.position=target+(equipmentDetail?new Vector3(0,1.1134f,7.9221f):new Vector3(0,17.1f,-15.2f));
                camera.transform.LookAt(target);return;
            }
            camera.orthographic=true;
            bool found=false;Bounds bounds=new Bounds();
            // Cache the envelope of actual idle/attack/cast poses. Camera bounds do not
            // chase animated limbs, breathing, cloak vertices or orbit angles each frame.
            for(int action=0;action<(centerOnAvatar?1:3);action++)for(int step=0;step<=(centerOnAvatar?0:4);step++)
            {
                model.SamplePreview(0,(CollectionPreviewAction)action,step*.25f);
                foreach(var renderer in renderers)
                    if(renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&InComposition(renderer.transform)&&(!centerOnAvatar||IsBodyRenderer(renderer.transform)))
                    {if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
            }
            if(!found)bounds=new Bounds(avatar.transform.position+Vector3.up*1.3f,new Vector3(1.3f,1.6f,1));
            if(composition==CollectionPreviewComposition.Back)
                bounds.Encapsulate(new Bounds(avatar.transform.position+Vector3.up*1.4f,new Vector3(1,1.3f,.7f)));
            if(centerOnAvatar)
            {
                // Keep the body centered even when a weapon or an attack pose extends sideways.
                Vector3 center=bounds.center;float bodyX=avatar.transform.position.x;
                Vector3 size=bounds.size;size.x+=2*Mathf.Abs(center.x-bodyX);center.x=bodyX;
                bounds=new Bounds(center,size);
            }
            camera.aspect=(float)texture.width/texture.height;
            camera.orthographicSize=CollectionPreviewFraming.Size(bounds.extents.x,bounds.extents.y,bounds.extents.z,camera.aspect,composition);
            camera.transform.position=bounds.center+new Vector3(0,1.1134f,7.9221f);camera.transform.LookAt(bounds.center);
        }
        void RenderIsolated()
        {
            if(sceneLights==null)
            {
#if UNITY_2023_1_OR_NEWER
                sceneLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
                sceneLights=UnityEngine.Object.FindObjectsOfType<Light>();
#endif
                sceneMasks=new int[sceneLights.Length];
            }
            var ambientMode=RenderSettings.ambientMode;Color ambient=RenderSettings.ambientLight;bool fog=RenderSettings.fog;
            try
            {
                for(int i=0;i<sceneLights.Length;i++)if(sceneLights[i]!=null)
                {sceneMasks[i]=sceneLights[i].cullingMask;if(!sceneLights[i].transform.IsChildOf(stage.transform))sceneLights[i].cullingMask&=~(1<<PreviewLayer);}
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.42f,.5f);RenderSettings.fog=false;
                BeginEquipmentHighlight();
                camera.Render();
            }
            finally
            {
                EndEquipmentHighlight();
                RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambient;RenderSettings.fog=fog;
                for(int i=0;i<sceneLights.Length;i++)if(sceneLights[i]!=null)sceneLights[i].cullingMask=sceneMasks[i];
            }
        }
        void CreateStage()
        {
            stage=new GameObject("Collection preview (isolated)"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=new Vector3(0,-1000-(++nextStage%256)*64,0);
            var lens=new GameObject("Preview camera");lens.transform.SetParent(stage.transform,false);camera=lens.AddComponent<Camera>();
            camera.enabled=false;camera.useOcclusionCulling=false;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.06f,.09f);
            camera.cullingMask=1<<PreviewLayer;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.allowHDR=false;camera.allowMSAA=true;
            AddLight("Preview key",new Vector3(35,155,0),1.05f,new Color(1,.91f,.8f));
            AddLight("Preview fill",new Vector3(15,30,0),.45f,new Color(.63f,.78f,1));
            AddLight("Preview rim",new Vector3(20,-65,0),.6f,new Color(.76f,.86f,1));
            CreateContact();
        }
        void AddLight(string name,Vector3 angle,float intensity,Color color)
        {
            var lamp=new GameObject(name);lamp.transform.SetParent(stage.transform,false);lamp.transform.localRotation=Quaternion.Euler(angle);
            var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=intensity;light.color=color;light.cullingMask=1<<PreviewLayer;light.shadows=LightShadows.None;
        }
        void CreateContact()
        {
            shadowTexture=new Texture2D(64,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float distance=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;float a=Mathf.Clamp01(1-distance);pixels[y*64+x]=new Color(.005f,.01f,.015f,a*a*.4f);}
            shadowTexture.SetPixels(pixels);shadowTexture.Apply(false,true);
            shadowMaterial=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave,mainTexture=shadowTexture};
            // A visual-only quad: CreatePrimitive would implicitly require MeshCollider
            // (which can be absent in an IL2CPP build). Own only the mesh and renderer.
            contactMesh=new Mesh{name="Preview contact quad",hideFlags=HideFlags.HideAndDontSave};
            contactMesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)};
            contactMesh.normals=new[]{new Vector3(0,0,-1),new Vector3(0,0,-1),new Vector3(0,0,-1),new Vector3(0,0,-1)};
            contactMesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
            contactMesh.triangles=new[]{0,2,1,2,3,1};contactMesh.RecalculateBounds();
            var shadow=new GameObject("Soft preview contact");shadow.layer=PreviewLayer;shadow.transform.SetParent(stage.transform,false);
            shadow.transform.localPosition=Vector3.up*.015f;shadow.transform.localRotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(2.3f,1.6f,1);
            shadow.AddComponent<MeshFilter>().sharedMesh=contactMesh;
            var contactRenderer=shadow.AddComponent<MeshRenderer>();contactRenderer.sharedMaterial=shadowMaterial;
            contactRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;contactRenderer.receiveShadows=false;
            var ring=new GameObject("Preview turn arc");ring.layer=PreviewLayer;ring.transform.SetParent(stage.transform,false);turnRing=ring.transform;
            var line=ring.AddComponent<LineRenderer>();ringMaterial=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave,color=new Color(.4f,.6f,.72f,.35f)};
            line.sharedMaterial=ringMaterial;line.useWorldSpace=false;line.positionCount=49;line.startWidth=line.endWidth=.018f;
            for(int i=0;i<49;i++){float a=i/48f*Mathf.PI*1.75f;line.SetPosition(i,new Vector3(Mathf.Cos(a)*1.2f,.025f,Mathf.Sin(a)*1.2f));}
        }
        void CreateTexture()
        {
            if(texture!=null){camera.targetTexture=null;texture.Release();UnityEngine.Object.Destroy(texture);}
            allocated=requested;surfaceFrame=Time.frameCount;framingDirty=true;
            var descriptor=new RenderTextureDescriptor(allocated.Width,allocated.Height,RenderTextureFormat.ARGB32,16){msaaSamples=allocated.Samples};
            descriptor.msaaSamples=Mathf.Clamp(SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor),1,allocated.Samples);
            texture=new RenderTexture(descriptor){name="Collection preview",hideFlags=HideFlags.HideAndDontSave};
            texture.Create();camera.targetTexture=texture;state.InvalidateTexture();
        }
        public void Dispose()
        {
            if(rendererGroup!=null){rendererGroup.Dispose();rendererGroup=null;}rendererRevision=-1;
            if(camera!=null)camera.targetTexture=null;
            if(stage!=null){stage.SetActive(false);UnityEngine.Object.Destroy(stage);}
            if(texture!=null){texture.Release();UnityEngine.Object.Destroy(texture);}
            if(contactMesh!=null)UnityEngine.Object.Destroy(contactMesh);
            if(shadowMaterial!=null)UnityEngine.Object.Destroy(shadowMaterial);if(ringMaterial!=null)UnityEngine.Object.Destroy(ringMaterial);if(shadowTexture!=null)UnityEngine.Object.Destroy(shadowTexture);
            stage=null;avatar=null;model=null;camera=null;texture=null;turnRing=null;shadowMaterial=ringMaterial=null;shadowTexture=null;contactMesh=null;renderers=null;sceneLights=null;sceneMasks=null;surfaceFrame=-1;
            state=new CollectionPreviewState();motion=new CollectionPreviewMotion();framingDirty=true;savedHighlightBlocks=null;highlighted=null;
        }
    }
}
