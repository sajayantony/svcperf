namespace EtlViewer.QueryFx
{
    using EtlViewer;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Tx.Windows;

    public class ManifestCompiler
    {
        public static string TempAssemblyCache = App.TempFiles + Path.DirectorySeparatorChar + "TemporaryCompiledAssemblies";
        static Dictionary<string, string> AssemblyCache = new Dictionary<string, string>();
        static HashSet<string> Providers = new HashSet<string>();

        public static bool Compile(string manifest, out string error)
        {
            error = null;
            EnsureTemporaryCache();

            //Get cached key for assembly. 
            FileInfo info = new FileInfo(manifest);
            string assemblyName = Path.GetFileNameWithoutExtension(manifest) + "_" + info.LastWriteTime.ToString("yyMMdd_HHmmss") + "_man.dll";
            string fullpath = Path.GetFullPath(TempAssemblyCache + "\\" + assemblyName);

            if (!File.Exists(fullpath))
            {
                var manifestDictionary = Tx.Windows.ManifestParser.Parse(File.ReadAllText(manifest));
                Dictionary<string, string> generated = new Dictionary<string, string>();

                if (TryAddManifest(manifestDictionary.Keys))
                {
                    AssemblyBuilder.OutputAssembly(manifestDictionary, fullpath);
                    EtlViewer.Logger.Log("Manifest compiled into : " + fullpath);
                    AssemblyCache[manifest] = fullpath;
                    Assembly.LoadFile(fullpath);
                }
                else
                {
                    error += "Could not load Provider - " + manifestDictionary.Keys.Aggregate((e, acc) => e + "\n" + acc);
                    error += "\n\nAlready loaded assemblies for Providers -\n* " + Providers.Aggregate((e, acc) => e + "\n *" + acc);
                }
            }
            else
            {
                AssemblyCache[manifest] = fullpath;
                var providers = TypeLoadHelper.GetProvidersInSandbox(fullpath);
                if (TryAddManifest(providers))
                {
                    Assembly.LoadFile(fullpath);
                }
                else
                {
                    error += "Could not load Provider - " + providers.Aggregate((e, acc) => e + "\n" + acc);
                    error += "\n\nAlready loaded assemblies for Providers -\n* " + Providers.Aggregate((e, acc) => e + "\n *" + acc);
                }

                Logger.Log("Loading precompiled manifest {0}: from {1} ", manifest, fullpath);
            }

            return String.IsNullOrEmpty(error);
        }


        /// <summary>
        /// The known types seems to have a bug if added to a typed query.
        /// We should not pass this for query execution and only doing 
        /// generic etw queries.
        /// </summary>
        /// <returns></returns>
        public static Type[] GetKnowntypesforPlayback()
        {
            List<Type> types = new List<Type>();
            foreach (var path in ManifestCompiler.GetGeneratedAssemblies())
            {
                types.AddRange(Assembly.LoadFile(path).GetTypes());
            }

            return types.Distinct().ToArray();
        }

        internal static IEnumerable<string> GetGeneratedAssemblies()
        {
            return AssemblyCache.Values;
        }

        static bool TryAddManifest(IEnumerable<string> keyCollection)
        {
            foreach (var key in keyCollection)
            {
                if (!Providers.Contains(key))
                {
                    Providers.Add(key);
                    Logger.Log("Adding Manifest: " + key);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        static void EnsureTemporaryCache()
        {
            if (!Directory.Exists(TempAssemblyCache))
            {
                Directory.CreateDirectory(TempAssemblyCache);
            }
        }

        public static IEnumerable<string> GetProviders()
        {
            return Providers;
        }

        class TypeLoadHelper : MarshalByRefObject
        {
            public static string[] GetProvidersInSandbox(string assemblyPath)
            {
                AppDomain domain = AppDomain.CreateDomain("Container");
                try
                {
                    TypeLoadHelper helper = (TypeLoadHelper)domain.CreateInstanceFromAndUnwrap(
                        Assembly.GetExecutingAssembly().Location,
                        typeof(TypeLoadHelper).FullName);
                    return helper.GetProviders(assemblyPath);
                }
                finally
                {
                    AppDomain.Unload(domain);
                }
            }

            string[] GetProviders(string path)
            {
                Assembly assembly = Assembly.LoadFrom(path);

                var q = (from s in assembly.GetTypes()
                         let attr = (ManifestEventAttribute)s.GetCustomAttributes(true).Where((e) => e is ManifestEventAttribute).FirstOrDefault()
                         where attr != null
                         select s.FullName.Split('.')[2]).Distinct().ToArray();

                return q;
            }
        }
    }
}
