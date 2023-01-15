namespace Bookmark4Unity.Editor
{
    using System;
    using System.Collections.Generic;
    using Bookmark4Unity.Guid;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SceneObjectBookmarkGroup
    {
        public VisualElement Element { get; private set; }
        public Foldout Root { get; private set; }
        public SceneObjectListView SceneObjListView { get; private set; }
        public SceneObjectCollectionListView SceneObjCollectionListView { get; private set; }


        public SceneObjectBookmarkGroup(string groupName, Color borderColor, List<GuidReference> objReferenceData, List<SceneObjectReferenceCollection> objCollectionData, VisualTreeAsset groupAsset, VisualTreeAsset btnAsset)
        {
            Element = groupAsset.Instantiate();
            Root = Element.Q<Foldout>("Root");
            Root.style.borderTopColor = borderColor;
            Root.style.borderBottomColor = borderColor;
            Root.style.borderLeftColor = borderColor;
            Root.style.borderRightColor = borderColor;
            Root.text = groupName;
            SceneObjListView = new SceneObjectListView(objReferenceData, btnAsset);
            SceneObjListView.ChangeEvent += OnListViewChange;
            SceneObjCollectionListView = new SceneObjectCollectionListView(objCollectionData, btnAsset);
            SceneObjCollectionListView.ChangeEvent += OnListViewChange;
            Root.Add(SceneObjCollectionListView.DataListView);
            Root.Add(SceneObjListView.DataListView);
        }

        public bool AddSceneObject(GuidReference data)
        {
            return SceneObjListView.Add(data);
        }

        public bool AddSceneObjectCollection(SceneObjectReferenceCollection data)
        {
            return SceneObjCollectionListView.Add(data);
        }

        public void RemoveAll()
        {
            SceneObjListView.RemoveAll();
            SceneObjCollectionListView.RemoveAll();
            Element.AddToClassList(Bookmark4UnityWindow.HiddenContentClassName);
            Bookmark4UnityWindow.UpdateSavedData();
        }

        public void SortDesc()
        {
            SceneObjListView.SortDesc();
            SceneObjCollectionListView.SortDesc();
        }

        public void SortAsc()
        {
            SceneObjListView.SortAsc();
            SceneObjCollectionListView.SortAsc();
        }

        public void Refresh()
        {
            SceneObjListView.DataListView.RefreshItems();
            SceneObjCollectionListView.DataListView.RefreshItems();
        }

        private void OnListViewChange()
        {
            if (SceneObjListView.IsEmpty && SceneObjCollectionListView.IsEmpty)
            {
                Element.AddToClassList(Bookmark4UnityWindow.HiddenContentClassName);
            }
            else
            {
                Element.RemoveFromClassList(Bookmark4UnityWindow.HiddenContentClassName);
            }
        }
    }
}