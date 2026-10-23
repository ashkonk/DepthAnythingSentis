using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

namespace DepthAnything.Samples
{
    /// <summary>Small image/video/webcam demo. The estimator itself has no scene or UI dependency.</summary>
    public sealed class DepthDemo : MonoBehaviour
    {
        public Texture2D sampleImage;
        public Shader displayShader;
        public Shader reliefShader;
        public Shader overviewShader;
        public DepthEstimator.Quality quality = DepthEstimator.Quality.Balanced;
        public VideoPlayer videoPlayer;
        public bool useWebcam;
        public int webcamIndex;
        public bool showInterface = true;
        [Range(0, 2)] public float reliefStrength = .6f;
        [Range(-45, 45)] public float orbit = -15;

        public Texture Source { get; private set; }
        public long SourceFrame { get; private set; }
        public DepthEstimator Estimator { get; private set; }
        public RenderTexture ProcessedDepth => ExternalProcessing ? _externalDepth : _post?.Texture;
        public bool ExternalProcessing { get; set; }
        public string Error { get; private set; }
        public event Action<DepthDemo> DepthTick;

        DepthPostProcessor _post;
        WebCamTexture _webcam;
        Texture _lastSource;
        DepthEstimator.Quality _lastQuality;
        RenderTexture _externalDepth, _preview, _relief;
        Material _displayMaterial, _reliefMaterial, _overviewMaterial;
        Mesh _grid;
        long _webcamFrame;
        bool _requestedWebcam, _webcamReady;
        GUIStyle _title, _label, _small;

        void OnEnable()
        {
            try
            {
                Error = null;
                Estimator = new DepthEstimator();
                _post = new DepthPostProcessor();
                _displayMaterial = new Material(displayShader) { hideFlags = HideFlags.HideAndDontSave };
                _reliefMaterial = new Material(reliefShader) { hideFlags = HideFlags.HideAndDontSave };
                _overviewMaterial = new Material(overviewShader) { hideFlags = HideFlags.HideAndDontSave };
                _grid = CreateGrid(192, 128);
                _preview = NewTarget("Depth preview", 768, 512);
                _relief = NewTarget("Relief preview", 768, 512);
            }
            catch (Exception e) { Error = e.Message; Debug.LogError("Depth demo: " + Error, this); }
        }

        void Update()
        {
            if (Estimator == null || Error != null) return;
            if (useWebcam != _requestedWebcam)
            {
                _requestedWebcam = useWebcam;
                if (useWebcam) StartCoroutine(StartWebcam());
                else StopWebcam();
            }
            Source = sampleImage;
            SourceFrame = sampleImage != null ? sampleImage.updateCount : 0;
            if (videoPlayer != null && videoPlayer.isPrepared && videoPlayer.texture != null)
            { Source = videoPlayer.texture; SourceFrame = videoPlayer.frame; }
            if (useWebcam && _webcam != null && _webcam.width > 16)
            {
                if (_webcam.didUpdateThisFrame) _webcamFrame++;
                Source = _webcam; SourceFrame = _webcamFrame;
            }
            if (Source == null) return;
            if (Source != _lastSource || quality != _lastQuality)
            {
                _post.ResetHistory(); _externalDepth = null;
                _lastSource = Source; _lastQuality = quality;
            }
            Estimator.Process(Source, quality, SourceFrame);
            DepthTick?.Invoke(this);
            if (!ExternalProcessing && Estimator.DepthTexture != null)
                _post.Process(Estimator.DepthTexture, deltaTime: Time.unscaledDeltaTime);
            if (ProcessedDepth != null) RenderPreviews();
        }

        public void SetExternalDepth(RenderTexture texture) { _externalDepth = texture; }

        public void RenderPreviews()
        {
            _displayMaterial.SetFloat("_Mode", 0);
            Graphics.Blit(ProcessedDepth, _preview, _displayMaterial);
            _reliefMaterial.SetTexture("_MainTex", Source);
            _reliefMaterial.SetTexture("_DepthTex", ProcessedDepth);
            _reliefMaterial.SetFloat("_Strength", reliefStrength);
            _reliefMaterial.SetFloat("_Aspect", Source.width / (float)Source.height);
            _reliefMaterial.SetFloat("_CutEdges", ExternalProcessing ? 0 : 1);
            var view = Matrix4x4.TRS(new Vector3(0, 0, -4.2f), Quaternion.identity, new Vector3(1, 1, -1));
            // This manually submitted mesh uses bottom-left UVs, without a camera's automatic target flip.
            var projection = GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(34, 1.5f, .1f, 20), false);
            using (var commands = new CommandBuffer { name = "Depth relief preview" })
            {
                commands.SetRenderTarget(_relief);
                commands.ClearRenderTarget(true, true, new Color(.025f, .045f, .075f, 1));
                commands.SetViewProjectionMatrices(view, projection);
                commands.DrawMesh(_grid, Matrix4x4.Rotate(Quaternion.Euler(0, orbit, 0)), _reliefMaterial);
                Graphics.ExecuteCommandBuffer(commands);
            }
        }

        public RenderTexture DepthPreview => _preview;
        public RenderTexture ReliefPreview => _relief;

