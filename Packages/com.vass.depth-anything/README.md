# Depth Anything V2 for Unity

Unity integration by **Ashkon Khalkhali**.

GPU depth estimation using Depth Anything V2 Small and Sentis 2.6.1.
Requires Unity 6 and compute shader support. WebGL is not supported.

```csharp
var estimator = new DepthAnything.DepthEstimator();

// Call each frame; the first result takes several frames.
estimator.Process(inputTexture);
var depth = estimator.DepthTexture;

// When finished:
estimator.Dispose();
```

Depth is relative: larger values are nearer, not meters. The estimator owns the
output texture. Use `DepthPostProcessor` for normalized 0–1 depth.

## Samples

Import **Basic** and open `DepthDemo.unity` for image, video or webcam input.
For person depth, install [BodyPixSentis 5.0.0](https://github.com/keijiro/BodyPixSentis#installation),
then import **BodyPix** and open `BodyPixDemo.unity`. Both samples use the Built-in
Render Pipeline.

See the [repository README](https://github.com/ashkonk/DepthAnythingSentis#readme)
for installation and API usage.

## License

Code: [MIT](LICENSE.md). Model: [Apache-2.0](ThirdParty/DepthAnything/LICENSE.txt).
Model credits and photo terms: [Third Party Notices](Third%20Party%20Notices.md).
