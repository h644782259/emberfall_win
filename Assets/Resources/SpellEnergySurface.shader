Shader "Emberfall/Layered Spell Energy"
{
 Properties { _Tint("Energy tint",Color)=(.3,.7,1,1) _Opacity("Envelope",Range(0,1))=1 _Phase("Flow time",Float)=0 _DarkCore("Absorption core",Float)=0 _Heat("Hot emission",Float)=1 _Wispy("Turbulent wind wall",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 local:TEXCOORD1;float3 view:TEXCOORD2;float2 uv:TEXCOORD3;};
   float4 _Tint;float _Opacity,_Phase,_DarkCore,_Heat,_Wispy;
   float hash(float3 p){p=frac(p*.3183+.17);p*=19;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
   float noise(float3 p){float3 b=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(b),hash(b+float3(1,0,0)),f.x),lerp(hash(b+float3(0,1,0)),hash(b+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(b+float3(0,0,1)),hash(b+float3(1,0,1)),f.x),lerp(hash(b+float3(0,1,1)),hash(b+1),f.x),f.y),f.z);}
   v2f vert(appdata v){v2f o;float wave=sin(v.vertex.y*6+v.vertex.x*3-_Phase*7)*.025*(1-_DarkCore);v.vertex.xyz+=v.normal*wave;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);o.uv=v.uv;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 n=normalize(i.normal);float rim=pow(1-saturate(abs(dot(n,normalize(i.view)))),3);
    float grain=noise(i.local*6+float3(_Phase*.8,-_Phase*2,0));float fine=noise(i.local*28+_Phase);
    float streams=pow(.5+.5*sin(i.uv.x*97-grain*7-_Phase*13),5);
    float shade=.25+.75*saturate(dot(n,normalize(float3(-.4,.8,-.5)))*.5+.5);
    float3 dark=_Tint.rgb*.15,hot=lerp(_Tint.rgb,float3(1,.96,.82),.76);
    float3 color=lerp(dark,_Tint.rgb,grain)*shade+hot*(streams*.52+rim*.7+pow(fine,8)*1.3)*_Heat;
    float alpha=_Opacity*_Tint.a*(.5+.5*smoothstep(.15,.75,grain));
    if(_Wispy>.5){float flow=noise(i.local*float3(5,9,5)+float3(_Phase*2,-_Phase*3,_Phase));float filaments=noise(i.local*float3(12,22,12)+float3(-_Phase*3,_Phase*2,0));float edge=smoothstep(0,.12,i.uv.y)*(1-smoothstep(.78,1,i.uv.y));alpha*=smoothstep(.25,.65,flow)*(.3+.7*filaments)*edge;color=lerp(_Tint.rgb*.35,_Tint.rgb*.9,flow)+hot*rim*.25;}
    if(_DarkCore>.5){color=float3(.008,.005,.023)+_Tint.rgb*pow(rim,2)*1.2;alpha=_Opacity;}
    clip(alpha-.025);return fixed4(color,alpha);
   }
   ENDCG
  }
 }
}
