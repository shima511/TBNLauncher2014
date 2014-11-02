using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Threading;

namespace TBNLauncher2014
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        [Flags]
        enum ControlFocusFlag
        {
            None = 0,
            CloseButton = 1,
            MinimizeButton = 2,
            ListBox = 4,
            ExecuteButton = 8
        }
        readonly Brush WindowBackGround;
        readonly Brush MouseOnButtonColor = Brushes.White;
        readonly Brush ClickedButtonColor = Brushes.Wheat;
        readonly int TimeInterval = 2;
        int currentImagePathIndex = 0;
        int nextImagePathIndex = 1;
        Point mousePoint;
        ControlFocusFlag control_flags = ControlFocusFlag.None;
        Contents[] contentsList;
        DispatcherTimer timer;
        int time;

        public MainWindow()
        {
            InitializeComponent();
            WindowBackGround = this.Background;
        }

        private void CloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((control_flags & ControlFocusFlag.CloseButton) == ControlFocusFlag.CloseButton)
            {
                this.Close();
            }
        }

        private void MinimizeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && control_flags == ControlFocusFlag.None)
            {
                var pos = e.GetPosition(this);
                this.Left += pos.X - mousePoint.X;
                this.Top += pos.Y - mousePoint.Y;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePoint = e.GetPosition(this);
        }

        private void ExecuteButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            control_flags |= ControlFocusFlag.ExecuteButton;
            this.ExecuteButton.Background = Brushes.LightGreen;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            control_flags |= ControlFocusFlag.ListBox;
            this.WorkDescription.Text = contentsList[this.ListBox.SelectedIndex].Description;
            currentImagePathIndex = 0;
            nextImagePathIndex = 1;
            time = 0;
            ChangeCurrentImage(0);
        }

        private void ListBox_MouseEnter(object sender, MouseEventArgs e)
        {
            control_flags |= ControlFocusFlag.ListBox;
        }

        private void ListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            control_flags -= ControlFocusFlag.ListBox;
        }

        [Conditional("DEBUG")]
        void DebugMessage(InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            timer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher);
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();

            if (System.Diagnostics.Process.GetProcessesByName(System.Diagnostics.Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("多重起動はできません", "アプリケーションエラー", MessageBoxButton.OK);
                this.Close();
                return;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(Contents[]));
            try
            {
                StreamReader sr = new StreamReader("ContentsList.xml", System.Text.Encoding.GetEncoding("shift_jis"));
                contentsList = (Contents[])serializer.Deserialize(sr);
                InitListBox();
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
            catch (InvalidOperationException ex)
            {
                DebugMessage(ex);
                MessageBox.Show("XMLファイルが破損しています。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        void UpdateCurrentImagePathIndex()
        {
            currentImagePathIndex++;
            nextImagePathIndex++;
            if (nextImagePathIndex == contentsList[this.ListBox.SelectedIndex].ScreenShotImagePath.Length)
            {
                nextImagePathIndex = 0;
            }
            if (currentImagePathIndex == contentsList[this.ListBox.SelectedIndex].ScreenShotImagePath.Length)
            {
                currentImagePathIndex = 0;
            }
        }

        void ChangeCurrentImage(int index)
        {
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                string baseDir = System.IO.Directory.GetCurrentDirectory();
                image.UriSource = new Uri(System.IO.Path.Combine(baseDir, contentsList[this.ListBox.SelectedIndex].ScreenShotImagePath[index]));
                image.DecodePixelWidth = 220;
                image.DecodePixelHeight = 160;
                image.EndInit();
                this.GameImage.Source = image;
            }
            catch (FileNotFoundException)
            {
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            time++;
            if (this.ListBox.SelectedIndex != -1)
            {
                if (time > TimeInterval)
                {
                    UpdateCurrentImagePathIndex();
                    ChangeCurrentImage(currentImagePathIndex);
                    time = 0;
                }
            }
        }

        void InitListBox()
        {
            foreach (var item in contentsList)
            {
                this.ListBox.Items.Add(item.Title);
            }
        }

        private void ExecuteButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBox.SelectedIndex != -1 && (control_flags & ControlFocusFlag.ExecuteButton) == ControlFocusFlag.ExecuteButton)
            {
                try
                {
                    var p = System.Diagnostics.Process.Start(contentsList[ListBox.SelectedIndex].AppPath);
                }catch(FileNotFoundException ex){
                    MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((control_flags & ControlFocusFlag.ExecuteButton) == ControlFocusFlag.ExecuteButton)
            {
                this.ExecuteButton.Background = Brushes.LightGreen;
            }
            else
            {
                this.ExecuteButton.Background = Brushes.DarkGreen;
            }
        }

        private void ExecuteButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if ((control_flags & ControlFocusFlag.ExecuteButton) == ControlFocusFlag.ExecuteButton)
            {
                this.ExecuteButton.Background = Brushes.DarkGreen;
            }
            else
            {
                this.ExecuteButton.Background = Brushes.Green;
            }
        }
    }
}
