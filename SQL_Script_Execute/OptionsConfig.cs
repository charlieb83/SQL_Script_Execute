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
    public partial class OptionsConfig : Form
    {
        OptionData od = OptionData.Instance;

        public OptionsConfig()
        {
            InitializeComponent();

            //Binds WinForms Objects to Class Properties (Log)
            checkBoxUseDefaultLogPath.DataBindings.Add("Checked", OptionData.Instance, "UseDefaultLogPath", true, DataSourceUpdateMode.OnPropertyChanged);
            textBoxLogPath.DataBindings.Add("Text", OptionData.Instance, "LogPath", true, DataSourceUpdateMode.OnPropertyChanged);
            checkBoxLogCreateExcel.DataBindings.Add("Checked", OptionData.Instance, "CreateExcelLog", true, DataSourceUpdateMode.OnPropertyChanged);
            checkBoxLogErrorsOnly.DataBindings.Add("Checked", OptionData.Instance, "LogErrorsOnly", true, DataSourceUpdateMode.OnPropertyChanged);
            labelLogFileName.DataBindings.Add("Text", OptionData.Instance, "LogFileName", true, DataSourceUpdateMode.OnPropertyChanged);

            //Manual bind to allow CheckBox checkBoxUseDefaultLogPath to control LogPath TextBox and Button Browse Enabled
            Binding bind = new Binding("Enabled", OptionData.Instance, "UseDefaultLogPath");
            Binding bind2 = new Binding("Enabled", OptionData.Instance, "UseDefaultLogPath");
            bind.Format += SwitchBool;
            bind.Parse += SwitchBool;
            bind2.Format += SwitchBool;
            bind2.Parse += SwitchBool;

            textBoxLogPath.DataBindings.Add(bind);
            buttonLogBrowse.DataBindings.Add(bind2);

            //Binds WinForms Objects to Class Properties (Execution and Errors)
            checkBoxIncludeSubfolders.DataBindings.Add("Checked", OptionData.Instance, "IncludeSubFolders", true, DataSourceUpdateMode.OnPropertyChanged);
            checkBoxWarnBeforeExecuting.DataBindings.Add("Checked", OptionData.Instance, "WarnBeforeRunning", true, DataSourceUpdateMode.OnPropertyChanged);
            radioButtonKeepExecutingUntilFinished.DataBindings.Add("Checked", OptionData.Instance, "ExecuteScriptsUntilFinished", true, DataSourceUpdateMode.OnPropertyChanged);
            comboBoxErrorReceivedCount.DataBindings.Add("Text", OptionData.Instance, "StopAfterErrorCount", true, DataSourceUpdateMode.OnPropertyChanged);
            checkBoxConsecutiveErrors.DataBindings.Add("Checked", OptionData.Instance, "ConsecutiveErrors", true, DataSourceUpdateMode.OnPropertyChanged);

            //Binds WinForms Objects to Class Properties (Connections)
            checkBoxWindowsAuthentication.DataBindings.Add("Checked", OptionData.Instance, "WindowsAuthentication", true, DataSourceUpdateMode.OnPropertyChanged);
            textBoxUserName.DataBindings.Add("Text", OptionData.Instance, "UserName", true, DataSourceUpdateMode.OnPropertyChanged);
            textBoxPassword.DataBindings.Add("Text", OptionData.Instance, "Password", true, DataSourceUpdateMode.OnPropertyChanged);
            //Safe Connection String does not display UserName or Password
            textBoxConnectionString.DataBindings.Add("Text", OptionData.Instance, "DisplaySafeConnectionString", true, DataSourceUpdateMode.OnPropertyChanged);
        }


        /*-----------------------------------------------------
        Switch Boolean Value
        -----------------------------------------------------*/
        private void SwitchBool(object sender, ConvertEventArgs e)
        {
            e.Value = !((bool)e.Value);
        }


        /*-----------------------------------------------------
        ButtonCancel Clicked
        -----------------------------------------------------*/
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /*-----------------------------------------------------
        Log Browse Clicked
        -----------------------------------------------------*/
        private void buttonLogBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxLogPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }


        /*-----------------------------------------------------
        Log Browse Clicked
        -----------------------------------------------------*/
        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /*-----------------------------------------------------
        Form Load
        -----------------------------------------------------*/
        private void OptionsConfig_Load(object sender, EventArgs e)
        {
            SetupRadioButtons();
        }


        /*-----------------------------------------------------
        Button Retore Defaults Clicked
        -----------------------------------------------------*/
        private void buttonRestoreDefaults_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("You are about to restore all settings to their default values." + Environment.NewLine + "Do you want to continue?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                OptionData.Instance.ResetDefaultValues();
                SetupRadioButtons();
            }
        }

        /*-----------------------------------------------------
        SetupRadioButtons
        -----------------------------------------------------*/
        private void SetupRadioButtons()
        {
            if (radioButtonKeepExecutingUntilFinished.Checked)
            {
                comboBoxErrorReceivedCount.Enabled = false;
                checkBoxConsecutiveErrors.Enabled = false;
            }
            else
            {
                radioButtonStopAfterError.Checked = true;
                comboBoxErrorReceivedCount.Enabled = true;
                checkBoxConsecutiveErrors.Enabled = true;
            }
        }


        /*-----------------------------------------------------
        Radio Button Keep Executing Until Finished Clicked
        -----------------------------------------------------*/
        private void radioButtonKeepExecutingUntilFinished_Click(object sender, EventArgs e)
        {
            if (radioButtonKeepExecutingUntilFinished.Checked)
            {
                comboBoxErrorReceivedCount.Enabled = false;
                checkBoxConsecutiveErrors.Enabled = false;
            }
        }

        /*-----------------------------------------------------
        Radio Button Stop After Error Clicked
        -----------------------------------------------------*/
        private void radioButtonStopAfterError_Click(object sender, EventArgs e)
        {
            if (radioButtonStopAfterError.Checked)
            {
                comboBoxErrorReceivedCount.Enabled = true;
                checkBoxConsecutiveErrors.Enabled = true;
            }
        }

        private void buttonServerList_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Coming soon!");
        }
    }
}
