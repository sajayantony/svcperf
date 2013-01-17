namespace EtlViewer.Viewer.Views
{
    using EtlViewer.Viewer.Models;
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    
    class QueryEditorView : DependencyObject, INotifyPropertyChanged
    {
        public static DelegateCommand<object> NewCommand { get; set; }
        public static DelegateCommand<string> OpenCommand { get; set; }
        public DelegateCommand<string> SaveCommand { get; set; }
        public DelegateCommand<string> SaveAsCommand { get; set; }
        public DelegateCommand<object> ExitCommand { get; set; }
        public DelegateCommand<object> ShowFileExplorerCommand { get; private set; }
        public DelegateCommand<object> ShowWizardCommand { get; private set; }
        public DelegateCommand<string> LoadFileCommand { get; private set; }
        public DelegateCommand<object> BuildAndRunCommand { get; set; }
        public bool closed;

        public QueryEditorModel EditorModel { get; set; }
        public QueryModel QueryModel { get; set; }        


        public const char TitleSeperator = ' ';
        static QueryEditorView()
        {
            OpenCommand = new DelegateCommand<string>();
            NewCommand = new DelegateCommand<object>();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                                        typeof(string),
                                        typeof(QueryEditorView),
                                        new PropertyMetadata(string.Empty));

        public const string TextPropertyName = "Text";
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(TextPropertyName,
                                        typeof(string),
                                        typeof(QueryEditorView),
                                        new PropertyMetadata(string.Empty, OnTextChanged));

        static void OnTextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            QueryEditorView source = sender as QueryEditorView;
            string filename = args.NewValue as string;

            if (source != null && source.PropertyChanged != null)
            {
                source.PropertyChanged(sender, new PropertyChangedEventArgs(TextPropertyName));
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public QueryEditorView(QueryEditorModel editor, QueryModel queryModel)
        {
            this.EditorModel = editor;
            this.QueryModel = queryModel;
            this.ShowFileExplorerCommand = new DelegateCommand<object>();
            this.ShowWizardCommand = new DelegateCommand<object>();
            this.ExitCommand = new DelegateCommand<object>();
            this.SaveCommand = new DelegateCommand<string>();
            this.SaveAsCommand = new DelegateCommand<string>();
            this.LoadFileCommand = new DelegateCommand<string>();
            this.BuildAndRunCommand = new DelegateCommand<object>();

            this.SaveCommand.CanExecuteTargets += () => this.EditorModel.State == QueryModelState.Changed;
            this.SaveCommand.ExecuteTargets += (s) =>
            {
                if (this.EditorModel.State == QueryModelState.Changed
                    && !String.IsNullOrEmpty(this.EditorModel.FileName))
                {
                    this.EditorModel.Save();
                    this.QueryModel.LogActivity(string.Format("Saved {0} on {1}",
                                            this.EditorModel.FileName,
                                            DateTime.Now.ToString()));
                }
                else
                {
                    if (this.SaveAsCommand.CanExecute(null))
                    {
                        this.SaveAsCommand.Execute(null);
                    }
                }
            };

            this.LoadFileCommand.CanExecuteTargets += () => true;
            this.LoadFileCommand.ExecuteTargets += (filename) =>
                {
                    CancelEventArgs args = new CancelEventArgs();
                    this.ConfirmAndSave(args);
                    if (!args.Cancel)
                    {
                        this.EditorModel.LoadFile(filename);
                    }
                };

            this.BuildAndRunCommand.CanExecuteTargets += () => true;
            this.BuildAndRunCommand.ExecuteTargets += (o) =>
            {
                if (this.EditorModel.BuildCommand.CanExecute(null))
                {
                    this.EditorModel.BuildCommand.Execute(null);
                }

                if (this.QueryModel.RunCommand.CanExecute(null))
                {
                    this.QueryModel.RunCommand.Execute(null);
                }
            };

            this.EditorModel.PropertyChanged += (sender, args) =>
            {
                if (!String.IsNullOrEmpty(this.Title) && this.Title.Contains(TitleSeperator))
                {
                    this.Title = this.Title.Split(TitleSeperator)[0] + TitleSeperator + this.EditorModel.FileName;
                }
            };

            this.Text = this.EditorModel.QueryString;

            // Bind the Text property to QueryString
            // and setup the editor pane.
            BindingOperations.SetBinding(
                this,
                TextProperty,
                new Binding()
                {
                    Source = this.EditorModel,
                    Mode = BindingMode.TwoWay,
                    Path = new PropertyPath(QueryEditorModel.QueryStringPropertyName)
                });            
        }

        public void Close(CancelEventArgs args)
        {
            if (closed)
            {
                return;
            }

            if (args.Cancel)
            {
                return;
            }

            this.ConfirmAndSave(args);
            if (!args.Cancel)
            {
                this.closed = true;
                this.QueryModel.Dispose();
            }
        }

        internal void ConfirmAndSave(CancelEventArgs args)
        {
            if (this.EditorModel.State == QueryModelState.Changed)
            {
                var result = MessageBox.Show(
                    string.Format("Do you want to save your changes - {0}", this.Title),
                    "Save Confirmation- {0}",
                    MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    this.SaveCommand.Execute(null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    args.Cancel = true;
                }
            }
        }

    }
}
