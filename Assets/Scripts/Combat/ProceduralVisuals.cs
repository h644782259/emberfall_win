using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    internal enum VisualSurface { Cloth, Skin, Metal, Crystal, Stone, Wood, Foliage, Water }

    // Original, texture-free meshes. A fixed cache is shared by every actor and room;
    // visual meshes never create colliders or change the traversal/collision model.
    internal static class ProceduralVisuals
    {
        private static readonly Dictionary<PrimitiveType, Mesh> shapes = new Dictionary<PrimitiveType, Mesh>();
        private static Mesh rock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            foreach (Mesh mesh in shapes.Values) if (mesh != null) UnityEngine.Object.Destroy(mesh);
            shapes.Clear();
            if (rock != null) UnityEngine.Object.Destroy(rock);
            rock = null;
        }

        public static Mesh Shape(PrimitiveType shape)
        {
            Mesh mesh;
            if (shapes.TryGetValue(shape, out mesh) && mesh != null) return mesh;
            if(shape==PrimitiveType.Quad)
            {
                mesh=new Mesh {name="Emberfall shared visual quad",
                    vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},
                    normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back},
                    uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)},triangles=new[]{0,2,1,2,3,1}};
                mesh.RecalculateBounds();shapes[shape]=mesh;return mesh;
            }
            VisualMeshData data = shape == PrimitiveType.Cube ? VisualMeshRecipes.BevelBox() :
                shape == PrimitiveType.Cylinder ? VisualMeshRecipes.Cylinder(32) :
                VisualMeshRecipes.RoundBody(shape == PrimitiveType.Capsule, 24, 18);
            mesh = Build(data, "Emberfall shared " + shape);
            shapes[shape] = mesh;
            return mesh;
        }

        public static Mesh WeatheredRock
        {
            get
            {
                if (rock == null) rock = Build(VisualMeshRecipes.Rock(), "Emberfall weathered boulder");
                return rock;
            }
        }

        public static Mesh Build(VisualMeshData data, string name)
        {
            Mesh mesh = new Mesh { name = name, vertices = data.Vertices, normals = data.Normals,
                uv = data.Uv, triangles = data.Triangles };
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject Create(string name, PrimitiveType shape, Material material)
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<MeshFilter>().sharedMesh = Shape(shape);
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return obj;
        }

        public static VisualSurface SurfaceFor(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("crystal") || n.Contains("rune") || n.Contains("luminous") || n.Contains("orb") || n.Contains("spirit core")) return VisualSurface.Crystal;
            if (n.Contains("blade") || n.Contains("armor") || n.Contains("plate") || n.Contains("guard") || n.Contains("steel") || n.Contains("helmet") || n.Contains("gold") || n.Contains("crest") || n.Contains("buckle") || n.Contains("pauldron") || n.Contains("hammer") || n.Contains("collar") || n.Contains("inlay")) return VisualSurface.Metal;
            if (n.Contains("head") || n.Contains("skin") || n.Contains("nose") || n.Contains("ear") || n.Contains("slime") || n.Contains("eye")) return VisualSurface.Skin;
            if (n.Contains("leaf") || n.Contains("crown foliage")) return VisualSurface.Foliage;
            if (n.Contains("staff") || n.Contains("wood") || n.Contains("grip") || n.Contains("bow limb")) return VisualSurface.Wood;
            return VisualSurface.Cloth;
        }

        public static void ApplySurface(Material material, VisualSurface surface)
        {
            bool metal = surface == VisualSurface.Metal, crystal = surface == VisualSurface.Crystal;
            float smoothness = surface == VisualSurface.Water ? .82f : metal ? .62f : crystal ? .76f : surface == VisualSurface.Skin ? .34f :
                surface == VisualSurface.Wood ? .23f : surface == VisualSurface.Stone ? .18f : .1f;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metal ? .58f : crystal ? .12f : 0f);
            if (crystal && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", material.color * .32f);
            }
            SurfaceTextureLibrary.Apply(material, surface);
            material.enableInstancing = true;
        }
    }

}
