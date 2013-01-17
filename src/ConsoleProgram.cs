using EtlViewer;
using System;
using System.Diagnostics;
using System.IO;
using Utilities;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

namespace SvcPerfConsole
{
    static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // The Console is the driver for SvcPerf
            // It unpacks the resources into the required app directory
            // and also itself so that it can be xcopied if necessary. 
            SupportFiles.UnpackResourcesIfNeeded();
            CopySelf();
            ProcessArgs(args);
        }

        private static void CopySelf()
        {
            // This is the console app and hence copy over this 
            // into the supporting directories folder for xcopy.
            string self = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            string dest = Path.Combine(SupportFiles.SupportFileDir, self);
            if (!File.Exists(dest))
            {
                File.Copy(Assembly.GetEntryAssembly().Location, dest);
            }
        }

        private static void ProcessArgs(string[] args)
        {
            CommandProcessor processor = null;
            try
            {
                processor = new CommandProcessor();
            }
            catch (Exception e)
            {
                CommandProcessor.CreateConsole();
                Console.WriteLine("Error: " + e.Message + "\r\n" + "Use -? for help.");
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();
                Environment.Exit(1);
            }

            if (processor.noGui)
            {
                processor.Process();
            }
            else
            {
                try
                {
                    const string SvcPerfExe = "SvcPerf.exe";                    
                    var info = new ProcessStartInfo();
                    string path = Path.Combine(SupportFiles.SupportFileDir, "SvcPerfUI.exe");
                    string exeName = Assembly.GetExecutingAssembly().Location;
                    string argsToUi = string.Empty;
                    
                    try
                    {
                        //By default we pass in a space seperated argument list;
                        argsToUi = args.Length > 0 ? args.Aggregate((curr, next) => curr + " " + next) : string.Empty;
                        int commandArgStartIndex = Environment.CommandLine.IndexOf("SvcPerf.exe", StringComparison.OrdinalIgnoreCase);
                        string commandLine = Environment.CommandLine;
                    
                        if (commandArgStartIndex > -1 && commandLine.Length > commandArgStartIndex + SvcPerfExe.Length)
                        {
                            commandLine = commandLine.Substring(commandArgStartIndex + SvcPerfExe.Length);
                            int spaceIndex = commandLine.IndexOf(' ');
                            if (spaceIndex >= 0)
                            {
                                argsToUi = commandLine.Substring(spaceIndex);
                            }
                        }   
                    }
                    catch (Exception)
                    {                                             
                    }

                    info.Arguments = argsToUi;
                    info.FileName = path;

                    if (processor.command == SvcPerfCommand.ShowUIRealtime)
                    {
                        //Realtime sessions require admin rights. 
                        info.UseShellExecute = true;
                        info.Verb = "runas";
                    }

                    Process.Start(info);

                }
                catch (Win32Exception exception)
                {
                    if (exception.NativeErrorCode == 1223)
                    {
                        return;
                    }
                    throw;
                }
            }
        }
    }
}
