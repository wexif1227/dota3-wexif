Shader "Rift/Atmosphere" {
 Properties { _MainTex("Scene",2D)="white"{} _Bloom("Bloom",2D)="black"{} }
 SubShader { Cull Off ZWrite Off ZTest Always
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_Bloom;float4 _MainTex_TexelSize;
 half4 bright(v2f_img i):SV_Target {half3 c=tex2D(_MainTex,i.uv).rgb;half l=max(c.r,max(c.g,c.b));return half4(c*saturate((l-.72)/max(l,.001)),1);}
 half4 blur(v2f_img i):SV_Target {float2 t=_MainTex_TexelSize.xy*1.5;return (tex2D(_MainTex,i.uv+float2(t.x,t.y))+tex2D(_MainTex,i.uv+float2(-t.x,t.y))+tex2D(_MainTex,i.uv+float2(t.x,-t.y))+tex2D(_MainTex,i.uv-t))*.25;}
 half4 composite(v2f_img i):SV_Target {
  half4 c=tex2D(_MainTex,i.uv);c.rgb+=tex2D(_Bloom,i.uv).rgb*.22;
  half l=dot(c.rgb,half3(.2126,.7152,.0722));c.rgb=lerp(l.xxx,c.rgb,1.045);c.rgb=(c.rgb-.18)*1.035+.18;
  c.rgb*=lerp(half3(.965,.992,1.025),half3(1.025,1.008,.975),saturate(l));float2 p=i.uv*2-1;c.rgb*=1-dot(p,p)*.035;return c;
 }
 ENDCG
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment bright
 ENDCG }
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment blur
 ENDCG }
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment composite
 ENDCG }
 } Fallback Off
}
