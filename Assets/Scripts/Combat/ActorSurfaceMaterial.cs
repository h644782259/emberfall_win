using UnityEngine;
namespace Emberfall
{
    internal static class ActorSurfaceMaterial
    {
        internal static bool Enabled=true;
        static Texture2D atlas;static Shader shader;static bool loaded;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){atlas=null;shader=null;loaded=false;Enabled=true;}
        internal static Shader ShaderFor(VisualSurface surface)
        {
            if(!Enabled||(surface!=VisualSurface.Cloth&&surface!=VisualSurface.Skin&&surface!=VisualSurface.Metal))return null;
            if(!loaded){atlas=Resources.Load<Texture2D>("WorldArt/ActorMaterialAtlas");shader=Resources.Load<Shader>("WorldArt/ActorSurface");loaded=true;}
            return atlas!=null&&shader!=null&&shader.isSupported?shader:null;
        }
        internal static void Apply(Material material,VisualSurface surface,Color tint)
        {
            if(shader==null||material.shader!=shader)return;
            bool skin=surface==VisualSurface.Skin,metal=surface==VisualSurface.Metal;
            bool leather=!skin&&!metal&&tint.r>tint.b*1.2f&&tint.r>tint.g*1.1f;
            material.mainTexture=atlas;material.mainTextureScale=Vector2.one;
            material.SetVector("_Region",skin?new Vector4(.48f,.48f,.51f,.01f):metal?new Vector4(.48f,.48f,.51f,.51f):leather?new Vector4(.48f,.48f,.01f,.01f):new Vector4(.48f,.48f,.01f,.51f));
            material.SetFloat("_PatternScale",skin?1:metal?1.5f:2.2f);
            material.SetFloat("_TextureStrength",skin?.22f:metal?.5f:leather?.5f:.65f);
            material.SetFloat("_Relief",skin?.45f:metal?1.1f:1.5f);
            material.SetFloat("_Glossiness",skin?.30f:metal?.57f:leather?.22f:.12f);
        }
    }
}
