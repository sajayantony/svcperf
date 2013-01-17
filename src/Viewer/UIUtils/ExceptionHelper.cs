namespace EtlViewer
{
    using EtlViewer.Viewer;
    using System;
    using System.Windows;
    using System.Windows.Threading;

    class ExceptionHelper
    {
        public static LogViewer logviewer { get; set; }

        public static void HanldeUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Exception exception = e.Exception;
            exception = ShowException(exception);
            e.Handled = true;
        }

        public static Exception ShowException(Exception exception)
        {
            string message = Logger.Log(exception);
            MessageBox.Show("You can use the LOG link on the bottom right to report this ERROR.\n" + message,
                "Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return exception;
        }
    }
}
