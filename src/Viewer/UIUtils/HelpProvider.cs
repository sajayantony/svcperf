using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace EtlViewer.Viewer.UIUtils
{
    class HelpProvider
    {
        public static event ExecutedRoutedEventHandler OnHelp;

        public static string GetHelp(DependencyObject obj)
        {
            return (string)obj.GetValue(HelpProperty);
        }


        public static void SetHelp(DependencyObject obj, string value)
        {
            obj.SetValue(HelpProperty, value);
        }


        public static readonly DependencyProperty HelpProperty =
            DependencyProperty.RegisterAttached(

                           "Help", typeof(string), typeof(HelpProvider));

        static HelpProvider()
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(FrameworkElement),
                new CommandBinding(
                    ApplicationCommands.Help,
                    new ExecutedRoutedEventHandler(Executed),
                    new CanExecuteRoutedEventHandler(CanExecute)));
        }


        static private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            if (HelpProvider.GetHelp(senderElement) != null)
                e.CanExecute = true;
        }


        static private void Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (OnHelp != null)
            {
                OnHelp(sender, e);
            }
        } 
    }
}
