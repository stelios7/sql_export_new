using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Data.Sql;
using ClosedXML.Excel;
using System.Windows;
using SQL_Export.Src;
using System.Data;
using System.Text;
using System.IO;
using System.Collections.Specialized;

namespace SQL_Export.ViewModels
{
	internal class MainWindowViewModel : BaseViewModel
	{
		#region Declarations

		public RelayCommand DisconnectSQL_Command => new RelayCommand(execute => DisconnectSQL(), canExecute => CanDisconnect());
		public RelayCommand ExtractData_Command => new RelayCommand(execute => ExtractData(), canExecute => CanExtract());
		public RelayCommand ConnectSQL_Command => new RelayCommand(execute => ConnectSQL(), canExecute => CanConnect());
		public RelayCommand DisConnectSQL_Command => new RelayCommand(execute => DisConnectSQL(), canExecute => CanDisConnect());

		private bool CanDisConnect()
		{
			if (SqlConnection == null) return false;
			return SqlConnection.State == System.Data.ConnectionState.Open;
		}

		private void DisConnectSQL()
		{
			SqlConnection.Close();
			SqlDatabases.Clear();
		}

		public RelayCommand Checkbox_Command => new RelayCommand(execute => { }, canExecute => { return true; });

		private bool CanExtract()
		{
			if (SqlConnection == null) return false;
			return SqlConnection.State == System.Data.ConnectionState.Open;
		}

		public ObservableCollection<System.Windows.Controls.CheckBox> DatabaseCheckboxList { get; set; }
		public ObservableCollection<string> SqlInstances { get; set; }

		private ObservableCollection<string> _sqlDatabases;
		public ObservableCollection<string> SqlDatabases
		{
			get { return _sqlDatabases; }
			set {
				if (_sqlDatabases != null)
				{
					// Unsubscribe from the previous CollectionChanged event
					_sqlDatabases.CollectionChanged -= SqlDatabases_CollectionChanged;
				}

				_sqlDatabases = value;

				if (_sqlDatabases != null)
				{
					// Subscribe toe the new CollectionChanged event
					_sqlDatabases.CollectionChanged += SqlDatabases_CollectionChanged;
				}

				OnPropertyChanged();
				OnPropertyChanged("IsComboBoxEnabled");
			}
		}

		// Event handler for when the collection changes
		private void SqlDatabases_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			// Notify that IsComboBoxEnabled has changed whenever the collection is modified
			OnPropertyChanged("IsComboBoxEnabled");
		}

		public bool IsComboBoxEnabled
		{
			get { return SqlDatabases != null && SqlDatabases.Count > 0; }
		}

		#region Properties


		private string _connectionState;

		public string ConnectionState
		{
			get { return _connectionState; }
			private set { 
				if (_connectionState != value)
				{
					_connectionState = value;
					OnPropertyChanged();
				}
			}
		}


		private SqlConnection _sqlConnection;
		private string _selectedSqlInstance;
		private string _selectedDatabase;
		private bool _isSupplierChecked;
		private bool _isButcherChecked;
		private string _password;
		private string _loginSql;
		private string _cnst;

