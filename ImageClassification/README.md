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

#### Switch between inference solutions (Task library vs Support Library)

This Image Classification Android reference app demonstrates two implementation
solutions:

(1)
[`lib_task_api`](https://github.com/tensorflow/examples/tree/master/lite/examples/image_classification/android/lib_task_api)
that leverages the out-of-box API from the
[TensorFlow Lite Task Library](https://www.tensorflow.org/lite/inference_with_metadata/task_library/image_classifier);

(2)
[`lib_support`](https://github.com/tensorflow/examples/tree/master/lite/examples/image_classification/android/lib_support)
that creates the custom inference pipleline using the
[TensorFlow Lite Support Library](https://www.tensorflow.org/lite/inference_with_metadata/lite_support).

Inside **Visual Studio**, you can change the build configuration to whichever one you
want to build and run—just go to `Project > Active Configuration` and select one
from the drop-down menu. See
[Understand build configurations](https://docs.microsoft.com/visualstudio/ide/understanding-build-configurations)
for more details.

*Note: If you simply want the out-of-box API to run the app, we recommend
`lib_task_api` for inference. If you want to customize your own models and
control the detail of inputs and outputs, it might be easier to adapt your model
inputs and outputs by using `lib_support`.*

