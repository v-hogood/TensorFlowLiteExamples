using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Java.IO;
using Java.Lang;
using Org.Tensorflow.Lite.Support.Label;
using Org.Tensorflow.Lite.Task.Text.Nlclassifier;

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
            classifier.Close();
            classifier = null;
        }

        public List<Result> Classify(string text)
        {
            IList<Category> apiResults = classifier.Classify(text);
            List<Result> results = new List<Result>(apiResults.Count);
            for (int i = 0; i < apiResults.Count; i++)
            {
                Category category = apiResults[i];
                results.Add(new Result("" + i, category.Label, new Float(category.Score)));
            }
            Java.Util.Collections.Sort(results);
            return results;
        }
    }
}
