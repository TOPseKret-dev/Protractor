using System.Drawing.Imaging;
using System.IO.Ports;
using Controller; // ��� ������� � ������ DrillAngleDetector

namespace Protractor
{
    /// <summary>
    /// ������� ����� ����������, ����������� ���������������� ��������� � ��������� ������ � ������ � COM-������.
    /// </summary>
    public class MainForm : Form
    {
        // �������� ����������:
        private PictureBox cameraView;               // ������ ��� ����������� ����������� � ������.
        private Label sensorDataLabel;               // ����� ��� ����������� ������ � ������� (��� X, Z � �������).
        private Label currentSpeedLabel;             // ����� ��� ����������� ������� ��������.
        private Label angleNotFoundLabel;            // ����� ��� ��������� ���������� ������������� ����.
        private TrackBar speedSelector;              // �������� ��� ������ ��������.
        private Button comMotorSettingsButton;       // ������ ��� ��������� COM-����� �������.
        private Button comSensorSettingsButton;      // ������ ��� ��������� COM-����� ������� (�������).

        // ������� ��� ������������� ��������:
        private System.Windows.Forms.Timer sensorTimer;    // ������ ��� ������ ������ � ������� (COM ���� �������).
        private System.Windows.Forms.Timer cameraCheckTimer; // ������ ��� �������� ����������� ������.

        // COM-����� ��� ���������� �������� � ��������:
        private SerialPort motorSerialPort = null;   // COM-���� ��� �������.
        private SerialPort sensorSerialPort = null;  // COM-���� ��� ������� (�������).

        private Toupcam cameraHandle;                // ���������� ������ ��� ������� �����������.

        /// <summary>
        /// ����������� �����. ��������� ������������� ����������� � �������� �� ������� �������� �����.
        /// </summary>
        public MainForm()
        {
            InitializeComponents();
            this.Load += MainForm_Load;
        }

        /// <summary>
        /// ���������� ������� �������� �����. �������������� ������ � ��������� �������� �����������.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeCamera();
            // ���� ������ �� ����������, ��������� ������ ��� ������������� �������� �����������.
            if (cameraHandle == null)
            {
                cameraCheckTimer.Start();
            }
        }

        /// <summary>
        /// ����� ������������� ���� ����������� ����� � ��������� ����������.
        /// </summary>
        private void InitializeComponents()
        {
            // ��������� �������� ���������� �����
            this.Text = "Protractor - ������ ��������";
            this.Size = new Size(1145, 700);
            this.MaximizeBox = true;

            // ������� ������� Layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.Controls.Add(mainLayout);

            // �������� ����� ������ ����������
            Panel controlPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.LightGray
            };
            this.Controls.Add(controlPanel);

            // ������ ��� ��������� COM-����� �������
            comMotorSettingsButton = new Button
            {
                Text = "��������� COM �������",
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightCyan
            };
            comMotorSettingsButton.Click += ComMotorSettingsButton_Click;
            controlPanel.Controls.Add(comMotorSettingsButton);

            // ������ ��� ��������� COM-����� ������� (�������)
            comSensorSettingsButton = new Button
            {
                Text = "��������� COM �������",
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightCyan
            };
            comSensorSettingsButton.Click += ComSensorSettingsButton_Click;
            controlPanel.Controls.Add(comSensorSettingsButton);

