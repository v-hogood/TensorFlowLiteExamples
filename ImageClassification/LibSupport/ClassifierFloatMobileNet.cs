using Android.App;
using Xamarin.TensorFlow.Lite.Support.Common;
using Xamarin.TensorFlow.Lite.Support.Common.Ops;

namespace ImageClassification
{
    // This TensorFlowLite classifier works with the float MobileNet model.
    public class ClassifierFloatMobileNet : Classifier
    {
        // Float MobileNet requires additional normalization of the used input.
        private const float ImageMean = 127.5f;
        private const float ImageStd = 127.5f;

        //
        // Float model does not need dequantization in the post-processing. Setting mean and std as 0.0f
        // and 1.0f, repectively, to bypass the normalization.
        //
        private const float ProbabilityMean = 0.0f;

        private const float ProbabilityStd = 1.0f;

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

        protected override string LabelPath { get; } = "labels.txt";

        protected override ITensorOperator PreprocessNormalizeOp { get; } = new NormalizeOp(ImageMean, ImageStd);

        protected override ITensorOperator PostprocessNormalizeOp { get; } = new NormalizeOp(ProbabilityMean, ProbabilityStd);
    }
}
