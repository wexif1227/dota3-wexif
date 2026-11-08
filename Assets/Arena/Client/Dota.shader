Shader "Rift/Dota" {
 Properties { _MainTex("Albedo",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _Color("Tint",Color)=(1,1,1,1) _Glow("Glow",Range(0,1))=0 _Hidden("Hidden replacement mesh",Float)=0 _Cutoff("Alpha cutoff",Range(0,1))=0 _HitFlash("Hit flash",Range(0,1))=0 _Wind("Foliage wind",Float)=0 }
 SubShader { Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"} LOD 250 Cull Off
 CGPROGRAM
 #pragma surface surf BlinnPhong vertex:vert fullforwardshadows addshadow noforwardadd exclude_path:deferred exclude_path:prepass
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap,_RiftVision;float _RiftFog,_Hidden,_Cutoff,_HitFlash,_Wind;half _Glow;fixed4 _Color;
 void vert(inout appdata_full v){float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;float phase=dot(world.xz,float2(.4,.27))+_Time.y*1.15;float3 sway=float3(sin(phase),0,cos(phase*.83))*.095*v.color.a*_Wind;v.vertex.xyz+=mul((float3x3)unity_WorldToObject,sway);}
 struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 worldPos;};
 void surf(Input IN,inout SurfaceOutput o){
  clip(.5-_Hidden);half4 texel=tex2D(_MainTex,IN.uv_MainTex);clip(texel.a-_Cutoff);half3 color=texel.rgb*_Color.rgb;
  float vision=tex2D(_RiftVision,(IN.worldPos.xz+60)/120).r;
  float fog=lerp(1,.3+.7*vision,_RiftFog*step(IN.worldPos.x,100));
  o.Albedo=color*fog;o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));o.Specular=.2;o.Gloss=.18;o.Emission=(color*_Glow+half3(.32,.19,.12)*_HitFlash)*fog;o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
