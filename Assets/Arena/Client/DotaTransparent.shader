Shader "Rift/DotaTransparent" {
 Properties { _MainTex("Albedo",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _Color("Tint",Color)=(1,1,1,1) _Glow("Glow",Range(0,1))=0 _Hidden("Hidden replacement mesh",Float)=0 _Cutoff("Alpha cutoff",Range(0,1))=0 _DstBlend("Destination blend",Float)=10 }
 SubShader { Tags {"RenderType"="Transparent" "Queue"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha [_DstBlend]
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_RiftVision;float4 _MainTex_ST;float _RiftFog,_Hidden;fixed4 _Color;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target{clip(.5-_Hidden);fixed4 c=tex2D(_MainTex,i.uv)*_Color;float vision=tex2D(_RiftVision,(i.world.xz+60)/120).r;c.rgb*=lerp(1,.3+.7*vision,_RiftFog);return c;}
 ENDCG
 }
 } Fallback Off
}
