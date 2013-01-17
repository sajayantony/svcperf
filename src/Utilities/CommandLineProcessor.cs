namespace EtlViewer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Utilities;
    using System.Runtime.CompilerServices;
    using EtlViewer.QueryFx;
    using System.Diagnostics;

    enum SvcPerfCommand { DumpRaw, ShowUI, ShowUIRealtime }


    class CommandProcessor
    {
        public string filter;
        public string[] inputFiles;
        public string[] manifests;
        public string[] queries;
        public string[] session;
        public bool verbose;
        public bool noGui;

        public SvcPerfCommand command;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public CommandProcessor()
        {
            ParseForConsoleApplication(delegate(CommandLineParser parser)
            {
                parser.DefineOptionalQualifier("verbose", ref verbose, "Output verbose logs.");
                parser.DefineOptionalQualifier("m", ref manifests, "Manifest files.");
                parser.DefineOptionalQualifier("q", ref queries, "Linq query files.");


                // dump requires input files.
                parser.DefineParameterSet("dump", ref command, SvcPerfCommand.DumpRaw, "Dump events");
                parser.DefineParameter("InputFile", ref inputFiles, "Specify the input ETL file to view.");

                parser.DefineParameterSet("session", ref command, SvcPerfCommand.ShowUIRealtime, "Dump events");
                parser.DefineOptionalQualifier("filter", ref filter, "filter string that can be applied on the trace.");
                parser.DefineParameter("sessions", ref session, "Specify session name to view.");
                parser.DefineOptionalParameter("InputFile", ref inputFiles, "Specify the input ETL file to view.");

                parser.DefineDefaultParameterSet(ref command, SvcPerfCommand.ShowUI, "Will show the SvcPerf Viewer.");
                parser.DefineOptionalQualifier("filter", ref filter, "filter string that can be applied on the trace.");
                parser.DefineOptionalParameter("InputFile", ref inputFiles, "Specify the input ETL file to view.");

                this.HelpRequested = parser.HelpRequested;

                if (parser.HelpRequested)
                {
                    int maxConsoleWidth = 80;
                    string helpString = parser.GetHelp(maxConsoleWidth, null, true);
                    Console.WriteLine(helpString);
                    ThreadPool.QueueUserWorkItem((s) =>
                    {
                        try
                        {
                            var userGuide = Path.Combine(SupportFiles.SupportFileDir, "UsersGuide.htm");
                            UsersGuide.DisplayUsersGuide(userGuide, "commandline");
                        }
                        finally
                        {
                            Environment.Exit(0);
                        }
                    });
                }
            });

            if (this.command == SvcPerfCommand.DumpRaw)
            {
                this.noGui = true;
            }
        }

        public bool HelpRequested { get; set; }

        TextWriter LogWriter { get; set; }

        static void ParseForConsoleApplication(Action<CommandLineParser> parseBody)
        {
            CommandLineParser parser = new CommandLineParser(Environment.CommandLine);
            parseBody(parser);
            parser.CompleteValidation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Process()
        {
            bool newConsoleCreated = false;
            if (!newConsoleCreated)
            {
                CreateConsole();
            }

            this.SetupLogging();

            this.ProcessManifests();

            if (this.command == SvcPerfCommand.DumpRaw)
            {
                this.ProcessDumpEvents();
            }
        }

        private void SetupLogging()
        {
            if (this.verbose)
            {
                this.LogWriter = Console.Out;
            }
            else
            {
                this.LogWriter = TextWriter.Null;
            }

            EtlViewer.Logger.Console = this.LogWriter;
        }

        private void ProcessDumpEvents()
        {
            string error;
            List<string> files = null;
            if (!FileUtilities.ValidateAndEnumerate(inputFiles, ref files, ".etl", out error))
            {
                Console.WriteLine(error);
                return;
            }

            if (queries == null || queries.Length == 0)
            {
                Logger.Log("Dumping Raw Events");
                string fileName = Path.Combine(SupportFiles.SupportFileDir, "AllEvents.linq");
                QueryCompiler.Compile(inputFiles,
                                                fileName,
                                                Console.Out,
                                                this.LogWriter,
                                                ManifestCompiler.GetKnowntypesforPlayback());
            }
            else
            {
                Logger.Log("Dumping events using query.");
                List<string> queryFiles = null;
                FileUtilities.ValidateAndEnumerate(inputFiles, ref queryFiles, ".linq", out error);
                if (!FileUtilities.ValidateAndEnumerate(queries, ref queryFiles, ".linq", out error))
                {
                    Console.WriteLine(error);
                    return;
                }

                foreach (var item in queryFiles)
                {
                    EtlViewer.QueryFx.QueryCompiler.Compile(inputFiles,
                                item,
                                Console.Out,
                                this.LogWriter,
                                ManifestCompiler.GetKnowntypesforPlayback());
                }
            }
        }

        private void ProcessManifests()
        {
            string error;
            List<string> files = null;
            FileUtilities.ValidateAndEnumerate(inputFiles, ref files, ".man", out error);

            if (!FileUtilities.ValidateAndEnumerate(manifests, ref files, ".man", out error)
                && this.manifests != null
                && this.manifests.Length > 0)
            {
                Console.WriteLine(error);
                return;
            }

            foreach (var item in files)
            {
                string itemError;
                ManifestCompiler.Compile(item, out itemError);
                error += itemError;
            }

            if (!String.IsNullOrEmpty(error))
            {
                Console.WriteLine("ERROR:" + error);
            }
        }

        #region CreateConsole
        [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
        static extern int AllocConsole();
        [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
        static extern int AttachConsole(int dwProcessId);
        [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        /// <summary>
        /// Tries to fetch the console that created this process or creates a new one if the parent process has no 
        /// console.   Returns true if a NEW console has been created.  
        /// </summary>
        internal static bool CreateConsole()
        {
            bool newConsoleCreated = false;
            // TODO AttachConsole is not reliable (GetStdHandle returns an invalid handle about half the time)
            // So I have given up on it. 
            AllocConsole();
            newConsoleCreated = true;

            IntPtr stdHandle = GetStdHandle(-11);       // Get STDOUT
            var safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdHandle, true);
            Thread.Sleep(100);
            FileStream fileStream;
            try
            {
                fileStream = new FileStream(safeFileHandle, FileAccess.Write);
            }
            catch (System.IO.IOException)
            {
                return false;       // This will simply fail.  
            }

            var encoding = System.Text.Encoding.GetEncoding(437);   // MSDOS Code page.  
            StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
            s_threadToInterrupt = Thread.CurrentThread;

            Console.CancelKeyPress += new ConsoleCancelEventHandler(delegate(object sender, ConsoleCancelEventArgs e)
            {
                if (Interlocked.CompareExchange(ref s_controlCPressed, 1, 0) == 0)
                {
                    Console.WriteLine("Control C Pressed.  Aborting.");
                    if (s_threadToInterrupt != null)
                    {
                        s_threadToInterrupt.Interrupt();
                        Thread.Sleep(30000);
                        Console.WriteLine("Thread did not die after 30 seconds.  Killing process.");
                    }
                    Environment.Exit(-20);
                }
                e.Cancel = true;
            });

            return newConsoleCreated;
        }

        static Thread s_threadToInterrupt;
        static int s_controlCPressed = 0;

        #endregion
    };
}
