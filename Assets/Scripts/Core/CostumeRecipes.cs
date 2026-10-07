using System;
using System.Collections.Generic;
namespace Emberfall
{
    public enum WingSilhouette { Feather, Crystal, Mechanical }
    public sealed class CostumeMeshRecipe
    {
        public readonly float[] Positions;public readonly int[] Triangles;
        public CostumeMeshRecipe(float[] positions,int[] triangles){Positions=positions;Triangles=triangles;}
    }
    public static class CostumeRecipes
    {
        public const int MaximumWingParts=40;
        public static WingSilhouette WingStyle(Rarity rarity)
        {return rarity==Rarity.Legendary?WingSilhouette.Mechanical:rarity==Rarity.Epic?WingSilhouette.Crystal:WingSilhouette.Feather;}
        public static float ChestWidth(HeroClass hero,int tier)
        {tier=Math.Max(1,Math.Min(4,tier));return hero==HeroClass.Vanguard?.68f+tier*.065f:hero==HeroClass.Arcanist?.40f+tier*.015f:hero==HeroClass.Ranger?.49f+tier*.015f:.57f+tier*.02f;}
        public static CostumeMeshRecipe Feather()
        {
            var p=new List<float>();var t=new List<int>();const int slices=8;
            for(int i=0;i<=slices;i++)
            {
                float y=i/(float)slices,w=.014f+(float)Math.Sin(y*Math.PI)*.17f,z=(float)Math.Sin(y*Math.PI)*.09f;
                foreach(var v in new[]{new[]{-w,y,z},new[]{0f,y,z+.035f},new[]{w*.72f,y,z},new[]{0f,y,z-.025f}})p.AddRange(v);
            }
            for(int i=0;i<slices;i++)for(int side=0;side<4;side++)
            {int a=i*4+side,b=i*4+(side+1)%4,c=b+4,d=a+4;t.AddRange(new[]{a,b,c,a,c,d});}
            t.AddRange(new[]{0,2,1,0,3,2,slices*4,slices*4+1,slices*4+2,slices*4,slices*4+2,slices*4+3});
            return new CostumeMeshRecipe(p.ToArray(),t.ToArray());
        }
        public static CostumeMeshRecipe Crystal()
        {
            var p=new List<float>{0,0,0,0,1.1f,0};var t=new List<int>();
            for(int i=0;i<6;i++){float a=i*(float)Math.PI/3;p.AddRange(new[]{(float)Math.Cos(a)*.16f,.3f,(float)Math.Sin(a)*.13f});}
            for(int i=0;i<6;i++){int a=2+i,b=2+(i+1)%6;t.AddRange(new[]{0,a,b,1,b,a});}
            return new CostumeMeshRecipe(p.ToArray(),t.ToArray());
        }
        public static CostumeMeshRecipe Ring()
        {
            const int around=48,tube=6;var p=new List<float>();var t=new List<int>();
            for(int i=0;i<around;i++)for(int j=0;j<tube;j++)
            {double a=i*Math.PI*2/around,b=j*Math.PI*2/tube;float r=.8f+(float)Math.Cos(b)*.055f;p.AddRange(new[]{(float)Math.Cos(a)*r,(float)Math.Sin(a)*r,(float)Math.Sin(b)*.055f});}
            for(int i=0;i<around;i++)for(int j=0;j<tube;j++)
            {int a=i*tube+j,b=((i+1)%around)*tube+j,c=((i+1)%around)*tube+(j+1)%tube,d=i*tube+(j+1)%tube;t.AddRange(new[]{a,b,c,a,c,d});}
            return new CostumeMeshRecipe(p.ToArray(),t.ToArray());
        }
    }
}
