namespace Bookmark4Unity.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class AssetBookmarkGroup
    {
        public VisualElement Element { get; private set; }
        public Foldout Root { get; private set; }
        public List<AssetData> Data { get; private set; }
        public ListView DataListView { get; private set; }
        public const int ItemHeight = 15;
        public bool IsEmpty => Data.Count < 1;
        public Dictionary<int, EventCallback<ClickEvent>> pingActions = new();
        public Dictionary<int, EventCallback<ClickEvent>> openActions = new();
        public Dictionary<int, EventCallback<ClickEvent>> delActions = new();
        public Dictionary<int, EventCallback<PointerLeaveEvent>> dragActions = new();


        public AssetBookmarkGroup(string groupName, Color borderColor, List<AssetData> data, VisualTreeAsset groupAsset, VisualTreeAsset btnAsset)
        {
            Element = groupAsset.Instantiate();
            Root = Element.Q<Foldout>("Root");
            Root.style.borderTopColor = borderColor;
            Root.style.borderBottomColor = borderColor;
            Root.style.borderLeftColor = borderColor;
            Root.style.borderRightColor = borderColor;
            Root.text = groupName;
            Data = data;

            DataListView = new(Data, ItemHeight, () =>
            {
                return btnAsset.Instantiate();
            },
            (item, i) =>
            {
                var icon = item.Q<Button>("Icon");
                var btn = item.Q<Button>("Btn");
                var open = item.Q<Button>("Open");
                var del = item.Q<Button>("Del");
                var data = Data[i];
                var index = i; // save value for lambda functions

                icon.style.backgroundImage = Background.FromTexture2D(
                    AssetDatabase.GetCachedIcon(data.path) is not Texture2D iconImage ?
                    EditorGUIUtility.IconContent("console.warnicon").image as Texture2D :
                    iconImage);
                del.style.backgroundImage = Background.FromTexture2D(SceneViewBookmarkManager.SceneViewEmptyIcon);
                btn.text = data.name;
                btn.tooltip = $"Ping \"{data.name}\"";

                // ping
                if (pingActions.ContainsKey(i)) btn.UnregisterCallback<ClickEvent>(pingActions[i]);
                pingActions[i] = evt => Ping(index);
                btn.RegisterCallback<ClickEvent>(pingActions[i]);

                // open
                if (openActions.ContainsKey(i)) open.UnregisterCallback<ClickEvent>(openActions[i]);
                openActions[i] = evt => Open(index);
                open.RegisterCallback<ClickEvent>(openActions[i]);
                open.tooltip = $"Open \"{data.name}\"";

                // del
                if (delActions.ContainsKey(i)) del.UnregisterCallback<ClickEvent>(delActions[i]);
                delActions[i] = evt => Remove(index);
                del.RegisterCallback<ClickEvent>(delActions[i]);
                del.tooltip = $"Unpin \"{data.name}\"";

                // drag
                if (dragActions.ContainsKey(i)) btn.UnregisterCallback<PointerLeaveEvent>(dragActions[i]);
                dragActions[i] = evt => OnDrag(index);
                btn.RegisterCallback<PointerLeaveEvent>(dragActions[i]);
            })
            {
                reorderable = true,
                showBorder = false
            };

            DataListView.style.flexGrow = 1f; // Fills the window
            Root.Add(DataListView);
        }

        public bool Add(AssetData data)
        {
            if (Data.Contains(data)) return false;
            Data.Add(data);
            DataListView.Rebuild();
            Element.RemoveFromClassList(Bookmark4UnityWindow.HiddenContentClassName);
            return true;
        }

        public void Ping(int index)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Data[index].path);

            if (asset is null)
            {
                if (EditorUtility.DisplayDialog(Data[index].name, $"Selected asset does not exist, remove it from list?\n\n\"{Data[index].path}\"", "Yes", "No"))
                {
                    Remove(index);
                }

                return;
            }

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        public void Open(int index)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Data[index].path);

            if (asset is null)
            {
                if (EditorUtility.DisplayDialog(Data[index].name, $"Selected asset does not exist, remove it from list?\n\n\"{Data[index].path}\"", "Yes", "No"))
                {
                    Remove(index);
                }

                return;
            }

            AssetDatabase.OpenAsset(asset);
        }

        public void Remove(int index)
        {
            Data.RemoveAt(index);
            DataListView.Rebuild();
            if (IsEmpty) Element.AddToClassList(Bookmark4UnityWindow.HiddenContentClassName);
            Bookmark4UnityWindow.UpdateSavedData();
        }

        public void RemoveAll()
        {
            Data.Clear();
            DataListView.Rebuild();
            Element.AddToClassList(Bookmark4UnityWindow.HiddenContentClassName);
            Bookmark4UnityWindow.UpdateSavedData();
        }

        private void OnDrag(int index)
        {
            if (Event.current.type != EventType.MouseDrag) return;
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Data[index].path);
            if (asset is null) return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { asset };
            DragAndDrop.StartDrag(Data[index].name);
            Event.current.Use();
        }

        public void SortDesc()
        {
            Data.Sort((a, b) => a.name.CompareTo(b.name));
            DataListView.RefreshItems();
        }

        public void SortAsc()
        {
            Data.Sort((a, b) => b.name.CompareTo(a.name));
            DataListView.RefreshItems();
        }
    }
}