//using Microsoft.Windows.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

public static class SystemCommandHandler
{
    public static void Bind(Window window)
    {        
        window.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CommandBinding_Executed_Close, CanExecute));
        window.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, CommandBinding_Executed_Maximize, CanExecute));
        window.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, CommandBinding_Executed_Minimize, CanExecute));
        window.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, CommandBinding_Executed_Restore, CanExecute));
        window.CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, CommandBinding_Executed_ShowSystemMenu, CanExecute));
    }

    private static void CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    public static void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
    {
        SystemCommands.CloseWindow((Window)e.Parameter);
    }

    public static void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow((Window)e.Parameter);
    }

    public static void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
    {
        SystemCommands.MaximizeWindow((Window)e.Parameter);
    }

    public static void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
    {
        SystemCommands.RestoreWindow((Window)e.Parameter);
    }

    public static void CommandBinding_Executed_ShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
    {
        Window _window = (Window)e.Parameter;
        Point _point;
        if (_window.WindowState == WindowState.Maximized)
        {
            _point = new Point(20, 20);
        }
        else
        {
            _point = new Point(_window.Left + 20, _window.Top + 20);
        }

        SystemCommands.ShowSystemMenu((Window)e.Parameter, _point);
    }
}