        /// <summary>Renders the three actual demo outputs offscreen. Caller owns the returned texture.</summary>
        public RenderTexture CaptureOverview()
        {
            var target = NewTarget("Depth demo overview", 1800, 452);
            _overviewMaterial.SetTexture("_Input", Source);
            _overviewMaterial.SetTexture("_Depth", _preview);
            _overviewMaterial.SetTexture("_Relief", _relief);
            _overviewMaterial.SetFloat("_InputAspect", Source.width / (float)Source.height);
            Graphics.Blit(Texture2D.blackTexture, target, _overviewMaterial);
            return target;
        }

        IEnumerator StartWebcam()
        {
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!this || !isActiveAndEnabled || !useWebcam) yield break;
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) { Debug.LogWarning("Webcam permission was not granted."); useWebcam = false; yield break; }
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) { Debug.LogWarning("No webcam found; showing the sample image."); useWebcam = false; yield break; }
            StopWebcam();
            _webcam = new WebCamTexture(devices[Mathf.Clamp(webcamIndex, 0, devices.Length - 1)].name, 1280, 720, 30);
            _webcam.Play(); _webcamReady = true;
        }

        void StopWebcam()
        {
            if (_webcam != null) { _webcam.Stop(); Destroy(_webcam); _webcam = null; }
            _webcamReady = false;
        }

        void OnGUI()
        {
            if (!showInterface) return;
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
                _label = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = new Color(.72f, .85f, .89f) } };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(.57f, .68f, .72f) } };
            }
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
            float margin = 32, gap = 18, width = (Screen.width - margin * 2 - gap * 2) / 3;
            float height = width / 1.5f, top = Mathf.Max(125, (Screen.height - height) * .43f);
            GUI.Label(new Rect(margin, 25, Screen.width - 64, 45), "Depth Anything V2 / Unity", _title);
            GUI.Label(new Rect(margin, 75, Screen.width - 64, 25), ExternalProcessing ? "BODYPIX  /  PERSON DEPTH" : "ONE IMAGE  /  RELATIVE DEPTH  /  GPU RELIEF", _label);
            DrawPanel(new Rect(margin, top, width, height), Source, "01  INPUT");
            DrawPanel(new Rect(margin + width + gap, top, width, height), _preview, "02  DEPTH  ·  BRIGHTER IS NEARER");
            DrawPanel(new Rect(margin + 2 * (width + gap), top, width, height), _relief, "03  DISPLACED MESH");
            float y = top + height + 55;
            GUI.Label(new Rect(margin, y, 115, 25), "QUALITY", _small);
            quality = (DepthEstimator.Quality)GUI.Toolbar(new Rect(margin + 105, y, 280, 25), (int)quality, new[] { "Fast", "Balanced", "Detail" });
            useWebcam = GUI.Toggle(new Rect(margin + 410, y, 170, 25), useWebcam, _webcamReady ? "Webcam active" : "Use webcam");
            GUI.Label(new Rect(margin, y + 42, 100, 25), "RELIEF", _small);
            reliefStrength = GUI.HorizontalSlider(new Rect(margin + 105, y + 48, 280, 20), reliefStrength, 0, 2);
            GUI.Label(new Rect(margin + 410, y + 42, 75, 25), "ORBIT", _small);
            orbit = GUI.HorizontalSlider(new Rect(margin + 485, y + 48, 230, 20), orbit, -45, 45);
            GUI.Label(new Rect(margin, Screen.height - 60, Screen.width - 64, 25), Error ?? Estimator?.Status ?? "Initializing...", _label);
            GUI.Label(new Rect(margin, Screen.height - 34, Screen.width - 64, 25), "Relative depth, not meters. Inference updates independently of rendering. One view cannot recover hidden surfaces.", _small);
        }

        void DrawPanel(Rect area, Texture image, string caption)
        {
            if (image != null) GUI.DrawTexture(area, image, ScaleMode.ScaleToFit, false);
            GUI.Label(new Rect(area.x, area.yMax + 12, area.width, 28), caption, _small);
        }

        static RenderTexture NewTarget(string label, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = label, hideFlags = HideFlags.HideAndDontSave };
            rt.Create(); return rt;
        }

        static Mesh CreateGrid(int columns, int rows)
        {
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];
            var indices = new int[columns * rows * 6];
            for (int y = 0; y <= rows; y++) for (int x = 0; x <= columns; x++)
            {
                int i = y * (columns + 1) + x;
                uv[i] = new Vector2(x / (float)columns, y / (float)rows);
                vertices[i] = new Vector3(uv[i].x * 2 - 1, uv[i].y * 2 - 1, 0);
            }
            int k = 0;
            for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
            {
                int a = y * (columns + 1) + x, b = a + columns + 1;
                indices[k++] = a; indices[k++] = b; indices[k++] = a + 1;
                indices[k++] = a + 1; indices[k++] = b; indices[k++] = b + 1;
            }
            var mesh = new Mesh { name = "Depth relief grid", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20);
            return mesh;
        }

        void OnDisable()
        {
            StopAllCoroutines(); StopWebcam(); _requestedWebcam = false;
            Estimator?.Dispose(); Estimator = null;
            _post?.Dispose(); _post = null; _externalDepth = null;
            Release(ref _preview); Release(ref _relief);
            if (_displayMaterial != null) Destroy(_displayMaterial);
            if (_reliefMaterial != null) Destroy(_reliefMaterial);
            if (_overviewMaterial != null) Destroy(_overviewMaterial);
            if (_grid != null) Destroy(_grid);
        }

        static void Release(ref RenderTexture rt) { if (rt == null) return; rt.Release(); Destroy(rt); rt = null; }
    }
}
