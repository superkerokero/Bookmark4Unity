namespace Bookmark4Unity.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bookmark4Unity.Guid;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;
    using Scene = UnityEngine.SceneManagement.Scene;

    public class Bookmark4UnityWindow : EditorWindow, IHasCustomMenu
    {
        [System.Serializable]
        public class DataWrapper
        {
            public List<GuidData> references = new();
            public List<AssetData> assets = new();
        }

        private readonly Dictionary<string, AssetBookmarkGroup> assetBookmarkGroups = new();
        private readonly Dictionary<string, SceneObjectBookmarkGroup> sceneObjectBookmarkGroups = new();

        public const string Name = "Bookmark4Unity";
        public static string Prefix => Application.productName + "_BOOKMARK4UNITY_";
        private static string PinnedKey => Prefix + "pinned";
        private static string IsAssetTabActiveKey => Prefix + "isAssetTabActive";

        private const string UXML_GUID_BookmarkWindow = "7789041336e00410f91f040d6e09f772";
        private const string USS_GUID_BookmarkWindow = "c2575018492804a408595d8f9445083b";
        private const string UXML_GUID_BookmarkGroup = "8898fc16a31fe4cd7887b75843e59563";
        private const string UXML_GUID_AssetBookmarkBtn = "71961e69d456347bd93e543583938f3c";
        private const string UXML_GUID_SceneObjectBookmarkBtn = "95c8e9370e2d54333a3bb77afd33e6a7";

        private VisualTreeAsset bookmarkGroupUxml;
        private VisualTreeAsset assetBtnUxml;
        private VisualTreeAsset sceneObjBtnUxml;

        private const string currentlySelectedTabClassName = "currentlySelectedTab";
        public const string HiddenContentClassName = "unselectedContent";

        private Button assetTab;
        private Button sceneObjTab;
        private ScrollView assetScrollView;
        private ScrollView sceneObjScrollView;


        [MenuItem("Tools/Bookmark4Unity/Open Bookmark Window")]
        public static void ShowMyEditor()
        {
            GetWindow<Bookmark4UnityWindow>(Name);
        }

        private void CreateGUI()
        {
            // title
            titleContent = new GUIContent(Name);

            // apply uxml & uss
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(UXML_GUID_BookmarkWindow));
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(USS_GUID_BookmarkWindow));
            bookmarkGroupUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(UXML_GUID_BookmarkGroup));
            assetBtnUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(UXML_GUID_AssetBookmarkBtn));
            sceneObjBtnUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(UXML_GUID_SceneObjectBookmarkBtn));
            rootVisualElement.Add(uxml.Instantiate());
            rootVisualElement.styleSheets.Add(uss);

            // query element references
            var pinBtn = rootVisualElement.Q<Button>("PinBtn");
            var sortBtnDesc = rootVisualElement.Q<Button>("SortBtnDesc");
            var sortBtnAsc = rootVisualElement.Q<Button>("SortBtnAsc");
            var saveBtn = rootVisualElement.Q<Button>("SaveBtn");
            var loadBtn = rootVisualElement.Q<Button>("LoadBtn");
            assetTab = rootVisualElement.Q<Button>("AssetTab");
            sceneObjTab = rootVisualElement.Q<Button>("SceneObjTab");
            assetScrollView = rootVisualElement.Q<ScrollView>("AssetScrollView");
            sceneObjScrollView = rootVisualElement.Q<ScrollView>("SceneObjScrollView");

            // register callbacks
            pinBtn.RegisterCallback<ClickEvent>(PinSelected);
            sortBtnDesc.RegisterCallback<ClickEvent>(_ =>
            {
                foreach (var group in assetBookmarkGroups.Values)
                {
                    group.SortDesc();
                }

                foreach (var group in sceneObjectBookmarkGroups.Values)
                {
                    group.SortDesc();
                }
            });
            sortBtnAsc.RegisterCallback<ClickEvent>(_ =>
            {
                foreach (var group in assetBookmarkGroups.Values)
                {
                    group.SortAsc();
                }

                foreach (var group in sceneObjectBookmarkGroups.Values)
                {
                    group.SortAsc();
                }
            });
            saveBtn.RegisterCallback<ClickEvent>(_ => SaveDataToFile());
            loadBtn.RegisterCallback<ClickEvent>(_ => LoadDataFromFile());
            assetTab.RegisterCallback<ClickEvent>(ActivateAssetTab);
            sceneObjTab.RegisterCallback<ClickEvent>(ActivateSceneObjTab);

            // bookmark groups
            LoadData();

            // register scene change handler
            EditorSceneManager.sceneOpened += OnSceneLoaded;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        private void OnDestroy()
        {
            EditorSceneManager.sceneOpened -= OnSceneLoaded;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            SaveData();
        }

        private void OnSceneLoaded(Scene scene, OpenSceneMode mode)
        {
            UpdateSceneObjectFoldoutStatus();
        }

        private void OnSceneClosed(Scene scene)
        {
            UpdateSceneObjectFoldoutStatus();
        }

        private void UpdateSceneObjectFoldoutStatus()
        {
            var opendScenes = new HashSet<string>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                opendScenes.Add(EditorSceneManager.GetSceneAt(i).name);
            }
            foreach (var group in sceneObjectBookmarkGroups.Values)
            {
                group.DataListView.RefreshItems();
                group.Root.value = opendScenes.Contains(group.Root.text);
            }
        }

        private void ActivateAssetTab(ClickEvent evt = null)
        {
            assetTab.AddToClassList(currentlySelectedTabClassName);
            assetScrollView.RemoveFromClassList(HiddenContentClassName);
            sceneObjTab.RemoveFromClassList(currentlySelectedTabClassName);
            sceneObjScrollView.AddToClassList(HiddenContentClassName);
        }

        private void ActivateSceneObjTab(ClickEvent evt = null)
        {
            assetTab.RemoveFromClassList(currentlySelectedTabClassName);
            assetScrollView.AddToClassList(HiddenContentClassName);
            sceneObjTab.AddToClassList(currentlySelectedTabClassName);
            sceneObjScrollView.RemoveFromClassList(HiddenContentClassName);
        }

        /// <summary>
        /// Implement IHasCustomMenu interface
        /// </summary>
        /// <param name="menu">menu</param>
        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Save Collections"), false, SaveDataToFile);
            menu.AddItem(new GUIContent("Load Collections"), false, LoadDataFromFile);
        }

        public void PinSelected(ClickEvent evt = null)
        {
            if (Selection.activeTransform is null)
            {
                // add assets
                foreach (string assetGUID in Selection.assetGUIDs)
                {
                    var assetData = new AssetData
                    {
                        guid = assetGUID,
                        path = AssetDatabase.GUIDToAssetPath(assetGUID)
                    };
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                    assetData.name = asset.name;
                    assetData.type = asset.GetType().ToString();
                    if (assetBookmarkGroups.ContainsKey(assetData.type))
                    {
                        if (assetBookmarkGroups[assetData.type].Add(assetData))
                        {
                            Debug.Log($"<color=green><b>{Bookmark4UnityWindow.Name}</b></color>: <color=yellow><b>{assetData.type}</b></color> asset <color=red><b>{assetData.path}</b></color> bookmarked.");
                        }
                    }
                    else
                    {
                        var group = new AssetBookmarkGroup(
                            assetData.type,
                            Random.ColorHSV(0f, 1f, 0.65f, 0.65f, 1f, 1f),
                            new() { assetData },
                            bookmarkGroupUxml,
                            assetBtnUxml);
                        assetBookmarkGroups[assetData.type] = group;
                        assetScrollView.Add(group.Element);
                        Debug.Log($"<color=green><b>{Bookmark4UnityWindow.Name}</b></color>: <color=yellow><b>{assetData.type}</b></color> asset <color=red><b>{assetData.path}</b></color> bookmarked.");
                    }
                }

                if (Selection.assetGUIDs.Length > 0)
                {
                    ActivateAssetTab();
                    SaveData();
                }
            }
            else
            {
                // add scene objects
                foreach (var trans in Selection.transforms)
                {
                    var gameObj = trans.gameObject;
                    var guidComponent = gameObj.GetComponent<GuidComponent>();
                    if (guidComponent == null) guidComponent = gameObj.AddComponent<GuidComponent>();
                    var reference = new GuidReference(guidComponent);
                    if (sceneObjectBookmarkGroups.ContainsKey(reference.CachedSceneName))
                    {
                        if (sceneObjectBookmarkGroups[reference.CachedSceneName].Add(reference))
                        {
                            Debug.Log($"<color=green><b>{Bookmark4UnityWindow.Name}</b></color>: [<color=yellow><b>{reference.CachedSceneName}</b></color>] Scene object <color=red><b>{reference.CachedName}</b></color> bookmarked.");
                        }
                    }
                    else
                    {
                        var group = new SceneObjectBookmarkGroup(
                            reference.CachedSceneName,
                            Random.ColorHSV(0f, 1f, 0.65f, 0.65f, 1f, 1f),
                            new() { reference },
                            bookmarkGroupUxml,
                            sceneObjBtnUxml);
                        sceneObjectBookmarkGroups[reference.CachedSceneName] = group;
                        sceneObjScrollView.Add(group.Element);
                        Debug.Log($"<color=green><b>{Bookmark4UnityWindow.Name}</b></color>: [<color=yellow><b>{reference.CachedSceneName}</b></color>] Scene object <color=red><b>{reference.CachedName}</b></color> bookmarked.");
                    }
                }

                if (Selection.transforms.Length > 0)
                {
                    ActivateSceneObjTab();
                    SaveData();
                }
            }
        }

        private DataWrapper GetCurrentData()
        {
            var data = new DataWrapper();
            foreach (var group in sceneObjectBookmarkGroups.Values)
            {
                data.references.AddRange(group.Data.Select(reference => reference.ToData()));
            }

            foreach (var group in assetBookmarkGroups.Values)
            {
                data.assets.AddRange(group.Data);
            }

            return data;
        }

        public void LoadData(DataWrapper data)
        {
            // update asset bookmark groups
            foreach (var asset in data.assets)
            {
                if (assetBookmarkGroups.ContainsKey(asset.type))
                {
                    assetBookmarkGroups[asset.type].Add(asset);
                }
                else
                {
                    var group = new AssetBookmarkGroup(
                        asset.type,
                        Random.ColorHSV(0f, 1f, 0.65f, 0.65f, 1f, 1f),
                        new() { asset },
                        bookmarkGroupUxml,
                        assetBtnUxml);
                    assetBookmarkGroups[asset.type] = group;
                    assetScrollView.Add(group.Element);
                }
            }

            // update scene object bookmark groups
            foreach (var reference in data.references)
            {
                if (sceneObjectBookmarkGroups.ContainsKey(reference.cachedScene))
                {
                    sceneObjectBookmarkGroups[reference.cachedScene].Add(new GuidReference(reference));
                }
                else
                {
                    var group = new SceneObjectBookmarkGroup(
                        reference.cachedScene,
                        Random.ColorHSV(0f, 1f, 0.65f, 0.65f, 1f, 1f),
                        new() { new GuidReference(reference) },
                        bookmarkGroupUxml,
                        sceneObjBtnUxml);
                    sceneObjectBookmarkGroups[reference.cachedScene] = group;
                    sceneObjScrollView.Add(group.Element);
                }
            }

            UpdateSceneObjectFoldoutStatus();
        }

        public void SaveData()
        {
            SaveData(GetCurrentData());
            EditorPrefs.SetBool(IsAssetTabActiveKey, assetTab.ClassListContains(currentlySelectedTabClassName));
        }

        public static void SaveData(DataWrapper data)
        {
            EditorPrefs.SetString(PinnedKey, JsonUtility.ToJson(data));
        }

        public void LoadData()
        {
            if (EditorPrefs.HasKey(PinnedKey))
            {
                var data = JsonUtility.FromJson<DataWrapper>(EditorPrefs.GetString(PinnedKey));
                LoadData(data);
            }

            if (EditorPrefs.HasKey(IsAssetTabActiveKey))
            {
                var state = EditorPrefs.GetBool(IsAssetTabActiveKey);
                if (state)
                {
                    ActivateAssetTab();
                }
                else
                {
                    ActivateSceneObjTab();
                }
            }
            else
            {
                ActivateAssetTab();
            }
        }

        private void SaveDataToFile()
        {
            var path = EditorUtility.SaveFilePanel("Bookmark4Unity", ".", "", "dat");
            if (path == "") return;

            var data = GetCurrentData();
            using (var dataStream = new FileStream(path, FileMode.Create))
            {
                var converter = new BinaryFormatter();
                converter.Serialize(dataStream, data);
            }
        }

        private void LoadDataFromFile()
        {
            var path = EditorUtility.OpenFilePanel("Bookmark4Unity", ".", "dat");
            if (path == "") return;

            using (var dataStream = new FileStream(path, FileMode.Open))
            {
                var converter = new BinaryFormatter();
                var data = converter.Deserialize(dataStream) as DataWrapper;
                LoadData(data);
            }
        }

        [MenuItem("Tools/Bookmark4Unity/Pin Selected %&a")]
        public static void PinSelectedToCollection()
        {
            if (EditorWindow.HasOpenInstances<Bookmark4UnityWindow>())
            {
                var window = GetWindow<Bookmark4UnityWindow>(Name);
                window.PinSelected();
            }
            else
            {
                // load data
                DataWrapper data;
                if (EditorPrefs.HasKey(PinnedKey))
                {
                    data = JsonUtility.FromJson<DataWrapper>(EditorPrefs.GetString(PinnedKey));
                }
                else
                {
                    data = new DataWrapper();
                }

                // add scene objects
                foreach (var trans in Selection.transforms)
                {
                    var gameObj = trans.gameObject;
                    var guidComponent = gameObj.GetComponent<GuidComponent>();
                    if (guidComponent == null) guidComponent = gameObj.AddComponent<GuidComponent>();
                    var guidReference = new GuidReference(guidComponent);
                    var guidData = guidReference.ToData();
                    if (!data.references.Contains(guidData))
                    {
                        data.references.Add(guidData);
                        Debug.Log($"<color=green><b>{Bookmark4UnityWindow.Name}</b></color>: [<color=yellow><b>{guidData.cachedScene}</b></color>] Scene object <color=red><b>{guidData.cachedName}</b></color> bookmarked.");
                    }
                }

                // add assets
                foreach (string assetGUID in Selection.assetGUIDs)
                {
                    var assetData = new AssetData
                    {
                        guid = assetGUID,
                        path = AssetDatabase.GUIDToAssetPath(assetGUID)
                    };
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                    assetData.name = asset.name;
                    assetData.type = asset.GetType().ToString();
                    if (!data.assets.Contains(assetData))
                    {
                        data.assets.Add(assetData);
                        Debug.Log($"<color=green><b>{Bookmark4UnityWindow.Name}</b></color>: <color=yellow><b>{assetData.type}</b></color> asset <color=red><b>{assetData.path}</b></color> bookmarked.");
                    }
                }

                SaveData(data);
            }
        }
    }
}
