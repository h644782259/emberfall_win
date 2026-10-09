using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Entire prototype scene is authored here so a fresh checkout needs no imported art.
    public static partial class WorldBuilder
    {
        public static GameObject Build(ZoneKind zone, int dungeonLayout = 0, int campProgress = 0, int hub = 0, int chapterSeed = 0)
        {
            WorldTraversal.Reset(zone);
            GameObject root = new GameObject(zone == ZoneKind.Wilderness ? "Windwhisper Fields" : "Fallen Star Sanctum");
            WorldResources resources = root.AddComponent<WorldResources>();
            bool dungeon = zone == ZoneKind.Dungeon;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = dungeon ? new Color(.27f,.32f,.48f) : new Color(.52f,.62f,.72f);
            RenderSettings.ambientEquatorColor = dungeon ? new Color(.15f,.17f,.27f) : new Color(.30f,.39f,.38f);
            RenderSettings.ambientGroundColor = dungeon ? new Color(.09f,.085f,.15f) : new Color(.19f,.23f,.20f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = dungeon ? new Color(.055f, .055f, .11f) : new Color(.1f, .18f, .24f);
            RenderSettings.fogDensity = dungeon ? .016f : .009f;

            GameObject sunlight = new GameObject("Sun");
            sunlight.transform.SetParent(root.transform);
            sunlight.transform.rotation = Quaternion.Euler(dungeon ? 48 : 42, -35, 0);
            Light light = sunlight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = dungeon ? new Color(.64f, .72f, 1) : new Color(1, .88f, .66f);
            light.intensity = dungeon ? 1.1f : 1.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .65f;
            light.shadowBias = .045f; light.shadowNormalBias = .2f;
            GameObject fill = new GameObject("Cool silhouette fill");
            fill.transform.SetParent(root.transform, false);
            fill.transform.rotation = Quaternion.Euler(25f,145f,0);
            Light rim = fill.AddComponent<Light>(); rim.type = LightType.Directional;
            rim.color = dungeon ? new Color(.34f,.43f,.8f) : new Color(.44f,.65f,.77f);
            rim.intensity = dungeon ? .28f : .32f; rim.shadows = LightShadows.None;
            ApplyEnvironmentLighting(dungeon,hub,light,rim);
            if (dungeon) { if(ChapterRoomGeometry.IsChapterLayout(dungeonLayout))BuildChapterRoom(root.transform,resources,ChapterRoomGeometry.FromLayout(dungeonLayout,chapterSeed));else if(dungeonLayout>=20)BuildTacticalRoom(root.transform,resources,dungeonLayout);else if(dungeonLayout>=10)BuildLinkedRoom(root.transform,resources,dungeonLayout-10);else if(dungeonLayout>=2)BuildChallengeArena(root.transform,resources,dungeonLayout-2);else BuildDungeon(root.transform, resources, dungeonLayout); } else { if(hub==0){BuildWilderness(root.transform, resources); BuildCampFacilities(root.transform, resources, campProgress);BuildHubNpcs(root.transform,resources);}else BuildTown(root.transform,resources,hub); }
            if(!dungeon){BuildHubLightPools(root.transform,hub);BuildClimbableProps(root.transform,resources);BuildTravelStation(root.transform,resources,hub);}
            if(dungeon)BuildBreakablePockets(root.transform,dungeonLayout);
            return root;
        }

        private static void BuildWilderness(Transform parent, WorldResources r)
        {
            Material grass = r.Material(new Color(.18f, .32f, .28f));
            Material darkRock = r.Material(new Color(.14f, .2f, .25f));
            Material stone = r.Material(new Color(.37f, .45f, .43f));
            Material edge = r.Material(new Color(.22f, .29f, .3f));
            Material gold = r.Material(new Color(.76f, .57f, .28f));
            Material jade = r.Material(new Color(.24f, .91f, .77f), true);
            Primitive(parent, "Floating island", PrimitiveType.Cylinder, new Vector3(0, -1.4f, 0), new Vector3(49, 1.15f, 49), darkRock);
            Primitive(parent, "Moss rim", PrimitiveType.Cylinder, new Vector3(0, -.45f, 0), new Vector3(48, .35f, 48), edge);
            // Ground traversal and raised blockers share the same authored layout.
            Vector2[] coast = new Vector2[20];
            for (int i = 0; i < coast.Length; i++)
            {
                float a = i * Mathf.PI * 2 / coast.Length;
                float radius = 25.7f + Mathf.Sin(i * 2.1f) * 1.1f + Mathf.Cos(i * .8f) * .9f;
                coast[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
            }
            Surface(parent, r, "Irregular meadow shoreline", coast, 0, grass);
            Skirt(parent, r, "Fractured meadow cliffs", coast, -2.8f, darkRock);
            Transform woodland = Region(parent, "West woodland and fern trail");
            Transform lowland = Region(parent, "Deep brook and timber crossing");
            Transform ruins = Region(parent, "Northeast overgrown courtyard");
            Transform camp = Region(parent, "Southern caravan approach");
            Material forestFloor = r.Material(new Color(.12f, .235f, .22f));
            Material sunGrass = r.Material(new Color(.30f, .39f, .265f));
            Material earth = r.Material(new Color(.40f, .355f, .255f));
            Material wornStone = r.Material(new Color(.39f, .43f, .39f));
            Surface(woodland, r, "Forest floor", new[] { new Vector2(-24,-10), new Vector2(-11,-15), new Vector2(-7,-8), new Vector2(-8,5), new Vector2(-13,16), new Vector2(-24,9) }, .009f, forestFloor);
            Surface(parent, r, "Sunlit eastern clearing", new[] { new Vector2(5,-17), new Vector2(15,-16), new Vector2(23,-8), new Vector2(21,5), new Vector2(11,8), new Vector2(4,1) }, .011f, sunGrass);
            Surface(ruins, r, "Courtyard stone foundations", new[] { new Vector2(-3,7), new Vector2(9,4), new Vector2(18,10), new Vector2(15,20), new Vector2(1,22), new Vector2(-7,16) }, .018f, wornStone);
            Vector3[] mainRoad = { new Vector3(1,0,-24), new Vector3(0,0,-16), new Vector3(0,0,-10), new Vector3(-1,0,-5), new Vector3(0,0,-1), new Vector3(2,0,4), new Vector3(0,0,11), new Vector3(2,0,19) };
            Ribbon(parent, r, "Caravan road", mainRoad, 3.5f, .022f, earth);
            Ribbon(woodland, r, "Woodland branch path", new[] { new Vector3(0,0,-9), new Vector3(-7,0,-7), new Vector3(-12,0,-2), new Vector3(-14,0,6), new Vector3(-20,0,14) }, 1.9f, .026f, earth);
            Ribbon(ruins, r, "Courtyard branch path", new[] { new Vector3(1,0,4), new Vector3(7,0,7), new Vector3(13,0,12), new Vector3(11,0,21) }, 2.2f, .029f, stone);
            Vector3[] stream = { new Vector3(-25,0,5), new Vector3(-18,0,4), new Vector3(-11,0,2), new Vector3(-5,0,.5f), new Vector3(0,0,-1), new Vector3(7,0,-2), new Vector3(14,0,-5), new Vector3(22,0,-7), new Vector3(26,0,-10) };
            WorldTraversal.SetRiver(stream, 2.4f, new Rect(-1.9f, -3.9f, 3.8f, 5.8f));
            Ribbon(lowland, r, "Pebble stream banks", stream, 3.4f, .032f, r.Material(new Color(.40f,.46f,.41f)));
            BuildWaterSurface(lowland,r,"Brook water",stream,2.4f,.038f,WaterEnvironment.Brook);
            BuildWaterBankDetail(lowland,r,stream);
            BuildCampRiverBlocks(camp,r);
            // The deck is the only ground crossing. Both banks remain reachable
            // by the shared creature route planner; leaps may clear the water.
            Material timber = r.Material(new Color(.43f,.32f,.215f),false,VisualSurface.Wood);
            for (int i = 0; i < 10; i++)
                Primitive(lowland, "Timber crossing plank", PrimitiveType.Cube, new Vector3(0,.043f,-2.8f+i*.39f), new Vector3(3.8f,.012f,.35f), timber);
            Primitive(lowland, "Bridge edge strip west", PrimitiveType.Cube, new Vector3(-1.85f,.05f,-1.04f), new Vector3(.08f,.008f,4.0f), gold);
            Primitive(lowland, "Bridge edge strip east", PrimitiveType.Cube, new Vector3(1.85f,.05f,-1.04f), new Vector3(.08f,.008f,4.0f), gold);

            BuildBridgeWaterContact(lowland,r,new Rect(-1.9f,-2.975f,3.8f,3.86f),.049f);

            Vector3[] trees = { new Vector3(-12,0,-8), new Vector3(-15,0,-4), new Vector3(-9,0,-9), new Vector3(-12,0,7), new Vector3(-15,0,9) };
            for (int i = 0; i < trees.Length; i++) Tree(woodland, r, trees[i], .9f + i % 2 * .2f, i);
            Rock(parent, r, new Vector3(8,0,-8), 1.65f, 2);
            Rock(parent, r, new Vector3(13,0,-10), 1.2f, 5);
            Rock(parent, r, new Vector3(14,0,5), 1.5f, 4);
            Primitive(ruins, "Broken courtyard barricade", PrimitiveType.Cube, new Vector3(10.5f,.75f,13), new Vector3(5,1.5f,.85f), stone,cameraOccluder:true);
            WorldTraversal.AddBox(new Vector3(10.5f,0,13), new Vector2(5,.85f));

            System.Random random = new System.Random(32019);
            for (int i = 0; i < 56; i++)
            {
                float angle = i * 2.399963f;
                float radius = 24.6f + (float)random.NextDouble() * 3.7f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                // Trees cluster in the west instead of forming a uniform ring.
                if (p.x < -7 || (p.z < -17 && i % 2 == 0)) Tree(woodland, r, p, 1.05f + (float)random.NextDouble() * .85f, i);
                else if (i % 3 == 0) Rock(parent, r, p, .8f + (float)random.NextDouble() * 1.6f, i);
            }
            for (int i = 0; i < 12; i++)
            {
                float z = 5 + i * 1.1f;
                float x = i % 2 == 0 ? -1.5f : 2.5f;
                GameObject tile = Primitive(ruins, "Broken processional paving", PrimitiveType.Cube,
                    new Vector3(x,.043f,z), new Vector3(1.1f,.025f,.73f), stone);
                tile.transform.rotation = Quaternion.Euler(0, i * 31 % 28 - 14, 0);
            }
            // Perimeter ruins complete the silhouette around the playable courtyard.
            for (int i = 0; i < 7; i++)
            {
                float x = -14 + i * 4.7f;
                float z = 24.2f + Mathf.Sin(i) * 1.4f;
                Pillar(ruins, r, new Vector3(x, 0, z), 2.5f + i % 3, false);
                if (i != 2 && i != 4)
                    Primitive(ruins, "Weathered courtyard wall", PrimitiveType.Cube, new Vector3(x+1.5f,1.1f,z+.5f), new Vector3(3.1f,2.2f,.8f), stone);
            }
            Primitive(ruins, "Buried north foundation", PrimitiveType.Cube, new Vector3(8,.033f,17), new Vector3(12,.03f,.55f), stone);
            Primitive(ruins, "Buried east foundation", PrimitiveType.Cube, new Vector3(14,.035f,12), new Vector3(.55f,.03f,9), stone);
            for (int i = 0; i < 8; i++)
                Primitive(ruins, "Courtyard mosaic", PrimitiveType.Cube, new Vector3(6+i%4*1.6f,.036f,10+i/4*1.6f), new Vector3(1.42f,.018f,1.42f), i%2==0 ? stone : edge);
            for (int i = 0; i < 65; i++)
            {
                Vector3 p = new Vector3((float)random.NextDouble() * 39 - 19.5f, .03f, (float)random.NextDouble() * 37 - 18.5f);
                if (p.magnitude > 20 || Mathf.Abs(p.x) < 3 || Mathf.Abs(p.z) < 5) continue;
                Material flowers = r.Material(i % 2 == 0 ? new Color(.65f,.58f,.8f) : new Color(.63f,.76f,.53f));
                Primitive(parent, "Wildflowers", PrimitiveType.Sphere, p + Vector3.up * .08f, new Vector3(.16f,.19f,.16f), flowers);
                Primitive(parent, "Wildflowers", PrimitiveType.Sphere, p + new Vector3(.23f,.03f,.11f), new Vector3(.12f,.14f,.12f), flowers);
            }

            Primitive(camp, "Caravan resting court", PrimitiveType.Cube, new Vector3(0,.025f,-10), new Vector3(7.4f,.035f,5.8f), stone);
            Primitive(camp, "Camp rug", PrimitiveType.Cube, new Vector3(0,.047f,-10), new Vector3(3.8f,.018f,2.7f), r.Material(new Color(.25f,.37f,.36f)));
            BuildCampfire(camp, r, new Vector3(-5.5f, 0, -23));
            Tent(camp, r, new Vector3(5.5f, 0, -23));
            // The travel rune supplies the only label at this location.
            Portal(parent, r, new Vector3(0, 0, 11), jade);
            Label(parent, "FALLEN STAR", "沉星遗迹", new Vector3(0, 4.65f, 11), .10f, new Color(.65f, 1, .87f), false);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 31, -3 - i % 4, Mathf.Sin(angle) * 31);
                Rock(parent, r, p, 2 + i % 3, i);
            }
        }

        private static void BuildCampRiverBlocks(Transform parent,WorldResources resources)
        {
            for(int i=0;i<2;i++)
            {
                Vector3 point=new Vector3(-5.5f-i*3.4f,0,-3.3f);
                Vector2 size=new Vector2(2.2f,1.8f);
                float height=i==0?.65f:.95f;
                WorldTraversal.AddPlatform(point,size,height);
                Primitive(parent,i==0?"Camp river low block":"Camp river high block",PrimitiveType.Cube,
                    point+Vector3.up*(height*.5f),new Vector3(size.x,height,size.y),
                    resources.Material(new Color(.43f,.48f,.47f),false,VisualSurface.Stone));
            }
        }

        private static void BuildCampFacilities(Transform parent, WorldResources r, int progress)
        {
            Transform facilities = Region(parent, "Camp progression facilities");
            // Stable object IDs are separate from player-facing text.
            string[] names = { "STAR CORE", "APPRENTICE", "CODEX", "CLASS TRIAL" };
            string[] labels = { "星核", "观星学徒", "装备图鉴", "职业试炼" };
            Color[] colors = { new Color(.4f,.85f,1), new Color(.8f,.6f,1), new Color(1,.77f,.4f), new Color(.4f,1,.7f) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3(i % 2 == 0 ? -4.4f : 4.4f, 0, i < 2 ? -8 : -12);
                Material stone = r.Material(new Color(.18f,.22f,.28f));
                Material glow = r.Material(colors[i] * (progress > i ? 1f : .6f), true);
                Primitive(facilities, names[i] + " plinth", PrimitiveType.Cylinder, p + Vector3.up*.35f, new Vector3(i==2?.96f:i==1?.74f:.9f,.35f,i==2?.65f:i==1?.96f:.9f), stone);
                AuthoredFixedScenery.Crest(facilities,names[i],p+Vector3.up*.76f,stone);
                Crystal(facilities, r, p + Vector3.up * (1.1f + Mathf.Min(progress,5)*.06f), .35f + Mathf.Min(progress,5)*.025f, glow);
                Label(facilities, names[i], labels[i], p + Vector3.up*2.1f, .055f, colors[i], false);
                WorldTraversal.AddJumpPlatform(p,i==2?.325f:i==1?.37f:.45f,.7f);
            }
        }

        private static void BuildDungeon(Transform parent, WorldResources r, int layout)
        {
            Material baseStone = r.Material(new Color(.095f, .11f, .17f));
            Material slab = r.Material(new Color(.21f, .23f, .31f));
            Material border = r.Material(new Color(.31f, .31f, .42f));
            Material rune = r.Material(new Color(.34f, .62f, .98f), true);
            // Unity's cylinder is two units tall: the old -.7/.7 placement
            // put both base and floor tops at y=0 and flickered as the camera moved.
            Primitive(parent, "Sanctum base", PrimitiveType.Cylinder, new Vector3(0, -.85f, 0), new Vector3(42, .7f, 42), baseStone);
            Vector2[] footprint = { new Vector2(-14,-21), new Vector2(14,-21), new Vector2(21,-13), new Vector2(21,13), new Vector2(14,22), new Vector2(-14,22), new Vector2(-21,13), new Vector2(-21,-13) };
            Surface(parent, r, "Broken cathedral footprint", footprint, 0, slab);
            Skirt(parent, r, "Cathedral foundation edges", footprint, -2.1f, baseStone);
            Transform nave = Region(parent, "Central nave and entrance causeway");
            Transform west = Region(parent, "West archive gallery");
            Transform east = Region(parent, "East crystal gallery");
            Transform altar = Region(parent, "North altar court");
            Material paleSlab = r.Material(new Color(.30f,.32f,.40f));
            Material westStone = r.Material(new Color(.20f,.19f,.29f));
            Material eastStone = r.Material(new Color(.17f,.255f,.30f));
            Primitive(nave, "Processional nave", PrimitiveType.Cube, new Vector3(0,.012f,-2), new Vector3(10.8f,.02f,33), paleSlab);
            Primitive(west, "Archive corridor floor", PrimitiveType.Cube, new Vector3(-12,.014f,0), new Vector3(10.5f,.022f,24), westStone);
            Primitive(east, "Crystal corridor floor", PrimitiveType.Cube, new Vector3(12,.016f,0), new Vector3(10.5f,.024f,24), eastStone);
            Primitive(nave, "Crossing transept", PrimitiveType.Cube, new Vector3(0,.033f,1), new Vector3(34,.018f,5.8f), border);
            // Floor slabs, flush stairs and colored inlays define navigable rooms
            // without pretending the planar controller can climb raised geometry.
            for (int z = -7; z <= 5; z++)
                for (int x = -1; x <= 1; x++)
                {
                    if (z >= -1 && z <= 1) continue;
                    Primitive(nave, "Processional paving joint", PrimitiveType.Cube, new Vector3(x*3.2f,.03f,z*2.25f), new Vector3(3.04f,.012f,2.08f), (x+z)%2 == 0 ? slab : paleSlab);
                }
            for (int i = 0; i < 7; i++)
            {
                float z = -9+i*3;
                Primitive(west, "Archive floor ribs", PrimitiveType.Cube, new Vector3(-12,.042f,z), new Vector3(8.7f,.012f,.1f), border);
                Primitive(east, "Crystal channel inset", PrimitiveType.Cube, new Vector3(12,.043f,z), new Vector3(8.7f,.014f,.065f), rune);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Transform gallery = side < 0 ? west : east;
                Primitive(gallery, "Nave edge inlay", PrimitiveType.Cube, new Vector3(side*5.3f,.05f,-2), new Vector3(.075f,.014f,29), rune);
                Pillar(gallery, r, new Vector3(side*7,0,-4), 2.5f, true);
                Pillar(gallery, r, new Vector3(side*7,0,6), 2.9f, true);
                Vector3 barricade = layout % 2 == 0 ? new Vector3(side*10.5f,0,-7.5f) : new Vector3(side*5f,0,-1.5f);
                Vector2 barrierSize = layout % 2 == 0 ? new Vector2(4.2f,.9f) : new Vector2(.9f,5.5f);
                Primitive(gallery, "Collapsed gallery partition", PrimitiveType.Cube, barricade + Vector3.up*.65f, new Vector3(barrierSize.x,1.3f,barrierSize.y), border,cameraOccluder:true);
                WorldTraversal.AddBox(barricade, barrierSize);
                for (int i = 0; i < 5; i++)
                {
                    Vector3 p = new Vector3(side*20.3f,0,-12+i*6);
                    float height = i == 2 ? 2.1f : 4.4f+i%2*.7f;
                    Pillar(gallery,r,p,height,true);
                    if (i != 2) Crystal(gallery,r,p+Vector3.up*(height+.75f),.58f,rune);
                    else
                    {
                        GameObject fallen = Primitive(gallery,"Fallen outer column",PrimitiveType.Cylinder,p+new Vector3(side*1.4f,.7f,1),new Vector3(.9f,1.8f,.9f),border,cameraOccluder:true);
                        fallen.transform.rotation=Quaternion.Euler(78,side*20,0);
                    }
                }
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = new Vector3(side*23,1.3f,-9+i*6);
                    Primitive(gallery,"Gallery exterior wall",PrimitiveType.Cube,p,new Vector3(.8f,2.6f+(i%2),5.1f),baseStone,cameraOccluder:true);
                    if (side < 0)
                        Primitive(gallery,"Sealed archive shelf",PrimitiveType.Cube,p+new Vector3(-1,1.2f,0),new Vector3(.8f,4.1f,3.4f),westStone,cameraOccluder:true);
                }
            }
            for (int step = 0; step < 3; step++)
                Primitive(altar,"Flush altar approach",PrimitiveType.Cube,new Vector3(0,.025f+step*.006f,7.5f+step*1.4f),new Vector3(12-step*1.4f,.016f,1.32f),paleSlab);
            Primitive(altar,"Altar mosaic court",PrimitiveType.Cube,new Vector3(0,.041f,13),new Vector3(9,.02f,6),border);
            GameObject diamond=Primitive(altar,"Fallen star floor emblem",PrimitiveType.Cube,new Vector3(0,.053f,12),new Vector3(2.8f,.01f,2.8f),rune);
            diamond.transform.rotation=Quaternion.Euler(0,45,0);
            // The high shrine and broken gateway frame the playable rooms from
            // outside the movement boundary, leaving portal/spawn routes clear.
            Primitive(altar,"Shrine lower step",PrimitiveType.Cube,new Vector3(0,.4f,22),new Vector3(12,.8f,4.5f),baseStone,cameraOccluder:true);
            Primitive(altar,"Shrine upper step",PrimitiveType.Cube,new Vector3(0,1.05f,23),new Vector3(8,.5f,3),border,cameraOccluder:true);
            Crystal(altar,r,new Vector3(0,3.3f,23),2.3f,rune);
            Pillar(altar,r,new Vector3(-7,0,23),6,true);
            Pillar(altar,r,new Vector3(7,0,23),4.5f,true);
            for(int side=-1;side<=1;side+=2)
            {
                Pillar(nave,r,new Vector3(side*9,0,-20),5.5f,true);
                Primitive(nave,"Broken entrance lintel",PrimitiveType.Cube,new Vector3(side*6.5f,5.5f,-20),new Vector3(5.5f,.7f,1.4f),border,cameraOccluder:true);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 5.8f;
                Primitive(parent, "Lost wall", PrimitiveType.Cube, new Vector3(x, 2.8f, 26), new Vector3(5.5f, 5.6f + i % 2, .7f), baseStone,cameraOccluder:true);
            }
            Label(parent, "THE FALLEN SANCTUM", "沉星遗迹", new Vector3(0,.075f,-7), .12f, new Color(.46f,.55f,.72f), true);
            Portal(parent, r, new Vector3(0, 0, -16), r.Material(new Color(.58f,.38f,.94f), true));
            PointLight(parent, new Vector3(-9,4,1), new Color(.18f,.48f,1), 2, 17);
            PointLight(parent, new Vector3(9,4,7), new Color(.7f,.25f,1), 1.8f, 16);
        }

        private static Transform Region(Transform parent, string name)
        {
            GameObject region = new GameObject(name);
            region.transform.SetParent(parent, false);
            return region.transform;
        }

        private static GameObject Surface(Transform parent, WorldResources r, string name, Vector2[] outline, float height, Material material)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector2 center = Vector2.zero;
            float area = 0;
            for (int i = 0; i < outline.Length; i++)
            {
                center += outline[i];
                Vector2 next = outline[(i + 1) % outline.Length];
                area += outline[i].x * next.y - next.x * outline[i].y;
            }
            center /= outline.Length;
            vertices.Add(new Vector3(center.x, height, center.y));
            foreach (Vector2 point in outline) vertices.Add(new Vector3(point.x, height, point.y));
            for (int i = 0; i < outline.Length; i++)
            {
                triangles.Add(0);
                triangles.Add(area >= 0 ? (i + 1) % outline.Length + 1 : i + 1);
                triangles.Add(area >= 0 ? i + 1 : (i + 1) % outline.Length + 1);
            }
            return Geometry(parent, r, name, vertices, triangles, material);
        }

        private static void Skirt(Transform parent, WorldResources r, string name, Vector2[] outline, float bottom, Material material)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                int start = vertices.Count;
                vertices.Add(new Vector3(a.x, 0, a.y));
                vertices.Add(new Vector3(b.x, 0, b.y));
                vertices.Add(new Vector3(a.x * .95f, bottom, a.y * .95f));
                vertices.Add(new Vector3(b.x * .95f, bottom, b.y * .95f));
                triangles.Add(start); triangles.Add(start+1); triangles.Add(start+2);
                triangles.Add(start+1); triangles.Add(start+3); triangles.Add(start+2);
            }
            Geometry(parent, r, name, vertices, triangles, material);
        }

        private static void Ribbon(Transform parent, WorldResources r, string name, Vector3[] path, float width, float height, Material material)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 normal;float miter;RibbonSection(path,i,out normal,out miter);
                float halfWidth = width * .5f * miter;
                Vector3 point = path[i]; point.y = height;
                vertices.Add(point + normal * halfWidth);
                vertices.Add(point - normal * halfWidth);
                if (i == path.Length-1) continue;
                int start = i * 2;
                triangles.Add(start); triangles.Add(start+2); triangles.Add(start+1);
                triangles.Add(start+1); triangles.Add(start+2); triangles.Add(start+3);
            }
            Geometry(parent, r, name, vertices, triangles, material);
        }

        private static GameObject Geometry(Transform parent, WorldResources r, string name, List<Vector3> vertices, List<int> triangles, Material material)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            r.Own(mesh);
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void Tree(Transform parent, WorldResources r, Vector3 p, float size, int seed)
        {
            if (p.sqrMagnitude < 22f*22f) WorldTraversal.AddCircle(p, .22f);
            BuildBranchTree(parent, r, p, size, seed);
        }

        private static void Rock(Transform parent, WorldResources r, Vector3 p, float scale, int seed)
        {
            if (p.y >= -.1f && p.sqrMagnitude < 22f*22f) WorldTraversal.AddCircle(p, scale * .82f);
            GameObject rock = Primitive(parent, "Weathered rock", PrimitiveType.Cube, p + Vector3.up * scale * .35f,
                new Vector3(scale * 1.3f, scale, scale * .9f), r.Material(seed % 2 == 0 ? new Color(.28f,.34f,.37f) : new Color(.32f,.4f,.39f)));
            if(scale>=1)CameraOcclusionSurface.Mark(rock);
            rock.GetComponent<MeshFilter>().sharedMesh = ProceduralVisuals.WeatheredRock;
            rock.transform.rotation = Quaternion.Euler(seed % 27, seed * 67 % 360, seed % 18);
        }

        private static void Pillar(Transform parent, WorldResources r, Vector3 p, float height, bool dungeon)
        {
            if (p.sqrMagnitude < (dungeon ? 18f*18f : 22f*22f)) WorldTraversal.AddBox(p, new Vector2(1.4f,1.4f));
            Material stone = r.Material(dungeon ? new Color(.24f,.25f,.35f) : new Color(.44f,.48f,.43f));
            Primitive(parent, "Column base", PrimitiveType.Cube, p + Vector3.up * .25f, new Vector3(1.4f,.5f,1.4f), stone);
            Primitive(parent, "Column", PrimitiveType.Cylinder, p + Vector3.up * height * .5f, new Vector3(.85f,height*.5f,.85f), stone,cameraOccluder:true);
            Primitive(parent, "Capital", PrimitiveType.Cube, p + Vector3.up * height, new Vector3(1.3f,.32f,1.3f), stone,cameraOccluder:true);
            Primitive(parent, "Capital bevel collar", PrimitiveType.Cylinder, p + Vector3.up * (height-.23f), new Vector3(1.02f,.08f,1.02f), stone);
            Material band = r.Material(dungeon ? new Color(.31f,.57f,.8f) : new Color(.67f,.59f,.39f), dungeon);
            Primitive(parent, "Column band", PrimitiveType.Cylinder, p + Vector3.up * (height-.5f), new Vector3(.94f,.1f,.94f), band);
        }

        private static void Portal(Transform parent, WorldResources r, Vector3 p, Material glow)
        {
            // All travel gates share the minimap's jade interaction colour;
            // town-specific amber/blue accents remain on architecture and props.
            glow = r.Material(new Color(.32f, .91f, .77f), true);
            Material stone = r.Material(new Color(.27f,.34f,.38f));
            Primitive(parent, "Gate plinth", PrimitiveType.Cylinder, p + Vector3.up * .022f, new Vector3(5,.022f,4), stone);
            Ring(parent, r, "Gate frame", p + Vector3.up * 2.1f, 1.85f, .2f, stone, true);
            GameObject inner = Ring(parent, r, "Gate light", p + new Vector3(0,2.1f,-.05f), 1.63f, .055f, glow, true);
            inner.AddComponent<WorldMotion>().spin = new Vector3(0,0,16);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI / 2;
                Vector3 point = p + new Vector3(Mathf.Cos(angle)*1.85f, 2.1f+Mathf.Sin(angle)*1.85f, -.09f);
                Crystal(parent,r,point,.23f,glow);
            }
            // Keep the arch opening clear; four large compass accents identify travel.
            PointLight(parent, p + new Vector3(0,2,0), glow.color, 1.6f, EnvironmentLightProfile.PortalRange);
            BuildPortalFocus(parent,r,p);
            for (int i=0;i<4;i++)
            {
                float angle=i*Mathf.PI*.5f;
                GameObject mote=Primitive(parent,"Starlight",PrimitiveType.Sphere,p+new Vector3(Mathf.Cos(angle)*1.8f,2.1f+Mathf.Sin(angle)*1.8f,-.15f),Vector3.one*.055f,glow);
                WorldMotion motion=mote.AddComponent<WorldMotion>(); motion.bob=.18f; motion.speed=1.5f+i*.1f;
            }
        }

        private static void Crystal(Transform parent, WorldResources r, Vector3 p, float size, Material material)
        {
            GameObject crystal = new GameObject("Aether crystal"); crystal.transform.SetParent(parent); crystal.transform.position=p;
            Cone(crystal.transform,r,"Crystal upper",Vector3.zero,.42f*size,1.1f*size,material,5,true);
            GameObject bottom=Cone(crystal.transform,r,"Crystal lower",Vector3.zero,.42f*size,.7f*size,material,5,true);
            bottom.transform.localRotation=Quaternion.Euler(180,0,0);
            WorldMotion motion=crystal.AddComponent<WorldMotion>(); motion.bob=.12f; motion.spin=new Vector3(0,30,0);
        }

        private static void BuildCampfire(Transform parent, WorldResources r, Vector3 p)
        {
            bool authored=BlenderSceneryArt.CreatePilotProp("StarEmberCampfire",parent,p)!=null;
            Material wood=r.Material(new Color(.3f,.22f,.17f));
            if(!authored)for(int i=0;i<3;i++) { GameObject log=Primitive(parent,"Firewood",PrimitiveType.Cylinder,p+new Vector3(0,.17f,0),new Vector3(.23f,.8f,.23f),wood); log.transform.rotation=Quaternion.Euler(90,i*60,0); }
            if(!authored)
            {
            GameObject flame = Primitive(parent,"Amber flame",PrimitiveType.Sphere,p+Vector3.up*.67f,
                new Vector3(.64f,1.05f,.6f),r.Material(new Color(1,.34f,.075f),true));
            flame.AddComponent<WorldMotion>().flame = true;
            }
            GameObject core = Primitive(parent,"Golden flame",PrimitiveType.Sphere,p+new Vector3(0,.48f,.12f),
                authored?new Vector3(.18f,.30f,.17f):new Vector3(.36f,.64f,.34f),r.Material(new Color(1,.76f,.22f),true));
            core.AddComponent<WorldMotion>().flame = true;
            PointLight(parent,p+Vector3.up*1.3f,new Color(1,.52f,.19f),2,8);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;Vector3 stone=p+new Vector3(Mathf.Cos(a)*.7f,0,Mathf.Sin(a)*.7f);
                if(!authored)Rock(parent,r,stone,.28f,i);
                else if(stone.y>=-.1f&&stone.sqrMagnitude<22f*22f)WorldTraversal.AddCircle(stone,.28f*.82f);
            }
        }

        private static void Tent(Transform parent, WorldResources r, Vector3 p)
        {
            Material cloth=r.Material(new Color(.29f,.47f,.48f),false,VisualSurface.Cloth);
            if(BlenderSceneryArt.CreatePilotProp("WayfarerTent",parent,p)==null)
            for(int i=0;i<2;i++) { GameObject slope=Primitive(parent,"Camp tent",PrimitiveType.Cube,p+new Vector3(i==0?-.62f:.62f,1,0),new Vector3(.08f,2.5f,2.5f),cloth); slope.transform.rotation=Quaternion.Euler(0,0,i==0?-30:30); }
            Vector3 chest = p + new Vector3(2,.45f,0);
            Material wood = r.Material(new Color(.31f,.19f,.115f),false,VisualSurface.Wood);
            Material bronze = r.Material(new Color(.64f,.44f,.21f),false,VisualSurface.Metal);
            Primitive(parent,"Provision coffer base",PrimitiveType.Cube,chest,new Vector3(.95f,.67f,.75f),wood);
            Primitive(parent,"Provision coffer domed lid",PrimitiveType.Capsule,chest+Vector3.up*.35f,new Vector3(.74f,.46f,.6f),wood).transform.localRotation=Quaternion.Euler(0,0,90);
            for(int side=-1;side<=1;side+=2)
                Primitive(parent,"Coffer bronze band",PrimitiveType.Cube,chest+new Vector3(side*.31f,.08f,.389f),new Vector3(.085f,.76f,.045f),bronze);
            Primitive(parent,"Coffer clasp",PrimitiveType.Cube,chest+new Vector3(0,.12f,.41f),new Vector3(.18f,.24f,.06f),bronze);
        }

        private static GameObject Primitive(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material material,bool cameraOccluder=false)
        {
            GameObject go=ProceduralVisuals.Create(name,type,material);
            AuthoredFixedScenery.Apply(go,name,type);
            go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localScale=scale;
            if(cameraOccluder)CameraOcclusionSurface.Mark(go);
            return go;
        }

        private static GameObject Cone(Transform parent,WorldResources resources,string name,Vector3 p,float radius,float height,Material material,int sides,bool local=false)
        {
            List<Vector3> vertices=new List<Vector3>(); List<int> triangles=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                int start=vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); vertices.Add(Vector3.up*height); vertices.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));
                triangles.Add(start); triangles.Add(start+1); triangles.Add(start+2);
                start=vertices.Count; vertices.Add(Vector3.zero); vertices.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); vertices.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
            }
            Mesh mesh=new Mesh(); mesh.name=name; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); resources.Own(mesh);
            GameObject go=new GameObject(name); go.transform.SetParent(parent); if(local)go.transform.localPosition=p;else go.transform.position=p;
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=material; return go;
        }

        private static GameObject Ring(Transform parent,WorldResources r,string name,Vector3 center,float radius,float thickness,Material material,bool vertical)
        {
            GameObject go=new GameObject(name); go.transform.SetParent(parent); go.transform.position=center;
            if(name=="Gate frame"&&AuthoredFixedScenery.Frame(go,radius,material))return go;
            LineRenderer line=go.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.loop=true; line.positionCount=72; line.widthMultiplier=thickness; line.sharedMaterial=material;
            line.numCornerVertices=2; line.numCapVertices=2; line.generateLightingData=true;
            for(int i=0;i<72;i++) { float a=i*Mathf.PI*2/72; line.SetPosition(i,vertical?new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0):new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); }
            return go;
        }

        private static void PointLight(Transform parent,Vector3 p,Color color,float intensity,float range)
        { GameObject go=new GameObject("Magic light"); go.transform.SetParent(parent); go.transform.position=p; Light light=go.AddComponent<Light>(); light.type=LightType.Point; light.color=color; light.intensity=intensity; light.range=range; light.shadows=LightShadows.None; light.renderMode=LightRenderMode.ForceVertex; }

        private static void Label(Transform parent,string objectName,string value,Vector3 p,float size,Color color,bool floor)
        {
            GameObject go=new GameObject(objectName); go.transform.SetParent(parent); go.transform.position=p;
            // Ship the exact Chinese glyphs rather than relying on a device's OS fonts.
            // TextMesh needs both the font and its atlas material to render correctly.
            Font font=GameFont.WorldLabels;
            if(font==null) throw new System.InvalidOperationException("Bundled world-label font is missing.");
            TextMesh text=go.AddComponent<TextMesh>(); text.font=font; text.fontSize=64; text.characterSize=size;
            text.anchor=TextAnchor.MiddleCenter; text.alignment=TextAlignment.Center; text.color=color;
            font.RequestCharactersInTexture(value,text.fontSize,FontStyle.Normal);
            go.GetComponent<MeshRenderer>().sharedMaterial=font.material;
            text.text=value;
            go.transform.rotation=Quaternion.Euler(floor?90:18,0,0);
            if(!floor)go.AddComponent<WorldLabelPresentation>().Initialize(text);
        }

        public static GameObject MakeLootBeacon(Vector3 position,Color color)
        {
            GameObject root=new GameObject("Loot acquired"); root.transform.position=position;
            WorldResources r=root.AddComponent<WorldResources>(); Material glow=r.Material(color,true);
            Primitive(root.transform,"Loot beam",PrimitiveType.Cylinder,new Vector3(0,1.3f,0),new Vector3(.035f,1.3f,.035f),glow);
            Crystal(root.transform,r,position+Vector3.up*.6f,.35f,glow);
            Ring(root.transform,r,"Loot ring",position+Vector3.up*.06f,.65f,.055f,glow,false);
            return root;
        }
    }

    public sealed class WorldResources : MonoBehaviour
    {
        private readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        public Material Material(Color color,bool emissive=false) { return Material(color,emissive,VisualSurface.Stone); }
        internal Material Material(Color color,bool emissive,VisualSurface surface)
        {
            string key=ColorUtility.ToHtmlStringRGBA(color)+(emissive?"E":"S")+(int)surface;
            Material material;
            if(materials.TryGetValue(key,out material))return material;
            Shader shader=Shader.Find("Standard");
            if(shader==null) shader=Shader.Find("Sprites/Default");
            material=new Material(shader); material.color=color;
            ProceduralVisuals.ApplySurface(material,emissive?VisualSurface.Crystal:surface);
            if(emissive && material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",color*.8f);
            materials.Add(key,material); return material;
        }
        public void Own(Mesh mesh) { meshes.Add(mesh); }
        private void OnDestroy() { foreach(Material material in materials.Values)Destroy(material);foreach(Mesh mesh in meshes)Destroy(mesh); }
    }

    public sealed class WorldMotion : MonoBehaviour
    {
        public Vector3 spin;
        public float bob;
        public float speed=1;
        public bool flame;
        private Vector3 origin, baseScale;
        private void Start() { origin=transform.localPosition; baseScale=transform.localScale; }
        private void Update()
        {
            if(Time.deltaTime<=0)return;
            transform.Rotate(spin*Time.deltaTime,Space.Self);
            if(bob>0)transform.localPosition=origin+Vector3.up*Mathf.Sin(Time.time*speed+origin.x)*bob;
            if(flame)
            {
                float flicker=Mathf.Sin(Time.time*8f+origin.y*13f)*.055f+Mathf.Sin(Time.time*13f)*.025f;
                transform.localScale=Vector3.Scale(baseScale,new Vector3(1-flicker,1+flicker,1-flicker));
                transform.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*3f)*5f,0,Mathf.Sin(Time.time*4f)*6f);
            }
        }
    }
}
