using Bookmark4Unity.Guid;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace Bookmark4Unity.Editor
{
    static class SceneViewBookmarkSearchProvider
    {
        const string id = "scene-view-bookmarks";

        [SearchItemProvider]
        static SearchProvider CreateProvider()
        {
            var icon = SceneViewBookmarkManager.SceneViewBookmarkIcon;
            return new SearchProvider(id, "Bookmark4Unity: Scene View Bookmarks")
            {
                actions =
                {
                    new SearchAction(id, "Move to Bookmark", null, null, HandleMoveToBookmark),
                },
                fetchItems = (context, items, provider) =>
                {
                    if (int.TryParse(context.searchQuery, out var slot) && SceneViewBookmarkManager.HasBookmark(slot))
                    {
                        var item = provider.CreateItem(
                            context,
                            slot.ToString(),
                            slot.ToString(),
                            $"Scene View Bookmark {slot}",
                            icon,
                            null);
                        items.Add(item);
                    }

                    return null;
                },
                filterId = "svb:",
                isExplicitProvider = true,
            };
        }

        static void HandleMoveToBookmark(SearchItem item)
        {
            var slot = int.Parse(item.id);
            SceneViewBookmarkManager.MoveToBookmark(slot);
        }
    }

    static class AssetBookmarkSearchProvider
    {
        const string id = "asset-bookmarks";

        [SearchItemProvider]
        static SearchProvider CreateProvider()
        {
            var icon = SceneViewBookmarkManager.SceneViewBookmarkIcon;
            return new SearchProvider(id, "Bookmark4Unity: Asset & Scene Object Bookmarks")
            {
                actions =
                {
                    new SearchAction(id, "Open Asset", null, null, HandleOpenBookmark),
                },
                fetchItems = (context, items, provider) =>
                {
                    if (EditorPrefs.HasKey(Bookmark4UnityWindow.PinnedKey))
                    {
                        var data = JsonUtility.FromJson<Bookmark4UnityWindow.DataWrapper>(EditorPrefs.GetString(Bookmark4UnityWindow.PinnedKey));

                        // assets
                        for (int i = 0; i < data.assets.Count; i++)
                        {
                            var assetData = data.assets[i];
                            if (assetData.name.Contains(context.searchQuery))
                            {
                                var icon = AssetDatabase.GetCachedIcon(assetData.path) is not Texture2D iconImage ?
                                    EditorGUIUtility.IconContent("console.warnicon").image as Texture2D :
                                    iconImage;
                                var item = provider.CreateItem(
                                    context,
                                    assetData.name,
                                    1,
                                    assetData.name,
                                    assetData.path,
                                    icon,
                                    null
                                );
                                items.Add(item);
                            }
                        }

                        // scene objects
                        for (int i = 0; i < data.references.Count; i++)
                        {
                            var guidData = data.references[i];
                            if (guidData.cachedName.Contains(context.searchQuery))
                            {
                                var reference = new GuidReference(guidData);
                                var icon = reference.gameObject is null ?
                                    EditorGUIUtility.IconContent("console.warnicon").image as Texture2D :
                                    PrefabUtility.GetIconForGameObject(reference.gameObject);
                                var item = provider.CreateItem(
                                    context,
                                    guidData.cachedName,
                                    1,
                                    guidData.cachedName,
                                    guidData.cachedScene,
                                    icon,
                                    reference.gameObject
                                );
                                items.Add(item);
                            }
                        }
                    }

                    return null;
                },
                filterId = "b4u:",
                isExplicitProvider = true,
            };
        }

        static void HandleOpenBookmark(SearchItem item)
        {
            if (item.data is null)
            {
                // assets
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.description);
                if (asset is not null)
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }
            }
            else
            {
                // scene objects
                Selection.activeGameObject = item.data as GameObject;
                SceneView.lastActiveSceneView.FrameSelected();
            }

        }
    }
}
