Shader "ThermalDiffusion/Sample_ref"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
			};


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _HeatTex;
			float _AddingHeatIntensity;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = float4(1, 1, 1, 1);
				fixed heat = tex2D(_HeatTex, i.uv);

				// 様々なトランスファーファンクションがダウンロードできるサイト：
				// https://www.kennethmoreland.com/color-advice/
				
				// トランスファーファンクション (heat -> color) Black Body, 8 bit
				const float4 col_000 = float4(0.0, 0.0, 0.0, 1);
				const float4 col_001 = float4(0.2567618382302789, 0.08862237092250158, 0.06900234709883349, 1);
				const float4 col_010 = float4(0.502299529628274, 0.12275205976842546, 0.10654041357261984, 1);
				const float4 col_011 = float4(0.7353154662963063, 0.1982320329476474, 0.12428036101896534, 1);
				const float4 col_100 = float4(0.8771435867383445, 0.39490510462624345, 0.03816328606394868, 1);
				const float4 col_101 = float4(0.911232394909533, 0.631724377007152, 0.10048201891972874, 1);
				const float4 col_110 = float4(0.9072006655243174, 0.8550025783221541, 0.18879408728283467, 1);
				const float4 col_111 = float4(1.0, 1.0, 1.0, 1);

				// トランスファーファンクション(heat->color) Kindleman, 8 bit
				//const float4 col_000 = float4(0.0, 0.0, 0.0, 1);
				//const float4 col_001 = float4(0.1395570645131698, 0.02198279213822543, 0.4598402747775218, 1);
				//const float4 col_010 = float4(0.028425818571614185, 0.24267452546380586, 0.5872810647948823, 1);
				//const float4 col_011 = float4(0.021455556526349926, 0.4492082077166399, 0.3816968956770979, 1);
				//const float4 col_100 = float4(0.03062583987948746, 0.6254558674785432, 0.0829129360827669, 1);
				//const float4 col_101 = float4(0.43953003388234246, 0.7674494579667936, 0.037212909121285366, 1);
				//const float4 col_110 = float4(0.9795488678271141, 0.8152887113235648, 0.5716520685975728, 1);
				//const float4 col_111 = float4(1.0, 1.0, 1.0, 1);

				//// トランスファーファンクション (heat -> color) Inferno, 8 bit
				//const float4 col_000 = float4(0.0014619955811715805, 0.0004659913919114934, 0.013866005775115809, 1);
				//const float4 col_001 = float4(0.15878054505364145, 0.04414588479176828, 0.32873705502988054, 1);
				//const float4 col_010 = float4(0.396786518835543, 0.08292103408227261, 0.4331726873798219, 1);
				//const float4 col_011 = float4(0.6234475076301247, 0.16486328557646127, 0.3880663468322876, 1);
				//const float4 col_100 = float4(0.8308925639196657, 0.28265548598550927, 0.2586364361687295, 1);
				//const float4 col_101 = float4(0.9615932007416385, 0.4896799300459282, 0.08356448400711391, 1);
				//const float4 col_110 = float4(0.9816315597243276, 0.7558372599499625, 0.15291300331162103, 1);
				//const float4 col_111 = float4(0.9883620799212208, 0.9983616470620554, 0.6449240982803861, 1);

				float t = heat / _AddingHeatIntensity;
				float tmp = 0;

				// Inferno に従って色を計算
				if (t > 1) {
					col *= col_111;
				}
				else if (t > 0.857142857) {
					tmp = (t - 0.857142857) * 7; // tmp を 0 から 1 に正規化
					col *= lerp(col_110, col_111, tmp);
				}
				else if (t > 0.714285714) {
					tmp = (t - 0.714285714) * 7;
					col *= lerp(col_101, col_110, tmp);
				}
				else if (t > 0.571428571) {
					tmp = (t - 0.571428571) * 7;
					col *= lerp(col_100, col_101, tmp);
				}
				else if (t > 0.428571429) {
					tmp = (t - 0.428571429) * 7;
					col *= lerp(col_011, col_100, tmp);
				}
				else if (t > 0.285714286) {
					tmp = (t - 0.285714286) * 7;
					col *= lerp(col_010, col_011, tmp);
				}
				else if (t > 0.142857143) {
					tmp = (t - 0.142857143) * 7;
					col *= lerp(col_001, col_010, tmp);
				}
				else if (t >= 0) {
					tmp = t * 7;
					col *= lerp(col_000, col_001, tmp);
				}
				else {
					col *= col_000;
				}
				return col;
			}
			ENDCG
		}
	}
}
