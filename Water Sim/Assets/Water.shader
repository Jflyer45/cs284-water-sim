Shader "Custom/SimpleWater"
{
    Properties
    {
        _MainTex("Displacement/Height Map", 2D) = "white" {}
        _Tile("Tile Scale", Float) = 1.0
        _Displacement("Displacement Strength", Float) = 1.0
        _WaterColor("Water Color", Color) = (0,0.3,0.7,1)     // Basic blue
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // BELOW IS VERTEX SHADER
            v2f vert(appdata v)
            {
                v2f o;
                float height = tex2Dlod(_MainTex, float4(v.uv * _Tile, 0, 0)).r;
                v.vertex.y += height * _Displacement;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // BELOW IS FRAGMENT SHADER
            fixed4 frag(v2f i) : SV_Target
            {
                //TODO add Phong and advance lighting
                return _WaterColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}