		public SqlConnection SqlConnection
		{
			get { return _sqlConnection; }
			set
			{
				_sqlConnection = value;
				OnPropertyChanged();
			}
		}
		public string SelectedSqlInstance
		{
			get { return _selectedSqlInstance; }
			set
			{
				_selectedSqlInstance = value;
				OnPropertyChanged();
			}
		}
		public string SelectedSQLDatabase
		{
			get { return _selectedDatabase; }
			set
			{
				_selectedDatabase = value;
				OnPropertyChanged();
				//PopulateTables();
			}
		}
		public string ConnectionStatus
		{
			get { return _cnst; }
			set
			{
				_cnst = value;
				OnPropertyChanged();
			}
		}
		public string PasswordSQL
		{
			get { return _password; }
			set
			{
				_password = value;
				OnPropertyChanged();
			}
		}
		public string LoginSQL
		{
			get { return _loginSql; }
			set
			{
				_loginSql = value;
				OnPropertyChanged();
			}
		}
		public bool IsSupplier
		{
			get { return _isSupplierChecked; }
			set
			{
				_isSupplierChecked = value;
				OnPropertyChanged();
			}
		}
		public bool IsButcher
		{
			get { return _isButcherChecked; }
			set
			{
				_isButcherChecked = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#endregion

		#region Functions

		private string GetSQLQueryString(int opt = 0)
		{
			var sb = new StringBuilder();
			var begin_try = $@"BEGIN TRY
	CREATE INDEX idx_products_guid ON {SelectedSQLDatabase}.dbo.Products (guid);
	CREATE INDEX idx_products_wh_prguid ON {SelectedSQLDatabase}.dbo.Products_WH (PrGuid);
	CREATE INDEX idx_products_messureType ON {SelectedSQLDatabase}.dbo.Products (messureType);
	CREATE INDEX idx_products_barcodes_prguid ON {SelectedSQLDatabase}.dbo.Products_Barcodes (prguid);
	CREATE INDEX idx_products_wh_scaleTeamId ON {SelectedSQLDatabase}.dbo.Products_WH (scaleTeamId);
	CREATE INDEX idx_scaleteams_team_zig_id ON {SelectedSQLDatabase}.dbo.ScaleTeams (team_zig_id);
END TRY
BEGIN CATCH
END CATCH";
			var mainQuery = $@"SELECT 
Products.des AS 'ΠΕΡΙΓΡΑΦΗ',
Products.category_des AS 'ΚΑΤΗΓΟΡΙΑ',
MessureType.showdes 'ΜΟΝ ΜΕΤΡ',
PRODUCTS_WH.price1 AS 'ΤΙΜΗ', 
PRODUCTS_WH.fpa AS 'ΦΠΑ',
products_wh.qty AS 'ΠΟΣΟΤΗΤΑ',
ISNULL(Products_Barcodes.barcode,'') as 'Barcode',
ISNULL(LEFT(Products_Barcodes.barcode,7),
CONCAT('21',RIGHT(CONCAT('00000', id_external),5))) AS 'Barcode',
ISNULL(Products.category_des2, '') AS 'ΧΩΡΑ',
ISNULL(ScaleTeams.team_name, '') AS 'ΖΥΓΑΡΙΑ ΟΝΟΜΑ',
ISNULL(ScaleTeams.team_zig_id, '') AS 'ΖΥΓΑΡΙΑ id',
RIGHT(CONCAT('00000', Products.id_external), 5) AS 'PLU',
Products.countryImpName,
Products.countryFeedName
FROM [{SelectedSQLDatabase}].[dbo].[Products]
JOIN [{SelectedSQLDatabase}].[dbo].[Products_WH] ON Products.guid = Products_WH.PrGuid
LEFT JOIN [{SelectedSQLDatabase}].[DBO].[MessureType] ON Products.messureType = MessureType.id
LEFT JOIN [{SelectedSQLDatabase}].[DBO].[Products_Barcodes] ON Products.guid = Products_Barcodes.prguid 
LEFT JOIN [{SelectedSQLDatabase}].[dbo].[ScaleTeams] ON [{SelectedSQLDatabase}].[dbo].[Products_WH].[scaleTeamId] = [{SelectedSQLDatabase}].[dbo].[ScaleTeams].team_zig_id;";
			sb.AppendLine(begin_try);
			sb.AppendLine(mainQuery);
			var queryDefault = sb.ToString();
			var queryMarket = $@"BEGIN TRY
	CREATE INDEX idx_products_guid ON {SelectedSQLDatabase}.dbo.Products (guid);
	CREATE INDEX idx_products_wh_prguid ON {SelectedSQLDatabase}.dbo.Products_WH (PrGuid);
	CREATE INDEX idx_products_messureType ON {SelectedSQLDatabase}.dbo.Products (messureType);
	CREATE INDEX idx_products_barcodes_prguid ON {SelectedSQLDatabase}.dbo.Products_Barcodes (prguid);
	CREATE INDEX idx_products_wh_scaleTeamId ON {SelectedSQLDatabase}.dbo.Products_WH (scaleTeamId);
	CREATE INDEX idx_scaleteams_team_zig_id ON {SelectedSQLDatabase}.dbo.ScaleTeams (team_zig_id);
END TRY
BEGIN CATCH
END CATCH

SELECT 
Products.des AS 'ΠΕΡΙΓΡΑΦΗ',
Products.category_des AS 'ΚΑΤΗΓΟΡΙΑ',
Products.category_des2 AS 'ΚΑΤΗΓΟΡΙΑ 2',
MessureType.showdes 'ΜΟΝ ΜΕΤΡ',
PRODUCTS_WH.price1 AS 'ΤΙΜΗ', 
PRODUCTS_WH.fpa AS 'ΦΠΑ',
products_wh.qty AS 'ΠΟΣΟΤΗΤΑ',
ISNULL(products_barcodes.barcode, ''),
Products.id_external AS 'PLU'
FROM {SelectedSQLDatabase}.[dbo].[Products]
JOIN {SelectedSQLDatabase}.[dbo].[Products_WH] ON Products.guid = Products_WH.PrGuid
LEFT JOIN {SelectedSQLDatabase}.[DBO].[MessureType] ON Products.messureType = MessureType.id
LEFT JOIN {SelectedSQLDatabase}.[DBO].[Products_Barcodes] ON Products.guid = Products_Barcodes.prguid
LEFT JOIN {SelectedSQLDatabase}.[dbo].[ScaleTeams] ON {SelectedSQLDatabase}.[dbo].[Products_WH].[scaleTeamId] = {SelectedSQLDatabase}.[dbo].[ScaleTeams].team_zig_id;";
			return queryDefault;
		}

		private string GetConnectionString()
		{
			var srv = $@"{Environment.MachineName}\{SelectedSqlInstance}";
			var db = $@"{SelectedSQLDatabase}";
			return @$"Server={srv};Database={db};User ID={LoginSQL};Password={PasswordSQL};TrustServerCertificate=True;";
		}

		private string GetFileFilters()
		{
			var sb = new StringBuilder();
			sb.Append("Excel files (*.xlsx)|*.xlsx");
			return sb.ToString();
		}

		private void PopulateTables()
		{
			if (DatabaseCheckboxList.Count > 0)
			{
				DatabaseCheckboxList.Clear();
			}

			string connectionString = GetConnectionString();
			string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;";

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();

				using (SqlCommand command = new SqlCommand(query, connection))
				using (SqlDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						DatabaseCheckboxList.Add(new System.Windows.Controls.CheckBox
						{
							Content = reader["TABLE_NAME"].ToString(),
							IsChecked = false,
							Margin = new Thickness(2)
						});
					}
					SqlConnection.Close();
				}
			}
		}

