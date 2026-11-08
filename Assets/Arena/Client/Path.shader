Shader "Rift/Path" {
 Properties { _Color("Stone / earth",Color)=(.6,.58,.5,1) _Paving("Paved",Float)=1 _MainTex("UV",2D)="white"{} }
 SubShader { Tags {"RenderType"="Opaque"} LOD 180
 CGPROGRAM
 #pragma surface surf Lambert fullforwardshadows noforwardadd
 #pragma target 3.0
 struct Input {float2 uv_MainTex;float3 worldPos;};
 fixed4 _Color;float _Paving,_RiftFog;sampler2D _RiftVision;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutput o) {
  float2 uv=IN.uv_MainTex;float row=floor(uv.y/1.05);float2 q=float2((uv.x+frac(row*.5)*1.25)/1.25,uv.y/1.05);float2 f=frac(q);
  float edge=min(min(f.x,1-f.x),min(f.y,1-f.y));float mortar=smoothstep(.018,.065,edge);
  float stone=(.8+hash(floor(q))*.23)*lerp(.62,1,mortar);
  float earth=.83+noise(uv*.8)*.14+noise(uv*5)*.04;
  float grain=hash(floor(uv*32))*.035;float vision=tex2D(_RiftVision,(IN.worldPos.xz+60)/120).r;
  o.Albedo=_Color.rgb*(lerp(earth,stone,_Paving)+grain)*lerp(1,.25+.75*vision,_RiftFog);o.Alpha=1;
 }
 ENDCG
 } Fallback "Diffuse"
}
