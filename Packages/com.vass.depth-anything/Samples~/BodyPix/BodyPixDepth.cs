using BodyPix;
using UnityEngine;

namespace DepthAnything.Samples
{
    /// <summary>Optional BodyPix 5 adapter. The core package never references BodyPix.</summary>
    [RequireComponent(typeof(DepthDemo))]
    public sealed class BodyPixDepth : MonoBehaviour
    {
        public ResourceSet resources;
        public Shader maskShader;
        [Range(128, 640)] public int resolution = 512;
        DepthDemo _demo;
        BodyDetector _detector;
        DepthPostProcessor _post;
        RenderTexture _currentMask, _pendingMask, _shownMask;
        Material _maskMaterial;
        Texture _source;
        ResourceSet _allocatedResources;
        Vector2Int _size;
        long _frame = -1;
        int _revision = -1;
        DepthEstimator.Quality _quality;

        void OnEnable()
        {
            _demo = GetComponent<DepthDemo>();
            if (resources == null || resources.model == null || maskShader == null)
            {
                Debug.LogError("BodyPix sample: assign the ResourceSet and mask extraction shader in the Inspector.", this);
                enabled = false; return;
            }
            _post = new DepthPostProcessor();
            _maskMaterial = new Material(maskShader) { hideFlags = HideFlags.HideAndDontSave };
            _demo.ExternalProcessing = true;
            _demo.DepthTick += Process;
        }

        void Process(DepthDemo demo)
        {
            if (demo.Source == null) return;
            int w = Mathf.Clamp(resolution, 128, 640);
            int h = Mathf.Clamp(Mathf.RoundToInt(w * demo.Source.height / (float)demo.Source.width), 64, 640);
            var size = new Vector2Int(w, h);
            if (_source != demo.Source || _size != size || _allocatedResources != resources || _quality != demo.quality)
            {
                ReleaseDetector();
                _source = demo.Source; _size = size; _quality = demo.quality; _allocatedResources = resources;
                _detector = new BodyDetector(resources, w, h);
                _detector.MaskTexture.filterMode = FilterMode.Bilinear;
                _post.ResetHistory(); _revision = -1; _frame = -1;
                demo.SetExternalDepth(null);
                // A still may already be cached when this adapter is enabled or reconfigured.
                // Start a matching depth run so the new mask snapshot can become visible.
                if (!demo.Estimator.StartedRun)
                {
                    demo.Estimator.Reset();
                    demo.Estimator.Process(demo.Source, demo.quality, demo.SourceFrame);
                }
            }
            if (_frame != demo.SourceFrame || demo.SourceFrame < 0)
            {
                _detector.ProcessImage(demo.Source);
                Ensure(ref _currentMask, w, h);
                // BodyPix 5 packs part labels in RGB and PERSON PROBABILITY IN ALPHA.
                // The generic depth postprocessor expects single-channel probability in red.
                Graphics.Blit(_detector.MaskTexture, _currentMask, _maskMaterial);
                _frame = demo.SourceFrame;
            }
            var mask = _currentMask;
            if (demo.Estimator.StartedRun)
            {
                Ensure(ref _pendingMask, mask.width, mask.height);
                Graphics.Blit(mask, _pendingMask);
            }
            if (_revision != demo.Estimator.Revision && demo.Estimator.DepthTexture != null)
            {
                _revision = demo.Estimator.Revision;
                (_pendingMask, _shownMask) = (_shownMask, _pendingMask);
            }
            if (demo.Estimator.DepthTexture == null || _shownMask == null) return;
            _post.Process(demo.Estimator.DepthTexture, mask, _shownMask, Time.unscaledDeltaTime);
            demo.SetExternalDepth(_post.Texture);
        }

        static void Ensure(ref RenderTexture texture, int width, int height)
        {
            if (texture != null && texture.width == width && texture.height == height) return;
            Release(ref texture);
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear)
            { name = "Depth estimate mask snapshot", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.Create();
        }

        void ReleaseDetector()
        {
            _detector?.Dispose(); _detector = null;
            Release(ref _currentMask);
            Release(ref _pendingMask); Release(ref _shownMask);
        }

        void OnDisable()
        {
            if (_demo != null)
            {
                _demo.DepthTick -= Process;
                _demo.ExternalProcessing = false;
                _demo.SetExternalDepth(null);
            }
            ReleaseDetector(); _post?.Dispose(); _post = null; _source = null;
            if (_maskMaterial != null) Destroy(_maskMaterial); _maskMaterial = null;
        }

        static void Release(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release(); Destroy(texture); texture = null;
        }
    }
}
