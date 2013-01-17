namespace EtlViewer.Viewer.Controls
{
    using EtlViewer.Viewer.Models;
    using EtlViewer.Viewer.Views;
    using Microsoft.Win32;
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for Ealdor.xaml
    /// </summary>
    partial class QueryDesigner : UserControl
    {
        internal QueryEditorModel Model { get; set; }
        internal QueryEditorView View { get; set; }

        public QueryDesigner()
        {
            InitializeComponent();
            this.FileExplorer.FileSelected += (s, args) =>
            {
                if (this.View.LoadFileCommand.CanExecute(null))
                {
                    this.View.LoadFileCommand.Execute(args.Value);
                }
            };
            this.DataContextChanged += Ealdor_DataContextChanged;
        }

        void Ealdor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is QueryEditorView)
            {
                this.View = (QueryEditorView)e.NewValue;
                this.Model = this.View.EditorModel;

                this.View.ShowFileExplorerCommand.CanExecuteTargets += () => this.FileExplorer.Visibility != Visibility.Visible;
                this.View.ShowFileExplorerCommand.ExecuteTargets += (o) =>
                {
                    this.FileExplorer.Visibility = System.Windows.Visibility.Visible;
                };

                this.View.ShowWizardCommand.CanExecuteTargets += () => this.Wizard.Visibility != Visibility.Visible && this.Model.Template != null;
                this.View.ShowWizardCommand.ExecuteTargets += (o) =>
                {
                    if (this.Model.Parameters.Count > 0)
                    {
                        this.Wizard.Visibility = System.Windows.Visibility.Visible;
                    }
                };

                this.View.SaveAsCommand.CanExecuteTargets += () => true;
                this.View.SaveAsCommand.ExecuteTargets += (s) =>
                    {
                        SaveFileDialog saveDialog = new SaveFileDialog();
                        saveDialog.FileName = String.IsNullOrEmpty(this.Model.FileName) ? "Query" : this.Model.FileName;
                        saveDialog.DefaultExt = ".linq";
                        saveDialog.Filter = "Linq documents (.linq)|*.linq";
                        if (saveDialog.ShowDialog() == true)
                        {
                            this.Model.FileName = saveDialog.FileName;
                            this.Model.Save();
                        }
                    };

                this.QueryEditor.DataContext = this.View;

                if (this.Model.FileName != null)
                {
                    if (File.Exists(this.Model.FileName))
                    {
                        this.FileExplorer.DirectoryRoot = this.Model.FileName;
                        this.Model.LoadFile(this.Model.FileName);
                    }
                }
            }
        }
    }
}
