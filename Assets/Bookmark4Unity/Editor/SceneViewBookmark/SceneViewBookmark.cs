using System;
using UnityEditor;
using UnityEngine;

namespace Bookmark4Unity.Editor
{
    [Serializable]
    struct SceneViewCameraBookmark
    {
        public Vector3 pivot;
        public Quaternion rotation;
        public float size;
        public bool in2DMode;
        public int sceneViewScreenShotSizeX;
        public int sceneViewScreenShotSizeY;
        public string sceneViewScreenShotBase64String;

        public SceneViewCameraBookmark(SceneView sceneView, Texture2D sceneViewScreenshot)
        {
            pivot = sceneView.pivot;
            rotation = sceneView.rotation;
            size = sceneView.size;
            in2DMode = sceneView.in2DMode;
            sceneViewScreenShotSizeX = sceneViewScreenshot.width;
            sceneViewScreenShotSizeY = sceneViewScreenshot.height;
            sceneViewScreenShotBase64String = System.Convert.ToBase64String(sceneViewScreenshot.EncodeToPNG());
        }

        public Texture2D GetSceneViewScreenShot()
        {
            if (string.IsNullOrEmpty(sceneViewScreenShotBase64String)) return null;
            // convert it to byte array
            byte[] texByte = System.Convert.FromBase64String(sceneViewScreenShotBase64String);
            var tex = new Texture2D(sceneViewScreenShotSizeX, sceneViewScreenShotSizeY);
            //load texture from byte array
            tex.LoadImage(texByte);
            return tex;
        }
    }
}
