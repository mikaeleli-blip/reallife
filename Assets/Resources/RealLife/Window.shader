// Window glass: dark by day, lit from inside at night. The vertex colour is the room light behind each pane.
Shader "RealLife/Window"
{
    Properties
    {
        _Color ("Glass", Color) = (0.26,0.33,0.42,1)
        _Glow ("Glow", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _Color;
        half _Glow;
        struct Input { float4 color : COLOR; };
        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = lerp(_Color.rgb, _Color.rgb * 0.4, _Glow);
            o.Emission = IN.color.rgb * _Glow;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
