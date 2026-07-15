using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RPGTable.Runtime
{
    public static class StandaloneFileDialog
    {
        public static string OpenFilePanel(string title, string filter)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            return PowerShellOpenFilePanel(title, filter);
#else
            Debug.LogWarning("Runtime file picker is only implemented for Windows standalone builds.");
            return null;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int MaxPathLength = 4096;
        private const int OfnPathMustExist = 0x00000800;
        private const int OfnFileMustExist = 0x00001000;
        private const int OfnNoChangeDir = 0x00000008;

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName openFileName);

        [DllImport("comdlg32.dll")]
        private static extern int CommDlgExtendedError();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public StringBuilder lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        private static string WindowsOpenFilePanel(string title, string filter)
        {
            var fileName = new StringBuilder(MaxPathLength);
            var openFileName = new OpenFileName
            {
                lStructSize = Marshal.SizeOf(typeof(OpenFileName)),
                lpstrFile = fileName,
                nMaxFile = fileName.Capacity,
                lpstrFilter = BuildFilter(filter),
                nFilterIndex = 1,
                lpstrTitle = title,
                Flags = OfnPathMustExist | OfnFileMustExist | OfnNoChangeDir
            };

            if (GetOpenFileName(openFileName))
            {
                return fileName.ToString();
            }

            var error = CommDlgExtendedError();
            if (error != 0)
            {
                Debug.LogWarning($"Native file picker failed with CommDlgExtendedError={error}.");
            }

            return null;
        }

        private static string PowerShellOpenFilePanel(string title, string filter)
        {
            var resultPath = Path.GetTempFileName();

            try
            {
                var script = BuildPowerShellScript(title, BuildPowerShellFilter(filter), resultPath);
                var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -STA -EncodedCommand {encodedScript}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return null;
                    }

                    if (!process.WaitForExit(300000))
                    {
                        process.Kill();
                        Debug.LogWarning("PowerShell file picker timed out.");
                        return null;
                    }
                }

                if (!File.Exists(resultPath))
                {
                    return null;
                }

                var selectedPath = File.ReadAllText(resultPath, Encoding.UTF8);
                return string.IsNullOrWhiteSpace(selectedPath) ? null : selectedPath.Trim();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PowerShell file picker failed: {exception.Message}");
                return null;
            }
            finally
            {
                try
                {
                    if (File.Exists(resultPath))
                    {
                        File.Delete(resultPath);
                    }
                }
                catch (Exception)
                {
                    // Best effort cleanup only.
                }
            }
        }

        private static string BuildFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return "All files\0*.*\0\0";
            }

            var patterns = filter.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < patterns.Length; i++)
            {
                var extension = patterns[i].Trim().TrimStart('.');
                patterns[i] = $"*.{extension}";
            }

            var label = filter.Replace(",", ", ").ToUpperInvariant();
            return $"{label}\0{string.Join(";", patterns)}\0All files\0*.*\0\0";
        }

        private static string BuildPowerShellFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return "All files (*.*)|*.*";
            }

            var patterns = filter.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < patterns.Length; i++)
            {
                var extension = patterns[i].Trim().TrimStart('.');
                patterns[i] = $"*.{extension}";
            }

            var label = filter.Replace(",", ", ").ToUpperInvariant();
            return $"{label} ({string.Join(";", patterns)})|{string.Join(";", patterns)}|All files (*.*)|*.*";
        }

        private static string BuildPowerShellScript(string title, string filter, string resultPath)
        {
            return $@"
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Application]::EnableVisualStyles()
$owner = New-Object System.Windows.Forms.Form
$owner.TopMost = $true
$owner.ShowInTaskbar = $false
$owner.WindowState = [System.Windows.Forms.FormWindowState]::Minimized
$dialog = New-Object System.Windows.Forms.OpenFileDialog
$dialog.Title = '{EscapePowerShellString(title)}'
$dialog.Filter = '{EscapePowerShellString(filter)}'
$dialog.CheckFileExists = $true
$dialog.CheckPathExists = $true
$dialog.Multiselect = $false
$dialog.RestoreDirectory = $true
try {{
    $owner.Show()
    $owner.Activate()
    if ($dialog.ShowDialog($owner) -eq [System.Windows.Forms.DialogResult]::OK) {{
        [System.IO.File]::WriteAllText('{EscapePowerShellString(resultPath)}', $dialog.FileName, [System.Text.Encoding]::UTF8)
    }}
}} finally {{
    $dialog.Dispose()
    $owner.Close()
    $owner.Dispose()
}}";
        }

        private static string EscapePowerShellString(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }
#endif
    }
}
