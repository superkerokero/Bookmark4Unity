namespace Bookmark4Unity.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bookmark4Unity.Guid;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    [Serializable]
    public class SceneObjectCollection
    {
        public string name;
        public string scene;
        public List<GuidData> datas;

        public SceneObjectCollection(string name, string scene, List<GuidData> datas)
        {
            this.name = name;
            this.scene = scene;
            this.datas = datas;
        }
    }

    public class SceneObjectReferenceCollection
    {
        public string name;
        public string scene;
        public List<GuidReference> references;

        public SceneObjectReferenceCollection(string name, string scene, List<GuidReference> references)
        {
            this.name = name;
            this.scene = scene;
            this.references = references;
        }

        public SceneObjectReferenceCollection(SceneObjectCollection collection)
        {
            this.name = collection.name;
            this.scene = collection.scene;
            this.references = collection.datas.Select(data => new GuidReference(data)).ToList();
        }

        public SceneObjectCollection ToData()
        {
            return new SceneObjectCollection(
                name,
                scene,
                references.Select(reference => reference.ToData()).ToList()
            );
        }
    }

    public class SceneObjectCollectionListView
    {
        public event Action ChangeEvent;
        public List<SceneObjectReferenceCollection> Data { get; private set; }
        public ListView DataListView { get; private set; }
        public const int ItemHeight = 15;
        public bool IsEmpty => Data.Count < 1;
        public Dictionary<int, EventCallback<ClickEvent>> pingActions = new();
        public Dictionary<int, EventCallback<ClickEvent>> focusActions = new();
        public Dictionary<int, EventCallback<ClickEvent>> delActions = new();
        public Dictionary<int, EventCallback<PointerLeaveEvent>> dragActions = new();

        public SceneObjectCollectionListView(List<SceneObjectReferenceCollection> data, VisualTreeAsset btnAsset)
        {
            Data = data;
            DataListView = new(Data, ItemHeight, () =>
            {
                return btnAsset.Instantiate();
            },
            (item, i) =>
            {
                var icon = item.Q<Button>("Icon");
                var btn = item.Q<Button>("Btn");
                var focus = item.Q<Button>("Focus");
                var del = item.Q<Button>("Del");
                var data = Data[i];
                var index = i; // save value for lambda functions
                icon.style.backgroundImage = Background.FromTexture2D(
                    data.references.Any(reference => reference.gameObject is null) ?
                    EditorGUIUtility.IconContent("console.warnicon").image as Texture2D :
                    EditorGUIUtility.IconContent("d_LODGroup Icon").image as Texture2D);
                focus.style.backgroundImage = Background.FromTexture2D(SceneViewBookmarkManager.SceneViewBookmarkIcon);
                del.style.backgroundImage = Background.FromTexture2D(SceneViewBookmarkManager.SceneViewEmptyIcon);
                btn.text = data.name;

                // ping
                if (pingActions.ContainsKey(i)) btn.UnregisterCallback<ClickEvent>(pingActions[i]);
                pingActions[i] = evt => Ping(index);
                btn.RegisterCallback<ClickEvent>(pingActions[i]);
                btn.tooltip = $"Select scene objects collection \"{data.name}\"";

                // focus
                if (focusActions.ContainsKey(i)) focus.UnregisterCallback<ClickEvent>(focusActions[i]);
                focusActions[i] = evt => Focus(index);
                focus.RegisterCallback<ClickEvent>(focusActions[i]);
                focus.tooltip = $"Focus on scene objects collection \"{data.name}\"";

                // del
                if (delActions.ContainsKey(i)) del.UnregisterCallback<ClickEvent>(delActions[i]);
                delActions[i] = evt => Remove(index);
                del.RegisterCallback<ClickEvent>(delActions[i]);
                del.tooltip = $"Unpin scene objects collection \"{data.name}\"";

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
            DataListView.style.display = IsEmpty ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public bool Add(SceneObjectReferenceCollection data)
        {
            if (Data.Contains(data)) return false;
            Data.Add(data);
            DataListView.Rebuild();
            ChangeEvent?.Invoke();
            DataListView.style.display = DisplayStyle.Flex;
            return true;
        }

        public void Ping(int index)
        {
            if (Data[index].references.Any(reference => reference.gameObject is null))
            {
                if (EditorUtility.DisplayDialog(Data[index].name, "Selected game object collection contains invalid references, remove it from list?", "Yes", "No"))
                {
                    Remove(index);
                }
            }
            else
            {
                Selection.objects = Data[index].references.Select(reference => reference.gameObject).ToArray();
            }
        }

        public void Focus(int index)
        {
            if (Data[index].references.Any(reference => reference.gameObject is null))
            {
                if (EditorUtility.DisplayDialog(Data[index].name, "Selected game object collection contains invalid references, remove it from list?", "Yes", "No"))
                {
                    Remove(index);
                }
            }
            else
            {
                Selection.objects = Data[index].references.Select(reference => reference.gameObject).ToArray();
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        public void Remove(int index)
        {
            // foreach (var reference in Data[index].references)
            // {
            //     if (reference.gameObject is not null)
            //     {
            //         UnityEngine.Object.DestroyImmediate(reference.gameObject.GetComponent<GuidComponent>());
            //     }
            // }

            Data.RemoveAt(index);
            DataListView.Rebuild();
            ChangeEvent?.Invoke();
            if (IsEmpty) DataListView.style.display = DisplayStyle.None;
            Bookmark4UnityWindow.UpdateSavedData();
        }

        public void RemoveAll()
        {
            // for (int i = 0; i < Data.Count; i++)
            // {
            //     foreach (var reference in Data[i].references)
            //     {
            //         if (reference.gameObject is not null)
            //         {
            //             UnityEngine.Object.DestroyImmediate(reference.gameObject.GetComponent<GuidComponent>());
            //         }
            //     }
            // }

            Data.Clear();
            DataListView.Rebuild();
            DataListView.style.display = DisplayStyle.None;
        }

        private void OnDrag(int index)
        {
            if (Event.current.type != EventType.MouseDrag || Data[index].references.Any(reference => reference.gameObject is null)) return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = Data[index].references.Select(reference => reference.gameObject).ToArray();
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

        public IEnumerable<SceneObjectCollection> ToData()
        {
            return Data.Select(reference => reference.ToData());
        }
    }
}