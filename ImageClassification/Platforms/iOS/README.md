# TensorFlow Lite image classification iOS example application

## Overview

This is an example application for [TensorFlow Lite](https://tensorflow.org/lite)
on iOS. It uses [Image classification](https://www.tensorflow.org/lite/examples/image_classification/overview)
to continuously classify whatever it sees from the device's back camera, using
a quantized MobileNet model. The application must be run on device.

These instructions walk you through building and
running the demo on an iOS device.

### Model

For details of the model used, visit [Image classification](https://www.tensorflow.org/lite/examples/image_classification/overview).

The model will be downloaded as part of the build process.

### iOS app details

The app is written entirely in C# and uses the TensorFlow Lite Task Library's
ImageClassifier(https://www.tensorflow.org/lite/inference_with_metadata/task_library/image_classifier#run_inference_in_ios)
for performing image classification.

## Requirements

*   Device with iOS 12.0 or above

*   Visual Studio 2022 for Mac

*   Valid Apple Developer ID

If this is a new install, you will need to run the Xcode application once to
agree to the license before continuing.

Note: The demo app requires a camera and must be executed on a real iOS device.
You can build it and run with the iPhone Simulator, but the app will raise a
`Camera not found` exception.

## Build and run

1.  Clone this GitHub repository to your workstation. `git clone
    https://github.com/v-hogood/TensorFlowLiteExamples.git`

At the end of this step you should have a directory called
`TensorFlowLiteExamples`.

2.  Open the solution in Visual Studio with the following command: `open
    TensorFlowLiteExamples.iOS.sln`

This launches Visual Studio and opens the `TensorFlowLiteExamples.iOS` solution.

3.  Select the `ImageClassification` project in the left hand navigation and open
    the project properties. In the **Bundle Signing** section of the **iOS**
    tab, select your development team from the dropdown.

4.  With an iOS device connected, build and run the app in Visual Studio.

You'll have to grant permissions for the app to use the device's camera. Point
the camera at various objects and enjoy seeing how the model classifies things!

## Model references

_Do not delete the empty references_ to the .tflite files after you
clone the repo and open the project. These references will be fulfilled once the
model files are downloaded when the application is built and run for
the first time. If you delete the references to them, you can still find that
the .tflite files are downloaded to the `Resources` folder, the next time you
build the application. You will have to add the references to these files in the
project separately in that case.
