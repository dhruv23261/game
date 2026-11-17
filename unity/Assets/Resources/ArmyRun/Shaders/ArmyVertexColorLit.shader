// Lit vertex-colour shader for the baked CC0 characters (horde zombies, Orc, Blue Demon, snowy pines).
// Colours live in the mesh, so every model is ONE draw call; GPU instancing + fog + shadows supported.
// _Color multiplies everything (used for the Master Tyrant's red tint and hit flashes).
Shader "ArmyRun/VertexColorLit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma multi_compile_instancing
        #pragma target 3.0

        struct Input { float4 color : COLOR; };

        half _Glossiness;
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            o.Albedo = IN.color.rgb * tint.rgb;
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
