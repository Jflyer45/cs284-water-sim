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
            // To adjust the color of the background (refraction and fog), we have to retrieve it with GrabPass
            // Just before the water gets drawn, what's rendered up to this points gets copied to a grab-pass texture (_WaterBackground).
            // It is then sent _WaterBackground to LookingThroughWater.cginc
            GrabPass { "_WaterBackground" }
            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            // We need to add addshadow so Unity knows it needs to create a separate shadow caster pass for our shader that also uses our vertex displacement function
            #pragma surface surf Standard alpha finalcolor:ResetAlphaAtEnd vertex:vert addshadow

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            // LookingThroughWater.cginc will return color of the fragment behind water. We provide:
            // 1. fragments screen position for function to be able to get depth texture
            // 2. tangent-space normal vector (XY coordinates) for the UV offset to wiggle and creates fake refraction
            // At output we receive RGB which we will apply to Emission
            #include "LookingThroughWater.cginc"

            // Skybox cubemap to calculate reflections
            samplerCUBE _CubeMap;
            // _MainTex is noise texure, _FlowMap is texture that contains 2D flow vectors with noise in A channel and speed in B channel
            // _DerivHeightMap contains the X derivative stored in the A channel and the Y derivative stored in the G channel. As a bonus, it also contains the original height map in its B channel
            // We would normally have normal map but since we need derivatives (that are calculated from normals) we can save on calculation by directly storing them to texture
            sampler2D _MainTex, _DerivHeightMap;
            // Reflection strength
            float _ReflectionStrength;

            struct Input
            {
                // UV coords, screen position for Refraction and Fog
                float2 uv_MainTex;
                float4 screenPos;
                // View direction and world reflection vector (world space direction of a mirror reflection using the vertex interpolated surface normal) for reflection
                float3 viewDir;
                float3 worldRefl;
                // INTERNAL_DATA is defined by the surface shader translator and it contains WorldReflectionVector
                INTERNAL_DATA
            };

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;

            void ResetAlphaAtEnd(Input IN, SurfaceOutputStandard o, inout fixed4 color) {
                // Since we already calculated how much is the background visible, we dont need to do it twice
                // Only thing we need to do is to reset alpha to 1
                color.a = 1;
            }

            float3 ScaleAndUnpackDerivative(float4 textureData) {
                // Unpack derivatives from channels A & G, and height from channel B
                float3 dh = textureData.agb;
                // Scale derivatives from 0 to 1 -> -1 to 1. Height isnt scaled because it does not have direction 
                dh.xy = dh.xy * 2 - 1;
                return dh;
            }
        
            void vert(inout appdata_full vertexData) {
                // Store vertex position in variable p
                float3 p = vertexData.vertex.xyz;
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);

                // The normal vector is the cross product of both tangent vectors.
                float3 normal = normalize(cross(binormal, tangent));
                
                // Apply calculations we have done on variable p to vertex position
                vertexData.vertex.xyz = p;
                // Change normals to fit waves we created
                vertexData.normal = normal;
            }

            void surf (Input IN, inout SurfaceOutputStandard o)
            {


                // // We need to blend two distortions because if we use single distortion it will be clear when loop ended (and resets to 0) which
                // // will indicate patterns in water. To avoid that we can use two distortions with fade effect to hide the transition from end to start. 
                // float3 uvwA = FlowUVW(IN.uv_MainTex, flow.xy, jump, _FlowOffset, _Tiling, time, false);
                // float3 uvwB = FlowUVW(IN.uv_MainTex, flow.xy, jump, _FlowOffset, _Tiling, time, true);

                // // We want to correctly scale the height of the waves with _HeightScale
                // // Second property (_HeightScaleModulated) gives effect of higher waves when there is strong flow, and lower waves when there is weak flow.
                // // The flow speed is equal to the length of the flow vector (length(flow.z)).
                // float height = length(flow.z) * _HeightScaleModulated + _HeightScale;

                // // We need derivatives for normal vector for each flow (A and B). First part is sampling derivatives from _DerivHeightMap
                // // and second part is scaling them with weight we got from FlowUVW function and with height scale.
                // float3 dhA = ScaleAndUnpackDerivative(tex2D(_DerivHeightMap, uvwA.xy)) * (uvwA.z * height);
                // float3 dhB = ScaleAndUnpackDerivative(tex2D(_DerivHeightMap, uvwB.xy)) * (uvwB.z * height);
                // // With normal vector we would use built in UnpackNormal function and even though result is almost identical our normals are cheaper to compute.
                // o.Normal = normalize(float3(-(dhA.xy + dhB.xy), 1));

                // // We need to sample noise texture twice with UV we got from FlowUVW function and multiply with weights to achieve smooth transition.
                // fixed4 textureA = tex2D(_MainTex, uvwA.xy) * uvwA.z;
                // fixed4 textureB = tex2D(_MainTex, uvwB.xy) * uvwB.z;

                // We get final RGB color by multiplying noise colors with original Color.
                fixed4 color = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = color.rgb;

                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = color.a;
                // Since the albedo is affected by lighting, we must add the underwater color and reflection to the surface lighting, 
                // which we can do by using it as the emissive color. We must modulate underwater fog by the water's alpha.
                o.Emission = ColorBelowWater(IN.screenPos, o.Normal * 20) * (1 - color.a)
                    // We cant use worldRefl declared in Input because it uses per vertex normal calculations and surf function uses per pixel calculations
                    // We need to use WorldReflectionVector function which returns reflection vector based on per-pixel normal map
                    // Then we sample CubeMap with that vector and get color which will fade as angle between IN.viewDir and o.Normal becomes smaller (camera above surface -> reflection = 0).
                    // _ReflectionStrength is used to control reflection strength
                    + texCUBE(_CubeMap, WorldReflectionVector(IN, o.Normal)).rgb * (1 - dot(IN.viewDir, o.Normal)) * (1 - _ReflectionStrength);
            }
            ENDCG
        }
    FallBack "Diffuse"
}

