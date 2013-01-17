namespace SvcPerf
{
    using EtlViewer.QueryFx;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Utilities;

    public class QueryEngine
    {
        static QueryEngine()
        {
            SupportFiles.UnpackResourcesIfNeeded();
        }

        public static IDisposable Dump(IList<string> etlfiles, 
                                Action<Type> onStart, 
                                Action<object> onNext,
                                Action<Exception> onError,
                                string query)
        {
            StringWriter writer = new StringWriter();
            StringWriter error = new StringWriter();
            QueryExecutionContext context = QueryExecutionContext.CreateFromFiles(etlfiles, onStart, onNext);
            QueryCompiler.CompileAndRun(context, 
                                        query, 
                                        error, 
                                        writer);

            string output = error.ToString();
            if (!String.IsNullOrEmpty(output))
            {
                if (onError != null)
                {
                    onError(new Exception(output));
                }
            }

            return context;
        }

        public static void CompileManifest(string manifest)
        {
            string error = null;
            ManifestCompiler.Compile(manifest, out error);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(error);
            }
        }
    }
}
