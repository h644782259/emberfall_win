Shader "Emberfall/Actor Surface"
{
 Properties {
  _Color("Tint",Color)=(1,1,1,1)
  _MainTex("Material atlas",2D)="white"{}
  _Region("Tile region",Vector)=(.48,.48,.01,.51)
  _PatternScale("Texture repeat",Float)=2
  _TextureStrength("Texture strength",Range(0,1))=.65
  _Relief("Micro relief",Range(0,4))=1
  _Glossiness("Smoothness",Range(0,1))=.25
  _Metallic("Metallic",Range(0,1))=0
  _EmissionColor("Emission",Color)=(0,0,0,0)
  _Mode("Blend mode",Float)=0
  _SrcBlend("Source blend",Float)=1
  _DstBlend("Destination blend",Float)=0
  _ZWrite("Depth write",Float)=1
 }
 SubShader {
 Tags {"RenderType"="Opaque"} LOD 250
 Blend [_SrcBlend] [_DstBlend]
 ZWrite [_ZWrite]
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow keepalpha
 #pragma target 3.0
 #pragma multi_compile_instancing
 #pragma shader_feature_local _ALPHABLEND_ON
 sampler2D _MainTex;fixed4 _Color,_EmissionColor;float4 _Region;half _PatternScale,_TextureStrength,_Relief,_Glossiness,_Metallic;
 struct Input { float2 uv_MainTex; };
 void surf(Input IN,inout SurfaceOutputStandard o) {
  float2 uv=frac(IN.uv_MainTex*_PatternScale)*_Region.xy+_Region.zw;
  fixed3 grain=tex2D(_MainTex,uv).rgb;
  o.Albedo=_Color.rgb*lerp(fixed3(1,1,1),grain*1.35,_TextureStrength);
  float2 dx=float2(.0007,0),dy=float2(0,.0007);
  half x=dot(tex2D(_MainTex,uv-dx).rgb-tex2D(_MainTex,uv+dx).rgb,half3(.3,.59,.11));
  half y=dot(tex2D(_MainTex,uv-dy).rgb-tex2D(_MainTex,uv+dy).rgb,half3(.3,.59,.11));
  o.Normal=normalize(half3(x*_Relief,y*_Relief,1));
  o.Metallic=_Metallic;o.Smoothness=saturate(_Glossiness+(dot(grain,half3(.3,.59,.11))-.5)*.1);
  o.Emission=_EmissionColor.rgb;o.Alpha=_Color.a;
 }
 ENDCG
 }
 Fallback "Standard"
}
