using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Java.Lang;
using TensorFlow.Lite.Support.Label;
using TensorFlow.Lite.Task.Vision.Classifier;
using Math = System.Math;
using View = Android.Views.View;

namespace ImageClassification
{
    public class ClassificationResultsAdapter : RecyclerView.Adapter
    {
        private const string NoValue = "--";

        private IList<Category> categories = new List<Category>();
        private int adapterSize = 0;

        public void UpdateResults(IList<Classifications> listClassifications)
        {
            categories = Enumerable.Repeat<Category>(null, adapterSize).ToList();
            if (listClassifications != null && listClassifications.Count > 0)
            {
                var sortedCategories = listClassifications[0].Categories.
                    OrderBy(it => it.Index).ToList();
                var min = Math.Min(sortedCategories.Count, categories.Count);
                for (int i = 0; i < min; i++)
                {
                    categories[i] = sortedCategories[i];
                }
            }
        }

        public void UpdateAdapterSize(int size)
        {
            adapterSize = size;
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            public ViewHolder(View view) : base(view) { }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.item_classification_result,
                parent,
                false);
            return new ViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var category = categories[position];
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvLabel).Text =
                category != null && category.Label != null ? category.Label : NoValue;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvScore).Text =
                category != null && category.Score != 0 ? category.Score.ToString("0.00") : NoValue;
        }

        public override int ItemCount => categories.Count;
    }
}
