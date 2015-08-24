using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SerialChart
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        System.Timers.Timer tim;
        Thread rxThread;
        Random rnd;
        bool running;
        StreamWriter log;

        ObservableCollection<KeyValuePair<DateTime, float>> points;
        public ObservableCollection<KeyValuePair<DateTime, float>> Points
        {
            get
            {
                return points;
            }
            set
            {
                points = value;
            }
        }

        SerialPort port;
        public SerialPort Port
        {
            get
            {
                return port;
            }

            set
            {
                port = value;
            }
        }

        string buttonContent;
        public string ButtonContent
        {
            get
            {
                return buttonContent;
            }

            set
            {
                buttonContent = value;
                OnPropertyChanged("ButtonContent");
            }
        }

        double stepY = 0.25;
        public double StepY
        {
            get
            {
                return stepY;
            }

            set
            {
                stepY = value;
                OnPropertyChanged("StepY");
            }
        }

        int sizeX = 10;
        public int SizeX
        {
            get
            {
                return sizeX;
            }

            set
            {
                sizeX = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        public MainWindow()
        {
            rnd = new Random();
            points = new ObservableCollection<KeyValuePair<DateTime, float>>();
            port = new SerialPort();
            var ports = SerialPort.GetPortNames();
            if (ports.Length > 0) port.PortName = ports.First();
            log = new StreamWriter(string.Format("log_{0}.txt", DateTime.Now.ToString("yyMMdd_HHmm")), true);
            buttonContent = "Open";
            InitializeComponent();
            //tim = new System.Timers.Timer(100);
            //tim.Elapsed += Tim_Elapsed;
            //tim.Start();
            running = true;
            rxThread = new Thread(new ThreadStart(RxTask));
            rxThread.Start();
        }

        private void AddPoint(KeyValuePair<DateTime, float> point)
        {
            if (Dispatcher.HasShutdownStarted == false)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Points.Add(point);
                    if (points.Count > 1)
                    {
                        while((points.Last().Key.Ticks - points.First().Key.Ticks) / 10e6 > SizeX)
                        {
                            points.RemoveAt(0);
                        }
                    }
                    log?.WriteLine(string.Format("[{0}]: {1}", point.Key.ToString("HH:mm:ss.fff"), point.Value));
                }));
            }
        }

        private void Tim_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            AddPoint(new KeyValuePair<DateTime, float>(DateTime.Now, (rnd.Next() % 400) / 10.0f + 5));
        }

        private void RxTask()
        {
            try
            {
                while (running)
                {
                    if (Port != null && Port.IsOpen && Port.BytesToRead > 0)
                    {
                        string line = Port.ReadLine().Replace(',', '.').TrimEnd('\r', '\n', ' ');
                        float value;
                        if (float.TryParse(line, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
                        {
                            AddPoint(new KeyValuePair<DateTime, float>(DateTime.Now, value));
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            catch
            {
                if (Dispatcher.HasShutdownStarted == false)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Close();
                    }));
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            running = false;
            port?.Close();
            log?.Close();
            log = null;
        }

        private void openClick(object sender, RoutedEventArgs e)
        {
            if (port.IsOpen)
            {
                port.Close();
                ButtonContent = "Open";
            }
            else
            {
                port.Open();
                ButtonContent = "Close";
            }
        }
    }
}
