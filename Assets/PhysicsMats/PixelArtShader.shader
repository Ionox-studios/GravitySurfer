Shader "Custom/PixelArtShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _PixelSize ("Pixel Density (Higher = Smoother)", Float) = 64
        _SnapStrength ("Vertex Snap Strength", Range(0,1)) = 1
        [Toggle] _QuantizeUV ("Quantize Texture UVs", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PixelSize;
            fixed4 _Color;
            float _SnapStrength;
            float _QuantizeUV;

            v2f vert (appdata v)
            {
                v2f o;
                
                // 1. Convert to Clip Space (Standard 3D projection)
                o.vertex = UnityObjectToClipPos(v.vertex);

                // 2. Vertex Snapping (The "PS1/Sprite" Jitter)
                // We snap the vertex position to a grid defined by _PixelSize
                // This makes the 3D shape's silhouette look jagged/pixelated
                if (_SnapStrength > 0)
                {
                    float4 screenPos = ComputeScreenPos(o.vertex);
                    float2 resolution = float2(_PixelSize * (_ScreenParams.x / _ScreenParams.y), _PixelSize);
                    
                    // Snap the clip space coordinates
                    o.vertex.xy = floor(o.vertex.xy * resolution) / resolution;
                }

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 3. UV Quantization (Optional)
                // This makes the texture itself look blocky, even if the image is HD
                float2 uv = i.uv;
                if (_QuantizeUV > 0.5)
                {
                    float2 grid = float2(_PixelSize, _PixelSize);
                    uv = floor(uv * grid) / grid;
                }

                fixed4 col = tex2D(_MainTex, uv) * _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}