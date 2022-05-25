using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Java.IO;
using Xamarin.TensorFlow.Lite.Support.Label;
using Xamarin.TensorFlow.Lite.Task.Text.NlClassifier;

namespace TextClassification
{
    // Load TfLite model and provide predictions with task api.
    public class TextClassificationClient
    {
        private const string Tag = "TaskApi";
        private const string ModelPath = "text_classification.tflite";

        private Context context;

        NLClassifier classifier;

        public TextClassificationClient(Context context)
        {
            this.context = context;
        }

        public void Load()
        {
            try
            {
                classifier = NLClassifier.CreateFromFile(context, ModelPath);
            }
            catch (IOException e)
            {
                Log.Error(Tag, e.Message);
            }
        }

        public void Unload()
        {
            classifier?.Close();
            classifier = null;
        }

        public List<Result> Classify(string text)
        {
            IList<Category> apiResults = classifier.Classify(text);
            List<Result> results = new List<Result>(apiResults.Count);
            for (int i = 0; i < apiResults.Count; i++)
            {
                Category category = apiResults[i];
                results.Add(new Result("" + i, category.Label, category.Score));
            }
            results.Sort((x, y) => y.Confidence.CompareTo(x.Confidence));
            return results;
        }
    }
}
