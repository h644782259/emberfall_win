using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Small original pictograms, rasterized once; no GUI rotation or font glyph dependencies.</summary>
    public static class UIIconAtlas
    {
        private static readonly Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();
        public static Texture2D Skill(HeroClass hero, int skill) { return Skill(hero, skill, 48); }
        public static Texture2D Skill(HeroClass hero, int skill, int requestedSize)
        {
            int rasterSize = SkillIconPresentation.RasterSize(requestedSize);
            int key = rasterSize * 1000 + (int)hero * 10 + skill;
            Texture2D texture;
            if (cache.TryGetValue(key, out texture)) return texture;
            var ink = new Icon(GameBalance.ClassColor(hero), rasterSize);
            if (hero == HeroClass.Summoner && skill != 3 && skill != 6 && skill != 8 && skill != 9)
            {
                if (skill == 0)
                {
                    ink.Disc(16, 32, 5); ink.Arrow(20, 32, 49, 32);
                    ink.Arc(16, 32, 22, -60, 60, 4); ink.Arc(16, 32, 35, -60, 60, 3);
                }
                else if (skill == 1)
                {
                    ink.Ring(32, 34, 19, 3);
                    for (int i = 0; i < 3; i++) { float x = 17 + i * 15; ink.Line(x, 52, x, 14, 4); ink.Line(x, 32, x - 7, 23, 3); ink.Line(x, 42, x + 7, 33, 3); }
                }
                else if (skill == 2)
                {
                    ink.Polygon(new[] { V(16, 31), V(11, 10), V(29, 22), V(35, 22), V(53, 10), V(48, 31), V(47, 45), V(32, 55), V(17, 45) });
                    ink.Disc(24, 34, 3); ink.Disc(40, 34, 3); ink.Line(29, 43, 35, 43, 3);
                }
                else if (skill == 4)
                {
                    ink.Ring(32, 32, 20, 3); ink.Disc(32, 32, 8);
                    for (int i = 0; i < 4; i++) ink.Radial(i * 90 + 45, 23, 29, 3);
                }
                else if (skill == 5)
                {
                    ink.Shield(); ink.Ring(32, 29, 9, 3);
                }
                else
                {
                    ink.Disc(32, 32, 6); ink.Ring(32, 32, 17, 3);
                    ink.Arrow(5, 5, 22, 22); ink.Arrow(59, 59, 42, 42);
                }
                texture = ink.Finish("Skill icon " + key); cache[key] = texture; return texture;
            }
            if (hero == HeroClass.Summoner && skill == 9)
            {
                ink.Line(32, 54, 32, 24, 8); ink.Line(32, 35, 13, 22, 5); ink.Line(32, 35, 51, 22, 5);
                ink.Disc(32, 16, 11); ink.Disc(14, 19, 9); ink.Disc(50, 19, 9);
                texture = ink.Finish("Skill icon " + key); cache[key] = texture; return texture;
            }
            switch (skill)
            {
                case 0:
                    if (hero == HeroClass.Vanguard) { ink.Sword(32, 32); ink.Arc(32, 32, 23, -50, 215, 3); }
                    else if (hero == HeroClass.Arcanist) { for (int i = 0; i < 6; i++) ink.Radial(i * 60, 5, 25, 3); ink.Ring(32, 32, 11, 2); }
                    else { ink.Arrow(32, 49, 10, 14); ink.Arrow(32, 49, 32, 9); ink.Arrow(32, 49, 54, 14); }
                    break;
                case 1:
                    if (hero == HeroClass.Arcanist) { ink.Disc(24, 41, 12); ink.Line(24, 29, 47, 8, 7); ink.Line(38, 38, 56, 20, 3); }
                    else { ink.Ring(32, 38, 18, 3); ink.Line(12, 50, 26, 27, 3); ink.Line(26, 27, 32, 42, 3); ink.Line(32, 42, 47, 11, 5); }
                    break;
                case 2:
                    for (int i = 0; i < 3; i++) ink.Arc(32, 32, 12 + i * 7, i * 95, i * 95 + 225, 3);
                    if (hero == HeroClass.Ranger) { ink.Arrow(18, 10, 18, 44); ink.Arrow(44, 20, 44, 53); }
                    else ink.Disc(32, 32, 5);
                    break;
                case 3:
                    ink.Polygon(new[] { V(10, 14), V(29, 18), V(29, 51), V(10, 47) });
                    ink.Polygon(new[] { V(35, 18), V(54, 14), V(54, 47), V(35, 51) });
                    ink.Line(32, 9, 32, 53, 2);
                    break;
                case 4:
                    if (hero == HeroClass.Vanguard) ink.Shield();
                    else if (hero == HeroClass.Arcanist) ink.Polygon(new[] { V(35, 5), V(15, 35), V(29, 33), V(24, 60), V(50, 25), V(36, 27) });
                    else { ink.Arrow(12, 32, 53, 32); ink.Line(10, 18, 30, 18, 3); ink.Line(7, 46, 33, 46, 3); }
                    break;
                case 5:
                    if (hero == HeroClass.Arcanist) { ink.Shield(); ink.Radial(0, 0, 16, 3); ink.Radial(90, 0, 16, 3); ink.Radial(180, 0, 16, 3); ink.Radial(270, 0, 16, 3); }
                    else if (hero == HeroClass.Vanguard) { ink.Arrow(6, 43, 54, 20); ink.Line(6, 21, 22, 21, 3); ink.Line(10, 53, 31, 53, 3); }
                    else { ink.Line(32, 56, 32, 9, 4); for (int i = 0; i < 3; i++) { ink.Line(32, 22 + i * 12, 14, 12 + i * 12, 4); ink.Line(32, 28 + i * 10, 51, 16 + i * 10, 4); } }
                    break;
                case 6:
                    ink.color = new Color(.48f, 1f, .65f);
                    ink.Ring(32, 32, 24, 2); ink.Line(32, 16, 32, 48, 9); ink.Line(16, 32, 48, 32, 9);
                    break;
                case 7:
                    if (hero == HeroClass.Ranger) { ink.Arrow(8, 18, 52, 18); ink.Arrow(12, 32, 58, 32); ink.Arrow(8, 46, 52, 46); }
                    else if (hero == HeroClass.Arcanist) { for (int i = 0; i < 4; i++) ink.Arc(32, 32, 6 + 6 * i, 60 * i, 280 + 60 * i, 3); }
                    else { ink.Line(9, 52, 22, 36, 5); ink.Line(22, 36, 17, 24, 5); ink.Line(17, 24, 34, 12, 5); ink.Line(30, 54, 40, 35, 4); ink.Line(40, 35, 56, 23, 4); }
                    break;
                case 8:
                    ink.Shield(); ink.Disc(32, 29, 7); ink.Line(32, 37, 32, 44, 3);
                    break;
                default:
                    ink.color = new Color(1, .82f, .4f);
                    for (int i = 0; i < 8; i++) ink.Radial(i * 45, 17, 27, 3);
                    ink.Polygon(new[] { V(32, 12), V(45, 32), V(32, 51), V(19, 32) });
                    break;
            }
            texture = ink.Finish("Skill icon " + key); cache[key] = texture; return texture;
        }

        public static Texture2D Utility(string name)
        {
            if (name == "inventory") name = "bag";
            if (name == "camp") name = "home";
            if (name == "blink") name = "dodge";
            string[] names = { "bag", "skills", "home", "portal", "attack", "dodge", "potion", "pause", "help", "confirm", "cancel", "jump" };
            int id = System.Array.IndexOf(names, name);
            if (id < 0) id = 1;
            int key = 100 + id;
            Texture2D texture;
            if (cache.TryGetValue(key, out texture)) return texture;
            var ink = new Icon(new Color(.8f, .91f, .96f));
            if (id == 0) { ink.Line(22, 19, 22, 10, 3); ink.Line(22, 10, 42, 10, 3); ink.Line(42, 10, 42, 19, 3); ink.Polygon(new[] { V(13, 20), V(51, 20), V(55, 53), V(9, 53) }); }
            else if (id == 1) { for (int i = 0; i < 5; i++) ink.Radial(-90 + i * 72, 7, 25, 4); ink.Ring(32, 32, 12, 3); }
            else if (id == 2) { ink.Line(7, 29, 32, 8, 5); ink.Line(32, 8, 57, 29, 5); ink.Line(16, 25, 16, 53, 5); ink.Line(16, 53, 49, 53, 5); ink.Line(49, 53, 49, 25, 5); }
            else if (id == 3) { ink.Ring(32, 31, 23, 4); ink.Arrow(11, 32, 42, 32); }
            else if (id == 4) ink.Sword(32, 32);
            else if (id == 5) { ink.Arc(40, 32, 17, -90, 90, 4); ink.Arrow(14, 32, 48, 32); ink.Line(10, 20, 20, 20, 3); ink.Line(10, 44, 20, 44, 3); }
            else if (id == 6) { ink.color = new Color(1f, .48f, .52f); ink.Line(25, 9, 39, 9, 5); ink.Line(26, 10, 26, 22, 3); ink.Line(38, 10, 38, 22, 3); ink.Disc(32, 38, 18); ink.color = Color.white; ink.Line(32, 29, 32, 47, 4); ink.Line(23, 38, 41, 38, 4); }
            else if (id == 7) { ink.Line(24, 14, 24, 50, 8); ink.Line(40, 14, 40, 50, 8); }
            else if (id == 8) { ink.Arc(32, 23, 13, 190, 470, 5); ink.Line(32, 36, 32, 42, 5); ink.Disc(32, 52, 3); }
            else if (id == 9) { ink.Line(10, 32, 26, 48, 6); ink.Line(26, 48, 54, 16, 6); }
            else if (id == 10) { ink.Line(16, 16, 48, 48, 6); ink.Line(16, 48, 48, 16, 6); }
            else { ink.Arrow(32, 46, 32, 10); ink.Line(15, 55, 49, 55, 4); }
            texture = ink.Finish("Utility " + name); cache[key] = texture; return texture;
        }

        public static Texture2D Reward(int kind)
        {
            int key=-1000-kind;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white);
            if(kind==0){ink.Disc(32,34,22);ink.color=new Color(.5f,.5f,.5f);ink.Ring(32,34,16,3);ink.Line(32,23,32,45,4);}
            else if(kind==1){ink.Polygon(new[]{V(32,5),V(48,28),V(39,57),V(20,51),V(14,24)});ink.color=new Color(.55f,.55f,.55f);ink.Line(32,7,28,49,3);ink.Line(16,25,46,28,3);}
            else if(kind==2){ink.Arc(32,32,21,-65,245,5);ink.Arc(32,32,12,115,425,4);for(int i=0;i<5;i++)ink.Radial(i*72-90,3,12,3);}
            else{ink.Line(19,51,19,13,5);ink.Line(32,51,32,24,5);ink.Line(45,51,45,34,5);ink.Arrow(13,23,40,9);}
            texture=ink.Finish("Reward resource "+kind);cache[key]=texture;return texture;
        }
        public static void Clear() { foreach (Texture2D texture in cache.Values) if (texture != null) Object.Destroy(texture); cache.Clear(); }
        private static Vector2 V(float x, float y) { return new Vector2(x, y); }
        private sealed class Icon
        {
            private const int Size = 64;
            private readonly Color[] pixels = new Color[Size * Size];
            public Color color;
            private readonly int outputSize;
            public Icon(Color tint, int size = 64) { color = tint; outputSize = size; }
            private void Plot(int x, int y, float alpha)
            {
                if (alpha <= 0 || x < 0 || y < 0 || x >= Size || y >= Size) return;
                Color c = color; c.a = Mathf.Clamp01(alpha);
                int index = (Size - y - 1) * Size + x;
                if (c.a > pixels[index].a) pixels[index] = c;
            }
            public void Line(float ax, float ay, float bx, float by, float thickness)
            {
                if (outputSize < 64) thickness = Mathf.Max(thickness, SkillIconPresentation.MinimumStroke(outputSize));
                Vector2 a = V(ax, ay), d = V(bx - ax, by - ay);
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                {
                    Vector2 p = V(x + .5f, y + .5f);
                    float t = d.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude);
                    Plot(x, y, thickness * .5f + .8f - Vector2.Distance(p, a + d * t));
                }
            }
            public void Disc(float cx, float cy, float radius) { for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) Plot(x, y, radius + .7f - Vector2.Distance(V(x + .5f, y + .5f), V(cx, cy))); }
            public void Ring(float x, float y, float radius, float thickness) { Arc(x, y, radius, 0, 360, thickness); }
            public void Arc(float x, float y, float radius, float start, float end, float thickness)
            {
                Vector2 prev = V(x, y) + V(Mathf.Cos(start * Mathf.Deg2Rad), Mathf.Sin(start * Mathf.Deg2Rad)) * radius;
                for (int i = 1; i <= 36; i++) { float a = Mathf.Lerp(start, end, i / 36f) * Mathf.Deg2Rad; Vector2 next = V(x, y) + V(Mathf.Cos(a), Mathf.Sin(a)) * radius; Line(prev.x, prev.y, next.x, next.y, thickness); prev = next; }
            }
            public void Radial(float angle, float inner, float outer, float thickness) { Vector2 d = V(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)); Line(32 + d.x * inner, 32 + d.y * inner, 32 + d.x * outer, 32 + d.y * outer, thickness); }
            public void Arrow(float x, float y, float tx, float ty) { Vector2 d = (V(tx, ty) - V(x, y)).normalized, n = V(-d.y, d.x); Line(x, y, tx, ty, 3); Vector2 end = V(tx, ty), a = end - d * 9 + n * 6, b = end - d * 9 - n * 6; Line(a.x, a.y, tx, ty, 3); Line(b.x, b.y, tx, ty, 3); }
            public void Sword(float x, float y) { Polygon(new[] { V(x, y - 25), V(x + 5, y - 16), V(x + 4, y + 10), V(x - 4, y + 10), V(x - 5, y - 16) }); Line(x - 14, y + 10, x + 14, y + 10, 4); Line(x, y + 10, x, y + 24, 5); }
            public void Shield() { Vector2[] p = { V(11, 12), V(32, 7), V(53, 12), V(48, 40), V(32, 57), V(16, 40), V(11, 12) }; for (int i = 1; i < p.Length; i++) Line(p[i-1].x, p[i-1].y, p[i].x, p[i].y, 4); }
            public void Polygon(Vector2[] points)
            {
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                {
                    bool inside = false; int j = points.Length - 1;
                    for (int i = 0; i < points.Length; j = i++)
                        if ((points[i].y > y) != (points[j].y > y) && x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x) inside = !inside;
                    if (inside) Plot(x, y, 1);
                }
            }
            public Texture2D Finish(string name)
            {
                Color[] output = pixels;
                if (outputSize != Size)
                {
                    output = new Color[outputSize * outputSize];
                    for(int y=0;y<outputSize;y++)for(int x=0;x<outputSize;x++)
                    {
                        Color sum=Color.clear;int count=0;
                        int left=x*Size/outputSize,right=(x+1)*Size/outputSize,top=y*Size/outputSize,bottom=(y+1)*Size/outputSize;
                        for(int py=top;py<bottom;py++)for(int px=left;px<right;px++){sum+=pixels[py*Size+px];count++;}
                        Color value=sum/Mathf.Max(1,count);
                        if(value.a<.16f)value=Color.clear;
                        output[y*outputSize+x]=value;
                    }
                }
                var texture = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                texture.SetPixels(output); texture.Apply(false, true); return texture;
            }
        }
    }
}
