Shader "Rift/Hero" {
 Properties { _Gloss ("Surface polish",Range(0,1))=.38 }
 SubShader { Tags { "RenderType"="Opaque" } LOD 250 Cull Off
 CGPROGRAM
 #pragma surface surf BlinnPhong fullforwardshadows noforwardadd exclude_path:deferred exclude_path:prepass
 #pragma target 3.0
 struct Input { float4 color:COLOR; float3 worldPos; float3 viewDir; };
 sampler2D _RiftVision; float _RiftFog; half _Gloss;
 void surf(Input IN,inout SurfaceOutput o) {
   float vision=tex2D(_RiftVision,(IN.worldPos.xz+60)/120).r;
   float fog=lerp(1,.3+.7*vision,_RiftFog*step(IN.worldPos.x,100));
   o.Albedo=IN.color.rgb*fog;
   o.Specular=.65;o.Gloss=_Gloss*(1-IN.color.a*.7);
   half rim=pow(1-saturate(dot(normalize(IN.viewDir),o.Normal)),3);
   o.Emission=(IN.color.rgb*IN.color.a*.45+float3(.035,.055,.08)*rim)*fog;o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
