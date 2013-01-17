namespace EtlViewer.Viewer.UIUtils
{
    using Microsoft.Win32;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    
    class MruFileHelper
    {
        const string MruPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU";

        [DllImport("shell32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SHGetPathFromIDListW(IntPtr pidl, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder pszPath);

        public static string GrabMruDirectory(string fileType)
        {
            string mostRecentFile = null;

            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(MruPath))
                {
                    if (rk != null)
                    {
                        mostRecentFile = GetMostRecentFile(fileType, rk);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            // It's ok to return null since setting the OpenFileDialog.InitialDirectory uses the default behavior
            return  Path.GetDirectoryName(mostRecentFile);
        }

        private static string GetMostRecentFile(string fileType, RegistryKey rk)
        {
            string mostRecentFile = null;

            using (RegistryKey subkey = rk.OpenSubKey(fileType))
            {
                if (subkey != null)
                {
                    int recentFileIndex = GetIndexFromMRUListEx(subkey);
                    if (recentFileIndex >= 0)
                    {
                        object value = subkey.GetValue(recentFileIndex.ToString());
                        if (value != null)
                        {
                            mostRecentFile = GetPath(value);
                        }
                    }
                }
            }

            if (!File.Exists(mostRecentFile))
            {
                mostRecentFile = null;
            }

            return mostRecentFile;
        }

        private static int GetIndexFromMRUListEx(RegistryKey key)
        {
            byte[] oIndex = key.GetValue("MRUListEx") as byte[];
            int index = -1;

            if (oIndex != null && oIndex.Length > 0)
            {
                index = (int)oIndex[0];                
            }

            return index;
        }

        private static string GetPath(object value)
        {
            const int MaxPath = 260;
            StringBuilder path = new StringBuilder(MaxPath);

            GCHandle handle = GCHandle.Alloc((byte[])(value), GCHandleType.Pinned);
            
            try
            {
                IntPtr ptrDestination = handle.AddrOfPinnedObject();
                SHGetPathFromIDListW(ptrDestination, path);

                return path.ToString();
            }
            finally
            {
                handle.Free();
            }
        }
    }
}