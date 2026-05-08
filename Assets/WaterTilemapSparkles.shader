Shader "Custom/Tilemap/Water Sparkles Pixel URP"
{
  Properties
  {
    _MainTex ("Sprite Texture", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)

    _SparkleColor ("Sparkle Color", Color) = (1,1,1,1)
    _SparkleIntensity ("Sparkle Intensity", Range(0, 5)) = 1
    _SparkleDensity ("Sparkle Density", Range(1, 128)) = 32
    _SparkleSize ("Sparkle Size", Range(0.001, 0.5)) = 0.03
    _SparkleSpeed ("Sparkle Speed", Range(0.1, 10)) = 1.5
    _SparkleChance ("Sparkle Chance", Range(0, 1)) = 0.1
  }

  SubShader
  {
    Tags
    {
      "RenderPipeline"="UniversalPipeline"
      "Queue"="Transparent"
      "RenderType"="Transparent"
      "CanUseSpriteAtlas"="True"
    }

    Cull Off
    Lighting Off
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
      Name "SpriteUnlit"
      Tags { "LightMode"="Universal2D" }

      HLSLPROGRAM

      #pragma vertex vert
      #pragma fragment frag

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

      TEXTURE2D(_MainTex);
      SAMPLER(sampler_MainTex);

      CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _Color;

        float4 _SparkleColor;

        float _SparkleIntensity;
        float _SparkleDensity;
        float _SparkleSize;
        float _SparkleSpeed;
        float _SparkleChance;
      CBUFFER_END

      float Hash21(float2 p)
      {
        p = frac(p * float2(234.34, 435.345));
        p += dot(p, p + 34.23);
        return frac(p.x * p.y);
      }

      Varyings vert(Attributes input)
      {
        Varyings output;

        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

        output.uv = TRANSFORM_TEX(input.uv, _MainTex);

        output.color = input.color * _Color;

        return output;
      }

      half4 frag(Varyings input) : SV_Target
      {
        half4 baseColor =
          SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

        baseColor *= input.color;

        float2 sparkleUV = input.uv * _SparkleDensity;

        float2 cell = floor(sparkleUV);

        float2 local = frac(sparkleUV);

        float randomValue = Hash21(cell);

        float active =
          step(1.0 - _SparkleChance, randomValue);

        float randomTimeOffset =
          Hash21(cell + 17.123) * 20.0;

        float randomSpeed =
          lerp(
            0.5,
            1.7,
            Hash21(cell + 92.77)
          );

        float pulse =
          sin(
            (_Time.y * _SparkleSpeed * randomSpeed)
            + randomTimeOffset
          );

        pulse = saturate(pulse * 0.5 + 0.5);

        pulse = pow(pulse, 8.0);

        float2 squareCenter;

        float safeMargin = _SparkleSize + 0.01;

        squareCenter.x = lerp(safeMargin, 1.0 - safeMargin, Hash21(cell + 45.12));
        squareCenter.y = lerp(safeMargin, 1.0 - safeMargin, Hash21(cell + 87.54));

        float2 distanceToCenter =
          abs(local - squareCenter);

        float square =
          step(distanceToCenter.x, _SparkleSize) *
          step(distanceToCenter.y, _SparkleSize);

        float sparkle =
          square *
          active *
          pulse *
          baseColor.a;

        baseColor.rgb +=
          _SparkleColor.rgb *
          sparkle *
          _SparkleIntensity;

        return baseColor;
      }

      ENDHLSL
    }
  }
}