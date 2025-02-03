using System.IO.Ports;
namespace Protractor
{
    /// <summary>
    /// Модальное окно для выбора COM-порта и Baud rate.
    /// Предназначено для настройки параметров подключения (для моторов или датчика).
    /// </summary>
    public class ComPortSelectorForm : Form
    {
        private ListBox portsListBox;         // Список доступных COM-портов.
        private ComboBox baudRateComboBox;      // Выпадающий список с вариантами скорости.
        private Button okButton;              // Кнопка подтверждения выбора.
        private Button cancelButton;          // Кнопка отмены.
        private System.Windows.Forms.Timer refreshTimer; // Таймер для периодического обновления списка портов.

        /// <summary>
        /// Выбранный COM-порт.
        /// </summary>
        public string SelectedPort { get; private set; }
        /// <summary>
        /// Выбранный Baud rate.
        /// </summary>
        public int SelectedBaudRate { get; private set; }

        /// <summary>
        /// Конструктор формы выбора COM-порта.
        /// </summary>
        /// <param name="currentPort">Текущий выбранный порт (если есть).</param>
        /// <param name="currentBaudRate">Текущая скорость (если задана).</param>
        public ComPortSelectorForm(string currentPort, int currentBaudRate)
        {
            this.Text = "Выбор COM-порта и Baud rate";
            this.Size = new Size(300, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            InitializeComponents();

            if (!string.IsNullOrEmpty(currentPort))
                SelectedPort = currentPort;
            SelectedBaudRate = currentBaudRate;

            // Если задана текущая скорость, устанавливаем выбранный элемент
            baudRateComboBox.SelectedItem = currentBaudRate > 0 ? currentBaudRate.ToString() : null;
        }

        /// <summary>
        /// Инициализация компонентов формы.
        /// Создает список портов, выпадающий список для Baud rate и кнопки управления.
        /// </summary>
        private void InitializeComponents()
        {
            // Список доступных COM-портов
            portsListBox = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 200,
                Font = new Font("Arial", 12)
            };
            this.Controls.Add(portsListBox);

            // Выпадающий список для выбора скорости
            baudRateComboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 12)
            };
            baudRateComboBox.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
            this.Controls.Add(baudRateComboBox);

            // Панель для кнопок OK и Отмена
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Left,
                Width = 140
            };
            okButton.Click += OkButton_Click;
            buttonPanel.Controls.Add(okButton);

            cancelButton = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Right,
                Width = 140
            };
            buttonPanel.Controls.Add(cancelButton);
            this.Controls.Add(buttonPanel);

            // Таймер для обновления списка COM-портов каждые 2 секунды
            refreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();

            // Первоначальное обновление списка портов
            RefreshPorts();
        }

        /// <summary>
        /// Обработчик таймера для обновления списка доступных COM-портов.
        /// </summary>
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshPorts();
        }

        /// <summary>
        /// Обновляет список COM-портов, сортируя их по имени.
        /// </summary>
        private void RefreshPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            portsListBox.Items.Clear();
            portsListBox.Items.AddRange(ports);

            if (!string.IsNullOrEmpty(SelectedPort))
            {
                int index = portsListBox.Items.IndexOf(SelectedPort);
                if (index >= 0)
                    portsListBox.SelectedIndex = index;
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку OK.
        /// Проверяет корректность выбранных значений и сохраняет их.
        /// </summary>
        private void OkButton_Click(object sender, EventArgs e)
        {
            if (portsListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите COM порт.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }
            SelectedPort = portsListBox.SelectedItem.ToString();

            if (baudRateComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите Baud rate.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }
            if (!int.TryParse(baudRateComboBox.SelectedItem.ToString(), out int baudRate))
            {
                MessageBox.Show("Некорректное значение Baud rate.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }
            SelectedBaudRate = baudRate;
        }

        /// <summary>
        /// Переопределенный метод закрытия формы.
        /// Останавливает и освобождает таймер обновления списка портов.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Tick -= RefreshTimer_Tick;
                refreshTimer.Dispose();
                refreshTimer = null;
            }
            base.OnFormClosed(e);
        }
    }
}
