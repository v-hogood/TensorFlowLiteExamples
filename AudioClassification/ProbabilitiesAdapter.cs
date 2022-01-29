using System;
using System.Collections.Generic;
using Android.Animation;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Org.Tensorflow.Lite.Support.Label;

namespace SoundClassification
{
    public class ProbabilitiesAdapter : RecyclerView.Adapter
    {
        public List<Category> CategoryList { get; set; } = new List<Category>();

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view =
                LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_probability, parent, false);
            return new ViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var category = CategoryList[position];
            (holder as ViewHolder).Bind(position, category.Label, category.Score);
        }

        public override int ItemCount => CategoryList.Count;

        public class ViewHolder : RecyclerView.ViewHolder
        {
            View view;
            public ViewHolder(View view) : base(view) { this.view = view; }

            public void Bind(int position, string label, float score)
            {
                TextView labelTextView = view.FindViewById<TextView>(Resource.Id.label_text_view);
                labelTextView.Text = label;
                ProgressBar progressBar = view.FindViewById<ProgressBar>(Resource.Id.progress_bar);
                progressBar.ProgressBackgroundTintList = ProgressColorPairList[position % 3].Item1;
                progressBar.ProgressTintList = ProgressColorPairList[position % 3].Item2;

                var newValue = (int)(score * 100);
                // If you don't want to animate, you can write like `progressBar.progress = newValue`.
                var animation =
                    ObjectAnimator.OfInt(progressBar, "progress", progressBar.Progress, newValue);
                animation.SetDuration(100);
                animation.SetInterpolator(new AccelerateDecelerateInterpolator());
                animation.Start();
            }
        }

        // List of pairs of background tint and progress tint
        private static List<Tuple<ColorStateList, ColorStateList>> ProgressColorPairList = new List<Tuple<ColorStateList, ColorStateList>>
        {
            Tuple.Create(
                ColorStateList.ValueOf(new Color(unchecked((int)0xfff9e7e4))), ColorStateList.ValueOf(new Color(unchecked((int)0xffd97c2e)))),
            Tuple.Create(
                ColorStateList.ValueOf(new Color(unchecked((int)0xfff7e3e8))), ColorStateList.ValueOf(new Color(unchecked((int)0xffc95670)))),
            Tuple.Create(
                ColorStateList.ValueOf(new Color(unchecked((int)0xffecf0f9))), ColorStateList.ValueOf(new Color(unchecked((int)0xff714Fe7))))
        };
    }
}
