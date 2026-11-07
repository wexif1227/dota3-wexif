Shader "Rift/Lit" {
 Properties { _Color ("Color",Color)=(1,1,1,1) _EmissionColor ("Glow",Color)=(0,0,0,0) }
 SubShader { Tags { "RenderType"="Opaque" } LOD 150
 CGPROGRAM
 #pragma surface surf Lambert fullforwardshadows noforwardadd exclude_path:deferred exclude_path:prepass
 #pragma target 3.0
 struct Input { float3 worldPos; };
 fixed4 _Color; fixed4 _EmissionColor;
 sampler2D _RiftVision; float _RiftFog;
 void surf(Input IN,inout SurfaceOutput o) { float vision=tex2D(_RiftVision,(IN.worldPos.xz+60)/120).r; float fog=lerp(1,.3+.7*vision,_RiftFog);o.Albedo=_Color.rgb*fog;o.Emission=_EmissionColor.rgb*fog;o.Alpha=1; }
 ENDCG
 } Fallback "Diffuse"
}
