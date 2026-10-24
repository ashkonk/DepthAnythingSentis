# BodyPix sample

1. Install [BodyPixSentis 5.0.0](https://github.com/keijiro/BodyPixSentis#installation)
   using Keijiro's scoped registry.
2. Import this package's **Basic** sample first, unless it is already in your project.
3. Import **BodyPix**, open `BodyPixDemo.unity` and press Play.

The scene uses BodyPix's `MobileNetV1-x100-stride8` model to isolate the person and
show their depth. If **Resources** is unassigned, select that asset from BodyPix's
`ResourceSet` folder. The sample uses the Built-in Render Pipeline.
