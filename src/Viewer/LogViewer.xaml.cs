namespace EtlViewer.Viewer
{
    using EtlViewer.Viewer.Models;
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Utilities;
    
    class LogEntry
    {
        public bool IsError { get; set; }
        public string LogMessage { get; set; }
        public string DateTime { get; set; }
        public override string ToString()
        {
            return string.Format("{0}{1} | {2}",
                                (IsError ? "Error: " : string.Empty),
                                DateTime,
                                LogMessage);

        }
    }

    /// <summary>
    /// Interaction logic for LogViewer.xaml
    /// </summary>
    partial class LogViewer : Window
    {
        LogEntries entries;
        internal MainModel Model { get; set; }
        static bool CanSendReport { get; set; }

        public static DelegateCommand<object> CloseCommand { get; set; }
        public static DelegateCommand<object> ShowLog { get; set; }

        static LogViewer()
        {
            CloseCommand = new DelegateCommand<object>();
            CloseCommand.CanExecuteTargets += () => true;
            CloseCommand.ExecuteTargets += (e) =>
                {
                    if (e is Window)
                    {
                        ((Window)e).Close();
                    }
                };

            ShowLog = new DelegateCommand<object>();
        }


        public LogViewer()
        {
            InitializeComponent();
            SystemCommandHandler.Bind(this);

            entries = new LogEntries();
            this.logList.ItemsSource = entries;
            this.GetDiagnostics();
            this.DataContextChanged += LogViewer_DataContextChanged;
            CanSendReport = true;
            Logger.CollectionChanged += this.OnLog;
            ShowLog.CanExecuteTargets += () => true;
            ShowLog.ExecuteTargets += (e) =>
            {
                this.Show();
                this.Activate();
            };
        }

        void LogViewer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainModel)
            {
                this.Model = (MainModel)e.NewValue;
                this.Model.SendFeedbackCommand.CanExecuteTargets += () => CanSendReport;
                this.Model.SendFeedbackCommand.ExecuteTargets += (s) =>
                    {
                        string message = "Version:" + SupportFiles.AppVersion + "\n" + (s == null ? string.Empty : s.ToString());
                        if (CanSendReport)
                        {
                            bool completed = false;
                            if (!MailHelpers.MailHellper.Failed)
                            {
                                string fileName = MailHelpers.MailHellper.TryCreateScreenshot(this);
                                completed = MailHelpers.MailHellper.TrySendEmailWithOutlook(fileName, message);
                            }

                            if (!completed)
                            {
                                try
                                {
                                    var navigate = Uri.EscapeUriString(string.Format("mailto:sajaya@microsoft.com?Subject=SvcPerf Feedback - Report&body={0}", message));
                                    if (navigate.Length > 1000)
                                    {
                                        navigate = navigate.Substring(0, 1000);
                                    }
                                    Process.Start(new ProcessStartInfo(navigate));
                                }
                                catch (Exception ex)
                                {
                                    CanSendReport = false;
                                    Logger.Log(ex);
                                }
                            }
                        }

                    };
                this.Model.SendFeedbackCommand.CanExecute(null);
            }
        }

        private void GetDiagnostics()
        {
            this.txtVersion.Text = Utilities.SupportFiles.AppVersion;
            this.txtAppRoot.Text = Utilities.SupportFiles.SupportFileDir;
            this.txtAssemblyCache.Text = Path.GetFullPath(EtlViewer.QueryFx.ManifestCompiler.TempAssemblyCache);
        }

        internal void Log(LogEntry entry)
        {
            if (this.entries.Count > 100)
            {
                for (int i = this.entries.Count - 1; i >= 50; i--)
                {
                    this.entries.RemoveAt(i);
                }
            }

            this.entries.Add(entry);
            this.logList.ScrollIntoView(entry);
            this.logList.SelectedItem = entry;
        }

        class LogEntries : ObservableCollection<LogEntry>
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        void OnLog(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                foreach (var item in e.NewItems)
                {
                    if (item != null)
                    {
                        if (item is LogEntry)
                        {
                            this.Log(item as LogEntry);
                        }
                        else
                        {
                            this.Log(new LogEntry() { DateTime = DateTime.Now.ToString(), LogMessage = item.ToString(), IsError = false });
                        }
                    }
                }
            }));
        }

        private void Hyperlink_RequestNavigate_1(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink thisLink = (Hyperlink)sender;
            string navigateUri = thisLink.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
    }

    namespace MailHelpers
    {
        using Microsoft.Office.Interop.Outlook;

        class MailHellper
        {
            public static bool Failed { get; private set; }

            public static string TryCreateScreenshot(LogViewer logViewer)
            {
                try
                {
                    Window win = logViewer;
                    RenderTargetBitmap bmp = new RenderTargetBitmap((int)win.Width, (int)win.Height, 96, 96, PixelFormats.Pbgra32);
                    bmp.Render(win);

                    string PicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Picture");
                    if (!Directory.Exists(PicPath))
                        Directory.CreateDirectory(PicPath);

                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmp));

                    string filePath = Path.Combine(PicPath, string.Format("{0:MMddyyyyHHmmss}.png", DateTime.Now));
                    using (Stream stream = File.Create(filePath))
                    {
                        encoder.Save(stream);
                    }
                    return Path.GetFullPath(filePath);
                }
                catch (System.Exception ex)
                {
                    Logger.Log(ex);
                    Failed = true;
                }

                return string.Empty;
            }

            public static bool TrySendEmailWithOutlook(string filename, string message)
            {
                if (Failed)
                {
                    return false;
                }

                try
                {
                    return SendEmailWithOutlook(filename, message);
                }
                catch (System.Exception ex)
                {
                    Failed = true;
                    Logger.Log(ex);
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static bool SendEmailWithOutlook(string filename, string message)
            {
                var msgFile = Path.GetFileNameWithoutExtension(filename) + ".msg";
                Microsoft.Office.Interop.Outlook.Application outlook = new Microsoft.Office.Interop.Outlook.Application();
                MailItem mi = outlook.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
                mi.To = "sajaya@microsoft.com";
                mi.Body = message;
                mi.Attachments.Add(filename);
                mi.Subject = "SvcPerf Report";
                Inspector inspector = mi.GetInspector;
                inspector.Activate();
                return true;
            }
        }
    }

}
