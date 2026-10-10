"""Compare production bounded strokes against full-canvas strokes, pixel for pixel."""
from pathlib import Path
import os,subprocess,tempfile,sys
root=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
source=(root/'Assets/Scripts/UI/UIIconAtlas.cs').read_text()
icon=member(source,'private sealed class Icon').replace('private sealed class Icon','public sealed class Icon')
icon=icon.replace(member(icon,'public Texture2D Finish('),'public Color[] Pixels=>pixels;')
# Restore the full-raster loop while retaining identical coverage and color equations.
old=icon
for method in ['public void Line(', 'public void Disc(']:
 part=member(old,method);begin=part.index('                float pad=');end=part.index('for (int y = y0;',begin)
 part=part[:begin]+part[end:];part=part.replace('for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)','for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)')
 old=old.replace(member(old,method),part)
part=member(old,'public void Polygon(');begin=part.index('                if(points.Length<3)');end=part.index('for (int y = y0;',begin)
part=part[:begin]+part[end:];part=part.replace('for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)','for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)')
old=old.replace(member(old,'public void Polygon('),part)
stubs=member((root/'Tests/SkillIconAtlasTests.cs').read_text(),'namespace UnityEngine')
stubs=stubs.replace('public struct Color{','public struct Color{public static Color operator*(Color c,float x)=>new Color(c.r*x,c.g*x,c.b*x,c.a*x);')
stubs=stubs.replace('public static class Mathf{','public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static float Abs(float x)=>Math.Abs(x);public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(b,x));public static float Min(float a,float b)=>Math.Min(a,b);public static int FloorToInt(float x)=>(int)Math.Floor(x);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);')
code='using System;using UnityEngine;'+stubs+'\nstatic class SkillIconPresentation{public static float MinimumStroke(int size)=>1;}\n'
for name,body in [('Baseline',old),('Optimized',icon)]:code+='public class '+name+'{static Vector2 V(float x,float y)=>new Vector2(x,y);'+body+'}\n'
code+='''class Program{static void Main(){var random=new Random(17);long checks=0;for(int test=0;test<120;test++){var a=new Baseline.Icon(new Color(.3f,.8f,.9f,.7f),test<108?128:512);var b=new Optimized.Icon(new Color(.3f,.8f,.9f,.7f),test<108?128:512);a.Layered=b.Layered=test%2==0;for(int stroke=0;stroke<8;stroke++){float x=(float)random.NextDouble()*90-13,y=(float)random.NextDouble()*90-13,tx=(float)random.NextDouble()*90-13,ty=(float)random.NextDouble()*90-13,w=(float)random.NextDouble()*12+.1f;a.Line(x,y,tx,ty,w);b.Line(x,y,tx,ty,w);a.Disc(x,y,w);b.Disc(x,y,w);var polygon=new[]{new Vector2(x,y),new Vector2(tx,ty),new Vector2(x+w,ty-w),new Vector2(tx-w,y+w)};a.Polygon(polygon);b.Polygon(polygon);}a.Ring(32,32,25,2);b.Ring(32,32,25,2);for(int i=0;i<a.Pixels.Length;i++){var p=a.Pixels[i];var q=b.Pixels[i];if(Math.Abs(p.r-q.r)+Math.Abs(p.g-q.g)+Math.Abs(p.b-q.b)+Math.Abs(p.a-q.a)>.000001f)throw new Exception("Raster differs at "+test+":"+i);checks++;}}Console.WriteLine("PASS "+checks+" identical RGBA pixels across bounded and full-canvas strokes");}}'''
with tempfile.TemporaryDirectory(prefix='icon-bounds-') as d:
 p=Path(d);(p/'Program.cs').write_text(code);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],check=True)
