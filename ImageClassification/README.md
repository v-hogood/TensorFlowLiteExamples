# TensorFlow Lite image classification Android example application

## Overview

This is an example application for
[TensorFlow Lite](https://tensorflow.org/lite) on Android. It uses
[Image classification](https://www.tensorflow.org/lite/models/image_classification/overview)
to continuously classify whatever it sees from the device's back camera.
Inference is performed using the TensorFlow Lite Java API. The demo app
classifies frames in real-time, displaying the top most probable
classifications. It allows the user to choose between a floating point or
[quantized](https://www.tensorflow.org/lite/performance/post_training_quantization)
model, select the thread count, and decide whether to run on CPU, GPU, or via
[NNAPI](https://developer.android.com/ndk/guides/neuralnetworks).

These instructions walk you through building and running the demo on an Android
device. For an explanation of the source, see
[TensorFlow Lite Android image classification example](EXPLORE_THE_CODE.md).

<!-- TODO(b/124116863): Add app screenshot. -->

### Model

We provide 4 models bundled in this App: MobileNetV1 (float), MobileNetV1
(quantized), EfficientNetLite (float) and EfficientNetLite (quantized).
Particularly, we chose "mobilenet_v1_1.0_224" and "efficientnet-lite0".
MobileNets are classical models, while EfficientNets are the latest work. The
chosen EfficientNet (lite0) has comparable speed with MobileNetV1, and on the
ImageNet dataset, EfficientNet-lite0 out performs MobileNetV1 by ~4% in terms of
top-1 accuracy.

For details of the model used, visit
[Image classification](https://www.tensorflow.org/lite/models/image_classification/overview).