		private bool CanDisconnect()
		{
			if (SqlConnection == null) return false;

			return SqlConnection.State == System.Data.ConnectionState.Open;
		}

		private void DisconnectSQL()
		{
			SqlConnection.Close();
			ConnectionStatus = "Disconnected";
			SqlDatabases.Clear();
		}

		private void ExtractData()
		{
			// FolderBrowserDialog to choose destination
			var ofd = new SaveFileDialog()
			{
				Title = "Αποθήκευση αρχείου...",
				Filter = GetFileFilters()
			};


			if (ofd.ShowDialog() == DialogResult.OK)
			{
				string selectedFile = ofd.FileName;

				try
				{
					using (SqlConnection connection = new SqlConnection(GetConnectionString()) { })
					{
						connection.Open();

						FileInfo newFile = new FileInfo(selectedFile);

						// Αν υπάρχει το αρχείο τότε το διαγράφω.
						if (newFile.Exists)
						{
							newFile.Delete();
							newFile = new FileInfo(selectedFile);
						}

						// Ανοίγω καινούργιο Workbook
						using (var workbook = new XLWorkbook())
						{
							DataTable dataTable = new DataTable();

							// Default query for products
							var command = new SqlCommand(GetSQLQueryString(), connection);
							var reader = command.ExecuteReader();
							dataTable.Load(reader);
							CreateWorksheet(dataTable, workbook, "extracted_products");

							if (IsSupplier)
							{
								var querySupplier = $"SELECT [afm], [name] FROM {SelectedSQLDatabase}.[dbo].[Promitheuths]";
								using (var newcom = new SqlCommand(querySupplier, connection))
								{
									using (var newreader = newcom.ExecuteReader())
									{
										var dt = new DataTable();
										dt.Load(reader);
										CreateWorksheet(dt, workbook, "suppliers");
									}
								}
							}

							workbook.SaveAs(selectedFile);
							System.Windows.MessageBox.Show("File created");
							Process.Start(new ProcessStartInfo
							{
								FileName = System.IO.Path.GetDirectoryName(selectedFile),
								UseShellExecute = true,
								Verb = "open"
							});
						}

						connection.Close();
					}
				}
				catch (Exception e)
				{
					System.Windows.MessageBox.Show($"Error {e.Message}");
				}

			}
		}

