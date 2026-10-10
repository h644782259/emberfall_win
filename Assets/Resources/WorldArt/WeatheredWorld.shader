Shader "Emberfall/Weathered World"
{
    Properties
    {
        _Color("Tint",Color)=(1,1,1,1)
        _MainTex("Material grain",2D)="gray"{}
        _Glossiness("Smoothness",Range(0,1))=.2
        _Metallic("Metallic",Range(0,1))=0
        _EmissionColor("Emission",Color)=(0,0,0,0)
        _GrainScale("Grain per meter",Float)=.45
        _GrainStrength("Grain contrast",Float)=.4
        _Wind("Leaf breeze",Float)=0
        _Cull("Face culling",Float)=2
        _ColorTexture("Colored ground detail",Float)=0
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        Cull [_Cull]
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #include "UnityCG.cginc"
        #include "TerrainField.cginc"
        sampler2D _MainTex;
        fixed4 _Color,_EmissionColor;
        half _Glossiness,_Metallic;
        float _GrainScale,_GrainStrength,_Wind,_ColorTexture;
        struct Input {float3 worldPos;float3 worldNormal;INTERNAL_DATA};
        void vert(inout appdata_full v)
        {
            float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
            float h=EmberHeight(p.xz);
            float dx=(EmberHeight(p.xz+float2(.02,0))-EmberHeight(p.xz-float2(.02,0)))/.04;
            float dz=(EmberHeight(p.xz+float2(0,.02))-EmberHeight(p.xz-float2(0,.02)))/.04;
            float3 n=UnityObjectToWorldNormal(v.normal);
            n=normalize(float3(n.x-dx*n.y,n.y,n.z-dz*n.y));
            p.y+=h;
            p.x+=_Wind*sin(_Time.y*1.3+p.z*.8+p.x*.5)*.035;
            p.z+=_Wind*cos(_Time.y*.9+p.x*.7)*.02;
            v.vertex=mul(unity_WorldToObject,float4(p,1));
            v.normal=normalize(mul(n,(float3x3)unity_ObjectToWorld));
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 w=pow(abs(IN.worldNormal),4);w/=max(dot(w,float3(1,1,1)),.001);
            float3 p=IN.worldPos*_GrainScale;
            fixed3 detail=tex2D(_MainTex,p.yz).rgb*w.x+tex2D(_MainTex,p.xz).rgb*w.y+tex2D(_MainTex,p.xy).rgb*w.z;
            float grain=detail.r;
            o.Albedo=lerp(_Color.rgb*lerp(1, .55+grain*1.1,_GrainStrength),detail*1.15,_ColorTexture);
            o.Metallic=_Metallic;
            o.Smoothness=_Glossiness*lerp(.7,1.05,grain);
            o.Occlusion=lerp(.85,1,grain);
            o.Emission=_EmissionColor.rgb;
            o.Alpha=_Color.a;
        }
        ENDCG
    }
    Fallback "Standard"
}
