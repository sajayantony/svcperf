namespace EtlViewer.Viewer.Plugins
{
    using EtlViewer.QueryFx;
    using EtlViewer.Viewer.Models;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reactive;
    
    public class EtlViewerContext : INotifyPropertyChanged
    {
        public const string EtlFilesPropertyName = "EtlFiles";

        MainModel Model { get; set; }

        internal EtlViewerContext(MainModel model)
        {
            this.Model = model;
            this.Model.PropertyChanged += Model_PropertyChanged;

        }

        void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                // Pass through for now
                PropertyChanged(sender, e);
            }
        }

        public Playback GetPlayback()
        {
            Playback playback = TxHelper.GetCurrentEtlScope(this.Model.EtlFiles, this.Model.IsRealTime);
            if (playback != null)
            {
                playback.KnownTypes = ManifestCompiler.GetKnowntypesforPlayback();
            }

            return playback;
        }

        public List<string> EtlFiles { get { return this.Model.EtlFiles; } }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
