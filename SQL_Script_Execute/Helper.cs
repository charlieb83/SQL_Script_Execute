using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data;
using ClosedXML.Excel;
using System.Diagnostics;


//https://www.wolfsys.net/exec-tsql-scripts-containing-go-statements-with-csharp/
//Need to goto Nuget Console (Tools, Nuget, Nuget Console)
//Install-Package Microsoft.SqlServer.Scripting

//Need to add this to the App.config <startup> tag
//useLegacyV2RuntimeActivationPolicy="true"
//Will look like this <startup useLegacyV2RuntimeActivationPolicy="true"> 

namespace SQL_Script_Execute
{
    public class Helper
    {
        BackgroundWorker backgroundWorker1;
        DataTable excelDataTable = new DataTable();

        enum UpdateResult { Fail = 0, Success = 1 };

        OptionData od = OptionData.Instance;

        //Constructor
        public Helper()
        {
            //Setup DataTable for Excel
            excelDataTable.Clear();
            excelDataTable.Columns.Add("Server");
            excelDataTable.Columns.Add("LinkToFile");
            excelDataTable.Columns.Add("File");
            excelDataTable.Columns.Add("SuccessOrError");
            excelDataTable.Columns.Add("Date");
            excelDataTable.Columns.Add("Message");

            // Create background worker thread
            //backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1 = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
        }


        /*-----------------------------------------------------
        Validates values of Option class
        -----------------------------------------------------*/
        public string ValidateValues(bool runtime)
        {
            string s = "";
            //Missing Server
            if (string.IsNullOrWhiteSpace(od.SelectedServer))
            {
                s += "Missing Server Name" + System.Environment.NewLine;
            }

            //Missing UserName or Password
            if (od.WindowsAuthentication == false)
            {
                if (string.IsNullOrWhiteSpace(od.UserName) || string.IsNullOrWhiteSpace(od.Password))
                {
                    s += "UserName and Password are required when NOT using Windows Authentication" + System.Environment.NewLine;
                }
            }

            //Only test at runtime
            if (runtime == true)
            {
                if (od.ProcessErrorFiles == true)
                {
                    //Invalid Error file.
                    IEnumerable<string> validItems = Enumerable.Empty<string>();
                    string t = "";
                    try
                    {
                        var tempList = File.ReadAllLines(od.ScriptsToExecutePath);
                        t = tempList.First();

                        if (t.Substring(0, 20) != "HEADER----StartTime:")
                        {
                            s += "Invalid Error File - Header is Invalid" + System.Environment.NewLine;
                        }
                    }
                    catch //(Exception ex)
                    {
                        s += "Invalid Error File" + System.Environment.NewLine;
                    }
                }

                //Missing Script Path
                if (string.IsNullOrWhiteSpace(od.ScriptsToExecutePath))
                {
                    s += "Missing Scripts To Execute Path" + System.Environment.NewLine;
                }

                //Missing Log Path
                if (string.IsNullOrWhiteSpace(od.LogPath))
                {
                    s += "Missing Log Path" + System.Environment.NewLine;
                }
            }

            return s;
        }


        /*-----------------------------------------------------
        Test If Conncection to SQL Server Is Valid
        -----------------------------------------------------*/
        public string TestServerConnection(string cs)
        {
            string validateInput = ValidateValues(false);
            if (!string.IsNullOrWhiteSpace(validateInput))
            {
                return validateInput;
            }
            else
            {
                using (SqlConnection connection = new SqlConnection(cs))
                {
                    try
                    {
                        connection.Open();
                        return "";              //Successful
                    }
                    catch (SqlException ex)
                    {
                        return ex.Message;      //Failed
                    }
                }
            }

        }

        /*-----------------------------------------------------
        Reset Run Status
        -----------------------------------------------------*/
        private void ResetRuntimeStats()
        {
            //Reset Stats
            od.FileSuccessfullUpdateCount = 0;
            od.FileErrorCount = 0;
            od.ProgressBarValue = 0;
            od.CurrentRecordCount = 0;
        }