// /* 4/21/2025 Derivative of Acerola, gasgiant by proxy. Simplified and changed to integrate our features */

// Shader "Custom/Water" {
		
// 	Properties {
// 		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
//         [Range(0,1)] _Alpha("Transparency", Range(0,1)) = 1
// 	}

// 	CGINCLUDE
//         #define _TessellationEdgeLength 10

//         struct TessellationFactors {
//             float edge[3] : SV_TESSFACTOR;
//             float inside : SV_INSIDETESSFACTOR;
//         };

//         float CalculateTessellationFactor(float3 controlPoint0, float3 controlPoint1) {
//             float edgeLength = distance(controlPoint0, controlPoint1);
//             float3 edgeCenter = (controlPoint0 + controlPoint1) * 0.5;
//             float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);
            
//             float distanceFactor = pow(viewDistance * 0.5f, 1.2f);
//             return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * distanceFactor);
//         }

//         bool IsTriangleBelowClipPlane(float3 p0, float3 p1, float3 p2, int planeIndex, float clipBias) {
//             float4 plane = unity_CameraWorldClipPlanes[planeIndex];
//             return dot(float4(p0, 1), plane) < clipBias && 
//                    dot(float4(p1, 1), plane) < clipBias && 
//                    dot(float4(p2, 1), plane) < clipBias;
//         }

//         bool ShouldCullTriangle(float3 p0, float3 p1, float3 p2, float clipBias) {
//             return IsTriangleBelowClipPlane(p0, p1, p2, 0, clipBias) ||
//                    IsTriangleBelowClipPlane(p0, p1, p2, 1, clipBias) ||
//                    IsTriangleBelowClipPlane(p0, p1, p2, 2, clipBias) ||
//                    IsTriangleBelowClipPlane(p0, p1, p2, 3, clipBias);
//         }
//     ENDCG

// 	SubShader {
// 		Tags { 
//         "LightMode"    = "ForwardBase"
//         "Queue"        = "Transparent"
//         "RenderType"   = "Transparent"
//         }
//         ZWrite Off
//         Blend SrcAlpha OneMinusSrcAlpha

// 		Pass {

// 			CGPROGRAM

//             float _Alpha;

// 			#pragma vertex dummyvp
// 			#pragma hull hp
// 			#pragma domain dp 
// 			#pragma geometry gp
// 			#pragma fragment fp

// 			#include "UnityPBSLighting.cginc"
//             #include "AutoLight.cginc"

//             // Constants
// 			#define PI 3.14159265358979323846
//             #define WATER_REFRACTION_INDEX 1.33
//             #define WATER_BASE_REFLECTIVITY 0.02
//             #define CLIP_BIAS -50.0
//             #define FRESNEL_POWER 5.0
//             #define MIN_DOT_PRODUCT 0.001
//             #define MIN_NORMAL_DOT_HALF 0.0001
//             #define BECKMANN_THRESHOLD 1.6
//             #define FRESNEL_SCALE_FACTOR 22.7
            
// 			struct TessellationControlPoint {
//                 float4 vertex : INTERNALTESSPOS;
//                 float2 uv : TEXCOORD0;
//             };

