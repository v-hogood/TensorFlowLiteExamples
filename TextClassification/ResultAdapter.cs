using Android.Views;
using AndroidX.RecyclerView.Widget;
using TensorFlow.Lite.Support.Label;

namespace TextClassification
{
    // An immutable result returned by a TextClassifier describing what was classified.
    public class ResultsAdapter : RecyclerView.Adapter
    {
        public IList<Category> ResultsList = new List<Category>();

        public class ViewHolder : RecyclerView.ViewHolder
        {
            public ViewHolder(View view) : base(view) { }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.item_classification,
                parent,
                false);
            return new ViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var category = ResultsList[position];
            var result = holder.ItemView.FindViewById<TextView>(Resource.Id.result);
            result.Text = holder.ItemView.Context.Resources.GetString(
                Resource.String.result_display_text,
                category.Label,
                category.Score);
        }

        public override int ItemCount => ResultsList.Count;
    }
}
