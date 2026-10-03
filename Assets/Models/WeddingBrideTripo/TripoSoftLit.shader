Shader "Wedding/Tripo Soft Lit" {
 Properties {
 _BaseMap("Base Texture",2D)="white"{}
_IrisMap("Detailed brown iris",2D)="black"{}
 _BaseColor("Tint",Color)=(1,1,1,1)
 _ShadeFloor("Soft ambient floor",Range(0,1))=.78
 _Wrap("Diffuse wrap",Range(0,1))=.6
 _BrideFinish("Bride skin and silk",Range(0,1))=0
_GroomSkin("Groom skin correction",Range(0,1))=0
 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_IrisMap);SAMPLER(sampler_IrisMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST;half4 _BaseColor;half _ShadeFloor;half _Wrap;half _BrideFinish;half _GroomSkin;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
 struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;float3 positionOS:TEXCOORD3;};
 V vert(A v){V o;VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.positionOS=v.positionOS.xyz;return o;}
 ENDHLSL
 Pass {
 Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 half4 frag(V i):SV_Target {
 half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
 half3 original=base; half cloth=smoothstep(.17,.30,min(original.r,min(original.g,original.b)))*(1-smoothstep(.13,.30,max(original.r,max(original.g,original.b))-min(original.r,min(original.g,original.b))))*(1-smoothstep(.76,.80,i.positionOS.y))*_BrideFinish; half skin=smoothstep(.17,.36,base.r)*smoothstep(.035,.12,base.r-base.b)*smoothstep(.24,.36,base.g/max(base.r,.01))*(1-smoothstep(.62,.78,base.b/max(base.r,.01)))*_BrideFinish;
 base=lerp(base,half3(.99,.755,.655),skin*.82);
 // Local coordinates are metres in the imported FBX; restrict correction to nose.
 float nose=1-smoothstep(.35,1,length((i.positionOS.xy-float2(0,1.052))/float2(.032,.026)));
 nose*=smoothstep(.20,.23,abs(i.positionOS.z));
 half3 nearby=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(.350502,.613775)).rgb*_BaseColor.rgb; nearby=lerp(nearby,half3(.99,.755,.655),.82); base=lerp(base,nearby,nose*_BrideFinish*.88);
 // Pale rosy cheeks with a gradual edge, not painted discs.
float2 faceXY=i.positionOS.xy;
float front=smoothstep(.17,.20,i.positionOS.z);
float blush=exp(-dot((faceXY-float2(sign(faceXY.x)*.15,1.052))/float2(.064,.04),(faceXY-float2(sign(faceXY.x)*.15,1.052))/float2(.064,.04)));
base=lerp(base,half3(.98,.46,.59),blush*skin*front*.37);
// Iris projection remains fixed to the local eye surface and only replaces dark iris texels.
float2 eyeCenter=float2(faceXY.x<0?-.094:.096,1.088);
float2 eye=(faceXY-eyeCenter)/float2(.045,.043);
float eyeArea=(1-smoothstep(1.15,1.6,length(eye)))*front*_BrideFinish;
float darkIris=1-smoothstep(.10,.18,max(original.r,max(original.g,original.b)));
float rim=1-smoothstep(.92,1.07,length(eye));
float er=max(length(eye),.001);float mapped=er<.55?er*.60:.33+(er-.55)*1.4889;float2 ie=eye*(mapped/er);half3 iris=SAMPLE_TEXTURE2D(_IrisMap,sampler_IrisMap,float2(-ie.x,ie.y)*.5+.5).rgb;
iris=lerp(iris*half3(.44,.55,.62),half3(.035,.018,.012),.12); float catchlight=1-smoothstep(.07,.13,length(eye-float2(.30,.38))); float smallglint=1-smoothstep(.025,.055,length(eye-float2(-.25,-.28))); iris=lerp(iris,half3(.97,.98,1),max(catchlight,smallglint*.8));
base=lerp(base,iris,eyeArea*darkIris*rim);
float sclera=eyeArea*(1-darkIris)*smoothstep(.3,.55,min(original.r,min(original.g,original.b)));
base=lerp(base,half3(.94,.96,.97),sclera*.7);
// Brown hair including eyebrows; keep face whites and generated irises separate.
float hair=smoothstep(.015,.05,original.r-original.b)*(1-smoothstep(.23,.38,max(original.r,max(original.g,original.b))))*(1-smoothstep(.42,.62,original.g/max(original.r,.01)))*smoothstep(.84,.90,i.positionOS.y)*_BrideFinish;
base=lerp(base,original*half3(.29,.35,.42),hair*.94);
float lip=(1-smoothstep(.060,.080,abs(i.positionOS.x)))*(1-smoothstep(.02,.032,abs(i.positionOS.y-1.018)))*front*_BrideFinish;
float red=smoothstep(.04,.10,original.r-original.g)*(1-smoothstep(.17,.30,original.g/max(original.r,.01)));
base=lerp(base,half3(.32,.17,.145),lip*red*.82);
float chest=smoothstep(.65,.68,i.positionOS.y)*(1-smoothstep(.85,.87,i.positionOS.y));
float laceRed=chest*red*_BrideFinish;
base=lerp(base,half3(.95,.93,.90),laceRed);
cloth=max(cloth,laceRed);
float groomMask=smoothstep(.20,.38,original.r)*smoothstep(.07,.15,original.r-original.b)*smoothstep(.17,.32,original.g/max(original.r,.01))*_GroomSkin;
base=lerp(base,base*half3(.929,1.595,2.622),groomMask);
float groomFace=smoothstep(.85,.89,i.positionOS.y)*smoothstep(.13,.18,i.positionOS.z)*_GroomSkin;
base=lerp(base,half3(.810,.520,.430),groomFace*groomMask);
float groomFront=smoothstep(.065,.085,i.positionOS.z);
float shirtWidth=lerp(.023,.067,saturate((i.positionOS.y-.72)/.11));
float shirt=(1-smoothstep(shirtWidth,shirtWidth+.008,abs(i.positionOS.x)))*smoothstep(.68,.70,i.positionOS.y)*(1-smoothstep(.835,.85,i.positionOS.y))*smoothstep(.09,.15,original.r)*groomFront*_GroomSkin;
base=lerp(base,half3(.96,.965,.97),shirt);
float groomLip=(1-smoothstep(.065,.078,abs(i.positionOS.x)))*(1-smoothstep(.016,.028,abs(i.positionOS.y-.943)))*red*smoothstep(.21,.24,i.positionOS.z)*_GroomSkin;
base=lerp(base,half3(.33,.16,.13),groomLip*.70);
Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
 half3 n=normalize(i.normalWS);half diffuse=saturate((dot(n,light.direction)+_Wrap)/(1+_Wrap));
 half3 illumination=_ShadeFloor.xxx+(1-_ShadeFloor)*diffuse*lerp(.7,1,light.shadowAttenuation)*light.color;
 illumination=lerp(illumination,half3(.94,.94,.94),groomFace*groomMask*.95);
 base=lerp(base,half3(.95,.93,.90),cloth*.92);
 half3 view=SafeNormalize(GetWorldSpaceViewDir(i.positionWS));half nh=saturate(dot(n,SafeNormalize(light.direction+view)));
 half silk=(pow(nh,30)*.60+pow(nh,8)*.14)*cloth*light.shadowAttenuation; illumination=lerp(illumination,half3(.64,.64,.64)+.36*diffuse*light.color,cloth*.65);
 return half4(base*illumination+silk*light.color,1);
 }
 ENDHLSL
 }
 Pass {
 Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
 ZWrite On ZTest LEqual ColorMask 0 Cull Back
 HLSLPROGRAM
 #pragma target 3.0
 #pragma vertex shadowVert
 #pragma fragment depthFrag
 #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
 float3 _LightDirection;float3 _LightPosition;
 V shadowVert(A v){V o=vert(v);float3 direction=_LightDirection;
 #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
 direction=normalize(_LightPosition-o.positionWS);
 #endif
 o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,normalize(o.normalWS),direction));
 #if UNITY_REVERSED_Z
 o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
 #else
 o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
 #endif
 return o;}
 half4 depthFrag(V i):SV_Target{return 0;}
 ENDHLSL
 }
 Pass {
 Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
 HLSLPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment depthFrag
 half4 depthFrag(V i):SV_Target{return 0;}
 ENDHLSL
 }
 }
}

















