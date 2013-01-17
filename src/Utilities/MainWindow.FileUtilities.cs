namespace Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    static partial class FileUtilities
    {

        public static bool ValidateAndEnumerate(string[] input, ref List<string> files, string validExtension, out string errorMessage)
        {
            if (files == null)
            {
                files = new List<string>();
            }
            errorMessage = null;            
            if (input == null || input.Length == 0)
            {
                return false;
            }

            foreach (var location in input)
            {
                if (Directory.Exists(location))
                {
                    foreach (string file in Directory.EnumerateFiles(location, "*" + validExtension))
                    {
                        if (IsValidFileType(validExtension, file, out errorMessage))
                        {
                            files.Add(file);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else if (File.Exists(location))
                {
                    if (IsValidFileType(validExtension, location, out errorMessage))
                    {
                        files.Add(location);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    errorMessage += Path.GetFullPath(location) + " not found.";
                    continue;
                }
            }

            return files.Count > 0;
        }

        static bool IsValidFileType(string validExtension, string inputFile, out string error)
        {
            error = null;
            if (!String.IsNullOrEmpty(validExtension))
            {
                if (String.Compare(Path.GetExtension(inputFile), validExtension) != 0)
                {
                    error = String.Format("Cannot load {0}. Expected a file with extension '.{1}'.", inputFile, validExtension);
                    return false;
                }
            }

            return true;
        }
    }
}
