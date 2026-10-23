# Validation — October 22, 2026

## Environment

Unity **6000.6.3f1**, Inference Engine **2.6.1**, Windows x64, D3D11, NVIDIA GeForce RTX 5080 Laptop GPU. The example project uses the Built-in Render Pipeline and Linear color space. Optional integration: BodyPix **5.0.0**, `MobileNetV1-x100-stride8`, processing width 512.

## 0.1.2: supplied waterfall photograph

The Basic and BodyPix Windows development players were rebuilt and run with the supplied 1536×2049 portrait. Both builds succeeded with zero errors and the same 185 dependency shader warnings. Both actual overview captures were visually inspected and are used in the README.

- **Basic: 36 assertions passed, zero captured runtime errors.** All three quality settings complete inference. The depth preview and mesh target preserve the portrait aspect; normalized depth spans approximately 0.145–1.000. The final Basic build was run after restoring the default manifest without BodyPix. Its initial relief strength is 0.35 and orbit is −6°, keeping the face readable while still showing displaced foreground layers.
- **BodyPix: 14 assertions passed, zero captured runtime errors.** Approximately 11.3% foreground coverage, the subject's torso included, distant waterfall excluded, portrait proportions preserved, and successful disable/re-enable with matching mask/depth.
- The original JPEG compressed image data is byte-identical after removal of metadata segments; no crop or image generation was used. The studio image remains available as an alternative.
- Full-scene relief still shows stretched or missing surfaces around the railing and silhouette when rotated. This reflects unseen background and depth discontinuities in a single image. The BodyPix view removes the background and gives a cleaner person-only preview. Neither is a complete 3D reconstruction.

Evidence: [Basic results](Validation/waterfall-basic-results.txt), [BodyPix results](Validation/waterfall-bodypix-results.txt), [Basic build](Validation/waterfall-build-basic.txt), [BodyPix build](Validation/waterfall-build-bodypix.txt). Counts refer to individual assertions, including scheduling checks, rather than separate scenarios. Runtime inference and the normalized-depth implementation were not changed; the code change is in the sample's presentation and capture sizing.

The **0.1.2 tarball** was also installed in a new Unity project without BodyPix. Package Manager imported the Basic sample; its scene, shaders and 1536×2049 photograph references were intact. GPU inference produced finite, non-flat output in four ticks using the fresh project's Gamma color space. [Import evidence](Validation/waterfall-tarball-import.txt), [inference evidence](Validation/waterfall-tarball-verify.txt).

[Preservation checks](Validation/waterfall-preservation.json) confirm identical image data, matching packaged/sample files, the original model hash and unchanged hashes for all ten VASS files captured at the start of this update. The project's default package manifest and settings retain the independent Basic setup.

The following sections retain the original studio-image validation and licensing history.

## 0.1.0: standalone builds and runtime

Both the Basic and BodyPix Windows development players built successfully with **zero build errors**. The final build reports contain **185 dependency shader warnings**, principally Inference Engine compute/pixel shader compiler diagnostics. No C# or shader warnings were found in this package's own files. Unity's transitive App UI dependency also logs that it creates default settings at player startup; it is not used by this sample's IMGUI presentation.

- **Basic: 34 checks passed, zero captured runtime errors.** All three quality settings completed GPU inference through multiple bounded ticks. Checks cover still caching, producer-frame invalidation, source replacement during inference, extreme-aspect grid bounds, finite/non-flat normalized output, empty masks, and resource recreation after disable/re-enable.
- **BodyPix: 12 checks passed, zero captured runtime errors.** Checks cover the intended optional processing mode, useful normalized depth, approximately 9.4% foreground coverage in the included photo, a selected torso landmark, excluded studio background, still caching, and adapter disable/re-enable with matching mask/depth reacquisition.
- The Basic player was rebuilt and tested after removing BodyPix from the example project. BodyPix is not in the delivered default manifest, and its sample scripts remain under the ignored `Samples~` directory until imported.
- Both README previews were visually inspected. They are direct offscreen renders of the source texture, actual inferred depth and displaced mesh. The image import preserves its original 1536×1024 aspect ratio. No generated depth visualization is substituted for inference output.

