# Third Party Notices

The original Unity integration and samples are by Ashkon Khalkhali, Copyright (c) 2026, and are licensed under MIT; see [LICENSE.md](LICENSE.md). Third-party components below retain their separate licenses. The integration's MIT license does not relicense the model or external dependencies.

## Depth Anything V2 Small

Lihe Yang, Bingyi Kang, Zilong Huang, Zhen Zhao, Xiaogang Xu, Jiashi Feng and Hengshuang Zhao, “Depth Anything V2”, 2024.

https://github.com/DepthAnything/Depth-Anything-V2

ONNX export: https://huggingface.co/onnx-community/depth-anything-v2-small

Original file: `onnx/model.onnx`, revision `4472b7362082ad9968fee890ca0f1e5aca36b93d`.

SHA-256: `afb6a5c28f3b6bf1618c6e43f02073ef9dfdc70e937502d51603e57b0a1df10c`.

The model at `Runtime/Resources/DepthAnything/DepthAnythingV2Small.onnx` is unmodified and redistributed under Apache-2.0; see [ThirdParty/DepthAnything/LICENSE.txt](ThirdParty/DepthAnything/LICENSE.txt). Preserve that license and the model attribution with redistributions. Only Small is included. Upstream Base, Large and Giant use CC-BY-NC-4.0 and are not part of this package.

## Optional BodyPix

BodyPixSentis by Keijiro Takahashi: https://github.com/keijiro/BodyPixSentis

The optional adapter calls its public API. BodyPix code and weights are downloaded separately through Unity Package Manager and remain subject to their original license and notices. No BodyPix assets are bundled in this package.

## Personal sample photograph

`Waterfall.jpg` is a real photograph of Ashkon Khalkhali, supplied by him for this demo on October 22, 2026. The 1536×2049 JPEG image data is unchanged; embedded EXIF/location metadata has been removed. The image is included in `Samples~/Basic` and referenced by both demo scenes. No AI image editing was used.

The personal photograph and README previews containing it are excluded from the software's MIT license. They are supplied for display in this project's demos and documentation; no broader permission to reuse the photograph is granted here. Contact the maintainer about other uses. The credit identifies the supplier and subject, without asserting the photographer's identity.

## Alternative generated sample photograph

Studio.png is an AI-generated fictional scene created for this demo with OpenAI's built-in image-generation tool, October 22, 2026. It is supplied for reuse with these examples, without an attribution requirement from this project's contributors. No claim of exclusive copyright in the generated image is made. See the repository's Documentation/SampleImage.md for the prompt.
