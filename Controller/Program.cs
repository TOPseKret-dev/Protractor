using System.Drawing.Imaging;
using System.IO.Ports;
using Controller; // Для доступа к классу DrillAngleDetector

namespace Protractor
{
    /// <summary>
    /// Главная форма приложения, реализующая пользовательский интерфейс и обработку данных с камеры и COM-портов.
    /// </summary>
    public class MainForm : Form
    {
        // Элементы интерфейса:
        private PictureBox cameraView;               // Панель для отображения изображения с камеры.
        private Label sensorDataLabel;               // Метка для отображения данных с линейки (оси X, Z и поворот).
        private Label currentSpeedLabel;             // Метка для отображения текущей скорости.
        private Label angleNotFoundLabel;            // Метка для индикации отсутствия обнаруженного угла.
        private TrackBar speedSelector;              // Ползунок для выбора скорости.
        private Button comMotorSettingsButton;       // Кнопка для настройки COM-порта моторов.
        private Button comSensorSettingsButton;      // Кнопка для настройки COM-порта датчика (линейки).

        // Таймеры для периодических операций:
        private System.Windows.Forms.Timer sensorTimer;    // Таймер для чтения данных с линейки (COM порт датчика).
        private System.Windows.Forms.Timer cameraCheckTimer; // Таймер для проверки подключения камеры.

        // COM-порты для управления моторами и датчиком:
        private SerialPort motorSerialPort = null;   // COM-порт для моторов.
        private SerialPort sensorSerialPort = null;  // COM-порт для датчика (линейки).

        private Toupcam cameraHandle;                // Дескриптор камеры для захвата изображения.

        /// <summary>
        /// Конструктор формы. Выполняет инициализацию компонентов и подписку на событие загрузки формы.
        /// </summary>
        public MainForm()
        {
            InitializeComponents();
            this.Load += MainForm_Load;
        }

        /// <summary>
        /// Обработчик события загрузки формы. Инициализирует камеру и запускает проверку подключения.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeCamera();
            // Если камера не подключена, запускаем таймер для периодической проверки подключения.
            if (cameraHandle == null)
            {
                cameraCheckTimer.Start();
            }
        }

        /// <summary>
        /// Метод инициализации всех компонентов формы и элементов управления.
        /// </summary>
        private void InitializeComponents()
        {
            // Настройка основных параметров формы
            this.Text = "Protractor - Панель контроля";
            this.Size = new Size(1145, 700);
            this.MaximizeBox = true;

            // Создаем главный Layout
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

            // Создание левой панели управления
            Panel controlPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.LightGray
            };
            this.Controls.Add(controlPanel);

            // Кнопка для настройки COM-порта моторов
            comMotorSettingsButton = new Button
            {
                Text = "Настройка COM моторов",
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightCyan
            };
            comMotorSettingsButton.Click += ComMotorSettingsButton_Click;
            controlPanel.Controls.Add(comMotorSettingsButton);

            // Кнопка для настройки COM-порта датчика (линейки)
            comSensorSettingsButton = new Button
            {
                Text = "Настройка COM линейки",
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightCyan
            };
            comSensorSettingsButton.Click += ComSensorSettingsButton_Click;
            controlPanel.Controls.Add(comSensorSettingsButton);

