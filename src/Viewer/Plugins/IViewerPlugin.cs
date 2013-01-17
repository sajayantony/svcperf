namespace EtlViewer.Viewer.Plugins
{
    public interface IViewerPlugin
    {
        string Name { get; }

        void Launch(EtlViewerContext context);
    }
}
