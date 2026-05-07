using System;
using System.Reflection;
using UnityEngine;

public static class NativeGalleryBridge
{
    private const string NativeGalleryTypeName = "NativeGallery";

    public static bool TrySaveImageToGallery(Texture2D texture, string album, string filename, out string error)
    {
        error = null;

        Type nativeGalleryType = ResolveNativeGalleryType();
        if (nativeGalleryType == null)
        {
            error = "NativeGallery plugin is not available.";
            return false;
        }

        MethodInfo method = nativeGalleryType.GetMethod(
            "SaveImageToGallery",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Texture2D), typeof(string), typeof(string) },
            null);

        if (method == null)
        {
            error = "NativeGallery.SaveImageToGallery method was not found.";
            return false;
        }

        try
        {
            method.Invoke(null, new object[] { texture, album, filename });
            return true;
        }
        catch (Exception e)
        {
            error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return false;
        }
    }

    public static bool TryGetImageFromGallery(Action<string> onPicked, string title, out bool denied, out string error)
    {
        denied = false;
        error = null;

        Type nativeGalleryType = ResolveNativeGalleryType();
        if (nativeGalleryType == null)
        {
            error = "NativeGallery plugin is not available.";
            return false;
        }

        MethodInfo method = nativeGalleryType.GetMethod(
            "GetImageFromGallery",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Action<string>), typeof(string) },
            null);

        if (method == null)
        {
            error = "NativeGallery.GetImageFromGallery method was not found.";
            return false;
        }

        try
        {
            object permission = method.Invoke(null, new object[] { onPicked, title });
            denied = string.Equals(permission != null ? permission.ToString() : null, "Denied", StringComparison.OrdinalIgnoreCase);
            return true;
        }
        catch (Exception e)
        {
            error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return false;
        }
    }

    private static Type ResolveNativeGalleryType()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type resolved = assembly.GetType(NativeGalleryTypeName, false);
            if (resolved != null)
                return resolved;
        }

        return null;
    }
}
