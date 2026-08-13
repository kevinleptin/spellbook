using System;
using System.Runtime.InteropServices;

namespace Spellbook.UI
{
    /// <summary>
    /// Windows 原生文件/文件夹选择器(P/Invoke comdlg32 / shell32)。
    /// 仅 Windows 独立播放器可用;调用会阻塞主线程直到用户关闭对话框(符合模态预期)。
    /// </summary>
    public static class NativeFileDialog
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class OpenFileName
        {
            public int structSize;
            public IntPtr owner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 1;
            public string file;
            public int maxFile;
            public string fileTitle = new string('\0', 260);
            public int maxFileTitle = 260;
            public string initialDir = null;
            public string title;
            public int flags;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileNameW([In, Out] OpenFileName ofn);

        private const int OfnFileMustExist = 0x00001000;
        private const int OfnPathMustExist = 0x00000800;
        private const int OfnNoChangeDir = 0x00000008;

        /// <summary>打开文件选择器;取消返回 null。filter 形如 "所有文件\0*.*\0脚本\0*.ps1\0"。</summary>
        public static string OpenFile(string title, string filter)
        {
            var ofn = new OpenFileName
            {
                filter = filter.Replace('|', '\0') + '\0',
                file = new string('\0', 4096),
                maxFile = 4096,
                title = title,
                flags = OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir,
            };
            ofn.structSize = Marshal.SizeOf(ofn);
            return GetOpenFileNameW(ofn) ? ofn.file.TrimEnd('\0') : null;
        }

        // ―― 文件夹选择:SHBrowseForFolder ――

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BrowseInfo
        {
            public IntPtr owner;
            public IntPtr pidlRoot;
            public IntPtr displayName;
            [MarshalAs(UnmanagedType.LPWStr)] public string title;
            public uint flags;
            public IntPtr callback;
            public IntPtr lparam;
            public int image;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHBrowseForFolderW(ref BrowseInfo bi);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SHGetPathFromIDListW(IntPtr pidl, IntPtr path);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr ptr);

        private const uint BifReturnOnlyDirs = 0x0001;
        private const uint BifNewDialogStyle = 0x0040;

        /// <summary>打开文件夹选择器;取消返回 null。</summary>
        public static string OpenFolder(string title)
        {
            var bi = new BrowseInfo
            {
                title = title,
                flags = BifReturnOnlyDirs | BifNewDialogStyle,
            };
            var pidl = SHBrowseForFolderW(ref bi);
            if (pidl == IntPtr.Zero) return null;

            var buffer = Marshal.AllocHGlobal(520);
            try
            {
                return SHGetPathFromIDListW(pidl, buffer)
                    ? Marshal.PtrToStringUni(buffer)
                    : null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
                CoTaskMemFree(pidl);
            }
        }
    }
}
