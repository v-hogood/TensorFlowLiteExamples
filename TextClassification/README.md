# TensorFlow Lite text classification sample

## Overview

This is an end-to-end example of movie review sentiment classification built
with TensorFlow 2.0 (Keras API), and trained on IMDB dataset. The demo app
processes input movie review texts, and classifies its sentiment into negative
(0) or positive (1).

These instructions walk you through the steps to train and test a simple text
classification model, export them to TensorFlow Lite format and deploy on a
mobile app.

## Model

See
[Text Classification with Movie Reviews](https://www.tensorflow.org/tutorials/keras/basic_text_classification)
for a step-by-step instruction of building a simple text classification model.

#### Switch between inference solutions (Task library vs TFLite Interpreter)

This Text Classification Android reference app demonstrates two implementation
solutions:

(1)
[`lib_task_api`](https://github.com/tensorflow/examples/tree/master/lite/examples/nl_classification/android/lib_task_api)
that leverages the out-of-box API from the
[TensorFlow Lite Task Library](https://www.tensorflow.org/lite/inference_with_metadata/task_library/text_classifier);

(2)
[`lib_interpreter`](https://github.com/tensorflow/examples/tree/master/lite/examples/text_classification/android/lib_interpreter)
that creates the custom inference pipleline using the
[TensorFlow Lite Interpreter Java API](https://www.tensorflow.org/lite/guide/inference#load_and_run_a_model_in_java).

*Note: If you simply want the out-of-box API to run the app, we recommend
`lib_task_api`for inference. If you want to customize your own models and
control the detail of inputs and outputs, it might be easier to adapt your model
inputs and outputs by using `lib_interpreter`.*
