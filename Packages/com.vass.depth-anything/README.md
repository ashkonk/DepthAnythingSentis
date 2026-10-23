# Depth Anything V2

Unity integration by **Ashkon Khalkhali**, extracted from VASS.

GPU monocular depth for Unity, based on the V2 Small ONNX export. Requires Unity 6 and Inference Engine 2.6.1. Tested with Unity 6000.6.3f1, Windows x64 and D3D11.

```csharp
var estimator = new DepthAnything.DepthEstimator();
// Call each frame, including while an estimate is pending:
estimator.Process(inputTexture, DepthAnything.DepthEstimator.Quality.Fast, producerFrameVersion);
var depth = estimator.DepthTexture; // borrowed RHalf texture, null until ready
// When the owner is destroyed:
estimator.Dispose();
```

Red contains relative inverse depth: larger means nearer, with unknown scale and offset. It is not meters or normalized to 0–1. Pass -1 for the producer version to use Texture2D.updateCount or continuous processing for other textures. Process, reset and dispose on Unity's main thread. Check HasError and Status; Reset clears failures and pending work. Output is owned by the estimator and may be replaced on resize.

`DepthPostProcessor` provides optional GPU normalization and mask-aware edge cleanup. Call `Process(depth, currentMask, estimateMask, deltaTime)` and consume `Texture` (R: 0–1, G: coverage). The two masks must use the same UVs, and estimateMask must match the frame where StartedRun became true. Omit masks for full-frame normalization. Dispose the processor with its owner.

Import **Basic** from Samples for a runnable image/webcam demo and sample image. Import **BodyPix** only after Basic and Keijiro's `jp.keijiro.bodypix` 5.0.0 are installed. The core never references BodyPix. The demo scenes use the Built-in Render Pipeline and standard IMGUI/video/image-conversion modules.

Both scenes default to `Waterfall.jpg`, a real photograph of Ashkon Khalkhali supplied for the demo. Portrait proportions are preserved. The generated `Studio.png` remains available as a landscape alternative; assign it to the presenter's **Sample Image** field to switch inputs.

The Small model is included and pinned. Other variants have different licensing. No Python, server or CUDA dependency is needed. Requires GPU compute; WebGL is unsupported. Other platforms and render pipelines need validation.

Depth work is spread across calls: 128 cold iterator steps, then approximately three calls per estimate with a 192-step cap. This bounds CPU scheduling, not individual GPU dispatch times. Range smoothing does not provide per-pixel video stabilization. Single-view relief does not reconstruct occluded geometry.

## Credits and license

Unity integration, GPU processing and samples by **Ashkon Khalkhali**. Copyright (c) 2026 Ashkon Khalkhali.

The original integration code, shaders, demo scenes and documentation are **MIT** licensed; see [LICENSE.md](LICENSE.md). The unmodified Depth Anything V2 Small model retains **Apache-2.0**; see [ThirdParty/DepthAnything/LICENSE.txt](ThirdParty/DepthAnything/LICENSE.txt). Package metadata uses `MIT AND Apache-2.0` for the software and model, not as alternative licenses for the model or a license for the personal photograph. Preserve both license texts and [Third Party Notices.md](Third%20Party%20Notices.md) when redistributing both components. External dependencies retain their own licenses. The personal photograph is excluded from MIT and supplied for display in this project's demos and documentation; both photographs' reuse terms are recorded in the notices.