            // ����� ��� ����������� ������ � ������� (�������)
            sensorDataLabel = new Label
            {
                Text = "X: 0, Z: 0, �������: 0",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            controlPanel.Controls.Add(sensorDataLabel);

            // ����� ��� ����������� ������� ��������
            currentSpeedLabel = new Label
            {
                Text = "������� ��������: 50",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            controlPanel.Controls.Add(currentSpeedLabel);

            // �����, ������������ ���������� ������������� ����
            angleNotFoundLabel = new Label
            {
                Text = "���� �� ������",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Red,
                Visible = false
            };
            controlPanel.Controls.Add(angleNotFoundLabel);

            // ������ ��� ��������� �������� � �������������� TrackBar
            Panel speedPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.LightGray
            };
            Label speedLabel = new Label
            {
                Text = "��������� ��������:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 10, FontStyle.Regular)
            };
            speedPanel.Controls.Add(speedLabel);

            speedSelector = new TrackBar
            {
                Minimum = 1,
                Maximum = 100,
                Value = 50,
                Dock = DockStyle.Bottom,
                Height = 40,
                TickFrequency = 10,
                BackColor = Color.White,
                LargeChange = 5,
                SmallChange = 1
            };
            speedSelector.ValueChanged += SpeedSelector_ValueChanged;
            speedPanel.Controls.Add(speedSelector);
            controlPanel.Controls.Add(speedPanel);

            // ���������� ������� � �������� ���������� ��� ���� � ��������
            Panel xPanel = CreateHorizontalButtonPanel("������� X-", "������� X+", MoveXNegative_Click, MoveXPositive_Click);
            controlPanel.Controls.Add(xPanel);
            Panel zPanel = CreateHorizontalButtonPanel("������� Z-", "������� Z+", MoveZNegative_Click, MoveZPositive_Click);
            controlPanel.Controls.Add(zPanel);
            Panel rotationPanel = CreateHorizontalButtonPanel("������� -", "������� +", RotateNegative_Click, RotatePositive_Click);
            controlPanel.Controls.Add(rotationPanel);

            mainLayout.Controls.Add(controlPanel, 0, 0);

            // �������� ������ ������ ��� ����������� ����������� � ������
            cameraView = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom // ���������� ����������� ������ �����������
            };
            mainLayout.Controls.Add(cameraView, 1, 0);

            // ���������� COM-����� �� ��������� � �� ������� ������������
            motorSerialPort = null;
            sensorSerialPort = null;

            // ������������� ������� ��� ������ ������ � ������� (�������� 1 �������)
            sensorTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            sensorTimer.Tick += SensorTimer_Tick;
            sensorTimer.Start();

