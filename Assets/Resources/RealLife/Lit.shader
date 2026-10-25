// Flat-shaded low-poly surfaces: the colour comes from the mesh's vertex colours.
Shader "RealLife/Lit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Emission ("Emission", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _Color;
        half _Emission;
        struct Input { float4 color : COLOR; };
        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
            o.Emission = IN.color.rgb * _Color.rgb * _Emission;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
