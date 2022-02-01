using Android.App;

namespace ImageClassification
{
    // This TensorFlowLite classifier works with the float MobileNet model.
    public class ClassifierFloatMobileNet : Classifier
    {
        //
        // Initializes a {@code ClassifierFloatMobileNet}.
        //
        // @param activity
        // @param device a {@link Device} object to configure the hardware accelerator
        // @param numThreads the number of threads during the inference
        // @throws IOException if the model is not loaded correctly
        //
        public ClassifierFloatMobileNet(Activity activity, Device device, int numThreads) :
            base(activity, device, numThreads)
        {
        }

        protected override string ModelPath { get; } = "mobilenet_v1_1.0_224.tflite";
    }
}
