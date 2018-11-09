using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;  //CallerMemberName



namespace SQL_Script_Execute
{

    public sealed class OptionData : INotifyPropertyChanged
    {

        private static OptionData instance;

        private OptionData()
        {
            LoadSettings();
        }

        static OptionData()
        {
            instance = new OptionData();
        }

        public static OptionData Instance
        {
            get { return instance; }
        }

        //Destructor
        ~OptionData()
        {
            //Save last settings
            SaveAppSettings();
        }

        //NotifyPropertyChanged (This was needed mainly for the progressBar...the binding wouldn't update without this)
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            //Console.WriteLine("Property That Changed:" + propertyName.ToString());

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        //Class Properties        
        private string _userName = "";
        private string _password = "";
        private string _selectedServer = Properties.Settings.Default.SelectedServer;
        private string _connectionString = "";
        private bool _windowsAuthentication = Properties.Settings.Default.WindowsAuthentication;
        private bool _createExcelLog = Properties.Settings.Default.CreateExcelLog;
        private bool _logErrorsOnly = Properties.Settings.Default.LogErrorsOnly;
        private bool _includeSubFolders = Properties.Settings.Default.IncludeSubFolders;
        private bool _warnBeforeRunning = Properties.Settings.Default.WarnBeforeRunning;
        private bool _executeScriptsUntilFinished = Properties.Settings.Default.ExecuteScriptsUntilFinished;
        private bool _stopAfterError = false;
        private int _stopAfterErrorCount = Properties.Settings.Default.StopAfterErrorCount;
        private bool _consecutiveErrors = Properties.Settings.Default.ConsecutiveErrors;
        private bool _workerIsBusy = false;
        //private bool _runButtonEnabled = true;
        // private bool _cancelButtonEnabled = false;

        private bool _disableWhenRunning = true;    //Run button, etc.
        private bool _enableWhenRunning = false;   //Cancel button, etc.

        private bool _useDefaultLogPath = Properties.Settings.Default.UseDefaultLogPath;
        private string _logPath = Properties.Settings.Default.LogPath;
        private string _scriptsToExecutePath = Properties.Settings.Default.ScriptsToExecutePath;
        private string _progressPercentText = "0";
        private int _progressBarValue = 0;
        private string _realTimeStatus { get; set; } = "";
        private bool _processErrorFiles = Properties.Settings.Default.ProcessErrorFiles;

        //public List<string> Servers { get; set; }        
        public int FileTotalCount { get; set; } = 0;
        public int FileSuccessfullUpdateCount { get; set; } = 0;
        public int FileErrorCount { get; set; } = 0;
        public List<string> ErrorFileNameList { get; set; }
        public string LogFileName { get; set; } = "";
        public int CurrentRecordCount { get; set; } = 0;
        public DateTime ProcessStartTime { get; set; } = DateTime.Now;

        //Excel FileName (Set from LogFileName)
        public string ExcelFileName
        {
            get { return LogPath + @"\" + LogFileName + @".xlsx"; }
        }

        //Error FileName (Set from LogFileName)
        public string ErrorFileName
        {
            get { return LogPath + @"\" + "Error_" + LogFileName + @".err"; }
        }

        //Create Excel Log        
        public bool CreateExcelLog
        {
            get { return _createExcelLog; }
            set
            {
                _createExcelLog = value;
                NotifyPropertyChanged();
            }
        }

        //Log Errors Only        
        public bool LogErrorsOnly
        {
            get { return _logErrorsOnly; }
            set
            {
                _logErrorsOnly = value;
                NotifyPropertyChanged();
            }
        }

        //Include SubFolders        
        public bool IncludeSubFolders
        {
            get { return _includeSubFolders; }
            set
            {
                _includeSubFolders = value;
                NotifyPropertyChanged();
                SetScriptCount();
            }
        }

        //Warn Before Running        
        public bool WarnBeforeRunning
        {
            get { return _warnBeforeRunning; }
            set
            {
                _warnBeforeRunning = value;
                NotifyPropertyChanged();
            }
        }

        //Execute Scripts Until Finished        
        public bool ExecuteScriptsUntilFinished
        {
            get { return _executeScriptsUntilFinished; }
            set
            {
                _executeScriptsUntilFinished = value;
                _stopAfterError = !_executeScriptsUntilFinished;
                NotifyPropertyChanged();
            }
        }

        //Stop After Error       
        public bool StopAfterError
        {
            get { return _stopAfterError; }
            set
            {
                _stopAfterError = value;
                NotifyPropertyChanged();
            }
        }
        

        //Stop After Error Count        
        public int StopAfterErrorCount
        {
            get { return _stopAfterErrorCount; }
            set
            {
                _stopAfterErrorCount = value;
                NotifyPropertyChanged();
            }
        }

        //Consecutive Errors        
        public bool ConsecutiveErrors
        {
            get { return _consecutiveErrors; }
            set
            {
                _consecutiveErrors = value;
                NotifyPropertyChanged();
            }
        }

        //Worker Is Busy        
        public bool WorkerIsBusy
        {
            get { return _workerIsBusy; }
            set
            {
                _workerIsBusy = value;
                NotifyPropertyChanged();
            }
        }

        //Run Button Enable        
        public bool DisableWhenRunning
        {
            get { return _disableWhenRunning; }

            set
            {
                _disableWhenRunning = value;
                NotifyPropertyChanged();    //Triggers Property Changed Notifier
            }
        }

        //Cancel Button Enable        
        public bool EnableWhenRunning
        {
            get { return _enableWhenRunning; }

            set
            {
                _enableWhenRunning = value;
                NotifyPropertyChanged();    //Triggers Property Changed Notifier
            }
        }

        //UserName        
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                SetConnectionString();
                NotifyPropertyChanged();
            }
        }

