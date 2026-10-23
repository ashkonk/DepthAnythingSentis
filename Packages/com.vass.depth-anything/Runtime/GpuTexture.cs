using UnityEngine;

namespace DepthAnything
{
    internal static class GpuTexture
    {
        internal static RenderTexture Create(int width, int height, RenderTextureFormat format, string name, bool mips = false, bool randomWrite = false)
        {
            var texture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear)
            {
                name = name, hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                useMipMap = mips, autoGenerateMips = false, enableRandomWrite = randomWrite
            };
            if (!texture.Create()) { Destroy(texture); throw new System.NotSupportedException($"Cannot create {format} render texture."); }
            return texture;
        }

        internal static void Release(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release(); Destroy(texture); texture = null;
        }

        internal static void Destroy(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
