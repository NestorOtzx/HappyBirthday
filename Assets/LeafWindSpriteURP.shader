Shader "Custom/Sprites/Leaf Wind URP"
{
  Properties
  {
    _MainTex ("Sprite Texture", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)

    _WindStrength ("Wind Strength", Range(0, 0.2)) = 0.035
    _WindSpeed ("Wind Speed", Range(0, 10)) = 2
    _WindFrequency ("Wind Frequency", Range(0, 30)) = 8
    _VerticalInfluence ("Vertical Influence", Range(0, 1)) = 0.75
    _RandomOffset ("Random Offset", Range(0, 20)) = 0

    _PivotY ("Pivot Y", Range(0, 1)) = 0
  }

  SubShader
  {
    Tags
    {
      "Queue"="Transparent"
      "RenderType"="Transparent"
      "RenderPipeline"="UniversalPipeline"
      "CanUseSpriteAtlas"="True"
    }

    Cull Off
    Lighting Off
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
      Name "SpriteWind"
      Tags { "LightMode"="Universal2D" }

      HLSLPROGRAM

      #pragma vertex vert
      #pragma fragment frag

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      TEXTURE2D(_MainTex);
      SAMPLER(sampler_MainTex);

      CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _Color;

        float _WindStrength;
        float _WindSpeed;
        float _WindFrequency;
        float _VerticalInfluence;
        float _RandomOffset;
        float _PivotY;
      CBUFFER_END

      struct Attributes
      {
        float4 positionOS : POSITION;
        float4 color : COLOR;
        float2 uv : TEXCOORD0;
      };

      struct Varyings
      {
        float4 positionCS : SV_POSITION;
        float4 color : COLOR;
        float2 uv : TEXCOORD0;
      };

      Varyings vert(Attributes input)
      {
        Varyings output;

        float2 uv = TRANSFORM_TEX(input.uv, _MainTex);

        float pivotMask = saturate((uv.y - _PivotY) / max(0.0001, 1.0 - _PivotY));
        pivotMask = pow(pivotMask, 1.5);

        float wind =
          sin(
            (_Time.y * _WindSpeed) +
            (uv.y * _WindFrequency) +
            _RandomOffset
          );

        float secondaryWind =
          sin(
            (_Time.y * _WindSpeed * 1.7) +
            (uv.x * _WindFrequency * 0.65) +
            _RandomOffset * 2.13
          ) * 0.35;

        float totalWind = wind + secondaryWind;

        float horizontalOffset =
          totalWind *
          _WindStrength *
          lerp(1.0, pivotMask, _VerticalInfluence);

        float3 positionOS = input.positionOS.xyz;
        positionOS.x += horizontalOffset;

        output.positionCS = TransformObjectToHClip(positionOS);
        output.uv = uv;
        output.color = input.color * _Color;

        return output;
      }

      half4 frag(Varyings input) : SV_Target
      {
        half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
        color *= input.color;
        return color;
      }

      ENDHLSL
    }
  }
}