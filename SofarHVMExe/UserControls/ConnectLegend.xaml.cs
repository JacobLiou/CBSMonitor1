using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SofarHVMExe.UserControls
{
    /// <summary>
    /// ConnectDiag.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectLegend : UserControl
    {
        private Storyboard sbChannel1 = new Storyboard();
        private Storyboard sbChannel2 = new Storyboard();

        public ConnectLegend()
        {
            InitializeComponent();

            ObjectAnimationUsingKeyFrames? animation1 = 
                this.TryFindResource("FaultBlinkAnimationUseKF") as ObjectAnimationUsingKeyFrames;
            if (animation1 != null)
            {
                Storyboard.SetTargetName(animation1, Channel1FaultX.Name);
                Storyboard.SetTargetProperty(animation1, new PropertyPath(TextBlock.VisibilityProperty));
                sbChannel1.Children.Add(animation1);
            }

            ObjectAnimationUsingKeyFrames? animation2 = animation1?.Clone();
            if (animation2 != null)
            {
                Storyboard.SetTargetName(animation2, Channel2FaultX.Name);
                Storyboard.SetTargetProperty(animation2, new PropertyPath(TextBlock.VisibilityProperty));
                sbChannel2.Children.Add(animation2);
            }
        }


        public bool? IsConnected
        {
            get
            {
                return (bool?)GetValue(IsConnectedProperty);
            }
            set
            {
                
                SetValue(IsConnectedProperty, value);
            }
        }
        public static readonly DependencyProperty IsConnectedProperty =
            DependencyProperty.Register("IsConnected", typeof(bool?), typeof(ConnectLegend));

        public bool IsChannel1Open
        {
            get
            {
                return (bool)GetValue(IsChannel1OpenProperty);
            }
            set
            {
                if (!value)
                {
                    //不要在这里响应属性更改，这里不会走进来
                }
                SetValue(IsChannel1OpenProperty, value);
            }
        }
        public static readonly DependencyProperty IsChannel1OpenProperty =
            DependencyProperty.Register("IsChannel1Open", typeof(bool), typeof(ConnectLegend), 
                new PropertyMetadata(false, OnPropertyChangedCallback_Channel1));

        private static void OnPropertyChangedCallback_Channel1(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConnectLegend cl = d as ConnectLegend;
            cl?.OpenAnimation_Channel1(e.NewValue);
        }
        private void OpenAnimation_Channel1(object value)
        {
            if ((bool)value)
            {
                //通道开启效果
                sbChannel1.Stop(Channel1FaultX);
                Channel1Line.Stroke = new SolidColorBrush(Color.FromRgb(168, 211, 24));
                Channel1FaultX.Visibility = Visibility.Hidden;
            }
            else
            {
                //通道关闭效果
                Channel1Line.Stroke = new SolidColorBrush(Colors.Gray);
                sbChannel1.Begin(Channel1FaultX, true);
            }
        }

        public bool IsChannel2Open
        {
            get
            {
                return (bool)GetValue(IsChannel2OpenProperty);
            }
            set
            {
                SetValue(IsChannel2OpenProperty, value);
            }
        }
        public static readonly DependencyProperty IsChannel2OpenProperty =
            DependencyProperty.Register("IsChannel2Open", typeof(bool), typeof(ConnectLegend),
                new PropertyMetadata(false, OnPropertyChangedCallback_Channel2));
        
        private static void OnPropertyChangedCallback_Channel2(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConnectLegend cl = d as ConnectLegend;
            cl?.OpenAnimation_Channel2(e.NewValue);
        }
        private void OpenAnimation_Channel2(object value)
        {
            if ((bool)value)
            {
                //通道开启效果
                sbChannel2.Stop(Channel2FaultX);
                Channel2Line.Stroke = new SolidColorBrush(Color.FromRgb(168, 211, 24));
                Channel2FaultX.Visibility = Visibility.Hidden;
            }
            else
            {
                //通道关闭效果
                Channel2Line.Stroke = new SolidColorBrush(Colors.Gray);
                sbChannel2.Begin(Channel2FaultX, true);
            }
        }

    }//class
}
