using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Course
{
    public class RenderUtility : MonoBehaviour
    {

        public static RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, TextureWrapMode wrapMode = TextureWrapMode.Repeat, FilterMode filterMode = FilterMode.Bilinear, RenderTexture rt = null)
        {
            if (rt != null)
            {
                if (rt.width == width && rt.height == height) return rt;
            }

            ReleaseRenderTexture(rt);
            rt = new RenderTexture(width, height, depth, format);
            rt.enableRandomWrite = true;
            rt.wrapMode = wrapMode;
            rt.filterMode = filterMode;
            rt.Create();
            ClearRenderTexture(rt, Color.clear);
            return rt;
        }

        public static void ReleaseRenderTexture(RenderTexture rt)
        {
            if (rt == null) return;

            rt.Release();
            Destroy(rt);
        }

        // RenderTextureの初期化
        public static void ClearRenderTexture(RenderTexture target, Color bg)
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
    }
}