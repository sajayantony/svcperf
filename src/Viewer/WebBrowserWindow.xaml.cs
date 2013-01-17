namespace EtlViewer
{
    using System.Windows;
    using System.ComponentModel;

    /// <summary>
    /// Interaction logic for WebBrowserWindow.xaml
    /// </summary>
    partial class WebBrowserWindow : Window
    {
        public WebBrowserWindow()
        {
            InitializeComponent();
            SystemCommandHandler.Bind(this);
        }
        /// <summary>
        /// If set simply hide the window rather than closing it when the user requests closing. 
        /// </summary>
        public bool HideOnClose;

        public bool CanGoForward { get { return Browser.CanGoForward; } }
        public bool CanGoBack { get { return Browser.CanGoBack; } }

        #region private
        private void BackClick(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack)
                Browser.GoBack();
        }
        private void ForwardClick(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoForward)
                Browser.GoForward();
        }
        /// <summary>
        /// We hide rather than close the editor.  
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (HideOnClose)
            {
                Hide();
                e.Cancel = true;
            }
        }
        /// <summary>
        /// The browser looses where it is when it resizes, which is very confusing to people
        /// Thus force a resync when the window resizes.  
        /// </summary>
        private void Browser_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (m_notFirst)
                Browser.Navigate(Browser.Source);
            m_notFirst = true;
        }
        bool m_notFirst;
        #endregion
    }
}
