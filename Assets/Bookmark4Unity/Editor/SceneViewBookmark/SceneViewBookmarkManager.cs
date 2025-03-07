using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bookmark4Unity.Editor
{
    static class SceneViewBookmarkManager
    {
        public static event Action<int> MoveToBookMarkEvent;
        public static event Action<int, SceneViewCameraBookmark, Texture2D> SetBookmarkEvent;
        public static event Action ClearBookmarksEvent;
        private const string SceneViewBookmarkIconBlackGUID = "94a274db694b5477d98a39cc07008f41";
        private const string SceneViewBookmarkIconWhiteGUID = "0dcf460af82ad477f8e42045d0e34711";
        private const string SceneViewEmptyIconGUID = "fede30e0a177c49e9bd9a9ef612d58ad";
        public const int maxBookmarkCount = 9;
        public const int previewScreenshotSizeX = 42;
        public const int previewScreenshotSizeY = 42;

        const int previousViewSlot = 0;

        public static bool HasPreviousView => HasBookmark(previousViewSlot);

        public static bool IsOverlayVisible = false;

        private static Texture2D _sceneViewBookmarkIconBlack;
        private static Texture2D _sceneViewBookmarkIconWhite;
        public static Texture2D SceneViewBookmarkIcon
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    _sceneViewBookmarkIconWhite ??= AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(SceneViewBookmarkManager.SceneViewBookmarkIconWhiteGUID));
                    return _sceneViewBookmarkIconWhite;
                }
                else
                {
                    _sceneViewBookmarkIconBlack ??= AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(SceneViewBookmarkManager.SceneViewBookmarkIconBlackGUID));
                    return _sceneViewBookmarkIconBlack;
                }
            }
        }

        private static Texture2D _sceneViewEmptyIcon;
        public static Texture2D SceneViewEmptyIcon
        {
            get
            {
                _sceneViewEmptyIcon ??= AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(SceneViewBookmarkManager.SceneViewEmptyIconGUID));
                return _sceneViewEmptyIcon;
            }
        }

        public static bool HasBookmark(int slot)
        {
            var key = GetEditorPrefsKey(slot);
            return EditorPrefs.HasKey(key);
        }

        public static void MoveToBookmark(int slot)
        {
            // load bookmark
            var bookmark = ReadFromEditorPrefs(slot);
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            // save current scene view camera
            var previewCapture = SceneViewScreenCapture();
            var prevBookmark = new SceneViewCameraBookmark(SceneView.lastActiveSceneView, previewCapture);

            sceneView.in2DMode = bookmark.in2DMode;
            sceneView.pivot = bookmark.pivot;
            if (!bookmark.in2DMode) sceneView.rotation = bookmark.rotation;
            sceneView.size = bookmark.size;

            // update previous view slot
            SetBookmark(prevBookmark, previousViewSlot, previewCapture);
            MoveToBookMarkEvent?.Invoke(slot);
        }

        public static void ReturnToPreviousView()
        {
            MoveToBookmark(previousViewSlot);
        }

        public static void SetBookmark(SceneViewCameraBookmark bookmark, int slot, Texture2D previewCapture)
        {
            WriteToEditorPrefs(slot, bookmark);
            SetBookmarkEvent?.Invoke(slot, bookmark, previewCapture);

            if (slot != previousViewSlot)
            {
                Debug.Log($"<color=green><b>{Bookmark4UnityWindow.Name}</b></color>: [<color=yellow><b>{SceneManager.GetActiveScene().name}</b></color>] Scene view camera bookmarked at <color=red>slot <b>{slot}</b></color>.");
            }
        }

        public static void SetBookmark(int slot)
        {
            var previewCapture = SceneViewScreenCapture();
            var bookmark = new SceneViewCameraBookmark(SceneView.lastActiveSceneView, previewCapture);
            SetBookmark(bookmark, slot, previewCapture);
        }

        public static void ClearAllBookmarksForCurrentScene()
        {
            for (int i = 0; i <= maxBookmarkCount; i++)
            {
                var key = GetEditorPrefsKey(i);
                EditorPrefs.DeleteKey(key);
            }

            ClearBookmarksEvent?.Invoke();
        }

        static string GetEditorPrefsKey(int slot)
        {
            var scene = SceneManager.GetActiveScene();
            return $"{Bookmark4UnityWindow.Prefix}{scene.name}_{slot}";
        }

        public static SceneViewCameraBookmark ReadFromEditorPrefs(int slot)
        {
            var key = GetEditorPrefsKey(slot);
            var json = EditorPrefs.GetString(key);
            return JsonUtility.FromJson<SceneViewCameraBookmark>(json);
        }

        static void WriteToEditorPrefs(int slot, SceneViewCameraBookmark bookmark)
        {
            var key = GetEditorPrefsKey(slot);
            var json = JsonUtility.ToJson(bookmark);
            EditorPrefs.SetString(key, json);
        }

        static Texture2D SceneViewScreenCapture()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView is null) return null;
            if (!sceneView.hasFocus) sceneView.Show();

            // Create a temporary RenderTexture with the desired resolution
            var renderTexture = new RenderTexture(previewScreenshotSizeX, previewScreenshotSizeY, 24);
            sceneView.camera.targetTexture = renderTexture;
            sceneView.camera.Render();

            // Create a new Texture2D and read the RenderTexture into it
            var result = new Texture2D(previewScreenshotSizeX, previewScreenshotSizeY, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            result.ReadPixels(new Rect(0, 0, previewScreenshotSizeX, previewScreenshotSizeY), 0, 0);
            result.Apply();

            // Clean up
            sceneView.camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);

            return result;
        }
    }
}
