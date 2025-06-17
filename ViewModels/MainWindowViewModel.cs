using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using ClosedXML.Excel;
using System.Windows;
using SQL_Export.Src;
using System.Data;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SQL_Export.ViewModels
{
    internal class MainWindowViewModel : BaseViewModel
    {
        #region DECLARATIONS

        #region HANDLERS

        // Event handler for when the collection changes
        private void SqlDatabases_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Notify that IsComboBoxEnabled has changed whenever the collection is modified
            OnPropertyChanged("IsComboBoxEnabled");
        }

        #endregion

        #region RELAY COMMANDS

        public RelayCommand RestoreDB_Command => new RelayCommand(execute => RestoreDatabase(SelectedSqlInstance, DatabaseToRestore, "C:\\Users\\Public\\Documents\\SQL_Export\\Backup.bak"), canExecute => CanRestoreDB());
        private bool CanRestoreDB()
        {
            if (SqlConnection == null) return false;

            return DatabaseToRestoreName.Length > 0 && File.Exists(DatabaseToRestore) && SqlConnection.State == System.Data.ConnectionState.Open;
        }

        public RelayCommand SelectLocalDatabase_command => new RelayCommand(execute => SelectLocalDatabase(), canExecute => true);

        public RelayCommand DisconnectSQL_Command => new RelayCommand(execute => DisconnectSQL(), canExecute => CanDisconnect());
        private bool CanDisconnect()
        {
            if (SqlConnection == null) return false;
            return SqlConnection.State == System.Data.ConnectionState.Open;
        }

        public RelayCommand ExtractData_Command => new RelayCommand(execute => ExtractData(), canExecute => CanExtract());
        private bool CanExtract()
        {
            if (SqlConnection == null) return false;
            return SqlConnection.State == System.Data.ConnectionState.Open;
        }

        public RelayCommand ConnectSQL_Command => new RelayCommand(execute => ConnectSQL(), canExecute => CanConnect());
        private bool CanConnect()
        {
            var result = LoginSQL.Length > 0 && PasswordSQL.Length > 0 && SelectedSqlInstance.Length > 0;
            if (SqlConnection == null) return result;
            if (SqlConnection.State == System.Data.ConnectionState.Open) return false;

            var b = SqlConnection.State == System.Data.ConnectionState.Closed;
            var c = SqlConnection.State == System.Data.ConnectionState.Broken;

            return result & (b || c);
        }

        public RelayCommand Checkbox_Command => new RelayCommand(execute => { }, canExecute => { return true; });

        #endregion

        private List<string> ExcelFilesToZip = new List<string>();
        private ObservableCollection<string> _sqlDatabases;
        public ObservableCollection<System.Windows.Controls.CheckBox> DatabaseCheckboxList { get; set; }
        public ObservableCollection<string> SqlInstances { get; set; }
        public ObservableCollection<string> SqlDatabases
        {
            get { return _sqlDatabases; }
            set
            {
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

        #region ENABLERS

        public bool IsComboBoxEnabled
        {
            get { return SqlDatabases != null && SqlDatabases.Count > 0; }
        }

        #endregion

        #region PROPERTIES

        private string _connectionString;

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }


        public string DatabaseToRestoreName
        {
            get { return _databaseToRestoreName; }
            set
            {
                _databaseToRestoreName = value;
                OnPropertyChanged(nameof(DatabaseToRestoreName));
            }
        }
        private string _databaseToRestoreName;

        private string _databaseToRestore;
        public string DatabaseToRestore
        {
            get { return _databaseToRestore; }
            set
            {
                _databaseToRestore = value;
                OnPropertyChanged(nameof(DatabaseToRestore));
            }
        }

        private bool _isLocalDatabase = true;
        public bool IsLocalDatabase
        {
            get { return _isLocalDatabase; }
            set
            {
                _isLocalDatabase = value;
                OnPropertyChanged(nameof(IsLocalDatabase));
            }
        }


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
            set
            {
                _connectionState = value;
                OnPropertyChanged(nameof(ConnectionState));
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

        DispatcherTimer ConnectionStatusTimer;
        private void LoadTimers()
        {
            ConnectionStatusTimer = new DispatcherTimer();
            ConnectionStatusTimer.Interval = TimeSpan.FromMilliseconds(100);
            ConnectionStatusTimer.Tick += new EventHandler((o, e) => CheckConnectionState());
            ConnectionStatusTimer.Start();
            Logger.Info("Timers started.");
        }

        private void Start()
        {
            SqlDatabases = new ObservableCollection<string>();
            SqlDatabases.CollectionChanged += SqlDatabases_CollectionChanged;

            IsLocalDatabase = false;
        }

        private void SelectLocalDatabase()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Επιλογή αρχείου βάσης...",
                Filter = "All Files|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                DatabaseToRestore = ofd.FileName;
            }
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

                    SqlConnection _conn = null;
                    if (IsLocalDatabase)
                    {
                        SelectedSQLDatabase = DatabaseToRestoreName;
                        var sqlInfo = new Tuple<string, string> (SelectedSqlInstance, DatabaseToRestoreName);
                        _conn = new SqlConnection(SqlQueries.GetConnectionString(sqlInfo, LoginSQL, PasswordSQL));
                    }
                    else
                    {
                        var sqlInfo = new Tuple<string, string>(SelectedSqlInstance, SelectedSQLDatabase);
                        _conn = new SqlConnection(SqlQueries.GetConnectionString(sqlInfo, LoginSQL, PasswordSQL));
                    }
                    using (SqlConnection connection = _conn)
                    {
                        connection.Open();

                        using (SqlCommand cmd = connection.CreateCommand())
                        {
                            // CREATE INDEXES
                            cmd.CommandText = SqlQueries.GetSQL_CreateIndexes();
                            cmd.ExecuteNonQuery();

                            // DROP VIEW IF IT EXISTS
                            cmd.CommandText = SqlQueries.GetSQL_DropView();
                            cmd.ExecuteNonQuery();

                            // CREATE VIEW AGAIN
                            cmd.CommandText = SqlQueries.GetSQL_CreateView();
                            cmd.ExecuteNonQuery();
                        }

                        CreateWorkbook(connection, SqlQueries.GetSQLQueryString(), selectedFolder, "products");
                        CreateWorkbook(connection, SqlQueries.GetSQL_DuplicateBarcodes(), selectedFolder, "duplicates");
                        CreateWorkbook(connection, SqlQueries.GetSQLCustomers(), selectedFolder, "customers");
                        CreateWorkbook(connection, SqlQueries.GetSQLPromitheftes(), selectedFolder, "suppliers");
                        CreateWorkbook(connection, SqlQueries.GetSQLSoftwareInfo(), selectedFolder, "information");
                        CreateWorkbook(connection, SqlQueries.GetSQL_Timokatalogoi(), selectedFolder, "timokatalogoi");
                    }

                    //ZipFiles(selectedFolder);
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
                    System.Windows.MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Error(ex.Message);
                }
            }
        }

        private void ZipFiles(string dir)
        {
            using (FileStream zipToCreate = new FileStream(dir, FileMode.Create, FileAccess.ReadWrite))
            {
                using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
                {
                    foreach (string file in ExcelFilesToZip)
                    {
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
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
                ExcelFilesToZip.Add(wb_filepath);
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

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SqlQueries.GetSQL_DuplicateBarcodes();
                    d2.Load(cmd.ExecuteReader());
                }

                var barcodesToRemove = d2.AsEnumerable()
                    .Select(row => row.Field<string>("AdditionalBarcode"))
                    .ToHashSet();
                var rowsToDelete = dt.AsEnumerable()
                    .Where(row => barcodesToRemove.Contains(row.Field<string>("ΚΩΔΙΚΟΣ")))
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
                    worksheet.Cell(j + 2, k + 1).Value = dt.Rows[j][k].ToString().Replace(",", ".").Trim();
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

            Logger.Info($"Disconnected from {SelectedSqlInstance}");
        }

        public void RestoreDatabase(string sqlInstance, string databaseName, string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                ConnectionString = SqlQueries.GetConnectionString(new Tuple<string, string>(sqlInstance, databaseName));
            }

            string v_databaseName = databaseName.Replace(".bak", "") + ".bak";
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                #region Step 1: Check if the database already exists and prompt user for confirmation

                try
                {
                    string checkDbExistsQuery = $"SELECT database_id FROM sys.databases WHERE name = '{DatabaseToRestoreName}'";
                    bool databaseExists = false;

                    using (SqlCommand cmd = new SqlCommand(checkDbExistsQuery, connection))
                    {
                        databaseExists = cmd.ExecuteScalar() != null;
                    }

                    if (databaseExists)
                    {
                        var result = System.Windows.MessageBox.Show($"Η βάση '{DatabaseToRestoreName}' υπάρχει ήδη. Να γίνει διαγραφή και φόρτωση εκ νέου;", "Επιβεβαίωση", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                        {
                            // Abort the procedure
                            return;
                        }

                        string dropDbQuery = $"DROP DATABASE {DatabaseToRestoreName}";
                        using (SqlCommand cmd = new SqlCommand(dropDbQuery, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }

                #endregion

                #region Step 2: Get Logical file names from backup
                string logicalNamesQuery = $"RESTORE FILELISTONLY FROM DISK = '{databaseName}'";
                string logicalDataName = "";
                string logicalLogName = "";

                try
                {


                    using (SqlCommand cmd = new SqlCommand(logicalNamesQuery, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["Type"].ToString() == "D")
                                {
                                    logicalDataName = reader["LogicalName"].ToString();
                                }
                                else if (reader["Type"].ToString() == "L")
                                {
                                    logicalLogName = reader["LogicalName"].ToString();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error retrieving logical file names: {ex.Message}");
                }
                #endregion

                #region Step 3: Get the SQL Server data directory

                string dataDirectoryQuery = "SELECT SERVERPROPERTY('InstanceDefaultDataPath') AS DataPath";
                string dataDirectory = "";

                using (SqlCommand cmd = new SqlCommand(dataDirectoryQuery, connection))
                {
                    dataDirectory = cmd.ExecuteScalar().ToString();
                }
                #endregion

                #region Step 4: Restore the database

                string restoreQuery = $@"
RESTORE DATABASE {DatabaseToRestoreName}
FROM DISK = '{DatabaseToRestore}'
WITH MOVE '{logicalDataName}' TO '{dataDirectory}{logicalDataName + "_" + DatabaseToRestoreName}.mdf',
MOVE '{logicalLogName}' TO '{dataDirectory}{logicalLogName + "_" + DatabaseToRestoreName}.ldf',
REPLACE, RECOVERY;";

                try
                {
                    using (SqlCommand restoreCmd = new SqlCommand(restoreQuery, connection))
                    {
                        restoreCmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error restoring database: {ex.Message}");
                    System.Windows.MessageBox.Show($"Error restoring database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Debug.WriteLine($"Database {databaseName} restored successfully.");
                ConnectionStatusTimer.Stop();
                Task.Run(async () =>
                {
                    ConnectionState = $"Restored {DatabaseToRestoreName} successfully.";
                    await Task.Delay(5000);
                    ConnectionStatusTimer.Start();
                });
                #endregion
            }
        }

        private void ConnectSQL()
        {
            ConnectionString = SqlQueries.GetConnectionString(SelectedSqlInstance, LoginSQL, PasswordSQL);

            try
            {
                SqlConnection = new SqlConnection(ConnectionString);
                SqlConnection.Open();

                Logger.Info($"SQL connected to Server: {SelectedSqlInstance}");

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
                var logLine = new StringBuilder();
                logLine.Append($"Connection string: '[{ConnectionString}]'");
                logLine.Append($"[{ex.Message}] ");

                Logger.Error(logLine.ToString());
            }

        }



        #endregion

        public MainWindowViewModel()
        {
            Logger.Info("Application started.");
            LoadTimers();
            Start();
        }


    }
}
