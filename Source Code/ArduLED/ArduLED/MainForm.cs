﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Threading;
using Un4seen.Bass;
using Un4seen.BassWasapi;
using System.Drawing.Imaging;
using System.Reflection;

namespace ArduLEDNameSpace
{
    public partial class MainForm : Form
    {
        #region Variabels

        const int ButtonWidth = 20;
        const int ButtonHeight = 15;
        const int BoxHeight = 45;
        const int Margins = 5;
        const int ScroolBarWidth = 25;
        const int Sizex = 1411;
        const int Sizey = 775;

        List<string> IntructionsList = new List<string>();
        bool ContinueInstructionsLoop = false;
        bool StopInstructionsLoop = false;
        bool InstructionsRunning = false;
        bool ShowLoadingScreen = true;
        bool UnitReady = false;
        bool ReadyToRecive = false;

        WASAPIPROC BassProcess;

        Task VisualizerThread;
        bool RunVisualizerThread = true;

        List<Control> ControlList = new List<Control>();
        Loading LoadingForm;
        Point DragStart = new Point(0,0);
        List<Block> BlockList = new List<Block>();

        Bitmap ImageWindowLeft = new Bitmap(50, 50, PixelFormat.Format32bppRgb);
        Bitmap ImageWindowTop = new Bitmap(50, 50, PixelFormat.Format32bppRgb);
        Bitmap ImageWindowRight = new Bitmap(50, 50, PixelFormat.Format32bppRgb);
        Bitmap ImageWindowBottom = new Bitmap(50, 50, PixelFormat.Format32bppRgb);
        Graphics GFXScreenshotLeft;
        Graphics GFXScreenshotTop;
        Graphics GFXScreenshotRight;
        Graphics GFXScreenshotBottom;
        List<List<List<int>>> AmbilightColorStore = new List<List<List<int>>>();
        bool RunAmbilight = false;
        DateTime AmbilightFPSCounter;
        int AmbilightFPSCounterFramesRendered;
        Task AmbilightTask;

        string SerialOutLeft;
        string SerialOutTop;
        string SerialOutRight;
        string SerialOutBottom;
        bool SerialOutLeftReady;
        bool SerialOutTopReady;
        bool SerialOutRightReady;
        bool SerialOutBottomReady;

        #endregion

        #region Loading Section

        public MainForm()
        {
            InitializeComponent();
        }

        public async void Form1_Load(object sender, EventArgs e)
        {
            LoadingScreen();

            while (LoadingForm == null) { }
            while (!LoadingForm.Visible) { }

            SerialPort1.Encoding = System.Text.Encoding.ASCII;
            SerialPort1.NewLine = "\n";

            SetLoadingLabelTo("BASS.NET");

            InitializeBass();

            SetLoadingLabelTo("Instructions folder");

            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\Instructions"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Instructions");
            }

