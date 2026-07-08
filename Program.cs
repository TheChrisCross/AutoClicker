using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AutoClicker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    sealed class MainForm : Form
    {
        private static readonly Color WindowBack = Color.FromArgb(18, 18, 20);
        private static readonly Color PanelBack = Color.FromArgb(26, 27, 31);
        private static readonly Color ControlBack = Color.FromArgb(34, 36, 42);
        private static readonly Color ControlHover = Color.FromArgb(45, 48, 56);
        private static readonly Color BorderColor = Color.FromArgb(74, 78, 88);
        private static readonly Color TextColor = Color.FromArgb(238, 239, 242);
        private static readonly Color MutedTextColor = Color.FromArgb(175, 178, 186);
        private static readonly Color AccentColor = Color.FromArgb(210, 51, 58);

        private const int HOTKEY_TOGGLE = 100;
        private const int HOTKEY_SET_LOCATION = 101;
        private const int MOD_NONE = 0;
        private const int WM_HOTKEY = 0x0312;

        private readonly NumericUpDown intervalInput = new NumericUpDown();
        private readonly NumericUpDown variabilityInput = new NumericUpDown();
        private readonly NumericUpDown xInput = new NumericUpDown();
        private readonly NumericUpDown yInput = new NumericUpDown();
        private readonly ComboBox buttonInput = new ComboBox();
        private readonly Button toggleButton = new Button();
        private readonly Button currentMouseButton = new Button();
        private readonly Button markerButton = new Button();
        private readonly Label statusLabel = new Label();
        private readonly TargetMarker marker = new TargetMarker();
        private readonly Random delayRandom = new Random();

        private Thread clickThread;
        private volatile bool isRunning;

        public MainForm()
        {
            Text = "AutoClicker";
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // The app still runs normally if Windows cannot load the embedded icon.
            }

            ClientSize = new Size(520, 416);
            BackColor = WindowBack;
            ForeColor = TextColor;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F);

            BuildUi();
            RegisterHotKey(Handle, HOTKEY_TOGGLE, MOD_NONE, (int)Keys.F6);
            RegisterHotKey(Handle, HOTKEY_SET_LOCATION, MOD_NONE, (int)Keys.F7);

            marker.LocationChangedByDrag += delegate(object sender, Point point)
            {
                SetClickPoint(point);
            };

            SetClickPoint(Cursor.Position);
            UpdateStatus();
        }

        private void BuildUi()
        {
            Label titleLabel = new Label
            {
                Text = "AutoClicker",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(26, 20),
                Size = new Size(220, 34)
            };

            Label subtitleLabel = new Label
            {
                Text = "F6 starts or stops. F7 captures the current mouse position.",
                ForeColor = MutedTextColor,
                Location = new Point(28, 56),
                Size = new Size(450, 24)
            };

            Panel settingsPanel = new Panel
            {
                BackColor = PanelBack,
                Location = new Point(22, 92),
                Size = new Size(476, 178)
            };
            settingsPanel.Paint += delegate(object sender, PaintEventArgs e)
            {
                DrawBorder(e.Graphics, settingsPanel.ClientRectangle);
            };

            Label intervalLabel = MakeLabel("Delay between clicks", 18, 18, 190);
            intervalInput.Location = new Point(230, 14);
            intervalInput.Size = new Size(220, 28);
            intervalInput.Minimum = 10;
            intervalInput.Maximum = 600000;
            intervalInput.Increment = 50;
            intervalInput.Value = 1000;
            StyleInput(intervalInput);

            Label variabilityLabel = MakeLabel("Random Variability (ms)", 18, 56, 190);
            variabilityInput.Location = new Point(230, 52);
            variabilityInput.Size = new Size(220, 28);
            variabilityInput.Minimum = 0;
            variabilityInput.Maximum = 600000;
            variabilityInput.Increment = 50;
            variabilityInput.Value = 0;
            StyleInput(variabilityInput);

            Label buttonLabel = MakeLabel("Mouse button", 18, 94, 190);
            buttonInput.Location = new Point(230, 90);
            buttonInput.Size = new Size(220, 28);
            buttonInput.DropDownStyle = ComboBoxStyle.DropDownList;
            buttonInput.Items.AddRange(new object[] { "Left", "Right", "Middle" });
            buttonInput.SelectedIndex = 0;
            StyleComboBox(buttonInput);

            Label xLabel = MakeLabel("Click X", 18, 132, 70);
            xInput.Location = new Point(88, 128);
            xInput.Size = new Size(130, 28);
            xInput.Minimum = -32768;
            xInput.Maximum = 32767;
            xInput.ValueChanged += delegate { MoveMarkerToInputs(); };
            StyleInput(xInput);

            Label yLabel = MakeLabel("Click Y", 250, 132, 70);
            yInput.Location = new Point(320, 128);
            yInput.Size = new Size(130, 28);
            yInput.Minimum = -32768;
            yInput.Maximum = 32767;
            yInput.ValueChanged += delegate { MoveMarkerToInputs(); };
            StyleInput(yInput);

            settingsPanel.Controls.AddRange(new Control[]
            {
                intervalLabel, intervalInput, variabilityLabel, variabilityInput,
                buttonLabel, buttonInput, xLabel, xInput, yLabel, yInput
            });

            currentMouseButton.Text = "Use Current Mouse Position (F7)";
            currentMouseButton.Location = new Point(22, 286);
            currentMouseButton.Size = new Size(476, 36);
            currentMouseButton.Click += delegate { SetClickPoint(Cursor.Position); };
            StyleButton(currentMouseButton);

            markerButton.Text = "Show / Drag Target Marker";
            markerButton.Location = new Point(22, 330);
            markerButton.Size = new Size(228, 36);
            markerButton.Click += delegate { ToggleMarker(); };
            StyleButton(markerButton);

            toggleButton.Text = "Start (F6)";
            toggleButton.Location = new Point(270, 330);
            toggleButton.Size = new Size(228, 36);
            toggleButton.Click += delegate { ToggleClicking(); };
            StyleButton(toggleButton);

            statusLabel.Location = new Point(26, 382);
            statusLabel.Size = new Size(468, 22);
            statusLabel.ForeColor = MutedTextColor;

            Controls.AddRange(new Control[]
            {
                titleLabel, subtitleLabel, settingsPanel, statusLabel,
                currentMouseButton, markerButton, toggleButton
            });
        }

        private Label MakeLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y + 4),
                Size = new Size(width, 24),
                ForeColor = TextColor,
                BackColor = PanelBack
            };
        }

        private static void StyleInput(NumericUpDown input)
        {
            input.BackColor = ControlBack;
            input.ForeColor = TextColor;
            input.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.BackColor = ControlBack;
            comboBox.ForeColor = TextColor;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        private static void StyleButton(Button button)
        {
            button.BackColor = ControlBack;
            button.ForeColor = TextColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ControlHover;
            button.FlatAppearance.MouseDownBackColor = AccentColor;
            button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        private static void DrawBorder(Graphics graphics, Rectangle bounds)
        {
            Rectangle rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            using (Pen pen = new Pen(BorderColor))
            {
                graphics.DrawRectangle(pen, rect);
            }
        }

        private void ToggleMarker()
        {
            if (marker.Visible)
            {
                marker.Hide();
                return;
            }

            MoveMarkerToInputs();
            marker.Show();
            marker.Activate();
        }

        private void MoveMarkerToInputs()
        {
            if (!marker.Visible)
                return;

            marker.SetCenter(new Point((int)xInput.Value, (int)yInput.Value));
        }

        private void SetClickPoint(Point point)
        {
            xInput.ValueChanged -= delegate { MoveMarkerToInputs(); };
            xInput.Value = Clamp(point.X, (int)xInput.Minimum, (int)xInput.Maximum);
            yInput.Value = Clamp(point.Y, (int)yInput.Minimum, (int)yInput.Maximum);
            MoveMarkerToInputs();
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private void ToggleClicking()
        {
            if (isRunning)
            {
                StopClicking();
            }
            else
            {
                StartClicking();
            }
        }

        private void StartClicking()
        {
            if (isRunning)
                return;

            isRunning = true;
            clickThread = new Thread(ClickLoop);
            clickThread.IsBackground = true;
            clickThread.Start();
            UpdateStatus();
        }

        private void StopClicking()
        {
            isRunning = false;
            if (clickThread != null && clickThread.IsAlive)
                clickThread.Join(250);
            UpdateStatus();
        }

        private void ClickLoop()
        {
            while (isRunning)
            {
                int x = 0;
                int y = 0;
                int delay = 1000;
                int variability = 0;
                string button = "Left";

                Invoke((MethodInvoker)delegate
                {
                    x = (int)xInput.Value;
                    y = (int)yInput.Value;
                    delay = (int)intervalInput.Value;
                    variability = (int)variabilityInput.Value;
                    button = buttonInput.SelectedItem.ToString();
                });

                MouseClicker.ClickAt(x, y, button);

                int actualDelay = GetNextClickDelay(delay, variability);
                int slept = 0;
                while (isRunning && slept < actualDelay)
                {
                    Thread.Sleep(Math.Min(25, actualDelay - slept));
                    slept += 25;
                }
            }
        }

        private int GetNextClickDelay(int baseDelay, int variability)
        {
            if (variability <= 0)
                return Math.Max(1, baseDelay);

            // Recalculate before every click wait so the cadence is not fixed.
            int offset = delayRandom.Next(-variability, variability + 1);
            return Math.Max(1, baseDelay + offset);
        }

        private void UpdateStatus()
        {
            toggleButton.Text = isRunning ? "Stop (F6)" : "Start (F6)";
            statusLabel.Text = isRunning
                ? "Running. Press F6 to stop."
                : "Stopped. F6 starts/stops, F7 sets location.";
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_TOGGLE)
                    ToggleClicking();
                else if (id == HOTKEY_SET_LOCATION)
                    SetClickPoint(Cursor.Position);
            }

            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopClicking();
            marker.Close();
            UnregisterHotKey(Handle, HOTKEY_TOGGLE);
            UnregisterHotKey(Handle, HOTKEY_SET_LOCATION);
            base.OnFormClosing(e);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }

    sealed class TargetMarker : Form
    {
        private const int MARKER_SIZE = 28;

        public event EventHandler<Point> LocationChangedByDrag;
        private Point dragOffset;
        private bool dragging;

        public TargetMarker()
        {
            Text = "Click Target";
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(MARKER_SIZE, MARKER_SIZE);
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Cursor = Cursors.SizeAll;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
        }

        public void SetCenter(Point point)
        {
            Location = new Point(point.X - Width / 2, point.Y - Height / 2);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int center = MARKER_SIZE / 2;
            int circlePadding = 5;

            using (Pen shadow = new Pen(Color.FromArgb(140, 0, 0, 0), 3))
            using (Pen markerPen = new Pen(Color.FromArgb(235, 38, 48), 1.6F))
            {
                e.Graphics.DrawEllipse(shadow, circlePadding, circlePadding, MARKER_SIZE - circlePadding * 2, MARKER_SIZE - circlePadding * 2);
                e.Graphics.DrawLine(shadow, center, 3, center, MARKER_SIZE - 3);
                e.Graphics.DrawLine(shadow, 3, center, MARKER_SIZE - 3, center);

                e.Graphics.DrawEllipse(markerPen, circlePadding, circlePadding, MARKER_SIZE - circlePadding * 2, MARKER_SIZE - circlePadding * 2);
                e.Graphics.DrawLine(markerPen, center, 3, center, MARKER_SIZE - 3);
                e.Graphics.DrawLine(markerPen, 3, center, MARKER_SIZE - 3, center);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            dragging = true;
            dragOffset = e.Location;
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
            base.OnMouseUp(e);
        }
    }

    static class MouseClicker
    {
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;

        public static void ClickAt(int x, int y, string button)
        {
            SetCursorPos(x, y);
            Thread.Sleep(10);

            int down;
            int up;
            switch (button)
            {
                case "Right":
                    down = MOUSEEVENTF_RIGHTDOWN;
                    up = MOUSEEVENTF_RIGHTUP;
                    break;
                case "Middle":
                    down = MOUSEEVENTF_MIDDLEDOWN;
                    up = MOUSEEVENTF_MIDDLEUP;
                    break;
                default:
                    down = MOUSEEVENTF_LEFTDOWN;
                    up = MOUSEEVENTF_LEFTUP;
                    break;
            }

            mouse_event(down, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            mouse_event(up, 0, 0, 0, UIntPtr.Zero);
        }

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
    }
}
