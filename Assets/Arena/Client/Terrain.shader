Shader "Rift/Terrain" {
 Properties { _Color ("Tint", Color) = (1,1,1,1) }
 SubShader { Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Lambert vertex:vert
 struct Input { float4 vertexColor; float3 worldPos; };
 fixed4 _Color;
 sampler2D _RiftVision; float _RiftFog;
 void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.vertexColor=v.color; }
 void surf(Input IN, inout SurfaceOutput o) { float vision=tex2D(_RiftVision,(IN.worldPos.xz+60)/120).r; o.Albedo=IN.vertexColor.rgb*_Color.rgb*lerp(1,.22+.78*vision,_RiftFog); o.Alpha=1; }
 ENDCG
 } Fallback "Diffuse"
}
