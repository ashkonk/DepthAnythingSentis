using System;
using UnityEngine;

namespace DepthAnything
{
    /// <summary>Optional GPU normalization and mask-aware edge cleanup, independent of the segmentation provider.</summary>
    public sealed class DepthPostProcessor : IDisposable
    {
        readonly Material _material;
        RenderTexture _stats, _rangeA, _rangeB, _fillA, _fillB, _output;
        bool _hasRange, _disposed;
        public RenderTexture Texture => _output;

        public DepthPostProcessor()
        {
            var shader = Resources.Load<Shader>("DepthAnything/DepthPost");
            if (shader == null || !shader.isSupported) throw new NotSupportedException("Depth postprocessing shader is unavailable.");
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        /// <summary>
        /// Output R is 0..1 (near = 1), G is current mask coverage. Without masks, G is 1.
        /// estimateMask must be the mask captured when this depth estimate began; currentMask
        /// may be newer. All inputs use the same UV framing. Output is owned by this processor.
        /// Smoothing affects the normalization range, not motion or per-pixel depth consistency.
        /// </summary>
        public void Process(Texture depth, Texture currentMask = null, Texture estimateMask = null, float deltaTime = 1f / 60f, float smoothingSeconds = 0.2f)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DepthPostProcessor));
            if (depth == null) return;
            if (_output == null || _output.width != depth.width || _output.height != depth.height) Allocate(depth.width, depth.height);
            bool masked = currentMask != null;
            _material.SetTexture("_CurrentMask", masked ? currentMask : Texture2D.whiteTexture);
            _material.SetTexture("_EstimateMask", estimateMask != null ? estimateMask : masked ? currentMask : Texture2D.whiteTexture);
            _material.SetFloat("_Masked", masked ? 1 : 0);
            Graphics.Blit(depth, _stats, _material, 0);
            _stats.GenerateMips();
            _material.SetTexture("_Stats", _stats);
            _material.SetFloat("_LastMip", _stats.mipmapCount - 1);
            float retain = _hasRange && smoothingSeconds > 0 ? Mathf.Exp(-Mathf.Max(0, deltaTime) / smoothingSeconds) : 0;
            _material.SetFloat("_Retain", retain);
            Graphics.Blit(_rangeA, _rangeB, _material, 1);
            (_rangeA, _rangeB) = (_rangeB, _rangeA);
            _hasRange = true;
            _material.SetTexture("_Range", _rangeA);
            Texture filled = depth;
            if (masked)
            {
                Graphics.Blit(_stats, _fillA, _material, 2);
                for (int offset = 1; offset <= 64; offset *= 2)
                {
                    _material.SetFloat("_FillOffset", offset);
                    Graphics.Blit(_fillA, _fillB, _material, 3);
                    (_fillA, _fillB) = (_fillB, _fillA);
                }
                filled = _fillA;
            }
            Graphics.Blit(filled, _output, _material, 4);
        }

        /// <summary>Call on source replacement to avoid carrying the previous scene's range forward.</summary>
        public void ResetHistory() { _hasRange = false; }

        void Allocate(int width, int height)
        {
            Release();
            _stats = GpuTexture.Create(width, height, RenderTextureFormat.ARGBFloat, "Depth statistics", mips: true);
            _rangeA = GpuTexture.Create(1, 1, RenderTextureFormat.ARGBFloat, "Depth range A");
            _rangeB = GpuTexture.Create(1, 1, RenderTextureFormat.ARGBFloat, "Depth range B");
            Graphics.Blit(Texture2D.blackTexture, _rangeA);
            _fillA = GpuTexture.Create(width, height, RenderTextureFormat.RGFloat, "Depth fill A");
            _fillB = GpuTexture.Create(width, height, RenderTextureFormat.RGFloat, "Depth fill B");
            _output = GpuTexture.Create(width, height, RenderTextureFormat.RGHalf, "Normalized depth and mask");
        }

        void Release()
        {
            GpuTexture.Release(ref _stats); GpuTexture.Release(ref _rangeA); GpuTexture.Release(ref _rangeB);
            GpuTexture.Release(ref _fillA); GpuTexture.Release(ref _fillB); GpuTexture.Release(ref _output);
            _hasRange = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Release(); GpuTexture.Destroy(_material); _disposed = true;
        }
    }
}