		private void CreateWorksheet(DataTable dt, XLWorkbook wb, string sName)
		{
			var worksheet = wb.Worksheets.Add(sName);

			// Write column headers
			for (int j = 0; j < dt.Columns.Count; j++)
			{
				worksheet.Cell(1, j + 1).Value = dt.Columns[j].ColumnName;
			}

			// Write data rows
			for (int j = 0; j < dt.Rows.Count; j++)
			{
				for (int k = 0; k < dt.Columns.Count; k++)
				{
					worksheet.Cell(j + 2, k + 1).Value = dt.Rows[j][k].ToString();
					//Debug.Print($@"Row {i} Column {j}");
				}
			}
		}

		private void ConnectSQL()
		{
			// Update UI
			ConnectionStatus = "Connecting...";
			string connectionString = @$"Server={Environment.MachineName}\{SelectedSqlInstance.Replace(@".\", "")};User ID={LoginSQL};Password={PasswordSQL};TrustServerCertificate=True;";
			//System.Windows.MessageBox.Show(connectionString);

			try
			{
				SqlConnection = new SqlConnection(connectionString);
				SqlConnection.Open();

				string query = "SELECT name FROM sys.databases WHERE state_desc = 'ONLINE'";
				using (SqlCommand command = new SqlCommand(query, SqlConnection))
				{
					// Execute the command and get a SqlDataReader to read the results
					using (SqlDataReader reader = command.ExecuteReader())
					{
						Debug.WriteLine("Available Databases:");

						// Loop through the results and print the database names
						while (reader.Read())
						{
							var db = (string)reader["name"];
							Debug.WriteLine(db);
							SqlDatabases.Add(db);
						}

						//OnPropertyChanged("IsComboBoxEnabled");
					}
				}

			}
			catch (Exception)
			{

				throw;
			}
			//using (SqlConnection = new SqlConnection(connectionString))
			//{
			//	SqlConnection.Open();

			//	using (SqlCommand command = new SqlCommand()
				
			//}

			//System.Windows.Forms.MessageBox.Show(SqlConnection.State.ToString());


			#region OLD FUNCTIONALITY

			//SqlDatabases.Clear();

			//try
			//{
			//	string connectionString = @$"Server={Environment.MachineName}\{SelectedSqlInstance};User ID={LoginSQL};Password={PasswordSQL};TrustServerCertificate=True;";
			//	SqlConnection = new SqlConnection(connectionString);

			//	// Create and open a connection to the SQL Server instance
			//	SqlConnection.Open();

			//	ConnectionStatus = "Connected";

			//	// SQL query to list all databases in the SQL Server instance
			//	string query = "SELECT name FROM sys.databases WHERE state_desc = 'ONLINE'";

			//	// Create a SqlCommand to execute the query
			//	using (SqlCommand command = new SqlCommand(query, SqlConnection))
			//	{
			//		// Execute the command and get a SqlDataReader to read the results
			//		using (SqlDataReader reader = command.ExecuteReader())
			//		{
			//			Debug.WriteLine("Available Databases:");

			//			// Loop through the results and print the database names
			//			while (reader.Read())
			//			{
			//				var db = (string)reader["name"];
			//				Debug.WriteLine(db);
			//				SqlDatabases.Add(db);
			//			}
			//		}
			//	}
			//}
			//catch (Exception ex)
			//{
			//	System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
			//	ConnectionStatus = "Disconnected";
			//}

			#endregion
		}

