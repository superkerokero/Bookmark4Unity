namespace Bookmark4Unity.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

    public class Bookmark4UnityWindow : EditorWindow
    {
        [System.Serializable]
        public class DataWrapper
        {
            public List<GuidData> references = new();
            public List<AssetData> assets = new();
        }

        [System.Serializable]
        public class AssetData
        {
            public string guid;
            public string path;
            public string name;
            public string type;
        }

        private static string GetPrefix() { return Application.productName + "_BOOKMARK4UNITY_"; }

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


        [MenuItem("Window/Bookmark4Unity")]
        public static void ShowWindow()
        {
            GetWindow<Bookmark4UnityWindow>(Name);
        }

        public void OnGUI()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("▼", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
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
                    if (GUILayout.Button(new GUIContent("Pin Selected", "Shortcut: Alt+Cmd+A"), EditorStyles.miniButton))
                    {
                        PinSelected();
                    }
                }
            }

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

        private void PinSelected()
        {
            foreach (var trans in Selection.transforms)
            {
                var gameObj = trans.gameObject;
                var guidComponent = gameObj.GetComponent<GuidComponent>();
                if (guidComponent == null) guidComponent = gameObj.AddComponent<GuidComponent>();
                if (_guidDataEntries.ContainsKey(gameObj.scene.name))
                {
                    _guidDataEntries[gameObj.scene.name].Add(new GuidReference(guidComponent));
                }
                else
                {
                    _guidDataEntries[gameObj.scene.name] = new List<GuidReference>() { new GuidReference(guidComponent) };
                }
            }

            foreach (string assetGUID in Selection.assetGUIDs)
            {
                var assetData = new AssetData();
                assetData.guid = assetGUID;
                assetData.path = AssetDatabase.GUIDToAssetPath(assetGUID);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                assetData.name = asset.name;
                assetData.type = asset.GetType().ToString();
                if (_assetsDataEntries.ContainsKey(assetData.type))
                {
                    _assetsDataEntries[assetData.type].Add(assetData);
                }
                else
                {
                    _assetsDataEntries[assetData.type] = new List<AssetData>() { assetData };
                }
            }

            SaveData();
        }

        private void SaveData()
        {
            string key = GetPrefix() + "pinned";
            var data = new DataWrapper();

            foreach (var item in GuidDataEntries.Values)
            {
                data.references.AddRange(item.Select(reference => reference.ToData()));
            }

            foreach (var item in AssetsDataEntries.Values)
            {
                data.assets.AddRange(item);
            }

            EditorPrefs.SetString(key, JsonUtility.ToJson(data));
        }

        private void LoadData()
        {
            _guidDataEntries = new Dictionary<string, List<GuidReference>>();
            _assetsDataEntries = new Dictionary<string, List<AssetData>>();

            string key = GetPrefix() + "pinned";
            if (EditorPrefs.HasKey(key))
            {
                var data = JsonUtility.FromJson<DataWrapper>(EditorPrefs.GetString(key));

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
        }

        private void RemovePin(GuidReference reference)
        {
            _guidDataEntries[reference.CachedSceneName].Remove(reference);
            if (_guidDataEntries[reference.CachedSceneName].Count == 0) _guidDataEntries.Remove(reference.CachedSceneName);
            if (reference.gameObject != null) DestroyImmediate(reference.gameObject.GetComponent<GuidComponent>());

            SaveData();
        }

        private void RemovePin(AssetData assetData)
        {
            _assetsDataEntries[assetData.type].Remove(assetData);
            if (_assetsDataEntries[assetData.type].Count == 0) _assetsDataEntries.Remove(assetData.type);
            SaveData();
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
            if (EditorWindow.HasOpenInstances<Bookmark4UnityWindow>())
            {
                var window = GetWindow<Bookmark4UnityWindow>("★ Fav. Assets");
                window.PinSelected();
            }
            else
            {
                if (Selection.assetGUIDs.Count() == 0) return;
                string key = GetPrefix() + "pinned";
                if (EditorPrefs.HasKey(key))
                {
                    var data = JsonUtility.FromJson<DataWrapper>(EditorPrefs.GetString(key));

                    foreach (string assetGUID in Selection.assetGUIDs)
                    {
                        var assetData = new AssetData();
                        assetData.guid = assetGUID;
                        assetData.path = AssetDatabase.GUIDToAssetPath(assetGUID);
                        Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                        assetData.name = asset.name;
                        assetData.type = asset.GetType().ToString();
                        data.assets.Add(assetData);
                    }

                    EditorPrefs.SetString(key, JsonUtility.ToJson(data));
                }
            }
        }
    }
}
