using Android.App;

namespace ImageClassification
{
    // This TensorFlowLite classifier works with the quantized MobileNet model.
    public class ClassifierQuantizedMobileNet : Classifier
    {
        //
        // Initializes a {@code ClassifierQuantizedMobileNet}.
        //
        // @param device a {@link Device} object to configure the hardware accelerator
        // @param numThreads the number of threads during the inference
        // @throws IOException if the model is not loaded correctly
        //
        public ClassifierQuantizedMobileNet(Activity activity, Device device, int numThreads) :
            base(activity, device, numThreads)
        {
        }

        protected override string GetModelPath()
        {
            // you can download this file from
            // see build.gradle for where to obtain this file. It should be auto
            // downloaded into assets.
            return "mobilenet_v1_1.0_224_quant.tflite";
        }
    }
}
