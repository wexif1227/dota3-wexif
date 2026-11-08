Shader "Rift/River" {
 Properties { _Color("Water",Color)=(.12,.32,.38,1) }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf BlinnPhong fullforwardshadows noforwardadd
 #pragma target 3.0
 struct Input {float3 worldPos;};
 sampler2D _RiftVision;float _RiftFog;fixed4 _Color;
 void surf(Input IN,inout SurfaceOutput o) {
  float2 p=IN.worldPos.xz;float t=_Time.y;float a=sin((p.x-p.y)*2.1+t*1.7),b=sin((p.x+p.y)*3.7+(p.x-p.y)*.4-t*1.1);
  float bank=smoothstep(2.1,3.4,abs(p.x+p.y)*.7071);float foam=pow(saturate(a*.5+b*.25+.2),8)*bank;
  float visibility=lerp(1,.22+.78*tex2D(_RiftVision,(p+60)/120).r,_RiftFog);
  o.Albedo=(_Color.rgb*(.9+a*.045+b*.025)+half3(.12,.17,.15)*bank+foam*.12)*visibility;
  o.Normal=normalize(float3(a*.065,b*.045,1));o.Specular=.55;o.Gloss=.48;o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