        /*-----------------------------------------------------
        Run Scripts
        -----------------------------------------------------*/
        public void ExecuteProcess()
        {
            string errorMessage = "";
            ResetRuntimeStats();
            od.RealTimeStatus = "";     //TODO:Use function
            excelDataTable.Clear();

            //Don't run if not valid
            this.AppendTextBoxStatusWithTimeStamp("Validating Options.....");
            errorMessage = this.ValidateValues(true);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                this.AppendTextBoxStatus("Failed\r\n" + errorMessage);
                return;
            }
            this.AppendTextBoxStatus("Passed\r\n");

            //Check Connection String
            this.AppendTextBoxStatusWithTimeStamp("Checking Connection String.....");
            if (string.IsNullOrWhiteSpace(od.ConnectionString))
            {
                this.AppendTextBoxStatus("Failed\r\n");
                return;
            }
            this.AppendTextBoxStatus("Passed\r\n");

            //Check server connection
            this.AppendTextBoxStatusWithTimeStamp("Checking Server Connection.....");
            if (!string.IsNullOrWhiteSpace(this.TestServerConnection(od.ConnectionString)))
            {
                this.AppendTextBoxStatus("Failed\r\n");
                return;
            }
            this.AppendTextBoxStatus("Passed\r\n");

            //Check to make sure scripts exist in selected file location
            this.AppendTextBoxStatusWithTimeStamp("Checking Path For SQL Script File Count.....");
            if (od.FileTotalCount <= 0)
            {
                this.AppendTextBoxStatus("Failed\r\n" + "There are 0 sql scripts found\r\n");
                return;
            }
            this.AppendTextBoxStatus("Passed\r\n");

            //Check background worker
            if (backgroundWorker1.IsBusy != true)
            {
                od.BuildLogFileName();  //Builds filename based on Root Scripts to Execute Path and timestamp
                this.AppendTextBoxStatusWithTimeStamp("Starting Worker Thread.....\r\n");
                backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                this.AppendTextBoxStatusWithTimeStamp("ERROR: Could Not Start Worker Thread.....\r\n");
                return;
            }
            //All good...
            od.ErrorFileNameList.Clear();
            od.EnableWhenRunning = true;
            od.DisableWhenRunning = false;
        }


        /*-----------------------------------------------------
        BackgroundWorker DoWork - Main Thread
        -----------------------------------------------------*/
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            od.WorkerIsBusy = true;
            od.ProcessStartTime = DateTime.Now;

            string fileFilter = "*.sql";
            string sqlScriptFile;
            string scriptsPath = "";
            string serverConnection = "";
            int consecutiveErrorCount = 0;
            string exceptionMessage = "";
            bool updateSuccessful = false;
            string fileName = "";

            //LogFile - Append timestamp to FileName
            od.LogFileName += "_" + od.ProcessStartTime.ToString("yyyyMMddHHmmss");

            //Search Option
            SearchOption option = od.IncludeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            //Set vals from data class            
            serverConnection = od.SelectedServer;
            scriptsPath = od.ScriptsToExecutePath;
            string sqlConnectionString = od.ConnectionString;

            //Log Header
            WriteToTextLog(GetLogHeader(), false);

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                Server server = new Server(new ServerConnection(connection));

                //Determine if processing error file or enumerating folders for .sql files
                IEnumerable<string> validItems = Enumerable.Empty<string>();
                if (od.ProcessErrorFiles == true)
                {
                    //When processing error file get list of files from within the file
                    var tempList = File.ReadAllLines(od.ScriptsToExecutePath);
                    validItems = new List<string>(tempList).Skip(1); //Skip Header Record
                }
                else
                {
                    //Enumerate directory
                    validItems = Directory.EnumerateFiles(scriptsPath, fileFilter, option).OrderBy(f => f);
                }

