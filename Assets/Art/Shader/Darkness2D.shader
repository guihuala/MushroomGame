Shader "Custom/Darkness2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DarknessColor ("Darkness Color", Color) = (0,0,0,0.8)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 worldPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _DarknessColor;
            float4 _LightPositions[50];
            float _LightRadiuses[50];
            int _LightCount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 originalColor = tex2D(_MainTex, i.uv);
                float totalLight = 0;
                
                for (int j = 0; j < _LightCount; j++)
                {
                    float2 lightPos = _LightPositions[j].xy;
                    float distance = length(lightPos - i.worldPos);
                    float radius = _LightRadiuses[j];
                    
                    if (distance < radius)
                    {
                        float attenuation = 1.0 - (distance / radius);
                        attenuation = attenuation * attenuation;
                        totalLight = max(totalLight, attenuation);
                    }
                }
                
                totalLight = saturate(totalLight);
                return lerp(_DarknessColor, originalColor, totalLight);
            }
            ENDCG
        }
    }
}