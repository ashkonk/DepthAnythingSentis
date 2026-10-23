# Optional BodyPix integration

Install Keijiro's `jp.keijiro.bodypix` **5.0.0**, then import this package's **Basic** sample, then this sample. Open **BodyPixDemo.unity** and press Play. The example repository already contains Basic under Assets/DepthDemo.

BodyPix is installed from the `https://registry.npmjs.org` scoped registry, with scopes `jp.keijiro.bodypix` and `jp.keijiro.klak.nnutils`. See https://github.com/keijiro/BodyPixSentis for its installation and license.

The scene references the separately installed `MobileNetV1-x100-stride8` ResourceSet at a 512-pixel processing width. It extracts person probability from BodyPix's alpha channel (RGB contains part labels), then feeds the generic DepthPostProcessor with a current mask and the mask captured when inference began. No BodyPix models are bundled here. If the Inspector resource is missing, assign that ResourceSet from BodyPix's ResourceSet directory. Use smaller resources/resolutions if your live-input budget requires them.

To remove BodyPix, remove this imported sample first. The core and Basic sample have no BodyPix assembly references. The optional adapter has no version-dependent code in the core package.
