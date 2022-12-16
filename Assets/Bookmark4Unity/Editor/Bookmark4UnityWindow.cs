namespace Bookmark4Unity.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bookmark4Unity.Guid;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

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

    public class Bookmark4UnityWindow : EditorWindow, IHasCustomMenu
    {
        [System.Serializable]
        public class DataWrapper
        {
            public List<GuidData> references = new();
            public List<AssetData> assets = new();
        }

        [System.Serializable]
        public class AssetData : IEquatable<AssetData>
        {
            public string guid;
            public string path;
            public string name;
            public string type;

            public override bool Equals(object obj) => this.Equals(obj as AssetData);

            public bool Equals(AssetData other)
            {
                if (other is null) return false;
                if (System.Object.ReferenceEquals(this, other)) return true;
                return this.guid == other.guid;
            }

            public override int GetHashCode() => guid.GetHashCode();

            public static bool operator ==(AssetData lhs, AssetData rhs)
            {
                if (lhs is null)
                {
                    if (rhs is null) return true;
                    return false;
                }

                return lhs.Equals(rhs);
            }

            public static bool operator !=(AssetData lhs, AssetData rhs) => !(lhs == rhs);
        }

        [SerializeField]
        Dictionary<string, List<AssetData>> _assetsDataEntries = null;
        Dictionary<string, List<AssetData>> AssetsDataEntries
        {
            get
            {
                if (_assetsDataEntries == null)
                {
                    LoadData();
                }

                return _assetsDataEntries;
            }
        }

        [SerializeField]
        Dictionary<string, List<GuidReference>> _guidDataEntries = null;
        Dictionary<string, List<GuidReference>> GuidDataEntries
        {
            get
            {
                if (_guidDataEntries == null)
                {
                    LoadData();
                }

                return _guidDataEntries;
            }
        }


        private bool sceneObjectFoldout = true;
        private bool assetsFoldout = true;
        private Vector2 scrollPosSceneObj = Vector2.zero;
        private Vector2 scrollPosAssets = Vector2.zero;

        private const string Name = "Bookmark4Unity";
        private static string Prefix => Application.productName + "_BOOKMARK4UNITY_";
        private static string PinnedKey => Prefix + "pinned";
        private static string SceneObjectsFoldoutKey => Prefix + "sceneObjects";
        private static string AssetsFoldoutKey => Prefix + "assets";



        [MenuItem("Window/Bookmark4Unity")]
        public static void ShowWindow()
        {
            GetWindow<Bookmark4UnityWindow>(Name);
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Save Collections"), false, SaveDataToFile);
            menu.AddItem(new GUIContent("Load Collections"), false, LoadDataFromFile);
        }

        public void OnGUI()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button(new GUIContent("▼", "Sort by name"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    foreach (var list in _guidDataEntries.Values)
                    {
                        list.Sort(GuidReferenceComparer);
                    }

                    foreach (var list in _assetsDataEntries.Values)
                    {
                        list.Sort(AssetDataComparer);
                    }
                }

                using (new BackgroundColorScope(Color.yellow))
                {
                    if (GUILayout.Button(new GUIContent("Pin Selected", "Shortcut: Alt+Ctrl+A/Alt+Cmd+A"), EditorStyles.miniButton))
                    {
                        PinSelected();
                        LoadData();
                        Repaint();
                    }
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                // scene objects
                sceneObjectFoldout = EditorGUILayout.Foldout(sceneObjectFoldout, "Scene Objects");
                if (sceneObjectFoldout)
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(scrollPosSceneObj))
                    {
                        scrollPosSceneObj = scrollView.scrollPosition;
                        foreach (var data in GuidDataEntries)
                        {
                            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                            {
                                GUILayout.Label(data.Key);
                                foreach (var guidData in data.Value)
                                {
                                    if (DrawSceneObjectData(guidData)) return;
                                }
                            }
                        }
                    }
                }

                if (check.changed) EditorPrefs.SetBool(SceneObjectsFoldoutKey, sceneObjectFoldout);
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                // assets list
                assetsFoldout = EditorGUILayout.Foldout(assetsFoldout, "Assets");
                if (assetsFoldout)
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(scrollPosAssets))
                    {
                        scrollPosAssets = scrollView.scrollPosition;
                        foreach (var data in AssetsDataEntries)
                        {
                            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                            {
                                GUILayout.Label(data.Key);
                                foreach (var assetData in data.Value)
                                {
                                    if (DrawAssetData(assetData)) return;
                                }
                            }
                        }
                    }
                }

                if (check.changed) EditorPrefs.SetBool(AssetsFoldoutKey, assetsFoldout);
            }
        }

        private bool DrawSceneObjectData(GuidReference reference)
        {
            var gameObject = reference.gameObject;
            using (new GUILayout.HorizontalScope())
            {
                var style = EditorStyles.miniButtonLeft;
                style.alignment = TextAnchor.MiddleLeft;
                using (new EditorGUIUtility.IconSizeScope(Vector2.one * 16))
                {
                    var icon = gameObject == null ? EditorGUIUtility.IconContent("console.warnicon").image : PrefabUtility.GetIconForGameObject(gameObject);
                    if (GUILayout.Button(new GUIContent(" " + reference.CachedName, icon, reference.CachedSceneName), style, GUILayout.Height(18)))
                    {
                        if (gameObject != null)
                        {
                            Selection.activeGameObject = gameObject;
                        }
                        else
                        {
                            if (EditorUtility.DisplayDialog(Name, "Selected game object dows not exist on current scene, remove it from list?", "Yes", "No"))
                            {
                                RemovePin(reference);
                                return true;
                            }
                        }
                    }
                }

                using (new BackgroundColorScope(Color.red))
                {
                    if (GUILayout.Button(new GUIContent("☓", "Un-pin"), GUILayout.ExpandWidth(false)))
                    {
                        RemovePin(reference);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool DrawAssetData(AssetData assetData)
        {
            using (new GUILayout.HorizontalScope())
            {
                var style = EditorStyles.miniButtonLeft;
                style.alignment = TextAnchor.MiddleLeft;
                using (new EditorGUIUtility.IconSizeScope(Vector2.one * 16))
                {
                    if (GUILayout.Button(new GUIContent(" " + assetData.name, AssetDatabase.GetCachedIcon(assetData.path), assetData.path), style, GUILayout.Height(18)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }

                using (new BackgroundColorScope(Color.green))
                {
                    if (GUILayout.Button(new GUIContent("⎋", "Open asset"), GUILayout.ExpandWidth(false)))
                    {
                        if (!Path.GetExtension(assetData.path).Equals(".unity"))
                        {
                            // EditorUtility.OpenWithDefaultApp(assetData.path);
                            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                            AssetDatabase.OpenAsset(asset);
                        }
                        else
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(assetData.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                        }
                    }
                }

                using (new BackgroundColorScope(Color.red))
                {
                    if (GUILayout.Button(new GUIContent("☓", "Un-pin"), GUILayout.ExpandWidth(false)))
                    {
                        RemovePin(assetData);
                        return true;
                    }
                }
            }

            return false;
        }

        public static void PinSelected()
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
                if (!data.references.Contains(guidData)) data.references.Add(guidData);
            }

            // add assets
            foreach (string assetGUID in Selection.assetGUIDs)
            {
                var assetData = new AssetData();
                assetData.guid = assetGUID;
                assetData.path = AssetDatabase.GUIDToAssetPath(assetGUID);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                assetData.name = asset.name;
                assetData.type = asset.GetType().ToString();
                if (!data.assets.Contains(assetData)) data.assets.Add(assetData);
            }

            SaveData(data);
        }

        private DataWrapper GetCurrentData()
        {
            var data = new DataWrapper();

            foreach (var item in GuidDataEntries.Values)
            {
                data.references.AddRange(item.Select(reference => reference.ToData()));
            }

            foreach (var item in AssetsDataEntries.Values)
            {
                data.assets.AddRange(item);
            }

            return data;
        }

        public void LoadData(DataWrapper data)
        {
            _guidDataEntries.Clear();
            _assetsDataEntries.Clear();

            foreach (var reference in data.references)
            {
                if (_guidDataEntries.ContainsKey(reference.cachedScene))
                {
                    _guidDataEntries[reference.cachedScene].Add(new GuidReference(reference));
                }
                else
                {
                    _guidDataEntries[reference.cachedScene] = new List<GuidReference>() { new GuidReference(reference) };
                }
            }

            foreach (var asset in data.assets)
            {
                if (_assetsDataEntries.ContainsKey(asset.type))
                {
                    _assetsDataEntries[asset.type].Add(asset);
                }
                else
                {
                    _assetsDataEntries[asset.type] = new List<AssetData>() { asset };
                }
            }
        }

        public static void SaveData(DataWrapper data)
        {
            EditorPrefs.SetString(PinnedKey, JsonUtility.ToJson(data));
        }

        public void LoadData()
        {
            _guidDataEntries = new Dictionary<string, List<GuidReference>>();
            _assetsDataEntries = new Dictionary<string, List<AssetData>>();

            if (EditorPrefs.HasKey(PinnedKey))
            {
                var data = JsonUtility.FromJson<DataWrapper>(EditorPrefs.GetString(PinnedKey));
                LoadData(data);
            }

            if (EditorPrefs.HasKey(SceneObjectsFoldoutKey)) sceneObjectFoldout = EditorPrefs.GetBool(SceneObjectsFoldoutKey);
            if (EditorPrefs.HasKey(AssetsFoldoutKey)) assetsFoldout = EditorPrefs.GetBool(AssetsFoldoutKey);
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

            SaveData(GetCurrentData());
        }

        private void RemovePin(GuidReference reference)
        {
            _guidDataEntries[reference.CachedSceneName].Remove(reference);
            if (_guidDataEntries[reference.CachedSceneName].Count == 0) _guidDataEntries.Remove(reference.CachedSceneName);
            if (reference.gameObject != null) DestroyImmediate(reference.gameObject.GetComponent<GuidComponent>());

            SaveData(GetCurrentData());
        }

        private void RemovePin(AssetData assetData)
        {
            _assetsDataEntries[assetData.type].Remove(assetData);
            if (_assetsDataEntries[assetData.type].Count == 0) _assetsDataEntries.Remove(assetData.type);
            SaveData(GetCurrentData());
        }

        private int GuidReferenceComparer(GuidReference left, GuidReference right)
        {
            if (left.gameObject == null || right.gameObject == null) return 0;
            return left.gameObject.name.CompareTo(right.gameObject.name);
        }

        private int AssetDataComparer(AssetData left, AssetData right)
        {
            return left.name.CompareTo(right.name);
        }

        [MenuItem("Assets/Pin Selected To Fav. Assets %&a")]
        public static void PinSelectedToCollection()
        {
            Bookmark4UnityWindow.PinSelected();
            if (EditorWindow.HasOpenInstances<Bookmark4UnityWindow>())
            {
                var window = GetWindow<Bookmark4UnityWindow>(Name);
                window.LoadData();
                window.Repaint();
            }
        }
    }
}
