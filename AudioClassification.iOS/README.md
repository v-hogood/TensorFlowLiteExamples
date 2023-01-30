# TensorFlow Lite audio classification iOS example application

## Overview

This is an example application for [TensorFlow Lite](https://tensorflow.org/lite)
on iOS.

### Model

The model will be downloaded as part of the build process. There are two models
included in this sample app: *
[YAMNet](https://tfhub.dev/google/lite-model/yamnet/classification/tflite/1) is
a general purpose audio classification model that can detects 521 different type
of sounds. *
[Speech command](https://www.tensorflow.org/lite/models/modify/model_maker/speech_recognition)
is a demonstrative model that can recognize a handful of single-word audio
command.

Also, you can use your own model generated on
[Teachable Machine](https://teachablemachine.withgoogle.com/train/audio) or
[Model Maker](https://www.tensorflow.org/lite/models/modify/model_maker/audio_classification).

### iOS app details

The app is written entirely in C# and uses the TensorFlow Lite
[C# library](https://github.com/v-hogood/TensorFlowLiteSupport/TensorFlowLiteTaskAudio)
for performing sound classification.

## Requirements

*   Device with iOS 12.0 or above

*   Visual Studio 2022 for Mac

*   (Optional) Valid Apple Developer ID. If you don't have one, you can run the
    sample app on an iOS Simulator.

If this is a new install, you will need to run the Xcode application once to
agree to the license before continuing.

## Build and run

1.  Clone this GitHub repository to your workstation. `git clone
    https://github.com/v-hogood/TensorFlowLiteExamples.git`

At the end of this step you should have a directory called
`TensorFlowLiteExamples`.

2.  Open the solution in Visual Studio with the following command: `open
    TensorFlowLiteExamples.iOS.sln`

This launches Visual Studio and opens the `TensorFlowLiteExamples.iOS` solution. You can run the
app on an iOS Simulator.

Follow these steps to run the sample app on a physical device.

3.  Select the `AudioClassification` project in the left hand navigation and open
    the project properties. In the **Bundle Signing** section of the **iOS**
    tab, select your development team from the dropdown.

4.  With an iOS device connected, build and run the app in Visual Studio.

You'll have to grant permissions for the app to use the device's microphone. Point
the microphone at various objects and enjoy seeing how the model classifies things!

## Model references

*Do not delete the empty references* to the .tflite files after you clone the
repo and open the project. These references will be fulfilled once the model and
label files are downloaded when the application is built and run for the first
time. If you delete the references to them, you can still find that the .tflite
and .txt files are downloaded to the Resources folder, the next time you build the
application. You will have to add the references to these files in the project
separately in that case.
