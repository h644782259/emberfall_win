using System;
using System.Collections.Generic;
using Emberfall;

public static class FilledVfxRecipeTests
{
    private static int checks;
    private static void Check(bool condition,string why){checks++;if(!condition)throw new Exception(why);}
    private static void Inspect(FilledMeshRecipe mesh,bool closed)
    {
        int vertices=mesh.Positions.Length/3;Check(vertices>0&&vertices<400,"bounded vertex count");
        Check(mesh.Uv.Length==vertices*2&&mesh.Triangles.Length%3==0,"valid vertex/uv/index buffers");
        foreach(float p in mesh.Positions)Check(!float.IsNaN(p)&&!float.IsInfinity(p)&&Math.Abs(p)<2,"finite bounded geometry");
        foreach(float uv in mesh.Uv)Check(uv>=0&&uv<=1,"normalized UVs");
        var edges=new Dictionary<string,int>();double volume=0;
        for(int n=0;n<mesh.Triangles.Length;n+=3)
        {
            int a=mesh.Triangles[n],b=mesh.Triangles[n+1],c=mesh.Triangles[n+2];
            Check(a>=0&&a<vertices&&b>=0&&b<vertices&&c>=0&&c<vertices,"triangle indices in range");
            double ax=mesh.Positions[a*3],ay=mesh.Positions[a*3+1],az=mesh.Positions[a*3+2];
            double bx=mesh.Positions[b*3],by=mesh.Positions[b*3+1],bz=mesh.Positions[b*3+2];
            double cx=mesh.Positions[c*3],cy=mesh.Positions[c*3+1],cz=mesh.Positions[c*3+2];
            double nx=(by-ay)*(cz-az)-(bz-az)*(cy-ay),ny=(bz-az)*(cx-ax)-(bx-ax)*(cz-az),nz=(bx-ax)*(cy-ay)-(by-ay)*(cx-ax);
            Check(nx*nx+ny*ny+nz*nz>1e-15,"no degenerate faces");
            volume+=(ax*(by*cz-bz*cy)+ay*(bz*cx-bx*cz)+az*(bx*cy-by*cx))/6;
            foreach(var pair in new[]{new[]{a,b},new[]{b,c},new[]{c,a}})
            {string key=Math.Min(pair[0],pair[1])+":"+Math.Max(pair[0],pair[1]);if(!edges.ContainsKey(key))edges[key]=0;edges[key]++;}
        }
        Check(volume>0.001,"outward-oriented filled volume rather than flat lines");
        if(closed)foreach(int count in edges.Values)Check(count==2,"watertight indexed volume");
    }
    public static string Run()
    {
        checks=0;
        Inspect(FilledVfxRecipes.EnergyCore(),true);
        foreach(int detail in new[]{-1,12,36,64,1000})Inspect(FilledVfxRecipes.Crescent(detail),true);
        Inspect(FilledVfxRecipes.Sword(),true);Inspect(FilledVfxRecipes.Lightning(),true);Inspect(FilledVfxRecipes.Arcane(),true);Inspect(FilledVfxRecipes.Rupture(),true);Inspect(FilledVfxRecipes.ArcaneShard(),true);
        Inspect(FilledVfxRecipes.Crystal(),false); // Hard normals intentionally duplicate each face's corners.
        Inspect(FilledVfxRecipes.Flame(),true);Inspect(FilledVfxRecipes.Flame(-4,999),true);
        foreach(FilledVfxKind kind in Enum.GetValues(typeof(FilledVfxKind)))
        foreach(float duration in new[]{.12f,.34f,1.05f,10f})
        {
            float previous=-1;
            for(int i=0;i<=100;i++)
            {
                var frame=FilledVfxRecipes.Sample(kind,duration*i/100,duration);
                Check(frame.Progress>=previous&&frame.Progress<=1,"monotonic bounded lifetime");previous=frame.Progress;
                Check(frame.Opacity>=0&&frame.Opacity<=1&&frame.Expansion>=0&&frame.Expansion<=1.15f,"finite bounded envelope");
            }
            Check(FilledVfxRecipes.Sample(kind,duration,duration).Opacity==0,"expires without a permanent field");
        }
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1})
        {Check(FilledVfxRecipes.Sample(FilledVfxKind.Fire,invalid,1).Opacity==0,"invalid age rejected");Check(FilledVfxRecipes.Sample(FilledVfxKind.Fire,1,invalid).Opacity==0,"invalid duration rejected");}
        Check(FilledVfxRecipes.MobileEffects<FilledVfxRecipes.DesktopEffects&&FilledVfxRecipes.ReducedParts<FilledVfxRecipes.MaximumParts,"mobile and reduced-effect budgets lower");
        return "Filled VFX recipe checks: "+checks;
    }
}
