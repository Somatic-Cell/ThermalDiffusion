using UnityEngine;

namespace Reference
{
    public class RenderEffect : MonoBehaviour
    {
        public TextureEvent OnCreateTex;
        public RenderTexture Output { get; private set; }


        [SerializeField] Material[] effects;
        [SerializeField] bool show = true;
        [SerializeField] RenderTextureFormat format = RenderTextureFormat.ARGBFloat;
        [SerializeField] TextureWrapMode wrapMode;
        [SerializeField] int downSample = 0;

        // postEffect のために使用する配列
        RenderTexture[] rts = new RenderTexture[2];

        void Update()
        {
            // MEMO: Alpha6 は 6キー のキーコード
            if (Input.GetKeyDown(KeyCode.Alpha6))
                show = !show;
        }

        // すべてのレンダリングが RenderImage へと完了したときに呼び出される
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            CheckRTs(src);
            // マテリアルを媒介にしたテクスチャの変換処理を行う関数
            Graphics.Blit(src, rts[0]);

            // 複数のpostEffectを適用
            foreach (var mat in effects)
            {
                // rts[0]にmatを適用してrts[1]の出力テクスチャを生成
                Graphics.Blit(rts[0], rts[1], mat);
                // スワップ
                SwapRTs();
            }

            // rts[0]をOutputにコピー
            Graphics.Blit(rts[0], Output);
            if (show)
                //show が true なら Output を描画
                Graphics.Blit(Output, dst);
            else
                Graphics.Blit(src, dst);
        }

        void CheckRTs(RenderTexture src)
        {
            if (rts[0] == null || rts[0].width != src.width >> downSample || rts[0].height != src.height >> downSample)
            {
                for (var i = 0; i < rts.Length; i++)
                {
                    var rt = rts[i];
                    rts[i] = RenderUtility.CreateRenderTexture(src.width >> downSample, src.height >> downSample, 16, format, wrapMode, FilterMode.Bilinear, rt);
                }
                Output = RenderUtility.CreateRenderTexture(src.width >> downSample, src.height >> downSample, 16, format, wrapMode, FilterMode.Bilinear, Output);
                OnCreateTex.Invoke(Output);
            }
        }

        void SwapRTs()
        {
            var tmp = rts[0];
            rts[0] = rts[1];
            rts[1] = tmp;
        }

        // ゲームオブジェクトが非アクティブになった場合，
		// もしくはゲームオブジェクトが削除される前に呼ばれる
        void OnDisable()
        {
            foreach (var rt in rts)
                RenderUtility.ReleaseRenderTexture(rt);
            RenderUtility.ReleaseRenderTexture(Output);
        }

        [System.Serializable]
        public class TextureEvent : UnityEngine.Events.UnityEvent<Texture> { }
    }
}

