using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        // The camp keeps its familiar services; new terrain grows around them.
        static void BuildOuterWilderness(Transform parent,WorldResources r)
        {
            var forest=Region(parent,"Outer ridge forest and northern exploration loop");
            Material earth=r.Material(new Color(.37f,.32f,.23f));NaturalWorldMaterial.Trail(earth);
            Ribbon(forest,r,"Northern ridge footpath",new[]{new Vector3(-20,0,14),new Vector3(-20,0,22),new Vector3(-9,0,28),new Vector3(5,0,29),new Vector3(20,0,22),new Vector3(26,0,12),new Vector3(21,0,3)},2.1f,.035f,earth);
            Ribbon(forest,r,"Western hillside approach",new[]{new Vector3(-12,0,-9),new Vector3(-22,0,-15),new Vector3(-29,0,-10),new Vector3(-28,0,0)},1.8f,.04f,earth);
            Ribbon(forest,r,"Eastern meadow approach",new[]{new Vector3(9,0,-15),new Vector3(20,0,-20),new Vector3(28,0,-16),new Vector3(28,0,-6)},2,.045f,earth);
            for(int i=0;i<34;i++)
            {
                float angle=i*2.399963f;float radius=26+i%4*1.6f;
                Vector3 p=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                if(p.z<-8&&Mathf.Abs(p.x)<12||!WorldTraversal.IsWalkable(p,.8f))continue;
                Tree(forest,r,p,.85f+(i%5)*.13f,731+i);
                if(i%3==0){Vector3 rock=p+new Vector3(1.6f,0,.7f);if(WorldTraversal.IsWalkable(rock,1))Rock(forest,r,rock,.6f+i%4*.12f,i+92);}
            }
            var ruins=Region(parent,"Northern weathered overlook");
            for(int i=0;i<6;i++)
            {
                Vector3 p=new Vector3(-6+i*2.1f,0,29);
                Pillar(ruins,r,p,1.2f+i%3*.65f,false);
            }
        }
        static void BuildDungeonNaturalScenery(Transform parent,WorldResources r,int layout)
        {
            var rubble=new DetailBatch();var moss=new DetailBatch();var roots=new DetailBatch();
            var random=new System.Random(98711+layout*53);float extent=WorldTraversal.ArenaRadius-1;
            bool forest=layout>=100&&ChapterRoomGeometry.FromLayout(layout,0).Node==ChapterNode.ForestCourt;
            // Decorative perimeter stays clear of the authored entrance and crossing lanes.
            if(layout>=100)
            {
                bool mine=ChapterRoomGeometry.FromLayout(layout,0).Node==ChapterNode.Redrock;
                for(int i=0;i<24;i++)
                {
                    float a=i*Mathf.PI*2/24;Vector3 p=new Vector3(Mathf.Sin(a)*17.4f,0,Mathf.Cos(a)*17.4f);
                    if(Mathf.Abs(p.x)<3.5f)continue;
                    if(forest)BuildBranchTree(parent,r,p,.85f+i%4*.16f,1213+i);
                    else if(mine)
                    {
                        var rock=Primitive(parent,"Mine exposed rock strata",PrimitiveType.Sphere,p+Vector3.up*.55f,new Vector3(2.4f,1.2f+i%3*.4f,1.6f),r.Material(new Color(.35f+i%3*.03f,.25f,.19f)));
                        rock.GetComponent<MeshFilter>().sharedMesh=ProceduralVisuals.WeatheredRock;
                        rock.transform.Rotate(i%17,i*47,13);
                        if(i%3==0){Primitive(parent,"Old mine perimeter support",PrimitiveType.Cube,p+Vector3.up*1.6f,new Vector3(.24f,3.2f,.27f),r.Material(new Color(.28f,.20f,.14f),false,VisualSurface.Wood));}
                    }
                }
            }
            for(int i=0;i<260;i++)
            {
                Vector3 p=new Vector3(Next(random,-extent,extent),.065f,Next(random,-extent,extent));
                if(!WorldTraversal.IsWalkable(p,.35f)||Mathf.Abs(p.x)<3||Mathf.Abs(p.z)<3.4f)continue;
                float size=Next(random,.12f,.42f);
                rubble.Pebble(p,new Vector3(size,size*.46f,size*.8f),Next(random,0,6.28f));
                if(i%3==0)
                {
                    for(int leaf=0;leaf<7;leaf++)moss.Leaf(p+new Vector3(Next(random,-.25f,.25f),.015f,Next(random,-.25f,.25f)),Next(random,0,6.28f),Next(random,.13f,.28f),new Color(.75f,.9f,.72f,0));
                }
                if(i%13==0)
                {
                    Vector3 to=p+new Vector3(Next(random,.3f,.8f),.1f,Next(random,-.8f,.8f));
                    roots.Branch(p,to,.045f,.008f);
                    roots.Branch(Vector3.Lerp(p,to,.45f),to+new Vector3(.1f,.06f,.35f),.022f,.004f);
                }
            }
            rubble.Build(parent,r,"Merged cathedral fractured rubble",r.Material(new Color(.44f,.43f,.41f),false,VisualSurface.Stone),false);
            Material leaves=r.Material(forest?new Color(.25f,.41f,.26f):new Color(.25f,.34f,.29f),false,VisualSurface.Foliage);
            if(leaves.HasProperty("_VertexTint")){leaves.SetFloat("_VertexTint",1);leaves.SetFloat("_Cull",0);}
            moss.Build(parent,r,"Merged creeping ruin moss",leaves,false);
            roots.Build(parent,r,"Merged weathered timber splinters",r.Material(new Color(.30f,.25f,.20f),false,VisualSurface.Wood),false);
            // Far silhouettes give the side galleries a roofline and depth while
            // remaining outside navigation and the central combat sightline.
            if(layout<2)for(int side=-1;side<=1;side+=2)for(int z=-14;z<=14;z+=14)
            {
                Vector3 p=new Vector3(side*29,0,z);
                Pillar(parent,r,p,3.8f+(z==0?1:0),true);
                Primitive(parent,"Broken gallery arch voussoir",PrimitiveType.Cube,p+new Vector3(-side*1.4f,4.2f,.4f),new Vector3(2.7f,.5f,1.2f),r.Material(new Color(.40f,.39f,.43f)),cameraOccluder:true).transform.Rotate(0,0,side*12);
            }
        }
    }
}
