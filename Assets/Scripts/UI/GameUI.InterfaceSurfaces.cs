using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Texture2D surfaceMask, surfaceGradient, surfaceOutline;
        // Reusable nine-sliced chamfered frames: corners remain crisp at every panel size.
        private void EnsureInterfaceSurfaces()
        {
            if(surfaceMask!=null)return;
            surfaceMask=InterfaceTexture(false,false);
            surfaceGradient=InterfaceTexture(true,false);
            surfaceOutline=InterfaceTexture(false,true);
        }
        private static Texture2D InterfaceTexture(bool gradient,bool outline)
        {
            const int size=32;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Emberfall UI frame",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int edge=Mathf.Min(x,size-1-x)+Mathf.Min(y,size-1-y);
                float alpha=Mathf.Clamp01(edge-4f);
                if(outline&&x>0&&x<size-1&&y>0&&y<size-1&&edge>6)alpha=0;
                float light=gradient?Mathf.Lerp(.72f,1.12f,y/(float)(size-1)):1;
                texture.SetPixel(x,y,new Color(light,light,light,alpha));
            }
            texture.Apply(false,true);return texture;
        }
        private void Surface(Rect rect,Color color,bool gradient=false)
        {
            EnsureInterfaceSurfaces();
            DrawInterfaceTexture(rect,gradient?surfaceGradient:surfaceMask,color);
        }
        private void SurfaceFrame(Rect rect,Color color)
        {
            EnsureInterfaceSurfaces();DrawInterfaceTexture(rect,surfaceOutline,color);
        }
        private static void DrawInterfaceTexture(Rect rect,Texture2D texture,Color color)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Color previous=GUI.color;color.a*=controlOpacity;GUI.color=color;
            // GUIStyle's border is a nine-slice margin, independent of the target aspect ratio.
            var style=InterfaceSurfaceStyle(texture);
            style.Draw(rect,GUIContent.none,false,false,false,false);GUI.color=previous;
        }
        private static readonly System.Collections.Generic.Dictionary<Texture2D,GUIStyle> interfaceSurfaceStyles=new System.Collections.Generic.Dictionary<Texture2D,GUIStyle>();
        private static GUIStyle InterfaceSurfaceStyle(Texture2D texture)
        {
            GUIStyle style;
            if(interfaceSurfaceStyles.TryGetValue(texture,out style))return style;
            style=new GUIStyle{border=new RectOffset(8,8,8,8)};style.normal.background=texture;
            interfaceSurfaceStyles.Add(texture,style);return style;
        }
        private void ReleaseInterfaceSurfaces()
        {
            foreach(var texture in new[]{surfaceMask,surfaceGradient,surfaceOutline})
                if(texture!=null){interfaceSurfaceStyles.Remove(texture);Destroy(texture);}
            surfaceMask=surfaceGradient=surfaceOutline=null;
        }
    }
}
