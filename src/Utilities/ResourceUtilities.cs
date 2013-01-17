namespace EtlViewer
{
    using System.IO;
    using System.Reflection;
    using Utilities;

    class ResourceUtilities
    {
        public static bool UnpackResourceAsFile(string resourceName, string targetFileName)
        {
            return UnpackResourceAsFile(resourceName, targetFileName, System.Reflection.Assembly.GetEntryAssembly());
        }
        public static bool UnpackResourceAsFile(string resourceName, string targetFileName, Assembly sourceAssembly)
        {
            Stream sourceStream = sourceAssembly.GetManifestResourceStream(resourceName);
            if (sourceStream == null)
                return false;

            var dir = Path.GetDirectoryName(targetFileName);
            Directory.CreateDirectory(dir);     // Create directory if needed.  
            FileUtilities.ForceDelete(targetFileName);
            FileStream targetStream = File.Open(targetFileName, FileMode.Create);
            StreamUtilities.CopyStream(sourceStream, targetStream);
            targetStream.Close();
            return true;
        }
    }
}