            // Метка для отображения данных с датчика (линейки)
            sensorDataLabel = new Label
            {
                Text = "X: 0, Z: 0, Поворот: 0",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            controlPanel.Controls.Add(sensorDataLabel);

            // Метка для отображения текущей скорости
            currentSpeedLabel = new Label
            {
                Text = "Текущая скорость: 50",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            controlPanel.Controls.Add(currentSpeedLabel);

            // Метка, отображающая отсутствие обнаруженного угла
            angleNotFoundLabel = new Label
            {
                Text = "Угол не найден",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Red,
                Visible = false
            };
            controlPanel.Controls.Add(angleNotFoundLabel);

            // Панель для настройки скорости с использованием TrackBar
            Panel speedPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.LightGray
            };
            Label speedLabel = new Label
            {
                Text = "Настройка скорости:",
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

            // Добавление панелей с кнопками управления для осей и поворота
            Panel xPanel = CreateHorizontalButtonPanel("Двигать X-", "Двигать X+", MoveXNegative_Click, MoveXPositive_Click);
            controlPanel.Controls.Add(xPanel);
            Panel zPanel = CreateHorizontalButtonPanel("Двигать Z-", "Двигать Z+", MoveZNegative_Click, MoveZPositive_Click);
            controlPanel.Controls.Add(zPanel);
            Panel rotationPanel = CreateHorizontalButtonPanel("Поворот -", "Поворот +", RotateNegative_Click, RotatePositive_Click);
            controlPanel.Controls.Add(rotationPanel);

            mainLayout.Controls.Add(controlPanel, 0, 0);

            // Создание правой панели для отображения изображения с камеры
            cameraView = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom // Сохранение соотношения сторон изображения
            };
            mainLayout.Controls.Add(cameraView, 1, 0);

            // Изначально COM-порты не настроены – их выберет пользователь
            motorSerialPort = null;
            sensorSerialPort = null;

            // Инициализация таймера для чтения данных с датчика (интервал 1 секунда)
            sensorTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            sensorTimer.Tick += SensorTimer_Tick;
            sensorTimer.Start();

            // Инициализация таймера для проверки подключения камеры (интервал 2 секунды)
            cameraCheckTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            cameraCheckTimer.Tick += CameraCheckTimer_Tick;
        }

