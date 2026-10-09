using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Texture2D terrainMap;
        private int terrainMapRevision = int.MinValue;
        private float terrainMapRadius;
        private const int TerrainMapSize = 384;
        private readonly Color[] terrainMapPixels = new Color[TerrainMapSize * TerrainMapSize];
        private void DrawMinimapTerrain(Rect r)
        {
            float radius = session.ArenaRadius;
            if (terrainMap == null || terrainMapRevision != WorldTraversal.Revision || terrainMapRadius != radius)
            {
                if (terrainMap == null)
                    terrainMap = new Texture2D(TerrainMapSize, TerrainMapSize, TextureFormat.RGBA32, false)
                    { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < TerrainMapSize; y++) for (int x = 0; x < TerrainMapSize; x++)
                {
                    Vector3 point = new Vector3(((x + .5f) / TerrainMapSize * 2 - 1) * radius, 0,
                        ((y + .5f) / TerrainMapSize * 2 - 1) * radius);
                    terrainMapPixels[y * TerrainMapSize + x] = WorldTraversal.IsWalkable(point, .16f)
                        ? new Color(.14f, .22f, .23f, .65f)
                        : WorldTraversal.IsOpenWater(point, .16f) ? new Color(.13f, .39f, .58f, .95f)
                        : new Color(.38f, .43f, .48f, .95f);
                }
                terrainMap.SetPixels(terrainMapPixels);
                terrainMap.Apply(false, false); // One reusable texture; future broken props update its pixels.
                terrainMapRevision = WorldTraversal.Revision;
                terrainMapRadius = radius;
            }
            GUI.DrawTexture(r, terrainMap, ScaleMode.StretchToFill, true);
            // Only the original forest actually has these road/bridge landmarks.
            // Other hubs, arenas and linked rooms derive their map from geometry.
            if (!session.InDungeon && session.CurrentHub == 0)
            {
                MapLine(r, new Vector3(0, 0, -16), new Vector3(0, 0, 11), new Color(.55f, .46f, .3f), 3);
                MapLine(r, new Vector3(-1.2f, 0, -3.2f), new Vector3(-1.2f, 0, 1.6f), gold, 2);
                MapLine(r, new Vector3(1.2f, 0, -3.2f), new Vector3(1.2f, 0, 1.6f), gold, 2);
            }
            Text(new Rect(r.xMax-17,r.y+1,16,15),"N",10,pale,true,false,TextAnchor.MiddleCenter);
            if(session.Player!=null)MapLine(r,session.Player.transform.position,session.Player.transform.position+session.Player.transform.forward*2.8f,Color.white,2);
        }
        private void MapLine(Rect r,Vector3 from,Vector3 to,Color tint,float thickness)
        {
            float radius=session.ArenaRadius;
            Vector2 a=new Vector2(r.x+(from.x/radius+1)*r.width*.5f,r.yMax-(from.z/radius+1)*r.height*.5f);
            Vector2 b=new Vector2(r.x+(to.x/radius+1)*r.width*.5f,r.yMax-(to.z/radius+1)*r.height*.5f);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/2));
            for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,i/(float)steps);Fill(new Rect(p.x-thickness*.5f,p.y-thickness*.5f,thickness,thickness),tint);}
        }
    }
}