// 			struct VertexData {
// 				float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
// 			};

// 			struct VertexToGeometry {
// 				float4 pos : SV_POSITION;
//                 float2 uv : TEXCOORD0;
// 				float3 worldPos : TEXCOORD1;
// 				float depth : TEXCOORD2;
// 			};
            
//             struct GeometryToFragment {
// 				VertexToGeometry data;
// 				float2 barycentricCoordinates : TEXCOORD9;
// 			};
            
//             // Shader Properties and Textures
// 			float3 _SunDirection;
// 			float _NormalStrength;
// 			samplerCUBE _EnvironmentMap;
// 			float _Roughness, _FoamRoughnessModifier;
// 			float _Tile0, _Tile1, _Tile2, _Tile3;
// 			float3 _SunIrradiance, _ScatterColor, _BubbleColor, _FoamColor;
// 			float _HeightModifier, _BubbleDensity;
// 			float _DisplacementDepthAttenuation, _FoamDepthAttenuation, _NormalDepthAttenuation;
// 			float _WavePeakScatterStrength, _ScatterStrength, _ScatterShadowStrength, _EnvironmentLightStrength;
// 			float _FoamSubtract;
// 			sampler2D _CameraDepthTexture;
//             UNITY_DECLARE_TEX2DARRAY(_DisplacementTextures);
//             UNITY_DECLARE_TEX2DARRAY(_SlopeTextures);
            
//             // Water Displacement and Vertex Processing
//             float3 SampleDisplacement(float3 worldPos) {
//                 float3 displacement1 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile0, 0), 0);
//                 float3 displacement2 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile1, 1), 0);
//                 float3 displacement3 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile2, 2), 0);
//                 float3 displacement4 = UNITY_SAMPLE_TEX2DARRAY_LOD(_DisplacementTextures, float3(worldPos.xz * _Tile3, 3), 0);
//                 return displacement1 + displacement2 + displacement3 + displacement4;
//             }
            
//             float2 SampleWaterSlopes(float2 uv) {
//                 float2 slopes1 = UNITY_SAMPLE_TEX2DARRAY(_SlopeTextures, float3(uv * _Tile0, 0));
//                 float2 slopes2 = UNITY_SAMPLE_TEX2DARRAY(_SlopeTextures, float3(uv * _Tile1, 1));
//                 float2 slopes3 = UNITY_SAMPLE_TEX2DARRAY(_SlopeTextures, float3(uv * _Tile2, 2));
//                 float2 slopes4 = UNITY_SAMPLE_TEX2DARRAY(_SlopeTextures, float3(uv * _Tile3, 3));
//                 return slopes1 + slopes2 + slopes3 + slopes4;
//             }
            
//             float4 SampleDisplacementAndFoam(float2 uv) {
//                 float4 displacementFoam1 = UNITY_SAMPLE_TEX2DARRAY(_DisplacementTextures, float3(uv * _Tile0, 0));
//                 float4 displacementFoam2 = UNITY_SAMPLE_TEX2DARRAY(_DisplacementTextures, float3(uv * _Tile1, 1));
//                 float4 displacementFoam3 = UNITY_SAMPLE_TEX2DARRAY(_DisplacementTextures, float3(uv * _Tile2, 2));
//                 float4 displacementFoam4 = UNITY_SAMPLE_TEX2DARRAY(_DisplacementTextures, float3(uv * _Tile3, 3));
//                 return displacementFoam1 + displacementFoam2 + displacementFoam3 + displacementFoam4 + _FoamSubtract;
//             }
            
// 			VertexToGeometry vp(VertexData v) {
// 				VertexToGeometry output;
// 				v.uv = 0;
//                 output.worldPos = mul(unity_ObjectToWorld, v.vertex);

//                 float3 displacement = SampleDisplacement(output.worldPos);

// 				float4 clipPos = UnityObjectToClipPos(v.vertex);
// 				float depth = 1 - Linear01Depth(clipPos.z / clipPos.w);

// 				displacement = lerp(0.0f, displacement, pow(saturate(depth), _DisplacementDepthAttenuation));

// 				v.vertex.xyz += mul(unity_WorldToObject, displacement.xyz);
				
//                 output.pos = UnityObjectToClipPos(v.vertex);
//                 output.uv = output.worldPos.xz;
//                 output.worldPos = mul(unity_ObjectToWorld, v.vertex);
// 				output.depth = depth;
// 				return output;
// 			}