		private bool CanConnect()
		{
			#region OLD CONNECTION

			//var b = LoginSQL.Length > 0 && PasswordSQL.Length > 0 && SelectedSqlInstance.Length > 0;

			////if (SqlConnection == null) return false;

			//if (SqlConnection == null)
			//{
			//	return b;
			//}

			//return b && SqlConnection.State == ConnectionState.Closed;

			#endregion

			//var result = LoginSQL.Length > 0 && PasswordSQL.Length > 0 && SelectedSqlInstance.Length > 0 && SelectedSQLDatabase.Length > 0;
			var result = LoginSQL.Length > 0 && PasswordSQL.Length > 0 && SelectedSqlInstance.Length > 0;
			if (SqlConnection == null) return result;

			var b = SqlConnection.State == System.Data.ConnectionState.Closed;
			var c = SqlConnection.State == System.Data.ConnectionState.Broken;

			return result & (b || c);
		}

		private int GetTotalQueries()
		{
			if (IsSupplier && IsButcher)
			{
				return 2;
			}

			if (IsSupplier)
			{
				return 1;
			}
			return 1;
		}

		#endregion

		public MainWindowViewModel()
		{
			LoadTimers();
			Start();
		}

		private void LoadTimers()
		{
			System.Timers.Timer _timer = new System.Timers.Timer(500);
			_timer.Elapsed += (sender, args) => CheckConnectionState();
			_timer.Start();
		}

		private void CheckConnectionState()
		{
			if (SqlConnection == null) return;
			ConnectionState = SqlConnection.State.ToString();
		}

		private void Start()
		{
			SqlDatabases = new ObservableCollection<string>();
			SqlDatabases.CollectionChanged += SqlDatabases_CollectionChanged;
			// Skip SQL Instances γιατί αργούσαν πολύ να φορτώσουν.
			// await LoadInstances();
		}

		private async Task LoadInstances()
		{
			SqlInstances = new ObservableCollection<string>();
			SqlDatabases = new ObservableCollection<string>();
			DatabaseCheckboxList = new ObservableCollection<System.Windows.Controls.CheckBox>();

			try
			{
				SqlDataSourceEnumerator instance = SqlDataSourceEnumerator.Instance;
				DataTable table = new DataTable();
				while (table.Rows.Count == 0)
				{
					await Task.Run(() =>
					{
						// System.Windows.MessageBox.Show("Begin GetDataSources");
						ConnectionStatus = "Loading...";
						table = instance.GetDataSources();
						Debug.Print($"{table.Rows.Count}");
					});
				}

				// System.Windows.MessageBox.Show($"Sources loaded {table.Rows.Count}");
				ConnectionStatus = "Ready";
				string servername = Environment.MachineName;
				foreach (DataRow row in table.Rows)
				{
					await Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
					{
						SqlInstances.Add(row["InstanceName"].ToString());
					}));
				}
			}
			catch (Exception ex) { }
		}
	}
}
