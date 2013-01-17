namespace EtlViewer.Viewer.UIUtils
{
    using System.Windows.Input;
    using System;
    using System.Windows.Controls;

    internal class CopyWithHeadersCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            DataGrid grid = parameter as DataGrid;
            if (grid != null)
            {
                DataGridUtilities.CopyWithHeaders(grid);
            }
        }
    }
}
