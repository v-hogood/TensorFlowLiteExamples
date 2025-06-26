using System.Collections.Generic;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using TensorFlow.Lite.Support.Label;
using Color = Android.Graphics.Color;
using ProgressBar = Android.Widget.ProgressBar;
using View = Android.Views.View;

namespace AudioClassification
{
    public class ProbabilitiesAdapter : RecyclerView.Adapter
    {
        public IList<Category> CategoryList = new List<Category>();

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view =
                LayoutInflater.From(parent.Context).Inflate(
                    Resource.Layout.item_probability,
                    parent,
                    false);
            return new ViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var category = CategoryList[position];
            (holder as ViewHolder).Bind(category.Label, category.Score, category.Index);
        }

        public override int ItemCount => CategoryList.Count;

        public class ViewHolder : RecyclerView.ViewHolder
        {
            View view;
            private int[] primaryProgressColorList;
            private int[] backgroundProgressColorList;

            public ViewHolder(View view) : base(view)
            {
                this.view = view;
                primaryProgressColorList = view.Context.Resources.GetIntArray(Resource.Array.colors_progress_primary);
                backgroundProgressColorList = view.Context.Resources.GetIntArray(Resource.Array.colors_progress_background);
            }

            public void Bind(string label, float score, int index)
            {
                TextView labelTextView = view.FindViewById<TextView>(Resource.Id.label_text_view);
                labelTextView.Text = label;

                ProgressBar progressBar = view.FindViewById<ProgressBar>(Resource.Id.progress_bar);
                progressBar.ProgressBackgroundTintList =
                    ColorStateList.ValueOf(new Color(
                        backgroundProgressColorList[index % backgroundProgressColorList.Length]));
                progressBar.ProgressTintList =
                    ColorStateList.ValueOf(new Color(
                        primaryProgressColorList[index % primaryProgressColorList.Length]));

                var newValue = (int)(score * 100);
                progressBar.Progress = newValue;
            }
        }
    }
}
