// 在 UnlitColorTransparent 的基础上加**边缘高光**（Fresnel 式）：正对镜头的面不亮，
// 法线越偏离视线（越接近轮廓）越亮，形成一圈勾边辉光。用于 Mesh 冰 —— 半透冰身 + 亮边很有玻璃感。
//
// 与 CrowdMatch/Unlit/Color (Transparent) 的差别只有两点：
//   1. vert 额外传出**世界法线**与**世界视线方向**；
//   2. frag 把 _RimColor **相加**到底色上（不动 alpha）。
// 其余（Transparent 队列 / Blend / ZWrite Off / Cull 下拉 / 不采样贴图）完全一致，两者的材质可以互换。
//
// 【用到 Mesh 的法线，所以生成 Mesh 时必须 RecalculateNormals】
// 本项目的 IceMeshBuilder 调了，但它的法线是**按面**的（每个面自己的顶点、没有平滑）——
// 于是高光按面出现而不是连续渐变。对一个棱柱来说恰好是想要的效果：侧面勾边、顶面不亮。
//
// 高光**不乘 alpha**：冰很透的时候仍然留一圈亮边。
// 若想让高光跟着透明度一起淡出，把 frag 里那行改成：c.rgb += _RimColor.rgb * rim * _Color.a;
Shader "CrowdMatch/Unlit/Color (Transparent + Rim)"
{
    Properties
    {
        _Color ("Main Color", Color) = (0.6,0.85,1,0.5)
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1, 16)) = 3     // 越大，亮边越细窄、越贴轮廓
        _RimIntensity ("Rim Intensity", Range(0, 8)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2   // 0 = Off，1 = Front，2 = Back
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull [_Cull]
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _RimColor;
            float _RimPower;
            float _RimIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldViewDir : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldViewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(i.worldViewDir);
                float rim = 1.0 - saturate(dot(n, v));   // 正对视线 = 0，擦着视线 = 1
                rim = pow(rim, _RimPower) * _RimIntensity;

                fixed4 c = _Color;
                c.rgb += _RimColor.rgb * rim;
                return c;
            }
            ENDCG
        }
    }

    Fallback Off
}
