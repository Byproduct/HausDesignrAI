Shader "Custom/SimpleShadowReceiver" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert addshadow

        struct Input {
            float4 color : COLOR;
        };

        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}