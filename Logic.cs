using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Diagnostics;
using System.Timers;
using Microsoft.Win32;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private Timer updateTimer;
        private const string ApiKey = "095afe9f59bd771963507ea19d7caddf";

        private const int HWND_BOTTOM = 1;
        private const int SWP_NOMOVE = 0x2;
        private const int SWP_NOSIZE = 0x1;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void CheckAndAddToAutostart()
        {
            bool isFirstRun = Properties.Settings.Default.FirstRun;

            if (isFirstRun)
            {
                Properties.Settings.Default.FirstRun = false;
                Properties.Settings.Default.Save();

                MessageBoxResult result = MessageBox.Show("Добавить программу в автозапуск?", "Автозапуск", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    AddToAutostart();
                }
            }
        }

        private void AddToAutostart()
        {
            try
            {
                string executablePath = Process.GetCurrentProcess().MainModule.FileName;
                string appName = Path.GetFileNameWithoutExtension(executablePath);

                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.SetValue(appName, executablePath);
                MessageBox.Show("Программа добавлена в автозапуск.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show($"Произошла ошибка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void LoadWindowPosition()
        {
            double x = Properties.Settings.Default.WindowPositionX;
            double y = Properties.Settings.Default.WindowPositionY;

            if (x >= 0 && y >= 0)
            {
                Left = x;
                Top = y;
            }
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            Properties.Settings.Default.WindowPositionX = Left;
            Properties.Settings.Default.WindowPositionY = Top;
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            SetWindowBottommost();
        }

        private void SetWindowBottommost()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void UpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DateTime currentTime = DateTime.Now;
                time.Text = currentTime.ToString("HH:mm");
            });
        }

        private string GetIconFileNameFromDescription(string weatherDescription)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan night = new TimeSpan(18, 0, 0);
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "weather_icons.json");
            JObject weatherIconsMapping = JObject.Parse(File.ReadAllText(jsonFilePath));

            if (weatherIconsMapping.TryGetValue(weatherDescription.ToLower(), out JToken iconNameToken))
            {
                if (currentTime.TimeOfDay >= night || iconNameToken.Value<string>() == "clear sky") { return "night.png"; }
                return iconNameToken.Value<string>();
            }

            return "clouds.png";
        }

        private async void GetWeather()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                    HttpResponseMessage responseGeo = await client.GetAsync("https://ipinfo.io?token=14fb71f7f766c0");
                    responseGeo.EnsureSuccessStatusCode();

                    string responseGeoBody = await responseGeo.Content.ReadAsStringAsync();
                    dynamic geoData = JObject.Parse(responseGeoBody);

                    string[] location = geoData.loc.ToString().Split(',');

                    string apiUrl = $"http://api.openweathermap.org/data/2.5/weather?lat={location[0]}&lon={location[1]}&appid={ApiKey}&units=metric";
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic weatherData = JObject.Parse(responseBody);

                    string weatherDescription = weatherData.weather[0].description;
                    double temperature = weatherData.main.temp;
                    string humid = weatherData.main.humidity;
                    string winds = weatherData.wind.speed;
                    string clouds = weatherData.clouds.all;

                    temp.Text = $"{Math.Round(temperature)}º";
                    status.Text = textInfo.ToTitleCase(weatherDescription);
                    wind.Text = $"{winds} km/h";
                    cloud.Text = $"{clouds} %";
                    humidity.Text = humid;
                    city.Text = textInfo.ToTitleCase(geoData.city.ToString());
                    string iconFileName = GetIconFileNameFromDescription(weatherDescription);
                    string assetsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                    string iconFilePath = Path.Combine(assetsFolderPath, iconFileName);
                    icondesc.Source = new BitmapImage(new Uri(iconFilePath));
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка при запросе погоды: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}");
                }
            }
        }
    }
}
