using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Texture2D titleSky, titleMist;
        private float titleClock;
        private void ReconcileTitleBackdrop()
        {
            if (session == null || session.HasStarted) { ReleaseTitleBackdrop(); return; }
            if (!session.BackgroundPaused && !EffectPreferences.ReducedEffects)
                titleClock += Mathf.Min(Time.unscaledDeltaTime, .05f);
        }
        private void ReleaseTitleBackdrop()
        {
            if (titleSky != null) Destroy(titleSky);
            if (titleMist != null) Destroy(titleMist);
            titleSky = titleMist = null;
            titleClock = 0;
        }
        private void BuildTitleBackdrop()
        {
            const int w = 256, h = 128;
            var pixels = new Color[w*h];
            for (int y=0;y<h;y++) for (int x=0;x<w;x++)
            {
                float nx=(x+.5f)/w, ny=(y+.5f)/h;
                float glow=Mathf.Exp(-((nx-.32f)*(nx-.32f)*13+(ny-.25f)*(ny-.25f)*9));
                Color c=Color.Lerp(new Color(.025f,.044f,.081f),new Color(.065f,.095f,.15f),ny);
                c+=new Color(.15f,.054f,.015f)*glow;
                // Layered ruined silhouettes, with a clear dark centre for the home actions.
                float ground=.09f+.016f*Mathf.Sin(nx*46)+.012f*Mathf.Sin(nx*93);
                bool ruin=(nx>.07f&&nx<.11f&&ny<.48f)||(nx>.16f&&nx<.19f&&ny<.37f)||
                    (nx>.81f&&nx<.85f&&ny<.43f)||(nx>.90f&&nx<.94f&&ny<.56f);
                bool lintel=((nx>.065f&&nx<.195f)||(nx>.80f&&nx<.95f))&&ny>.32f&&ny<.35f;
                if(ny<ground||ruin||lintel)c=Color.Lerp(c,new Color(.01f,.018f,.03f),.9f);
                // Sparse stars and the distant starfall scar; all baked once, not per frame.
                if(ny>.55f&&(x*37+y*97)%733==0)c=Color.Lerp(c,new Color(.64f,.77f,.85f),.65f);
                float scar=Mathf.Abs(ny-(.82f-(nx-.12f)*.56f));
                if(nx>.12f&&nx<.39f&&scar<.009f)c=Color.Lerp(c,new Color(.72f,.39f,.17f),.6f*(1-(nx-.12f)/.27f));
                c.a=1;pixels[y*w+x]=c;
            }
            titleSky=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Starfall ruins title",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            titleSky.SetPixels(pixels);titleSky.Apply(false,true);
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
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),titleSky);
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