// 			float CalculateSmithMaskingBeckmann(float3 halfVector, float3 surfaceVector, float roughness) {
// 				float hdots = max(MIN_DOT_PRODUCT, DotClamped(halfVector, surfaceVector));
// 				float a = hdots / (roughness * sqrt(1 - hdots * hdots));
// 				float a2 = a * a;

// 				return a < BECKMANN_THRESHOLD ? (1.0f - 1.259f * a + 0.396f * a2) / (3.535f * a + 2.181 * a2) : 0.0f;
// 			}

// 			float CalculateBeckmannDistribution(float normalDotHalf, float roughness) {
// 				float exp_arg = (normalDotHalf * normalDotHalf - 1) / (roughness * roughness * normalDotHalf * normalDotHalf);
// 				return exp(exp_arg) / (PI * roughness * roughness * normalDotHalf * normalDotHalf * normalDotHalf * normalDotHalf);
// 			}
            
//             float3 CalculateWaterNormal(float2 slopes, float depth) {
//                 float3 macroNormal = float3(0, 1, 0);
//                 float3 mesoNormal = normalize(float3(-slopes.x, 1.0f, -slopes.y));
//                 mesoNormal = normalize(lerp(macroNormal, mesoNormal, pow(saturate(depth), _NormalDepthAttenuation)));
//                 return normalize(UnityObjectToWorldNormal(normalize(mesoNormal)));
//             }
            
//             float CalculateWaterFresnel(float3 mesoNormal, float3 viewDir, float roughness) {

//                 float reflectivityRatio = ((WATER_REFRACTION_INDEX - 1) * (WATER_REFRACTION_INDEX - 1)) / ((WATER_REFRACTION_INDEX + 1) * (WATER_REFRACTION_INDEX + 1));
                                         
//                 float fresnelExponent = FRESNEL_POWER * exp(-2.69 * roughness);
//                 float fresnelNumerator = pow(1 - dot(mesoNormal, viewDir), fresnelExponent);
//                 float fresnelDenominator = 1.0f + FRESNEL_SCALE_FACTOR * pow(roughness, 1.5f);
                
//                 float fresnel = reflectivityRatio + (1 - reflectivityRatio) * fresnelNumerator / fresnelDenominator;
//                 return saturate(fresnel);
//             }
            
//             float3 CalculateWaterSpecular(float3 mesoNormal, float3 macroNormal, float3 lightDir, float3 viewDir, float3 halfwayDir, float roughness, float fresnel) {
//                 float normalDotHalf = max(MIN_NORMAL_DOT_HALF, dot(mesoNormal, halfwayDir));
//                 float normalDotLight = DotClamped(mesoNormal, lightDir);
                
//                 float viewMask = CalculateSmithMaskingBeckmann(halfwayDir, viewDir, roughness);
//                 float lightMask = CalculateSmithMaskingBeckmann(halfwayDir, lightDir, roughness);
//                 float geometryTerm = rcp(1 + viewMask + lightMask);
                
//                 float3 specular = _SunIrradiance * fresnel * geometryTerm * CalculateBeckmannDistribution(normalDotHalf, roughness);
                                 
//                 specular /= 4.0f * max(MIN_DOT_PRODUCT, DotClamped(macroNormal, lightDir));
//                 specular *= normalDotLight;
                
//                 return specular;
//             }
            
//             float3 CalculateWaterScatter(float3 mesoNormal, float3 viewDir, float3 lightDir, 
//                                         float waveHeight, float lightMask) {
//                 float wavePeakScatter = _WavePeakScatterStrength * waveHeight * pow(DotClamped(lightDir, -viewDir), 4.0f) * pow(0.5f - 0.5f * dot(lightDir, mesoNormal), 3.0f);
                                      
//                 float viewDirScatter = _ScatterStrength * pow(DotClamped(viewDir, mesoNormal), 2.0f);
//                 float shadowScatter = _ScatterShadowStrength * DotClamped(mesoNormal, lightDir);
//                 float bubbleScatterIntensity = _BubbleDensity;

//                 // Calculate final scatter color
//                 float3 scatter = (wavePeakScatter + viewDirScatter) * _ScatterColor * _SunIrradiance * rcp(1 + lightMask);
//                 scatter += shadowScatter * _ScatterColor * _SunIrradiance + bubbleScatterIntensity * _BubbleColor * _SunIrradiance;
                           
//                 return scatter;
//             }
            
// 			TessellationControlPoint dummyvp(VertexData v) {
// 				TessellationControlPoint p;
// 				p.vertex = v.vertex;
// 				p.uv = v.uv;
// 				return p;
// 			}
            
