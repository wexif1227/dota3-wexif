Shader "Rift/CosmeticParticle" {
 Properties { _MainTex("Sprite",2D)="white"{} _Gain("Intensity",Float)=1.6 _DstBlend("Destination blend",Float)=1 _Monochrome("Recolor sprite",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Cull Off ZWrite Off Blend SrcAlpha [_DstBlend]
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_RiftVision;float4 _MainTex_ST;float _RiftFog,_Gain,_Monochrome;
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float3 world:TEXCOORD1;};
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target {
  float vision=tex2D(_RiftVision,(i.world.xz+60)/120).r;
  float visibility=lerp(1,vision,_RiftFog*step(i.world.x,100));clip(visibility-.025);
  fixed4 c=tex2D(_MainTex,i.uv);c.rgb=lerp(c.rgb,max(c.r,max(c.g,c.b)).xxx,_Monochrome);c*=i.color;c.rgb*=_Gain;c.a*=visibility;return c;
 }
 ENDCG
 }
 } Fallback Off
}
