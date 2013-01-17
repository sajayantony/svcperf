namespace EtlViewer.QueryFx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;
    using Tx.Windows;

    class TxHelper
    {
        internal static Task<TxReader> GetReader(IEnumerable<string> etlFileName, bool isRealtime = false)
        {
            TaskCompletionSource<TxReader> tsc = new TaskCompletionSource<TxReader>();
            TxReader reader = new TxReader(etlFileName, isRealtime);
            tsc.SetResult(reader);
            return tsc.Task;
        }


        internal static Playback GetCurrentEtlScope(IEnumerable<string> etlfiles, bool isRealtime)
        {
            try
            {
                if ((etlfiles == null || etlfiles.Count() == 0))
                {
                    return null;
                }
                Playback scope = new Playback();
                foreach (var file in etlfiles)
                {
                    if (isRealtime)
                    {
                        scope.AddRealTimeSession(file);
                    }
                    else
                    {
                        scope.AddEtlFiles(file);
                    }
                }

                return scope;
            }
            catch (Exception)
            {
                throw new Exception("Please try loading the ETL files since the Playback cannot be created.");
            }
        }
    }
}
