using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DepthAnything.Samples
{
    /// <summary>Opt-in standalone validation. Does nothing unless launched with -depthSmoke output-directory.</summary>
    public sealed class DemoSmoke : MonoBehaviour
    {
        readonly List<string> _results = new List<string>();
        string _directory;
        int _errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-depthSmoke");
            if (index < 0 || index + 1 >= args.Length) return;
            var runner = new GameObject("Depth smoke validation").AddComponent<DemoSmoke>();
            runner._directory = args[index + 1];
            Directory.CreateDirectory(runner._directory);
            runner.StartCoroutine(runner.RunGuarded());
        }

        IEnumerator RunGuarded()
        {
            var run = Run();
            while (true)
            {
                object current;
                try { if (!run.MoveNext()) yield break; current = run.Current; }
                catch (Exception exception) { Check(false, exception.ToString()); Finish(); yield break; }
                yield return current;
            }
        }

        IEnumerator Run()
        {
            Application.logMessageReceived += OnLog;
            var demo = FindAnyObjectByType<DepthDemo>();
            Check(demo != null, "Demo scene contains the presenter");
            if (demo == null) { Finish(); yield break; }
            bool expectBodyPix = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "BodyPixDemo";
            Check(demo.ExternalProcessing == expectBodyPix, "Scene uses its intended processing mode");
            for (int i = 0; i < 600 && demo.ProcessedDepth == null && demo.Error == null; i++) yield return null;
            Check(demo.Error == null && demo.Estimator != null && !demo.Estimator.HasError, "GPU model initialized without errors");
            Check(demo.ProcessedDepth != null, "Scene produces normalized depth");
            if (demo.ProcessedDepth == null) { Finish(); yield break; }
            for (int i = 0; i < 12; i++) yield return null;
            float sourceAspect = demo.Source.width / (float)demo.Source.height;
            Check(Mathf.Abs(demo.DepthPreview.width / (float)demo.DepthPreview.height - sourceAspect) < .01f,
                "Depth preview preserves the source aspect ratio");
            Check(Mathf.Abs(demo.ReliefPreview.width / (float)demo.ReliefPreview.height - sourceAspect) < .01f,
                "Relief preview preserves the source aspect ratio");
            var normalized = Read(demo.ProcessedDepth);
            var pixels = normalized.GetPixels();
            float min = 1, max = 0; int covered = 0;
            foreach (var pixel in pixels)
            {
                if (float.IsNaN(pixel.r) || float.IsInfinity(pixel.r)) { Check(false, "Finite normalized output"); break; }
                min = Mathf.Min(min, pixel.r); max = Mathf.Max(max, pixel.r);
                if (pixel.g > .5f) covered++;
            }
            Check(min >= -.001f && max <= 1.001f && max - min > .15f, $"Normalized image has useful range [{min:F3}, {max:F3}]");
            Check(demo.ExternalProcessing ? covered > pixels.Length * .03f && covered < pixels.Length * .35f : covered == pixels.Length,
                $"Mask coverage matches mode ({covered / (float)pixels.Length:P1})");
            if (demo.ExternalProcessing)
            {
                Check(normalized.GetPixelBilinear(.66f, .38f).g > .8f, "Person torso is selected in the waterfall photo");
                Check(normalized.GetPixelBilinear(.38f, .85f).g < .1f, "Distant waterfall is excluded in the waterfall photo");
            }
            Destroy(normalized);
            int revision = demo.Estimator.Revision;
            for (int i = 0; i < 12; i++) yield return null;
            Check(demo.Estimator.Revision == revision, "Unchanged still does not restart inference");
            Save(demo.DepthPreview, "depth.png"); Save(demo.ReliefPreview, "relief.png");
            var overview = demo.CaptureOverview();
            Save(overview, "demo.png"); overview.Release(); Destroy(overview);
            if (expectBodyPix)
            {
                var adapter = Array.Find(demo.GetComponents<MonoBehaviour>(), c => c.GetType().Name == "BodyPixDepth");
                Check(adapter != null, "Optional adapter is present");
                if (adapter != null)
                {
                    adapter.enabled = false;
                    yield return null;
                    Check(!demo.ExternalProcessing, "Disabling BodyPix restores the core presentation");
                    adapter.enabled = true;
                    for (int i = 0; i < 120 && demo.ProcessedDepth == null; i++) yield return null;
                    Check(demo.ExternalProcessing && demo.ProcessedDepth != null, "Re-enabling BodyPix reacquires a matching still-image mask and depth");
                }
            }
            if (!demo.ExternalProcessing)
            {
                demo.enabled = false;
                yield return null;
                using (var estimator = new DepthEstimator())
                {
                    foreach (DepthEstimator.Quality quality in Enum.GetValues(typeof(DepthEstimator.Quality)))
                    {
                        estimator.Reset(); int ticks = 0;
                        while (estimator.DepthTexture == null && !estimator.HasError && ticks < 120)
                        {
                            estimator.Process(demo.sampleImage, quality, 10); ticks++;
                            Check(estimator.ScheduledStepsLastTick <= DepthEstimator.MaxStepsPerTick, quality + " scheduling remains bounded");
                            yield return null;
                        }
                        Check(estimator.DepthTexture != null && ticks > 1, quality + " completes through multiple ticks");
                        int previous = estimator.Revision;
                        estimator.Process(demo.sampleImage, quality, 10);
                        Check(!estimator.StartedRun && estimator.Revision == previous, quality + " reuses a producer frame");
                        estimator.Process(demo.sampleImage, quality, 11);
                        Check(estimator.StartedRun, quality + " starts on a fresh producer frame");
                    }
                    estimator.Reset();
                    estimator.Process(demo.sampleImage, DepthEstimator.Quality.Fast, 1);
                    var replacement = new RenderTexture(320, 240, 0); replacement.Create(); Graphics.Blit(demo.sampleImage, replacement);
                    estimator.Process(replacement, DepthEstimator.Quality.Fast, 1);
                    Check(estimator.StartedRun && estimator.DepthTexture == null, "Source replacement cancels stale in-flight work");
                    estimator.Reset(); replacement.Release(); Destroy(replacement);
                    foreach (var size in new[] { new Vector2Int(16384, 128), new Vector2Int(128, 16384), new Vector2Int(1920, 1080) })
                    {
                        var input = DepthEstimator.InputSize(size.x, size.y, DepthEstimator.Quality.Detail);
                        Check(input.x <= 924 && input.y <= 518 && input.x % 14 == 0 && input.y % 14 == 0, "Input is bounded and patch aligned: " + size);
                    }
                    // Empty segmentation must never become a full-screen body.
                    using (var post = new DepthPostProcessor())
                    {
                        var depth = new Texture2D(32, 32, TextureFormat.RGBAFloat, false, true);
                        post.Process(depth, Texture2D.blackTexture);
                        var empty = Read(post.Texture);
                        Check(Array.TrueForAll(empty.GetPixels(), p => p.g == 0 && !float.IsNaN(p.r)), "Empty mask yields finite, empty output");
                        Destroy(empty); Destroy(depth);
                    }
                }
                demo.enabled = true;
                for (int i = 0; i < 120 && demo.ProcessedDepth == null; i++) yield return null;
                Check(demo.ProcessedDepth != null, "Disable and re-enable recreates resources");
            }
            Finish();
        }

        void Check(bool condition, string message)
        {
            _results.Add((condition ? "PASS: " : "FAIL: ") + message);
            if (!condition) _errors++;
        }
        void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            { _errors++; _results.Add("CONSOLE: " + message); }
        }
        static Texture2D Read(RenderTexture source)
        {
            var previous = RenderTexture.active; RenderTexture.active = source;
            var image = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
            image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); image.Apply(); RenderTexture.active = previous;
            return image;
        }
        void Save(RenderTexture texture, string name)
        {
            var previous = RenderTexture.active; RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply(); RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(_directory, name), image.EncodeToPNG()); Destroy(image);
        }
        void Finish()
        {
            Application.logMessageReceived -= OnLog;
            _results.Add("Errors: " + _errors);
            _results.Add("GPU: " + SystemInfo.graphicsDeviceName);
            _results.Add("Unity: " + Application.unityVersion);
            File.WriteAllLines(Path.Combine(_directory, "results.txt"), _results);
            Application.Quit(_errors == 0 ? 0 : 1);
        }
    }
}
