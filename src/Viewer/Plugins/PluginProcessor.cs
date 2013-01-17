namespace EtlViewer.Viewer.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using Utilities;
    
    class PluginProcessor
    {
        static List<IViewerPlugin> Plugins { get; set; }

        public static IList<IViewerPlugin> LoadPlugins()
        {
            Plugins = new List<IViewerPlugin>();

            string path = Path.GetFullPath("Plugins");
            string installedPluginPath = Path.Combine(SupportFiles.SupportFileDir, "Plugins");
            if (Directory.Exists("Plugins"))
            {
                Logger.Log("Looking for plugins under + " + Path.GetFullPath("Plugins"));
                var files = Directory.GetFiles(path);
                LoadPlugins(files);
            }

            // Check if we need to load installed plugins
            if (String.Compare(path, installedPluginPath, true) != 0)
            {

                if (Directory.Exists(installedPluginPath))
                {
                    Logger.Log("Looking for plugins under + " + installedPluginPath);
                    var files = Directory.GetFiles(installedPluginPath);
                    LoadPlugins(files);
                }
            }

            return Plugins;
        }

        private static void LoadPlugins(string[] files)
        {
            foreach (string file in files)
            {
                string filename = Path.GetFullPath(file);
                if (IsAssembly(filename))
                {
                    var plugins = Assembly.LoadFile(filename).GetTypes().Where((t) => typeof(IViewerPlugin).IsAssignableFrom(t) && t.IsClass);
                    foreach (var plugin in plugins)
                    {
                        Logger.Log("Loading Plugin - " + plugin.FullName);
                        IViewerPlugin instance = Activator.CreateInstance(plugin) as IViewerPlugin;
                        Plugins.Add(instance);
                    }
                }
            }
        }

        private static bool IsAssembly(string file)
        {
            bool isAssembly = false;
            try
            {
                System.Reflection.AssemblyName testAssembly =
                    System.Reflection.AssemblyName.GetAssemblyName(file);

                System.Console.WriteLine("Yes, the file is an Assembly.");
                isAssembly = true;
            }

            catch (System.IO.FileNotFoundException)
            {
                System.Console.WriteLine("The file cannot be found.");
            }

            catch (System.BadImageFormatException)
            {
                System.Console.WriteLine("The file is not an Assembly.");
            }

            catch (System.IO.FileLoadException)
            {
                System.Console.WriteLine("The Assembly has already been loaded.");
            }
            return isAssembly;
        }

        internal static Task<IList<IViewerPlugin>> LoadPluginsAsync()
        {
            return Task.Factory.StartNew(() =>
                {
                    return LoadPlugins();
                });
        }

        public static void AddEntryPoints(IList<IViewerPlugin> plugins,
                            EtlViewerContext context,
                            MenuItem root)
        {
            if (plugins != null && plugins.Count > 0)
            {
                root.Items.Add(new Separator());

                MenuItem pluginMenu = new MenuItem();
                foreach (var plugin in plugins)
                {
                    MenuItem item = new MenuItem();
                    item.Header = plugin.Name.ToUpper();
                    item.Click += (s, e) =>
                        {
                            plugin.Launch(context);
                        };
                    root.Items.Add(item);
                }
            }
        }
    }
}
