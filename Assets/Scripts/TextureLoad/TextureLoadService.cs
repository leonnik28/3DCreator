using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public class TextureLoadService
{
    private MonoBehaviour _coroutineRunner;
    private int _maxTextureSize = 1024;

    public TextureLoadService(MonoBehaviour runner)
    {
        _coroutineRunner = runner;
    }

    public void LoadFromGallery(Action<Texture2D> onSuccess, Action<string> onError)
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            _coroutineRunner.StartCoroutine(LoadFromFileCoroutine(path, onSuccess, onError));
        }
#elif UNITY_ANDROID || UNITY_IOS
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                coroutineRunner.StartCoroutine(LoadFromFileCoroutine(path, onSuccess, onError));
            }
        }, "Select Image");
        
        if (permission == NativeGallery.Permission.Denied)
        {
            onError?.Invoke("Gallery access denied");
        }
#endif
    }

    private IEnumerator LoadFromFileCoroutine(string path, Action<Texture2D> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file://" + path))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(uwr.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                onSuccess?.Invoke(OptimizeTexture(texture));
            }
        }
    }

    private Texture2D OptimizeTexture(Texture2D original)
    {
        int width = original.width;
        int height = original.height;

        if (width > _maxTextureSize || height > _maxTextureSize)
        {
            float ratio = (float)width / height;
            if (width > height)
            {
                width = _maxTextureSize;
                height = Mathf.RoundToInt(width / ratio);
            }
            else
            {
                height = _maxTextureSize;
                width = Mathf.RoundToInt(height * ratio);
            }

            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(original, rt);

            RenderTexture.active = rt;
            Texture2D resized = new Texture2D(width, height);
            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            UnityEngine.Object.Destroy(original);
            return resized;
        }

        return original;
    }
}