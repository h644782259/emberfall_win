using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Texture2D titleSky, titleMist;
        private bool titleSkyAsset;
        private float titleClock;
        private void ReconcileTitleBackdrop()
        {
            if (session == null || session.HasStarted) { ReleaseTitleBackdrop(); return; }
            if (!session.BackgroundPaused && !EffectPreferences.ReducedEffects)
                titleClock += Mathf.Min(Time.unscaledDeltaTime, .05f);
        }
        private void ReleaseTitleBackdrop()
        {
            if (titleSky != null) { if(titleSkyAsset)Resources.UnloadAsset(titleSky);else Destroy(titleSky); }
            if (titleMist != null) Destroy(titleMist);
            titleSky = titleMist = null;
            titleClock = 0;
        }
        private void BuildTitleBackdrop()
        {
            titleSky=Resources.Load<Texture2D>("UI/TitleBackdrop");
            titleSkyAsset=titleSky!=null;
            if(titleSky==null)
            {
                titleSky=new Texture2D(1,1,TextureFormat.RGBA32,false);
                titleSky.SetPixel(0,0,new Color(.018f,.029f,.048f,1));titleSky.Apply(false,true);
            }
            Color[] pixels;
            const int fw=128,fh=32;pixels=new Color[fw*fh];
            for(int y=0;y<fh;y++)for(int x=0;x<fw;x++)
            {
                float nx=(x+.5f)/fw,ny=(y+.5f)/fh;
                float edge=Mathf.Sin(nx*Mathf.PI)*Mathf.Sin(ny*Mathf.PI);
                pixels[y*fw+x]=new Color(.33f,.43f,.53f,edge*edge*(.12f+.04f*Mathf.Sin(nx*22+ny*9)));
            }
            titleMist=new Texture2D(fw,fh,TextureFormat.RGBA32,false){name="Title mist",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            titleMist.SetPixels(pixels);titleMist.Apply(false,true);
        }
        private void DrawTitleBackdrop()
        {
            Matrix4x4 previousMatrix=GUI.matrix;Color previousColor=GUI.color;
            GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
            // Always opaque and screen sized, independent of safe area, DPI and panel max widths.
            Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.018f,.029f,.048f,1));
            if(Event.current.type==EventType.Repaint)
            {
                if(titleSky==null)BuildTitleBackdrop();
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),titleSky,ScaleMode.ScaleAndCrop);
                float t=EffectPreferences.ReducedEffects?0:titleClock;
                for(int layer=0;layer<3;layer++)
                {
                    float drift=Mathf.Sin(t*.045f+layer*2)*Screen.width*.035f;
                    GUI.DrawTexture(new Rect(-Screen.width*.12f+drift,Screen.height*(.48f+layer*.11f),Screen.width*1.24f,Screen.height*.24f),titleMist);
                }
                int count=EffectPreferences.ReducedEffects?8:28;
                for(int i=0;i<count;i++)
                {
                    float phase=Mathf.Repeat(i*.618034f+t*(.012f+(i%4)*.002f),1);
                    float x=Screen.width*(.04f+Mathf.Repeat(i*.381966f, .92f))+Mathf.Sin(t*.22f+i)*Screen.width*.007f;
                    float y=Screen.height*(1.06f-phase*.66f);
                    float size=Mathf.Clamp(Screen.height/420f,1.5f,4)*(i%3==0?1.6f:1);
                    float brightness=.45f+.3f*Mathf.Sin(t*.6f+i);
                    Fill(new Rect(x,y,size,size*1.8f),new Color(1,.57f,.25f,brightness));
                }
            }
            GUI.matrix=previousMatrix;GUI.color=previousColor;
        }
    }
}