        /// <summary>
        /// Создает панель с двумя кнопками для горизонтального управления.
        /// </summary>
        /// <param name="textNegative">Надпись на кнопке с отрицательным значением.</param>
        /// <param name="textPositive">Надпись на кнопке с положительным значением.</param>
        /// <param name="negativeHandler">Обработчик для кнопки с отрицательным значением.</param>
        /// <param name="positiveHandler">Обработчик для кнопки с положительным значением.</param>
        /// <returns>Панель с двумя кнопками.</returns>
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
        /// Обработчик нажатия на кнопку настройки COM-порта моторов.
        /// Открывает модальное окно для выбора порта и скорости.
        /// </summary>
        private void ComMotorSettingsButton_Click(object sender, EventArgs e)
        {
            using (ComPortSelectorForm comForm = new ComPortSelectorForm("", 0))
            {
                if (comForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Если порт уже открыт, закрываем его перед повторной настройкой
                        if (motorSerialPort != null && motorSerialPort.IsOpen)
                            motorSerialPort.Close();

                        motorSerialPort = new SerialPort(comForm.SelectedPort, comForm.SelectedBaudRate);
                        motorSerialPort.Open();
                        MessageBox.Show($"Выбран моторный порт: {motorSerialPort.PortName} со скоростью: {motorSerialPort.BaudRate}",
                            "Настройка COM моторов", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка открытия моторного COM порта: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку настройки COM-порта датчика (линейки).
        /// Открывает модальное окно для выбора порта и скорости.
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
                        MessageBox.Show($"Выбран порт линейки: {sensorSerialPort.PortName} со скоростью: {sensorSerialPort.BaudRate}",
                            "Настройка COM линейки", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка открытия COM порта линейки: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик изменения значения ползунка скорости.
        /// Обновляет метку с текущей скоростью.
        /// </summary>
        private void SpeedSelector_ValueChanged(object sender, EventArgs e)
        {
            currentSpeedLabel.Text = $"Текущая скорость: {speedSelector.Value}";
        }

        // Обработчики для команд управления моторами (движение по осям и поворот)
        private void MoveXNegative_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 X{-speedSelector.Value}"); }
        private void MoveXPositive_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 X{speedSelector.Value}"); }
        private void MoveZNegative_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Z{-speedSelector.Value}"); }
        private void MoveZPositive_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Z{speedSelector.Value}"); }
        private void RotateNegative_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Y{-speedSelector.Value}"); }
        private void RotatePositive_Click(object sender, EventArgs e) { SendCommandToMotor($"G01 Y{speedSelector.Value}"); }

        /// <summary>
        /// Отправляет команду на управление мотором через настроенный COM-порт.
        /// Если порт не настроен, выводит сообщение об ошибке.
        /// </summary>
        /// <param name="command">Команда в строковом формате.</param>
        private void SendCommandToMotor(string command)
        {
            if (motorSerialPort == null || !motorSerialPort.IsOpen)
            {
                MessageBox.Show("Моторный COM порт не настроен!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                motorSerialPort.WriteLine(command);
                Console.WriteLine("Команда отправлена: " + command);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка отправки команды: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Метод анализа изображения с камеры и детекции угла.
        /// Если камера подключена, захватывает изображение, обрабатывает его через метод детекции угла
        /// и обновляет изображение на PictureBox, а также индицирует наличие угла.
        /// </summary>
        private void AnalyzeImageAndDetectAngle()
        {
            if (cameraHandle == null)
            {
                Console.WriteLine("Камера не подключена.");
                return;
            }

            Bitmap bitmap = null;
            bool angleFound = false;

            try
            {
                // Создаем Bitmap для захвата изображения заданного разрешения и формата
                bitmap = new Bitmap(2048, 1534, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                       ImageLockMode.WriteOnly, bitmap.PixelFormat);
                try
                {
                    // Захват изображения с камеры. Если успешно, то обрабатываем изображение.
                    if (cameraHandle.PullImage(bmpData.Scan0, 0, 24, bmpData.Stride, out Toupcam.FrameInfoV4 info))
                    {
                        Console.WriteLine($"Изображение получено: {info.v3.width}x{info.v3.height}");
                        // Вызов метода детекции угла из класса DrillAngleDetector
                        Bitmap processedImage = DrillAngleDetector.Detect((Bitmap)bitmap.Clone(), bmpData, out angleFound);
                        // Обновление PictureBox в потоке UI
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
                        Console.WriteLine("Не удалось получить изображение с камеры.");
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка захвата изображения: " + ex.Message);
            }
            finally
            {
                bitmap?.Dispose();
            }
        }

        /// <summary>
        /// Инициализация камеры. Пытается открыть камеру и запустить режим Pull.
        /// В случае ошибки выводит сообщение об ошибке.
        /// </summary>
        private void InitializeCamera()
        {
            cameraHandle = Toupcam.Open(null);
            if (cameraHandle == null)
            {
                MessageBox.Show("Не удалось открыть камеру. Проверьте подключение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!cameraHandle.StartPullModeWithCallback(evt => DelegateOnEventCallback(evt)))
            {
                MessageBox.Show("Не удалось запустить камеру в режиме Pull.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cameraHandle.Close();
                cameraHandle = null;
            }
            else
            {
                MessageBox.Show("Камера подключена успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Обработчик событий камеры. В зависимости от типа события вызывает соответствующие методы.
        /// </summary>
        /// <param name="evt">Тип события камеры.</param>
        private void DelegateOnEventCallback(Toupcam.eEVENT evt)
        {
            if (!IsHandleCreated)
            {
                Console.WriteLine("Дескриптор окна еще не создан, событие пропущено.");
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
                        MessageBox.Show("Произошла ошибка камеры.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case Toupcam.eEVENT.EVENT_DISCONNECTED:
                        MessageBox.Show("Камера была отключена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    default:
                        break;
                }
            }));
        }

        /// <summary>
        /// Таймер для проверки подключения камеры. Если камера не подключена, пытается открыть её.
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
                        MessageBox.Show("Камера подключена успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        /// Переопределенный метод закрытия формы. Закрывает камеру и COM-порты, если они открыты.
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
        /// Точка входа в приложение.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        /// <summary>
        /// Таймер для чтения данных с датчика (COM порт датчика).
        /// Ожидается строка формата "X Y Z" с дробными значениями. При ошибке выводит 0.
        /// </summary>
        private void SensorTimer_Tick(object sender, EventArgs e)
        {
            if (sensorSerialPort != null && sensorSerialPort.IsOpen)
            {
                try
                {
                    string line = sensorSerialPort.ReadLine();
                    // Предполагается, что данные приходят в формате "X Y Z"
                    string[] parts = line.Trim().Split(' ');
                    if (parts.Length >= 3 &&
                        float.TryParse(parts[0], out float x) &&
                        float.TryParse(parts[1], out float angle) &&  // Y – угол
                        float.TryParse(parts[2], out float z))
                    {
                        sensorDataLabel.Text = $"X: {x:F2}, Z: {z:F2}, Поворот: {angle:F2}";
                        return;
                    }
                }
                catch { /* Игнорируем ошибки чтения данных */ }
            }
            // Если порт не настроен или данные не прочитаны – выводим значения по умолчанию.
            sensorDataLabel.Text = "X: 0, Z: 0, Поворот: 0";
        }
    }
}
