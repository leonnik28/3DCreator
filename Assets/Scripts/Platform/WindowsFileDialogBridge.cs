using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class WindowsFileDialogBridge
{
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_NOCHANGEDIR = 0x00000008;
    private const int OFN_EXPLORER = 0x00080000;
    private const int OFN_HIDEREADONLY = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct OpenFileName
    {
        public int structSize;
        public System.IntPtr dlgOwner;
        public System.IntPtr instance;
        public string filter;
        public string customFilter;
        public int maxCustFilter;
        public int filterIndex;
        public StringBuilder file;
        public int maxFile;
        public StringBuilder fileTitle;
        public int maxFileTitle;
        public string initialDir;
        public string title;
        public int flags;
        public short fileOffset;
        public short fileExtension;
        public string defExt;
        public System.IntPtr custData;
        public System.IntPtr hook;
        public string templateName;
        public System.IntPtr reservedPtr;
        public int reservedInt;
        public int flagsEx;
    }

    [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetOpenFileName(ref OpenFileName ofn);

    [DllImport("Comdlg32.dll")]
    private static extern int CommDlgExtendedError();

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    public static bool TryOpenImageFilePanel(string title, out string path, out string error)
    {
        path = null;
        error = null;

        if (TryOpenWithExternalPowerShell(title, out path, out error))
            return true;

        error = null;

        if (TryOpenWithWinForms(title, out path, out error))
            return true;

        error = null;

        var fileBuffer = new StringBuilder(1024);
        var fileTitleBuffer = new StringBuilder(256);
        var dialog = new OpenFileName
        {
            structSize = Marshal.SizeOf(typeof(OpenFileName)),
            dlgOwner = GetActiveWindow(),
            filter = "Image Files\0*.png;*.jpg;*.jpeg;*.bmp;*.gif\0All Files\0*.*\0\0",
            file = fileBuffer,
            maxFile = fileBuffer.Capacity,
            fileTitle = fileTitleBuffer,
            maxFileTitle = fileTitleBuffer.Capacity,
            title = title,
            defExt = "png",
            flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR | OFN_HIDEREADONLY
        };

        bool result = GetOpenFileName(ref dialog);
        if (result)
        {
            path = dialog.file.ToString();
            return true;
        }

        int dialogError = CommDlgExtendedError();
        if (dialogError == 0)
            return true;

        error = $"Windows file dialog failed with error code {dialogError}.";
        return false;
    }

    private static bool TryOpenWithExternalPowerShell(string title, out string path, out string error)
    {
        path = null;
        error = null;

        string escapedTitle = EscapeForSingleQuotedPowerShell(title);
        string script =
            "Add-Type -AssemblyName System.Windows.Forms; " +
            "$dialog = New-Object System.Windows.Forms.OpenFileDialog; " +
            "$dialog.Title = '" + escapedTitle + "'; " +
            "$dialog.Filter = 'Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*'; " +
            "$dialog.Multiselect = $false; " +
            "$dialog.CheckFileExists = $true; " +
            "$dialog.RestoreDirectory = $true; " +
            "if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { " +
            "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " +
            "[Console]::Write($dialog.FileName) }";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -STA -Command \"" + EscapeForDoubleQuotedArgument(script) + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    error = "Failed to start PowerShell file picker.";
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    error = string.IsNullOrWhiteSpace(stderr)
                        ? $"PowerShell file picker failed with exit code {process.ExitCode}."
                        : stderr.Trim();
                    return false;
                }

                path = string.IsNullOrWhiteSpace(output) ? null : output.Trim();
                return true;
            }
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    private static bool TryOpenWithWinForms(string title, out string path, out string error)
    {
        path = null;
        error = null;

        try
        {
            Type dialogType = Type.GetType("System.Windows.Forms.OpenFileDialog, System.Windows.Forms", false);
            if (dialogType == null)
                return false;

            Exception threadException = null;
            bool? threadResult = null;
            string selectedPath = null;

            var thread = new Thread(() =>
            {
                try
                {
                    object dialog = Activator.CreateInstance(dialogType);
                    SetProperty(dialogType, dialog, "Title", title);
                    SetProperty(dialogType, dialog, "Filter", "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*");
                    SetProperty(dialogType, dialog, "Multiselect", false);
                    SetProperty(dialogType, dialog, "CheckFileExists", true);
                    SetProperty(dialogType, dialog, "RestoreDirectory", true);

                    object result = dialogType.GetMethod("ShowDialog", Type.EmptyTypes)?.Invoke(dialog, null);
                    string resultName = result != null ? result.ToString() : string.Empty;

                    if (string.Equals(resultName, "OK", StringComparison.OrdinalIgnoreCase))
                    {
                        selectedPath = dialogType.GetProperty("FileName")?.GetValue(dialog) as string;
                        threadResult = true;
                    }
                    else
                    {
                        threadResult = true;
                    }
                }
                catch (Exception e)
                {
                    threadException = e;
                    threadResult = false;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadResult == true)
            {
                path = selectedPath;
                return true;
            }

            if (threadException != null)
            {
                error = UnwrapException(threadException);
                return false;
            }

            return false;
        }
        catch (Exception e)
        {
            error = UnwrapException(e);
            return false;
        }
    }

    private static void SetProperty(Type type, object instance, string propertyName, object value)
    {
        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property != null && property.CanWrite)
            property.SetValue(instance, value);
    }

    private static string UnwrapException(Exception exception)
    {
        return exception.InnerException != null ? exception.InnerException.Message : exception.Message;
    }

    private static string EscapeForSingleQuotedPowerShell(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("'", "''");
    }

    private static string EscapeForDoubleQuotedArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\"", "\\\"");
    }
}
