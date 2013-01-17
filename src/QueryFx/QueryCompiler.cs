namespace EtlViewer.QueryFx
{
    using Microsoft.CSharp;
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reflection;
    using System.Text;
    using Tx.Windows;

    public class QueryCompiler
    {
        public static string TempAssemblyCache
        {
            get
            {
                return App.TempFiles + Path.DirectorySeparatorChar + "TemporaryCompiledAssemblies";
            }
        }

        static Dictionary<string, Assembly> AssemblyCache = new Dictionary<string, Assembly>();

        static QueryCompiler()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
        }

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Directory.GetCurrentDirectory();
            string assemblyPath = Path.Combine(folderPath, TempAssemblyCache, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }


        internal static void Compile(string[] fileNames,
                                    string linqFile,
                                    TextWriter outputStream,
                                    TextWriter errorStream,
                                    Type[] knownTypes = null,
                                    DateTime? startTime = null,
                                    DateTime? endTime = null)
        {
            StringWriter errorLogger = new StringWriter();
            string query = LinqpadHelpers.ExtractQuery(linqFile, errorLogger);

            Playback playback = new Playback();
            foreach (string file in fileNames)
            {
                string extension = Path.GetExtension(file);
                if (string.Equals(".etl", extension, StringComparison.InvariantCultureIgnoreCase))
                {
                    playback.AddEtlFiles(file);
                }
                //else if (string.Equals(".csv", extension, StringComparison.InvariantCultureIgnoreCase))
                //{
                //    playback.AddCsvFile(file);
                //}
            }

            Dictionary<string, object> playbackProperties = new Dictionary<string, object>();
            if (startTime.HasValue)
            {
                playbackProperties.Add("StartTime", startTime.Value);
            }

            if (endTime.HasValue)
            {
                playbackProperties.Add("EndTime", endTime.Value);
            }

            playback.KnownTypes = knownTypes;


            CsvWriterSettings csvsettings = new CsvWriterSettings
            {
                Writer = outputStream
            };

            Func<Type, object, Action<object>> onDumpStartCsv = (t, result) =>
            {
                CsvHelper.PrintHeader(t, csvsettings);

                Action<object> onNext = (o) =>
                {
                    CsvHelper.Dump(o, csvsettings);
                };

                return onNext;
            };

            CompileAndRun(new QueryExecutionContext(playback, onDumpStartCsv),
                query,
                errorStream,
                errorStream,
                playbackProperties);
        }

        public static bool CompileAndRun(QueryExecutionContext context,
                                        string query,
                                        TextWriter errorLogger,
                                        TextWriter logger,
                                        Dictionary<string, object> playbackProperties = null)
        {
            try
            {
                EnsureTemporaryCache();
                Assembly assembly = null;
                Logger.Log("Compiling Query \n" + query);

                if (string.IsNullOrEmpty(query))
                {
                    errorLogger.Write("No query present to execute.");
                    return false;
                }

                if (!AssemblyCache.TryGetValue(query, out assembly))
                {
                    string usings = GetUsings();
                    assembly = GenerateAssembly(usings, query, errorLogger, logger);
                    if (assembly != null)
                    {
                        AssemblyCache[query] = assembly;
                    }
                }

                if (assembly != null)
                {
                    object t = Activator.CreateInstance(assembly.GetType("QueryExecutionTemplate.PlaybackWrapper"), context.Playback, logger);

                    if (playbackProperties != null)
                    {
                        foreach (var extraParam in playbackProperties)
                        {
                            t.GetType().GetProperty(extraParam.Key).SetValue(t, extraParam.Value, null);
                        }
                    }
                    QueryExecutionContext.Current = context;
                    bool result = (bool)t.GetType().GetMethod("CompileQuery").Invoke(t, null);
                    if (result == false)
                    {
                        Exception ex = (Exception)t.GetType().GetProperty("Exception").GetValue(t, null);
                        context.SetException(ex);
                    }
                    else
                    {
                        context.Run();
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                context.SetException(ex);
                errorLogger.WriteLine(ex.Message);
            }

            return false;
        }

        private static string GetUsings()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var providerName in ManifestCompiler.GetProviders())
            {
                builder.Append("using Tx.Windows.").Append(providerName).AppendLine(";");
            }

            return builder.ToString();
        }

        private static void EnsureTemporaryCache()
        {
            if (!Directory.Exists(TempAssemblyCache))
            {
                Directory.CreateDirectory(TempAssemblyCache);
            }
        }

        private static Assembly GenerateAssembly(string usings,
                                                string query,
                                                TextWriter errorLogger,
                                                TextWriter compilerOutput)
        {
            Assembly assembly = null;
            string location = TempAssemblyCache + "\\" + query.GetHashCode() + ".dll";
            var csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });
            var parameters = new CompilerParameters(QueryCompiler.GetAssemblies(), location, true);
            parameters.GenerateInMemory = true;
            string source = usings + templateProlog + query + templateEpilog;
            CompilerResults results = csc.CompileAssemblyFromSource(parameters, source);
            results.Errors.Cast<CompilerError>().ToList().ForEach(error => errorLogger.WriteLine(error.ErrorText));
            if (results.Errors.Count == 0)
            {
                assembly = results.CompiledAssembly;
            }
            return assembly;
        }

        internal static string[] GetAssemblyLocations()
        {
            return new string[] {
                typeof(System.Reactive.Observer).Assembly.Location,                 // "Reactive"
                typeof(System.Reactive.Linq.Observable).Assembly.Location,          // "Reactive"
                typeof(System.Reactive.Concurrency.IScheduler).Assembly.Location,   // Reactive.Interfaces
                typeof(Tx.Windows.SystemEvent).Assembly.Location,                // Rx.Tx.Etw.dll
                typeof(System.Reactive.Playback).Assembly.Location,              // Playback
                typeof(EtlViewerQuery.DurationItem).Assembly.Location,              // QueryExtensions
            };
        }

        private static string[] GetAssemblies()
        {
            List<string> assemblies = new List<string>();

            assemblies.Add("mscorlib.dll");
            assemblies.Add("System.dll");
            assemblies.Add("System.Core.dll");
            foreach (string location in GetAssemblyLocations())
            {
                assemblies.Add(location);
            }

            foreach (string assembly in ManifestCompiler.GetGeneratedAssemblies())
            {
                assemblies.Add(assembly);
            }

            return assemblies.ToArray();
        }


        const string templateProlog = @"
