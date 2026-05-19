Shader "VRSYS/CylinderGrid_URP"
{
    Properties
    {
        _FirstColor ("Grid Color", Color) = (1,1,1,1)
        _SecondColor ("Background Color", Color) = (0.5,0.5,0.5,0.2)
        _AlphaMultiplier ("Alpha Multiplier", Range(0,1)) = 0.99
        _Inside ("Is Inside", Float) = 1
        _VerticalGridSize ("Vertical Grid Size", Float) = 10.0
        _HorizontalGridSize ("Horizontal Grid Size", Float) = 8.0
        _GridThickness ("Grid Thickness", Range(0.001, 0.1)) = 0.02
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }

        // Depth pre-pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly"}

            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Front faces
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward"
                "Queue"="Transparent" 
            }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FirstColor;
                half4 _SecondColor;
                float _AlphaMultiplier;
                float _Inside;
                float _VerticalGridSize;
                float _HorizontalGridSize;
                float _GridThickness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float height : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float fogCoord : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                output.height = input.positionOS.y;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            // Vertical grid lines
            float verticalGrid(float y, float size, float thickness)
            {
                float g = abs(frac(y * size - 0.5) - 0.5) / thickness;
                return saturate(1.0 - g);
            }
            
            // Horizontal grid lines (circular)
            float horizontalGrid(float2 xz, float size, float thickness)
            {
                float angle = atan2(xz.y, xz.x) / (2.0 * PI);
                angle = frac(angle);
                
                float g = abs(frac(angle * size - 0.5) - 0.5) / thickness;
                return saturate(1.0 - g);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Create vertical grid lines
                float vGrid = verticalGrid(input.positionOS.y, _VerticalGridSize, _GridThickness);
                
                // Create horizontal grid circles
                float hGrid = horizontalGrid(input.positionOS.xz, _HorizontalGridSize, _GridThickness);
                
                // Combine grids
                float gridPattern = max(vGrid, hGrid);
                
                // Create final color
                half4 color = lerp(_SecondColor, _FirstColor, gridPattern);
                
                // Apply alpha calculations
                color.a *= 1 - 3 * input.height;
                color.a *= (1 - _AlphaMultiplier);
                
                // Environment occlusion
                float bias = 0.0;
                float isVisible = CalculateEnvironmentDepthOcclusion(input.positionWS, bias);
                if (_Inside == 1)
                {
                    color.a *= isVisible;
                }
                
                clip(color.a - 0.01);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                return color;
            }
            ENDHLSL
        }

        // Back faces
        Pass
        {
            Name "ForwardLitBack"
            Tags { "LightMode" = "UniversalForward"
                "Queue"="Transparent - 5" 
            }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FirstColor;
                half4 _SecondColor;
                float _AlphaMultiplier;
                float _Inside;
                float _VerticalGridSize;
                float _HorizontalGridSize;
                float _GridThickness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float height : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float fogCoord : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                output.height = input.positionOS.y;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            // Vertical grid lines
            float verticalGrid(float y, float size, float thickness)
            {
                float g = abs(frac(y * size - 0.5) - 0.5) / thickness;
                return saturate(1.0 - g);
            }
            
            // Horizontal grid lines (circular)
            float horizontalGrid(float2 xz, float size, float thickness)
            {
                float angle = atan2(xz.y, xz.x) / (2.0 * PI);
                angle = frac(angle);
                
                float g = abs(frac(angle * size - 0.5) - 0.5) / thickness;
                return saturate(1.0 - g);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Create vertical grid lines
                float vGrid = verticalGrid(input.positionOS.y, _VerticalGridSize, _GridThickness);
                
                // Create horizontal grid circles
                float hGrid = horizontalGrid(input.positionOS.xz, _HorizontalGridSize, _GridThickness);
                
                // Combine grids
                float gridPattern = max(vGrid, hGrid);
                
                // Create final color
                half4 color = lerp(_SecondColor, _FirstColor, gridPattern);
                
                // Apply alpha calculations
                color.a *= 1 - 1.6 * input.height;
                color.a *= (1 - _AlphaMultiplier);
                
                // Environment occlusion
                float bias = 0.0;
                float isVisible = CalculateEnvironmentDepthOcclusion(input.positionWS, bias);
                if (_Inside == 1)
                {
                    color.a *= isVisible;
                }
                
                clip(color.a - 0.01);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
    }
}