Shader "Custom/LightHalo2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HaloColor ("Halo Color", Color) = (1, 0.8, 0.4, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Radius ("Radius", Range(0.1, 10)) = 2
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _HaloColor;
            float _Intensity;
            float _Radius;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _HaloColor;
                o.color.a *= _Intensity;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 从中心到边缘的衰减
                float2 center = float2(0.5, 0.5);
                float distance = length(i.uv - center);
                float attenuation = 1.0 - smoothstep(0, _Radius * 0.5, distance * _Radius);
                
                fixed4 color = i.color;
                color.a *= attenuation;
                
                return color;
            }
            ENDCG
        }
    }
}