Evidence: [Basic results](Validation/basic-results.txt), [BodyPix results](Validation/bodypix-results.txt), [Basic build](Validation/build-basic.txt), [BodyPix build](Validation/build-bodypix.txt).

The checks count individual assertions, including per-tick scheduling limits; they are not 46 separate test scenarios. Runtime tests use the included still photo and a replacement RenderTexture. No physical webcam is opened automatically.

## Fresh tarball installation

The exported `com.vass.depth-anything-0.1.0.tgz` was installed through a local tarball dependency in a new Unity project with no embedded copy of the package and no BodyPix dependency. Package Manager imported the Basic sample successfully. A second Editor run verified intact scene/script/image/shader references, original image dimensions, absence of the optional adapter assembly, and finite, non-flat GPU inference output. The estimate completed in four scheduling ticks. This additional inference check used the fresh project's default **Gamma** color space.

Evidence: [Import](Validation/tarball-import.txt), [GPU verification](Validation/tarball-verify.txt). The check source is supplied as `Tools/PackageInstallCheck.cs.txt`; copy it into the fresh project's Assets/Editor as a `.cs` file, then run `PackageInstallCheck.ImportBasic` with `-executeMethod -quit`, followed by `PackageInstallCheck.Verify` with `-executeMethod` (the latter exits itself).

## VASS preservation

The standalone project lives outside VASS's active Assets, Packages and ProjectSettings directories. No existing VASS file was edited or moved. SHA-256 checks confirm that the original estimator, model, preprocessing and stage shaders, media/stage code, VASS scene, package manifest/lock and Unity version file match the baseline captured before extraction. [Preservation hashes](Validation/vass-preservation.json).

This validates the absence of changes to the relevant VASS integration; it is not a new execution of VASS's full regression suite.

## License and author correction

After the runtime and installation checks above, the original integration's license was set to MIT, author credits were added for Ashkon Khalkhali, and the bundled model's Apache-2.0 license was preserved in `Packages/com.vass.depth-anything/ThirdParty/DepthAnything/LICENSE.txt`. Package metadata records `MIT AND Apache-2.0` for these separate components. Version **0.1.1** packages these corrections in a new tarball and source ZIP; 0.1.0 is the prior export used for the runtime and fresh-install checks above. The source export script now derives the ZIP filename from the package version.

A SHA-256 comparison against the earlier source ZIP found all 112 files outside the licensing/author changes identical, including runtime code, shaders, scenes, sample images, project settings and the model. The changelog and this validation note were then updated. The original model hash remains `afb6a5c28f3b6bf1618c6e43f02073ef9dfdc70e937502d51603e57b0a1df10c`. Runtime and fresh-install checks were not rerun for the documentation and metadata correction. VASS's active project directories were not edited for this correction; preservation evidence above is the snapshot from the original extraction.

## Scope and limits

Not validated: physical webcam or video devices, other graphics APIs/platforms, URP/HDRP presentation, IL2CPP, long-duration memory behavior, worst-case GPU budgets or universal model accuracy. Build success and finite depth do not establish those properties. The model predicts relative inverse depth, and a displaced single image is not a complete reconstructed person or scene.

## Reproduction

Run `Tools/Validate.ps1` with Unity closed for Basic. Enable the optional sample as described in the README, then run `Tools/Validate.ps1 -BodyPix`. The scripts launch Windows players with `-depthSmoke <output-directory>`, which enables the otherwise inactive validation component.

Failed intermediate captures/builds are retained in the workspace's ignored Validation directory. They caught a hidden-window screenshot limitation, the manual render target's Y orientation, a missing optional assembly reference and the distinction between BodyPix's RGB part labels and alpha person probability. Those issues were corrected before the final runs above.
