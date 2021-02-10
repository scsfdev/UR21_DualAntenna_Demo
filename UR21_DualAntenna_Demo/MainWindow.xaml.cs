using GalaSoft.MvvmLight.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UR21_DualAntenna_Demo.ViewModel;
using UR21_DualAntenna_Demo.Model;

namespace UR21_DualAntenna_Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            Messenger.Default.Register<string>(this, MsgType.MAIN_V, ShowMsg);

            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();


            Messenger.Default.Register<NotificationMessage>(this, (nm) =>
            {
                if (nm.Notification == MyConst.EXIT)
                {
                    if (nm.Sender == DataContext)
                        Close();
                }
            });
        }

        private void ShowMsg(string strMsg)
        {
            MessageBoxImage mboxImg;

            if (strMsg.ToUpper().Contains(MyConst.ERROR))
                mboxImg = MessageBoxImage.Error;
            else if (strMsg.ToUpper().Contains(MyConst.WARNING))
                mboxImg = MessageBoxImage.Warning;
            else
                mboxImg = MessageBoxImage.Information;

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, strMsg, this.Title, MessageBoxButton.OK, mboxImg);
            });
        }

        private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (sender as Button).BorderBrush = Brushes.Blue;
        }

        private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            LinearGradientBrush gBrush = new LinearGradientBrush();
            gBrush.StartPoint = new Point(0.5, 0);
            gBrush.EndPoint = new Point(0.5, 1);
            gBrush.MappingMode = BrushMappingMode.RelativeToBoundingBox;

            GradientStop gs1 = new GradientStop();
            if ((sender as Button).Tag != null && (sender as Button).Tag.ToString() == "DELETE")
                gs1.Color = (Color)new ColorConverter().ConvertFrom("#FFF59A9A");
            else
                gs1.Color = (Color)new ColorConverter().ConvertFrom("#FF9737F7");

            gs1.Offset = 0;
            gBrush.GradientStops.Add(gs1);

            GradientStop gs2 = new GradientStop();
            gs2.Color = Colors.White;
            gs2.Offset = 0.802;
            gBrush.GradientStops.Add(gs2);

            // lblIMChange.BorderBrush = null;
            (sender as Button).BorderBrush = gBrush;
        }

        private void TextBlock_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            string strMsg = (sender as TextBlock).Text;

            if (string.IsNullOrEmpty(strMsg))
                return;
            else if (strMsg.ToUpper().StartsWith("ERROR"))
            {
                (sender as TextBlock).Foreground = Brushes.White;
                (sender as TextBlock).Background = Brushes.Red;
            }
            else if (strMsg.ToUpper().StartsWith("WARNING"))
            {
                (sender as TextBlock).Foreground = Brushes.Red;
                (sender as TextBlock).ClearValue(TextBlock.BackgroundProperty);
            }
            else
            {
                (sender as TextBlock).Foreground = Brushes.Black;
                (sender as TextBlock).ClearValue(TextBlock.BackgroundProperty);
            }

            (sender as TextBlock).ToolTip = strMsg;
        }
    }
}