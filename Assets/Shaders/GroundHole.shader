// 挖洞地面：在 stencil=1 的区域不渲染（被 HoleMask 挖出真实空洞，露出地下竖井与像素），
// 其余区域按原地面材质（纹理 + 颜色 + 透明裁剪）正常渲染。
// 透明方式与原材质一致：alpha test（clip）——Opaque/AlphaTest 队列下通过 _Cutoff 丢弃 alpha 低于阈值的像素，
// 实现硬边镂空（非半透明混合），因此保留深度写入、无透明排序问题。
Shader "CrowdMatch/GroundHole"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Stencil { Ref 1 Comp NotEqual }

        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/Diffuse"
}
