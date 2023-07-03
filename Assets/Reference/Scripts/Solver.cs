using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Reference
{

	// コンピュートシェーダのスレッド数を格納しておくための構造体．
	public struct GPUThreads
	{
		public int x;
		public int y;
		public int z;

		public GPUThreads(uint x, uint y, uint z)
		{
			this.x = (int)x;
			this.y = (int)y;
			this.z = (int)z;
		}
	}

	// DirectCompute 5.0 の仕様．
	public static class DirectCompute5_0
	{
		public const int MAX_THREAD = 1024;
		public const int MAX_X = 1024;
		public const int MAX_Y = 1024;
		public const int MAX_Z = 64;
		public const int MAX_DISPATCH = 65535;
		public const int MAX_PROCESS = MAX_DISPATCH * MAX_THREAD;
	}


	public class Solver : MonoBehaviour
	{

		#region Variables

		protected GPUThreads gpuThreads;
		protected RenderTexture heatTex;		// レンダリング可能なテクスチャ．
		protected RenderTexture prevHeatTex;	// ダブルバッファ用にもう1枚用意．
		protected int width, height;			// スクリーンの幅と高さを格納．

		[SerializeField]
		protected ComputeShader computeShader;

		[SerializeField, Range(0.1f, 500f)]
		protected float thermalDiffuseCoef = 500f;	// 熱拡散係数 

		[SerializeField, Range(0.1f, 200f)]
		protected float addingHeatIntensity = 10f;	// マウスがクリックされたときに加える熱の程度

		[SerializeField, Range(0.001f, 10f)]
		protected float deltaX = 5.0f;				// X 方向の格子点間隔

		[SerializeField, Range(0.001f, 10f)]
		protected float deltaY = 5.0f;				// Y 方向の格子点間隔

		[SerializeField]
		protected int iteration = 10;				// 1フレーム進めるごとに，熱拡散の計算を繰り返す回数

		[SerializeField]
		protected int lod = 0;
		public int getLod { get { return lod; } }

		[SerializeField] RenderTexture sourceTex;
		public RenderTexture SourceTex { set { sourceTex = value; } get { return sourceTex; } }

		#endregion


		// Start is called before the first frame update
		void Start()
        {
			Initialize();
		}

        // Update is called once per frame
        void Update()
        {
			// 温度更新に関する処理．
			// もしスクリーンのサイズが変更されていたら，コンピュートシェーダを初期化．
			if (width != Screen.width || height != Screen.height) InitializeComputeShader();

			// コンピュートシェーダに各変数を転送．
			computeShader.SetFloat("_ThermalDiffuseCoef", thermalDiffuseCoef);
			computeShader.SetFloat("_DeltaTime", 0.01f);
			computeShader.SetFloat("_AddingHeatIntensity", addingHeatIntensity);
			computeShader.SetFloat("_DeltaX", deltaX);
			computeShader.SetFloat("_DeltaY", deltaY);

			// iteration の回数だけ拡散方程式を計算．
			for (int i = 0; i < iteration; i++)
			{
				ThermalDiffuseStep();
			}

			// 描画に関する処理．
			// Sample.shader の _HeatTex に計算結果をセット．
			Shader.SetGlobalTexture("_HeatTex", heatTex);
			Shader.SetGlobalFloat("_AddingHeatIntensity", addingHeatIntensity);
		}

		protected virtual void Initialize()
		{
			uint threadX, threadY, threadZ;

			int id = -1;

			id = computeShader.FindKernel("AddSourceHeat");

			// threadのグループサイズを取得
			computeShader.GetKernelThreadGroupSizes(id, out threadX, out threadY, out threadZ);
			gpuThreads = new GPUThreads(threadX, threadY, threadZ);

			// thread数などの確認
			InitialCheck();

			InitializeComputeShader();
		}

		protected virtual void InitialCheck()
		{
			// グラフィックデバイスのシェーダーの性能レベル（読み取り専用）
			Assert.IsTrue(SystemInfo.graphicsShaderLevel >= 50, "Under the DirectCompute5.0 (DX11 GPU) doesn't work : ThermalDiffusion");
			// MAX_PROCESS = 65535(dispatch)*1024(thread), Max_x = 1024, Max_y = 1024, Max_z = 64
			Assert.IsTrue(gpuThreads.x * gpuThreads.y * gpuThreads.z <= DirectCompute5_0.MAX_PROCESS, "Resolution is too heigh : ThermalDiffusion");
			Assert.IsTrue(gpuThreads.x <= DirectCompute5_0.MAX_X, "THREAD_X is too large : ThermalDiffusion");
			Assert.IsTrue(gpuThreads.y <= DirectCompute5_0.MAX_Y, "THREAD_Y is too large : ThermalDiffusion");
			Assert.IsTrue(gpuThreads.z <= DirectCompute5_0.MAX_Z, "THREAD_Z is too large : ThermalDiffusion");
		}

		protected virtual void InitializeComputeShader()
		{
			width  = Screen.width;
			height = Screen.height;

			// lod で解像度を変更可能
			heatTex			= CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, heatTex);		// 16ビット浮動小数点
			prevHeatTex		= CreateRenderTexture(width >> lod, height >> lod, 0, RenderTextureFormat.RHalf, prevHeatTex);  // 16ビット浮動小数点

			// Sample.Shader 
			Shader.SetGlobalTexture("_HeatTex", heatTex);

			// ComputeShader
			computeShader.SetFloat("_ThermalDiffuseCoef", thermalDiffuseCoef);                
			computeShader.SetFloat("_DeltaTime", 0.01f);       
			computeShader.SetFloat("_AddingHeatIntensity", addingHeatIntensity);
		}

		#region ThermalDiffusion gpu kernel steps

		protected virtual void ThermalDiffuseStep()
		{
			int id = -1;

			// SourceTex 内にある情報をもとに heatTex, prevHeatTex に熱を追加 
			id = computeShader.FindKernel("AddSourceHeat");
			if (SourceTex != null)
			{
				computeShader.SetTexture(id, "_Source", SourceTex);
				computeShader.SetTexture(id, "_Heat", heatTex);
				computeShader.SetTexture(id, "_PrevHeat", prevHeatTex);
				computeShader.Dispatch(id, Mathf.CeilToInt(heatTex.width / (float)gpuThreads.x), Mathf.CeilToInt(heatTex.height / (float)gpuThreads.y), 1);
			}

			// 熱拡散の計算
			id = computeShader.FindKernel("DiffuseHeat");
			computeShader.SetTexture(id, "_Heat", heatTex);
			computeShader.SetTexture(id, "_PrevHeat", prevHeatTex);
			computeShader.Dispatch(id, Mathf.CeilToInt(heatTex.width / (float)gpuThreads.x), Mathf.CeilToInt(heatTex.height / (float)gpuThreads.y), 1);

			// 境界条件処理
			id = computeShader.FindKernel("SetBoundaryHeat");
			computeShader.SetTexture(id, "_Heat", heatTex);
			computeShader.Dispatch(id, Mathf.CeilToInt(heatTex.width / (float)gpuThreads.x), Mathf.CeilToInt(heatTex.height / (float)gpuThreads.y), 1);

			// 計算結果を prevHeatTexにコピー
			Graphics.Blit(heatTex, prevHeatTex);

		}

		#endregion

		#region render texture

		public RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTexture rt = null)
		{
			if (rt != null)
			{
				// RenderTextureサイズが画面サイズとあっていればそのまま
				if (rt.width == width && rt.height == height) return rt;
			}

			// rtがnullでないなら，Release して null に
			ReleaseRenderTexture(rt);
			// テクスチャを生成
			rt = new RenderTexture(width, height, depth, format);
			rt.enableRandomWrite = true;
			rt.wrapMode = TextureWrapMode.Clamp;
			rt.filterMode = FilterMode.Point;
			rt.Create();
			//MEMO: Color.clear = (0, 0, 0, 0)
			ClearRenderTexture(rt, Color.clear);
			return rt;
		}

		// RenderTextureの初期化
		public void ClearRenderTexture(RenderTexture target, Color bg)
		{
			// 現在のアクティブなRenderTextureをキャッシュ (型：RenderTexture)
			var active = RenderTexture.active;

			// Pixel情報を読み込むために target をアクティブに指定
			RenderTexture.active = target;

			// target のレンダリングバッファをクリア
			GL.Clear(true, true, bg);

			// キャッシュしていた render texture を再びアクティブに
			RenderTexture.active = active;
		}

        #endregion

		#region release

		public void ReleaseRenderTexture(RenderTexture rt)
		{
			if (rt == null) return;

			rt.Release();
			Destroy(rt);
		}

		void CleanUp()
		{
			ReleaseRenderTexture(heatTex);
			ReleaseRenderTexture(prevHeatTex);

			#if UNITY_EDITOR
            UnityEngine.Debug.Log("Buffer released");
			#endif
		}

		#endregion
	}

}
