using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // One tiny tile per surface, shared by every actor and room. No per-frame pixels.
    public static class SurfaceTextureLibrary
    {
        public static bool Enabled=true;
        static readonly Dictionary<VisualSurface,Texture2D> tiles=new Dictionary<VisualSurface,Texture2D>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){foreach(var t in tiles.Values)if(t!=null)Object.Destroy(t);tiles.Clear();}
        internal static Texture2D Get(VisualSurface surface)
        {
            if(surface==VisualSurface.Stone)return Resources.Load<Texture2D>("WorldArt/WeatheredSlate");
            Texture2D tile;if(tiles.TryGetValue(surface,out tile))return tile;
            const int size=128;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float noise=Mathf.Sin(x*127.1f+y*311.7f)*43758.5453f;noise-=Mathf.Floor(noise);
                float value=.88f;
                if(surface==VisualSurface.Cloth)value=.83f+.12f*Mathf.Sin(x*Mathf.PI*.5f)*Mathf.Sin(y*Mathf.PI*.5f)+noise*.045f;
                else if(surface==VisualSurface.Wood)value=.77f+.17f*Mathf.Sin(x*Mathf.PI/8+Mathf.Sin(y*Mathf.PI/32)*1.2f)+noise*.05f;
                else if(surface==VisualSurface.Metal)value=.88f+.06f*Mathf.Sin(y*Mathf.PI*.5f)+noise*.035f;
                else if(surface==VisualSurface.Foliage)value=.78f+noise*.12f+.08f*Mathf.Sin(x*Mathf.PI/16)*Mathf.Sin(y*Mathf.PI/16);
                else if(surface==VisualSurface.Skin)value=.94f+noise*.035f;
                else value=.9f+noise*.05f;
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            tile=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Shared "+surface+" grain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=2};
            tile.SetPixels(pixels);tile.Apply(true,true);tiles.Add(surface,tile);return tile;
        }
        internal static void Apply(Material material,VisualSurface surface)
        {
            if(!Enabled)return;
            if(surface==VisualSurface.Water||surface==VisualSurface.Crystal)return;
            Texture2D tile=Get(surface);if(tile==null)return;
            material.mainTexture=tile;
            material.mainTextureScale=surface==VisualSurface.Cloth?new Vector2(3,3):surface==VisualSurface.Metal?new Vector2(2,2):Vector2.one;
        }
    }
}
