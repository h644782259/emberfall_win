using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Small original pictograms, rasterized once; no GUI rotation or font glyph dependencies.</summary>
    public static class UIIconAtlas
    {
        private static readonly Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();
        private static readonly Dictionary<int,Texture2D> detailCache=new Dictionary<int,Texture2D>();
        private static readonly Queue<int> detailOrder=new Queue<int>();
        private static readonly Dictionary<Texture2D,System.Func<Texture2D>> detailSources=new Dictionary<Texture2D,System.Func<Texture2D>>();
        public static Texture2D ForDisplay(Texture2D source,float physicalSize)
        {
            System.Func<Texture2D> create;
            return source!=null&&physicalSize>source.width&&detailSources.TryGetValue(source,out create)?create():source;
        }
        private static void StoreDetail(int key,Texture2D texture)
        {
            while(detailCache.Count>=32){int old=detailOrder.Dequeue();Texture2D retired;if(detailCache.TryGetValue(old,out retired)){detailCache.Remove(old);if(retired!=null)Object.Destroy(retired);}}
            detailCache[key]=texture;detailOrder.Enqueue(key);
        }
        // Skill identity colors are shared by glyphs, borders and rank marks.
        private static readonly Color[,] skillColors = {
            { new Color(1f,.68f,.32f), new Color(.9f,.48f,.28f), new Color(1f,.4f,.48f), new Color(.83f,.72f,1f), new Color(1f,.89f,.48f), new Color(.48f,.86f,.92f), new Color(.48f,1f,.65f), new Color(.87f,.65f,.4f), new Color(.73f,.83f,1f), new Color(1f,.82f,.4f) },
            { new Color(.48f,.91f,1f), new Color(1f,.48f,.26f), new Color(.79f,.56f,1f), new Color(.91f,.69f,1f), new Color(1f,.91f,.36f), new Color(.59f,.76f,1f), new Color(.48f,1f,.65f), new Color(.96f,.43f,.84f), new Color(.62f,.88f,.94f), new Color(1f,.82f,.4f) },
            { new Color(.65f,1f,.57f), new Color(1f,.67f,.31f), new Color(.46f,.85f,1f), new Color(1f,.76f,.52f), new Color(.48f,1f,.85f), new Color(.79f,.92f,.29f), new Color(.48f,1f,.65f), new Color(.84f,.61f,1f), new Color(.64f,.86f,1f), new Color(1f,.82f,.4f) },
            { new Color(.81f,.58f,1f), new Color(.77f,.94f,.34f), new Color(.52f,.88f,1f), new Color(.95f,.67f,.88f), new Color(1f,.86f,.43f), new Color(.58f,.73f,1f), new Color(.48f,1f,.65f), new Color(.95f,.48f,.77f), new Color(.67f,.9f,.9f), new Color(.68f,1f,.46f) }
        };
        public static Color SkillColor(HeroClass hero, int skill)
        {
            return skill >= 0 && skill < GameBalance.SkillCount ? skillColors[(int)hero, skill] : Color.white;
        }
        public static Texture2D Skill(HeroClass hero, int skill) { return Skill(hero, skill, 48); }
        public static Texture2D Skill(HeroClass hero, int skill, int requestedSize) { return BuildSkill(hero,skill,requestedSize,false); }
        public static Texture2D SkillGlyph(HeroClass hero,int skill,int requestedSize=48) { return BuildSkill(hero,skill,requestedSize,true); }
        private static Texture2D BuildSkill(HeroClass hero,int skill,int requestedSize,bool monochrome)
        {
            Texture2D painting = AuthoredIconArt.Skill(hero, skill);
            if (painting != null) return painting;
            int rasterSize = Mathf.Max(128,SkillIconPresentation.RasterSize(requestedSize));
            int key = (monochrome?1000000:0)+rasterSize * 1000 + (int)hero * 10 + skill;
            Texture2D texture;
            if (cache.TryGetValue(key, out texture)) return texture;
            var ink = new Icon(SkillColor(hero, skill), rasterSize);
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
                    ink.Arrow(9, 53, 53, 9); ink.Line(12, 44, 25, 51, 3); ink.Line(17, 33, 32, 42, 3);
                }
                else
                {
                    ink.Disc(32, 32, 6); ink.Ring(32, 32, 17, 3);
                    ink.Arrow(5, 5, 22, 22); ink.Arrow(59, 59, 42, 42);
                }
                texture = ink.Finish("Skill icon " + key,monochrome); cache[key] = texture; return texture;
            }
            if (hero == HeroClass.Summoner && skill == 9)
            {
                ink.Line(32, 54, 32, 24, 8); ink.Line(32, 35, 13, 22, 5); ink.Line(32, 35, 51, 22, 5);
                ink.Disc(32, 16, 11); ink.Disc(14, 19, 9); ink.Disc(50, 19, 9);
                texture = ink.Finish("Skill icon " + key,monochrome); cache[key] = texture; return texture;
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
                    else { ink.Disc(30,12,5); ink.Line(30,19,34,31,6); ink.Line(34,31,46,37,5); ink.Arrow(16,15,16,39); ink.color=new Color(1f,.63f,.22f); ink.Polygon(new[]{V(8,53),V(20,39),V(30,49),V(42,39),V(57,53)}); }
                    break;
                case 5:
                    if (hero == HeroClass.Arcanist) { ink.color=new Color(1f,.35f,.12f); ink.Polygon(new[]{V(7,51),V(16,28),V(24,40),V(39,19),V(36,39),V(56,29),V(49,54)}); ink.color=new Color(1f,.9f,.46f); ink.Disc(28,11,5); ink.Line(28,19,30,31,6); ink.Line(30,31,42,32,5); ink.Line(42,32,43,42,4); }
                    else if (hero == HeroClass.Vanguard) { ink.Arrow(6, 43, 54, 20); ink.Line(6, 21, 22, 21, 3); ink.Line(10, 53, 31, 53, 3); }
                    else { ink.Line(32, 56, 32, 9, 4); for (int i = 0; i < 3; i++) { ink.Line(32, 22 + i * 12, 14, 12 + i * 12, 4); ink.Line(32, 28 + i * 10, 51, 16 + i * 10, 4); } }
                    break;
                case 6:
                    if(hero==HeroClass.Vanguard) { ink.color = new Color(.48f, 1f, .65f); ink.Ring(32, 32, 24, 2); ink.Line(32, 16, 32, 48, 9); ink.Line(16, 32, 48, 32, 9); }
                    else if(hero==HeroClass.Ranger) { for(int i=0;i<3;i++)ink.Arrow(8,18+i*14,54,10+i*18); }
                    else if(hero==HeroClass.Summoner) { ink.Disc(32,32,7);ink.Line(29,32,10,14,5);ink.Line(35,32,54,14,5);ink.Line(29,36,8,28,4);ink.Line(35,36,56,28,4);ink.Arrow(32,54,32,39); }
                    else { ink.Ring(32,32,17,3);for(int i=0;i<6;i++)ink.Radial(i*60,21,29,3);ink.Arrow(23,48,41,16); }
                    break;
                case 7:
                    if (hero == HeroClass.Ranger) { ink.Arrow(8, 18, 52, 18); ink.Arrow(12, 32, 58, 32); ink.Arrow(8, 46, 52, 46); }
                    else if (hero == HeroClass.Arcanist) { for (int i = 0; i < 4; i++) ink.Arc(32, 32, 6 + 6 * i, 60 * i, 280 + 60 * i, 3); }
                    else { ink.Line(9, 52, 22, 36, 5); ink.Line(22, 36, 17, 24, 5); ink.Line(17, 24, 34, 12, 5); ink.Line(30, 54, 40, 35, 4); ink.Line(40, 35, 56, 23, 4); }
                    break;
                case 8:
                    if(hero==HeroClass.Vanguard) { ink.Shield(); ink.Disc(32,29,7); } else { ink.Arc(32,32,20,30,300,4); ink.Arrow(14,46,49,15); }
                    break;
                default:
                    ink.color = SkillColor(hero, skill);
                    for (int i = 0; i < 8; i++) ink.Radial(i * 45, 17, 27, 3);
                    ink.Polygon(new[] { V(32, 12), V(45, 32), V(32, 51), V(19, 32) });
                    break;
            }
            texture = ink.Finish("Skill icon " + key,monochrome); cache[key] = texture; return texture;
        }

        public static Texture2D ControlDisc()
        {const int key=2000000;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;var ink=new Icon(Color.white,48);ink.Disc(32,32,30);texture=ink.Finish("Circular battle control");cache[key]=texture;return texture;}
        public static Texture2D ControlRing(bool glow=false)
        {
            int key=glow?2000002:2000001;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white,64);ink.Ring(32,32,30,glow?4f:1.1f);
            texture=ink.Finish(glow?"Soft control halo":"Continuous control rim");cache[key]=texture;return texture;
        }
        public static Texture2D CompanionCommand(bool recall,bool recalled=false)
        {
            int key=recall?(recalled?2000012:2000011):2000010;
            Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white,64);
            if(!recall)
            {ink.Ring(32,32,17,3);ink.Disc(32,32,4);ink.Line(32,5,32,20,3);ink.Line(32,44,32,59,3);ink.Line(5,32,20,32,3);ink.Line(44,32,59,32,3);}
            else
            {
                // Spirit silhouette and a return/resume arrow, independent of font glyphs.
                ink.Polygon(new[]{V(22,27),V(31,19),V(40,28),V(43,47),V(31,43),V(19,47)});
                ink.Disc(27,30,2);ink.Disc(35,30,2);
                ink.Arc(32,31,25,190,350,3);
                if(recalled)ink.Arrow(39,9,56,23);else ink.Arrow(19,9,7,24);
            }
            texture=ink.Finish(recall?(recalled?"Companions resume":"Companions recall"):"Companions focus",true);cache[key]=texture;return texture;
        }
        public static Texture2D SkillPageArrow()
        {
            const int key=2000003;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white,48);ink.Arrow(15,23,48,23);ink.Arrow(48,41,15,41);
            texture=ink.Finish("Skill page switch arrows",true);cache[key]=texture;return texture;
        }

        public static Texture2D Utility(string name,bool highResolution=false)
        {
            Texture2D painting = AuthoredIconArt.Utility(name);
            if (painting != null) return painting;
            var targetCache=highResolution?detailCache:cache;
            if (name == "inventory") name = "bag";
            if (name == "camp") name = "home";
            if (name == "blink") name = "dodge";
            string[] names = { "bag", "skills", "home", "portal", "attack", "dodge", "potion", "pause", "help", "confirm", "cancel", "jump", "codex", "coin", "shard", "compare", "save", "apply", "reset", "upgrade", "core", "lock", "settings", "shop", "smith", "gem", "critical" };
            int id = System.Array.IndexOf(names, name);
            if (id < 0) id = 1;
            int key = 100 + id;
            Texture2D texture;
            if (targetCache.TryGetValue(key, out texture)) return texture;
            var ink = new Icon(new Color(.8f, .91f, .96f),highResolution?512:128);
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
            else if(id==26)
            {
                // A broad comic impact with a quiet centre for the damage number.
                Vector2[] burst={V(6,15),V(20,19),V(22,5),V(31,17),V(42,7),V(42,21),V(59,17),V(49,30),V(61,39),V(46,41),V(47,56),V(34,47),V(23,59),V(22,44),V(6,49),V(14,35),V(3,28),V(16,26)};
                ink.color=new Color(.43f,.075f,.055f,.9f);ink.Polygon(burst);
                for(int i=0;i<burst.Length;i++)burst[i]=V(32+(burst[i].x-32)*.88f,32+(burst[i].y-32)*.88f);
                ink.color=new Color(1f,.48f,.16f);ink.Polygon(burst);
                for(int i=0;i<burst.Length;i++)burst[i]=V(32+(burst[i].x-32)*.83f,32+(burst[i].y-32)*.83f);
                ink.color=new Color(1f,.92f,.70f);ink.Polygon(burst);
                ink.color=new Color(1f,.98f,.86f);ink.Disc(32,32,17);
            }
            else if(id==12){ink.color=new Color(.58f,.83f,1f);ink.Polygon(new[]{V(8,13),V(28,17),V(32,22),V(36,17),V(56,13),V(56,49),V(36,53),V(32,57),V(28,53),V(8,49)});ink.color=new Color(1f,.78f,.28f);ink.Line(32,22,32,54,4);ink.Line(14,24,24,27,3);ink.Line(40,27,50,24,3);}
            else if(id==13){ink.color=new Color(1f,.78f,.22f);ink.Disc(32,32,24);ink.color=new Color(.62f,.38f,.08f);ink.Ring(32,32,17,3);ink.Line(32,20,32,44,4);}
            else if(id==16){ink.Polygon(new[]{V(10,9),V(49,9),V(55,16),V(55,55),V(10,55)});ink.color=new Color(.1f,.2f,.25f);ink.Line(22,12,22,28,5);ink.Line(22,28,43,28,5);ink.Line(21,43,44,43,5);}
            else if(id==17){ink.Line(10,12,33,12,4);ink.Line(10,12,10,53,4);ink.Line(10,53,33,53,4);ink.Arrow(25,32,55,32);}
            else if(id==18){ink.Arc(32,32,22,0,290,4);ink.Arrow(11,29,10,9);}
            else if(id==19){ink.Line(32,10,32,54,6);ink.Line(10,32,54,32,6);}
            else if(id==20){ink.Ring(32,32,23,3);ink.Polygon(new[]{V(32,13),V(47,32),V(32,51),V(17,32)});}
            else if(id==21){ink.Line(20,27,20,15,4);ink.Arc(32,16,12,180,360,4);ink.Line(44,15,44,27,4);ink.Polygon(new[]{V(14,28),V(50,28),V(50,55),V(14,55)});}
            else if(id==23){ink.Line(12,29,12,53,4);ink.Line(12,53,52,53,4);ink.Line(52,53,52,29,4);ink.Polygon(new[]{V(8,25),V(15,11),V(49,11),V(56,25)});ink.Line(23,34,23,52,4);ink.Line(23,34,39,34,4);ink.Line(39,34,39,52,4);}
            else if(id==25){ink.Polygon(new[]{V(20,10),V(44,10),V(56,26),V(32,55),V(8,26)});ink.color=new Color(.12f,.2f,.3f);ink.Line(9,26,55,26,2);ink.Line(20,11,32,54,2);ink.Line(44,11,32,54,2);}
            else if(id==24){ink.Line(20,51,41,23,7);ink.Polygon(new[]{V(22,16),V(31,7),V(56,27),V(47,37)});ink.Line(10,56,54,56,4);}
            else if(id==22){ink.Ring(32,32,18,6);ink.Ring(32,32,7,3);for(int tooth=0;tooth<8;tooth++)ink.Radial(tooth*45,19,27,7);}
            else if(id==15){ink.Line(12,12,12,52,4);ink.Line(26,22,26,52,4);ink.Line(40,12,40,52,4);ink.Line(54,22,54,52,4);ink.Arrow(19,10,47,10);}
            else if(id==14){ink.color=new Color(.68f,.63f,1f);ink.Polygon(new[]{V(32,6),V(51,28),V(39,56),V(18,48),V(13,23)});ink.color=Color.white;ink.Line(32,9,27,44,3);}
            else { ink.Arrow(32, 46, 32, 10); ink.Line(15, 55, 49, 55, 4); }
            texture = ink.Finish("Utility " + name); if(highResolution)StoreDetail(key,texture);else{cache[key]=texture;detailSources[texture]=()=>Utility(name,true);} return texture;
        }

        public static Texture2D Mastery(MasteryType mastery)
        {
            int key=-5000-(int)mastery;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white);
            if(mastery==MasteryType.Offense){ink.Sword(23,32);ink.Arrow(40,48,54,15);ink.Line(34,41,49,47,4);}
            else if(mastery==MasteryType.Vitality){ink.Polygon(new[]{V(32,54),V(9,32),V(11,17),V(23,10),V(32,19),V(41,10),V(53,17),V(55,32)});ink.color=new Color(.1f,.22f,.17f);ink.Line(32,24,32,42,4);ink.Line(23,33,41,33,4);}
            else if(mastery==MasteryType.Guard){ink.Shield();ink.color=new Color(.12f,.2f,.26f);ink.Line(32,19,32,43,4);}
            else{ink.Ring(32,32,22,3);ink.Arrow(14,41,45,20);ink.Line(12,19,23,19,3);ink.Line(42,47,53,47,3);}
            texture=ink.Finish("Mastery branch "+mastery,true);cache[key]=texture;return texture;
        }
        public static Texture2D NpcDialogCapsule(bool outline=false)
        {
            int key=outline?2000006:2000005;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            const int w=192,h=88;var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float cx=Mathf.Clamp(x+.5f,44,w-44),dx=x+.5f-cx,dy=y+.5f-44;
                float distance=Mathf.Sqrt(dx*dx+dy*dy);
                float alpha=outline?Mathf.Clamp01(1.5f-Mathf.Abs(distance-42)):Mathf.Clamp01(43-distance);
                pixels[y*w+x]=new Color(1,1,1,alpha);
            }
            texture=new Texture2D(w,h,TextureFormat.RGBA32,false){name="NPC dialogue capsule",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixels(pixels);texture.Apply(false,true);cache[key]=texture;return texture;
        }
        public static Texture2D EquipmentUpgradeArrow()
        {
            const int key=2000004;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white);float px=8,py=54;
            for(int step=1;step<=24;step++)
            {float t=step/24f,x=8+44*t,y=54-44*t*t*t;ink.Line(px,py,x,y,6);px=x;py=y;}
            ink.Line(39,19,52,10,6);ink.Line(52,10,57,25,6);
            texture=ink.Finish("Curved equipment upgrade arrow");cache[key]=texture;return texture;
        }
        public static Texture2D StatTrendArrow(bool up)
        {
            int key=up?-5090:-5091;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white);float px=12,py=up?51:13;
            for(int step=1;step<=24;step++)
            {
                float t=step/24f,x=12+36*t,y=51-38*t*t;
                if(!up)y=64-y;ink.Line(px,py,x,y,5);px=x;py=y;
            }
            ink.Line(33,up?17:47,48,up?13:51,5);
            ink.Line(48,up?13:51,52,up?28:36,5);
            texture=ink.Finish(up?"Attribute increase":"Attribute decrease");cache[key]=texture;return texture;
        }
        public static Texture2D EquipmentLock(bool locked)
        {
            int key=locked?-4010:-4011;Texture2D texture;if(cache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(locked?new Color(1f,.78f,.25f):new Color(.66f,.76f,.78f));
            ink.Polygon(new[]{V(16,29),V(48,29),V(48,56),V(16,56)});
            ink.Arc(locked?32:42,29,13,180,360,5);ink.color=new Color(.09f,.15f,.19f);ink.Line(32,39,32,48,4);
            texture=ink.Finish(locked?"Locked equipment":"Unlocked equipment");cache[key]=texture;return texture;
        }
        public static Texture2D EquipmentCardIcon(ItemSlot slot,int level=1,Rarity rarity=Rarity.Common,HeroClass hero=HeroClass.Vanguard,bool highResolution=false)
        {
            Texture2D painting = AuthoredIconArt.Equipment(slot, hero, rarity);
            if (painting != null) return painting;
            var targetCache=highResolution?detailCache:cache;
            int tier=Mathf.Clamp(level/10,0,10),rank=Mathf.Clamp((int)rarity,0,3),key=-3000000-(int)slot*10000-tier*100-rank*10-(int)hero;
            Texture2D texture;if(targetCache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white,highResolution?512:128);
            IconQualityHalo(ink,rank);
            if(slot==ItemSlot.Weapon&&hero==HeroClass.Ranger)
            {
                ink.color=Color.white;ink.Arc(15,32,22+tier*.3f,-75,75,4+tier*.15f);ink.Line(21,10,21,54,2);ink.Arrow(12,32,54,32);
                for(int side=-1;side<=1;side+=2)ink.Line(23,32+side*12,30+rank*3,32+side*(17+tier*.4f),3);
            }
            else if(slot==ItemSlot.Weapon&&hero!=HeroClass.Vanguard)
            {
                ink.color=Color.white;ink.Line(28,56,33,23,5+tier*.18f);
                if(hero==HeroClass.Arcanist){ink.Polygon(new[]{V(33,6),V(43+tier*.3f,17),V(33,29),V(22-tier*.3f,17)});ink.Ring(33,17,14+rank,2);}
                else{ink.Arc(33,19,14,195,345,4);ink.Line(21,17,16,8,3);ink.Line(44,17,49,8,3);ink.Polygon(new[]{V(33,11),V(41,19),V(33,27),V(25,19)});}
            }
            else if(slot==ItemSlot.Weapon)
            {
                float breadth=3+tier*.65f;
                ink.Polygon(new[]{V(32,4),V(32+breadth,15),V(32+breadth,39),V(32-breadth,39),V(32-breadth,15)});
                ink.color=new Color(.33f,.55f,.7f);ink.Line(32,12,32,36,2);
                ink.color=Color.white;ink.Line(21-tier*.65f,41,43+tier*.65f,41,4);ink.Line(32,43,32,56,5);ink.Disc(32,58,3);
                if(tier>=3){ink.Line(21-tier*.65f,41,19-tier*.65f,34,3);ink.Line(43+tier*.65f,41,45+tier*.65f,34,3);}
                if(tier>=6){ink.Polygon(new[]{V(25,24),V(18,17),V(21,33),V(26,36)});ink.Polygon(new[]{V(39,24),V(46,17),V(43,33),V(38,36)});}
            }
            else if(slot==ItemSlot.Armor)
            {
                ink.Polygon(new[]{V(22,11),V(27,17),V(37,17),V(42,11),V(53,24),V(45,33),V(44,55),V(20,55),V(19,33),V(11,24)});
                if(tier>=2)for(int side=-1;side<=1;side+=2)ink.Polygon(new[]{V(32+side*10,16),V(32+side*(19+tier*.5f),13),V(32+side*(23+tier*.35f),28),V(32+side*13,31)});
                ink.color=new Color(.3f,.55f,.7f);for(int plate=0;plate<=tier/2;plate++)ink.Line(24,24+plate*5,40,24+plate*5,2);
                if(tier>=6){ink.color=Color.white;ink.Polygon(new[]{V(32,20),V(39,29),V(32,38),V(25,29)});}
            }
            else
            {
                ink.Ring(32,20,15+tier*.3f,2+tier*.2f);
                ink.Polygon(new[]{V(32,29-tier*.5f),V(42+tier*.6f,43),V(32,57),V(22-tier*.6f,43)});
                if(tier>=3)for(int side=-1;side<=1;side+=2)ink.Line(32+side*14,30,32+side*(18+tier*.5f),48,3);
                ink.color=new Color(.3f,.55f,.7f);ink.Disc(32,42,3+tier*.3f);
            }
            // Small engraved marks distinguish adjacent ten-level sets without relying on rarity tint.
            ink.color=new Color(.75f,.85f,1f);
            for(int mark=0;mark<tier;mark++)ink.Line(5+(mark%5)*3,53+(mark/5)*5,6+(mark%5)*3,53+(mark/5)*5,2);
            if(slot!=ItemSlot.Weapon)
            {
                ink.color=new Color(.2f,.36f,.48f);
                if(hero==HeroClass.Vanguard)ink.Polygon(new[]{V(32,23),V(37,28),V(32,33),V(27,28)});
                else if(hero==HeroClass.Arcanist){ink.Ring(32,28,6,2);ink.Line(32,20,32,36,2);}
                else if(hero==HeroClass.Ranger)ink.Arrow(25,35,39,21);
                else{ink.Line(32,23,32,35,2);ink.Line(32,28,25,23,2);ink.Line(32,28,39,23,2);}
            }
            IconQualityDetails(ink,rank);
            texture=ink.Finish("Equipment "+hero+" "+slot+" tier "+tier+" quality "+rank);if(highResolution)StoreDetail(key,texture);else{cache[key]=texture;detailSources[texture]=()=>EquipmentCardIcon(slot,level,rarity,hero,true);}return texture;
        }
        private static void IconQualityHalo(Icon ink,int rank)
        {
            ink.Layered=true;
            if(rank<2)return;
            ink.color=new Color(.75f,.85f,1f,.16f+rank*.05f);ink.Ring(32,32,26,rank==3?8:5);
            ink.color=Color.white;
        }
        private static void IconQualityDetails(Icon ink,int rank)
        {
            ink.color=Color.white;
            if(rank>=1){ink.Line(8,24,8,40,2);ink.Line(56,24,56,40,2);ink.Polygon(new[]{V(32,34-rank),V(35+rank,39),V(32,44+rank),V(29-rank,39)});}
            if(rank>=2){ink.Line(9,18,15,12,2);ink.Line(49,12,55,18,2);}
            if(rank==3)
            {
                ink.Arc(32,32,26,210,330,2);ink.Arc(32,32,26,30,150,2);
                for(int side=-1;side<=1;side+=2){ink.Line(32+side*25,42,32+side*25,54,2);ink.Line(32+side*21,48,32+side*29,48,2);}
            }
        }
        public static Texture2D FashionCardIcon(FashionSlot slot,int appearanceTier=3,HeroClass hero=HeroClass.Vanguard,bool highResolution=false)
        {
            Texture2D painting = AuthoredIconArt.Fashion(slot, appearanceTier, hero);
            if (painting != null) return painting;
            var targetCache=highResolution?detailCache:cache;
            int tier=Mathf.Clamp(appearanceTier,0,3),key=-4000000-(int)slot*100-tier*10-(int)hero;
            Texture2D texture;if(targetCache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white,highResolution?512:128);IconQualityHalo(ink,3);
            if(slot==FashionSlot.Weapon)
            {
                if(hero==HeroClass.Ranger){ink.Arc(15,32,24,-80,80,5);ink.Line(19,7,19,57,2);ink.Arrow(13,32,55,32);}
                else if(hero==HeroClass.Vanguard){ink.Sword(32,32);}
                else{ink.Line(30,57,32,24,5);ink.Ring(32,17,12+tier,3);ink.Polygon(new[]{V(32,7),V(40,17),V(32,27),V(24,17)});}
                for(int side=-1;side<=1;side+=2)for(int layer=0;layer<=tier;layer++)
                    ink.Line(32+side*5,29+layer*5,32+side*(12+layer*3),22+layer*6,3);
            }
            else
            {
                for(int side=-1;side<=1;side+=2)for(int feather=0;feather<3+tier;feather++)
                {
                    float tip=11+feather*4;
                    if(tier>=2)ink.Polygon(new[]{V(32+side*4,39),V(32+side*tip,8+feather*7),V(32+side*(tip-4),34+feather*3)});
                    else ink.Line(32+side*3,40-feather*3,32+side*tip,10+feather*9,5);
                }
                if(tier==3){ink.Ring(32,26,12,2);ink.Disc(32,26,4);}
            }
            IconQualityDetails(ink,3);ink.color=Color.white;
            for(int mark=0;mark<=tier;mark++)ink.Disc(25+mark*5,58,1.6f);
            texture=ink.Finish("Legendary fashion "+hero+" "+slot+" design "+tier);if(highResolution)StoreDetail(key,texture);else{cache[key]=texture;detailSources[texture]=()=>FashionCardIcon(slot,appearanceTier,hero,true);}return texture;
        }
        public static Texture2D Reward(int kind,bool highResolution=false)
        {
            Texture2D painting = kind >= 0 && kind <= 2 ? AuthoredIconArt.Load("resources/" + (kind == 0 ? "01" : kind == 1 ? "04" : "05")) : null;
            if (painting != null) return painting;
            var targetCache=highResolution?detailCache:cache;
            int key=-1000-kind;Texture2D texture;if(targetCache.TryGetValue(key,out texture))return texture;
            var ink=new Icon(Color.white,highResolution?512:128);
            if(kind==0){ink.Disc(32,34,22);ink.color=new Color(.5f,.5f,.5f);ink.Ring(32,34,16,3);ink.Line(32,23,32,45,4);}
            else if(kind==1){ink.Polygon(new[]{V(32,5),V(48,28),V(39,57),V(20,51),V(14,24)});ink.color=new Color(.55f,.55f,.55f);ink.Line(32,7,28,49,3);ink.Line(16,25,46,28,3);}
            else if(kind==2){ink.Arc(32,32,21,-65,245,5);ink.Arc(32,32,12,115,425,4);for(int i=0;i<5;i++)ink.Radial(i*72-90,3,12,3);}
            else{ink.Line(19,51,19,13,5);ink.Line(32,51,32,24,5);ink.Line(45,51,45,34,5);ink.Arrow(13,23,40,9);}
            texture=ink.Finish("Reward resource "+kind);if(highResolution)StoreDetail(key,texture);else{cache[key]=texture;detailSources[texture]=()=>Reward(kind,true);}return texture;
        }
        public static void Clear() { foreach (Texture2D texture in cache.Values) if (texture != null) Object.Destroy(texture); cache.Clear();foreach(var texture in detailCache.Values)if(texture!=null)Object.Destroy(texture);detailCache.Clear();detailOrder.Clear();detailSources.Clear(); }
        private static Vector2 V(float x, float y) { return new Vector2(x, y); }
        private sealed class Icon
        {
            private readonly int Size;
            private readonly float RasterScale;
            private readonly Color[] pixels;
            public Color color;
            public bool Layered;
            private readonly int outputSize;
            public Icon(Color tint, int size = 128) { color=tint;Size=outputSize=Mathf.Clamp(size,128,512);RasterScale=Size/64f;pixels=new Color[Size*Size]; }
            private void Plot(int x, int y, float alpha)
            {
                if (alpha <= 0 || x < 0 || y < 0 || x >= Size || y >= Size) return;
                Color c = color; c.a = Mathf.Clamp01(alpha)*color.a;
                int index = (Size - y - 1) * Size + x;
                if(!Layered){if(c.a>pixels[index].a)pixels[index]=c;return;}
                Color previous=pixels[index];float combined=c.a+previous.a*(1-c.a);
                Color blended=(c*c.a+previous*(previous.a*(1-c.a)))/combined;blended.a=combined;pixels[index]=blended;
            }
            public void Line(float ax, float ay, float bx, float by, float thickness)
            {
                if (outputSize < 64) thickness = Mathf.Max(thickness, SkillIconPresentation.MinimumStroke(outputSize));
                Vector2 a = V(ax, ay), d = V(bx - ax, by - ay);
                float pad=thickness*.5f+1f/RasterScale;
                int x0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(ax,bx)-pad)*RasterScale),0,Size-1),x1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(ax,bx)+pad)*RasterScale),0,Size-1);
                int y0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(ay,by)-pad)*RasterScale),0,Size-1),y1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(ay,by)+pad)*RasterScale),0,Size-1);
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    Vector2 p = V((x + .5f)/RasterScale, (y + .5f)/RasterScale);
                    float t = d.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude);
                    Plot(x, y, (thickness * .5f + .8f/RasterScale - Vector2.Distance(p, a + d * t))*RasterScale);
                }
            }
            public void Disc(float cx, float cy, float radius) {
                float pad=radius+1f/RasterScale;
                int x0=Mathf.Clamp(Mathf.FloorToInt((cx-pad)*RasterScale),0,Size-1),x1=Mathf.Clamp(Mathf.CeilToInt((cx+pad)*RasterScale),0,Size-1);
                int y0=Mathf.Clamp(Mathf.FloorToInt((cy-pad)*RasterScale),0,Size-1),y1=Mathf.Clamp(Mathf.CeilToInt((cy+pad)*RasterScale),0,Size-1);
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Plot(x, y, (radius + .7f/RasterScale - Vector2.Distance(V((x + .5f)/RasterScale, (y + .5f)/RasterScale), V(cx, cy)))*RasterScale); }
            public void Ring(float x, float y, float radius, float thickness) { Arc(x, y, radius, 0, 360, thickness); }
            public void Arc(float x, float y, float radius, float start, float end, float thickness)
            {
                Vector2 prev = V(x, y) + V(Mathf.Cos(start * Mathf.Deg2Rad), Mathf.Sin(start * Mathf.Deg2Rad)) * radius;
                int segments=Mathf.Max(36,Mathf.CeilToInt(Mathf.Abs(end-start)*Size/2048f));
                for (int i = 1; i <= segments; i++) { float a = Mathf.Lerp(start, end, i / (float)segments) * Mathf.Deg2Rad; Vector2 next = V(x, y) + V(Mathf.Cos(a), Mathf.Sin(a)) * radius; Line(prev.x, prev.y, next.x, next.y, thickness); prev = next; }
            }
            public void Radial(float angle, float inner, float outer, float thickness) { Vector2 d = V(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)); Line(32 + d.x * inner, 32 + d.y * inner, 32 + d.x * outer, 32 + d.y * outer, thickness); }
            public void Arrow(float x, float y, float tx, float ty) { Vector2 d = (V(tx, ty) - V(x, y)).normalized, n = V(-d.y, d.x); Line(x, y, tx, ty, 3); Vector2 end = V(tx, ty), a = end - d * 9 + n * 6, b = end - d * 9 - n * 6; Line(a.x, a.y, tx, ty, 3); Line(b.x, b.y, tx, ty, 3); }
            public void Sword(float x, float y) { Polygon(new[] { V(x, y - 25), V(x + 5, y - 16), V(x + 4, y + 10), V(x - 4, y + 10), V(x - 5, y - 16) }); Line(x - 14, y + 10, x + 14, y + 10, 4); Line(x, y + 10, x, y + 24, 5); }
            public void Shield() { Vector2[] p = { V(11, 12), V(32, 7), V(53, 12), V(48, 40), V(32, 57), V(16, 40), V(11, 12) }; for (int i = 1; i < p.Length; i++) Line(p[i-1].x, p[i-1].y, p[i].x, p[i].y, 4); }
            public void Polygon(Vector2[] points)
            {
                if(points.Length<3)return;
                float minX=points[0].x,maxX=minX,minY=points[0].y,maxY=minY;
                foreach(var point in points){minX=Mathf.Min(minX,point.x);maxX=Mathf.Max(maxX,point.x);minY=Mathf.Min(minY,point.y);maxY=Mathf.Max(maxY,point.y);}
                int x0=Mathf.Clamp(Mathf.FloorToInt(minX*RasterScale),0,Size-1),x1=Mathf.Clamp(Mathf.CeilToInt(maxX*RasterScale),0,Size-1);
                int y0=Mathf.Clamp(Mathf.FloorToInt(minY*RasterScale),0,Size-1),y1=Mathf.Clamp(Mathf.CeilToInt(maxY*RasterScale),0,Size-1);
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    float sx=(x+.5f)/RasterScale,sy=(y+.5f)/RasterScale;
                    bool inside = false; int j = points.Length - 1;
                    for (int i = 0; i < points.Length; j = i++)
                        if ((points[i].y > sy) != (points[j].y > sy) && sx < (points[j].x - points[i].x) * (sy - points[i].y) / (points[j].y - points[i].y) + points[i].x) inside = !inside;
                    if(Size<=128){if(inside)Plot(x,y,1);}
                    else {
                        int coverage=0;
                        for(int sample=0;sample<4;sample++){
                            float sampleX=(x+((sample&1)==0?.25f:.75f))/RasterScale,sampleY=(y+(sample<2?.25f:.75f))/RasterScale;bool hit=false;int prior=points.Length-1;
                            for(int point=0;point<points.Length;prior=point++)if((points[point].y>sampleY)!=(points[prior].y>sampleY)&&sampleX<(points[prior].x-points[point].x)*(sampleY-points[point].y)/(points[prior].y-points[point].y)+points[point].x)hit=!hit;
                            if(hit)coverage++;
                        }
                        Plot(x,y,coverage*.25f);
                    }
                }
            }
            public Texture2D Finish(string name,bool monochrome=false)
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
                if(monochrome)for(int i=0;i<output.Length;i++)output[i]=new Color(1,1,1,output[i].a);
                var texture = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                texture.SetPixels(output); texture.Apply(false, true); return texture;
            }
        }
    }
}
