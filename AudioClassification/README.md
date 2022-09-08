# TensorFlow Lite Audio Classification Android Demo

### Overview

This sample will record audio on a physical Android device and attempt to
classify those recordings. The supported classification models include
[YAMNET](https://tfhub.dev/google/lite-model/yamnet/classification/tflite/1) and
a custom speech command model trained using
[TensorFlow's Model Maker](https://www.tensorflow.org/lite/models/modify/model_maker/speech_recognition).
These instructions walk you through building and running the demo on an Android
device.

This application should be run on a physical Android device.

## Build the demo using Visual Studio

### Prerequisites

*   The **[Visual Studio](https://visualstudio.microsoft.com/vs/mac/)**
    IDE. This sample has been tested on Mac Visual Studio 2022.

*   A physical Android device with a minimum OS version of SDK 23 (Android 6.0)
    with developer mode enabled. The process of enabling developer mode may vary
    by device.

### Building

*   Open Visual Studio. From the Welcome screen, select Open a local
    Visual Studio project, solution, or file.

*   From the Open File or Project window that appears, navigate to and select
    the TensorFlowLiteExamples/AudioClassification/AudioClassification
    solution. Click Open.

*   With your Android device connected to your computer and developer mode
    enabled, click on the black Run arrow in Visual Studio.

