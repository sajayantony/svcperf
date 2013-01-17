using EtlViewer.Viewer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EtlViewer.Viewer
{
    /// <summary>
    /// Interaction logic for StatsWindow.xaml
    /// </summary>
    partial class EventStatsWindow : Window
    {
        public EventStatsWindow()
        {
            InitializeComponent();
            SystemCommandHandler.Bind(this);
        }


        public EventStatsModel Model { get; set; }

        private void Window_Activated_1(object sender, EventArgs e)
        {

            this.StatsGrid.LoadEvents();
        }

   
    }
}
