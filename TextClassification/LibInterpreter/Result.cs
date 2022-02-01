using Java.Lang;

namespace TextClassification
{
    // An immutable result returned by a TextClassifier describing what was classified.
    public class Result
    {
        //
        // A unique identifier for what has been classified. Specific to the class, not the instance of
        // the object.
        //
        public string Id { get; }

        // Display name for the result.
        public string Title { get; }

        // A sortable score for how good the result is relative to others. Higher should be better.
        public Float Confidence { get; }

        public Result(string id, string title, float confidence)
        {
            this.Id = id;
            this.Title = title;
            this.Confidence = new Float(confidence);
        }

        public override string ToString()
        {
            string resultString = "";
            if (Id != null)
            {
                resultString += "[" + Id + "] ";
            }

            if (Title != null)
            {
                resultString += Title + " ";
            }

            if (Confidence != null)
            {
                resultString += string.Format("({0:0.0}) ", Confidence.FloatValue() * 100.0f);
            }

            return resultString.Trim();
        }
    }
}
