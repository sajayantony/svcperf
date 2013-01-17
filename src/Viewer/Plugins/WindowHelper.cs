namespace EtlViewer.Viewer.Plugins
{
    using System.Windows;

    public class WindowHelper
    {
        public static void ApplyWindowStyles(Window window)
        {
            window.Style = Application.Current.Resources["MetroWindow"] as Style;
            SystemCommandHandler.Bind(window);
        }
    }
}
