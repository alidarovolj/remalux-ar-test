Shader "Custom/WallPaintShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PaintMask ("Paint Mask", 2D) = "black" {}
        _Color ("Paint Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PaintMask;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_PaintMask;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Получаем базовый цвет текстуры
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex);
            
            // Получаем значение маски покраски
            fixed4 paintMask = tex2D(_PaintMask, IN.uv_PaintMask);
            
            // Смешиваем базовый цвет с цветом покраски на основе маски
            fixed4 finalColor = lerp(baseColor, _Color, paintMask.r);
            
            o.Albedo = finalColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = finalColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
} 