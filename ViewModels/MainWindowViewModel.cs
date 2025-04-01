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
using Microsoft.SqlServer.Management.Common;
using System.Linq.Expressions;
using DocumentFormat.OpenXml.InkML;
using System.Security.Principal;

namespace SQL_Export.ViewModels
{
	internal class MainWindowViewModel : BaseViewModel
	{
		#region Declarations

		public RelayCommand DisconnectSQL_Command => new RelayCommand(execute => DisconnectSQL(), canExecute => CanDisconnect());
		public RelayCommand ExtractData_Command => new RelayCommand(execute => ExtractData(), canExecute => CanExtract());
		public RelayCommand ConnectSQL_Command => new RelayCommand(execute => ConnectSQL(), canExecute => CanConnect());
		public RelayCommand Checkbox_Command => new RelayCommand(execute => { }, canExecute => { return true; });

        private bool CanDisconnect()
        {
            if (SqlConnection == null) return false;
            return SqlConnection.State == System.Data.ConnectionState.Open;
        }
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


		private SqlConnection _sqlConnection;
		private string _selectedSqlInstance;
		private string _selectedDatabase;
		private string _connectionState;
		private bool _isSupplierChecked;
		private bool _isButcherChecked;
		private string _password;
		private string _loginSql;

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
		public string ConnectionState
		{
			get { return _connectionState; }
			private set { 
				if (_connectionState != value)
				{
					_connectionState = value;
					OnPropertyChanged(nameof(ConnectionState));
				}
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
			string query_create_index = $@"BEGIN TRY
	CREATE INDEX idx_products_guid ON {SelectedSQLDatabase}.dbo.Products (guid);
	CREATE INDEX idx_products_wh_prguid ON {SelectedSQLDatabase}.dbo.Products_WH (PrGuid);
	CREATE INDEX idx_products_messureType ON {SelectedSQLDatabase}.dbo.Products (messureType);
	CREATE INDEX idx_products_barcodes_prguid ON {SelectedSQLDatabase}.dbo.Products_Barcodes (prguid);
	CREATE INDEX idx_products_wh_scaleTeamId ON {SelectedSQLDatabase}.dbo.Products_WH (scaleTeamId);
	CREATE INDEX idx_scaleteams_team_zig_id ON {SelectedSQLDatabase}.dbo.ScaleTeams (team_zig_id);
END TRY
BEGIN CATCH
END CATCH";
			string query_main_butcher = $@"SELECT 
pr.des AS 'ΠΕΡΙΓΡΑΦΗ',
pr.category_des AS 'ΚΑΤΗΓΟΡΙΑ',
ISNULL(pr.category_des2, '') AS 'ΥΠΟΚΑΤΗΓΟΡΙΑ',
CASE
	WHEN mt.showdes = 'TEM' THEN 'ΤΕΜ'
	WHEN mt.showdes = 'Kg' THEN 'ΚΙΛ'
	ELSE mt.showdes
END  AS 'ΜΟΝ ΜΕΤΡ',
pw.price1 AS 'ΤΙΜΗ', 
pw.fpa AS 'ΦΠΑ',
ISNULL(pb.barcode,'') as 'ΚΩΔΙΚΟΣ',
ISNULL(LEFT(pb.barcode,7),
CONCAT('21',RIGHT(CONCAT('00000', id_external),5))) AS 'ΚΩΔΙΚΟΣ ΖΥΓ',
ISNULL(st.team_name, '') AS 'ΖΥΓΑΡΙΑ ΟΝΟΜΑ',
ISNULL(st.team_zig_id, '') AS 'ΖΥΓΑΡΙΑ id',
RIGHT(CONCAT('00000', pr.id_external), 5) AS 'PLU',
pr.countryImpName as 'ΧΩΡΑ ΓΕΝΝΗΣΗΣ',
pr.countryFeedName AS 'ΧΩΡΑ ΕΚΤΡΟΦΗΣ',
pw.qty AS 'ΠΟΣΟΤΗΤΑ'
FROM [dbo].[Products] pr
JOIN [dbo].[Products_WH] pw ON pr.guid = pw.PrGuid
LEFT JOIN [DBO].[MessureType] mt ON pr.messureType = mt.id
LEFT JOIN [DBO].[Products_Barcodes] pb ON pr.guid = pb.prguid 
LEFT JOIN [dbo].[ScaleTeams] st ON pw.[scaleTeamId] = st.team_zig_id
order by pr.des";
			sb.AppendLine(query_create_index);
			sb.AppendLine(query_main_butcher);
			File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"), sb.ToString());
			return sb.ToString();
		}

        private string GetSQLCustomers()
        {
            return $@"SELECT 
	afm AS 'ΑΦΜ', 
	CASE phone_1
		WHEN '1' then ''
		ELSE phone_1
	END AS 'ΤΗΛΕΦΩΝΟ', 
	ISNULL(email, '') as 'EMAIL',
	creditMoney AS 'ΥΠΟΛΟΙΠΟ',
	bonus_points AS 'ΠΟΝΤΟΙ'
	FROM [dbo].[Customers]";
        }

        private string GetSQLPromitheftes()
        {
            return $@"SELECT afm AS 'ΑΦΜ' FROM [dbo].[Promitheuths]";
        }

		private string GetSQL_Timokatalogoi()
		{
			return $@"SELECT       
	dbo.Customers.afm as 'ΑΦΜ', 
	CONCAT('21',RIGHT(CONCAT('00000', id_external),5)) AS 'ΚΩΔΙΚΟΣ ΖΥΓ',
	dbo.Price_PriceList.priceXondriki AS 'ΤΙΜΗ ΤΙΜΟΚΑΤΑΛΟΓΟΥ'
FROM            dbo.Customers INNER JOIN
                         dbo.PriceList ON dbo.Customers.priceList = dbo.PriceList.listGuid INNER JOIN
                         dbo.Price_PriceList ON dbo.PriceList.listGuid = dbo.Price_PriceList.listguid INNER JOIN
                         dbo.Products ON dbo.Price_PriceList.prguid = dbo.Products.guid INNER JOIN
                         dbo.Products_WH ON dbo.Products.guid = dbo.Products_WH.PrGuid
						 order by afm";
		}

        private string GetSQLSoftwareInfo()
		{
			return @$"
            SELECT [title] as 'ΕΠΩΝΥΜΙΑ'
      ,[profession] AS 'ΕΠΑΓΓΕΛΜΑ' 
      ,[street] AS 'ΔΙΕΥΘΥΝΣΗ'
      ,[region] AS 'ΠΕΡΙΟΧΗ'
      ,[city] AS 'ΠΟΛΗ'
      ,[zip] AS 'ΤΚ'
      ,[afm] AS	'ΑΦΜ'
      ,[doy] AS 'ΔΟΥ'
      ,[tel1] AS 'ΤΗΛΕΦΩΝΟ'
      ,[mobile] AS 'ΚΙΝΗΤΟ'
      ,[email] AS 'EMAIL'
      ,[shopid] AS 'ΚΩΔΙΚΟΣ ΚΑΤΑΣΤΗΜΑΤΟΣ'
      ,[sn] AS 'ΣΕΙΡΙΑΚΟ'
            FROM [dbo].[Info_Software]";
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

		private void ExtractData()
		{
			var fbd = new FolderBrowserDialog
			{
				Description = "Επιλογή φακέλου...",
				ShowNewFolderButton = true
			};

			if (fbd.ShowDialog() == DialogResult.OK)
			{
				string selectedFolder = fbd.SelectedPath;

				try
				{
					using (SqlConnection connection = new SqlConnection(GetConnectionString()))
					{
						connection.Open();
						CreateWorkbook(connection, GetSQLQueryString(), selectedFolder, "products");

						// TODO: Create SQL Query for duplicates opos ta thelei o Alex
						//CreateWorkbook(connection, GetSQL_Duplicates(), selectedFolder, "diplotipa");
                        CreateWorkbook(connection, GetSQLCustomers(), selectedFolder, "customers");
						CreateWorkbook(connection, GetSQLPromitheftes(), selectedFolder, "suppliers");
						CreateWorkbook(connection, GetSQLSoftwareInfo(), selectedFolder, "information");
						CreateWorkbook(connection, GetSQL_Timokatalogoi(), selectedFolder, "timokatalogoi");
                    }

					System.Windows.MessageBox.Show("Files created.");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = System.IO.Path.GetDirectoryName(selectedFolder),
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
				catch (Exception ex)
				{
					Debug.Print(ex.Message);
				}
			}
		}

        private void CreateWorkbook(SqlConnection connection, string query, string directory, string type)
        {
            using (var workbook = new XLWorkbook())
            {
                DataTable dataTable = new DataTable();

                // CREATE WORKSHEETS
                CreateWorksheet(connection, query, workbook, type);

                // SAVE WORKBOOK
				var wb_filepath = Path.Combine(directory, type + ".xlsx");
                workbook.SaveAs(wb_filepath);
            }
        }

        private void CreateWorksheet(SqlConnection conn, string sql, XLWorkbook wb, string sName)
		{
			DataTable dt = new DataTable();
			SqlCommand command = new SqlCommand(sql, conn);
			dt.Load(command.ExecuteReader());

			if (sName == "products")
			{
				DataTable d2 = new DataTable();

                // Step 1: Identify one barcode per 'des' as the primary barcode
                string query_step1 = $@"WITH CTE AS (
    SELECT 
        pr.des, 
        pb.barcode,
        ROW_NUMBER() OVER (PARTITION BY pr.des, pw.price1 ORDER BY (SELECT NULL)) AS rn
    FROM [dbo].[Products] pr
JOIN [dbo].[Products_WH] pw ON pr.guid = pw.PrGuid
LEFT JOIN [DBO].[MessureType] mt ON pr.messureType = mt.id
LEFT JOIN [DBO].[Products_Barcodes] pb ON pr.guid = pb.prguid 
LEFT JOIN [dbo].[ScaleTeams] st ON pw.[scaleTeamId] = st.team_zig_id
)
SELECT des, barcode AS PrimaryBarcode
INTO #PrimaryBarcodes
FROM CTE
WHERE rn = 1;";

                // Step 2: Create a mapping table with the primary barcode and the rest barcodes
                string query_step2 = $@"WITH CTE AS (
    SELECT 
        pr.des, 
        pb.barcode,
		pw.price1,
        ROW_NUMBER() OVER (PARTITION BY pr.des ORDER BY pb.barcode) AS rn
    FROM [dbo].[Products] pr
JOIN [dbo].[Products_WH] pw ON pr.guid = pw.PrGuid
LEFT JOIN [DBO].[MessureType] mt ON pr.messureType = mt.id
LEFT JOIN [DBO].[Products_Barcodes] pb ON pr.guid = pb.prguid 
LEFT JOIN [dbo].[ScaleTeams] st ON pw.[scaleTeamId] = st.team_zig_id
)
SELECT p.PrimaryBarcode, c.barcode AS AdditionalBarcode
FROM CTE c
JOIN #PrimaryBarcodes p ON c.des = p.des
WHERE c.rn > 1;";

				string drop_temp_table_query = "DROP TABLE #PrimaryBarcodes;";

				using (SqlCommand cmd = conn.CreateCommand())
				{
					cmd.CommandText = query_step1;
					cmd.ExecuteNonQuery();

					cmd.CommandText = query_step2;
					d2.Load(cmd.ExecuteReader());

					cmd.CommandText = drop_temp_table_query;
                    cmd.ExecuteNonQuery();
                }
				//	// Run first query
				//	SqlCommand cmd = new SqlCommand(query_step1, conn);
				//cmd.ExecuteNonQuery();

				//// Run second query
				//cmd = new SqlCommand(query_step2, conn);
    //            dt.Load(cmd.ExecuteReader());

				//// Drop temp table
    //            cmd = new SqlCommand(drop_temp_table_query, conn);
				//cmd.ExecuteNonQuery();

				var barcodesToRemove = d2.AsEnumerable()
					.Select(row => row.Field<string>("AdditionalBarcode"))
					.ToHashSet();
				var rowsToDelete = dt.AsEnumerable()
					.Where(row => barcodesToRemove.Contains(row.Field<string>("ΚΩΔΙΚΟΣ ΖΥΓ")))
					.ToList();

				foreach (var row in rowsToDelete)
				{
					dt.Rows.Remove(row);
				}
            }

                var worksheet = wb.Worksheets.Add(sName);

			// Write column headers
			for (int j = 0; j < dt.Columns.Count; j++)
			{
				var c = dt.Columns[j].ColumnName;
					worksheet.Cell(1, j + 1).Value = c;
			}

			// Write data rows
			for (int j = 0; j < dt.Rows.Count; j++)
			{
				for (int k = 0; k < dt.Columns.Count; k++)
				{
					worksheet.Cell(j + 2, k + 1).Value = dt.Rows[j][k].ToString().Replace(",",".");
					//Debug.Print($@"Row {i} Column {j}");
				}
			}

			// Adjust column width to fit content
			worksheet.Columns().AdjustToContents();
		}
        private void DisconnectSQL()
        {
			//System.Windows.MessageBox.Show("Disconnect");
            SqlConnection.Close();
			SqlConnection.Dispose();
            SqlDatabases.Clear();
        }

        private void ConnectSQL()
		{
			string connectionString = @$"Server={Environment.MachineName}\{SelectedSqlInstance.Replace(@".\", "")};User ID={LoginSQL};Password={PasswordSQL};TrustServerCertificate=True;Connect Timeout=5;";

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
					}
				}

			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message);
			}

		}

		private bool CanConnect()
		{
			var result = LoginSQL.Length > 0 && PasswordSQL.Length > 0 && SelectedSqlInstance.Length > 0;
			if (SqlConnection == null) return result;
			if (SqlConnection.State == System.Data.ConnectionState.Open) return false;

            var b = SqlConnection.State == System.Data.ConnectionState.Closed;
			var c = SqlConnection.State == System.Data.ConnectionState.Broken;

			return result & (b || c);
		}

		#endregion

		public MainWindowViewModel()
		{
			LoadTimers();
			Start();
		}

		private void LoadTimers()
		{
			DispatcherTimer t = new DispatcherTimer();
			t.Interval = TimeSpan.FromMilliseconds(100);
			t.Tick += new EventHandler((o, e) => CheckConnectionState());
			t.Start();
		}

		private void CheckConnectionState()
		{
			if (SqlConnection == null)
			{
				ConnectionState = "None";
				return;
			}
			ConnectionState = SqlConnection.State.ToString();
		}

		private void Start()
		{
			SqlDatabases = new ObservableCollection<string>();
			SqlDatabases.CollectionChanged += SqlDatabases_CollectionChanged;
		}

	}
}
