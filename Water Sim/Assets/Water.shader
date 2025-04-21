Shader "Custom/SimpleWater"
{
    Properties
    {
        _MainTex("Displacement/Height Map", 2D) = "white" {}
        _Tile("Tile Scale", Float) = 1.0
        _Displacement("Displacement Strength", Float) = 1.0
        _WaterColor("Water Color", Color) = (0,0.3,0.7,1)     // Basic blue

        // Phong shading
        _AmbientStrength("Ambient Strength", Range(0, 5)) = 1.0
        _DiffuseStrength("Diffuse Strength", Range(0, 5)) = 1.0
        _SpecularStrength("Specular Strength", Range(0, 5)) = 1.0
        _Shininess("Shininess", Range(1, 256)) = 128

        // Fresnel
        _FresnelStrength("Fresnel Strength", Range(1, 10)) = 1
        _FresnelScale("Fresnel Scale", Range(0.1, 2)) = 0.2

        // Color slider (cause lighting was making water color look very diluted)
        _ColorStrength("Color Strength", Range(0.5, 5)) = 2

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Tile;
            float _Displacement;
            fixed4 _WaterColor;

            float _AmbientStrength;
            float _DiffuseStrength;
            float _SpecularStrength;
            float _Shininess;

            float _ColorStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            // BELOW IS VERTEX SHADER
            v2f vert(appdata v)
            {
                v2f o;
                float height = tex2Dlod(_MainTex, float4(v.uv * _Tile, 0, 0)).r;
                v.vertex.y += height * _Displacement;

                // Calculate normal from displacement map
                float2 du = float2(1.0 / 256.0, 0);
                float2 dv = float2(0, 1.0 / 256.0);

                float heightU1 = tex2Dlod(_MainTex, float4((v.uv + du) * _Tile, 0, 0)).r * _Displacement;
                float heightU2 = tex2Dlod(_MainTex, float4((v.uv - du) * _Tile, 0, 0)).r * _Displacement;
                float heightV1 = tex2Dlod(_MainTex, float4((v.uv + dv) * _Tile, 0, 0)).r * _Displacement;
                float heightV2 = tex2Dlod(_MainTex, float4((v.uv - dv) * _Tile, 0, 0)).r * _Displacement;

                // Normals
                float3 tangent = normalize(float3(2.0, (heightU1 - heightU2) * 15.0, 0));
                float3 bitangent = normalize(float3(0, (heightV1 - heightV2) * 5.0, 2.0));
                float3 calculatedNormal = normalize(cross(tangent, bitangent));
                o.normal = normalize(mul(unity_ObjectToWorld, float4(calculatedNormal, 0)).xyz);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _FresnelStrength;
            float _FresnelScale;
            fixed4 _DeepWaterColor;

            // BELOW IS FRAGMENT SHADER
            fixed4 frag(v2f i) : SV_Target
            {
                //TODO add Phong and advance lighting
                ///return _WaterColor;

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflectDir = reflect(-lightDir, i.normal);

                fixed4 ambient = _AmbientStrength;
                fixed4 diffuse = _DiffuseStrength * (max(dot(i.normal, lightDir), 0.0));
                fixed4 specular = _SpecularStrength * (pow(max(dot(viewDir, reflectDir), 0.0), _Shininess));

                float fresnel = _FresnelScale * pow(1.0 - max(0.0, dot(i.normal, viewDir)), _FresnelStrength);

                fixed4 color = (_WaterColor * _ColorStrength) * (0.8 + (ambient + diffuse) * 0.2);
                color += specular * fresnel * 0.2;
  

                return color;

            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}