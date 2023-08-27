using System;
using System.Windows;
using System.Timers;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            GetWeather();
            SetWindowBottommost();
            LoadWindowPosition();
            CheckAndAddToAutostart();
            Closing += MainWindow_Closing;
            Activated += MainWindow_Activated;
            updateTimer = new Timer(1000);
            updateTimer.Elapsed += UpdateTimerElapsed;
            updateTimer.Start();
        }
    }
}
