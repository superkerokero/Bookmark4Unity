#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Scene = UnityEngine.SceneManagement.Scene;

namespace Bookmark4Unity.Editor
{
    [Overlay(typeof(SceneView), "Scene View Camera Bookmarks")]
    public class SceneViewBookmarkOverlay : Overlay, ITransientOverlay
    {
        public bool visible => SceneViewBookmarkManager.IsOverlayVisible;

        private const string UXML_GUID = "0a5cb4dbb8d6b4b8b9aabfe0f499af04";
        private const string BTN_UXML_GUID = "4598592c7ca0248ea97b3bdb91dd66c1";
        private Color SAVE_BTN_COLOR = new(0.25f, 0.25f, 0.25f, 1f);
        private Color PREV_BTN_COLOR = new(0.45f, 0f, 0f, 1f);
        private VisualElement rootVisualElement;
        private readonly Button[] moveToBtns = new Button[SceneViewBookmarkManager.maxBookmarkCount + 1];
        private readonly Button[] saveToBtns = new Button[SceneViewBookmarkManager.maxBookmarkCount + 1];
        private int prevIndex;

        public override void OnCreated()
        {
            base.OnCreated();
            rootVisualElement = new VisualElement();
            rootVisualElement.style.height = 60;
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(UXML_GUID));
            var btnUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(BTN_UXML_GUID));
            rootVisualElement.Add(uxml.Instantiate());
            var scroll = rootVisualElement.Q<ScrollView>("ScrollView");
            for (int i = 0; i <= SceneViewBookmarkManager.maxBookmarkCount; i++)
            {
                var btn = btnUxml.Instantiate();
                moveToBtns[i] = btn.Q<Button>("MoveTo");
                saveToBtns[i] = btn.Q<Button>("SaveTo");
                moveToBtns[i].text = "";
                moveToBtns[i].tooltip = i > 0 ? $"Move to scene view camera bookmark slot {i}" : "Return to previous view";
                if (SceneViewBookmarkManager.HasBookmark(i))
                {
                    var bookmark = SceneViewBookmarkManager.ReadFromEditorPrefs(i);
                    var preview = bookmark.GetSceneViewScreenShot() ?? SceneViewBookmarkManager.SceneViewBookmarkIcon;
                    moveToBtns[i].style.backgroundImage = new StyleBackground(preview);
                    moveToBtns[i].text = bookmark.in2DMode ? "2D" : "";
                }
                else
                {
                    moveToBtns[i].style.backgroundImage = new StyleBackground(SceneViewBookmarkManager.SceneViewEmptyIcon);
                    moveToBtns[i].text = "";
                }

                var index = i; // saved for lambda functions
                moveToBtns[i].clicked += () =>
                {
                    if (!SceneViewBookmarkManager.HasBookmark(index)) return;
                    SceneViewBookmarkManager.MoveToBookmark(index);
                };

                if (i > 0)
                {
                    // previous slot
                    saveToBtns[i].text = $"SAVE {i}";
                    saveToBtns[i].tooltip = $"Save scene view camera to bookmark slot {i}";
                    saveToBtns[i].style.backgroundColor = SAVE_BTN_COLOR;
                    saveToBtns[i].style.color = Color.white;
                    saveToBtns[i].clicked += () =>
                    {
                        SceneViewBookmarkManager.SetBookmark(index);
                    };
                }
                else
                {
                    // normal slots
                    saveToBtns[i].text = $"<b>PREV</b>";
                    saveToBtns[i].tooltip = $"Return to previous view";
                    saveToBtns[i].style.backgroundColor = PREV_BTN_COLOR;
                    saveToBtns[i].style.color = Color.yellow;
                    saveToBtns[i].clicked += () =>
                    {
                        if (!SceneViewBookmarkManager.HasBookmark(index)) return;
                        SceneViewBookmarkManager.MoveToBookmark(index);
                    };
                }


                scroll.Add(btn);
            }

            // subscribe to events
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            SceneViewBookmarkManager.ClearBookmarksEvent += OnClearBookmarks;
            SceneViewBookmarkManager.MoveToBookMarkEvent += OnMoveToBookmark;
            SceneViewBookmarkManager.SetBookmarkEvent += OnSetBookmark;
        }

        private void OnActiveSceneChanged(Scene current, Scene next)
        {
            for (int i = 0; i <= SceneViewBookmarkManager.maxBookmarkCount; i++)
            {
                if (SceneViewBookmarkManager.HasBookmark(i))
                {
                    var bookmark = SceneViewBookmarkManager.ReadFromEditorPrefs(i);
                    var preview = bookmark.GetSceneViewScreenShot() ?? SceneViewBookmarkManager.SceneViewBookmarkIcon;
                    moveToBtns[i].style.backgroundImage = new StyleBackground(preview);
                    moveToBtns[i].text = bookmark.in2DMode ? "2D" : "";
                }
                else
                {
                    moveToBtns[i].style.backgroundImage = new StyleBackground(SceneViewBookmarkManager.SceneViewEmptyIcon);
                    moveToBtns[i].text = "";
                }
            }

            moveToBtns[prevIndex].style.borderTopColor = Color.grey;
            prevIndex = 0;
        }

        private void OnClearBookmarks()
        {
            for (int i = 0; i <= SceneViewBookmarkManager.maxBookmarkCount; i++)
            {
                moveToBtns[i].style.backgroundImage = new StyleBackground(SceneViewBookmarkManager.SceneViewEmptyIcon);
                moveToBtns[i].text = "";
            }

            moveToBtns[prevIndex].style.borderTopColor = Color.grey;
            prevIndex = 0;
        }

        private void OnMoveToBookmark(int index)
        {
            moveToBtns[prevIndex].style.borderTopColor = Color.grey;
            moveToBtns[index].style.borderTopColor = Color.yellow;
            prevIndex = index;
        }

        private void OnSetBookmark(int index, SceneViewCameraBookmark bookmark, Texture2D preview)
        {
            // destroy previous texture
            var prevTexture = moveToBtns[index].style.backgroundImage.value.texture;
            if (prevTexture != SceneViewBookmarkManager.SceneViewBookmarkIcon && prevTexture != SceneViewBookmarkManager.SceneViewEmptyIcon)
            {
                UnityEngine.Object.DestroyImmediate(prevTexture);
            }

            // assign new preview texture
            preview ??= SceneViewBookmarkManager.SceneViewBookmarkIcon;
            moveToBtns[index].style.backgroundImage = new StyleBackground(preview);
            moveToBtns[index].text = bookmark.in2DMode ? "2D" : "";
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();

            // unsubscribe events
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            SceneViewBookmarkManager.ClearBookmarksEvent -= OnClearBookmarks;
            SceneViewBookmarkManager.MoveToBookMarkEvent -= OnMoveToBookmark;
            SceneViewBookmarkManager.SetBookmarkEvent -= OnSetBookmark;

            // unload unused assets
            // EditorUtility.UnloadUnusedAssetsImmediate();
        }

        public override VisualElement CreatePanelContent()
        {
            return rootVisualElement;
        }
    }
}
#endif
