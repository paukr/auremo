using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Auremo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show("Uncaught exception in Auremo.\n" +
                                           "Please take a screenshot of this message and send it to the developer.\n\n" +
                                           e.Exception.ToString(),
                                           "Auremo has crashed!");
        }
    }
}
