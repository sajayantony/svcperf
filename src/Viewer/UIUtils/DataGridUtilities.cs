namespace EtlViewer.Viewer.UIUtils
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using System.Windows.Input;
    
    static class DataGridUtilities
    {
        public static void CopyWithHeaders(DataGrid grid)
        {
            var previous = grid.ClipboardCopyMode;
            grid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            ApplicationCommands.Copy.Execute(null, grid);
            grid.ClipboardCopyMode = previous;
        }

        public static void SetDynamicItemSource(DataGrid grid, string message)
        {
            List<MessageContainer> msg = new List<MessageContainer>();
            msg.Add(new MessageContainer() { Message = message });
            grid.ItemsSource = msg;
        }

        class MessageContainer
        {
            public String Message { get; set; }
        }
    }

}