                //Loop through folders and files order by name
                //foreach (string file in Directory.EnumerateFiles(scriptsPath, fileFilter, option).OrderBy(f => f))
                foreach (string file in validItems)
                {
                    od.CurrentRecordCount += 1;
                    sqlScriptFile = File.ReadAllText(file);

                    fileName = Path.GetFileName(file);
                    AppendTextBoxStatusWithTimeStamp(fileName); //Writes Only FileName
                    //AppendTextBoxStatusWithTimeStamp(file); //Writes whole path

                    exceptionMessage = "";
                    updateSuccessful = false;

                    try
                    {
                        server.ConnectionContext.ExecuteNonQuery(sqlScriptFile);    //Execute Script

                        AppendTextBoxStatus("  -- Complete" + "\r\n");
                        od.FileSuccessfullUpdateCount += 1;
                        consecutiveErrorCount = 0;  //Reset Consecutive Error count
                        updateSuccessful = true;
                    }

                    catch (Exception ex)
                    {
                        exceptionMessage = ex.InnerException.Message.ToString();
                        exceptionMessage = exceptionMessage.Replace(System.Environment.NewLine, "-");   //Remove line feeds from error messages - ???
                        AppendTextBoxStatus("  -- Error: " + exceptionMessage + "\r\n");

                        od.FileErrorCount += 1;
                        consecutiveErrorCount++;
                        updateSuccessful = false;
                    }

                    //Compile values into an Array (currentRecordCount, ScriptsUpdated, ErrorCount)
                    //string[] arr1 = new string[] {od.CurrentRecordCount.ToString(), scriptsUpdated.ToString(), errorCount.ToString() };                        
                    backgroundWorker1.ReportProgress(0);

                    //Log
                    LogWrapper(file, updateSuccessful, exceptionMessage);

                    //Stop Process if Max Errors Hit
                    if (StopProcessBecauseOfErrorCount(consecutiveErrorCount) == true)
                    {
                        AppendTextBoxStatus("Max Consecutive Errors of " + od.StopAfterErrorCount.ToString() + " has been reached.....Canceling...." + "\r\n");
                        e.Cancel = true;
                        break;
                    }

                    //User Canceled
                    if (backgroundWorker1.CancellationPending)
                    {
                        // Set the e.Cancel flag so the WorkerCompleted event knows the process was Canceled
                        e.Cancel = true;
                        AppendTextBoxStatusWithTimeStamp("User Canceled...");
                        return;
                    }
                }
            }
        }


        /*-----------------------------------------------------
        Log Wrapper
        -----------------------------------------------------*/
        private void LogWrapper(string file, bool updateSuccessful, string exceptionMessage)
        {
            string result = "";
            string logMessage = "";

            //General Log message
            if (updateSuccessful == true)
            {
                //Successful
                result = "S";
                logMessage = file + "  -- Successful" + System.Environment.NewLine;
            }
            else
            {
                //Error
                result = "E";
                logMessage = file + "-- Error: " + exceptionMessage + System.Environment.NewLine;
            }

            //----Log File----
            //Only Log Errors if Log Errors Only = true
            if (updateSuccessful == false || od.LogErrorsOnly == false)
            {
                WriteToTextLog(logMessage, true);
            }

            //Add FilePath to ErrorList
            if (updateSuccessful == false)
            {
                od.ErrorFileNameList.Add(file);
            }

            //----Excel File----
            //Add to Datatable if Create Excel Log is True
            if (od.CreateExcelLog == true)
            {
                if (updateSuccessful == true)
                {
                    exceptionMessage = "Success";
                }
                //Only Log Errors if Log Errors Only = true
                if (updateSuccessful == false || od.LogErrorsOnly == false)
                {
                    PopulateDataTable(od.SelectedServer, file, result, DateTime.Now.ToString("yyyyMMddHHmmss"), exceptionMessage);
                }
            }
        }


        /*-----------------------------------------------------
        This determines if the process should keep running based on Error counts
        -----------------------------------------------------*/
        private bool StopProcessBecauseOfErrorCount(int consecutiveErrorCount)
        {
            bool retVal = false;

            //If Stop after xx Errors is set, check here and cancel 
            if (od.ExecuteScriptsUntilFinished == false)
            {
                if (od.ConsecutiveErrors == true)
                {
                    if (consecutiveErrorCount >= od.StopAfterErrorCount)
                    {
                        retVal = true;
                    }
                }
                else if (od.FileErrorCount >= od.StopAfterErrorCount)
                {
                    retVal = true;
                }
            }

            return retVal;
        }

        /*-----------------------------------------------------
        BackgroundWorker Progress Changed
        -----------------------------------------------------*/
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //string[] runStats = (string[])e.UserState;
            //Console.WriteLine(runStats[0].ToString());  //currentRecordCount
            //Console.WriteLine(runStats[1].ToString());  //scriptsUpdated
            //Console.WriteLine(runStats[2].ToString());  //errorCount
            //int progressStep = Int32.Parse(runStats[0]);

            //double progressPercent = (double.Parse(runStats[0]) / Convert.ToDouble(od.FileTotalCount)) * 100;
            double progressPercent = (Convert.ToDouble(od.CurrentRecordCount) / Convert.ToDouble(od.FileTotalCount)) * 100;
            od.ProgressPercentText = Convert.ToInt32(progressPercent).ToString();

            //od.FileSuccessfullUpdateCount = Int32.Parse(runStats[1]);
            //od.FilesErrorCount = Int32.Parse(runStats[2]);
            od.ProgressBarValue += 1;  //The step value is always going to be 1     //+= progressStep;
            //Artificial Wait (Helps with screen refresh)
            System.Threading.Thread.Sleep(100);
        }

        /*-----------------------------------------------------
        BackgroundWorker Completed
        -----------------------------------------------------*/
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            od.ProgressBarValue = od.FileTotalCount;    //TODO: Create ProgressBar Max value...set to FileCount in class
            od.ProgressPercentText = "100";             //Do this for when worker is canceled
            //MessageBox.Show("DONE!!!");

            TimeSpan span = (DateTime.Now - od.ProcessStartTime);
            AppendTextBoxStatus("Execution Complete--" + "\r\n");
            AppendTextBoxStatus("----------Total Runtime: " + String.Format("{0} minutes, {1} seconds", span.Minutes, span.Seconds) + "\r\n");
            AppendTextBoxStatus("----------Total Scripts In Queue: " + od.FileTotalCount.ToString() + "    Total Scripts Run: " + od.CurrentRecordCount.ToString() + "    SuccessFully Updated: " + od.FileSuccessfullUpdateCount.ToString() + "    Errors: " + od.FileErrorCount.ToString() + "\r\n");

            //Log
            WriteToTextLog(GetLogFooter(od.FileTotalCount, od.FileSuccessfullUpdateCount, od.FileErrorCount), false);

            //Write To Excel Log
            if (od.CreateExcelLog == true)
            {
                WriteToExcelLog();
            }
            //This is a file of just the error scripts.  Use incase you want to just execute these in a 2nd run
            WriteErrorFile();

            od.WorkerIsBusy = false;
            od.EnableWhenRunning = false;
            od.DisableWhenRunning = true;
        }

        /*-----------------------------------------------------
        Cancel Process
        -----------------------------------------------------*/
        public void CancelProcess()
        {
            if (backgroundWorker1.WorkerSupportsCancellation == true)
            {
                backgroundWorker1.CancelAsync();
            }
        }


        /*-----------------------------------------------------
        Populate DataTable  - TODO: Do this better - May not want to populate entire datatable, might get too big
        -----------------------------------------------------*/
        private void PopulateDataTable(string server, string file, string status, string date, string message)
        {
            DataRow dr = excelDataTable.NewRow();
            dr[0] = server;     //Server
            dr[1] = file;       //File
            dr[2] = file;       //HyperLink To File Path    --"=HYPERLINK(\"" + file + "\", \"" + linkName + "\")";
            dr[3] = status;     //Status
            dr[4] = date;       //Date
            dr[5] = message;    //Message
            excelDataTable.Rows.Add(dr);//Add to end of the datatable
        }

        /*-----------------------------------------------------
        Get Log Header
        -----------------------------------------------------*/
        private string GetLogHeader()
        {
            string logHeader = "";
            logHeader += "--------------------Execution Starting--------------------" + System.Environment.NewLine;
            logHeader += "--Start Time: " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + System.Environment.NewLine;
            logHeader += "--Script To Execute Path: " + od.ScriptsToExecutePath + System.Environment.NewLine;
            logHeader += "--Selected Server: " + od.SelectedServer + System.Environment.NewLine;
            logHeader += "--Log Errors Only: " + od.LogErrorsOnly.ToString() + System.Environment.NewLine;
            logHeader += "--Create Excel Log: " + od.CreateExcelLog.ToString() + System.Environment.NewLine;
            logHeader += "--Include SubFolders: " + od.IncludeSubFolders.ToString() + System.Environment.NewLine;
            logHeader += "----------------------------------------------------------";
            return logHeader;
        }

        /*-----------------------------------------------------
        Get Log Footer
        -----------------------------------------------------*/
        private string GetLogFooter(int totalFiles, int successfulCount, int errorCount)
        {
            string logFooter = "";
            logFooter += "--------------------Execution Complete--------------------" + System.Environment.NewLine;
            logFooter += "--End Time: " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + System.Environment.NewLine;
            logFooter += "--Total Files Evaluated: " + totalFiles.ToString() + System.Environment.NewLine;
            logFooter += "--Total Successful: " + successfulCount.ToString() + System.Environment.NewLine;
            logFooter += "--Total Errors: " + errorCount.ToString() + System.Environment.NewLine;
            logFooter += "----------------------------------------------------------";
            return logFooter;
        }


        /*-----------------------------------------------------
        Apend TextBox Status
        -----------------------------------------------------*/
        public void AppendTextBoxStatus(string text)
        {
            od.RealTimeStatus += text;
        }

        public void AppendTextBoxStatusWithTimeStamp(string text)
        {
            od.RealTimeStatus += DateTime.Now.ToString("yyyyMMddHHmmss") + "--" + text;
        }

        /*-----------------------------------------------------
        Get Log Footer
        -----------------------------------------------------*/
        public void OpenLogLocation()
        {
            if (!string.IsNullOrWhiteSpace(od.LogPath))
            {
                Process.Start(od.LogPath);
            }
        }

        /*-----------------------------------------------------
        Error File Header
        -----------------------------------------------------*/
        private void WriteErrorFile()
        {
            //Only write file if there are Errors
            if (od.FileErrorCount > 0)
            {
                //using (TextWriter tw = new StreamWriter(od.ErrorFileName))
                using (StreamWriter w = new StreamWriter(od.ErrorFileName))
                {
                    //Write Header to error file
                    w.WriteLine("HEADER----StartTime:" + od.ProcessStartTime.ToString("yyyyMMddHHmmss") + ", TotalFilesRun: " + od.FileTotalCount.ToString() + ", ErrorCount: " + od.FileErrorCount.ToString());

                    foreach (String s in od.ErrorFileNameList)
                        w.WriteLine(s);
                }
            }
        }

        /*-----------------------------------------------------
        Write to Log
        -----------------------------------------------------*/
        private void WriteToTextLog(string logMessage, bool includeDate)
        {
            //using (StreamWriter w = File.CreateText(od.LogPath + @"\" + od.LogFileName + @".log"))
            using (StreamWriter w = new StreamWriter(od.LogPath + @"\" + od.LogFileName + @".log", true))
            {
                if (includeDate == true)
                {
                    w.WriteLine("{0} {1} {2}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), logMessage);
                }
                else
                {
                    w.WriteLine(logMessage);
                }
            }
        }

        /*-----------------------------------------------------
        Write To Excel Log
        -----------------------------------------------------*/        
        private void WriteToExcelLog()
        {
            // use ClosedXML to write to excel
            using (var book = new XLWorkbook(XLEventTracking.Disabled))
            {
                var ws = book.Worksheets.Add(excelDataTable, "ExcelLog");
                book.SaveAs(od.ExcelFileName);
                //Total Rows in SS
                //var totalRows = ws.RowsUsed().Count();

                int currentRow = 0;
                string hyperLinkValue;

                //Loop through spreadsheet to set Hyperlinks
                foreach (var row in book.Worksheet("ExcelLog").Rows())
                {
                    currentRow++;

                    //Skip Header Row
                    if (currentRow > 1)
                    {
                        //MessageBox.Show(row.Cell(2).GetString());
                        hyperLinkValue = row.Cell(2).GetString();

                        //Check if cell is blank
                        if (!string.IsNullOrWhiteSpace(hyperLinkValue))
                        {
                            row.Cell(2).Value = "Link To File";
                            row.Cell(2).Hyperlink = new XLHyperlink(hyperLinkValue);
                        }
                    }
                }
                book.Save();
            }
        }


    }


}
