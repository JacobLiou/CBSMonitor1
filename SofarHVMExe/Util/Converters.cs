using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Collections.ObjectModel;
using FontAwesome.Sharp;
using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using System.Drawing;
using Color = System.Windows.Media.Color;

namespace SofarHVMExe.Utilities
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public BoolToVisibilityConverter()
            : this(true)
        {

        }
        public BoolToVisibilityConverter(bool collapsewhenInvisible)
            : base()
        {
            CollapseWhenInvisible = collapsewhenInvisible;
        }
        public bool CollapseWhenInvisible { get; set; } //invisible时收缩
        public bool Reverse { get; set; } = false; //反转 True的时候隐藏 False的时候显示


        public Visibility FalseVisible
        {
            get
            {
                if (CollapseWhenInvisible)
                {
                    if (!Reverse)
                    {
                        return Visibility.Collapsed;
                    }
                    else
                    {
                        return Visibility.Visible;
                    }
                }
                else
                {
                    if (!Reverse)
                    {
                        return Visibility.Hidden;
                    }
                    else
                    {
                        return Visibility.Visible;
                    }
                }
            }
        }

        // 绑定源传给绑定目标 
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Visible;

            Visibility visibility;
            if (!Reverse)
            {
                return (bool)value ? Visibility.Visible : FalseVisible;
            }
            else
            {
                return (bool)value ? FalseVisible : Visibility.Visible;
            }
        }

        // 绑定目标传给绑定源 
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return true;

            if (!Reverse)
            {
                return ((Visibility)value == Visibility.Visible);
            }
            else
            {
                return ((Visibility)value == FalseVisible);
            }
        }
    }

    /// <summary>
    /// 事件类型到索引转换器
    /// EventType<->int
    /// </summary>
    public class EventType2IndexConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            EventType? type = (EventType)value;
            if (type == null)
                return DependencyProperty.UnsetValue;

            return type switch
            {
                EventType.Status => 0,
                EventType.Exception => 1,
                EventType.Fault => 2,
                _ => -1
            };
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            int index = (int)value;
            return index switch
            {
                0 => EventType.Status,
                1 => EventType.Exception,
                2 => EventType.Fault,
                _ => EventType.None
            };
        }
    }//class

    /// <summary>
    /// 事件类型到背景色转换器
    /// </summary>
    public class EventType2BackgroudConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string eventType = (string)value;

            //if (eventType == "状态")
            //{
            //    return new SolidColorBrush(Color.FromRgb(46, 204, 113));
            //}
            //else if (eventType == "异常")
            //{
            //    return new SolidColorBrush(Colors.Yellow);
            //}
            //else if (eventType == "故障")
            //{
            //    return new SolidColorBrush(Colors.Red);
            //}

            return new SolidColorBrush(Colors.White);
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }//class

    /// <summary>
    /// 命令类型到使能转换器
    /// </summary>
    public class CommandType2EnableConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string eventType = (string)value;

            if (eventType == "0(禁止)")
            {
                return false;
            }

            return true;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }//class

    /// <summary>
    /// 命令类型到前景色转换器
    /// </summary>
    public class CommandType2ForegroudConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            int commandType = (int)value;

            if (commandType == 0 || commandType == 2) //禁用命令 固定命令
            {
                return new SolidColorBrush(Colors.Black);
            }
            else if (commandType == 1) //可设命令
            {
                return new SolidColorBrush(Colors.Blue);
            }

            return new SolidColorBrush(Colors.Black);
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }//class

    /// <summary>
    /// 命令类型到命令值转换器
    /// string<->int
    /// </summary>
    public class CommandType2ValueConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            int commandIndex = (int)value;
            if (commandIndex == 0)
            {
                return "0(禁用命令)";
            }
            else if (commandIndex == 1)
            {
                return "1(可设命令)";
            }
            else
            {
                return "2(固定命令)";
            }
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string commandText = (string)value;
            if (commandText.Contains("禁用"))
            {
                return 0;
            }
            else if (commandText.Contains("可设"))
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

    }//class

    /// <summary>
    /// 命令类型到bool转换器
    /// string<->bool
    /// </summary>
    public class CommandType2BoolConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            int cmdType = (int)value;
            if (cmdType == 2) //固定命令
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value == null)
            return DependencyProperty.UnsetValue;
        }

    }//class

    /// <summary>
    /// uint到16进制string转换器
    /// </summary>
    public class UInt2HexStrConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            uint data = (uint)value;
            return "0x" + data.ToString("X8");
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string hexVal = (string)value;
            uint data = 0;

            hexVal = hexVal.Replace("0x", "").Replace("0X", "");
            if (uint.TryParse(hexVal, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out data))
            {
                return data;
            }

            return value;
        }

    }//class

    /// <summary>
    /// 帧类型到Visible转换器
    /// </summary>
    public class FrameType2VisibleConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string frameType = (string)value;
            if (frameType.Contains("标准帧"))
                return false;
            else
                return true;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }//class

    /// <summary>
    /// 帧类型到字符串转换器
    /// </summary>
    public class FrameType2StrConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            byte v = (byte)value;
            int frameType = (int)v;
            switch (frameType)
            {
                case 0:
                    return "0（标准帧）";
                case 1:
                    return "1（数据帧）";
                case 2:
                    return "2（请求帧）";
                case 3:
                    return "3（应答帧）";
                default:
                    return "";
            }
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// 帧数据集到字符串转换器
    /// </summary>
    public class FrameDatas2StrConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            List<CanFrameData>? frameDatas = value as List<CanFrameData>;
            if (frameDatas == null)
                return "";

            string result = "";
            foreach (CanFrameData frameData in frameDatas)
            {
                foreach (CanFrameDataInfo dataInfo in frameData.DataInfos)
                {
                    result += $"{dataInfo.Name}({dataInfo.Value}), ";
                }

                result = result.TrimEnd(", ".ToArray()) + ";"; //去除结尾
            }

            result = result.TrimEnd(";".ToArray()); //去除结尾
            return result;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// 十六进制字符串到int转换器
    /// </summary>
    public class Hexstr2intConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            int intVal = (int)value;
            return intVal.ToString("X");
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string? hexVal = value as string;
            if (hexVal == null)
                return 0;

            int result = 0;
            hexVal = hexVal.Trim().Replace("0x", "").Replace("0X", "");
            if (int.TryParse(hexVal, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
                return result;
            else
                return 0;
        }
    }//class

    /// <summary>
    /// 帧到值字符串转换器
    /// CanFrameModel<->string
    /// </summary>
    public class FrameModel2DataStrConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            CanFrameModel frameModel = value as CanFrameModel;
            if (frameModel == null)
                return "";

            if (frameModel.FrameDatas.Count == 0)
                return "";

            string result = "";
            CanFrameData frameData = frameModel.FrameDatas[0]; //第一包数据
            foreach (CanFrameDataInfo dataInfo in frameData.DataInfos)
            {
                result += $"{dataInfo.Name}({dataInfo.Value}), ";
            }

            result = result.TrimEnd(", ".ToArray()); //去除结尾
            return result;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// 帧值字符串转换器
    /// string<->string
    /// 用于校验值是否设置正确
    /// </summary>
    public class FrameStrValueValidateConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string strVal = value as string;
            if (strVal == null)
                return "0";

            return strVal;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string strVal = value as string;
            string type = parameter as string;

            return strVal;

            //校验type为null 这里暂时不进行校验 
            //这个bug后面再改

            if (ValidateData(strVal, type))
            {
                return strVal;
            }

            return DependencyProperty.UnsetValue;
        }

        private bool ValidateData(string strVal, string type)
        {
            if (strVal == null || type == null || strVal == "" || strVal == "-")
                return false;

            if (strVal.Contains("0x") || strVal.Contains("0X"))
            {
                //校验16进制数
                strVal = strVal.Replace("0x", "").Replace("0X", "");

                if (type.Contains("float"))
                {
                    //小数不允许输16进制
                    return false;
                }
                else
                {
                    long value = 0;
                    return long.TryParse(strVal, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                }
            }
            else
            {
                if (type.Contains("float"))
                {
                    //校验小数
                    float fV;
                    return (float.TryParse(strVal, out fV));
                }
                else if (type.Contains("char"))
                {
                    //校验char字符
                    char cV;
                    return char.TryParse(strVal, out cV);
                }
                else
                {
                    //校验10进制数
                    int iV;
                    return int.TryParse(strVal, out iV);
                }
            }

            //通过字符串来校验，对于0x0001校验会不通过
            /*
            {
                if (strVal.StartsWith('0') && strVal.Length > 1)
                    return false;

                if (Regex.IsMatch(strVal, @"^[0-9A-Fa-f]+$"))
                {
                    return true;
                }

                return false;
            }
            */

        }//func
    }//class

    /// <summary>
    /// 帧值字符串转换器
    /// string<->string
    /// 用于校验值是否设置正确
    /// </summary>
    //public class FrameStrValueValidateConverter : IMultiValueConverter
    //{
    //    //当值从绑定源传播给绑定目标时，调用方法Convert
    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (values == null || values.Length != 2)
    //            return DependencyProperty.UnsetValue;

    //        string? strVal = values[0] as string;
    //        //string? type = values[1] as string;
    //        if (strVal == null)
    //            return "0";

    //        return strVal;
    //    }

    //    //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
    //    public object[] ConvertBack(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (values == null || values.Length != 2)
    //            return DependencyProperty.UnsetValue;

    //        string? strVal = values[0] as string;
    //        string? type = values[1] as string;
    //        if (strVal == null || type == null)
    //            return DependencyProperty.UnsetValue;

    //        if (ValidateData(strVal, type))
    //        {
    //            return strVal;
    //        }

    //        return DependencyProperty.UnsetValue;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private bool ValidateData(string strVal, string type)
    //    {
    //        if (strVal == null || strVal == "" || strVal == "-")
    //            return false;

    //        if (strVal.Contains("0x") || strVal.Contains("0X"))
    //        {
    //            //校验16进制数
    //            strVal = strVal.Replace("0x", "").Replace("0X", "");

    //            if (type.Contains("float"))
    //            {
    //                //小数不允许输16进制
    //                return false;
    //            }
    //            else
    //            {
    //                long value = 0;
    //                return long.TryParse(strVal, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    //            }
    //        }
    //        else
    //        {
    //            if (type.Contains("float"))
    //            {
    //                //校验小数
    //                float fV;
    //                return (float.TryParse(strVal, out fV));
    //            }
    //            else if (type.Contains("char"))
    //            {
    //                //校验char字符
    //                char cV;
    //                return char.TryParse(strVal, out cV);
    //            }
    //            else
    //            {
    //                //校验10进制数
    //                int iV;
    //                return int.TryParse(strVal, out iV);
    //            }
    //        }
    //    }//func
    //}//class

    /// <summary>
    /// Can帧数据类型校验转换器
    /// string<->string
    /// </summary>
    public class FrameDataTypeValidateConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            return value;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter is not ObservableCollection<CanFrameDataInfo>)
                return DependencyProperty.UnsetValue;

            string currentType = (string)value;
            ObservableCollection<CanFrameDataInfo> dataInfos = parameter as ObservableCollection<CanFrameDataInfo>;

            //判断数据是否超出8个字节长度
            {
                int count = 0;
                foreach (CanFrameDataInfo info in dataInfos)
                {
                    if (info.Type.Contains("8"))
                    {
                        count += 1;
                    }
                    else if (info.Type.Contains("16"))
                    {
                        count += 2;
                    }
                    else if (info.Type.Contains("32"))
                    {
                        count += 4;
                    }
                }

                if (currentType.Contains("8"))
                {
                    count += 1;
                }
                else if (currentType.Contains("16"))
                {
                    count += 2;
                }
                else if (currentType.Contains("32"))
                {
                    count += 4;
                }

                if (count > 8)
                {
                    MessageBox.Show($"长度最大8个字节，无法更改类型为{currentType}！", "提示");
                    return DependencyProperty.UnsetValue; ;
                }
            }

            return currentType;
        }
    }//class

    /// <summary>
    /// 发送接收标识到图标转换器
    /// bool<->Icon
    /// </summary>
    public class SendRecv2IconConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            bool isSend = (bool)value;
            if (isSend)
                return FontAwesome.Sharp.IconChar.CloudArrowDown;
            else
                return FontAwesome.Sharp.IconChar.CloudArrowUp;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// 发送接收标识到颜色转换器
    /// bool<->Color
    /// </summary>
    public class SendRecv2ColorConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            bool isSend = (bool)value;
            if (isSend)
                return new SolidColorBrush(Color.FromRgb(75, 101, 196));
            else
                return new SolidColorBrush(Color.FromRgb(82, 161, 70));
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// 连接转背景色
    /// bool<->color
    /// </summary>
    public class Connect2BgdConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            bool v = (bool)value;
            return v ? new SolidColorBrush(Color.FromRgb(30, 144, 255)) : new SolidColorBrush(Color.FromRgb(154, 167, 177));
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }//class

    /// <summary>
    /// bool到颜色的转换
    /// bool<->color
    /// 必须设置parameter，格式："#000000 #FFFFFF"
    /// 颜色1为true时颜色，颜色2为false时颜色，中间用空格隔开
    /// </summary>
    public class Bool2ColorConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return DependencyProperty.UnsetValue;

            bool v = (bool)value;

            string strColors = (string)parameter;
            string[] colorArr = strColors.Split('_');
            if (colorArr.Length != 2)
                return DependencyProperty.UnsetValue;

            BrushConverter brushConverter = new BrushConverter();
            SolidColorBrush? trueColor = (SolidColorBrush?)brushConverter.ConvertFrom(colorArr[0]);
            SolidColorBrush? falseColor = (SolidColorBrush?)brushConverter.ConvertFrom(colorArr[1]);

            if (trueColor == null)
            {
                trueColor = new SolidColorBrush();
            }

            if (falseColor == null)
            {
                falseColor = new SolidColorBrush();
            }

            return v ? trueColor : falseColor;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// 字符串到颜色的转换
    /// string<->color
    /// </summary>
    public class Str2ColorConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strVal && parameter is string strType)
            {
                if (strType == "HasFault")
                {
                    //心跳故障颜色
                    return (strVal == "有") ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                }
                else if (strType == "ConnectSts")
                {
                    //连接状态颜色
                    BrushConverter bc = new BrushConverter();
                    SolidColorBrush greenColor = (SolidColorBrush)bc.ConvertFrom("#2ecc71");
                    return (strVal == "断开") ? new SolidColorBrush(Colors.Red) : greenColor;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// bool到string的转换
    /// </summary>
    public class Bool2StrConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            bool v = (bool)value;
            return v ? "是" : "否";
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string v = (string)value;
            return (v == "是") ? true : false;
        }
    }//class

    /// <summary>
    /// String到帧数据的转换  not used
    /// </summary>
    public class CmdType2BoolConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            List<CanFrameData>? frameDatas = value as List<CanFrameData>;
            if (frameDatas == null)
                return DependencyProperty.UnsetValue;

            string datas = "";
            foreach (var dataInfo in frameDatas[0].DataInfos)
            {
                if (!(dataInfo.Type.Contains("包序号") ||
                    dataInfo.Type.Contains("起始地址") ||
                    dataInfo.Type.Contains("个数") ||
                    dataInfo.Type.Contains("校验")
                    ))
                {
                    datas += dataInfo.Value;
                }
            }

            return datas;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;


            return DependencyProperty.UnsetValue;
        }
    }//class

    /// <summary>
    /// String <-> Visibility
    /// </summary>
    public class Str2VisibilityConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            if (value is string str)
            {
                //if (str == "")
                //{
                //    return 0;
                //}
                //else 
                //{ 
                //    return Visibility.Visible;
                //}

                //if (str == "")
                //{
                //    return Visibility.Collapsed;
                //}
                //else
                //{
                //    return Visibility.Visible;
                //}
                return (str.Contains("测试")) ? Visibility.Collapsed : Visibility.Visible;
            }

            return DependencyProperty.UnsetValue;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// String <-> Visibility
    /// </summary>
    public class SpecialFrameInfo2StringConverter : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            if (value is ObservableCollection<CanFrameDataInfo> frame)
            {
                return string.Join("\t", frame.Select(item => $"{item.Name}：{item.Value}"));
            }

            return DependencyProperty.UnsetValue;
        }

        //当值从绑定目标传播给绑定源时，调用此方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            return DependencyProperty.UnsetValue;
        }
    }


}//namespace
