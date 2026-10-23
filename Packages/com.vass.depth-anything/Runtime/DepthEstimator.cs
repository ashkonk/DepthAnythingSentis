using System;
using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;

namespace DepthAnything
{
    /// <summary>Owns a GPU worker and its textures. Call Process once per frame on Unity's main thread.</summary>
    public sealed class DepthEstimator : IDisposable
    {
        public enum Quality { Fast, Balanced, Detail }
        public const int MaxStepsPerTick = 192;
        public const int MaxPatchColumns = 66;
        const int ColdSteps = 128;
        static readonly int[] PatchRows = { 22, 28, 37 };
        readonly Model _model;
        readonly Material _material;
        Worker _worker;
        Tensor<float> _input;
        RenderTexture _prepared, _output;
        IEnumerator _run;
        Texture _source;
        Vector2Int _size;
        long _sourceFrame = -1;
        int _steps, _budget = ColdSteps;
        bool _ready, _disposed;

        /// <summary>Borrowed RHalf texture. Larger values mean nearer; values are not meters or normalized.</summary>
        public RenderTexture DepthTexture => _ready ? _output : null;
        public int Revision { get; private set; }
        public bool StartedRun { get; private set; }
        public bool IsRunning => _run != null;
        public bool HasError { get; private set; }
        public int ScheduledStepsLastTick { get; private set; }
        public long InputFrameVersion => _sourceFrame;
        public string Status { get; private set; } = "Waiting for an image.";

        /// <summary>Uses the bundled, pinned ONNX V2 Small export and preprocessing shader.</summary>
        public DepthEstimator() : this(Resources.Load<ModelAsset>("DepthAnything/DepthAnythingV2Small")) { }

        /// <summary>The supplied model must have the same input/output contract as the bundled export.</summary>
        public DepthEstimator(ModelAsset modelAsset)
        {
            if (modelAsset == null) throw new ArgumentNullException(nameof(modelAsset), "Depth Anything V2 Small model is missing.");
            if (!SystemInfo.supportsComputeShaders) throw new NotSupportedException("Depth Anything requires compute shaders.");
            var shader = Resources.Load<Shader>("DepthAnything/DepthPrep");
            if (shader == null || !shader.isSupported) throw new NotSupportedException("Depth Anything preprocessing shader is unavailable.");
            _model = ModelLoader.Load(modelAsset);
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        /// <summary>
        /// Advances in-flight inference, then starts another only for a fresh producer frame.
        /// Pass an increasing frame version for video. -1 uses Texture2D.updateCount, or continuously
        /// processes other texture types. Keep ticking even if the source frame has not changed.
        /// A null source pauses work. Replacing the source or changing its grid cancels stale work.
        /// </summary>
        public void Process(Texture source, Quality quality = Quality.Fast, long sourceFrame = -1)
        {
            ThrowIfDisposed();
            StartedRun = false;
            ScheduledStepsLastTick = 0;
            if (source == null || HasError) return;
            try
            {
                var size = InputSize(source.width, source.height, quality);
                if (_worker == null || _size != size || _source != source) Allocate(size);
                long version = sourceFrame >= 0 ? sourceFrame : source is Texture2D ? source.updateCount : -1;
                if (_run == null)
                {
                    if (_ready && version >= 0 && _sourceFrame == version) return;
                    _source = source;
                    _sourceFrame = version;
                    _material.SetFloat("_Linearised", QualitySettings.activeColorSpace == ColorSpace.Linear && source.isDataSRGB ? 1 : 0);
                    Graphics.Blit(source, _prepared, _material);
                    TextureConverter.ToTensor(_prepared, _input, new TextureTransform());
                    _run = _worker.ScheduleIterable(_input);
                    _steps = 0;
                    StartedRun = true;
                }
                for (int remaining = _budget; remaining > 0; remaining--)
                {
                    if (!_run.MoveNext()) { Finish(); break; }
                    _steps++;
                    ScheduledStepsLastTick++;
                }
            }
            catch (Exception exception)
            {
                HasError = true;
                Status = "Depth estimation failed: " + exception.Message;
                Release();
                Debug.LogError(Status);
            }
        }

        /// <summary>Pixel dimensions, aligned to 14-pixel patches and bounded for extreme input aspects.</summary>
        public static Vector2Int InputSize(int width, int height, Quality quality)
        {
            int rows = PatchRows[Mathf.Clamp((int)quality, 0, PatchRows.Length - 1)];
            float aspect = Mathf.Max(1, width) / (float)Mathf.Max(1, height);
            int columns = Patches(rows * aspect);
            if (columns > MaxPatchColumns)
            {
                columns = MaxPatchColumns;
                rows = Patches(columns / aspect);
            }
            return new Vector2Int(columns * 14, rows * 14);
        }

        // The pinned export on Inference Engine 2.6.1 has a positional-embedding
        // resize mismatch at 24/48 patches. Revalidate before changing this workaround.
        static int Patches(float exact)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(exact));
            return n % 24 == 0 ? n + 1 : n;
        }

        void Allocate(Vector2Int size)
        {
            Release();
            _size = size;
            _worker = new Worker(_model, BackendType.GPUCompute);
            _input = new Tensor<float>(new TensorShape(1, 3, size.y, size.x));
            _prepared = GpuTexture.Create(size.x, size.y, RenderTextureFormat.ARGBHalf, "Depth input");
            _output = GpuTexture.Create(size.x, size.y, RenderTextureFormat.RHalf, "Relative inverse depth", randomWrite: true);
        }

        void Finish()
        {
            _run = null;
            _budget = Mathf.Clamp(Mathf.CeilToInt(_steps / 3f), 1, MaxStepsPerTick);
            var output = _worker.PeekOutput() as Tensor<float>;
            if (output == null || output.shape.rank != 3) throw new InvalidOperationException("Expected a float [1, height, width] output.");
            var original = output.shape;
            try
            {
                output.Reshape(new TensorShape(1, 1, original[-2], original[-1]));
                TextureConverter.RenderToTexture(output, _output, new TextureTransform());
            }
            finally { output.Reshape(original); }
            _ready = true;
            Revision++;
            Status = $"Depth ready: {_size.x} x {_size.y}";
        }

        /// <summary>Clears output and any failure, allowing a new inference attempt.</summary>
        public void Reset()
        {
            ThrowIfDisposed();
            Release();
            HasError = StartedRun = false;
            ScheduledStepsLastTick = 0;
            Status = "Waiting for an image.";
        }

        void Release()
        {
            _run = null;
            _ready = false;
            _budget = ColdSteps;
            _source = null;
            _sourceFrame = -1;
            _worker?.Dispose(); _worker = null;
            _input?.Dispose(); _input = null;
            GpuTexture.Release(ref _prepared);
            GpuTexture.Release(ref _output);
        }

        void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DepthEstimator));
        }

        public void Dispose()
        {
            if (_disposed) return;
            Release();
            GpuTexture.Destroy(_material);
            _disposed = true;
        }
    }
}
