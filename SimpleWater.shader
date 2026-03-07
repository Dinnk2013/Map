Shader "Custom/SimpleWater"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.1, 0.5, 0.7, 1)
        _WaveSpeed ("Wave Speed", Float) = 0.2
        _WaveStrength ("Wave Strength", Float) = 0.1
        _Transparency ("Transparency", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Pass
        {
            Name "FORWARD"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float _WaveSpeed;
            float _WaveStrength;
            float4 _MainColor;
            float _Transparency;

            Varyings vert (Attributes input)
            {
                Varyings output;
                float wave = sin(input.uv.x * 10 + _Time.y * _WaveSpeed) * _WaveStrength;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                worldPos.y += wave;
                output.positionHCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.worldPos = worldPos;
                return output;
            }


            half4 frag (Varyings input) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);
                float fresnel = pow(1.0 - saturate(dot(viewDir, float3(0,1,0))), 3);
                float alpha = _Transparency + fresnel * 0.2;

                return float4(_MainColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Unlit/Transparent"
}
