# TensorFlow Lite Image Classification Demo

### Overview

This is a camera app that continuously classifies the objects in the frames
seen by your device's back camera, with the option to use a quantized
[MobileNet V1](https://tfhub.dev/tensorflow/lite-model/mobilenet_v1_1.0_224_quantized/1/metadata/1),
[EfficientNet Lite0](https://tfhub.dev/tensorflow/lite-model/efficientnet/lite0/int8/2),
[EfficientNet Lite1](https://tfhub.dev/tensorflow/lite-model/efficientnet/lite1/int8/2),
or
[EfficientNet Lite2](https://tfhub.dev/tensorflow/lite-model/efficientnet/lite2/int8/2)
model trained on Imagenet (ILSVRC-2012-CLS). These instructions
walk you through building and running the demo on an Android device.

This application should be run on a physical Android device.

![App example showing UI controls. Result is espresso.](screenshot1.jpg?raw=true "Screenshot with controls")

![App example without UI controls. Result is espresso.](screenshot2.jpg?raw=true "Screenshot without controls")

