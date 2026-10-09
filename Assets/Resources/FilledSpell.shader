Shader "Emberfall/Filled Spell Volume"
{
    Properties
    {
        _Color ("Tint", Color) = (0.25,0.75,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Progress ("Lifetime", Range(0,1)) = 0
        _Style ("Dissolve", Range(0,1)) = 0
        _Element ("Element family", Float) = 0
        _Seed ("Variation", Float) = 0
        _EnvelopeMode ("State envelope", Float) = 0
        _EnvelopeAge ("State age", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 local:TEXCOORD2; float3 view:TEXCOORD3; };
            fixed4 _Color; float _Opacity, _Progress, _Style, _EnvelopeMode, _EnvelopeAge, _Element, _Seed;
            v2f vert(appdata v)
            {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;o.view=WorldSpaceViewDir(v.vertex);return o;}
            float hash(float3 p) { p=frac(p*.3183099+float3(.13,.37,.71));p*=17;return frac(p.x*p.y*p.z*(p.x+p.y+p.z)); }
            float noise(float3 p) {
                float3 b=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(b),hash(b+float3(1,0,0)),f.x),lerp(hash(b+float3(0,1,0)),hash(b+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(b+float3(0,0,1)),hash(b+float3(1,0,1)),f.x),lerp(hash(b+float3(0,1,1)),hash(b+1),f.x),f.y),f.z);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 normal=normalize(i.normal);
                float rim=pow(1-saturate(abs(dot(normal,normalize(i.view)))),2);
                float time=_Time.y+_Seed*3.17;
                float3 flow=i.local*5+float3(_Seed,-time*1.7,0);
                float grain=noise(flow)*.65+noise(flow*2.13+7)*.35;
                float fine=noise(i.local*23+_Seed);
                float light=.62+.38*abs(dot(normal,normalize(float3(.35,.8,.4))));
                float heat=saturate(grain*.85+rim*.4+(1-saturate(i.uv.y))*.25);
                float3 core=lerp(_Color.rgb,1,.7),outer=_Color.rgb*.45;
                float pattern=grain;
                if(_Element>.5&&_Element<1.5) { // Fire: turbulent bright root, dark cooling tongues.
                    core=float3(1,.94,.55);outer=lerp(float3(.48,.025,.006),_Color.rgb,.2);
                    heat=saturate(heat+.2-saturate(i.uv.y)*.3);
                } else if(_Element<2.5&&_Element>1.5) { // Ice: crisp internal facets and fine cracks.
                    core=float3(.8,.98,1);outer=_Color.rgb*.32;
                    float veins=1-smoothstep(.025,.065,abs(fine-.5));pattern=max(grain*.65,veins);heat=saturate(rim*.8+veins*.6+.18);
                } else if(_Element<3.5&&_Element>2.5) { // Lightning: travelling filaments, white-hot core.
                    float strand=pow(saturate(.5+.5*sin(i.local.y*38+grain*12-time*19)),10);
                    core=float3(.94,.93,1);outer=_Color.rgb*.4;pattern=max(grain,strand);heat=saturate(.35+strand*.65+rim*.3);
                } else if(_Element<4.5&&_Element>3.5) { // Poison: mottled bubbles with yellow-green edges.
                    core=float3(.85,1,.24);outer=float3(.08,.18,.035);pattern=smoothstep(.2,.75,grain);heat=saturate(pattern*.7+rim*.35);
                } else if(_Element<5.5&&_Element>4.5) { // Summoning: opposing currents and violet seams.
                    core=float3(.72,1,.94);outer=lerp(float3(.18,.035,.38),_Color.rgb,.2);
                    pattern=.5+.5*sin(i.local.y*18+grain*9-time*5);heat=saturate(pattern*.55+rim*.6);
                } else { // Steel/arcane: bright cutting edge over a shaded body.
                    heat=saturate(rim*.6+grain*.45+i.uv.y*.3);
                }
                float dissolve=saturate((_Progress-.48)*1.92)*_Style;
                float alpha=_Color.a*_Opacity*saturate((pattern+.48-dissolve)*3);
                if(_EnvelopeMode>.5)
                {
                    // The authored cage already leaves face/chest open. Preserve its
                    // silhouette, with steady guard ribs and a soft rising healing band.
                    float ribs=.65+.35*abs(sin(i.uv.x*6.283185));
                    alpha=_Color.a*_Opacity*ribs;
                    if(_EnvelopeMode>1.5 && _EnvelopeMode<2.5)
                        alpha*=.65+.35*saturate((_EnvelopeAge-i.uv.y*.18)*18+1);
                    if(_EnvelopeMode>2.5 && _EnvelopeMode<3.5)
                        alpha*=.55+.45*pow(.5+.5*sin(i.uv.y*6.283185-_EnvelopeAge*4),3);
                }
                clip(alpha-.025);
                float3 tint=lerp(outer,core,smoothstep(.1,.9,heat))*light;
                tint+=core*rim*.25;
                return fixed4(tint,alpha);
            }
            ENDCG
        }
    }
}
