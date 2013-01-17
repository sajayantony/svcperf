namespace EtlViewer
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using Utilities;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    partial class App : Application
    {
        private static string tempFiles;
        internal static CommandProcessor CommandLineArgs;

        internal const string AppExe = "SvcPerf.exe";

        public static string TempFiles
        {
            get
            {
                if (String.IsNullOrEmpty(tempFiles))
                {
                    //TODO: Fix unpack to unpack all files into a single folder. 
                    var exeAssembly = Assembly.GetEntryAssembly();

                    if (exeAssembly != null)
                    {
                        var exeLastWriteTime = File.GetLastWriteTime(exeAssembly.ManifestModule.FullyQualifiedName);
                        var version = exeLastWriteTime.ToString("VER.yyyy'-'MM'-'dd'.'HH'.'mm'.'ss.fff");
                        tempFiles = version;
                    }
                    else
                    {
                        tempFiles = "Temp";
                    }
                }

                return tempFiles;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            SplashScreen screen = new SplashScreen(@"\Assets\Images\SplashScreen.png");
            screen.Show(true, true);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // The UI is never responsible for unpacking and is always
            // assumed to run from an unpacked from SvcPerf.exe into the app folder.
            SupportFiles.SupportFileDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            try
            {
                CommandLineArgs = new CommandProcessor();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                Environment.Exit(1);
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                MessageBox.Show(
                    "Please copy using Ctrl+C or and report at \n http://svcperf.codeplex.com/workitem/list/basic to help improve svcperf.\n\n" +
                    e.ExceptionObject.ToString(),
                    "App Terminating",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                ExceptionHelper.ShowException(e.ExceptionObject as Exception);
            }
        }
    }
}
