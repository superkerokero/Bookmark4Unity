#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SearchService;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bookmark4Unity.Editor
{
    [Overlay(typeof(SceneView), "Bookmark4Unity")]
    class SceneViewBookmarkToolbarOverlay : ToolbarOverlay
    {
        SceneViewBookmarkToolbarOverlay() : base(SceneViewBookmarkToggle.id) { }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class SceneViewBookmarkToggle : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public const string id = "SceneViewBookmarkToolbarToggle";

        public SceneViewBookmarkToggle()
        {
            dropdownClicked += ShowMenu;
            this.RegisterValueChangedCallback(OnValueChanged);
            text = "Toggle";
            icon = SceneViewBookmarkManager.SceneViewBookmarkIcon;
            tooltip = "Bookmark the scene view camera.";
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            SceneViewBookmarkManager.IsOverlayVisible = evt.newValue;
        }

        public EditorWindow containerWindow { get; set; }

        static void HandleMoveToBookmark(object userData)
        {
            var slot = (int)userData;
            SceneViewBookmarkManager.MoveToBookmark(slot);
        }

        static void HandleSetBookmark(object userData)
        {
            var slot = (int)userData;
            SceneViewBookmarkManager.SetBookmark(slot);
        }

        static void HandleClearAllBookmarks()
        {
            SceneViewBookmarkManager.ClearAllBookmarksForCurrentScene();
        }

        private void ShowMenu()
        {
            var menu = new GenericMenu();

            for (var slot = 1; slot <= SceneViewBookmarkManager.maxBookmarkCount; slot++)
            {
                var content = new GUIContent($"Move to Bookmark {slot} &{slot}");

                if (SceneViewBookmarkManager.HasBookmark(slot))
                {
                    menu.AddItem(content, false, HandleMoveToBookmark, slot);
                }
                else
                {
                    menu.AddDisabledItem(content);
                }
            }

            menu.AddSeparator(string.Empty);

            var returnToPreviousViewContent = new GUIContent("Return to Previous View &0");

            if (SceneViewBookmarkManager.HasPreviousView)
            {
                menu.AddItem(returnToPreviousViewContent, false, SceneViewBookmarkManager.ReturnToPreviousView);
            }
            else
            {
                menu.AddDisabledItem(returnToPreviousViewContent);
            }

            menu.AddSeparator(string.Empty);

            for (var slot = 1; slot <= SceneViewBookmarkManager.maxBookmarkCount; slot++)
            {
                menu.AddItem(new GUIContent($"Set Bookmark {slot} &#{slot}"), false, HandleSetBookmark, slot);
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent($"Clear All Bookmarks"), false, HandleClearAllBookmarks);

            menu.ShowAsContext();
        }
    }
}
#endif
