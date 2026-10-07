// Rendering and prop object boundaries only. Room geometry executes production builders.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public enum PrimitiveType { Cube,Cylinder }
    public class Material { public Color color; }
}
namespace Emberfall
{
    public enum VisualSurface { Wood }
    public enum WaterEnvironment { Courtyard,Tactical }
    public enum DestructibleKind { Crate,Pot,Rubble }
    public enum PropRecovery { None,Energy,Health }
    public class WorldResources { public Material Material(Color color,bool emission=false,VisualSurface surface=VisualSurface.Wood)=>new Material{color=color}; }
    public static class DestructiblePropFactory
    {
        private struct Placed { public Vector3 Point;public float Radius; }
        private static readonly List<Placed> placed=new List<Placed>();
        public static void Reset(){placed.Clear();}
        public static void Create(Transform parent,Vector3 point,DestructibleKind kind,int level,bool blocksPath=false,PropRecovery recovery=PropRecovery.Energy)
        {
            float radius=kind==DestructibleKind.Rubble?.7f:kind==DestructibleKind.Crate?.52f:.4f;
            if(!WorldTraversal.IsWalkable(point,radius))return;
            foreach(var prop in placed)if(Vector3.Distance(point,prop.Point)<radius+prop.Radius+.12f)return;
            placed.Add(new Placed{Point=point,Radius=radius});
            if(blocksPath)WorldTraversal.AddDynamicCircle(point,radius);
        }
    }
    public static partial class WorldBuilder
    {
        private static void Primitive(Transform parent,string name,PrimitiveType type,Vector3 at,Vector3 size,Material material,bool cameraOccluder=false){}
        private static void PointLight(Transform parent,Vector3 at,Color color,float intensity,float range){}
        private static void Crystal(Transform parent,WorldResources resources,Vector3 at,float size,Material material){}
        private static void BuildWaterSurface(Transform parent,WorldResources resources,string name,Vector3[] river,float width,float height,WaterEnvironment mode){}
        private static void BuildBridgeWaterContact(Transform parent,WorldResources resources,Rect bridge,float height){}
        private static void Tree(Transform parent,WorldResources resources,Vector3 at,float scale,int seed){throw new NotSupportedException("Legacy room trees outside tested route");}
        private static void Rock(Transform parent,WorldResources resources,Vector3 at,float scale,int seed){throw new NotSupportedException("Legacy room rocks outside tested route");}
    }
}
