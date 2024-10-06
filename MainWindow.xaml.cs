using SQL_Export.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SQL_Export
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public enum CONNECTION_STATUS
		{
			Disconnected,
			Connecting,
			Connected
		}

		public MainWindow()
		{
			InitializeComponent();
			MainWindowViewModel vm = new MainWindowViewModel();
			this.DataContext = vm;
		}

		private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e)
		{

		}
	}
}