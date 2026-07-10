using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("AutoClicker")]
[assembly: AssemblyDescription("A lightweight precision auto-clicker for Windows")]
[assembly: AssemblyCompany("Chris Arlington")]
[assembly: AssemblyProduct("AutoClicker")]
[assembly: AssemblyCopyright("Copyright © 2026 Chris Arlington")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace AutoClicker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.Run(new MainForm());
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }

    static class Theme
    {
        public static readonly Color Window = Color.FromArgb(15, 19, 24);
        public static readonly Color Panel = Color.FromArgb(23, 29, 36);
        public static readonly Color PanelRaised = Color.FromArgb(28, 35, 43);
        public static readonly Color Control = Color.FromArgb(31, 39, 48);
        public static readonly Color ControlHover = Color.FromArgb(39, 49, 59);
        public static readonly Color Border = Color.FromArgb(54, 66, 78);
        public static readonly Color BorderStrong = Color.FromArgb(107, 130, 151);
        public static readonly Color BorderSoft = Color.FromArgb(42, 52, 62);
        public static readonly Color Text = Color.FromArgb(241, 245, 248);
        public static readonly Color Muted = Color.FromArgb(153, 166, 179);
        public static readonly Color Faint = Color.FromArgb(111, 125, 139);
        public static readonly Color Accent = Color.FromArgb(73, 207, 193);
        public static readonly Color AccentHover = Color.FromArgb(91, 221, 207);
        public static readonly Color AccentPressed = Color.FromArgb(53, 177, 166);
        public static readonly Color AccentInk = Color.FromArgb(8, 35, 35);
        public static readonly Color Danger = Color.FromArgb(235, 104, 109);
        public static readonly Color DangerHover = Color.FromArgb(244, 122, 127);
        public static readonly Color Warning = Color.FromArgb(239, 185, 92);
    }

    sealed class MainForm : Form
    {
        private const int HotkeyToggle = 100;
        private const int HotkeySetLocation = 101;
        private const int WmHotkey = 0x0312;
        private const int DwmUseImmersiveDarkMode = 20;

        private readonly NumericUpDown intervalInput = new NumericUpDown();
        private readonly NumericUpDown variabilityInput = new NumericUpDown();
        private readonly NumericUpDown repeatInput = new NumericUpDown();
        private readonly NumericUpDown xInput = new NumericUpDown();
        private readonly NumericUpDown yInput = new NumericUpDown();
        private readonly ComboBox clickTypeInput = new ComboBox();
        private readonly ComboBox buttonInput = new ComboBox();
        private readonly AccentButton toggleButton = new AccentButton();
        private readonly AccentButton currentMouseButton = new AccentButton();
        private readonly AccentButton markerButton = new AccentButton();
        private readonly CheckBox topMostCheck = new CheckBox();
        private readonly Label stateLabel = new Label();
        private readonly Label stateDetailLabel = new Label();
        private readonly Label clickCountLabel = new Label();
        private readonly Label countCaptionLabel = new Label();
        private readonly Label footerStatusLabel = new Label();
        private readonly Panel statusIndicator = new Panel();
        private readonly TargetMarker marker = new TargetMarker();
        private readonly ToolTip toolTip = new ToolTip();
        private readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer captureTimer = new System.Windows.Forms.Timer();
        private readonly ManualResetEvent stopEvent = new ManualResetEvent(false);
        private readonly Random delayRandom = new Random();
        private readonly Control[] settingsControls;

        private Color statusColor = Theme.Accent;

        private Thread clickThread;
        private volatile bool isRunning;
        private volatile bool isStopping;
        private bool closing;
        private bool toggleHotkeyRegistered;
        private bool locationHotkeyRegistered;
        private long clickCount;
        private int completedRepeats;
        private int requestedRepeats;
        private int captureCountdown;

        public MainForm()
        {
            Text = "AutoClicker";
            Icon = LoadApplicationIcon();
            ClientSize = new Size(760, 642);
            MinimumSize = new Size(776, 681);
            MaximumSize = new Size(776, 681);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;
            DoubleBuffered = true;

            settingsControls = new Control[]
            {
                intervalInput, variabilityInput, repeatInput, clickTypeInput,
                xInput, yInput, buttonInput, currentMouseButton, markerButton
            };

            BuildUi();

            intervalInput.ValueChanged += SettingsChanged;
            variabilityInput.ValueChanged += SettingsChanged;
            repeatInput.ValueChanged += SettingsChanged;
            clickTypeInput.SelectedIndexChanged += SettingsChanged;
            buttonInput.SelectedIndexChanged += SettingsChanged;
            xInput.ValueChanged += CoordinateChanged;
            yInput.ValueChanged += CoordinateChanged;

            marker.LocationChangedByDrag += delegate(object sender, Point point)
            {
                SetClickPoint(point, "Target moved with the marker.");
            };

            SetClickPoint(Cursor.Position, "Ready to click.");
            UpdateReadySummary();

            uiTimer.Interval = 100;
            uiTimer.Tick += delegate { RefreshLiveCount(); };
            uiTimer.Start();

            captureTimer.Interval = 1000;
            captureTimer.Tick += CaptureTimerTick;
        }

        private Icon LoadApplicationIcon()
        {
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { return SystemIcons.Application; }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyDarkTitleBar();
            toggleHotkeyRegistered = RegisterHotKey(Handle, HotkeyToggle, 0, (int)Keys.F6);
            locationHotkeyRegistered = RegisterHotKey(Handle, HotkeySetLocation, 0, (int)Keys.F7);
            UpdateHotkeyStatus();
        }

        private void BuildUi()
        {
            SuspendLayout();

            PictureBox appIcon = new PictureBox
            {
                Location = new Point(28, 24),
                Size = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Icon.ToBitmap(),
                BackColor = Theme.Window,
                TabStop = false
            };

            Label title = new Label
            {
                Text = "AutoClicker",
                Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Theme.Text,
                BackColor = Theme.Window,
                Location = new Point(91, 20),
                Size = new Size(300, 37)
            };

            Label subtitle = new Label
            {
                Text = "Precise, repeatable clicks without the clutter.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Theme.Muted,
                BackColor = Theme.Window,
                Location = new Point(94, 58),
                Size = new Size(390, 23)
            };

            Label hotkeyHint = new Label
            {
                Text = "F6  START / STOP     F7  CAPTURE TARGET",
                Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Theme.Faint,
                BackColor = Theme.Window,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(450, 34),
                Size = new Size(282, 28)
            };

            RoundedPanel runPanel = new RoundedPanel
            {
                Location = new Point(28, 96),
                Size = new Size(704, 112),
                FillColor = Theme.PanelRaised,
                BorderColor = Theme.BorderSoft,
                Radius = 20
            };

            statusIndicator.Location = new Point(24, 28);
            statusIndicator.Size = new Size(10, 10);
            statusIndicator.BackColor = Theme.PanelRaised;
            statusIndicator.TabStop = false;
            statusIndicator.Paint += DrawStatusIndicator;

            stateLabel.Text = "Ready";
            stateLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point);
            stateLabel.ForeColor = Theme.Text;
            stateLabel.BackColor = Theme.PanelRaised;
            stateLabel.Location = new Point(48, 18);
            stateLabel.Size = new Size(265, 32);

            stateDetailLabel.Text = "Ready to click.";
            stateDetailLabel.Font = new Font("Segoe UI", 9.25F, FontStyle.Regular, GraphicsUnit.Point);
            stateDetailLabel.ForeColor = Theme.Muted;
            stateDetailLabel.BackColor = Theme.PanelRaised;
            stateDetailLabel.Location = new Point(24, 57);
            stateDetailLabel.Size = new Size(385, 38);

            clickCountLabel.Text = "0";
            clickCountLabel.Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold, GraphicsUnit.Point);
            clickCountLabel.ForeColor = Theme.Text;
            clickCountLabel.BackColor = Theme.PanelRaised;
            clickCountLabel.TextAlign = ContentAlignment.MiddleRight;
            clickCountLabel.Location = new Point(410, 20);
            clickCountLabel.Size = new Size(90, 38);

            countCaptionLabel.Text = "CLICKS SENT";
            countCaptionLabel.Font = new Font("Segoe UI Semibold", 7.75F, FontStyle.Bold, GraphicsUnit.Point);
            countCaptionLabel.ForeColor = Theme.Faint;
            countCaptionLabel.BackColor = Theme.PanelRaised;
            countCaptionLabel.TextAlign = ContentAlignment.MiddleRight;
            countCaptionLabel.Location = new Point(399, 58);
            countCaptionLabel.Size = new Size(101, 22);

            toggleButton.Text = "Start clicking";
            toggleButton.Location = new Point(520, 27);
            toggleButton.Size = new Size(160, 58);
            toggleButton.BackColor = Theme.PanelRaised;
            toggleButton.BackFill = Theme.Accent;
            toggleButton.HoverFill = Theme.AccentHover;
            toggleButton.PressedFill = Theme.AccentPressed;
            toggleButton.TextColor = Theme.AccentInk;
            toggleButton.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            toggleButton.Radius = 14;
            toggleButton.Click += delegate { ToggleClicking(); };
            toolTip.SetToolTip(toggleButton, "Start or stop clicking. Global hotkey: F6");

            runPanel.Controls.AddRange(new Control[]
            {
                statusIndicator, stateLabel, stateDetailLabel,
                clickCountLabel, countCaptionLabel, toggleButton
            });

            RoundedPanel timingPanel = new RoundedPanel
            {
                Location = new Point(28, 226),
                Size = new Size(340, 304),
                FillColor = Theme.Panel,
                BorderColor = Theme.BorderSoft,
                Radius = 18
            };
            BuildTimingPanel(timingPanel);

            RoundedPanel targetPanel = new RoundedPanel
            {
                Location = new Point(388, 226),
                Size = new Size(344, 304),
                FillColor = Theme.Panel,
                BorderColor = Theme.BorderSoft,
                Radius = 18
            };
            BuildTargetPanel(targetPanel);

            RoundedPanel footerPanel = new RoundedPanel
            {
                Location = new Point(28, 548),
                Size = new Size(704, 66),
                FillColor = Theme.Panel,
                BorderColor = Theme.BorderSoft,
                Radius = 16
            };

            topMostCheck.Text = "Keep window on top";
            topMostCheck.AutoSize = false;
            topMostCheck.Size = new Size(180, 25);
            topMostCheck.Location = new Point(20, 20);
            topMostCheck.ForeColor = Theme.Text;
            topMostCheck.BackColor = Theme.Panel;
            topMostCheck.FlatStyle = FlatStyle.Flat;
            topMostCheck.CheckedChanged += delegate { TopMost = topMostCheck.Checked; };
            toolTip.SetToolTip(topMostCheck, "Keep AutoClicker above other windows. The target marker is always on top.");

            footerStatusLabel.Text = "Global hotkeys will be checked when the window opens.";
            footerStatusLabel.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular, GraphicsUnit.Point);
            footerStatusLabel.ForeColor = Theme.Muted;
            footerStatusLabel.BackColor = Theme.Panel;
            footerStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            footerStatusLabel.Location = new Point(210, 17);
            footerStatusLabel.Size = new Size(472, 30);

            footerPanel.Controls.Add(topMostCheck);
            footerPanel.Controls.Add(footerStatusLabel);

            Controls.AddRange(new Control[]
            {
                appIcon, title, subtitle, hotkeyHint,
                runPanel, timingPanel, targetPanel, footerPanel
            });

            ResumeLayout(false);
        }

        private void BuildTimingPanel(RoundedPanel panel)
        {
            panel.Controls.Add(MakeSectionTitle("Timing", 20, 16, 150));
            panel.Controls.Add(MakeSectionHint("Set a steady cadence or add natural variance.", 20, 45, 300));

            panel.Controls.Add(MakeFieldLabel("Click interval", 20, 82, 130));
            ConfigureNumeric(intervalInput, 10, 600000, 1000, 50);
            intervalInput.AccessibleName = "Click interval in milliseconds";
            intervalInput.Location = new Point(178, 78);
            intervalInput.Size = new Size(118, 29);
            panel.Controls.Add(intervalInput);
            panel.Controls.Add(MakeUnitLabel("ms", 300, 82, 24));

            FlowLayoutPanel presets = new FlowLayoutPanel
            {
                Location = new Point(20, 116),
                Size = new Size(300, 32),
                BackColor = Theme.Panel,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            presets.Controls.Add(MakePresetButton("100 ms", 100));
            presets.Controls.Add(MakePresetButton("500 ms", 500));
            presets.Controls.Add(MakePresetButton("1 sec", 1000));
            presets.Controls.Add(MakePresetButton("5 sec", 5000));
            panel.Controls.Add(presets);

            panel.Controls.Add(MakeFieldLabel("Random variance", 20, 162, 140));
            ConfigureNumeric(variabilityInput, 0, 600000, 0, 25);
            variabilityInput.AccessibleName = "Random timing variance in milliseconds";
            variabilityInput.Location = new Point(178, 158);
            variabilityInput.Size = new Size(118, 29);
            panel.Controls.Add(variabilityInput);
            panel.Controls.Add(MakeUnitLabel("ms", 300, 162, 24));

            panel.Controls.Add(MakeFieldLabel("Click type", 20, 207, 130));
            ConfigureCombo(clickTypeInput, new object[] { "Single", "Double" });
            clickTypeInput.AccessibleName = "Click type";
            clickTypeInput.Location = new Point(178, 203);
            clickTypeInput.Size = new Size(142, 29);
            panel.Controls.Add(clickTypeInput);

            panel.Controls.Add(MakeFieldLabel("Repeat count", 20, 252, 130));
            ConfigureNumeric(repeatInput, 0, 999999, 0, 1);
            repeatInput.AccessibleName = "Repeat count, zero means unlimited";
            repeatInput.Location = new Point(178, 248);
            repeatInput.Size = new Size(142, 29);
            panel.Controls.Add(repeatInput);
            Label unlimited = MakeSectionHint("0 runs until stopped", 20, 278, 220);
            unlimited.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            panel.Controls.Add(unlimited);
        }

        private void BuildTargetPanel(RoundedPanel panel)
        {
            panel.Controls.Add(MakeSectionTitle("Target", 20, 16, 150));
            panel.Controls.Add(MakeSectionHint("Choose the button and exact screen position.", 20, 45, 300));

            panel.Controls.Add(MakeFieldLabel("Mouse button", 20, 82, 130));
            ConfigureCombo(buttonInput, new object[] { "Left", "Right", "Middle" });
            buttonInput.AccessibleName = "Mouse button";
            buttonInput.Location = new Point(176, 78);
            buttonInput.Size = new Size(148, 29);
            panel.Controls.Add(buttonInput);

            panel.Controls.Add(MakeFieldLabel("Position", 20, 127, 70));
            Label xCaption = MakeFieldLabel("X", 98, 127, 22);
            xCaption.ForeColor = Theme.Muted;
            xCaption.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(xCaption);
            Rectangle virtualScreen = SystemInformation.VirtualScreen;
            ConfigureNumeric(xInput, virtualScreen.Left, virtualScreen.Right - 1, 0, 1);
            xInput.AccessibleName = "Target X coordinate";
            xInput.Location = new Point(124, 123);
            xInput.Size = new Size(84, 29);
            panel.Controls.Add(xInput);
            Label yCaption = MakeFieldLabel("Y", 212, 127, 22);
            yCaption.ForeColor = Theme.Muted;
            yCaption.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(yCaption);
            ConfigureNumeric(yInput, virtualScreen.Top, virtualScreen.Bottom - 1, 0, 1);
            yInput.AccessibleName = "Target Y coordinate";
            yInput.Location = new Point(238, 123);
            yInput.Size = new Size(86, 29);
            panel.Controls.Add(yInput);

            currentMouseButton.Text = "Capture position in 2 seconds";
            currentMouseButton.Location = new Point(20, 172);
            currentMouseButton.Size = new Size(304, 45);
            currentMouseButton.BackColor = Theme.Panel;
            currentMouseButton.BackFill = Theme.Control;
            currentMouseButton.HoverFill = Theme.ControlHover;
            currentMouseButton.PressedFill = Theme.AccentPressed;
            currentMouseButton.TextColor = Theme.Text;
            currentMouseButton.BorderColor = Theme.BorderStrong;
            currentMouseButton.BorderWidth = 1F;
            currentMouseButton.DrawBorder = true;
            currentMouseButton.Radius = 12;
            currentMouseButton.Click += delegate { BeginDelayedCapture(); };
            toolTip.SetToolTip(currentMouseButton, "Gives you two seconds to move the cursor. F7 captures immediately.");
            panel.Controls.Add(currentMouseButton);

            markerButton.Text = "Show draggable target";
            markerButton.Location = new Point(20, 229);
            markerButton.Size = new Size(304, 45);
            markerButton.BackColor = Theme.Panel;
            markerButton.BackFill = Theme.Panel;
            markerButton.HoverFill = Theme.Control;
            markerButton.PressedFill = Theme.ControlHover;
            markerButton.TextColor = Theme.Text;
            markerButton.BorderColor = Theme.BorderStrong;
            markerButton.BorderWidth = 1F;
            markerButton.DrawBorder = true;
            markerButton.Radius = 12;
            markerButton.Click += delegate { ToggleMarker(); };
            toolTip.SetToolTip(markerButton, "Show a crosshair you can drag to the click target.");
            panel.Controls.Add(markerButton);
        }

        private Label MakeSectionTitle(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Theme.Text,
                BackColor = Theme.Panel,
                Location = new Point(x, y),
                Size = new Size(width, 27)
            };
        }

        private Label MakeSectionHint(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Theme.Muted,
                BackColor = Theme.Panel,
                Location = new Point(x, y),
                Size = new Size(width, 22)
            };
        }

        private Label MakeFieldLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Theme.Text,
                BackColor = Theme.Panel,
                Location = new Point(x, y),
                Size = new Size(width, 24)
            };
        }

        private Label MakeUnitLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Theme.Faint,
                BackColor = Theme.Panel,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(x, y),
                Size = new Size(width, 22)
            };
        }

        private AccentButton MakePresetButton(string text, int value)
        {
            AccentButton button = new AccentButton
            {
                Text = text,
                Size = new Size(68, 28),
                Margin = new Padding(0, 0, 7, 0),
                BackColor = Theme.Panel,
                BackFill = Theme.Control,
                HoverFill = Theme.ControlHover,
                PressedFill = Theme.AccentPressed,
                TextColor = Theme.Muted,
                BorderColor = Theme.BorderStrong,
                BorderWidth = 1F,
                DrawBorder = true,
                Radius = 9,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold, GraphicsUnit.Point),
                Tag = value
            };
            button.Click += delegate(object sender, EventArgs e)
            {
                AccentButton preset = sender as AccentButton;
                if (preset != null)
                    intervalInput.Value = Convert.ToDecimal((int)preset.Tag);
            };
            toolTip.SetToolTip(button, "Set click interval to " + text + ".");
            return button;
        }

        private static void ConfigureNumeric(NumericUpDown input, decimal minimum, decimal maximum, decimal value, decimal increment)
        {
            input.Minimum = minimum;
            input.Maximum = maximum;
            input.Value = value;
            input.Increment = increment;
            input.BackColor = Theme.Control;
            input.ForeColor = Theme.Text;
            input.BorderStyle = BorderStyle.FixedSingle;
            input.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            input.TextAlign = HorizontalAlignment.Left;
        }

        private static void ConfigureCombo(ComboBox input, object[] items)
        {
            input.DropDownStyle = ComboBoxStyle.DropDownList;
            input.FlatStyle = FlatStyle.Flat;
            input.BackColor = Theme.Control;
            input.ForeColor = Theme.Text;
            input.Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold, GraphicsUnit.Point);
            input.Items.AddRange(items);
            input.SelectedIndex = 0;
        }

        private void DrawStatusIndicator(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(statusColor))
                e.Graphics.FillEllipse(brush, 1, 1, statusIndicator.Width - 2, statusIndicator.Height - 2);
        }

        private void SettingsChanged(object sender, EventArgs e)
        {
            if (!isRunning && !isStopping)
                UpdateReadySummary();
        }

        private void CoordinateChanged(object sender, EventArgs e)
        {
            if (marker.Visible)
                marker.SetCenter(new Point((int)xInput.Value, (int)yInput.Value));
            SettingsChanged(sender, e);
        }

        private void UpdateReadySummary()
        {
            string clickType = SelectedText(clickTypeInput, "Single").ToLowerInvariant();
            string mouseButton = SelectedText(buttonInput, "Left").ToLowerInvariant();
            string cadence = FormatDuration((int)intervalInput.Value);
            string repeat = repeatInput.Value == 0 ? "until stopped" : repeatInput.Value + " time" + (repeatInput.Value == 1 ? "" : "s");
            stateLabel.Text = "Ready";
            stateLabel.ForeColor = Theme.Text;
            stateDetailLabel.Text = Capitalize(clickType) + " " + mouseButton + " click every " + cadence + ", " + repeat + ".";
            statusColor = Theme.Accent;
            statusIndicator.Invalidate();
            toggleButton.Text = "Start clicking";
            SetToggleButtonRunningStyle(false);
        }

        private static string SelectedText(ComboBox input, string fallback)
        {
            return input.SelectedItem == null ? fallback : input.SelectedItem.ToString();
        }

        private static string Capitalize(string value)
        {
            if (String.IsNullOrEmpty(value)) return value;
            return Char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static string FormatDuration(int milliseconds)
        {
            if (milliseconds < 1000)
                return milliseconds + " ms";
            double seconds = milliseconds / 1000.0;
            return seconds.ToString(seconds == Math.Floor(seconds) ? "0" : "0.##") + " sec";
        }

        private void ToggleMarker()
        {
            if (isRunning || isStopping)
                return;

            if (marker.Visible)
            {
                marker.Hide();
                markerButton.Text = "Show draggable target";
                footerStatusLabel.Text = "Target marker hidden.";
                return;
            }

            marker.SetCenter(new Point((int)xInput.Value, (int)yInput.Value));
            marker.Show(this);
            markerButton.Text = "Hide draggable target";
            footerStatusLabel.Text = "Drag the crosshair to update the target position.";
        }

        private void BeginDelayedCapture()
        {
            if (isRunning || isStopping)
                return;

            marker.Hide();
            markerButton.Text = "Show draggable target";
            captureCountdown = 2;
            currentMouseButton.Enabled = false;
            currentMouseButton.Text = "Move cursor to target   2";
            stateDetailLabel.Text = "Move the cursor. Capturing the target in 2 seconds.";
            footerStatusLabel.Text = "F7 can still capture immediately.";
            footerStatusLabel.ForeColor = Theme.Muted;
            captureTimer.Start();
        }

        private void CaptureTimerTick(object sender, EventArgs e)
        {
            captureCountdown--;
            if (captureCountdown > 0)
            {
                currentMouseButton.Text = "Move cursor to target   " + captureCountdown;
                stateDetailLabel.Text = "Move the cursor. Capturing the target in " + captureCountdown + " second.";
                return;
            }

            captureTimer.Stop();
            currentMouseButton.Enabled = true;
            currentMouseButton.Text = "Capture position in 2 seconds";
            SetClickPoint(Cursor.Position, "Captured the target after the countdown.");
        }

        private void CancelDelayedCapture()
        {
            if (!captureTimer.Enabled)
                return;
            captureTimer.Stop();
            captureCountdown = 0;
            currentMouseButton.Enabled = true;
            currentMouseButton.Text = "Capture position in 2 seconds";
        }

        private void CaptureCurrentPosition()
        {
            if (isRunning || isStopping)
            {
                footerStatusLabel.Text = "Stop clicking before changing the target.";
                footerStatusLabel.ForeColor = Theme.Warning;
                return;
            }
            CancelDelayedCapture();
            SetClickPoint(Cursor.Position, "Captured the current cursor position with F7.");
        }

        private void SetClickPoint(Point point, string message)
        {
            decimal x = Clamp(point.X, xInput.Minimum, xInput.Maximum);
            decimal y = Clamp(point.Y, yInput.Minimum, yInput.Maximum);
            if (xInput.Value != x) xInput.Value = x;
            if (yInput.Value != y) yInput.Value = y;
            if (marker.Visible)
                marker.SetCenter(new Point((int)x, (int)y));
            footerStatusLabel.Text = message;
            footerStatusLabel.ForeColor = Theme.Muted;
            if (!isRunning && !isStopping)
                UpdateReadySummary();
        }

        private static decimal Clamp(int value, decimal min, decimal max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private void ToggleClicking()
        {
            if (isStopping)
                return;
            if (isRunning)
                StopClicking("Stopped by user.");
            else
                StartClicking();
        }

        private void StartClicking()
        {
            if (isRunning || isStopping)
                return;

            CancelDelayedCapture();

            ClickSettings settings = new ClickSettings
            {
                X = (int)xInput.Value,
                Y = (int)yInput.Value,
                Interval = (int)intervalInput.Value,
                Variability = (int)variabilityInput.Value,
                MouseButton = SelectedText(buttonInput, "Left"),
                DoubleClick = SelectedText(clickTypeInput, "Single") == "Double",
                RepeatCount = (int)repeatInput.Value
            };

            if (settings.Variability >= settings.Interval)
            {
                footerStatusLabel.Text = "Variance may reduce some delays to 1 ms.";
                footerStatusLabel.ForeColor = Theme.Warning;
            }
            else
            {
                footerStatusLabel.Text = "F6 or Escape stops immediately.";
                footerStatusLabel.ForeColor = Theme.Muted;
            }

            requestedRepeats = settings.RepeatCount;
            completedRepeats = 0;
            Interlocked.Exchange(ref clickCount, 0);
            stopEvent.Reset();
            isRunning = true;
            isStopping = false;
            SetSettingsEnabled(false);
            marker.Hide();
            markerButton.Text = "Show draggable target";
            UpdateRunningState();

            clickThread = new Thread(ClickLoop);
            clickThread.IsBackground = true;
            clickThread.Name = "AutoClicker Worker";
            clickThread.Start(settings);
        }

        private void UpdateRunningState()
        {
            stateLabel.Text = "Clicking";
            stateLabel.ForeColor = Theme.Accent;
            stateDetailLabel.Text = "Target " + xInput.Value + ", " + yInput.Value + ". Press F6 or Escape to stop.";
            statusColor = Theme.Accent;
            statusIndicator.Invalidate();
            toggleButton.Text = "Stop clicking";
            toggleButton.Enabled = true;
            SetToggleButtonRunningStyle(true);
        }

        private void SetToggleButtonRunningStyle(bool running)
        {
            toggleButton.BackFill = running ? Theme.Danger : Theme.Accent;
            toggleButton.HoverFill = running ? Theme.DangerHover : Theme.AccentHover;
            toggleButton.PressedFill = running ? Color.FromArgb(205, 82, 88) : Theme.AccentPressed;
            toggleButton.TextColor = running ? Theme.Text : Theme.AccentInk;
            toggleButton.Invalidate();
        }

        private void StopClicking(string reason)
        {
            if (!isRunning)
                return;

            isRunning = false;
            isStopping = true;
            stopEvent.Set();
            stateLabel.Text = "Stopping";
            stateLabel.ForeColor = Theme.Muted;
            stateDetailLabel.Text = reason;
            statusColor = Theme.Warning;
            statusIndicator.Invalidate();
            toggleButton.Enabled = false;
        }

        private void ClickLoop(object state)
        {
            ClickSettings settings = (ClickSettings)state;
            bool completedNaturally = false;

            try
            {
                while (!stopEvent.WaitOne(0))
                {
                    MouseClicker.ClickAt(settings.X, settings.Y, settings.MouseButton);
                    Interlocked.Increment(ref clickCount);

                    if (settings.DoubleClick && !stopEvent.WaitOne(70))
                    {
                        MouseClicker.ClickAt(settings.X, settings.Y, settings.MouseButton);
                        Interlocked.Increment(ref clickCount);
                    }

                    completedRepeats++;
                    if (settings.RepeatCount > 0 && completedRepeats >= settings.RepeatCount)
                    {
                        completedNaturally = true;
                        break;
                    }

                    int delay = GetNextClickDelay(settings.Interval, settings.Variability);
                    if (stopEvent.WaitOne(delay))
                        break;
                }
            }
            catch (Exception ex)
            {
                QueueFinish(false, "Clicking stopped: " + ex.Message);
                return;
            }

            QueueFinish(completedNaturally, completedNaturally ? "Repeat count completed." : "Stopped by user.");
        }

        private int GetNextClickDelay(int baseDelay, int variability)
        {
            if (variability <= 0)
                return Math.Max(1, baseDelay);
            lock (delayRandom)
            {
                int offset = delayRandom.Next(-variability, variability + 1);
                return Math.Max(1, baseDelay + offset);
            }
        }

        private void QueueFinish(bool completedNaturally, string message)
        {
            if (closing || IsDisposed)
                return;
            try
            {
                BeginInvoke((MethodInvoker)delegate { FinishRun(completedNaturally, message); });
            }
            catch (InvalidOperationException) { }
        }

        private void FinishRun(bool completedNaturally, string message)
        {
            if (closing)
                return;

            isRunning = false;
            isStopping = false;
            toggleButton.Enabled = true;
            SetSettingsEnabled(true);
            stateLabel.Text = completedNaturally ? "Complete" : "Ready";
            stateLabel.ForeColor = completedNaturally ? Theme.Accent : Theme.Text;
            stateDetailLabel.Text = message;
            statusColor = Theme.Accent;
            statusIndicator.Invalidate();
            toggleButton.Text = "Start clicking";
            SetToggleButtonRunningStyle(false);
            footerStatusLabel.Text = message;
            footerStatusLabel.ForeColor = Theme.Muted;
            RefreshLiveCount();
        }

        private void SetSettingsEnabled(bool enabled)
        {
            foreach (Control control in settingsControls)
                control.Enabled = enabled;
        }

        private void RefreshLiveCount()
        {
            long count = Interlocked.Read(ref clickCount);
            clickCountLabel.Text = count.ToString("N0");
            countCaptionLabel.Text = requestedRepeats > 0
                ? "CLICKS SENT  |  " + completedRepeats + "/" + requestedRepeats + " RUNS"
                : "CLICKS SENT";
        }

        private void UpdateHotkeyStatus()
        {
            footerStatusLabel.ForeColor = Theme.Muted;
            if (toggleHotkeyRegistered && locationHotkeyRegistered)
                footerStatusLabel.Text = "Global hotkeys active: F6 start / stop, F7 capture target.";
            else if (!toggleHotkeyRegistered && !locationHotkeyRegistered)
            {
                footerStatusLabel.Text = "F6 and F7 are in use by another app. Buttons still work.";
                footerStatusLabel.ForeColor = Theme.Warning;
            }
            else if (!toggleHotkeyRegistered)
            {
                footerStatusLabel.Text = "F6 is in use by another app. Use the Start button.";
                footerStatusLabel.ForeColor = Theme.Warning;
            }
            else
            {
                footerStatusLabel.Text = "F7 is in use by another app. Use Capture current position.";
                footerStatusLabel.ForeColor = Theme.Warning;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && isRunning)
            {
                StopClicking("Stopped with Escape.");
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey)
            {
                int id = m.WParam.ToInt32();
                if (id == HotkeyToggle)
                    ToggleClicking();
                else if (id == HotkeySetLocation)
                    CaptureCurrentPosition();
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            closing = true;
            isRunning = false;
            stopEvent.Set();
            uiTimer.Stop();
            captureTimer.Stop();
            captureTimer.Dispose();
            if (clickThread != null && clickThread.IsAlive)
                clickThread.Join(600);
            marker.Close();
            if (toggleHotkeyRegistered) UnregisterHotKey(Handle, HotkeyToggle);
            if (locationHotkeyRegistered) UnregisterHotKey(Handle, HotkeySetLocation);
            stopEvent.Dispose();
            base.OnFormClosing(e);
        }

        private void ApplyDarkTitleBar()
        {
            try
            {
                int enabled = 1;
                DwmSetWindowAttribute(Handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
            }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }

    sealed class ClickSettings
    {
        public int X;
        public int Y;
        public int Interval;
        public int Variability;
        public string MouseButton;
        public bool DoubleClick;
        public int RepeatCount;
    }

    sealed class RoundedPanel : Panel
    {
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public int Radius { get; set; }

        public RoundedPanel()
        {
            FillColor = Theme.Panel;
            BorderColor = Theme.BorderSoft;
            Radius = 16;
            BackColor = Theme.Window;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Shape.RoundRect(rect, Radius))
            using (SolidBrush brush = new SolidBrush(FillColor))
            using (Pen pen = new Pen(BorderColor, 1F))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    sealed class AccentButton : Button
    {
        private bool hovering;
        private bool pressing;

        public Color BackFill { get; set; }
        public Color HoverFill { get; set; }
        public Color PressedFill { get; set; }
        public Color TextColor { get; set; }
        public Color BorderColor { get; set; }
        public float BorderWidth { get; set; }
        public bool DrawBorder { get; set; }
        private int radius;
        public int Radius
        {
            get { return radius; }
            set { radius = value; UpdateButtonRegion(); Invalidate(); }
        }

        public AccentButton()
        {
            BackFill = Theme.Control;
            HoverFill = Theme.ControlHover;
            PressedFill = Theme.AccentPressed;
            TextColor = Theme.Text;
            BorderColor = Theme.Border;
            BorderWidth = 1F;
            Radius = 12;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            TabStop = true;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateButtonRegion();
        }

        private void UpdateButtonRegion()
        {
            if (Width <= 0 || Height <= 0 || radius <= 0)
                return;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (GraphicsPath path = Shape.RoundRect(bounds, radius))
            {
                Region previous = Region;
                Region = new Region(path);
                if (previous != null) previous.Dispose();
            }
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; pressing = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressing = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressing = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = pressing ? PressedFill : (hovering ? HoverFill : BackFill);
            if (!Enabled) fill = Color.FromArgb(75, fill);

            using (GraphicsPath path = Shape.RoundRect(rect, Radius))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                e.Graphics.FillPath(brush, path);
                if (DrawBorder)
                {
                    float inset = Math.Max(0.5F, BorderWidth / 2F);
                    RectangleF borderRect = new RectangleF(
                        inset,
                        inset,
                        Math.Max(1F, Width - 1F - inset * 2F),
                        Math.Max(1F, Height - 1F - inset * 2F));
                    using (GraphicsPath borderPath = Shape.RoundRect(borderRect, Math.Max(1F, Radius - inset)))
                    using (Pen pen = new Pen(Enabled ? BorderColor : Theme.BorderSoft, BorderWidth))
                    {
                        pen.LineJoin = LineJoin.Round;
                        e.Graphics.DrawPath(pen, borderPath);
                    }
                }
            }

            Color text = Enabled ? TextColor : Theme.Faint;
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues)
            {
                Rectangle focus = Rectangle.Inflate(rect, -4, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, text, fill);
            }
        }
    }

    static class Shape
    {
        public static GraphicsPath RoundRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath RoundRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = Math.Max(2F, Math.Min(radius * 2F, Math.Min(rect.Width, rect.Height)));
            RectangleF arc = new RectangleF(rect.Location, new SizeF(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    sealed class TargetMarker : Form
    {
        private const int MarkerSize = 40;
        private const int WsExNoActivate = 0x08000000;
        private const int WsExToolWindow = 0x00000080;

        public event EventHandler<Point> LocationChangedByDrag;
        private Point dragOffset;
        private bool dragging;

        public TargetMarker()
        {
            Text = "Click target";
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(MarkerSize, MarkerSize);
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Cursor = Cursors.SizeAll;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WsExNoActivate | WsExToolWindow;
                return parameters;
            }
        }

        public void SetCenter(Point point)
        {
            Location = new Point(point.X - Width / 2, point.Y - Height / 2);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float center = MarkerSize / 2F;
            using (Pen shadow = new Pen(Color.FromArgb(165, 0, 0, 0), 5F))
            using (Pen accent = new Pen(Theme.Accent, 2.2F))
            using (SolidBrush centerBrush = new SolidBrush(Theme.Accent))
            {
                e.Graphics.DrawEllipse(shadow, 7, 7, MarkerSize - 14, MarkerSize - 14);
                e.Graphics.DrawLine(shadow, center, 2, center, MarkerSize - 2);
                e.Graphics.DrawLine(shadow, 2, center, MarkerSize - 2, center);
                e.Graphics.DrawEllipse(accent, 7, 7, MarkerSize - 14, MarkerSize - 14);
                e.Graphics.DrawLine(accent, center, 2, center, 13);
                e.Graphics.DrawLine(accent, center, MarkerSize - 13, center, MarkerSize - 2);
                e.Graphics.DrawLine(accent, 2, center, 13, center);
                e.Graphics.DrawLine(accent, MarkerSize - 13, center, MarkerSize - 2, center);
                e.Graphics.FillEllipse(centerBrush, center - 3, center - 3, 6, 6);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                dragOffset = e.Location;
                Capture = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                Location = new Point(MousePosition.X - dragOffset.X, MousePosition.Y - dragOffset.Y);
                EventHandler<Point> handler = LocationChangedByDrag;
                if (handler != null)
                    handler(this, new Point(Location.X + Width / 2, Location.Y + Height / 2));
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            Capture = false;
            base.OnMouseUp(e);
        }
    }

    static class MouseClicker
    {
        private const int InputMouse = 0;
        private const uint MouseeventfLeftdown = 0x0002;
        private const uint MouseeventfLeftup = 0x0004;
        private const uint MouseeventfRightdown = 0x0008;
        private const uint MouseeventfRightup = 0x0010;
        private const uint MouseeventfMiddledown = 0x0020;
        private const uint MouseeventfMiddleup = 0x0040;

        public static void ClickAt(int x, int y, string button)
        {
            if (!SetCursorPos(x, y))
                throw new InvalidOperationException("Windows could not move the cursor to the target.");

            uint down;
            uint up;
            switch (button)
            {
                case "Right":
                    down = MouseeventfRightdown;
                    up = MouseeventfRightup;
                    break;
                case "Middle":
                    down = MouseeventfMiddledown;
                    up = MouseeventfMiddleup;
                    break;
                default:
                    down = MouseeventfLeftdown;
                    up = MouseeventfLeftup;
                    break;
            }

            Input[] inputs = new Input[2];
            inputs[0].Type = InputMouse;
            inputs[0].Data.Mouse.Flags = down;
            inputs[1].Type = InputMouse;
            inputs[1].Data.Mouse.Flags = up;

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
            if (sent != inputs.Length)
                throw new InvalidOperationException("Windows could not send the mouse click.");
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public int Type;
            public InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int Dx;
            public int Dy;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInput);
    }
}
