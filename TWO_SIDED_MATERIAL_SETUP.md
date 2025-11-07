# Two-Sided Material Setup for Wave Road

## Quick Setup (for Built-in Render Pipeline)

1. **Select your WaveRoad GameObject**
2. **In the MeshRenderer component**, click on the Material
3. **In the Inspector**, find the material settings
4. **Change Render Face** to **Both** (if available)

OR

**For Standard Shader:**
1. Select the material asset
2. Change shader to: `Standard` 
3. Under "Rendering Mode" dropdown, select `Fade` or `Transparent`
4. This usually enables two-sided rendering

## Better Option: Custom Two-Sided Shader

If you need proper two-sided rendering, here's a simple shader:

1. Create a new shader: Right-click in Assets > Create > Shader > Unlit Shader
2. Name it "TwoSidedWaveRoad"
3. Replace the contents with the shader code below
4. Create a material using this shader
5. Assign to your WaveRoad

### TwoSidedWaveRoad.shader
```shader
Shader "Custom/TwoSidedWaveRoad"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off // THIS IS THE KEY LINE - disables backface culling
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
```

## For URP (Universal Render Pipeline)

If you're using URP:
1. Create material with shader: `Universal Render Pipeline/Lit`
2. In material inspector, change **Render Face** to **Both**
3. Done!

## Quick Test

After setting up:
- Move your camera under the wave road
- You should now see the surface from below
- Waves should be visible from any angle
