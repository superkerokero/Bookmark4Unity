namespace Bookmark4Unity.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public class BackgroundColorScope : IDisposable
    {
        private Color originalColor;
        public BackgroundColorScope(Color backgroundColor)
        {
            originalColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
        }

        public void Dispose()
        {
            GUI.backgroundColor = originalColor;
        }
    }
}