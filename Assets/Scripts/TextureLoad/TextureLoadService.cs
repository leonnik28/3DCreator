using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public class TextureLoadService
{
    public const string UserCancelledError = "__IMAGE_PICKER_CANCELLED__";

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
        else
        {
            onError?.Invoke(UserCancelledError);
        }
#elif UNITY_STANDALONE_WIN
        _coroutineRunner.StartCoroutine(LoadFromWindowsFileDialogCoroutine(onSuccess, onError));
#elif UNITY_ANDROID || UNITY_IOS
        bool requestStarted = NativeGalleryBridge.TryGetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                _coroutineRunner.StartCoroutine(LoadFromFileCoroutine(path, onSuccess, onError));
            }
            else
            {
                onError?.Invoke(UserCancelledError);
            }
        }, "Select Image", out bool permissionDenied, out string error);

        if (!requestStarted)
        {
            onError?.Invoke(error);
        }
        else if (permissionDenied)
        {
            onError?.Invoke("Gallery access denied");
        }
#else
        onError?.Invoke("Image picking is not implemented for this platform.");
#endif
    }

#if UNITY_STANDALONE_WIN
    private IEnumerator LoadFromWindowsFileDialogCoroutine(Action<Texture2D> onSuccess, Action<string> onError)
    {
        // Больше не переключаем режим экрана — окно остаётся полноэкранным
        Debug.Log("Opening Windows image picker (fullscreen)...");

        bool dialogOpened = WindowsFileDialogBridge.TryOpenImageFilePanel("Select Image", out string path, out string error);

        if (!dialogOpened)
        {
            onError?.Invoke(error);
            yield break;
        }

        if (string.IsNullOrEmpty(path))
        {
            onError?.Invoke(UserCancelledError);
            yield break;
        }

        Debug.Log($"Selected image path: {path}");
        yield return _coroutineRunner.StartCoroutine(LoadFromFileCoroutine(path, onSuccess, onError));
    }
#endif

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
