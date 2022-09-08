
# TensorFlow Lite Text Classification Android Demo

### Overview

This sample will accept text entered into a field and classify it as either
positive or negative with a provided confidence score. The supported
classification models include Word Vector and MobileBERT, both of which are
generated using
[TensorFlow's Model Maker](https://www.tensorflow.org/lite/models/modify/model_maker/text_classification).
These instructions walk you through building and running the demo on an Android
device.

## Build the demo using Visual Studio

### Prerequisites

* The **[Visual Studio](https://visualstudio.microsoft.com/vs/mac/)** IDE.
  This sample has been tested on Mac Visual Studio 2022.

* A physical or emulated Android device with a minimum OS version of SDK 21
  (Android 5.0) with developer mode enabled. The process of enabling
  developer mode may vary by device.

### Building

* Open Visual Studio. From the Welcome screen, select Open a local
    Visual Studio project, solution, or file.

* From the Open File or Project window that appears, navigate to and select
    the TensorFlowLiteExamples/TextClassification/TextClassification solution.
    Click Open.

* With your Android device connected to your computer and developer mode
    enabled, click on the black Run arrow in Visual Studio.

