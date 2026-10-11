using UnityEngine;
using System.Collections.Generic;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Texture2D terrainMap, mapMarker;
        private int terrainMapRevision = int.MinValue;
        private float terrainMapRadius;
        private const int TerrainMapSize = 192;
        private readonly Color32[] terrainMapPixels = new Color32[TerrainMapSize * TerrainMapSize];
        private readonly byte[] terrainMapSurface = new byte[TerrainMapSize * TerrainMapSize];
        private sealed class CachedTerrainMap { public string Key; public Texture2D Texture; }
        private readonly List<CachedTerrainMap> terrainMapCache=new List<CachedTerrainMap>();
        private Coroutine terrainMapBuild;
        private int terrainMapRequestedRevision=int.MinValue;
        private float terrainMapRequestedRadius;
        private string terrainMapRequestedKey;
        private int terrainMapBuildCount,terrainMapCacheHits;
        private static readonly Vector3[] MinimapCampRoad={new Vector3(0,0,-16),new Vector3(0,0,-10),new Vector3(-1,0,-5),new Vector3(0,0,-1),new Vector3(2,0,4),new Vector3(0,0,11),new Vector3(2,0,19)};
        private System.Collections.IEnumerator BuildTerrainMap(float radius,int revision,string key,bool dungeon,bool forest)
        {
            yield return null;
            float sliceStart=Time.realtimeSinceStartup;
            for(int y=0;y<TerrainMapSize;y++)
            {
                if(WorldTraversal.Revision!=revision){terrainMapBuild=null;yield break;}
                for(int x=0;x<TerrainMapSize;x++)
                {
                    Vector3 point=new Vector3(((x+.5f)/TerrainMapSize*2-1)*radius,0,((y+.5f)/TerrainMapSize*2-1)*radius);
                    terrainMapSurface[y*TerrainMapSize+x]=(byte)WorldTraversal.MapSurface(point);
                }
                if(Time.realtimeSinceStartup-sliceStart>=.0015f){yield return null;sliceStart=Time.realtimeSinceStartup;}
            }
            for(int y=0;y<TerrainMapSize;y++)
            {
                if(WorldTraversal.Revision!=revision){terrainMapBuild=null;yield break;}
                for(int x=0;x<TerrainMapSize;x++)
                {
                    int i=y*TerrainMapSize+x;byte surface=terrainMapSurface[i];
                    Vector3 p=new Vector3(((x+.5f)/TerrainMapSize*2-1)*radius,0,((y+.5f)/TerrainMapSize*2-1)*radius);
                    float grain=(Mathf.PerlinNoise(p.x*.85f+53,p.z*.85f+71)-.5f)*.045f;
                    Color tint=new Color(.045f,.065f,.075f);
                    bool edge=x>0&&x<TerrainMapSize-1&&y>0&&y<TerrainMapSize-1;
                    if(surface==1)
                    {
                        tint=dungeon?new Color(.24f,.25f,.31f):forest?Color.Lerp(new Color(.15f,.26f,.22f),new Color(.32f,.38f,.25f),Mathf.PerlinNoise(p.x*.13f+15,p.z*.13f+38)):new Color(.29f,.32f,.31f);
                        float h=WorldTerrain.Height(p),slope=WorldTerrain.Height(p+new Vector3(-.3f,0,.3f))-h;
                        tint*=1+slope*.9f+h*.08f;
                        if(h>.12f&&Mathf.Repeat(h,.25f)<.025f)tint*=.86f;
                        if(edge&&(terrainMapSurface[i-1]==2||terrainMapSurface[i+1]==2||terrainMapSurface[i-TerrainMapSize]==2||terrainMapSurface[i+TerrainMapSize]==2))tint=new Color(.43f,.46f,.34f);
                    }
                    else if(surface==2)tint=Color.Lerp(new Color(.08f,.24f,.31f),new Color(.16f,.39f,.43f),Mathf.PerlinNoise(p.x*.3f,p.z*.3f));
                    else if(p.x*p.x+p.z*p.z<(radius-.16f)*(radius-.16f))
                    {tint=dungeon?new Color(.13f,.14f,.19f):new Color(.10f,.17f,.15f);if(edge&&(terrainMapSurface[i-1]==1||terrainMapSurface[i+1]==1||terrainMapSurface[i-TerrainMapSize]==1||terrainMapSurface[i+TerrainMapSize]==1))tint=new Color(.38f,.39f,.32f);}
                    tint.r+=grain;tint.g+=grain;tint.b+=grain;tint.a=1;terrainMapPixels[i]=QualitySettings.activeColorSpace==ColorSpace.Linear?tint.linear:tint;
                }
                if(Time.realtimeSinceStartup-sliceStart>=.0015f){yield return null;sliceStart=Time.realtimeSinceStartup;}
            }
            if(WorldTraversal.Revision!=revision){terrainMapBuild=null;yield break;}
            var texture=new Texture2D(TerrainMapSize,TerrainMapSize,TextureFormat.RGBA32,false){name="Navigation terrain",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(terrainMapPixels);texture.Apply(false,true);
            if(terrainMapCache.Count>=6){Destroy(terrainMapCache[0].Texture);terrainMapCache.RemoveAt(0);}
            terrainMapCache.Add(new CachedTerrainMap{Key=key,Texture=texture});terrainMap=texture;
            terrainMapRevision=revision;terrainMapRadius=radius;terrainMapBuild=null;terrainMapBuildCount++;
        }
        private void DrawMinimapTerrain(Rect r)
        {
            float radius=session.ArenaRadius;int revision=WorldTraversal.Revision;
            if(terrainMapRevision!=revision||terrainMapRadius!=radius)
            {
                if(terrainMapBuild==null||terrainMapRequestedRevision!=revision||terrainMapRequestedRadius!=radius)
                {
                    if(terrainMapBuild!=null){StopCoroutine(terrainMapBuild);terrainMapBuild=null;}
                    terrainMapRequestedRevision=revision;terrainMapRequestedRadius=radius;
                    terrainMapRequestedKey=(session.InDungeon?"D":"H"+session.CurrentHub)+(WorldTerrain.Enabled?"R":"F")+radius.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+WorldTraversal.NavigationMapKey;
                    bool found=false;
                    for(int i=0;i<terrainMapCache.Count;i++)if(terrainMapCache[i].Key==terrainMapRequestedKey)
                    {var cached=terrainMapCache[i];terrainMapCache.RemoveAt(i);terrainMapCache.Add(cached);terrainMap=cached.Texture;terrainMapRevision=revision;terrainMapRadius=radius;terrainMapCacheHits++;found=true;break;}
                    if(!found)terrainMapBuild=StartCoroutine(BuildTerrainMap(radius,revision,terrainMapRequestedKey,session.InDungeon,!session.InDungeon&&session.CurrentHub==0));
                }
            }
            Fill(r,new Color(.035f,.055f,.067f,.97f));
            Rect field=new Rect(r.x+3,r.y+3,r.width-6,r.height-6);
            if(terrainMap!=null&&terrainMapRevision==revision&&terrainMapRadius==radius)GUI.DrawTexture(field,terrainMap,ScaleMode.StretchToFill,true);
            Border(r,new Color(.48f,.49f,.36f,.85f));Border(field,new Color(.025f,.045f,.05f,.8f));
            if(!session.InDungeon&&session.CurrentHub==0)
            {
                var road=MinimapCampRoad;
                for(int i=1;i<road.Length;i++)MapLine(r,road[i-1],road[i],new Color(.66f,.58f,.40f,.7f),1.5f);
                MapLine(r,new Vector3(-1.2f,0,-3.2f),new Vector3(-1.2f,0,1.6f),gold,1);
                MapLine(r,new Vector3(1.2f,0,-3.2f),new Vector3(1.2f,0,1.6f),gold,1);
            }
            Fill(new Rect(r.xMax-19,r.y+4,15,16),new Color(.03f,.05f,.06f,.8f));
            Text(new Rect(r.xMax-19,r.y+4,15,16),"N",10,new Color(.87f,.83f,.64f),true,false,TextAnchor.MiddleCenter);
            if(session.Player!=null)MapLine(r,session.Player.transform.position,session.Player.transform.position+session.Player.transform.forward*2.8f,Color.white,2);
        }
        private void ReleaseTerrainMaps()
        {
            if(terrainMapBuild!=null)StopCoroutine(terrainMapBuild);
            foreach(var cached in terrainMapCache)if(cached.Texture!=null)Destroy(cached.Texture);
            terrainMapCache.Clear();terrainMap=null;if(mapMarker!=null)Destroy(mapMarker);
        }
        private void DrawMapMarker(Rect rect,Color tint)
        {
            if(mapMarker==null)
            {
                mapMarker=new Texture2D(24,24,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
                var pixels=new Color32[24*24];for(int y=0;y<24;y++)for(int x=0;x<24;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(12,12));pixels[y*24+x]=new Color(1,1,1,Mathf.Clamp01(11.5f-d));}mapMarker.SetPixels32(pixels);mapMarker.Apply(false,true);
            }
            Color previous=GUI.color;GUI.color=tint;GUI.DrawTexture(rect,mapMarker);GUI.color=previous;
        }
        private void MapLine(Rect r,Vector3 from,Vector3 to,Color tint,float thickness)
        {
            float radius=session.ArenaRadius;
            r=new Rect(r.x+3,r.y+3,r.width-6,r.height-6);
            Vector2 a=new Vector2(r.x+(from.x/radius+1)*r.width*.5f,r.yMax-(from.z/radius+1)*r.height*.5f);
            Vector2 b=new Vector2(r.x+(to.x/radius+1)*r.width*.5f,r.yMax-(to.z/radius+1)*r.height*.5f);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/2));
            for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,i/(float)steps);Fill(new Rect(p.x-thickness*.5f,p.y-thickness*.5f,thickness,thickness),tint);}
        }
    }
}
