# Basic sample

Open **DepthDemo.unity** and press Play. The included `Studio.png` is a generated fictional photo with a foreground person and several background depth layers. The scene displays the input, actual depth output and a GPU-displaced image mesh.

Use the on-screen controls for quality, relief, orbit and optional webcam input. Assign a different `Texture2D` or a prepared `VideoPlayer` on the DepthDemo component for your own content. Preserve non-power-of-two dimensions in your image's import settings so the input aspect ratio is retained.

This sample uses the Built-in Render Pipeline and Unity's standard IMGUI, video and image-conversion modules. The core has no dependency on those presentation choices. GPU compute is required; WebGL is unsupported.

The photo was generated with OpenAI's built-in image-generation tool on October 22, 2026, for reuse with these examples. No reference photograph was used. See Third Party Notices.md in the package for model and sample-image notices.
