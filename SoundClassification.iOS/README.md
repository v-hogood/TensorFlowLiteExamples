# TensorFlow Lite sound classification iOS example application

## Overview

This is an example application for [TensorFlow Lite](https://tensorflow.org/lite)
on iOS.

### Model

The model will be downloaded as part of the build process. Also, you can use
your own model generated on
[Teachable Machine](https://teachablemachine.withgoogle.com/train/audio). For an
explanation of training the model, see [Build sound classification models for
mobile apps with Teachable Machine and
TFLite](https://blog.tensorflow.org/2020/12/build-sound-classification-models-for-mobile-apps-with-teachable-machine-and-tflite.html).

### iOS app details

The app is written entirely in C# and uses the TensorFlow Lite
[C# library](https://github.com/v-hogood/TensorFlowLiteSupport/TensorFlowLite)
for performing sound classification.

## Requirements

*   Device with iOS 12.0 or above

*   Visual Studio 2022 for Mac

*   Valid Apple Developer ID

If this is a new install, you will need to run the Xcode application once to
agree to the license before continuing.

Note:
The demo app requires `SelectTfOps` library which only works on a real iOS
device. You can build it and run with the iPhone Simulator, but the app will
raise a exception while initializing TensorFlow Lite runtime.

## Build and run

1.  Clone this GitHub repository to your workstation. `git clone
    https://github.com/v-hogood/TensorFlowLiteExamples.git`

At the end of this step you should have a directory called
`TensorFlowLiteExamples`.

2.  Open the solution in Visual Studio with the following command: `open
    TensorFlowLiteExamples.iOS.sln`

This launches Visual Studio and opens the `TensorFlowLiteExamples.iOS` solution.

3.  Select the `SoundClassification` project in the left hand navigation and open
    the project properties. In the **Bundle Signing** section of the **iOS**
    tab, select your development team from the dropdown.

4.  In order to build the project, you must modify the **Bundle Identifier** in
    the **Info.plist** file so that it is unique across all Xcode projects. To
    create a unique identifier, try adding your initials and a number to the end
    of the string.

5.  With an iOS device connected, build and run the app in Visual Studio.

You'll have to grant permissions for the app to use the device's microphone. Point
the microphone at various objects and enjoy seeing how the model classifies things!

## Model references
_Do not delete the empty references_ to the .tflite and .txt files after you
clone the repo and open the project. These references will be fulfilled once the
model and label files are downloaded when the application is built and run for
the first time. If you delete the references to them, you can still find that
the .tflite and .txt files are downloaded to the Resources folder, the next time you
build the application. You will have to add the references to these files in the
project separately in that case.
