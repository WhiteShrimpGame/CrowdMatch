// Unlit/Color 的**半透版本**：不参与光照、只有一个纯色，alpha 直接当不透明度用。
// 用于实时生成的 Mesh 冰（IceMeshBuilder），也适用于任何「只要颜色、要透」的东西。
//
// 与内置 Unlit/Color 的差别只在渲染状态：
//   · RenderType / Queue 走 Transparent，Blend SrcAlpha OneMinusSrcAlpha，ZWrite Off
//     —— 半透必须关掉深度写入，否则先画的冰面会把后面的像素挡住。
//   · 多一个 Cull 下拉：默认 Back（像玻璃壳，只看见朝你的那面）；
//     切成 Off 就是双面，能看见背面内壁，冰会更「实」一些（同一叠面会混合两次，颜色更浓）。
//
// **不采样贴图**：Mesh 冰不生成 UV，挂贴图只会永远取到 (0,0) 那一个像素 —— 所以这里干脆不给贴图属性。
// 也没有雾和阴影投射（与内置 Unlit/Color 一致；要阴影得另加 ShadowCaster Pass，半透物体通常不要）。
//
// alpha = 1 时就是一块纯色不透明物体（因为 ZTest 仍是 LEqual，与不透明几何的前后关系是对的）。
Shader "CrowdMatch/Unlit/Color (Transparent)"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
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
                return _Color;
            }
            ENDCG
        }
    }

    Fallback Off
}