        //Password
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                SetConnectionString();
                NotifyPropertyChanged();
            }
        }

        //Selected Server
        public string SelectedServer
        {
            get { return _selectedServer; }
            set
            {
                _selectedServer = value;
                SetConnectionString();
                NotifyPropertyChanged();
            }
        }

        //MasterConnectionString
        public string MasterConnectionString { get; set; } = Properties.Settings.Default.MasterConnectionString;

        //ConnectionString
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
            }
        }

        //SafeConnectionString
        public string DisplaySafeConnectionString
        {
            get { return GetConnectionStringSafeForDisplay(_connectionString); }
        }

        //WindowsAuthentication        
        public bool WindowsAuthentication
        {
            get { return _windowsAuthentication; }
            set
            {
                _windowsAuthentication = value;
                SetConnectionString();
                NotifyPropertyChanged();
            }
        }

        //UseDefaultLogPath        
        public bool UseDefaultLogPath
        {
            get { return _useDefaultLogPath; }
            set
            {
                _useDefaultLogPath = value;
                if (_useDefaultLogPath == true)
                {
                    _logPath = GetLogPath();
                }
                NotifyPropertyChanged();
            }
        }

        //LogPath        
        public string LogPath
        {
            get { return _logPath; }
            set
            {
                if (_useDefaultLogPath == true)
                {
                    _logPath = GetLogPath();
                }
                else
                {
                    _logPath = value;
                }
            }
        }

        //ScriptsToExecutePath        
        public string ScriptsToExecutePath
        {
            get { return _scriptsToExecutePath; }
            set
            {
                _scriptsToExecutePath = value;

                if (_useDefaultLogPath == true)
                {
                    _logPath = GetLogPath();
                }
                SetScriptCount();
                BuildLogFileName(); //Log FileName is the root folder of the scripts to Execute path
            }
        }

        //ProgressBar Value        
        public int ProgressBarValue
        {
            get { return _progressBarValue; }

            set
            {
                _progressBarValue = value;
                NotifyPropertyChanged();    //Triggers Property Changed Notifier
            }
        }

        //ProgressBar Percent Text        
        public string ProgressPercentText
        {
            get { return _progressPercentText + "%"; }
            //get { return _progressBarValue.ToString() + "%"; }
            //od.FileTotalCount / currentRecordCount;
            set
            {
                _progressPercentText = value;
                NotifyPropertyChanged();    //Triggers Property Changed Notifier
            }
        }

        //RealTimeStatus
        public string RealTimeStatus
        {
            get { return _realTimeStatus; }

            set
            {
                _realTimeStatus = value;
                NotifyPropertyChanged();    //Triggers Property Changed Notifier
            }
        }

        //ProcessErrorFiles        
        public bool ProcessErrorFiles
        {
            get { return _processErrorFiles; }
            set
            {
                _processErrorFiles = value;
                SetScriptCount();
                NotifyPropertyChanged();
            }
        }

        /*-----------------------------------------------------
        Set Total Script Count
        -----------------------------------------------------*/
        private void SetScriptCount()
        {
            string fileFilter = "*.sql";
            if (string.IsNullOrWhiteSpace(ScriptsToExecutePath) || ScriptsToExecutePath == "Invalid")
            {
                FileTotalCount = 0;
                return;
            }

            if (ProcessErrorFiles == false)
            {
                //Process normal Folder for .sql files
                SearchOption option = IncludeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                try
                {
                    FileTotalCount = Directory.GetFiles(ScriptsToExecutePath, fileFilter, option).Length;
                    //Reset other counters
                    FileSuccessfullUpdateCount = 0;
                    FileErrorCount = 0;
                }
                catch //(Exception ex)
                {
                    //TODO: FIX THIS!
                    //System.Windows.Forms.MessageBox.Show(ex.Message);
                    ScriptsToExecutePath = "Invalid";
                    FileTotalCount = 0;
                }
            }
            else
            {
                try
                {
                    //Process an error file
                    IEnumerable<string> validItems = Enumerable.Empty<string>();
                    var scriptFile = File.ReadAllLines(ScriptsToExecutePath);
                    validItems = new List<string>(scriptFile).Skip(1);
                    FileTotalCount = validItems.Count();
                    //Reset other counters
                    FileSuccessfullUpdateCount = 0;
                    FileErrorCount = 0;
                }
                catch //(Exception ex)
                {
                    ScriptsToExecutePath = "Invalid";
                    FileTotalCount = 0;
                }
            }
        }

        /*-----------------------------------------------------
        Returns LogPath based on Processing an Error file or Normal sql files
        -----------------------------------------------------*/
        private string GetLogPath()
        {
            string returnLogPath = "";

            try
            {
                FileAttributes attr = File.GetAttributes(this.ScriptsToExecutePath.Trim());
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //MessageBox.Show("Its a directory");
                    returnLogPath = _scriptsToExecutePath.Trim();
                }
                else
                {
                    //MessageBox.Show("Its a file");
                    returnLogPath = Path.GetDirectoryName(_scriptsToExecutePath.Trim());
                }

            }
            catch
            {
                //MessageBox.Show("Its neither");
                returnLogPath = "";
            }

            return returnLogPath;
        }


        /*-----------------------------------------------------
        Builds Log FileName (Needs to be built at Runtime for Datestamp)
        -----------------------------------------------------*/
        public void BuildLogFileName()
        {
            // Set LogName when Script Path is given
            string dirName = "";

            //Get Log FileName
            //if (string.IsNullOrWhiteSpace(_scriptsToExecutePath))
            //{
            //    dirName = "log";
            //}
            //else
            //{
            //Process normal Folder for .sql files
            if (ProcessErrorFiles == false)
            {
                try
                {
                    dirName = new DirectoryInfo(_scriptsToExecutePath.Trim()).Name;
                }
                catch
                {
                    dirName = "Log";
                }
            }
            //Process an error file
            else
            {
                try
                {
                    dirName = "Error_" + new DirectoryInfo(Path.GetDirectoryName(_scriptsToExecutePath.Trim())).Name;
                }
                catch
                {
                    dirName = "Error_";
                }

            }
            //}
            //Sets Class LogFileName
            LogFileName = dirName.Replace(@"\", "").Replace(":", "");
        }

        /*-----------------------------------------------------
        Save App Settings
        -----------------------------------------------------*/
        private void SaveAppSettings()
        {
            Properties.Settings.Default.SelectedServer = this.SelectedServer.ToString();
            Properties.Settings.Default.WindowsAuthentication = this.WindowsAuthentication;
            Properties.Settings.Default.UseDefaultLogPath = this.UseDefaultLogPath;
            Properties.Settings.Default.CreateExcelLog = this.CreateExcelLog;
            Properties.Settings.Default.LogErrorsOnly = this.LogErrorsOnly;
            Properties.Settings.Default.ScriptsToExecutePath = this.ScriptsToExecutePath;
            Properties.Settings.Default.LogPath = this.LogPath;
            Properties.Settings.Default.IncludeSubFolders = this.IncludeSubFolders;
            Properties.Settings.Default.WarnBeforeRunning = this.WarnBeforeRunning;
            Properties.Settings.Default.ExecuteScriptsUntilFinished = this.ExecuteScriptsUntilFinished;
            Properties.Settings.Default.StopAfterErrorCount = this.StopAfterErrorCount;
            Properties.Settings.Default.ConsecutiveErrors = this.ConsecutiveErrors;
            Properties.Settings.Default.ProcessErrorFiles = this.ProcessErrorFiles;
            Properties.Settings.Default.Save();
        }

        /*-----------------------------------------------------
        Load App Settings
        -----------------------------------------------------*/
        private void LoadSettings()
        {
            this.SelectedServer = Properties.Settings.Default.SelectedServer;
            this.WindowsAuthentication = Properties.Settings.Default.WindowsAuthentication;
            this.UseDefaultLogPath = Properties.Settings.Default.UseDefaultLogPath;
            this.CreateExcelLog = Properties.Settings.Default.CreateExcelLog;
            this.LogErrorsOnly = Properties.Settings.Default.LogErrorsOnly;
            this.ScriptsToExecutePath = Properties.Settings.Default.ScriptsToExecutePath;
            this.LogPath = Properties.Settings.Default.LogPath;
            this.IncludeSubFolders = Properties.Settings.Default.IncludeSubFolders;
            this.WarnBeforeRunning = Properties.Settings.Default.WarnBeforeRunning;
            this.ExecuteScriptsUntilFinished = Properties.Settings.Default.ExecuteScriptsUntilFinished;
            this.StopAfterErrorCount = Properties.Settings.Default.StopAfterErrorCount;
            this.ConsecutiveErrors = Properties.Settings.Default.ConsecutiveErrors;
            this.ProcessErrorFiles = Properties.Settings.Default.ProcessErrorFiles;
            SetScriptCount();
            this.ErrorFileNameList = new List<string>();
            BuildLogFileName();
        }

        /*-----------------------------------------------------
        Reset Default Values
        -----------------------------------------------------*/
        public void ResetDefaultValues()
        {
            Properties.Settings.Default.Reset();
            this.SelectedServer = Properties.Settings.Default.SelectedServer;
            this.WindowsAuthentication = Properties.Settings.Default.WindowsAuthentication;
            this.UseDefaultLogPath = Properties.Settings.Default.UseDefaultLogPath;
            this.CreateExcelLog = Properties.Settings.Default.CreateExcelLog;
            this.LogErrorsOnly = Properties.Settings.Default.LogErrorsOnly;
            this.ScriptsToExecutePath = Properties.Settings.Default.ScriptsToExecutePath;
            this.LogPath = Properties.Settings.Default.LogPath;
            this.IncludeSubFolders = Properties.Settings.Default.IncludeSubFolders;
            this.WarnBeforeRunning = Properties.Settings.Default.WarnBeforeRunning;
            this.ExecuteScriptsUntilFinished = Properties.Settings.Default.ExecuteScriptsUntilFinished;
            this.StopAfterErrorCount = Properties.Settings.Default.StopAfterErrorCount;
            this.ConsecutiveErrors = Properties.Settings.Default.ConsecutiveErrors;
            this.ProcessErrorFiles = false;
            //CreateExcelLog = true;
            //LogErrorsOnly = false;
            //IncludeSubFolders = true;
            //WarnBeforeRunning = true;
            //ExecuteScriptsUntilFinished = true;
            //StopAfterErrorCount = 10;
            //ConsecutiveErrors = false;
            //WindowsAuthentication = true;
            //UserName = "";
            //Password = "";
            //UseDefaultLogPath = true;
            //SelectedServer = "";
            //ScriptsToExecutePath = "";
        }


        /*-----------------------------------------------------
        GetConnectionStringSafeForDisplay - Removes UserName and Password
        -----------------------------------------------------*/
        private string GetConnectionStringSafeForDisplay(string cn)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(cn);
            builder.Remove("User ID");
            builder.Remove("Password");
            return builder.ConnectionString;
        }

        /*-----------------------------------------------------
        GetConnectionString
        -----------------------------------------------------*/
        public string SetConnectionString(string intialCatalog = "master")
        {
            string dataSource = this.SelectedServer;
            bool windowsAuth = WindowsAuthentication;
            string masterConnectionString = MasterConnectionString;
            string retConnectionString;

            // Create a new SqlConnectionStringBuilder based on master ConnectionString
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(masterConnectionString);

            //Windows Authentication
            if (windowsAuth == true)
            {
                // Remove UnNeeded values
                builder.Remove("User ID");
                builder.Remove("Password");

                // Supply the additional values.
                builder.DataSource = dataSource;
                builder.InitialCatalog = intialCatalog;
                builder.IntegratedSecurity = true;

                retConnectionString = builder.ConnectionString;
            }
            else
            {
                //Without Windows Authentication, UserName and Password are required
                string userName = UserName;
                string password = Password;
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                {
                    //retConnectionString = "UserName and Password are required when not using Windows Auth";
                    retConnectionString = "";
                }
                else
                {
                    // Supply the additional values.
                    builder.IntegratedSecurity = false;
                    builder.DataSource = dataSource;
                    builder.InitialCatalog = intialCatalog;
                    builder.UserID = userName;
                    builder.Password = password;
                    //Console.WriteLine("Modified: {0}", builder.ConnectionString);
                    retConnectionString = builder.ConnectionString;
                }
            }
            _connectionString = retConnectionString;

            return retConnectionString;
        }

    }
}
