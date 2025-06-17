using SQL_Export.Src;
using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SQL_Export
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
            this.DispatcherUnhandledException += App_DispathcerUnhandledException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var message = "Task Exception: " + e.Exception.Message;
            Logger.Error(message);
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.SetObserved();
        }

        private void App_DispathcerUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var message = "UI Thread Exception: " + e.Exception.Message;
            Logger.Error(message);
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            var message = "Domain Unhandled Exception: " + ex?.Message;
            Logger.Error(message);
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}