            SetLoadingLabelTo("Setup folder");

            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\Setups"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Setups");
            }

            SetLoadingLabelTo("Visualizer settings folder");

            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\VisualizerSettings"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\VisualizerSettings");
            }

            SetLoadingLabelTo("Ambilight settings folder");

            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\AmbilightSettings"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\AmbilightSettings");
            }

            SetLoadingLabelTo("Language Packs");

            if (Directory.GetFiles(Directory.GetCurrentDirectory() + "\\Languages").Length > 0)
            {
                foreach (string f in Directory.GetFiles(Directory.GetCurrentDirectory() + "\\Languages"))
                {
                    LanguageComboBox.Items.Add(f.Substring(f.Length - 6, 2));
                }
            }
            else
                MessageBox.Show("No language packs found! Using default preset");

            SetLoadingLabelTo("Visuals");

            MaximumSize = new Size(Sizex, Sizey);
            MinimumSize = new Size(Sizex, Sizey);
            Location = new Point(Screen.PrimaryScreen.Bounds.Width - Sizex, 0);

            SetLoadingLabelTo("Save/Load Mechanisms");

            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory();

            SetLoadingLabelTo("Charts");

            BeatZoneChart.ChartAreas[0].AxisY.Maximum = 255;
            SpectrumChart.ChartAreas[0].AxisY.Maximum = 255;
            WaveChart.ChartAreas[0].AxisY.Maximum = 255;
            BeatZoneChart.ChartAreas[0].AxisY.Minimum = 0;
            SpectrumChart.ChartAreas[0].AxisY.Minimum = 0;
            WaveChart.ChartAreas[0].AxisY.Minimum = 0;

            BeatZoneChart.ChartAreas[0].AxisX.Minimum = 0;
            SpectrumChart.ChartAreas[0].AxisX.Minimum = 0;
            WaveChart.ChartAreas[0].AxisX.Minimum = 0;
            BeatZoneChart.ChartAreas[0].AxisX.Maximum = BeatZoneToTrackBar.Maximum;
            SpectrumChart.ChartAreas[0].AxisX.Maximum = BeatZoneToTrackBar.Maximum;

            SetLoadingLabelTo("Presetting Combobox indexes");

            ModeSelectrionComboBox.Items.Add(" ");
            ModeSelectrionComboBox.Items.Add(" ");
            ModeSelectrionComboBox.Items.Add(" ");
            ModeSelectrionComboBox.Items.Add(" ");
            ModeSelectrionComboBox.Items.Add(" ");
            ModeSelectrionComboBox.Items.Add(" ");
            VisualizationTypeComboBox.Items.Add(" ");
            VisualizationTypeComboBox.Items.Add(" ");
            VisualizationTypeComboBox.Items.Add(" ");
            VisualizationTypeComboBox.Items.Add(" ");
            VisualizationTypeComboBox.Items.Add(" ");
            VisualizationTypeComboBox.Items.Add(" ");
            PixelTypeComboBox.Items.Add(" ");
            PixelTypeComboBox.Items.Add(" ");
            PixelBitstreamComboBox.Items.Add(" ");
            PixelBitstreamComboBox.Items.Add(" ");

            SetLoadingLabelTo("Indexing Comboboxes");

            AudioSourceComboBox.SelectedIndex = 0;
            VisualizationTypeComboBox.SelectedIndex = 0;
            AudioSampleRateComboBox.SelectedIndex = 6;
            PixelTypeComboBox.SelectedIndex = 0;
            PixelBitstreamComboBox.SelectedIndex = 0;

            SetLoadingLabelTo("Screen index");

            AmbiLightModeScreenIDNumericUpDown.Maximum = SystemInformation.MonitorCount - 1;

            SetLoadingLabelTo("Last Setup");

            AutoloadLastSetup();

            SetLoadingLabelTo("Last instructions");

            AutoloadLastInstructions();

            SetLoadingLabelTo("Default language pack");

            if (LanguageComboBox.Items.Contains("EN"))
                LanguageComboBox.SelectedIndex = LanguageComboBox.FindString("EN");
            else
                LanguageComboBox.SelectedIndex = 0;

            SetLoadingLabelTo("Previus settings");

            AutoLoadAllSettings();

            SetLoadingLabelTo("Formating layout");

            FormatLayout();

            SetLoadingLabelTo("Complete!");

            ShowLoadingScreen = false;
            for (double i = 0; i <= 100; i += 2)
            {
                Opacity = i / 100;
                await Task.Delay(10);
            }
            if (ConfigureSetupAutoSendCheckBox.Checked)
            {
                if (ComPortsComboBox.Items.Count > 0)
                {
                    ConfigureSetupAutoSendCheckBox.Enabled = false;
                    ConfigureSetupHiddenProgressBar.Visible = true;
                    for (int i = Width; i > Width - MenuButton.Width; i--)
                    {
                        ConfigureSetupHiddenProgressBar.Location = new Point(i, 0);
                        await Task.Delay(5);
                    }
                    ConnectToComDevice();
                }
                else
                    MessageBox.Show("Error, saved COM port not found!");
            }
        }

        public void LoadingScreen()
        {
            Task.Run(() =>
            {
                LoadingForm = new Loading();
                LoadingForm.Show();
                for (double i = 0; i <= 100; i += 2)
                {
                    LoadingForm.Opacity = i / 100;
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                while (ShowLoadingScreen) { Application.DoEvents(); Thread.Sleep(10); }
                for (double i = 100; i >= 0; i -= 4)
                {
                    LoadingForm.Opacity = i / 100;
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                LoadingForm.Dispose();
                LoadingForm.Dispose();
            });
        }

        void SetLoadingLabelTo(string _Input)
        {
            LoadingForm.LoadingScreenLabel.Invoke((MethodInvoker)delegate { LoadingForm.LoadingScreenLabel.Text = "Loading: " + _Input; });
        }

        private void InitializeBass()
        {
            AudioSourceComboBox.Items.Clear();
            for (int i = 0; i < BassWasapi.BASS_WASAPI_GetDeviceCount(); i++)
            {
                var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
                if (device.IsEnabled && device.IsLoopback)
                {
                    AudioSourceComboBox.Items.Add(string.Format("{0} - {1}", i, device.name));
                }
            }

            foreach (string s in SerialPort.GetPortNames())
            {
                ComPortsComboBox.Items.Add(s);
            }
        }

        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!UnitReady)
                UnitReady = true;
            ReadyToRecive = true;
            SerialPort1.ReadChar();       
        }

        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] Lines = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\Languages\\" + LanguageComboBox.SelectedItem + ".txt");
            for (int i = 0; i < Lines.Length; i++)
            {
                try
                {
                    string[] Split = Lines[i].Split(';');
                    if (Split[0] != "")
                    {
                        if (Split[0].ToUpper() == "INDEXNAMES")
                        {
                            Control[] ControlText = Controls.Find(Split[1], true);
                            if (ControlText.Length > 0)
                            {
                                foreach (Control c in ControlText)
                                {
                                    ComboBox ChangeIndexNameComboBox = c as ComboBox;
                                    for (int j = 2; j < Split.Length; j++)
                                    {
                                        ChangeIndexNameComboBox.Items[j - 2] = Split[j];
                                    }
                                }
                            }
                            else
                                MessageBox.Show("Control: " + Split[1] + " Was not found! ( Line: " + i + " )");
                        }
                        else
                        {
                            if (Split[0].ToUpper() == "POSSIBLE")
                            {
                                Control[] ControlText = Controls.Find(Split[0], true);
                                if (ControlText.Length > 0)
                                {
                                    foreach (Control c in ControlText)
                                    {
                                        c.Font = new Font(Split[1], Int32.Parse(Split[2]));
                                        if (Split[3] != "")
                                            c.Text = Split[3];
                                    }
                                }
                            }
                            else
                            {
                                Control[] ControlText = Controls.Find(Split[0], true);
                                if (ControlText.Length > 0)
                                {
                                    foreach (Control c in ControlText)
                                    {
                                        c.Font = new Font(Split[1], Int32.Parse(Split[2]));
                                        if (Split[3] != "")
                                            c.Text = Split[3];
                                    }
                                }
                                else
                                    MessageBox.Show("Control: " + Split[0] + " Was not found! ( Line: " + i + " )");
                            }
                        }
                    }
                }
                catch {  }
            }

            FormatLayout();
        }

        void AutoLoadAllSettings()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\cfg.txt"))
            {
                LoadSettings(Directory.GetCurrentDirectory() + "\\cfg.txt");
            }
        }

        void AutoSaveAllSettings()
        {
            GetAllControls(this);

            SaveSettings(Directory.GetCurrentDirectory() + "\\cfg.txt", "SERIALPORT;" + SerialPort1.BaudRate);
        }

        void GetAllControls(Control _InputControl)
        {
            foreach (Control c in _InputControl.Controls)
            {
                GetAllControls(c);
                if (c.Tag != null)
                    if (c.Tag is string)
                        if ((string)c.Tag == "Setting")
                            ControlList.Add(c);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            AutoSaveAllSettings();
        }

        private void ResetToDefaultPosition(object sender, EventArgs e)
        {
            Size = new Size(Sizex, Sizey);
            Location = new Point(Screen.PrimaryScreen.Bounds.Width - Sizex, 0);
        }

        void SendDataBySerial(string _Input)
        {
            if (UnitReady)
            {
                int TimeoutCounter = 0;
                while (!ReadyToRecive)
                {
                    Thread.Sleep(1);
                    TimeoutCounter++;
                    if (TimeoutCounter > 500)
                    {
                        ReadyToRecive = true;
                        break;
                    }
                }
            }
            if (ReadyToRecive)
            {
                try
                {
                    SerialPort1.WriteLine(";" + _Input + ";-10;");
                }
                catch { }
                ReadyToRecive = false;
            }
        }

        private async void HideTimer_Tick(object sender, EventArgs e)
        {
            for (double i = 100; i >= 0; i -= 2)
            {
                Opacity = i / 100;
                await Task.Delay(10);
            }
            HideTimer.Stop();
        }

        private async void MainForm_Activated(object sender, EventArgs e)
        {
            if (MenuAutoHideCheckBox.Checked)
            {
                if (!MenuPanel.Visible)
                {
                    if (Opacity != 1)
                    {
                        if (!ShowLoadingScreen)
                        {
                            HideTimer.Stop();
                            bool BreakInside = false;
                            for (double i = 0; i <= 100; i += 2)
                            {
                                if (Opacity == 1)
                                {
                                    BreakInside = true;
                                    break;
                                }
                                Opacity = i / 100;
                                await Task.Delay(10);
                            }
                            if (!BreakInside)
                                HideTimer.Start();
                        }
                    }
                }
            }
        }

        #endregion

        #region Menu Section

        private void Connect(object sender, EventArgs e)
        {
            ConnectToComDevice();
        }

        async void ConnectToComDevice()
        {
            try
            {
                if (SerialPort1.IsOpen)
                    SerialPort1.Close();
                SerialPort1.PortName = ComPortsComboBox.Text;
                SerialPort1.Open();
                UnitReady = false;
            }
            catch { }

            if (SerialPort1.IsOpen)
            {
                int DelayCount = 0;
                bool NoError = true;
                while (!UnitReady)
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                    DelayCount++;
                    if (DelayCount >= 100)
                    {
                        if (ConfigureSetupAutoSendCheckBox.Checked)
                            ConfigureSetupHiddenProgressBar.Visible = false;
                        MessageBox.Show("Error, unit timed out!");
                        NoError = false;
                        break;
                    }
                }
                if (NoError)
                {
                    ModeSelectrionComboBox.Enabled = true;
                    if (!ConfigureSetupAutoSendCheckBox.Checked)
                        ModeSelectrionComboBox.SelectedIndex = 5;
                    else
                        await SendSetup();
                }
            }
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            if (MenuPanel.Visible)
            {
                MenuPanel.Visible = false;
                IndividualLEDPanel.Visible = false;
                VisualizerPanel.Visible = false;
                ConfigureSetupPanel.Visible = false;
                InstructionsPanel.Visible = false;
                AmbiLightModePanel.Visible = false;
                AutoSaveAllSettings();
                if (MenuAutoHideCheckBox.Checked)
                    HideTimer.Start();
            }
            else
            {
                if (MenuAutoHideCheckBox.Checked)
                {
                    HideTimer.Stop();
                    Opacity = 1;
                }
                MenuPanel.Visible = true;
            }
        }

        private void ModeSelectrionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FadeLEDPanel.Enabled = false;
            VisualizerPanel.Visible = false;
            IndividualLEDPanel.Visible = false;
            InstructionsPanel.Visible = false;
            ConfigureSetupPanel.Visible = false;
            AmbiLightModePanel.Visible = false;

            if (!ContinueInstructionsLoop)
                EnableBASS(false);

            if (ModeSelectrionComboBox.SelectedIndex == 0)
            {
                FadeLEDPanel.Enabled = true;
                FadeLEDPanel.BringToFront();
                if (!ContinueInstructionsLoop)
                    FadeColorsSendData(true);
            }
            if (ModeSelectrionComboBox.SelectedIndex == 1)
            {
                StopAmbilight();

                VisualizerPanel.Visible = true;
                VisualizerPanel.BringToFront();
                if (!ContinueInstructionsLoop)
                {
                    string SerialOut = "6;" + VisualizerFromSeriesIDNumericUpDown.Value + ";" + VisualizerToSeriesIDNumericUpDown.Value;
                    SendDataBySerial(SerialOut);
                    EnableBASS(true);
                }
            }
            if (ModeSelectrionComboBox.SelectedIndex == 2)
            {
                IndividualLEDPanel.Visible = true;
                IndividualLEDPanel.BringToFront();
                if (!ContinueInstructionsLoop)
                    FadeColorsSendData(true);
            }
            if (ModeSelectrionComboBox.SelectedIndex == 3)
            {
                InstructionsPanel.Visible = true;
                InstructionsPanel.BringToFront();
                if (!ContinueInstructionsLoop)
                    FadeColorsSendData(true);
            }
            if (ModeSelectrionComboBox.SelectedIndex == 4)
            {
                AmbiLightModePanel.Visible = true;
                AmbiLightModePanel.BringToFront();
            }
            if (ModeSelectrionComboBox.SelectedIndex == 5)
            {
                ConfigureSetupPanel.Visible = true;
                ConfigureSetupPanel.BringToFront();
            }
        }

        private void MenuExitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region Fade Colors Section

        private void FadeColorsRed_ValueChanged(object sender, EventArgs e)
        {
            FadeColorsRedLabel.Text = FadeColorsRedTrackBar.Value.ToString();
            FormatCustomText((int)Math.Round(((double)(FadeColorsRedTrackBar.Value + FadeColorsGreenTrackBar.Value + FadeColorsBlueTrackBar.Value) / (double)(3 * 255)) * 100, 0), FadeColorsBrightnessLabel, "%");
        }

        private void FadeColorsGreen_ValueChanged(object sender, EventArgs e)
        {
            FadeColorsGreenLabel.Text = FadeColorsGreenTrackBar.Value.ToString();
            FormatCustomText((int)Math.Round(((double)(FadeColorsRedTrackBar.Value + FadeColorsGreenTrackBar.Value + FadeColorsBlueTrackBar.Value) / (double)(3 * 255)) * 100, 0), FadeColorsBrightnessLabel, "%");
        }

        private void FadeColorsBlue_ValueChanged(object sender, EventArgs e)
        {
            FadeColorsBlueLabel.Text = FadeColorsBlueTrackBar.Value.ToString();
            FormatCustomText((int)Math.Round(((double)(FadeColorsRedTrackBar.Value + FadeColorsGreenTrackBar.Value + FadeColorsBlueTrackBar.Value) / (double)(3 * 255)) * 100, 0), FadeColorsBrightnessLabel, "%");
        }

        private void FadeColors_BeginSendData(object sender, MouseEventArgs e)
        {
            FadeColorsSendData(false);
        }

        void FadeColorsSendData(bool _FromZero)
        {
            if (SerialPort1.IsOpen)
            {
                string SerialOut;
                SerialOut = "6;" + FadeLEDPanelFromIDNumericUpDown.Value + ";" + FadeLEDPanelToIDNumericUpDown.Value;
                SendDataBySerial(SerialOut);

                if (_FromZero)
                {
                    SerialOut = "1;0;0;0;0;0";
                    SendDataBySerial(SerialOut);
                }

                Color AfterShuffel = ShuffleColors(Color.FromArgb(FadeColorsRedTrackBar.Value, FadeColorsGreenTrackBar.Value, FadeColorsBlueTrackBar.Value));

                SerialOut = "1;" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B + ";" + FadeColorsFadeSpeedNumericUpDown.Value + ";" + Math.Round(FadeColorsFadeFactorNumericUpDown.Value * 100, 0);
                SendDataBySerial(SerialOut);
            }
        }

        Color ShuffleColors(Color _InputColors)
        {
            int Red = 0;
            int Green = 0;
            int Blue = 0;

            if (ConfigureSetupRGBColorOrderFirstTextbox.Text == "R")
                Red = _InputColors.R;
            if (ConfigureSetupRGBColorOrderFirstTextbox.Text == "G")
                Red = _InputColors.G;
            if (ConfigureSetupRGBColorOrderFirstTextbox.Text == "B")
                Red = _InputColors.B;

            if (ConfigureSetupRGBColorOrderSeccondTextbox.Text == "R")
                Green = _InputColors.R;
            if (ConfigureSetupRGBColorOrderSeccondTextbox.Text == "G")
                Green = _InputColors.G;
            if (ConfigureSetupRGBColorOrderSeccondTextbox.Text == "B")
                Green = _InputColors.B;

            if (ConfigureSetupRGBColorOrderThirdTextbox.Text == "R")
                Blue = _InputColors.R;
            if (ConfigureSetupRGBColorOrderThirdTextbox.Text == "G")
                Blue = _InputColors.G;
            if (ConfigureSetupRGBColorOrderThirdTextbox.Text == "B")
                Blue = _InputColors.B;

            return Color.FromArgb(Red, Green, Blue);
        }

        #endregion

        #region Configure Setup Section

        private void AddLEDStrip(object sender, EventArgs e)
        {
            MakeLEDStrip(0, 0, (int)ConfigureSetupAddStripFromLEDID.Value, ConfigureSetupAddStripInvertX.Checked, ConfigureSetupAddStripInvertY.Checked, (int)ConfigureSetupAddStripXDir.Value, (int)ConfigureSetupAddStripYDir.Value, (int)ConfigureSetupAddStripPinID.Value, "", false, PixelTypeComboBox.SelectedIndex, PixelBitstreamComboBox.SelectedIndex);
        }

        void MakeLEDStrip(int _XLocation, int _YLocation, int _FromLEDID, bool _InvertXDir, bool _InvertYDir, int _XLEDAmount, int _YLEDAmount, int _PinID, string _IndputTextData, bool _IsIndividualLEDs, int _PixelTypeIndex, int _PixelBitstreamIndex)
        {
            int CurrentLED = _FromLEDID;
            Panel BackPanel = new Panel();
            BackPanel.BorderStyle = BorderStyle.FixedSingle;
            BackPanel.Width = (int)(_XLEDAmount * ButtonWidth + Margins) + Margins * 2;
            BackPanel.Height = (int)(_YLEDAmount * (2 * ButtonHeight) + Margins) + Margins * 2 + ButtonHeight;
            BackPanel.Location = new Point(_XLocation, _YLocation);
            if (_IsIndividualLEDs)
                BackPanel.Font = new Font(IndividualLEDWorkingPanel.Font.FontFamily, BackPanel.Font.Size);
            else
                BackPanel.Font = new Font(ConfigureSetupWorkingPanel.Font.FontFamily, BackPanel.Font.Size);
            BackPanel.BackColor = Color.Gray;
            if (!_IsIndividualLEDs)
            {
                BackPanel.MouseMove += MoveLEDStrip;
                BackPanel.MouseDown += MoveLEDStripDown;
                BackPanel.MouseUp += MoveLEDStripUp;
            }

            Button DeleteButton = new Button();
            DeleteButton.Width = ButtonWidth;
            DeleteButton.Height = ButtonHeight;
            DeleteButton.Text = "X";
            DeleteButton.Parent = BackPanel;
            DeleteButton.Click += RemoveLEDStrip;
            DeleteButton.Font = new Font(BackPanel.Font.FontFamily, 7);
            DeleteButton.Location = new Point(0, Margins);
            DeleteButton.FlatStyle = FlatStyle.Flat;
            DeleteButton.FlatAppearance.BorderSize = 0;
            DeleteButton.BackColor = Color.DarkGray;
            DeleteButton.Name = "MakeLEDPanelDeleteButton";
            BackPanel.Controls.Add(DeleteButton);

            Label StripIDLabel = new Label();
            StripIDLabel.Height = ButtonHeight;
            StripIDLabel.Font = new Font(BackPanel.Font.FontFamily, 7);
            StripIDLabel.Tag = _PinID;
            StripIDLabel.Text = _PinID.ToString();
            StripIDLabel.Location = new Point(StripIDLabel.Location.X + ButtonWidth, Margins);
            StripIDLabel.ForeColor = Color.White;
            StripIDLabel.Name = "MakeLEDPanelStripIDLabel";
            BackPanel.Controls.Add(StripIDLabel);

            bool UseDefaultText = true;
            string[] InputTextDataSplit = _IndputTextData.Split(';');
            if (_IndputTextData != "")
                UseDefaultText = false;

            int BoxNumber = 0;

            for (int i = 0; i < _XLEDAmount; i++)
            {
                for (int j = 0; j < _YLEDAmount; j++)
                {
                    if (!_IsIndividualLEDs)
                    {
                        TextBox NewButton = new TextBox();
                        NewButton.Width = ButtonWidth;
                        NewButton.Height = ButtonHeight;
                        NewButton.Text = CurrentLED.ToString();
                        NewButton.Font = new Font(BackPanel.Font.FontFamily, 7);
                        NewButton.Enabled = false;
                        NewButton.TextAlign = HorizontalAlignment.Center;
                        NewButton.BorderStyle = BorderStyle.None;
                        NewButton.BackColor = Color.DarkGray;
                        NewButton.ForeColor = Color.White;
                        NewButton.Name = "MakeLEDPanelStripLEDIDLabel";
                        if (_InvertXDir)
                        {
                            if (_InvertYDir)
                                NewButton.Location = new Point(Margins + ButtonWidth * ((_XLEDAmount - 1) - i), Margins + (ButtonHeight * 2) * ((_YLEDAmount - 1) - j) + (Margins + ButtonHeight));
                            else
                                NewButton.Location = new Point(Margins + ButtonWidth * ((_XLEDAmount - 1) - i), Margins + (ButtonHeight * 2) * j + (Margins + ButtonHeight));
                        }
                        else
                        {
                            if (_InvertYDir)
                                NewButton.Location = new Point(Margins + ButtonWidth * i, Margins + (ButtonHeight * 2) * ((_YLEDAmount - 1) - j) + (Margins + ButtonHeight));
                            else
                                NewButton.Location = new Point(Margins + ButtonWidth * i, Margins + (ButtonHeight * 2) * j + (Margins + ButtonHeight));
                        }

                        TextBox NewTextBox = new TextBox();
                        NewTextBox.Width = ButtonWidth;
                        NewTextBox.Height = ButtonHeight;
                        NewTextBox.Click += ClickToSetSeries;
                        if (UseDefaultText)
                        {
                            NewTextBox.Text = "0";
                        }
                        else
                        {
                            NewTextBox.Text = InputTextDataSplit[BoxNumber];
                        }
                        NewTextBox.Font = new Font(BackPanel.Font.FontFamily, 7);
                        NewTextBox.TextChanged += FormatText;
                        NewTextBox.Location = new Point(NewButton.Location.X, NewButton.Location.Y + ButtonHeight);
                        NewTextBox.Parent = BackPanel;
                        NewTextBox.BorderStyle = BorderStyle.None;
                        NewTextBox.BackColor = Color.DarkGray;
                        NewTextBox.ForeColor = Color.White;
                        NewTextBox.Name = "MakeLEDPanelStripSeriesIDLabel";

                        BackPanel.Controls.Add(NewButton);
                        BackPanel.Controls.Add(NewTextBox);
                    }
                    else
                    {
                        Button NewButton = new Button();
                        NewButton.Width = ButtonWidth;
                        NewButton.Height = ButtonHeight;
                        NewButton.Text = CurrentLED.ToString();
                        NewButton.Font = new Font(BackPanel.Font.FontFamily, 5);
                        NewButton.FlatStyle = FlatStyle.Flat;
                        NewButton.FlatAppearance.BorderSize = 0;
                        NewButton.BackColor = Color.DarkGray;
                        NewButton.ForeColor = Color.White;
                        NewButton.Name = "MakeLEDPanelStripLEDIDButton";
                        if (_InvertXDir)
                        {
                            if (_InvertYDir)
                                NewButton.Location = new Point(Margins + ButtonWidth * ((_XLEDAmount - 1) - i), Margins + (ButtonHeight * 2) * ((_YLEDAmount - 1) - j) + (Margins + ButtonHeight));
                            else
                                NewButton.Location = new Point(Margins + ButtonWidth * ((_XLEDAmount - 1) - i), Margins + (ButtonHeight * 2) * j + (Margins + ButtonHeight));
                        }
                        else
                        {
                            if (_InvertYDir)
                                NewButton.Location = new Point(Margins + ButtonWidth * i, Margins + (ButtonHeight * 2) * ((_YLEDAmount - 1) - j) + (Margins + ButtonHeight));
                            else
                                NewButton.Location = new Point(Margins + ButtonWidth * i, Margins + (ButtonHeight * 2) * j + (Margins + ButtonHeight));
                        }

                        NewButton.Click += ColorSingleLED;
                        BackPanel.Controls.Add(NewButton);
                    }


                    CurrentLED++;
                    BoxNumber++;
                }
            }

            Point3D[] TagData = { new Point3D(_XLocation, _YLocation, 0), new Point3D(_XLEDAmount, _YLEDAmount, _PinID), new Point3D(Convert.ToInt32(_InvertXDir), Convert.ToInt32(_InvertYDir), _FromLEDID), new Point3D(_PixelTypeIndex, _PixelBitstreamIndex, 0) };
            BackPanel.Tag = TagData;

            if (_IsIndividualLEDs)
                IndividualLEDWorkingPanel.Controls.Add(BackPanel);
            else
                ConfigureSetupWorkingPanel.Controls.Add(BackPanel);
        }

        private void FormatText(object sender, EventArgs e)
        {
            TextBox SenderTextBox = sender as TextBox;
            if (!IsDigitsOnly(SenderTextBox.Text))
                if (SenderTextBox.Text.Length > 0)
                {
                    SenderTextBox.Text = SenderTextBox.Text.Substring(0, SenderTextBox.Text.Length - 1);
                    SenderTextBox.SelectionStart = SenderTextBox.Text.Length;
                    SenderTextBox.SelectionLength = 0;
                }
        }

        bool IsDigitsOnly(string _Input)
        {
            foreach (char c in _Input)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        private void ClickToSetSeries(object sender, EventArgs e)
        {
            if (ConfigureSetupClickToSetupSeriesCheckBox.Checked)
            {
                TextBox SenderTextBox = sender as TextBox;
                SenderTextBox.Text = ConfigureSetupClickToSetupSeriesFromIDNumericUpDown.Value.ToString();
                ConfigureSetupClickToSetupSeriesFromIDNumericUpDown.Value++;
            }
        }

        private void MoveLEDStrip(object sender, MouseEventArgs e)
        {
            Panel SenderPanel = sender as Panel;
            Point3D[] MomentaryDataTag = (Point3D[])SenderPanel.Tag;
            if (MomentaryDataTag[0].Z == 1)
            {
                SenderPanel.Location = new Point((int)MomentaryDataTag[0].X + (MousePosition.X - DragStart.X), (int)MomentaryDataTag[0].Y + (MousePosition.Y - DragStart.Y));
            }
        }

        private void MoveLEDStripDown(object sender, MouseEventArgs e)
        {
            DragStart = MousePosition;
            Panel SenderPanel = sender as Panel;
            foreach (Control InnerControl in SenderPanel.Parent.Controls)
            {
                foreach (Control InnerInnerControl in InnerControl.Controls)
                {
                    InnerInnerControl.Visible = false;
                }
            }
            Point3D[] MomentaryDataTag = (Point3D[])SenderPanel.Tag;
            MomentaryDataTag[0] = new Point3D(SenderPanel.Location.X, SenderPanel.Location.Y, 1);
            SenderPanel.Tag = MomentaryDataTag;
        }

        private void MoveLEDStripUp(object sender, MouseEventArgs e)
        {
            Panel SenderPanel = sender as Panel;
            foreach (Control InnerControl in SenderPanel.Parent.Controls)
            {
                foreach (Control InnerInnerControl in InnerControl.Controls)
                {
                    InnerInnerControl.Visible = true;
                }
            }
            Point3D[] MomentaryDataTag = (Point3D[])SenderPanel.Tag;
            MomentaryDataTag[0] = new Point3D(SenderPanel.Location.X, SenderPanel.Location.Y, 0);
            SenderPanel.Tag = MomentaryDataTag;
        }

        private void RemoveLEDStrip(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            SenderButton.Parent.Dispose();
        }

        private void SaveSetup(object sender, EventArgs e)
        {
            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\Setups";
            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveCurrentSetup();
            }
            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        void SaveCurrentSetup()
        {
            using (StreamWriter SaveFile = new StreamWriter(SaveFileDialog.FileName, false))
            {
                using (StreamWriter AutoSaveFile = new StreamWriter(Directory.GetCurrentDirectory() + "\\Setups\\0.txt", false))
                {
                    foreach (Control c in ConfigureSetupWorkingPanel.Controls)
                    {
                        Point3D[] MomentaryDataTag = (Point3D[])c.Tag;
                        string SerialOut = c.Location.X + ";" + c.Location.Y + ";" + MomentaryDataTag[2].Z + ";" + Convert.ToBoolean(MomentaryDataTag[2].X) + ";" + Convert.ToBoolean(MomentaryDataTag[2].Y) + ";" + MomentaryDataTag[1].X + ";" + MomentaryDataTag[1].Y + ";" + MomentaryDataTag[1].Z + ";" + MomentaryDataTag[3].X + ";" + MomentaryDataTag[3].Y;
                        SaveFile.WriteLine(SerialOut);
                        AutoSaveFile.WriteLine(SerialOut);

                        SerialOut = "";
                        for (int j = 0; j < c.Controls.Count; j++)
                        {
                            if (c.Controls[j].Name == "MakeLEDPanelStripSeriesIDLabel")
                            {
                                SerialOut += c.Controls[j].Text + ";";
                            }
                        }

                        SaveFile.WriteLine(SerialOut);
                        AutoSaveFile.WriteLine(SerialOut);
                    }
                }
            }
        }

        void AutoloadLastSetup()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\Setups\\0.txt"))
            {
                while (ConfigureSetupWorkingPanel.Controls.Count > 0)
                    ConfigureSetupWorkingPanel.Controls[0].Dispose();
                while (IndividualLEDWorkingPanel.Controls.Count > 0)
                    IndividualLEDWorkingPanel.Controls[0].Dispose();

                string[] Lines = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\Setups\\0.txt", System.Text.Encoding.UTF8);
                for (int i = 0; i < Lines.Length; i++)
                {
                    string[] Split = Lines[i].Split(';');
                    MakeLEDStrip(Int32.Parse(Split[0]), Int32.Parse(Split[1]), Int32.Parse(Split[2]), Boolean.Parse(Split[3]), Boolean.Parse(Split[4]), Int32.Parse(Split[5]), Int32.Parse(Split[6]), Int32.Parse(Split[7]), Lines[i + 1], false, Int32.Parse(Split[8]), Int32.Parse(Split[9]));
                    MakeLEDStrip(Int32.Parse(Split[0]), Int32.Parse(Split[1]), Int32.Parse(Split[2]), Boolean.Parse(Split[3]), Boolean.Parse(Split[4]), Int32.Parse(Split[5]), Int32.Parse(Split[6]), Int32.Parse(Split[7]), Lines[i + 1], true, Int32.Parse(Split[8]), Int32.Parse(Split[9]));
                    i++;
                }
            }
        }

        private void LoadSetup(object sender, EventArgs e)
        {
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\Setups";
            if (LoadFileDialog.ShowDialog() == DialogResult.OK)
            {
                while (ConfigureSetupWorkingPanel.Controls.Count > 0)
                    ConfigureSetupWorkingPanel.Controls[0].Dispose();
                while (IndividualLEDWorkingPanel.Controls.Count > 0)
                    IndividualLEDWorkingPanel.Controls[0].Dispose();

                string[] Lines = File.ReadAllLines(LoadFileDialog.FileName, System.Text.Encoding.UTF8);
                for (int i = 0; i < Lines.Length; i++)
                {
                    string[] Split = Lines[i].Split(';');
                    MakeLEDStrip(Int32.Parse(Split[0]), Int32.Parse(Split[1]), Int32.Parse(Split[2]), Boolean.Parse(Split[3]), Boolean.Parse(Split[4]), Int32.Parse(Split[5]), Int32.Parse(Split[6]), Int32.Parse(Split[7]), Lines[i + 1], false, Int32.Parse(Split[8]), Int32.Parse(Split[9]));
                    MakeLEDStrip(Int32.Parse(Split[0]), Int32.Parse(Split[1]), Int32.Parse(Split[2]), Boolean.Parse(Split[3]), Boolean.Parse(Split[4]), Int32.Parse(Split[5]), Int32.Parse(Split[6]), Int32.Parse(Split[7]), Lines[i + 1], true, Int32.Parse(Split[8]), Int32.Parse(Split[9]));
                    i++;
                }
            }
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private async void SendSetupButton_Click(object sender, EventArgs e)
        {
            while (IndividualLEDWorkingPanel.Controls.Count > 0)
                IndividualLEDWorkingPanel.Controls[0].Dispose();

            foreach (Control InnerControl in ConfigureSetupWorkingPanel.Controls)
            {
                Point3D[] MomentaryDataTag = (Point3D[])InnerControl.Tag;
                MakeLEDStrip((int)MomentaryDataTag[0].X, (int)MomentaryDataTag[0].Y, (int)MomentaryDataTag[2].Z, Convert.ToBoolean(MomentaryDataTag[2].X), Convert.ToBoolean(MomentaryDataTag[2].Y), (int)MomentaryDataTag[1].X, (int)MomentaryDataTag[1].Y, (int)MomentaryDataTag[1].Z, "", true, (int)MomentaryDataTag[3].X, (int)MomentaryDataTag[3].Y);
            }

            await SendSetup();
        }

        public async Task SendSetup()
        {
            await Task.Run(async () =>
            {
                List<int> Pins = new List<int>();
                List<int> LEDCount = new List<int>();
                List<int> PixelTypesIndexs = new List<int>();
                List<int> PixelBitrateIndexs = new List<int>();

                foreach (Control c in ConfigureSetupWorkingPanel.Controls)
                {
                    Point3D[] MomentaryDataTag = (Point3D[])c.Tag;
                    if (!Pins.Contains((int)MomentaryDataTag[1].Z))
                    {
                        Pins.Add((int)MomentaryDataTag[1].Z);
                        LEDCount.Add(((int)MomentaryDataTag[1].X * (int)MomentaryDataTag[1].Y));
                        PixelTypesIndexs.Add((int)MomentaryDataTag[3].X);
                        PixelBitrateIndexs.Add((int)MomentaryDataTag[3].Y);
                    }
                    else
                    {
                        int Index = Pins.FindIndex(x => x == (int)MomentaryDataTag[1].Z);
                        LEDCount[Index] += ((int)MomentaryDataTag[1].X * (int)MomentaryDataTag[1].Y);
                    }
                }

                for (int i = 0; i < Pins.Count; i++)
                {
                    string SerialOut = "-1;" + LEDCount[i] + ";" + Pins[i] + ";" + PixelTypesIndexs[i] + ";" + PixelBitrateIndexs[i];
                    SendDataBySerial(SerialOut);
                }

                SendDataBySerial("-1;9999");

                int TotalLEDs = 0;
                foreach (int i in LEDCount)
                    TotalLEDs += i;

                if (EnableDataCompressionMode.Checked)
                {
                    SendSetupProgressBar.Invoke((MethodInvoker)delegate { SendSetupProgressBar.Maximum = ConfigureSetupWorkingPanel.Controls.Count; });
                    if (ConfigureSetupAutoSendCheckBox.Checked)
                        ConfigureSetupHiddenProgressBar.Invoke((MethodInvoker)delegate { ConfigureSetupHiddenProgressBar.Maximum = ConfigureSetupWorkingPanel.Controls.Count; });

                    List<int> UpOrDownFrom = new List<int>();
                    List<int> UpOrDownTo = new List<int>();
                    List<int> InternalPins = new List<int>();
                    List<int> SeriesData = new List<int>();

                    foreach (Control c in ConfigureSetupWorkingPanel.Controls)
                    {
                        Point3D[] MomentaryDataTag = (Point3D[])c.Tag;
                        InternalPins.Add((int)MomentaryDataTag[1].Z);

                        int Lowest = 999999;
                        int Highest = 0;
                        foreach (Control g in c.Controls)
                        {
                            if (g is TextBox)
                            {
                                if (!g.Enabled)
                                {
                                    int Value = Int32.Parse(g.Text);
                                    if (Value > Highest)
                                        Highest = Value;
                                    if (Value < Lowest)
                                        Lowest = Value;
                                }
                            }
                        }
                        int UpDownValue = Int32.Parse(c.Controls[c.Controls.Count - 1].Text) - Int32.Parse(c.Controls[c.Controls.Count - 3].Text);
                        if (UpDownValue < 0)
                            UpDownValue = 0;
                        if (UpDownValue > 0)
                        {
                            UpOrDownFrom.Add(Lowest);
                            UpOrDownTo.Add(Highest);
                        }
                        else
                        {
                            UpOrDownFrom.Add(Highest);
                            UpOrDownTo.Add(Lowest);
                        }
                    }

                    SendDataBySerial("-1;8888");

                    int PanelNumber = 0;
                    for (int i = 0; i < ConfigureSetupWorkingPanel.Controls.Count; i++)
                    {
                        int Position = SeriesData.Count;
                        for (int j = 0; j < SeriesData.Count; j++)
                        {
                            int ValueA = Int32.Parse(ConfigureSetupWorkingPanel.Controls[i].Controls[ConfigureSetupWorkingPanel.Controls[i].Controls.Count - 1].Text);
                            int ValueB = Int32.Parse(ConfigureSetupWorkingPanel.Controls[j].Controls[ConfigureSetupWorkingPanel.Controls[j].Controls.Count - 1].Text);
                            if (ValueA < ValueB)
                            {
                                Position--;
                            }
                        }
                        SeriesData.Insert(Position, PanelNumber);
                        PanelNumber++;
                    }

                    for (int i = 0; i < InternalPins.Count; i++)
                    {
                        SendSetupProgressBar.Invoke((MethodInvoker)delegate { SendSetupProgressBar.Value = i; });
                        if (ConfigureSetupAutoSendCheckBox.Checked)
                            ConfigureSetupHiddenProgressBar.Invoke((MethodInvoker)delegate { ConfigureSetupHiddenProgressBar.Value = i; });

                        string SerialOut = "-1;" + UpOrDownFrom[SeriesData[i]] + ";" + UpOrDownTo[SeriesData[i]] + ";" + InternalPins[SeriesData[i]];
                        SendDataBySerial(SerialOut);
                    }

                    SendDataBySerial("-1;9999;");
                }
                else
                {
                    SendSetupProgressBar.Invoke((MethodInvoker)delegate { SendSetupProgressBar.Maximum = TotalLEDs; });
                    if (ConfigureSetupAutoSendCheckBox.Checked)
                        ConfigureSetupHiddenProgressBar.Invoke((MethodInvoker)delegate { ConfigureSetupHiddenProgressBar.Maximum = TotalLEDs; });

                    for (int i = 0; i < TotalLEDs; i++)
                    {
                        SendSetupProgressBar.Invoke((MethodInvoker)delegate { SendSetupProgressBar.Value = i; });
                        if (ConfigureSetupAutoSendCheckBox.Checked)
                            ConfigureSetupHiddenProgressBar.Invoke((MethodInvoker)delegate { ConfigureSetupHiddenProgressBar.Value = i; });
                        foreach (Control c in ConfigureSetupWorkingPanel.Controls)
                        {
                            for (int j = 0; j < c.Controls.Count; j++)
                            {
                                if (c.Controls[j] is TextBox)
                                {
                                    if (c.Controls[j].Enabled)
                                    {
                                        TextBox LEDTextBox = c.Controls[j] as TextBox;
                                        if (c.Controls[j].Text == i.ToString())
                                        {
                                            Point3D[] MomentaryDataTag = (Point3D[])c.Controls[j].Parent.Tag;
                                            string SerialOut = "-1;" + c.Controls[j - 1].Text + ";" + MomentaryDataTag[1].Z;
                                            SendDataBySerial(SerialOut);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                SendSetupProgressBar.Invoke((MethodInvoker)delegate { SendSetupProgressBar.Value = 0; });
                if (ConfigureSetupAutoSendCheckBox.Checked)
                {
                    for (int i = Width - MenuButton.Width; i < Width; i++)
                    {
                        ConfigureSetupHiddenProgressBar.Invoke((MethodInvoker)delegate { ConfigureSetupHiddenProgressBar.Location = new Point(i, 0); });
                        await Task.Delay(5);
                    }
                    ConfigureSetupHiddenProgressBar.Invoke((MethodInvoker)delegate { ConfigureSetupHiddenProgressBar.Visible = false; });
                    ConfigureSetupAutoSendCheckBox.Invoke((MethodInvoker)delegate { ConfigureSetupAutoSendCheckBox.Enabled = true; });
                }

                SendDataBySerial("-1;9999");
            });
        }

        private void ConfigureSetupWorkingPanel_MouseDown(object sender, MouseEventArgs e)
        {
            WorkingPanelMouseDown(sender);
        }

        private void ConfigureSetupWorkingPanel_MouseUp(object sender, MouseEventArgs e)
        {
            WorkingPanelMouseUp(sender);
        }

        private void ConfigureSetupWorkingPanel_MouseMove(object sender, MouseEventArgs e)
        {
            WorkingPanelMouseMove(sender);
        }

        void WorkingPanelMouseDown(object _Sender)
        {
            DragStart = MousePosition;
            Panel SenderPanel = _Sender as Panel;
            foreach (Control InnerControl in SenderPanel.Controls)
            {
                foreach (Control InnerInnerControl in InnerControl.Controls)
                {
                    InnerInnerControl.Visible = false;
                }
                Point3D[] MomentaryDataTag = (Point3D[])InnerControl.Tag;
                MomentaryDataTag[0] = new Point3D(InnerControl.Location.X, InnerControl.Location.Y, 1);
                InnerControl.Tag = MomentaryDataTag;
            }
        }
        void WorkingPanelMouseUp(object _Sender)
        {
            Panel SenderPanel = _Sender as Panel;
            foreach (Control InnerControl in SenderPanel.Controls)
            {
                foreach (Control InnerInnerControl in InnerControl.Controls)
                {
                    InnerInnerControl.Visible = true;
                }
                Point3D[] MomentaryDataTag = (Point3D[])InnerControl.Tag;
                MomentaryDataTag[0] = new Point3D(InnerControl.Location.X, InnerControl.Location.Y, 0);
                InnerControl.Tag = MomentaryDataTag;
            }
        }
        void WorkingPanelMouseMove(object _Sender)
        {
            Panel SenderPanel = _Sender as Panel;
            foreach (Control InnerControl in SenderPanel.Controls)
            {
                Point3D[] MomentaryDataTag = (Point3D[])InnerControl.Tag;
                if (MomentaryDataTag[0].Z == 1)
                {
                    InnerControl.Location = new Point((int)MomentaryDataTag[0].X + (MousePosition.X - DragStart.X), (int)MomentaryDataTag[0].Y + (MousePosition.Y - DragStart.Y));
                }
            }
        }

        private void ConfigureSetupRGBColorOrderTextboxes_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'R' | e.KeyChar == 'G' | e.KeyChar == 'B')
            {
                TextBox SenderTextbox = sender as TextBox;
                SenderTextbox.Text = e.KeyChar.ToString();
            }
            e.Handled = true;
        }

        #endregion

        #region Individual LED Section

        private async void ColorSingleLED(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            if (ColorEntireLEDStripCheckBox.Checked)
            {
                await ColorEntireLEDStrip(SenderButton);
            }
            else
            {
                Point3D[] MomentaryDataTag = (Point3D[])SenderButton.Parent.Tag;
                SenderButton.BackColor = Color.FromArgb(IndividalLEDRedTrackBar.Value, IndividalLEDGreenTrackBar.Value, IndividalLEDBlueTrackBar.Value);
                Color AfterShuffel = ShuffleColors(Color.FromArgb(IndividalLEDRedTrackBar.Value, IndividalLEDGreenTrackBar.Value, IndividalLEDBlueTrackBar.Value));
                string SerialOut = "4;" + MomentaryDataTag[1].Z + ";" + SenderButton.Text + ";" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B;
                SendDataBySerial(SerialOut);
            }
        }

        public async Task ColorEntireLEDStrip(Button SenderButton)
        {
            await Task.Run(async () =>
            {
                foreach (Control c in SenderButton.Parent.Controls)
                {
                    if (c is Button)
                    {
                        Button Button = c as Button;
                        if (Button.Text != "X")
                        {
                            IndividalLEDRedTrackBar.Invoke((MethodInvoker)delegate {
                                IndividalLEDGreenTrackBar.Invoke((MethodInvoker)delegate {
                                    IndividalLEDBlueTrackBar.Invoke((MethodInvoker)delegate {
                                        Point3D[] MomentaryDataTag = (Point3D[])Button.Parent.Tag;
                                        Button.BackColor = Color.FromArgb(IndividalLEDRedTrackBar.Value, IndividalLEDGreenTrackBar.Value, IndividalLEDBlueTrackBar.Value);
                                        Color AfterShuffel = ShuffleColors(Color.FromArgb(IndividalLEDRedTrackBar.Value, IndividalLEDGreenTrackBar.Value, IndividalLEDBlueTrackBar.Value));
                                        string SerialOut = "4;" + MomentaryDataTag[1].Z + ";" + Button.Text + ";" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B;
                                        SendDataBySerial(SerialOut);
                                    });
                                });
                            });
                            await Task.Delay(10);
                        }
                    }
                }
            });
        }

        private void IndividalLEDRedTrackBar_Scroll(object sender, EventArgs e)
        {
            IndividalLEDRedLabel.Text = IndividalLEDRedTrackBar.Value.ToString();
        }

        private void IndividalLEDGreenTrackBar_Scroll(object sender, EventArgs e)
        {
            IndividalLEDGreenLabel.Text = IndividalLEDGreenTrackBar.Value.ToString();
        }

        private void IndividalLEDBlueTrackBar_Scroll(object sender, EventArgs e)
        {
            IndividalLEDBlueLabel.Text = IndividalLEDBlueTrackBar.Value.ToString();
        }

        private void IndividualLEDWorkingPanel_MouseDown(object sender, MouseEventArgs e)
        {
            WorkingPanelMouseDown(sender);
        }
        private void IndividualLEDWorkingPanel_MouseUp(object sender, MouseEventArgs e)
        {
            WorkingPanelMouseUp(sender);
        }

        private void IndividualLEDWorkingPanel_MouseMove(object sender, MouseEventArgs e)
        {
            WorkingPanelMouseMove(sender);
        }

        #endregion

        #region Instructions Region

        private async void InstructionStartLoopButton_Click(object sender, EventArgs e)
        {
            if (!InstructionsRunning)
            {
                ContinueInstructionsLoop = true;
                await RunInstructions();
            }
        }

        private void InstructionsAddDelayButton_Click(object sender, EventArgs e)
        {
            MakeInstructionsInvisable();
            InstructionsAddDelayPanel.Visible = true;
        }

        private void InstructionsAddFadeColorsButton_Click(object sender, EventArgs e)
        {
            MakeInstructionsInvisable();
            InstructionsAddFadeColorsPanel.Visible = true;
        }

        private void InstructionsAddIndividualLEDButton_Click(object sender, EventArgs e)
        {
            MakeInstructionsInvisable();
            InstructionsAddIndividualLEDPanel.Visible = true;
        }

        private void InstructionsAddVisualizerButton_Click(object sender, EventArgs e)
        {
            MakeInstructionsInvisable();
            InstructionsAddVisualizerPanel.Visible = true;
        }

        private void InstructionsAddAmbilightButton_Click(object sender, EventArgs e)
        {
            MakeInstructionsInvisable();
            InstructionsAddAmbilightPanel.Visible = true;
        }

        void MakeInstructionsInvisable()
        {
            InstructionsAddDelayPanel.Visible = false;
            InstructionsAddFadeColorsPanel.Visible = false;
            InstructionsAddVisualizerPanel.Visible = false;
            InstructionsAddIndividualLEDPanel.Visible = false;
            InstructionsAddAmbilightPanel.Visible = false;
        }

        private void InstructionsAddDelayAddButton_Click(object sender, EventArgs e)
        {
            IntructionsList.Add("Delay;" + InstructionsAddDelayNumericUpDown.Value);
            RestructureInstructions();
        }

        private void InstructionsAddFadeColorsAddButton_Click(object sender, EventArgs e)
        {
            IntructionsList.Add("Fade Colors;" + InstructionsAddFadeColorsFromSeriesIDNumericUpDown.Value + ";" + InstructionsAddFadeColorsToSeriesIDNumericUpDown.Value + ";" + InstructionsAddFadeColorsRedTrackBar.Value.ToString() + ";" + InstructionsAddFadeColorsGreenTrackBar.Value.ToString() + ";" + InstructionsAddFadeColorsBlueTrackBar.Value.ToString() + ";" + InstructionsAddFadeColorsFadeSpeedNumericUpDown.Value.ToString() + ";" + InstructionsAddFadeColorsFadeFactorNumericUpDown.Value.ToString());
            RestructureInstructions();
        }

        private void InstructionsAddIndividualLEDAddButton_Click(object sender, EventArgs e)
        {
            IntructionsList.Add("Individual LED;" + InstructionsAddIndividualLEDOnPinNumericUpDown.Value + ";" + InstructionsAddIndividualLEDAtIdNumericUpDown.Value + ";" + InstructionsAddIndividualLEDRedTrackBar.Value + ";" + InstructionsAddIndividualLEDGreenTrackBar.Value.ToString() + ";" + InstructionsAddIndividualLEDBlueTrackBar.Value.ToString());
            RestructureInstructions();
        }

        private void InstructionsAddVisualizerPanelAdd_Click(object sender, EventArgs e)
        {
            IntructionsList.Add("Visualizer;" + InstructionsAddVisualizerStopVisualizerCheckBox.Checked + ";" + (string)InstructionsAddVisualizerLoadSetup.Tag);
            RestructureInstructions();
        }

        private void InstructionsAddAmbilightAddButton_Click(object sender, EventArgs e)
        {
            IntructionsList.Add("Ambilight;" + InstructionsAddAmbilightStopAmbilightCheckBox.Checked + ";" + InstructionsAddAmbilightShowHideBlocksCheckBox.Checked + ";" + InstructionsAddAmbilightAutoSetOffsetsCheckBox.Checked + ";" + (string)InstructionsAddAmbilightUseAmbilightSettings.Tag);
            RestructureInstructions();
        }

        private void InstructionsAddFadeColorsRedTrackBar_Scroll(object sender, EventArgs e)
        {
            InstructionsAddFadeColorsRedLabel.Text = InstructionsAddFadeColorsRedTrackBar.Value.ToString();
        }

        private void InstructionsAddFadeColorsGreenTrackBar_Scroll(object sender, EventArgs e)
        {
            InstructionsAddFadeColorsGreenLabel.Text = InstructionsAddFadeColorsGreenTrackBar.Value.ToString();
        }

        private void InstructionsAddFadeColorsBlueTrackBar_Scroll(object sender, EventArgs e)
        {
            InstructionsAddFadeColorsBlueLabel.Text = InstructionsAddFadeColorsBlueTrackBar.Value.ToString();
        }

        private void InstructionsAddIndividualLEDRedTrackBar_Scroll(object sender, EventArgs e)
        {
            InstructionsAddIndividualLEDRedValueLabel.Text = InstructionsAddIndividualLEDRedTrackBar.Value.ToString();
        }

        private void InstructionsAddIndividualLEDGreenTrackBar_Scroll(object sender, EventArgs e)
        {
            InstructionsAddIndividualLEDGreenValueLabel.Text = InstructionsAddIndividualLEDGreenTrackBar.Value.ToString();
        }

        private void InstructionsAddIndividualLEDBlueTrackBar_Scroll(object sender, EventArgs e)
        {
            InstructionsAddIndividualLEDBlueValueLabel.Text = InstructionsAddIndividualLEDBlueTrackBar.Value.ToString();
        }

        private void InstructionsAddVisualizerLoadSetup_Click(object sender, EventArgs e)
        {
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\VisualizerSettings";
            if (LoadFileDialog.ShowDialog() == DialogResult.OK)
            {
                Button SenderButton = sender as Button;
                SenderButton.Tag = LoadFileDialog.FileName.Split('\\')[LoadFileDialog.FileName.Split('\\').Length - 1];
            }
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private void InstructionsAddAmbilightUseAmbilightSettings_Click(object sender, EventArgs e)
        {
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\AmbilightSettings";
            if (LoadFileDialog.ShowDialog() == DialogResult.OK)
            {
                Button SenderButton = sender as Button;
                SenderButton.Tag = LoadFileDialog.FileName.Split('\\')[LoadFileDialog.FileName.Split('\\').Length - 1];
            }
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        void RestructureInstructions()
        {
            InstructionsWorkingPanel.Controls.Clear();
            for (int i = 0; i < IntructionsList.Count; i++)
            {
                MakeInstructionPanel(IntructionsList[i],i);
            }
        }

        void MakeInstructionPanel(string _Input, int _ID)
        {
            Panel BackPanel = new Panel();
            BackPanel.Location = new Point(Margins, InstructionsWorkingPanel.Controls.Count * BoxHeight + 2 * Margins);
            BackPanel.Height = BoxHeight;
            BackPanel.Width = InstructionsWorkingPanel.Width - 2 * Margins - ScroolBarWidth;
            BackPanel.BorderStyle = BorderStyle.FixedSingle;
            BackPanel.BackColor = Color.White;
            BackPanel.Tag = _Input;
            BackPanel.Font = new Font(BackPanel.Font.FontFamily, BackPanel.Font.Size);
            BackPanel.BackColor = Color.White;

            Button RemovePanelButton = new Button();
            RemovePanelButton.Tag = _ID;
            RemovePanelButton.Text = "X";
            RemovePanelButton.Width = ButtonWidth;
            RemovePanelButton.Height = ButtonHeight * 2;
            RemovePanelButton.Location = new Point(BackPanel.Width - ButtonWidth - Margins, Margins);
            RemovePanelButton.Parent = BackPanel;
            RemovePanelButton.Click += RemoveInstruction;
            RemovePanelButton.BackColor = Color.DarkGray;
            RemovePanelButton.ForeColor = Color.White;
            RemovePanelButton.Name = "InstructionsInstructionPanelRemoveButton";

            BackPanel.Controls.Add(RemovePanelButton);

            Button MoveUpButton = new Button();
            MoveUpButton.Tag = _ID;
            MoveUpButton.Text = "^";
            MoveUpButton.Width = ButtonWidth;
            MoveUpButton.Height = ButtonHeight;
            MoveUpButton.Click += MoveInstructionUp;
            MoveUpButton.Location = new Point(BackPanel.Width - 2 * ButtonWidth - 3 * Margins, 0);
            MoveUpButton.BackColor = Color.DarkGray;
            MoveUpButton.ForeColor = Color.White;
            MoveUpButton.Name = "InstructionsInstructionPanelMoveUpButton";

            BackPanel.Controls.Add(MoveUpButton);

            Button MoveDownButton = new Button();
            MoveDownButton.Tag = _ID;
            MoveDownButton.Text = "v";
            MoveDownButton.Width = ButtonWidth;
            MoveDownButton.Height = ButtonHeight;
            MoveDownButton.Click += MoveInstructionDown;
            MoveDownButton.Location = new Point(BackPanel.Width - 2 * ButtonWidth - 3 * Margins, BackPanel.Height - ButtonHeight - Margins);
            MoveDownButton.BackColor = Color.DarkGray;
            MoveDownButton.ForeColor = Color.White;
            MoveDownButton.Name = "InstructionsInstructionPanelMoveDownButton";

            BackPanel.Controls.Add(MoveDownButton);

            TextBox InfoTextBox = new TextBox();
            InfoTextBox.Text = _Input.Replace(";"," : ");
            InfoTextBox.Width = BackPanel.Width - ButtonWidth * 2 - 4 * Margins;
            InfoTextBox.Height = BackPanel.Height;
            InfoTextBox.Location = new Point(Margins,Margins);
            InfoTextBox.BorderStyle = BorderStyle.None;
            InfoTextBox.BackColor = Color.DarkGray;
            InfoTextBox.ForeColor = Color.White;
            InfoTextBox.ReadOnly = true;
            InfoTextBox.Name = "InstructionsInstructionPanelInfoTextBox";

            BackPanel.Controls.Add(InfoTextBox);

            InstructionsWorkingPanel.Controls.Add(BackPanel);
        }

        private void RemoveInstruction(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            int ID = (int)SenderButton.Tag;
            IntructionsList.RemoveAt(ID);
            RestructureInstructions();
        }

        private void MoveInstructionUp(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            int ID = (int)SenderButton.Tag;
            if (ID - 1 >= 0)
            {
                string Data = IntructionsList[ID];
                IntructionsList.RemoveAt(ID);
                IntructionsList.Insert(ID - 1, Data);
                RestructureInstructions();
            }
        }

        private void MoveInstructionDown(object sender, EventArgs e)
        {
            Button SenderButton = sender as Button;
            int ID = (int)SenderButton.Tag;
            if (ID + 1 < IntructionsList.Count)
            {
                string Data = IntructionsList[ID];
                IntructionsList.RemoveAt(ID);
                IntructionsList.Insert(ID + 1, Data);
                RestructureInstructions();
            }
        }

        private void SaveInstructions(object sender, EventArgs e)
        {
            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\Instructions";
            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter AutoSaveFile = new StreamWriter(GenerateStreamFromString(Directory.GetCurrentDirectory() + "\\Instructions\\0.txt"), System.Text.Encoding.UTF8))
                {
                    using (StreamWriter SaveFile = new StreamWriter(SaveFileDialog.OpenFile(), System.Text.Encoding.UTF8))
                    {
                        foreach (string c in IntructionsList)
                        {
                            string SerialOut = c;
                            SaveFile.WriteLine(SerialOut);
                            AutoSaveFile.WriteLine(SerialOut);
                        }
                    }
                }
            }
            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private void LoadInstructions(object sender, EventArgs e)
        {
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\Instructions";
            if (LoadFileDialog.ShowDialog() == DialogResult.OK)
            {
                while (InstructionsWorkingPanel.Controls.Count > 0)
                    InstructionsWorkingPanel.Controls[0].Dispose();

                IntructionsList.Clear();

                string[] Lines = File.ReadAllLines(LoadFileDialog.FileName, System.Text.Encoding.UTF8);
                for (int i = 0; i < Lines.Length; i++)
                {
                    IntructionsList.Add(Lines[i]);
                    MakeInstructionPanel(Lines[i], i);
                }
            }
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        void AutoloadLastInstructions()
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\Instructions\\0.txt"))
            {
                while (InstructionsWorkingPanel.Controls.Count > 0)
                    InstructionsWorkingPanel.Controls[0].Dispose();

                IntructionsList.Clear();

                string[] Lines = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\Instructions\\0.txt", System.Text.Encoding.UTF8);
                for (int i = 0; i < Lines.Length; i++)
                {
                    IntructionsList.Add(Lines[i]);
                    MakeInstructionPanel(Lines[i], i);
                }
            }
        }
        public static Stream GenerateStreamFromString(string _Input)
        {
            var Stream = new MemoryStream();
            var Writer = new StreamWriter(Stream);
            Writer.Write(_Input);
            Writer.Flush();
            Stream.Position = 0;
            return Stream;
        }

        public async Task RunInstructions()
        {
            await Task.Run(async () =>
            {
                InstructionsRunning = true;
                while (ContinueInstructionsLoop)
                {
                    for (int i = 0; i < IntructionsList.Count; i++)
                    {
                        if (i == 0)
                        {
                            InstructionsWorkingPanel.Invoke((MethodInvoker)delegate { InstructionsWorkingPanel.Controls[IntructionsList.Count - 1].BackColor = Color.White; });
                        }
                        else
                        {
                            InstructionsWorkingPanel.Invoke((MethodInvoker)delegate { InstructionsWorkingPanel.Controls[i - 1].BackColor = Color.White; });
                        }
                        InstructionsWorkingPanel.Invoke((MethodInvoker)delegate { InstructionsWorkingPanel.Controls[i].BackColor = Color.Gray; });

                        string[] Data = IntructionsList[i].Split(';');
                        if (Data[0] == "Delay")
                        {
                            for (int j = 0; j < Int32.Parse(Data[1]); j += 100)
                            {
                                await Task.Delay(100);
                                if (StopInstructionsLoop)
                                    break;
                            }
                        }
                        if (Data[0] == "Fade Colors")
                        {
                            string SerialOut = "6;" + Data[1] + ";" + Data[2];
                            SendDataBySerial(SerialOut);
                            Color AfterShuffel = ShuffleColors(Color.FromArgb(Int32.Parse(Data[3]), Int32.Parse(Data[4]), Int32.Parse(Data[5])));
                            SerialOut = "1;" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B + ";" + Data[6] + ";" + Math.Round((Convert.ToDecimal(Data[7]) * 100), 0).ToString();
                            SendDataBySerial(SerialOut);
                        }
                        if (Data[0] == "Individual LED")
                        {
                            Color AfterShuffel = ShuffleColors(Color.FromArgb(Int32.Parse(Data[3]), Int32.Parse(Data[4]), Int32.Parse(Data[5])));
                            string SerialOut = "4;" + Data[1] + ";" + Data[2] + ";" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B;
                            SendDataBySerial(SerialOut);
                        }
                        if (Data[0] == "Visualizer")
                        {
                            if (Data[1] == "True")
                            {
                                EnableBASS(false);
                            }
                            else
                            {
                                string SerialOut = "";
                                VisualizerPanel.Invoke((MethodInvoker)delegate {
                                    LoadSettings(Directory.GetCurrentDirectory() + "\\VisualizerSettings\\" + Data[2]);
                                });
                                VisualizerFromSeriesIDNumericUpDown.Invoke((MethodInvoker)delegate {
                                    VisualizerToSeriesIDNumericUpDown.Invoke((MethodInvoker)delegate {
                                        SerialOut = "6;" + VisualizerFromSeriesIDNumericUpDown.Value + ";" + VisualizerToSeriesIDNumericUpDown.Value;
                                    });
                                });
                                SendDataBySerial(SerialOut);
                                VisualizerPanel.Invoke((MethodInvoker)delegate {
                                    EnableBASS(true);
                                });
                            }
                        }
                        if (Data[0] == "Ambilight")
                        {
                            if (Data[1] == "True")
                            {
                                AmbiLightModePanel.Invoke((MethodInvoker)delegate {
                                    StopAmbilight();
                                });
                            }
                            else
                            {
                                if (Data[2] == "True")
                                {
                                    AmbiLightModePanel.Invoke((MethodInvoker)delegate {
                                        ShowOrHideBlocks();
                                    });
                                }
                                else
                                {
                                    if (Data[3] == "True")
                                    {
                                        AmbiLightModePanel.Invoke((MethodInvoker)delegate {
                                            AutoSetOffsets();
                                        });
                                    }
                                    else
                                    {
                                        AmbiLightModePanel.Invoke((MethodInvoker)delegate {
                                            LoadSettings(Directory.GetCurrentDirectory() + "\\AmbilightSettings\\" + Data[4]);
                                            StartAmbilight();
                                        });
                                    }
                                }
                            }
                        }
                        if (StopInstructionsLoop)
                            break;
                    }
                    InstructionsLoopCheckBox.Invoke((MethodInvoker)delegate { ContinueInstructionsLoop = InstructionsLoopCheckBox.Checked; });
                    if (StopInstructionsLoop)
                    {
                        StopInstructionsLoop = false;
                        ContinueInstructionsLoop = false;
                        InstructionsRunning = false;
                    }
                }
                for (int i = 0; i < IntructionsList.Count; i++)
                {
                    InstructionsWorkingPanel.Invoke((MethodInvoker)delegate { InstructionsWorkingPanel.Controls[i].BackColor = Color.White; });
                }
                InstructionsRunning = false;
            });
        }

        private void InstructionStopLoopButton_Click(object sender, EventArgs e)
        {
            if (ContinueInstructionsLoop)
                if (InstructionsRunning)
                    StopInstructionsLoop = true;
        }

        #endregion

        #region Visualizer Section

        private void SmoothnessTrackBar_ValueChanged(object sender, EventArgs e)
        {
            SmoothnessLabel.Text = SmoothnessTrackBar.Value.ToString();
        }

        private void SampleTimeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            SampleTimeLabel.Text = SampleTimeTrackBar.Value.ToString();
        }

        private void SensitivityTrackBar_ValueChanged(object sender, EventArgs e)
        {
            SensitivityLabel.Text = SensitivityTrackBar.Value.ToString();
        }

        private void BeatZoneTriggerHeight_Scroll(object sender, EventArgs e)
        {
            FormatCustomText(BeatZoneTriggerHeight.Value, BeatZoneTriggerHeightLabel, "");
        }

        private void BeatZoneFromTrackBar_Scroll(object sender, EventArgs e)
        {
            if (BeatZoneFromTrackBar.Value < BeatZoneFromTrackBar.Value)
                BeatZoneFromTrackBar.Value = BeatZoneFromTrackBar.Value + 1;
            FormatCustomText(BeatZoneFromTrackBar.Value, BeatZoneFromLabel, "");
        }

        private void BeatZoneToTrackBar_Scroll(object sender, EventArgs e)
        {
            if (BeatZoneToTrackBar.Value < BeatZoneFromTrackBar.Value)
                BeatZoneToTrackBar.Value = BeatZoneFromTrackBar.Value + 1;

            FormatCustomText(BeatZoneToTrackBar.Value, BeatZoneToLabel, "");
        }

        private void TrackBarUpdateBASSKey(object sender, KeyEventArgs e)
        {
            EnableBASS(true);
        }

        private void TrackBarUpdateBASSMouse(object sender, MouseEventArgs e)
        {
            EnableBASS(true);
        }

        private void VisualSamplesNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            BeatZoneFromTrackBar.Maximum = (int)VisualSamplesNumericUpDown.Value;
            BeatZoneToTrackBar.Maximum = (int)VisualSamplesNumericUpDown.Value;
            UpdateSpectrumChart(SpectrumChart, SpectrumRedTextBox.Text, SpectrumGreenTextBox.Text, SpectrumBlueTextBox.Text, (int)VisualSamplesNumericUpDown.Value, SpectrumAutoScaleValuesCheckBox.Checked);
            UpdateSpectrumChart(WaveChart, WaveRedTextBox.Text, WaveGreenTextBox.Text, WaveBlueTextBox.Text, 255 * 3, WaveAutoScaleValuesCheckBox.Checked);
            BeatZoneChart.ChartAreas[0].AxisX.Minimum = 0;
            SpectrumChart.ChartAreas[0].AxisX.Minimum = 0;
            WaveChart.ChartAreas[0].AxisX.Minimum = 0;
            BeatZoneChart.ChartAreas[0].AxisX.Maximum = BeatZoneToTrackBar.Maximum;
            SpectrumChart.ChartAreas[0].AxisX.Maximum = BeatZoneToTrackBar.Maximum;
        }

        private void VisualizationTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SpectrumPanel.Enabled = false;
            WavePanel.Enabled = false;
            FullSpectrumPanel.Enabled = false;
            BeatWaveProgressBar.Value = 0;
            if (VisualizationTypeComboBox.SelectedIndex == 1 | VisualizationTypeComboBox.SelectedIndex == 2)
            {
                SpectrumPanel.Enabled = true;
            }
            if (VisualizationTypeComboBox.SelectedIndex == 3 | VisualizationTypeComboBox.SelectedIndex == 4)
            {
                WavePanel.Enabled = true;
            }
            if (VisualizationTypeComboBox.SelectedIndex == 5)
            {
                FullSpectrumPanel.Enabled = true;
            }

            EnableBASS(true);
        }

        private void AudioSampleRateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableBASS(true);
        }

        private void UpdateSpectrumButton_Click(object sender, EventArgs e)
        {
            if (SpectrumPanel.Enabled)
            {
                UpdateSpectrumChart(SpectrumChart, SpectrumRedTextBox.Text, SpectrumGreenTextBox.Text, SpectrumBlueTextBox.Text, (int)VisualSamplesNumericUpDown.Value, SpectrumAutoScaleValuesCheckBox.Checked);
            }

            EnableBASS(true);
        }

        private void UpdateWaveButton_Click(object sender, EventArgs e)
        {
            if (WavePanel.Enabled)
            {
                UpdateSpectrumChart(WaveChart, WaveRedTextBox.Text, WaveGreenTextBox.Text, WaveBlueTextBox.Text, 255 * 3, WaveAutoScaleValuesCheckBox.Checked);
            }

            EnableBASS(true);
        }

        void UpdateSpectrumChart(Chart _Chart, string _Red, string _Green, string _Blue, int _XValues, bool _AutoScale)
        {
            _Chart.Series.Clear();
            Series SeriesRed = new Series { IsVisibleInLegend = false, IsXValueIndexed = false, ChartType = SeriesChartType.FastLine, Color = Color.Red, Tag = _Red };
            Series SeriesGreen = new Series { IsVisibleInLegend = false, IsXValueIndexed = false, ChartType = SeriesChartType.FastLine, Color = Color.Green, Tag = _Green };
            Series SeriesBlue = new Series { IsVisibleInLegend = false, IsXValueIndexed = false, ChartType = SeriesChartType.FastLine, Color = Color.Blue, Tag = _Blue };
            Series[] AllSeries = { SeriesRed, SeriesGreen, SeriesBlue };
            for (int i = 0; i < _XValues; i++)
            {
                foreach (Series InnerSeries in AllSeries)
                {
                    InnerSeries.Points.Add(0);
                }
            }

            try
            {

                for (int i = 0; i < _XValues; i++)
                {
                    foreach (Series InnerSeries in AllSeries)
                    {
                        if (((string)InnerSeries.Tag).Contains("PW["))
                        {
                            string CurColor = (string)InnerSeries.Tag;
                            List<string> RedInternals = new List<string>();
                            while (CurColor.Contains("PW["))
                            {
                                int StartIndex = CurColor.IndexOf('[');
                                int EndIndex = CurColor.IndexOf(']');
                                RedInternals.Add(CurColor.Substring(StartIndex + 1, EndIndex - StartIndex - 1));
                                CurColor = CurColor.Remove(EndIndex, 1);
                                CurColor = CurColor.Remove(StartIndex - 2, 3);
                            }
                            foreach (string s in RedInternals)
                            {
                                string[] InternalSplit = s.Split(':');
                                if (Int32.Parse(InternalSplit[1]) <= i && Int32.Parse(InternalSplit[2]) >= i)
                                {
                                    double ColorValue = TransformToPoint(InternalSplit[0], i);
                                    if (_AutoScale)
                                    {
                                        if (ColorValue > 255)
                                        {
                                            InnerSeries.Points[i].YValues[0] = 255;
                                        }
                                        else
                                        {
                                            if (ColorValue < 0)
                                                InnerSeries.Points[i].YValues[0] = 0;
                                            else
                                                InnerSeries.Points[i].YValues[0] = ColorValue;
                                        }
                                    }
                                    else
                                        InnerSeries.Points[i].YValues[0] = ColorValue;
                                }
                            }
                        }
                        else
                        {
                            double ColorValue = TransformToPoint((string)InnerSeries.Tag, i);
                            if (_AutoScale)
                            {
                                if (ColorValue > 255)
                                {
                                    InnerSeries.Points[i].YValues[0] = 255;
                                }
                                else
                                {
                                    if (ColorValue < 0)
                                        InnerSeries.Points[i].YValues[0] = 0;
                                    else
                                        InnerSeries.Points[i].YValues[0] = ColorValue;
                                }
                            }
                            else
                                InnerSeries.Points[i].YValues[0] = ColorValue;
                        }
                    }
                }
            } catch { MessageBox.Show("Error in input string"); }

            foreach (Series InnerSeries in AllSeries)
            {
                _Chart.Series.Add(InnerSeries);
            }
        }

        double TransformToPoint(string _InputEquation, int _XValue)
        {
            string TransformedInputString = _InputEquation.ToLower().Replace("x", _XValue.ToString()).Replace(".", ",").Replace(" ", "");
            string[] Split = System.Text.RegularExpressions.Regex.Split(TransformedInputString, @"(?<=[()^*/+-])");

            List<string> EquationParts = new List<string>();
            foreach(string s in Split)
            {
                EquationParts.Add(s);
            }

            if (EquationParts[0] == "-")
            {
                EquationParts[0] = "-" + EquationParts[1];
                EquationParts.RemoveAt(1);
            }
            if (EquationParts[0] == "+")
            {
                EquationParts.RemoveAt(0);
            }

            for (int i = 0; i < EquationParts.Count; i++)
            {
                if (EquationParts[i].Contains("(") && EquationParts[i].Length > 1)
                {
                    EquationParts.Insert(i + 1, EquationParts[i].Replace(EquationParts[i].Replace("(", ""), ""));
                    EquationParts[i] = EquationParts[i].Replace("(", "");
                    i = 0;
                }
                if (EquationParts[i].Contains(")") && EquationParts[i].Length > 1)
                {
                    EquationParts.Insert(i + 1, EquationParts[i].Replace(EquationParts[i].Replace(")", ""), ""));
                    EquationParts[i] = EquationParts[i].Replace(")", "");
                    i = 0;
                }
                if (EquationParts[i].Contains("^") && EquationParts[i].Length > 1)
                {
                    EquationParts.Insert(i + 1, EquationParts[i].Replace(EquationParts[i].Replace("^", ""), ""));
                    EquationParts[i] = EquationParts[i].Replace("^", "");
                    i = 0;
                }
                if (EquationParts[i].Contains("*") && EquationParts[i].Length > 1)
                {
                    EquationParts.Insert(i + 1, EquationParts[i].Replace(EquationParts[i].Replace("*", ""), ""));
                    EquationParts[i] = EquationParts[i].Replace("*", "");
                    i = 0;
                }
                if (EquationParts[i].Contains("/") && EquationParts[i].Length > 1)
                {
                    EquationParts.Insert(i + 1, EquationParts[i].Replace(EquationParts[i].Replace("/", ""), ""));
                    EquationParts[i] = EquationParts[i].Replace("/", "");
                    i = 0;
                }
                if (EquationParts[i].Contains("+") && EquationParts[i].Length > 1)
                {
                    EquationParts.Insert(i + 1, EquationParts[i].Replace(EquationParts[i].Replace("+", ""), ""));
                    EquationParts[i] = EquationParts[i].Replace("+", "");
                    i = 0;
                }
                if (EquationParts[i].Contains("-") && EquationParts[i].Length > 1 && EquationParts[i].IndexOf('-') != 0)
                {
                    EquationParts.Insert(i + 1, EquationParts[i].Replace(EquationParts[i].Replace("-", ""), ""));
                    EquationParts[i] = EquationParts[i].Replace("-", "");
                    i = 0;
                }
            }

            try
            {
                while (EquationParts.Contains("("))
                {
                    int StartIndex = EquationParts.FindIndex(s => s.Equals("("));
                    int EndIndex = EquationParts.FindIndex(s => s.Equals(")"));
                    string ComputeString = "";
                    for (int i = StartIndex + 1; i < EndIndex; i++)
                        ComputeString += EquationParts[i];
                    EquationParts[StartIndex] = TransformToPoint(ComputeString, _XValue).ToString();
                    EquationParts.RemoveRange(StartIndex + 1, EndIndex - StartIndex);
                }
                while (EquationParts.Contains("^"))
                {
                    int Index = EquationParts.FindIndex(s => s.Equals("^"));
                    EquationParts[Index] = (Math.Pow(Convert.ToDouble(EquationParts[Index - 1]), Convert.ToDouble(EquationParts[Index + 1]))).ToString();
                    EquationParts.RemoveAt(Index + 1);
                    EquationParts.RemoveAt(Index - 1);
                }
                while (EquationParts.Contains("*"))
                {
                    int Index = EquationParts.FindIndex(s => s.Equals("*"));
                    EquationParts[Index] = (Convert.ToDecimal(EquationParts[Index - 1]) * Convert.ToDecimal(EquationParts[Index + 1])).ToString();
                    EquationParts.RemoveAt(Index + 1);
                    EquationParts.RemoveAt(Index - 1);
                }
                while (EquationParts.Contains("/"))
                {
                    int Index = EquationParts.FindIndex(s => s.Equals("/"));
                    EquationParts[Index] = (Convert.ToDecimal(EquationParts[Index - 1]) / Convert.ToDecimal(EquationParts[Index + 1])).ToString();
                    EquationParts.RemoveAt(Index + 1);
                    EquationParts.RemoveAt(Index - 1);
                }
                while (EquationParts.Contains("+"))
                {
                    int Index = EquationParts.FindIndex(s => s.Equals("+"));
                    EquationParts[Index] = (Convert.ToDecimal(EquationParts[Index - 1]) + Convert.ToDecimal(EquationParts[Index + 1])).ToString();
                    EquationParts.RemoveAt(Index + 1);
                    EquationParts.RemoveAt(Index - 1);
                }
                while (EquationParts.Contains("-"))
                {
                    int Index = EquationParts.FindIndex(s => s.Equals("-"));
                    EquationParts[Index] = (Convert.ToDecimal(EquationParts[Index - 1]) - Convert.ToDecimal(EquationParts[Index + 1])).ToString();
                    EquationParts.RemoveAt(Index + 1);
                    EquationParts.RemoveAt(Index - 1);
                }

                return Convert.ToDouble(EquationParts[0]);

            }
            catch
            {
                MessageBox.Show("Error in input string");
                return 0;
            }
        }

        void EnableBASS(bool setto)
        {
            if (setto)
            {
                if (ModeSelectrionComboBox.SelectedIndex == 1)
                {
                    if (BassWasapi.BASS_WASAPI_IsStarted())
                        BassWasapi.BASS_WASAPI_Stop(true);

                    BassWasapi.BASS_WASAPI_Free();
                    Bass.BASS_Free();

                    if (VisualizerThread != null)
                    {
                        RunVisualizerThread = false;
                        while (VisualizerThread.Status == TaskStatus.Running)
                        {
                            Thread.Sleep(5);
                            Application.DoEvents();
                        }
                    }

                    BassProcess = new WASAPIPROC(Process);

                    var array = (AudioSourceComboBox.Items[AudioSourceComboBox.SelectedIndex] as string).Split(' ');
                    int devindex = Convert.ToInt32(array[0]);
                    Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);
                    Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                    bool result = BassWasapi.BASS_WASAPI_Init(devindex, 0, 0, BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, BassProcess, IntPtr.Zero);
                    if (!result)
                    {
                        var error = Bass.BASS_ErrorGetCode();
                        MessageBox.Show(error.ToString());
                    }

                    BassWasapi.BASS_WASAPI_Start();

                    RunVisualizerThread = true;

                    VisualizerThread = new Task(delegate { AudioDataThread(); });
                    VisualizerThread.Start();
                }
            }
            else
            {
                if (VisualizerThread != null)
                {
                    RunVisualizerThread = false;
                    while (VisualizerThread.Status == TaskStatus.Running)
                    {
                        Thread.Sleep(5);
                        Application.DoEvents();
                    }
                }

                if (BassWasapi.BASS_WASAPI_IsStarted())
                    BassWasapi.BASS_WASAPI_Stop(true);

                BassWasapi.BASS_WASAPI_Free();
                Bass.BASS_Free();
            }
        }

        private int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }

        void AudioDataThread()
        {
            DateTime VisualizerRPSCounter = new DateTime();
            DateTime CalibrateRefreshRate = new DateTime();
            int VisualizerUpdatesCounter = 0;
            List<List<int>> AudioDataPointStore = new List<List<int>>();
            float[] AudioData = { };

            int VisualSamles = 0;
            int Smoothness = 0;
            int Sensitivity = 0;
            int BASSDataRate = 0;
            int BeatZoneFrom = 0;
            int BeatZoneTo = 0;
            int SelectedIndex = 0;
            int TriggerHeight = 0;
            int SpectrumSplit = 0;
            int RefreshTime = 0;
            Chart SpectrumChartInner = new Chart();
            Chart WaveChartInner = new Chart();

            VisualizerPanel.Invoke((MethodInvoker)delegate {
                VisualSamles = (int)VisualSamplesNumericUpDown.Value;
                Smoothness = SmoothnessTrackBar.Value;
                Sensitivity = SensitivityTrackBar.Value;
                BASSDataRate = Int32.Parse(AudioSampleRateComboBox.SelectedItem.ToString());
                BeatZoneFrom = BeatZoneFromTrackBar.Value;
                BeatZoneTo = BeatZoneToTrackBar.Value;
                AudioData = new float[Int32.Parse(AudioSampleRateComboBox.SelectedItem.ToString())];
                SelectedIndex = VisualizationTypeComboBox.SelectedIndex;
                TriggerHeight = BeatZoneTriggerHeight.Value;
                SpectrumChartInner = SpectrumChart;
                WaveChartInner = WaveChart;
                RefreshTime = SampleTimeTrackBar.Value;
                SpectrumSplit = (int)FullSpectrumNumericUpDown.Value;
                for (int i = 0; i < VisualSamplesNumericUpDown.Value; i++)
                    AudioDataPointStore.Add(new List<int>(new int[Smoothness]));
            });

            while (RunVisualizerThread)
            {
                CalibrateRefreshRate = DateTime.Now;

                BeatZoneTriggerHeight.Invoke((MethodInvoker)delegate { TriggerHeight = BeatZoneTriggerHeight.Value; });

                Series BeatZoneSeries = new Series
                {
                    IsVisibleInLegend = false,
                    IsXValueIndexed = false,
                    ChartType = SeriesChartType.Column,
                    Color = Color.FromArgb(0, 122, 217)
                };

                int ReturnValue = BassWasapi.BASS_WASAPI_GetData(AudioData, (int)(BASSData)Enum.Parse(typeof(BASSData), "BASS_DATA_FFT" + BASSDataRate));
                if (ReturnValue < -1) return;

                int X, Y;
                int B0 = 0;
                for (X = BeatZoneFrom; X < BeatZoneTo; X++)
                {
                    float Peak = 0;
                    int B1 = (int)Math.Pow(2, X * 10.0 / ((int)VisualSamles - 1));
                    if (B1 > 1023) B1 = 1023;
                    if (B1 <= B0) B1 = B0 + 1;
                    for (; B0 < B1; B0++)
                    {
                        if (Peak < AudioData[1 + B0]) Peak = AudioData[1 + B0];
                    }
                    Y = (int)(Math.Sqrt(Peak) * Sensitivity * 255 - 4);
                    if (Y > 255) Y = 255;
                    if (Y < 1) Y = 1;

                    if (X >= BeatZoneFrom)
                    {
                        if (X <= BeatZoneTo)
                        {

                            AudioDataPointStore[X].Add((byte)Y);
                            while (AudioDataPointStore[X].Count > Smoothness)
                                AudioDataPointStore[X].RemoveAt(0);

                            int AverageValue = 0;
                            if (Smoothness > 1)
                            {
                                for (int s = 0; s < Smoothness; s++)
                                {
                                    AverageValue += AudioDataPointStore[X][s];
                                }
                                AverageValue = AverageValue / Smoothness;
                            }
                            else
                            {
                                AverageValue = AudioDataPointStore[X][0];
                            }
                            if (AverageValue > 255)
                                AverageValue = 255;
                            if (AverageValue < 0)
                                AverageValue = 0;

                            BeatZoneSeries.Points.AddXY(X, AverageValue);
                        }
                    }
                }

                if (SelectedIndex == 0)
                {
                    double Hit = 0;
                    for (int i = 0; i < BeatZoneSeries.Points.Count; i++)
                    {
                        if (BeatZoneSeries.Points[i].YValues[0] >= TriggerHeight)
                            Hit++;
                    }
                    double OutValue = Math.Round(Math.Round((Hit / ((double)BeatZoneTo - (double)BeatZoneFrom)), 2) * 100, 0);
                    AutoTrigger((OutValue / 100) * (255 * 3));
                    string SerialOut = "2;" + OutValue.ToString().Replace(',', '.');
                    SendDataBySerial(SerialOut);
                }
                if (SelectedIndex == 1 | SelectedIndex == 2)
                {
                    double EndR = 0;
                    double EndG = 0;
                    double EndB = 0;
                    int CountR = 0;
                    int CountG = 0;
                    int CountB = 0;
                    int Hit = 0;
                    for (int i = 0; i < BeatZoneSeries.Points.Count; i++)
                    {
                        if (BeatZoneSeries.Points[i].YValues[0] >= TriggerHeight)
                        {
                            if (SpectrumChartInner.Series[0].Points[i].YValues[0] <= 255)
                            {
                                if (SpectrumChartInner.Series[0].Points[i].YValues[0] >= 0)
                                {
                                    EndR += SpectrumChartInner.Series[0].Points[i].YValues[0];
                                    CountR++;
                                }
                            }
                            if (SpectrumChartInner.Series[1].Points[i].YValues[0] <= 255)
                            {
                                if (SpectrumChartInner.Series[1].Points[i].YValues[0] >= 0)
                                {
                                    EndG += SpectrumChartInner.Series[1].Points[i].YValues[0];
                                    CountG++;
                                }
                            }
                            if (SpectrumChartInner.Series[2].Points[i].YValues[0] <= 255)
                            {
                                if (SpectrumChartInner.Series[2].Points[i].YValues[0] >= 0)
                                {
                                    EndB += SpectrumChartInner.Series[2].Points[i].YValues[0];
                                    CountB++;
                                }
                            }
                            Hit++;
                        }
                    }

                    AutoTrigger(((float)Hit / ((float)BeatZoneTo - (float)BeatZoneFrom)) * (255 * 3));

                    if (CountR > 0)
                    {
                        EndR = EndR / CountR;
                    }
                    if (CountG > 0)
                    {
                        EndG = EndG / CountG;
                    }
                    if (CountB > 0)
                    {
                        EndB = EndB / CountB;
                    }

                    Color AfterShuffel = ShuffleColors(Color.FromArgb((int)Math.Round(EndR, 0), (int)Math.Round(EndG, 0), (int)Math.Round(EndB, 0)));

                    string SerialOut = "";
                    if (SelectedIndex == 1)
                        SerialOut = "1;" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B + ";0;0";
                    if (SelectedIndex == 2)
                        SerialOut = "3;" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B;
                    SendDataBySerial(SerialOut);
                }
                if (SelectedIndex == 3 | SelectedIndex == 4)
                {
                    int EndR = 0;
                    int EndG = 0;
                    int EndB = 0;
                    int Hit = 0;

                    for (int i = 0; i < BeatZoneSeries.Points.Count; i++)
                    {
                        if (BeatZoneSeries.Points[i].YValues[0] >= TriggerHeight)
                        {
                            Hit++;
                        }
                    }

                    int EndValue = (int)(((float)255 * (float)3) * ((float)Hit / ((float)BeatZoneTo - (float)BeatZoneFrom)));
                    if (EndValue >= 765)
                        EndValue = 764;
                    if (EndValue < 0)
                        EndValue = 0;

                    BeatWaveProgressBar.Invoke((MethodInvoker)delegate { BeatWaveProgressBar.Value = EndValue; });

                    EndR = (int)WaveChartInner.Series[0].Points[EndValue].YValues[0];
                    EndG = (int)WaveChartInner.Series[1].Points[EndValue].YValues[0];
                    EndB = (int)WaveChartInner.Series[2].Points[EndValue].YValues[0];

                    AutoTrigger(((float)Hit / ((float)BeatZoneTo - (float)BeatZoneFrom)) * (255 * 3));

                    if (EndR > 255)
                        EndR = 0;

                    if (EndG > 255)
                        EndG = 0;

                    if (EndB > 255)
                        EndB = 0;

                    if (EndR < 0)
                        EndR = 0;

                    if (EndG < 0)
                        EndG = 0;

                    if (EndB < 0)
                        EndB = 0;

                    Color AfterShuffel = ShuffleColors(Color.FromArgb(EndR, EndG, EndB));

                    string SerialOut = "";
                    if (SelectedIndex == 4)
                        SerialOut = "1;" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B + ";0;0";
                    if (SelectedIndex == 3)
                        SerialOut = "3;" + AfterShuffel.R + ";" + AfterShuffel.G + ";" + AfterShuffel.B + "";
                    SendDataBySerial(SerialOut);
                }
                if (SelectedIndex == 5)
                {
                    int Hit = 0;
                    string SerialOut = "5;" + SpectrumSplit.ToString() + ";";
                    for (int i = 0; i < BeatZoneSeries.Points.Count; i++)
                    {
                        if (BeatZoneSeries.Points[i].YValues[0] >= TriggerHeight)
                        {
                            SerialOut += Math.Round((BeatZoneSeries.Points[i].YValues[0] / 255) * (double)SpectrumSplit, 0) + ";";
                            Hit++;
                        }
                        else
                            SerialOut += "0;";
                    }

                    AutoTrigger(((float)Hit / ((float)BeatZoneTo - (float)BeatZoneFrom)) * (255 * 3));

                    SendDataBySerial(SerialOut);
                }

                VisualizerUpdatesCounter++;
                if ((DateTime.Now - VisualizerRPSCounter).TotalSeconds >= 1)
                {
                    VisualizerRPSLabel.Invoke((MethodInvoker)delegate { VisualizerRPSLabel.Text = "RPS: " + VisualizerUpdatesCounter; });
                    VisualizerUpdatesCounter = 0;
                    VisualizerRPSCounter = DateTime.Now;
                }
                BeatZoneChart.Invoke((MethodInvoker)delegate {
                    BeatZoneChart.Series.Clear();
                    BeatZoneChart.Series.Add(BeatZoneSeries);
                });

                int ExectuionTime = (int)(DateTime.Now - CalibrateRefreshRate).TotalMilliseconds;
                int ActuralRefreshTime = RefreshTime - ExectuionTime;

                if (ActuralRefreshTime < 0)
                    ActuralRefreshTime = 0;

                Thread.Sleep(ActuralRefreshTime);
            }
        }

        void AutoTrigger(double _TriggerValue)
        {
            if (AutoTriggerCheckBox.Checked)
            {
                BeatZoneTriggerHeight.Invoke((MethodInvoker)delegate {
                VisualizerCurrentValueLabel.Text = ((int)(_TriggerValue)).ToString();
                    if (_TriggerValue >= (double)AutoTriggerDecreseAtNumericUpDown.Value)
                    {
                        if (BeatZoneTriggerHeight.Value < AutoTriggerMaxNumericUpDown.Value)
                            BeatZoneTriggerHeight.Value = BeatZoneTriggerHeight.Value + 1;
                    }
                    if (_TriggerValue <= (double)AutoTriggerIncreseAtNumericUpDown.Value)
                    {
                        if (BeatZoneTriggerHeight.Value > AutoTriggerMinNumericUpDown.Value)
                            BeatZoneTriggerHeight.Value = BeatZoneTriggerHeight.Value - 1;
                    }

                    FormatCustomText(BeatZoneTriggerHeight.Value, BeatZoneTriggerHeightLabel, "");
                });
            }
            else
                VisualizerCurrentValueLabel.Invoke((MethodInvoker)delegate { VisualizerCurrentValueLabel.Text = "0"; });
        }

        void FormatCustomText(int _Value, Control _Control, string _Additional)
        {
            if (_Value < 10)
            {
                _Control.Text = _Control.Text.Substring(0, _Control.Text.Length - (3 + _Additional.Length)) + "  " + _Value.ToString() + _Additional;
            }
            else
            {
                if (_Value < 100)
                {
                    _Control.Text = _Control.Text.Substring(0, _Control.Text.Length - (3 + _Additional.Length)) + " " + _Value.ToString() + _Additional;
                }
                else
                {
                    _Control.Text = _Control.Text.Substring(0, _Control.Text.Length - (3 + _Additional.Length)) + _Value.ToString() + _Additional;
                }
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            BeatZoneTriggerHeight.Enabled = !AutoTriggerCheckBox.Checked;
        }

        private void AudioSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AudioSourceComboBox.Visible)
                EnableBASS(true);
        }

        private void VisualizerSaveSettingsButton_Click(object sender, EventArgs e)
        {
            GetAllControls(VisualizerPanel);

            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\VisualizerSettings";
            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings(SaveFileDialog.FileName, "");
            }
            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private void VisualizerLoadSettingsButton_Click(object sender, EventArgs e)
        {
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\VisualizerSettings";
            if (LoadFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadSettings(LoadFileDialog.FileName);
            }
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        void LoadSettings(string _Location)
        {
            string[] Lines = File.ReadAllLines(_Location, System.Text.Encoding.UTF8);
            for (int i = 0; i < Lines.Length; i++)
            {
                try
                {
                    string[] Split = Lines[i].Split(';');
                    if (Split[0] != "")
                    {
                        if (Split[0].ToUpper() == "COMBOBOX")
                        {
                            ComboBox LoadCombobox = Controls.Find(Split[1], true)[0] as ComboBox;
                            LoadCombobox.SelectedIndex = Int32.Parse(Split[2]);
                        }
                        if (Split[0].ToUpper() == "CHECKBOX")
                        {
                            CheckBox LoadCheckBox = Controls.Find(Split[1], true)[0] as CheckBox;
                            LoadCheckBox.Checked = Convert.ToBoolean(Split[2]);
                        }
                        if (Split[0].ToUpper() == "TEXTBOX")
                        {
                            TextBox LoadTextBox = Controls.Find(Split[1], true)[0] as TextBox;
                            LoadTextBox.Text = Split[2];
                        }
                        if (Split[0].ToUpper() == "NUMERICUPDOWN")
                        {
                            NumericUpDown LoadNumericUpDown = Controls.Find(Split[1], true)[0] as NumericUpDown;
                            LoadNumericUpDown.Value = Convert.ToDecimal(Split[2]);
                        }
                        if (Split[0].ToUpper() == "TRACKBAR")
                        {
                            TrackBar LoadTrackBar = Controls.Find(Split[1], true)[0] as TrackBar;
                            LoadTrackBar.Value = Int32.Parse(Split[2]);
                        }
                        if (Split[0].ToUpper() == "SERIALPORT")
                        {
                            SerialPort1.BaudRate = Int32.Parse(Split[1]);
                        }
                    }
                }
                catch { }
            }
            FormatLayout();
        }

        void SaveSettings(string _Location, string _Additional)
        {
            if (File.Exists(_Location))
                File.Delete(_Location);

            using (StreamWriter SaveFile = File.CreateText(_Location))
            {
                string SerialOut;
                if (_Additional != "")
                {
                    SerialOut = _Additional;
                    SaveFile.WriteLine(SerialOut);
                }
                foreach (Control c in ControlList)
                {
                    if (c is ComboBox)
                    {
                        ComboBox SaveComboBox = c as ComboBox;
                        SerialOut = "COMBOBOX;" + SaveComboBox.Name + ";" + SaveComboBox.SelectedIndex;
                        SaveFile.WriteLine(SerialOut);
                        continue;
                    }
                    if (c is CheckBox)
                    {
                        CheckBox SaveCheckBox = c as CheckBox;
                        SerialOut = "CHECKBOX;" + SaveCheckBox.Name + ";" + SaveCheckBox.Checked;
                        SaveFile.WriteLine(SerialOut);
                        continue;
                    }
                    if (c is TextBox)
                    {
                        TextBox SaveTextBox = c as TextBox;
                        SerialOut = "TEXTBOX;" + SaveTextBox.Name + ";" + SaveTextBox.Text;
                        SaveFile.WriteLine(SerialOut);
                        continue;
                    }
                    if (c is NumericUpDown)
                    {
                        NumericUpDown SaveNumericUpDown = c as NumericUpDown;
                        SerialOut = "NUMERICUPDOWN;" + SaveNumericUpDown.Name + ";" + SaveNumericUpDown.Value;
                        SaveFile.WriteLine(SerialOut);
                        continue;
                    }
                    if (c is TrackBar)
                    {
                        TrackBar SaveTrackBar = c as TrackBar;
                        SerialOut = "TRACKBAR;" + SaveTrackBar.Name + ";" + SaveTrackBar.Value;
                        SaveFile.WriteLine(SerialOut);
                        continue;
                    }
                }
            }
            ControlList.Clear();
        }

        void FormatLayout()
        {
            BeatZoneChart.ChartAreas[0].AxisX.Minimum = 0;
            SpectrumChart.ChartAreas[0].AxisX.Minimum = 0;
            BeatZoneChart.ChartAreas[0].AxisX.Maximum = BeatZoneToTrackBar.Maximum;
            SpectrumChart.ChartAreas[0].AxisX.Maximum = BeatZoneToTrackBar.Maximum;

            UpdateSpectrumChart(SpectrumChart, SpectrumRedTextBox.Text, SpectrumGreenTextBox.Text, SpectrumBlueTextBox.Text, (int)VisualSamplesNumericUpDown.Value, SpectrumAutoScaleValuesCheckBox.Checked);
            UpdateSpectrumChart(WaveChart, WaveRedTextBox.Text, WaveGreenTextBox.Text, WaveBlueTextBox.Text, 255 * 3, WaveAutoScaleValuesCheckBox.Checked);

            FadeColorsRedLabel.Text = FadeColorsRedTrackBar.Value.ToString();
            FadeColorsGreenLabel.Text = FadeColorsGreenTrackBar.Value.ToString();
            FadeColorsBlueLabel.Text = FadeColorsBlueTrackBar.Value.ToString();
            FormatCustomText((int)Math.Round(((double)(FadeColorsRedTrackBar.Value + FadeColorsGreenTrackBar.Value + FadeColorsBlueTrackBar.Value) / (double)(3 * 255)) * 100, 0), FadeColorsBrightnessLabel, "%");

            IndividalLEDRedLabel.Text = IndividalLEDRedTrackBar.Value.ToString();
            IndividalLEDGreenLabel.Text = IndividalLEDGreenTrackBar.Value.ToString();
            IndividalLEDBlueLabel.Text = IndividalLEDBlueTrackBar.Value.ToString();

            SmoothnessLabel.Text = SmoothnessTrackBar.Value.ToString();
            SampleTimeLabel.Text = SampleTimeTrackBar.Value.ToString();
            SensitivityLabel.Text = SensitivityTrackBar.Value.ToString();

            FormatCustomText(BeatZoneTriggerHeight.Value, BeatZoneTriggerHeightLabel, "");
            FormatCustomText(BeatZoneFromTrackBar.Value, BeatZoneFromLabel, "");
            FormatCustomText(BeatZoneToTrackBar.Value, BeatZoneToLabel, "");
        }

        private void VisualizerToSeriesIDNumericUpDown_KeyDown(object sender, KeyEventArgs e)
        {
            EnableBASS(false);
            string SerialOut = "6;" + VisualizerFromSeriesIDNumericUpDown.Value + ";" + VisualizerToSeriesIDNumericUpDown.Value;
            SendDataBySerial(SerialOut);
            EnableBASS(true);
        }

        #endregion

        #region Ambilight Sectrion

        private void AmbiLightModeShowHideBlocksButton_Click(object sender, EventArgs e)
        {
            ShowOrHideBlocks();
        }

        void ShowOrHideBlocks()
        {
            if (BlockList.Count == 0)
            {
                if (AmbiLightModeLeftCheckBox.Checked)
                {
                    for (int i = (Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - (int)AmbiLightModeLeftBlockHeightNumericUpDown.Value + (int)AmbiLightModeLeftBlockOffsetYNumericUpDown.Value); i > (int)AmbiLightModeLeftBlockOffsetYNumericUpDown.Value; i -= (int)(AmbiLightModeLeftBlockHeightNumericUpDown.Value + AmbiLightModeLeftBlockSpacingNumericUpDown.Value))
                    {
                        Block NewBlock = new Block();
                        NewBlock.Show();
                        NewBlock.Width = (int)AmbiLightModeLeftBlockWidthNumericUpDown.Value;
                        NewBlock.Height = (int)AmbiLightModeLeftBlockHeightNumericUpDown.Value;
                        NewBlock.Location = new Point(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + (int)AmbiLightModeLeftBlockOffsetXNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + i);
                        BlockList.Add(NewBlock);
                    }
                }
                if (AmbiLightModeTopCheckBox.Checked)
                {
                    for (int i = (int)AmbiLightModeTopBlockOffsetXNumericUpDown.Value; i < (Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - (int)AmbiLightModeTopBlockWidthNumericUpDown.Value); i += (int)(AmbiLightModeTopBlockWidthNumericUpDown.Value + AmbiLightModeTopBlockSpacingNumericUpDown.Value))
                    {
                        Block NewBlock = new Block();
                        NewBlock.Show();
                        NewBlock.Width = (int)AmbiLightModeTopBlockWidthNumericUpDown.Value;
                        NewBlock.Height = (int)AmbiLightModeTopBlockHeightNumericUpDown.Value;
                        NewBlock.Location = new Point(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + i, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + (int)AmbiLightModeTopBlockOffsetYNumericUpDown.Value);
                        BlockList.Add(NewBlock);
                    }
                }
                if (AmbiLightModeRightCheckBox.Checked)
                {
                    for (int i = (int)AmbiLightModeRightBlockOffsetYNumericUpDown.Value; i < Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - (int)AmbiLightModeRightBlockHeightNumericUpDown.Value; i += (int)(AmbiLightModeRightBlockHeightNumericUpDown.Value + AmbiLightModeRightBlockSpacingNumericUpDown.Value))
                    {
                        Block NewBlock = new Block();
                        NewBlock.Show();
                        NewBlock.Width = (int)AmbiLightModeRightBlockWidthNumericUpDown.Value;
                        NewBlock.Height = (int)AmbiLightModeRightBlockHeightNumericUpDown.Value;
                        NewBlock.Location = new Point(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - (int)AmbiLightModeRightBlockOffsetXNumericUpDown.Value - (int)AmbiLightModeRightBlockWidthNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + i);
                        BlockList.Add(NewBlock);
                    }
                }
                if (AmbiLightModeBottomCheckBox.Checked)
                {
                    for (int i = (Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - (int)AmbiLightModeBottomBlockWidthNumericUpDown.Value) + (int)AmbiLightModeBottomBlockOffsetXNumericUpDown.Value; i > (int)AmbiLightModeBottomBlockWidthNumericUpDown.Value; i -= (int)(AmbiLightModeBottomBlockWidthNumericUpDown.Value + AmbiLightModeBottomBlockSpacingNumericUpDown.Value))
                    {
                        Block NewBlock = new Block();
                        NewBlock.Show();
                        NewBlock.Width = (int)AmbiLightModeBottomBlockWidthNumericUpDown.Value;
                        NewBlock.Height = (int)AmbiLightModeBottomBlockHeightNumericUpDown.Value;
                        NewBlock.Location = new Point(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + i, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - (int)AmbiLightModeBottomBlockOffsetYNumericUpDown.Value - (int)AmbiLightModeBottomBlockHeightNumericUpDown.Value);
                        BlockList.Add(NewBlock);
                    }
                }
            }
            else
            {
                foreach (Block b in BlockList)
                    b.Close();
                BlockList.Clear();
            }
        }

        private void AmbiLightModeStartAmbilightButton_Click(object sender, EventArgs e)
        {
            StartAmbilight();
        }

        private void AmbiLightModeStopAmbilightButton_Click(object sender, EventArgs e)
        {
            StopAmbilight();
        }

        private void AmbiLightModeAutosetOffsets_Click(object sender, EventArgs e)
        {
            AutoSetOffsets();
        }

        void AutoSetOffsets()
        {
            Opacity = 0;
            Bitmap Screenshot = new Bitmap(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height, PixelFormat.Format32bppRgb);
            using (Graphics GFXScreenshot = Graphics.FromImage(Screenshot))
            {
                GFXScreenshot.CopyFromScreen(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y, 0, 0, new Size(Screenshot.Width, Screenshot.Height), CopyPixelOperation.SourceCopy);
            }
            Opacity = 1;

            if (AmbiLightModeLeftCheckBox.Checked)
            {
                for (int i = 0; i < Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width / 2; i++)
                {
                    Color Pixel = Screenshot.GetPixel(i, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height / 2);
                    if (Convert.ToInt32(Pixel.R > 5) + Convert.ToInt32(Pixel.G > 5) + Convert.ToInt32(Pixel.B > 5) > 0)
                    {
                        AmbiLightModeLeftBlockOffsetXNumericUpDown.Value = i;
                        break;
                    }
                }
            }

            if (AmbiLightModeTopCheckBox.Checked)
            {
                for (int i = 0; i < Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height / 2; i++)
                {
                    Color Pixel = Screenshot.GetPixel(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width / 2, i);
                    if (Convert.ToInt32(Pixel.R > 5) + Convert.ToInt32(Pixel.G > 5) + Convert.ToInt32(Pixel.B > 5) > 0)
                    {
                        AmbiLightModeTopBlockOffsetYNumericUpDown.Value = i;
                        break;
                    }
                }
            }

            if (AmbiLightModeRightCheckBox.Checked)
            {
                for (int i = Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - 1; i > Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width / 2; i--)
                {
                    Color Pixel = Screenshot.GetPixel(i, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height / 2);
                    if (Convert.ToInt32(Pixel.R > 5) + Convert.ToInt32(Pixel.G > 25) + Convert.ToInt32(Pixel.B > 5) > 0)
                    {
                        AmbiLightModeRightBlockOffsetXNumericUpDown.Value = Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - i;
                        break;
                    }
                }
            }

            if (AmbiLightModeBottomCheckBox.Checked)
            {
                for (int i = Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - 1; i > Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height / 2; i--)
                {
                    Color Pixel = Screenshot.GetPixel(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width / 2, i);
                    if (Convert.ToInt32(Pixel.R > 5) + Convert.ToInt32(Pixel.G > 25) + Convert.ToInt32(Pixel.B > 5) > 0)
                    {
                        AmbiLightModeBottomBlockOffsetYNumericUpDown.Value = Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - i;
                        break;
                    }
                }
            }
            Screenshot.Dispose();

            ShowOrHideBlocks();

            Thread.Sleep(1000);

            ShowOrHideBlocks();
        }

        void StartAmbilight()
        {
            if (AmbilightTask != null)
                if (AmbilightTask.Status == TaskStatus.Running)
                    StopAmbilight();

            int Highest = 0;
            int Lowest = 0;

            if (AmbiLightModeLeftCheckBox.Checked)
                if (AmbiLightModeLeftFromIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeLeftFromIDNumericUpDown.Value;

            if (AmbiLightModeTopCheckBox.Checked)
                if (AmbiLightModeTopFromIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeTopFromIDNumericUpDown.Value;

            if (AmbiLightModeRightCheckBox.Checked)
                if (AmbiLightModeRightFromIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeRightFromIDNumericUpDown.Value;

            if (AmbiLightModeBottomCheckBox.Checked)
                if (AmbiLightModeBottomFromIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeBottomFromIDNumericUpDown.Value;

            if (AmbiLightModeLeftCheckBox.Checked)
                if (AmbiLightModeLeftToIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeLeftToIDNumericUpDown.Value;

            if (AmbiLightModeTopCheckBox.Checked)
                if (AmbiLightModeTopToIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeTopToIDNumericUpDown.Value;

            if (AmbiLightModeRightCheckBox.Checked)
                if (AmbiLightModeRightToIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeRightToIDNumericUpDown.Value;

            if (AmbiLightModeBottomCheckBox.Checked)
                if (AmbiLightModeBottomToIDNumericUpDown.Value < Lowest)
                    Lowest = (int)AmbiLightModeBottomToIDNumericUpDown.Value;

            if (AmbiLightModeLeftCheckBox.Checked)
                if (AmbiLightModeLeftToIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeLeftToIDNumericUpDown.Value;

            if (AmbiLightModeTopCheckBox.Checked)
                if (AmbiLightModeTopToIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeTopToIDNumericUpDown.Value;

            if (AmbiLightModeRightCheckBox.Checked)
                if (AmbiLightModeRightToIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeRightToIDNumericUpDown.Value;

            if (AmbiLightModeBottomCheckBox.Checked)
                if (AmbiLightModeBottomToIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeBottomToIDNumericUpDown.Value;

            if (AmbiLightModeLeftCheckBox.Checked)
                if (AmbiLightModeLeftFromIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeLeftFromIDNumericUpDown.Value;

            if (AmbiLightModeTopCheckBox.Checked)
                if (AmbiLightModeTopFromIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeTopFromIDNumericUpDown.Value;

            if (AmbiLightModeRightCheckBox.Checked)
                if (AmbiLightModeRightFromIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeRightFromIDNumericUpDown.Value;

            if (AmbiLightModeBottomCheckBox.Checked)
                if (AmbiLightModeBottomFromIDNumericUpDown.Value > Highest)
                    Highest = (int)AmbiLightModeBottomFromIDNumericUpDown.Value;

            string SerialOut = "6;" + Lowest + ";" + Highest;
            SendDataBySerial(SerialOut);

            ImageWindowLeft = new Bitmap((int)AmbiLightModeLeftBlockWidthNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height, PixelFormat.Format24bppRgb);
            ImageWindowTop = new Bitmap(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width, (int)AmbiLightModeTopBlockHeightNumericUpDown.Value, PixelFormat.Format24bppRgb);
            ImageWindowRight = new Bitmap((int)AmbiLightModeRightBlockWidthNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height, PixelFormat.Format24bppRgb);
            ImageWindowBottom = new Bitmap(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width, (int)AmbiLightModeBottomBlockHeightNumericUpDown.Value, PixelFormat.Format24bppRgb);

            if (AmbilightColorStore.Count != 4)
            {
                AmbilightColorStore.Clear();
                AmbilightColorStore.Add(new List<List<int>>());
                AmbilightColorStore.Add(new List<List<int>>());
                AmbilightColorStore.Add(new List<List<int>>());
                AmbilightColorStore.Add(new List<List<int>>());
            }

            AmbiLightModeWorkingPanel.Enabled = false;

            AmbilightTask = new Task(delegate { AmbilightThread(); });
            AmbilightTask.Start();

            RunAmbilight = true;
        }

        void StopAmbilight()
        {
            if (AmbilightTask != null)
            {
                RunAmbilight = false;
                while (AmbilightTask.Status == TaskStatus.Running)
                {
                    Thread.Sleep(5);
                    Application.DoEvents();
                }
            }
            AmbiLightModeWorkingPanel.Enabled = true;
        }

        void AmbilightThread()
        {
            DateTime CalibrateRefreshRate = new DateTime();
            int AmbilightSendingStep = 0;
            while (RunAmbilight)
            {
                if (AmbilightSendingStep == 0)
                {
                    CalibrateRefreshRate = DateTime.Now;
                    SerialOutLeftReady = false;
                    SerialOutTopReady = false;
                    SerialOutRightReady = false;
                    SerialOutBottomReady = false;

                    if (AmbiLightModeLeftCheckBox.Checked)
                    {
                        Task.Run(() =>
                        {
                            using (GFXScreenshotLeft = Graphics.FromImage(ImageWindowLeft))
                            {
                                GFXScreenshotLeft.CopyFromScreen(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + (int)AmbiLightModeLeftBlockOffsetXNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + (int)AmbiLightModeLeftBlockOffsetYNumericUpDown.Value, 0, 0, new Size((int)AmbiLightModeLeftBlockWidthNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height), CopyPixelOperation.SourceCopy);
                            }
                            int Count = 0;
                            SerialOutLeft = "7;" + AmbiLightModeLeftFromIDNumericUpDown.Value + ";" + AmbiLightModeLeftToIDNumericUpDown.Value + ";" + AmbiLightModeLeftLEDsPrBlockNumericUpDown.Value + ";";
                            for (int i = Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - (int)AmbiLightModeLeftBlockHeightNumericUpDown.Value; i > 0; i -= (int)(AmbiLightModeLeftBlockHeightNumericUpDown.Value + AmbiLightModeLeftBlockSpacingNumericUpDown.Value))
                            {
                                Color OutPutColor = GetColorOfSection(ImageWindowLeft, (int)AmbiLightModeLeftBlockWidthNumericUpDown.Value, (int)AmbiLightModeLeftBlockHeightNumericUpDown.Value, 0, i);
                                if (AmbiLightModeFadeFactorNumericUpDown.Value != 0)
                                {
                                    if (AmbilightColorStore[0].Count == Count)
                                    {
                                        AmbilightColorStore[0].Add(new List<int>());
                                        AmbilightColorStore[0][Count].Add(OutPutColor.R);
                                        AmbilightColorStore[0][Count].Add(OutPutColor.G);
                                        AmbilightColorStore[0][Count].Add(OutPutColor.B);
                                    }
                                    else
                                    {
                                        AmbilightColorStore[0][Count][0] = AmbilightColorStore[0][Count][0] + (int)(((double)OutPutColor.R - (double)AmbilightColorStore[0][Count][0]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[0][Count][0] > 255)
                                            AmbilightColorStore[0][Count][0] = 255;
                                        if (AmbilightColorStore[0][Count][0] < 0)
                                            AmbilightColorStore[0][Count][0] = 0;
                                        AmbilightColorStore[0][Count][1] = AmbilightColorStore[0][Count][1] + (int)(((double)OutPutColor.G - (double)AmbilightColorStore[0][Count][1]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[0][Count][1] > 255)
                                            AmbilightColorStore[0][Count][1] = 255;
                                        if (AmbilightColorStore[0][Count][1] < 0)
                                            AmbilightColorStore[0][Count][1] = 0;
                                        AmbilightColorStore[0][Count][2] = AmbilightColorStore[0][Count][2] + (int)(((double)OutPutColor.B - (double)AmbilightColorStore[0][Count][2]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[0][Count][2] > 255)
                                            AmbilightColorStore[0][Count][2] = 255;
                                        if (AmbilightColorStore[0][Count][2] < 0)
                                            AmbilightColorStore[0][Count][2] = 0;
                                    }
                                    OutPutColor = GammaCorrection(Color.FromArgb(AmbilightColorStore[0][Count][0], AmbilightColorStore[0][Count][1], AmbilightColorStore[0][Count][2]));
                                }
                                else
                                {
                                    OutPutColor = GammaCorrection(OutPutColor);
                                }
                                Color AfterShuffel = ShuffleColors(OutPutColor);
                                SerialOutLeft += Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.R + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.G + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.B + 1)), 0) + ";";
                                Count++;
                            }
                            SerialOutLeftReady = true;
                        });
                    }
                    else
                        SerialOutLeftReady = true;

                    if (AmbiLightModeTopCheckBox.Checked)
                    {
                        Task.Run(() =>
                        {
                            using (GFXScreenshotTop = Graphics.FromImage(ImageWindowTop))
                            {
                                GFXScreenshotTop.CopyFromScreen(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + (int)AmbiLightModeTopBlockOffsetXNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + (int)AmbiLightModeTopBlockOffsetYNumericUpDown.Value, 0, 0, new Size(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width, (int)AmbiLightModeTopBlockHeightNumericUpDown.Value), CopyPixelOperation.SourceCopy);
                            }
                            int Count = 0;
                            SerialOutTop = "7;" + AmbiLightModeTopFromIDNumericUpDown.Value + ";" + AmbiLightModeTopToIDNumericUpDown.Value + ";" + AmbiLightModeTopLEDsPrBlockNumericUpDown.Value + ";";
                            for (int i = 0; i < (Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - (int)AmbiLightModeTopBlockWidthNumericUpDown.Value); i += (int)(AmbiLightModeTopBlockWidthNumericUpDown.Value + AmbiLightModeTopBlockSpacingNumericUpDown.Value))
                            {
                                Color OutPutColor = GetColorOfSection(ImageWindowTop, (int)AmbiLightModeTopBlockWidthNumericUpDown.Value, (int)AmbiLightModeTopBlockHeightNumericUpDown.Value, i, 0);
                                if (AmbiLightModeFadeFactorNumericUpDown.Value != 0)
                                {
                                    if (AmbilightColorStore[1].Count <= Count)
                                    {
                                        AmbilightColorStore[1].Add(new List<int>());
                                        AmbilightColorStore[1][Count].Add(OutPutColor.R);
                                        AmbilightColorStore[1][Count].Add(OutPutColor.G);
                                        AmbilightColorStore[1][Count].Add(OutPutColor.B);
                                    }
                                    else
                                    {
                                        AmbilightColorStore[1][Count][0] = AmbilightColorStore[1][Count][0] + (int)(((double)OutPutColor.R - (double)AmbilightColorStore[1][Count][0]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[1][Count][0] > 255)
                                            AmbilightColorStore[1][Count][0] = 255;
                                        if (AmbilightColorStore[1][Count][0] < 0)
                                            AmbilightColorStore[1][Count][0] = 0;
                                        AmbilightColorStore[1][Count][1] = AmbilightColorStore[1][Count][1] + (int)(((double)OutPutColor.G - (double)AmbilightColorStore[1][Count][1]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[1][Count][1] > 255)
                                            AmbilightColorStore[1][Count][1] = 255;
                                        if (AmbilightColorStore[1][Count][1] < 0)
                                            AmbilightColorStore[1][Count][1] = 0;
                                        AmbilightColorStore[1][Count][2] = AmbilightColorStore[1][Count][2] + (int)(((double)OutPutColor.B - (double)AmbilightColorStore[1][Count][2]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[1][Count][2] > 255)
                                            AmbilightColorStore[1][Count][2] = 255;
                                        if (AmbilightColorStore[1][Count][2] < 0)
                                            AmbilightColorStore[1][Count][2] = 0;
                                    }
                                    OutPutColor = GammaCorrection(Color.FromArgb(AmbilightColorStore[1][Count][0], AmbilightColorStore[1][Count][1], AmbilightColorStore[1][Count][2]));
                                }
                                else
                                {
                                    OutPutColor = GammaCorrection(OutPutColor);
                                }
                                Color AfterShuffel = ShuffleColors(OutPutColor);
                                SerialOutTop += Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.R + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.G + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.B + 1)), 0) + ";";
                                Count++;
                            }
                            SerialOutTopReady = true;
                        });
                    }
                    else
                        SerialOutTopReady = true;

                    if (AmbiLightModeRightCheckBox.Checked)
                    {
                        Task.Run(() =>
                        {
                            using (GFXScreenshotRight = Graphics.FromImage(ImageWindowRight))
                            {
                                GFXScreenshotRight.CopyFromScreen((Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - (int)AmbiLightModeRightBlockWidthNumericUpDown.Value) + (int)AmbiLightModeRightBlockOffsetXNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + (int)AmbiLightModeRightBlockOffsetYNumericUpDown.Value, 0, 0, new Size((int)AmbiLightModeRightBlockWidthNumericUpDown.Value, Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height), CopyPixelOperation.SourceCopy);
                            }
                            int Count = 0;
                            SerialOutRight = "7;" + AmbiLightModeRightFromIDNumericUpDown.Value + ";" + AmbiLightModeRightToIDNumericUpDown.Value + ";" + AmbiLightModeRightLEDsPrBlockNumericUpDown.Value + ";";
                            for (int i = 0; i < Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - (int)AmbiLightModeRightBlockHeightNumericUpDown.Value; i += (int)(AmbiLightModeRightBlockHeightNumericUpDown.Value + AmbiLightModeRightBlockSpacingNumericUpDown.Value))
                            {
                                Color OutPutColor = GetColorOfSection(ImageWindowRight, (int)AmbiLightModeRightBlockWidthNumericUpDown.Value, (int)AmbiLightModeRightBlockHeightNumericUpDown.Value, 0, i);
                                if (AmbiLightModeFadeFactorNumericUpDown.Value != 0)
                                {
                                    if (AmbilightColorStore[2].Count <= Count)
                                    {
                                        AmbilightColorStore[2].Add(new List<int>());
                                        AmbilightColorStore[2][Count].Add(OutPutColor.R);
                                        AmbilightColorStore[2][Count].Add(OutPutColor.G);
                                        AmbilightColorStore[2][Count].Add(OutPutColor.B);
                                    }
                                    else
                                    {
                                        AmbilightColorStore[2][Count][0] = AmbilightColorStore[2][Count][0] + (int)(((double)OutPutColor.R - (double)AmbilightColorStore[2][Count][0]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[2][Count][0] > 255)
                                            AmbilightColorStore[2][Count][0] = 255;
                                        if (AmbilightColorStore[2][Count][0] < 0)
                                            AmbilightColorStore[2][Count][0] = 0;
                                        AmbilightColorStore[2][Count][1] = AmbilightColorStore[2][Count][1] + (int)(((double)OutPutColor.G - (double)AmbilightColorStore[2][Count][1]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[2][Count][1] > 255)
                                            AmbilightColorStore[2][Count][1] = 255;
                                        if (AmbilightColorStore[2][Count][1] < 0)
                                            AmbilightColorStore[2][Count][1] = 0;
                                        AmbilightColorStore[2][Count][2] = AmbilightColorStore[2][Count][2] + (int)(((double)OutPutColor.B - (double)AmbilightColorStore[2][Count][2]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[2][Count][2] > 255)
                                            AmbilightColorStore[2][Count][2] = 255;
                                        if (AmbilightColorStore[2][Count][2] < 0)
                                            AmbilightColorStore[2][Count][2] = 0;
                                    }
                                    OutPutColor = GammaCorrection(Color.FromArgb(AmbilightColorStore[2][Count][0], AmbilightColorStore[2][Count][1], AmbilightColorStore[2][Count][2]));
                                }
                                else
                                {
                                    OutPutColor = GammaCorrection(OutPutColor);
                                }
                                Color AfterShuffel = ShuffleColors(OutPutColor);
                                SerialOutRight += Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.R + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.G + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.B + 1)), 0) + ";";
                                Count++;
                            }
                            SerialOutRightReady = true;
                        });
                    }
                    else
                        SerialOutRightReady = true;

                    if (AmbiLightModeBottomCheckBox.Checked)
                    {
                        Task.Run(() =>
                        {
                            using (GFXScreenshotBottom = Graphics.FromImage(ImageWindowBottom))
                            {
                                GFXScreenshotBottom.CopyFromScreen(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.X + (int)AmbiLightModeBottomBlockOffsetXNumericUpDown.Value, (Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Y + Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Height - (int)AmbiLightModeBottomBlockWidthNumericUpDown.Value) + (int)AmbiLightModeBottomBlockOffsetYNumericUpDown.Value, 0, 0, new Size(Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width, (int)AmbiLightModeBottomBlockHeightNumericUpDown.Value), CopyPixelOperation.SourceCopy);
                            }
                            int Count = 0;
                            SerialOutBottom = "7;" + AmbiLightModeBottomFromIDNumericUpDown.Value + ";" + AmbiLightModeBottomToIDNumericUpDown.Value + ";" + AmbiLightModeBottomLEDsPrBlockNumericUpDown.Value + ";";
                            for (int i = (Screen.AllScreens[(int)AmbiLightModeScreenIDNumericUpDown.Value].Bounds.Width - (int)AmbiLightModeBottomBlockWidthNumericUpDown.Value); i > (int)AmbiLightModeBottomBlockWidthNumericUpDown.Value; i -= (int)(AmbiLightModeBottomBlockWidthNumericUpDown.Value + AmbiLightModeBottomBlockSpacingNumericUpDown.Value))
                            {
                                Color OutPutColor = GetColorOfSection(ImageWindowBottom, (int)AmbiLightModeBottomBlockWidthNumericUpDown.Value, (int)AmbiLightModeBottomBlockHeightNumericUpDown.Value, i, 0);
                                if (AmbiLightModeFadeFactorNumericUpDown.Value != 0)
                                {
                                    if (AmbilightColorStore[3].Count <= Count)
                                    {
                                        AmbilightColorStore[3].Add(new List<int>());
                                        AmbilightColorStore[3][Count].Add(OutPutColor.R);
                                        AmbilightColorStore[3][Count].Add(OutPutColor.G);
                                        AmbilightColorStore[3][Count].Add(OutPutColor.B);
                                    }
                                    else
                                    {
                                        AmbilightColorStore[3][Count][0] = AmbilightColorStore[3][Count][0] + (int)(((double)OutPutColor.R - (double)AmbilightColorStore[3][Count][0]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[3][Count][0] > 255)
                                            AmbilightColorStore[3][Count][0] = 255;
                                        if (AmbilightColorStore[3][Count][0] < 0)
                                            AmbilightColorStore[3][Count][0] = 0;
                                        AmbilightColorStore[3][Count][1] = AmbilightColorStore[3][Count][1] + (int)(((double)OutPutColor.G - (double)AmbilightColorStore[3][Count][1]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[3][Count][1] > 255)
                                            AmbilightColorStore[3][Count][1] = 255;
                                        if (AmbilightColorStore[3][Count][1] < 0)
                                            AmbilightColorStore[3][Count][1] = 0;
                                        AmbilightColorStore[3][Count][2] = AmbilightColorStore[3][Count][2] + (int)(((double)OutPutColor.B - (double)AmbilightColorStore[3][Count][2]) * (double)AmbiLightModeFadeFactorNumericUpDown.Value);
                                        if (AmbilightColorStore[3][Count][2] > 255)
                                            AmbilightColorStore[3][Count][2] = 255;
                                        if (AmbilightColorStore[3][Count][2] < 0)
                                            AmbilightColorStore[3][Count][2] = 0;
                                    }
                                    OutPutColor = GammaCorrection(Color.FromArgb(AmbilightColorStore[3][Count][0], AmbilightColorStore[3][Count][1], AmbilightColorStore[3][Count][2]));
                                }
                                else
                                {
                                    OutPutColor = GammaCorrection(OutPutColor);
                                }
                                Color AfterShuffel = ShuffleColors(OutPutColor);
                                SerialOutBottom += Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.R + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.G + 1)), 0) + ";" + Math.Round((decimal)9 / ((decimal)255 / (AfterShuffel.B + 1)), 0) + ";";
                                Count++;
                            }
                            SerialOutBottomReady = true;
                        });
                    }
                    else
                        SerialOutBottomReady = true;

                    while (Convert.ToInt32(SerialOutLeftReady) + Convert.ToInt32(SerialOutTopReady) + Convert.ToInt32(SerialOutRightReady) + Convert.ToInt32(SerialOutBottomReady) < 4)
                    {
                        Thread.Sleep(5);
                    }
                }
                if (AmbilightSendingStep == 1)
                {
                    if (AmbiLightModeLeftCheckBox.Checked)
                    {
                        SendDataBySerial(SerialOutLeft);
                    }
                }
                if (AmbilightSendingStep == 2)
                {
                    if (AmbiLightModeTopCheckBox.Checked)
                    { 
                        SendDataBySerial(SerialOutTop);
                    }
                }
                if (AmbilightSendingStep == 3)
                {
                    if (AmbiLightModeRightCheckBox.Checked)
                    { 
                        SendDataBySerial(SerialOutRight);
                    }
                }
                if (AmbilightSendingStep == 4)
                {
                    if (AmbiLightModeBottomCheckBox.Checked)
                        SendDataBySerial(SerialOutBottom);

                    AmbilightSendingStep = -1;

                    AmbilightFPSCounterFramesRendered++;

                    if ((DateTime.Now - AmbilightFPSCounter).TotalSeconds >= 1)
                    {
                        AmbilightModeFPSCounterLabel.Invoke((MethodInvoker)delegate { AmbilightModeFPSCounterLabel.Text = "FPS: " + AmbilightFPSCounterFramesRendered; });
                        AmbilightFPSCounterFramesRendered = 0;
                        AmbilightFPSCounter = DateTime.Now;
                    }

                    int ExectuionTime = (int)(DateTime.Now - CalibrateRefreshRate).TotalMilliseconds;
                    int ActuralRefreshTime = (int)AmbiLightModeRefreshRateNumericUpDown.Value - ExectuionTime;

                    if (ActuralRefreshTime < 0)
                        ActuralRefreshTime = 0;

                    Thread.Sleep(ActuralRefreshTime);
                }

                AmbilightSendingStep++;
            }
        }

        Color GetColorOfSection(Bitmap _InputImage, int _Width, int _Height, int _Xpos, int _Ypos)
        {
            int Count = 0;
            int AvgR = 0;
            int AvgG = 0;
            int AvgB = 0;

            for (int y = _Ypos; y < _Ypos + _Height; y += (int)AmbiLightModeBlockSampleSplitNumericUpDown.Value)
            {
                for (int x = _Xpos; x < _Xpos + _Width; x += (int)AmbiLightModeBlockSampleSplitNumericUpDown.Value)
                {
                    Color Pixel = _InputImage.GetPixel(x, y);
                    AvgR += Pixel.R;
                    AvgG += Pixel.G;
                    AvgB += Pixel.B;
                    Count++;
                }
            }

            AvgR = AvgR / Count;
            AvgG = AvgG / Count;
            AvgB = AvgB / Count;

            return Color.FromArgb(AvgR, AvgG, AvgB);
        }

        private void AmbiLightModeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AmbiLightModeLeftPanel.Enabled = false;
            AmbiLightModeTopPanel.Enabled = false;
            AmbiLightModeRightPanel.Enabled = false;
            AmbiLightModeBottomPanel.Enabled = false;

            if (AmbiLightModeLeftCheckBox.Checked)
                AmbiLightModeLeftPanel.Enabled = true;

            if (AmbiLightModeTopCheckBox.Checked)
                AmbiLightModeTopPanel.Enabled = true;

            if (AmbiLightModeRightCheckBox.Checked)
                AmbiLightModeRightPanel.Enabled = true;

            if (AmbiLightModeBottomCheckBox.Checked)
                AmbiLightModeBottomPanel.Enabled = true;
        }

        Color GammaCorrection(Color _InputColor)
        {
            int OutColorR = (int)(Math.Pow((float)_InputColor.R / (float)255, (double)AmbiLightModeGammaFactorNumericUpDown.Value) * 255 + 0.5);
            if (OutColorR > 255)
                OutColorR = 0;
            if (OutColorR < 0)
                OutColorR = 0;

            int OutColorG = (int)(Math.Pow((float)_InputColor.G / (float)255, (double)AmbiLightModeGammaFactorNumericUpDown.Value) * 255 + 0.5);
            if (OutColorG > 255)
                OutColorG = 0;
            if (OutColorG < 0)
                OutColorG = 0;

            int OutColorB = (int)(Math.Pow((float)_InputColor.B / (float)255, (double)AmbiLightModeGammaFactorNumericUpDown.Value) * 255 + 0.5);
            if (OutColorB > 255)
                OutColorB = 0;
            if (OutColorB < 0)
                OutColorB = 0;

            return Color.FromArgb(OutColorR, OutColorG, OutColorB);
        }

        private void LoadAAmbilightSetup(object sender, EventArgs e)
        {
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\AmbilightSettings";
            if (LoadFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadSettings(LoadFileDialog.FileName);
            }
            LoadFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private void SaveCurrentAmbilightSetup(object sender, EventArgs e)
        {
            GetAllControls(AmbiLightModePanel);

            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory() + "\\AmbilightSettings";
            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings(SaveFileDialog.FileName, "");
            }
            SaveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
        }

        #endregion
    }
}