//We need to access the internal anonymous types in graph generation.
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""SvcPerfUI"")]  
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""EventSeries"")]  

namespace QueryExecutionTemplate
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    using System.Reactive;
    using System.Reactive.Linq;
    using EtlViewer.QueryFx;    
    using EtlViewerQuery;    
    using Tx.Windows;
    
    class PlaybackWrapper
    {
        #region WrapperFunctions
        Playback _playback;   
        TextWriter logger; 
        public Exception Exception{ get; set; }
    
        public PlaybackWrapper(Playback playback, TextWriter logger = null)
        {
            this.playback = playback;
            this.logger = logger; 
        }

        // This is the actual underlying property
        // we provide both cases so that the user can easily 
        // access the this.Playback or this.playback
        public Playback playback
        {
            get
            { 
                if(this._playback == null)
                {
                    throw new PlaybackUninitializedException();
                }
                return this._playback;
            }
            set
            {
                this._playback = value;
            }
        }

        public Playback Playback
        {
            get{ return this.playback; }  
            set{ this.playback = value; }
        }

        public DateTime StartTime
        {
            get { return playback.StartTime.DateTime; }
            set { playback.StartTime = value; }
        }

        public void Run()
        {
            playback.Run();
        }

        public void Start()
        {
            playback.Start();
        }

        #endregion

        #region EtwSystem
        public IObservable<T> GetEtwStream<T>()
        {
            return playback.GetObservable<T>();
        }

        public IObservable<T> GetObservable<T>()
        {
            return playback.GetObservable<T>();
        }
       
        #endregion

        public bool CompileQuery()
        {
            try
            {
                this.CompileQueryAndRunInternal();
                return true;
            }
            catch(Exception ex) 
            {
               this.Exception = ex;
               if(logger != null)
               {
                    logger.WriteLine(""##################"");
                    logger.WriteLine(""Exception"");
                    logger.WriteLine(ex.ToString());
                    logger.WriteLine(ex.StackTrace.ToString());
                    logger.WriteLine(""##################"");
               }
            }

            return false;
        }

        void CompileQueryAndRunInternal()
        {
            logger.WriteLine(""#Starting Playback"");   
            logger.WriteLine(""##################"");
";
        const string templateEpilog = @"        
            logger.WriteLine(""##################"");
        }
    }
}
";
    }

    public class PlaybackUninitializedException : Exception
    {
        public PlaybackUninitializedException()
        {
        }
    }
}
