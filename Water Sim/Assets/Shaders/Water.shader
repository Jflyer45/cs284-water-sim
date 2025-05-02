Shader "Custom/Water"
{
    Properties
    {
        [Header(Main)]
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        [Header(Fog)]
        _WaterFogColor("Water Fog Color", Color) = (0, 0, 0, 0)
        _WaterFogDensity("Water Fog Density", Range(0, 2)) = 0.1

        [Header(Reflection)]
        _CubeMap("Cube Map", CUBE) = "white" {}
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 1

        [Header(Refraction)]
        _RefractionStrength("Refraction Strength", Range(0, 1)) = 0.25
        _RefractionStrength2("Refraction Strength2", Range(0, 1)) = 0.25
        
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
        LOD 200
        CULL OFF
        GrabPass { "_WaterBackground" }
        CGPROGRAM
        #pragma surface surf Standard alpha finalcolor:ResetAlphaAtEnd vertex:vert addshadow
        #pragma target 3.0

        #include "LookingThroughWater.cginc"

        // Skybox cubemap to calculate reflections
        samplerCUBE _CubeMap;
        sampler2D _MainTex, _DerivHeightMap;
        
        // Displacement textures array - will be set via script
        UNITY_DECLARE_TEX2DARRAY(_DisplacementTextures);
        
        // Hidden properties - will be set via script
        float _DisplacementDepthAttenuation;
        float _Tile0, _Tile1, _Tile2, _Tile3;
        float _ReflectionStrength;

        struct Input
        {
            float2 uv_MainTex;
            float4 screenPos;
            float3 viewDir;
            float3 worldRefl;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float3 SampleDisplacement(float3 worldPos) {
            float3 displacement1 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile0, 0), 0);
            float3 displacement2 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile1, 1), 0);
            float3 displacement3 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile2, 2), 0);
            float3 displacement4 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile3, 3), 0);
            return displacement1 + displacement2 + displacement3 + displacement4;
        }

        void ResetAlphaAtEnd(Input IN, SurfaceOutputStandard o, inout fixed4 color) {
            color.a = 1;
        }

        float3 ScaleAndUnpackDerivative(float4 textureData) {
            float3 dh = textureData.agb;
            dh.xy = dh.xy * 2 - 1;
            return dh;
        }
    
        void vert(inout appdata_full vertexData) {
            // Store vertex position in variable p
            float3 p = vertexData.vertex.xyz;
            
            // Get world position for displacement sampling
            float3 worldPos = mul(unity_ObjectToWorld, vertexData.vertex).xyz;
            
            // Sample displacement
            float3 displacement = SampleDisplacement(worldPos);
            
            // Calculate depth for attenuation
            float4 clipPos = UnityObjectToClipPos(vertexData.vertex);
            float depth = 1 - Linear01Depth(clipPos.z / clipPos.w);
            
            // Apply depth attenuation to displacement
            displacement = lerp(0.0f, displacement, pow(saturate(depth), _DisplacementDepthAttenuation));
            
            // Apply displacement in object space
            p += mul(unity_WorldToObject, displacement.xyz);
            
            // Set the displaced vertex position
            vertexData.vertex.xyz = p;
            
            // Calculate tangent space for normal calculations
            float3 tangent = float3(1, 0, 0);
            float3 binormal = float3(0, 0, 1);
            
            // The normal vector is the cross product of both tangent vectors.
            float3 normal = normalize(cross(binormal, tangent));
            
            // Update normal to match the displacement
            vertexData.normal = normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 color = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = color.rgb;

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = color.a;
            o.Emission = ColorBelowWater(IN.screenPos, o.Normal * 20) * (1 - color.a)
                + texCUBE(_CubeMap, WorldReflectionVector(IN, o.Normal)).rgb * (1 - dot(IN.viewDir, o.Normal)) * (1 - _ReflectionStrength);
        }
        ENDCG
    }
    FallBack "Diffuse"
}