            // ������������� ������� ��� �������� ����������� ������ (�������� 2 �������)
            cameraCheckTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            cameraCheckTimer.Tick += CameraCheckTimer_Tick;
        }

        /// <summary>
        /// ������� ������ � ����� �������� ��� ��������������� ����������.
        /// </summary>
        /// <param name="textNegative">������� �� ������ � ������������� ���������.</param>
        /// <param name="textPositive">������� �� ������ � ������������� ���������.</param>
        /// <param name="negativeHandler">���������� ��� ������ � ������������� ���������.</param>
        /// <param name="positiveHandler">���������� ��� ������ � ������������� ���������.</param>
        /// <returns>������ � ����� ��������.</returns>
        private Panel CreateHorizontalButtonPanel(string textNegative, string textPositive, EventHandler negativeHandler, EventHandler positiveHandler)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.WhiteSmoke
            };

            Button btnNegative = new Button
            {
                Text = textNegative,
                Dock = DockStyle.Left,
                Width = 120,
                BackColor = Color.LightCoral,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnNegative.Click += negativeHandler;
            panel.Controls.Add(btnNegative);

            Button btnPositive = new Button
            {
                Text = textPositive,
                Dock = DockStyle.Right,
                Width = 120,
                BackColor = Color.LightBlue,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnPositive.Click += positiveHandler;
            panel.Controls.Add(btnPositive);

            return panel;
        }

        /// <summary>
        /// ���������� ������� �� ������ ��������� COM-����� �������.
        /// ��������� ��������� ���� ��� ������ ����� � ��������.
        /// </summary>
        private void ComMotorSettingsButton_Click(object sender, EventArgs e)
        {
            using (ComPortSelectorForm comForm = new ComPortSelectorForm("", 0))
            {
                if (comForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // ���� ���� ��� ������, ��������� ��� ����� ��������� ����������
                        if (motorSerialPort != null && motorSerialPort.IsOpen)
                            motorSerialPort.Close();

                        motorSerialPort = new SerialPort(comForm.SelectedPort, comForm.SelectedBaudRate);
                        motorSerialPort.Open();
                        MessageBox.Show($"������ �������� ����: {motorSerialPort.PortName} �� ���������: {motorSerialPort.BaudRate}",
                            "��������� COM �������", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("������ �������� ��������� COM �����: " + ex.Message, "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// ���������� ������� �� ������ ��������� COM-����� ������� (�������).
        /// ��������� ��������� ���� ��� ������ ����� � ��������.
        /// </summary>
        private void ComSensorSettingsButton_Click(object sender, EventArgs e)
        {
            using (ComPortSelectorForm comForm = new ComPortSelectorForm("", 0))
            {
                if (comForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (sensorSerialPort != null && sensorSerialPort.IsOpen)
                            sensorSerialPort.Close();

                        sensorSerialPort = new SerialPort(comForm.SelectedPort, comForm.SelectedBaudRate);
                        sensorSerialPort.NewLine = "\n";
                        sensorSerialPort.Open();
                        MessageBox.Show($"������ ���� �������: {sensorSerialPort.PortName} �� ���������: {sensorSerialPort.BaudRate}",
                            "��������� COM �������", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("������ �������� COM ����� �������: " + ex.Message, "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// ���������� ��������� �������� �������� ��������.
        /// ��������� ����� � ������� ���������.
        /// </summary>
        private void SpeedSelector_ValueChanged(object sender, EventArgs e)
        {
            currentSpeedLabel.Text = $"������� ��������: {speedSelector.Value}";
        }

        // ����������� ��� ������ ���������� �������� (�������� �� ���� � �������)
        private void MoveXNegative_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 X{-speedSelector.Value}"); }
        private void MoveXPositive_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 X{speedSelector.Value}"); }
        private void MoveZNegative_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Z{-speedSelector.Value}"); }
        private void MoveZPositive_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Z{speedSelector.Value}"); }
        private void RotateNegative_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Y{-speedSelector.Value}"); }
        private void RotatePositive_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Y{speedSelector.Value}"); }

        /// <summary>
        /// ���������� ������� �� ���������� ������� ����� ����������� COM-����.
        /// ���� ���� �� ��������, ������� ��������� �� ������.
        /// </summary>
        /// <param name="command">������� � ��������� �������.</param>
        private void SendCommandToMotor(string command)
        {
            if (motorSerialPort == null || !motorSerialPort.IsOpen)
            {
                MessageBox.Show("�������� COM ���� �� ��������!", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                motorSerialPort.WriteLine(command);
                Console.WriteLine("������� ����������: " + command);
            }
            catch (Exception ex)
            {
                MessageBox.Show("������ �������� �������: " + ex.Message, "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ����� ������� ����������� � ������ � �������� ����.
        /// ���� ������ ����������, ����������� �����������, ������������ ��� ����� ����� �������� ����
        /// � ��������� ����������� �� PictureBox, � ����� ���������� ������� ����.
        /// </summary>
        private void AnalyzeImageAndDetectAngle()
        {
            if (cameraHandle == null)
            {
                Console.WriteLine("������ �� ����������.");
                return;
            }

            Bitmap bitmap = null;
            bool angleFound = false;

            try
            {
                // ������� Bitmap ��� ������� ����������� ��������� ���������� � �������
                bitmap = new Bitmap(2048, 1534, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                       ImageLockMode.WriteOnly, bitmap.PixelFormat);
                try
                {
                    // ������ ����������� � ������. ���� �������, �� ������������ �����������.
                    if (cameraHandle.PullImage(bmpData.Scan0, 0, 24, bmpData.Stride, out Toupcam.FrameInfoV4 info))
                    {
                        Console.WriteLine($"����������� ��������: {info.v3.width}x{info.v3.height}");
                        // ����� ������ �������� ���� �� ������ DrillAngleDetector
                        Bitmap processedImage = DrillAngleDetector.Detect((Bitmap)bitmap.Clone(), bmpData, out angleFound);
                        // ���������� PictureBox � ������ UI
                        cameraView.Invoke((MethodInvoker)(() =>
                        {
                            cameraView.Image?.Dispose();
                            cameraView.Image = processedImage;
                            cameraView.Refresh();
                            angleNotFoundLabel.Visible = !angleFound;
                        }));
                    }
                    else
                    {
                        Console.WriteLine("�� ������� �������� ����������� � ������.");
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("������ ������� �����������: " + ex.Message);
            }
            finally
            {
                bitmap?.Dispose();
            }
        }

        /// <summary>
        /// ������������� ������. �������� ������� ������ � ��������� ����� Pull.
        /// � ������ ������ ������� ��������� �� ������.
        /// </summary>
        private void InitializeCamera()
        {
            cameraHandle = Toupcam.Open(null);
            if (cameraHandle == null)
            {
                MessageBox.Show("�� ������� ������� ������. ��������� �����������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!cameraHandle.StartPullModeWithCallback(evt => DelegateOnEventCallback(evt)))
            {
                MessageBox.Show("�� ������� ��������� ������ � ������ Pull.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cameraHandle.Close();
                cameraHandle = null;
            }
            else
            {
                MessageBox.Show("������ ���������� �������!", "�����", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// ���������� ������� ������. � ����������� �� ���� ������� �������� ��������������� ������.
        /// </summary>
        /// <param name="evt">��� ������� ������.</param>
        private void DelegateOnEventCallback(Toupcam.eEVENT evt)
        {
            if (!IsHandleCreated)
            {
                Console.WriteLine("���������� ���� ��� �� ������, ������� ���������.");
                return;
            }
            BeginInvoke((Action)(() =>
            {
                if (cameraHandle == null)
                    return;
                switch (evt)
                {
                    case Toupcam.eEVENT.EVENT_IMAGE:
                        AnalyzeImageAndDetectAngle();
                        break;
                    case Toupcam.eEVENT.EVENT_ERROR:
                        MessageBox.Show("��������� ������ ������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case Toupcam.eEVENT.EVENT_DISCONNECTED:
                        MessageBox.Show("������ ���� ���������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    default:
                        break;
                }
            }));
        }

        /// <summary>
        /// ������ ��� �������� ����������� ������. ���� ������ �� ����������, �������� ������� �.
        /// </summary>
        private void CameraCheckTimer_Tick(object sender, EventArgs e)
        {
            if (cameraHandle == null)
            {
                cameraHandle = Toupcam.Open(null);
                if (cameraHandle != null)
                {
                    if (cameraHandle.StartPullModeWithCallback(evt => DelegateOnEventCallback(evt)))
                    {
                        cameraCheckTimer.Stop();
                        MessageBox.Show("������ ���������� �������!", "�����", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        cameraHandle.Close();
                        cameraHandle = null;
                    }
                }
            }
        }

        /// <summary>
        /// ���������������� ����� �������� �����. ��������� ������ � COM-�����, ���� ��� �������.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (cameraHandle != null)
                cameraHandle.Close();
            if (motorSerialPort != null && motorSerialPort.IsOpen)
                motorSerialPort.Close();
            if (sensorSerialPort != null && sensorSerialPort.IsOpen)
                sensorSerialPort.Close();
        }

        /// <summary>
        /// ����� ����� � ����������.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        /// <summary>
        /// ������ ��� ������ ������ � ������� (COM ���� �������).
        /// ��������� ������ ������� "X Y Z" � �������� ����������. ��� ������ ������� 0.
        /// </summary>
        private void SensorTimer_Tick(object sender, EventArgs e)
        {
            if (sensorSerialPort != null && sensorSerialPort.IsOpen)
            {
                try
                {
                    string line = sensorSerialPort.ReadLine();
                    // ��������������, ��� ������ �������� � ������� "X Y Z"
                    string[] parts = line.Trim().Split(' ');
                    if (parts.Length >= 3 &&
                        float.TryParse(parts[0], out float x) &&
                        float.TryParse(parts[1], out float angle) &&  // Y � ����
                        float.TryParse(parts[2], out float z))
                    {
                        sensorDataLabel.Text = $"X: {x:F2}, Z: {z:F2}, �������: {angle:F2}";
                        return;
                    }
                }
                catch { /* ���������� ������ ������ ������ */ }
            }
            // ���� ���� �� �������� ��� ������ �� ��������� � ������� �������� �� ���������.
            sensorDataLabel.Text = "X: 0, Z: 0, �������: 0";
        }
    }
}
