Shader "VRSYS/InvertedSkyboxSphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaMultiplier ("Alpha", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType"="Transparent" }
        LOD 100
        Cull Front
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Off
        ZWrite On
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AlphaMultiplier;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                col.a *= _AlphaMultiplier;
                return col;
            }
            ENDCG
        }
    }
}