//             TessellationFactors PatchFunction(InputPatch<TessellationControlPoint, 3> patch) {
//                 float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex);
//                 float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex);
//                 float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex);

//                 TessellationFactors factors;
                
//                 if (ShouldCullTriangle(p0, p1, p2, CLIP_BIAS)) {
//                     factors.edge[0] = factors.edge[1] = factors.edge[2] = factors.inside = 0;
//                 } else {
//                     factors.edge[0] = CalculateTessellationFactor(p1, p2);
//                     factors.edge[1] = CalculateTessellationFactor(p2, p0);
//                     factors.edge[2] = CalculateTessellationFactor(p0, p1);
//                     factors.inside = (factors.edge[0] + factors.edge[1] + factors.edge[2]) / 3.0;
//                 }
//                 return factors;
//             }

//             [UNITY_domain("tri")]
//             [UNITY_outputcontrolpoints(3)]
//             [UNITY_outputtopology("triangle_cw")]
//             [UNITY_partitioning("integer")]
//             [UNITY_patchconstantfunc("PatchFunction")]
//             TessellationControlPoint hp(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OUTPUTCONTROLPOINTID) {
//                 return patch[id];
//             }

//             #define DP_INTERPOLATE(fieldName) data.fieldName = \
//                 patch[0].fieldName * barycentricCoordinates.x + \
//                 patch[1].fieldName * barycentricCoordinates.y + \
//                 patch[2].fieldName * barycentricCoordinates.z;               

//             [UNITY_domain("tri")]
//             VertexToGeometry dp(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DOMAINLOCATION) {
//                 VertexData data;
//                 DP_INTERPOLATE(vertex)
//                 DP_INTERPOLATE(uv)

//                 return vp(data);
//             }

//             [maxvertexcount(3)]
//             void gp(triangle VertexToGeometry input[3], inout TriangleStream<GeometryToFragment> stream) {
//                 GeometryToFragment g0, g1, g2;
//                 g0.data = input[0];
//                 g1.data = input[1];
//                 g2.data = input[2];

//                 g0.barycentricCoordinates = float2(1, 0);
//                 g1.barycentricCoordinates = float2(0, 1);
//                 g2.barycentricCoordinates = float2(0, 0);

//                 stream.Append(g0);
//                 stream.Append(g1);
//                 stream.Append(g2);
//             }
            
//             // Fragment Shader
// 			float4 fp(GeometryToFragment input) : SV_TARGET {

//                 float3 lightDir = -normalize(_SunDirection);
//                 float3 viewDir = normalize(_WorldSpaceCameraPos - input.data.worldPos);
//                 float3 halfwayDir = normalize(lightDir + viewDir);
// 				float depth = input.data.depth;
				
//                 float4 displacementFoam = SampleDisplacementAndFoam(input.data.uv);
// 				float2 slopes = SampleWaterSlopes(input.data.uv);
				
// 				// Apply normal strength and calculate foam
// 				slopes *= _NormalStrength;
// 				float foam = lerp(0.0f, saturate(displacementFoam.a), pow(depth, _FoamDepthAttenuation));

// 				// Calculate normals
// 				float3 macroNormal = float3(0, 1, 0);
//                 float3 mesoNormal = CalculateWaterNormal(slopes, depth);

// 				float adjustedRoughness = _Roughness + foam * _FoamRoughnessModifier;
//                 float lightMask = CalculateSmithMaskingBeckmann(halfwayDir, lightDir, adjustedRoughness);
				
//                 float fresnel = CalculateWaterFresnel(mesoNormal, viewDir, adjustedRoughness);
// 				float3 specular = CalculateWaterSpecular(mesoNormal, macroNormal, lightDir, viewDir, halfwayDir, adjustedRoughness, fresnel);
// 				float3 envReflection = texCUBE(_EnvironmentMap, reflect(-viewDir, mesoNormal)).rgb;
// 				envReflection *= _EnvironmentLightStrength;

// 				float waveHeight = max(0.0f, displacementFoam.y) * _HeightModifier;
//                 float3 scatter = CalculateWaterScatter(mesoNormal, viewDir, lightDir, waveHeight, lightMask);

// 				// Combine reflection, scatter, and foam
// 				float3 finalColor = (1 - fresnel) * scatter + specular + fresnel * envReflection;
// 				finalColor = max(0.0f, finalColor);
// 				finalColor = lerp(finalColor, _FoamColor, saturate(foam));
				
// 				return float4(finalColor, _Alpha);
// 			}

// 			ENDCG
// 		}
// 	}
// }