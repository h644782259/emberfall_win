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
        _AtlasRegion("Texture region",Vector)=(1,1,0,0)
        _Relief("Surface micro relief",Range(0,.06))=.012
        _Weather("Stone weathering",Float)=0
        _VertexTint("Use vegetation color and wind weights",Float)=0
        _TerrainFollow("Follow authored terrain",Float)=0
        _GroundTex("Trail edge meadow",2D)="gray"{}
        _TrailEdge("Blend road shoulders",Float)=0
        _WaterSurface("Animated water micro waves",Float)=0
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
        sampler2D _MainTex,_GroundTex;
        fixed4 _Color,_EmissionColor;
        half _Glossiness,_Metallic;
        float _GrainScale,_GrainStrength,_Wind,_ColorTexture,_Relief,_Weather,_VertexTint,_TrailEdge,_WaterSurface,_TerrainFollow;
        float4 _AtlasRegion;
        struct Input {float3 worldPos;float3 worldNormal;float2 uv_MainTex;float4 color:COLOR;INTERNAL_DATA};
        fixed3 Tile(float2 uv){return tex2D(_MainTex,_AtlasRegion.zw+frac(uv)*_AtlasRegion.xy).rgb;}
        fixed3 Grain(float3 p,float3 w){return Tile(p.yz)*w.x+Tile(p.xz)*w.y+Tile(p.xy)*w.z;}
        void vert(inout appdata_full v)
        {
            float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
            float h=EmberHeight(p.xz)*_TerrainFollow;
            float dx=(EmberHeight(p.xz+float2(.02,0))-EmberHeight(p.xz-float2(.02,0)))/.04*_TerrainFollow;
            float dz=(EmberHeight(p.xz+float2(0,.02))-EmberHeight(p.xz-float2(0,.02)))/.04*_TerrainFollow;
            float3 n=UnityObjectToWorldNormal(v.normal);
            n=normalize(float3(n.x-dx*n.y,n.y,n.z-dz*n.y));
            p.y+=h;
            float sway=_Wind*lerp(1,v.color.a,_VertexTint);
            p.x+=sway*sin(_Time.y*1.3+p.z*.8+p.x*.5)*.035;
            p.z+=sway*cos(_Time.y*.9+p.x*.7)*.02;
            v.vertex=mul(unity_WorldToObject,float4(p,1));
            v.normal=normalize(mul(n,(float3x3)unity_ObjectToWorld));
            // World meshes include ribbons without UV tangents; supply a stable frame.
            float3 reference=abs(v.normal.y)>.9?float3(0,0,1):float3(0,1,0);
            v.tangent=float4(normalize(cross(reference,v.normal)),1);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 normal=normalize(WorldNormalVector(IN,float3(0,0,1)));
            float3 w=pow(abs(normal),4);w/=max(dot(w,float3(1,1,1)),.001);
            float3 p=IN.worldPos*_GrainScale;
            fixed3 detail=Grain(p,w);
            // A secondary frequency hides large repeated texture patches at close range.
            float grain=dot(lerp(detail,Grain(p*3.17+float3(.31,.17,.43),w),.22),float3(.299,.587,.114));
            o.Albedo=lerp(_Color.rgb*lerp(1, .55+grain*1.1,_GrainStrength),detail*1.15,_ColorTexture);
            float macro=.95+.05*sin(IN.worldPos.x*.57+sin(IN.worldPos.z*.41))*sin(IN.worldPos.z*.63);
            float patina=saturate(normal.y)*_Weather*(.035+.025*sin(IN.worldPos.x*1.7+IN.worldPos.z*.93));
            o.Albedo=lerp(o.Albedo*macro,o.Albedo*float3(.75,.93,.72),patina);
            o.Albedo*=lerp(float3(1,1,1),IN.color.rgb,_VertexTint);
            if(_TrailEdge>.5)
            {
                float edge=smoothstep(.01,.20,min(IN.uv_MainTex.x,1-IN.uv_MainTex.x)+(grain-.4)*.08);
                fixed3 meadow=tex2D(_GroundTex,IN.worldPos.xz*.4).rgb*1.15;
                o.Albedo=lerp(meadow,o.Albedo,edge);
            }
            // Surface gradient uses pixel derivatives: no extra normal-map texture reads.
            float3 dpdx=ddx(IN.worldPos),dpdy=ddy(IN.worldPos);
            float3 r1=cross(dpdy,normal),r2=cross(normal,dpdx);
            float det=dot(dpdx,r1);
            float3 grad=(r1*ddx(grain)+r2*ddy(grain))*_Relief/(abs(det)>.000001?det:1);
            grad=clamp(grad,-.45,.45);
            float3 bumped=normalize(normal-grad);
            if(_WaterSurface>.5)
            {
                float2 q=IN.worldPos.xz;
                float waveA=dot(q,float2(2.1,1.4))-_Time.y*1.2;
                float waveB=dot(q,float2(-1.3,3.1))+_Time.y*.83;
                float2 ripples=cos(waveA)*float2(2.1,1.4)*.025+cos(waveB)*float2(-1.3,3.1)*.012;
                bumped=normalize(normal-float3(ripples.x,0,ripples.y));
                o.Albedo*=.96+.04*sin(waveA)*sin(waveB);
            }
            o.Normal=normalize(float3(dot(bumped,WorldNormalVector(IN,float3(1,0,0))),dot(bumped,WorldNormalVector(IN,float3(0,1,0))),dot(bumped,normal)));
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
