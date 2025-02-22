Shader "Custom/WallShader"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        _Roughness ("Roughness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0
        _Wetness ("Wetness", Range(0,1)) = 0.0
        _Glossiness ("Glossiness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                float4 fogFactor : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            TEXTURE2D(_BumpMap);
            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Roughness;
                float _Metallic;
                float _NormalStrength;
                float _Wetness;
                float _Glossiness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                float4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_BaseMap, uv);
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uv), _NormalStrength);

                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                normalWS = mul(normalTS, tangentToWorld);

                float3 viewDirectionWS = normalize(input.viewDirWS);
                Light mainLight = GetMainLight(input.shadowCoord);

                float roughness = lerp(_Roughness, 0.0, _Wetness);
                float metallic = lerp(_Metallic, 1.0, _Wetness * 0.5);
                float glossiness = lerp(_Glossiness, 1.0, _Wetness);

                float3 specular = 0;
                float3 diffuse = albedoAlpha.rgb * mainLight.color * saturate(dot(normalWS, mainLight.direction));

                // Добавляем блики от влажности
                if (_Wetness > 0)
                {
                    float3 halfDir = normalize(mainLight.direction + viewDirectionWS);
                    float NdotH = saturate(dot(normalWS, halfDir));
                    float spec = pow(NdotH, glossiness * 100);
                    specular = mainLight.color * spec * _Wetness;
                }

                float3 color = diffuse + specular;

                // Применяем тени и туман
                float shadow = mainLight.shadowAttenuation;
                color *= shadow;
                color = MixFog(color, input.fogFactor);

                return float4(color, albedoAlpha.a);
            }
            ENDHLSL
        }

        // Shadow casting support
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
} 