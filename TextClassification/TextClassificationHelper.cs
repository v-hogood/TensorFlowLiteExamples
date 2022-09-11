using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Java.Lang;
using Java.Util.Concurrent;
using Xamarin.TensorFlow.Lite.Support.Label;
using Xamarin.TensorFlow.Lite.Task.Base;
using Xamarin.TensorFlow.Lite.Task.Text.NlClassifier;

namespace TextClassification
{
    public class TextClassificationHelper
    {
        public int CurrentDelegate = 0;
        public string CurrentModel = WordVec;
        private Context context;
        private TextResultsListener listener;

        // There are two different classifiers here to support both the Average Word Vector
        // model (NLClassifier) and the MobileBERT model (BertNLClassifier). Model selection
        // can be changed from the UI bottom sheet.
        private BertNLClassifier bertClassifier;
        private NLClassifier nlClassifier;

        private ScheduledThreadPoolExecutor executor;

        public TextClassificationHelper(Context context, TextResultsListener listener)
        {
            this.context = context;
            this.listener = listener;

            InitClassifier();
        }

        public void InitClassifier()
        {
            var baseOptionsBuilder = BaseOptions.InvokeBuilder();

            // Use the specified hardware for running the model. Default to CPU.
            // Possible to also use a GPU delegate, but this requires that the classifier be created
            // on the same thread that is using the classifier, which is outside of the scope of this
            // sample's design.
            switch(CurrentDelegate)
            {
                default:
                case DelegateCpu:
                    // Default
                    break;
                case DelegateNnapi:
                    baseOptionsBuilder.UseNnapi();
                    break;
            }

            var baseOptions = baseOptionsBuilder.Build();

            // Directions for generating both models can be found at
            // https://www.tensorflow.org/lite/models/modify/model_maker/text_classification
            if (CurrentModel == MobileBert)
            {
                var options = BertNLClassifier.BertNLClassifierOptions
                    .InvokeBuilder()
                    .SetBaseOptions(baseOptions)
                    .Build();

                bertClassifier = BertNLClassifier.CreateFromFileAndOptions(
                    context,
                    MobileBert,
                    options);
            }
            else if (CurrentModel == WordVec)
            {
                var options = NLClassifier.NLClassifierOptions.InvokeBuilder()
                    .SetBaseOptions(baseOptions)
                    .Build();

                nlClassifier = NLClassifier.CreateFromFileAndOptions(
                    context,
                    WordVec,
                    options);
            }
        }

        public void Classify(string text)
        {
            executor = new ScheduledThreadPoolExecutor(1);

            executor.Execute(new Runnable(() =>
            {
                IList<Category> results;
                // inferenceTime is the amount of time, in milliseconds, that it takes to
                // classify the input text.
                var inferenceTime = SystemClock.UptimeMillis();

                // Use the appropriate classifier based on the selected model
                if (CurrentModel == MobileBert)
                {
                    results = bertClassifier.Classify(text);
                }
                else
                {
                    results = nlClassifier.Classify(text);
                }

                inferenceTime = SystemClock.UptimeMillis() - inferenceTime;

                listener.OnResult(results, inferenceTime);
            }));
        }

        public interface TextResultsListener
        {
            void OnError(string error);
            void OnResult(IList<Category> results, long inferenceTime);
        }

        const int DelegateCpu = 0;
        const int DelegateNnapi = 1;
        public const string WordVec = "wordvec.tflite";
        public const string MobileBert = "mobilebert.tflite";
    }
}
