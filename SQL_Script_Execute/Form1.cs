using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQL_Script_Execute
{
    public partial class Form1 : Form
    {
        Helper hp = new Helper();

        public Form1()
        {
            InitializeComponent();
            
            comboBoxServer.DataBindings.Add("Text", OptionData.Instance, "SelectedServer", true, DataSourceUpdateMode.OnPropertyChanged);
            textBoxScriptPath.DataBindings.Add("Text", OptionData.Instance, "ScriptsToExecutePath", true, DataSourceUpdateMode.OnPropertyChanged);
            labelUseWindowsAuthentication.DataBindings.Add("Text", OptionData.Instance, "WindowsAuthentication", true, DataSourceUpdateMode.OnPropertyChanged);
            labelIncludeSubfolders.DataBindings.Add("Text", OptionData.Instance, "IncludeSubFolders", true, DataSourceUpdateMode.OnPropertyChanged);
            labelExecuteUntilFinished.DataBindings.Add("Text", OptionData.Instance, "ExecuteScriptsUntilFinished", true, DataSourceUpdateMode.OnPropertyChanged);
            labelCreateExcelLog.DataBindings.Add("Text", OptionData.Instance, "CreateExcelLog", true, DataSourceUpdateMode.OnPropertyChanged);
            labelLogErrorsOnly.DataBindings.Add("Text", OptionData.Instance, "LogErrorsOnly", true, DataSourceUpdateMode.OnPropertyChanged);
            labelStopAfterErrorCount.DataBindings.Add("Text", OptionData.Instance, "StopAfterErrorCount", true, DataSourceUpdateMode.OnPropertyChanged);
            labelStopAfterErrorCount.DataBindings.Add("Visible", OptionData.Instance, "StopAfterError", true, DataSourceUpdateMode.OnPropertyChanged);
            labelScriptsInQueueCount.DataBindings.Add("Text", OptionData.Instance, "FileTotalCount", true, DataSourceUpdateMode.OnPropertyChanged);
            labelScriptsUpdatedCount.DataBindings.Add("Text", OptionData.Instance, "FileSuccessfullUpdateCount", true, DataSourceUpdateMode.OnPropertyChanged);
            labelScriptErrorCount.DataBindings.Add("Text", OptionData.Instance, "FileErrorCount", true, DataSourceUpdateMode.OnPropertyChanged);
            textBoxStatus.DataBindings.Add("Text", OptionData.Instance, "RealTimeStatus", true, DataSourceUpdateMode.OnPropertyChanged);
            buttonRun.DataBindings.Add("Enabled", OptionData.Instance, "DisableWhenRunning", true, DataSourceUpdateMode.OnPropertyChanged);
            buttonCancel.DataBindings.Add("Enabled", OptionData.Instance, "EnableWhenRunning", true, DataSourceUpdateMode.OnPropertyChanged);
            buttonBrowse.DataBindings.Add("Enabled", OptionData.Instance, "DisableWhenRunning", true, DataSourceUpdateMode.OnPropertyChanged);
            buttonTest.DataBindings.Add("Enabled", OptionData.Instance, "DisableWhenRunning", true, DataSourceUpdateMode.OnPropertyChanged);
            checkBoxProcessErrorFile.DataBindings.Add("Checked", OptionData.Instance, "ProcessErrorFiles", true, DataSourceUpdateMode.OnPropertyChanged);
            pictureBoxSpinner.DataBindings.Add("Visible", OptionData.Instance, "WorkerIsBusy", true, DataSourceUpdateMode.OnPropertyChanged);
            //ProgressBar
            progressBar1.DataBindings.Add("Maximum", OptionData.Instance, "FileTotalCount", true, DataSourceUpdateMode.OnPropertyChanged);
            progressBar1.DataBindings.Add("Value", OptionData.Instance, "ProgressBarValue", true, DataSourceUpdateMode.OnPropertyChanged);
            //Progress Label
            labelProgressPercent.DataBindings.Add("Enabled", OptionData.Instance, "EnableWhenRunning", true, DataSourceUpdateMode.OnPropertyChanged);
            labelProgressPercent.DataBindings.Add("Text", OptionData.Instance, "ProgressPercentText", true, DataSourceUpdateMode.OnPropertyChanged);


            //TESTING
            //OptionData.Instance.UserName = "";
            //OptionData.Instance.Password = "";
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsConfig o = new OptionsConfig();
            o.ShowDialog();
            //SetFormOptionValues();
        }


        /*-----------------------------------------------------
        Run Clicked
        -----------------------------------------------------*/
        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (OptionData.Instance.WarnBeforeRunning == true)
            {
                //TODO: Move to Helper or OptionData
                DialogResult result = MessageBox.Show("You are about to execute SQL Scripts." + Environment.NewLine + "You can Cancel while running but it will NOT rollback scripts that already executed." + Environment.NewLine + "Are you sure you want to continue?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                {
                    textBoxStatus.AppendText("User Chickened Out....." + "\r\n");
                    return;
                }
            }
            hp.ExecuteProcess();
        }


        /*-----------------------------------------------------
        Test Server Connection Clicked
        -----------------------------------------------------*/
        private void buttonTest_Click(object sender, EventArgs e)
        {
            string retValue = "";

            retValue = hp.TestServerConnection(OptionData.Instance.ConnectionString);
            if (retValue != "")
            {
                MessageBox.Show(retValue, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Successful!", "Message", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
        }

        /*-----------------------------------------------------
        Browse Clicked
        -----------------------------------------------------*/
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            //TODO: Move to Helper or OptionData
            //If checkBoxProcessErrorFile is true, Open file dialog to select Error file
            if (checkBoxProcessErrorFile.Checked == true)
            {
                openFileDialog1.Filter = "Error Files|*.err";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    textBoxScriptPath.Text = openFileDialog1.FileName;
                }
            }
            else
            {
                //Open Folder Dialog to the path that is currently in the textbox
                this.folderBrowserDialog1.SelectedPath = textBoxScriptPath.Text;
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    textBoxScriptPath.Text = folderBrowserDialog1.SelectedPath;
                }
            }
        }

        /*-----------------------------------------------------
        Cancel Clicked
        -----------------------------------------------------*/
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            hp.CancelProcess();
        }


        /*-----------------------------------------------------
        Open Log Path Label Clicked
        -----------------------------------------------------*/
        private void labelClickOpenLogPath_Click(object sender, EventArgs e)
        {
            hp.OpenLogLocation();
        }


        /*-----------------------------------------------------
        Error Checkbox Clicked
        -----------------------------------------------------*/
        private void checkBoxProcessErrorFile_Click(object sender, EventArgs e)
        {
            if (checkBoxProcessErrorFile.Checked == true)
            {
                buttonBrowse_Click(sender, e);
            }
        }


        /*-----------------------------------------------------
        TextBoxStatus Text Changed
        -----------------------------------------------------*/
        private void textBoxStatus_TextChanged(object sender, EventArgs e)
        {
            textBoxStatus.SelectionStart = textBoxStatus.TextLength;
            textBoxStatus.ScrollToCaret();
        }
    }
}
