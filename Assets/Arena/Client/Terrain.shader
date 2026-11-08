Shader "Rift/Terrain" {
 Properties { _Color ("Tint", Color) = (1,1,1,1) }
 SubShader { Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Lambert vertex:vert fullforwardshadows
 #pragma target 3.0
 struct Input { float4 vertexColor; float3 worldPos; };
 fixed4 _Color;
 sampler2D _RiftVision; float _RiftFog;
 void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.vertexColor=v.color; }
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 void surf(Input IN, inout SurfaceOutput o) { float vision=tex2D(_RiftVision,(IN.worldPos.xz+60)/120).r;float detail=.88+noise(IN.worldPos.xz*1.8)*.16+noise(IN.worldPos.xz*.25)*.13;o.Albedo=IN.vertexColor.rgb*_Color.rgb*detail*lerp(1,.22+.78*vision,_RiftFog); o.Alpha=1; }
 ENDCG
 } Fallback "Diffuse"
}
