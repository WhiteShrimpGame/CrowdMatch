// 洞蒙版：只写 stencil（Ref 1），不写颜色/深度。配合 GroundHole 在地面上挖出真实空洞。
// Queue=Background 保证先于地面渲染，ZTest Always + ZWrite Off 保证无论深度都写入 stencil。
Shader "CrowdMatch/HoleMask"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
        ColorMask 0
        ZWrite Off
        ZTest Always
        Stencil { Ref 1 Comp Always Pass Replace }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
