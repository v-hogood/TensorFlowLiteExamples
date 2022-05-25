using Android.App;
using Xamarin.TensorFlow.Lite.Support.Common;
using Xamarin.TensorFlow.Lite.Support.Common.Ops;

namespace ImageClassification
{
    // This TensorFlowLite classifier works with the quantized MobileNet model.
    public class ClassifierQuantizedMobileNet : Classifier
    {
        //
        // The quantized model does not require normalization, thus set mean as 0.0f, and std as 1.0f to
        // bypass the normalization.
        //
        private const float ImageMean = 0.0f;
        private const float ImageStd = 1.0f;

        // Quantized MobileNet requires additional dequantization to the output probability.
        private const float ProbabilityMean = 0.0f;
        private const float ProbabilityStd = 255.0f;

        //
        // Initializes a {@code ClassifierQuantizedMobileNet}.
        //
        // @param activity
        // @param device a {@link Device} object to configure the hardware accelerator
        // @param numThreads the number of threads during the inference
        // @throws IOException if the model is not loaded correctly
        //
        public ClassifierQuantizedMobileNet(Activity activity, Device device, int numThreads) :
            base(activity, device, numThreads)
        {
        }

        protected override string ModelPath { get; } = "mobilenet_v1_1.0_224_quant.tflite";

        protected override string LabelPath { get; } = "labels.txt";

        protected override ITensorOperator PreprocessNormalizeOp { get; } = new NormalizeOp(ImageMean, ImageStd);

        protected override ITensorOperator PostprocessNormalizeOp { get; } = new NormalizeOp(ProbabilityMean, ProbabilityStd);
    }
}
