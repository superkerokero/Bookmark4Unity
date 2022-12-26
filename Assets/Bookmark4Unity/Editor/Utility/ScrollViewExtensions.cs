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
                return fa.text.CompareTo(fb.text);
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
                return fb.text.CompareTo(fa.text);
            });
        }
    }
}