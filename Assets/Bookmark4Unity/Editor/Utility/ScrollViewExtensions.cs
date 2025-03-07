using System;
namespace Bookmark4Unity.Editor
{
    using UnityEngine.UIElements;

    public static class ScrollViewExtensions
    {
        public static void SortFoldoutsDesc(this ScrollView scrollView)
        {
            scrollView.Sort((a, b) =>
            {
                var fa = a.Q<Foldout>("Root");
                var fb = b.Q<Foldout>("Root");
                if (fa is null) return 1;
                if (fb is null) return -1;
                return String.Compare(fa.text, fb.text, StringComparison.Ordinal);
            });
        }

        public static void SortFoldoutsAsc(this ScrollView scrollView)
        {
            scrollView.Sort((a, b) =>
            {
                var fa = a.Q<Foldout>("Root");
                var fb = b.Q<Foldout>("Root");
                if (fa is null) return -1;
                if (fb is null) return 1;
                return String.Compare(fb.text, fa.text, StringComparison.Ordinal);
            });
        }
    }
}