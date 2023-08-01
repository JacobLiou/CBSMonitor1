using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;

namespace CanProtocol.ProtocolModel
{
    /// <summary>
    /// 表示一帧中8个字节数据
    /// 一帧中一个包数据
    /// </summary>
    public class CanFrameData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public CanFrameData()
        {
            DataLen = 8;
            Data = new byte[8];
            DataInfos = new ObservableCollection<CanFrameDataInfo>();
        }

        /// <summary>
        /// 复制构造
        /// </summary>
        /// <param name="other"></param>
        public CanFrameData(CanFrameData other)
        {
            int len = other.Data.Length;
            DataLen = len;
            Data = new byte[len];

            for (int i = 0; i < len; ++i)
            {
                Data[i] = other.Data[i];
            }

            DataInfos = new ObservableCollection<CanFrameDataInfo>();
            foreach (CanFrameDataInfo info in other.DataInfos)
            {
                CanFrameDataInfo newInfo = new CanFrameDataInfo(info);
                DataInfos.Add(newInfo);
            }
        }

        public int DataLen { get; set; } //数据长度
        public byte[] Data { get; set; } //数据内容
        public int DataNum { get; set; } //有效数据个数（用于连续多包中 结束帧的有效数据 计数）
        //字段数据集合
        private ObservableCollection<CanFrameDataInfo> dataInfos = new ObservableCollection<CanFrameDataInfo>();
        public ObservableCollection<CanFrameDataInfo> DataInfos
        {
            get => dataInfos;
            set
            {
                dataInfos = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 增加点表起始字段和个数字段
        /// 用于非连续-单包
        /// </summary>
        public void AddInitData()
        {
            CanFrameDataInfo startAddrDataInfo = new CanFrameDataInfo();
            startAddrDataInfo.Name = "点表起始地址";
            startAddrDataInfo.Type = "U16";
            startAddrDataInfo.Value = "0x0000";
            DataInfos.Add(startAddrDataInfo);

            CanFrameDataInfo numDataInfo = new CanFrameDataInfo();
            numDataInfo.Name = "个数";
            numDataInfo.Type = "U8";
            numDataInfo.Value = "1";
            numDataInfo.Unit = "个";
            DataInfos.Add(numDataInfo);
        }

        /// <summary>
        /// 增加点表起始字段和个数字段
        /// 用于连续-多包
        /// </summary>
        public void AddInitMultiData()
        {
            CanFrameDataInfo info1 = new CanFrameDataInfo();
            info1.Name = "包序号";
            info1.Type = "U8";
            info1.Value = "0";
            DataInfos.Add(info1);

            CanFrameDataInfo info2 = new CanFrameDataInfo();
            info2.Name = "点表起始地址";
            info2.Type = "U16";
            info2.Value = "0x0000";
            DataInfos.Add(info2);

            CanFrameDataInfo info3 = new CanFrameDataInfo();
            info3.Name = "个数";
            info3.Type = "U8";
            info3.Value = "0";
            info3.Unit = "个";
            DataInfos.Add(info3);

            CanFrameDataInfo info4 = new CanFrameDataInfo();
            info4.Name = "CRC校验";
            info4.Type = "U16";
            info4.Value = "0x0";
            DataInfos.Add(info4);
        }

        /// <summary>
        /// 增加文件传输子设备交互报文
        /// 用于非连续-单包
        /// </summary>
        public void AddInitDataTofile()
        {
            CanFrameDataInfo info1 = new CanFrameDataInfo();
            info1.Name = "子设备地址";
            info1.Type = "U8";
            info1.Value = "0";
            DataInfos.Add(info1);

            CanFrameDataInfo info2 = new CanFrameDataInfo();
            info2.Name = "文件编号";
            info2.Type = "U8";
            info2.Value = "0";
            DataInfos.Add(info2);

            CanFrameDataInfo info3 = new CanFrameDataInfo();
            info3.Name = "结果描述符";
            info3.Type = "U8";
            info3.Value = "0";
            DataInfos.Add(info3);

            CanFrameDataInfo info4 = new CanFrameDataInfo();
            info4.Name = "文件大小";
            info4.Type = "U24";
            info4.Value = "0";
            DataInfos.Add(info4);
        }

        /// <summary>
        /// 增加文件传输子设备交互报文
        /// 用于连续-多包
        /// </summary>
        public void AddInitMultiDataTofile()
        {
            CanFrameDataInfo info0 = new CanFrameDataInfo();
            info0.Name = "子设备地址";
            info0.Type = "U8";
            info0.Value = "0";
            DataInfos.Add(info0);

            CanFrameDataInfo info1 = new CanFrameDataInfo();
            info1.Name = "包序号";
            info1.Type = "U8";
            info1.Value = "0";
            DataInfos.Add(info1);

            CanFrameDataInfo info2 = new CanFrameDataInfo();
            info2.Name = "当前文件偏移地址";
            info2.Type = "U24";
            info2.Value = "0x0000";
            DataInfos.Add(info2);

            CanFrameDataInfo info3 = new CanFrameDataInfo();
            info3.Name = "数据长度";
            info3.Type = "U16";
            info3.Value = "0";
            info3.Unit = "个";
            DataInfos.Add(info3);

            CanFrameDataInfo info4 = new CanFrameDataInfo();
            info4.Name = "CRC校验";
            info4.Type = "U16";
            info4.Value = "0x0";
            DataInfos.Add(info4);
        }

        /// <summary>
        /// 增加起始帧默认数据（废弃无用）
        /// 用于连续-多包
        /// </summary>
        public void AddStFrameDefData()
        {
            CanFrameDataInfo info1 = new CanFrameDataInfo();
            info1.Name = "包序号";
            info1.Type = "U8";
            info1.Value = "0";
            DataInfos.Add(info1);

            CanFrameDataInfo info2 = new CanFrameDataInfo();
            info2.Name = "点表起始地址";
            info2.Type = "U16";
            info2.Value = "0x0000";
            DataInfos.Add(info2);

            CanFrameDataInfo info3 = new CanFrameDataInfo();
            info3.Name = "个数";
            info3.Type = "U8";
            info3.Value = "0";
            info3.Unit = "个";
            DataInfos.Add(info3);
        }

        /// <summary>
        /// 增加中间帧默认数据
        /// 用于连续-多包
        /// </summary>
        public void AddMidFrameDefData()
        {
            CanFrameDataInfo info1 = new CanFrameDataInfo();
            info1.Name = "包序号";
            info1.Type = "U8";
            info1.Value = "1";
            DataInfos.Add(info1);
        }

        /// <summary>
        /// 增加结束帧默认数据
        /// 用于连续-多包
        /// </summary>
        public void AddEndFrameDefData()
        {
            CanFrameDataInfo info1 = new CanFrameDataInfo();
            info1.Name = "包序号";
            info1.Type = "U8";
            info1.Value = "0xff";
            DataInfos.Add(info1);

            CanFrameDataInfo info2 = new CanFrameDataInfo();
            info2.Name = "CRC校验";
            info2.Type = "U16";
            info2.Value = "0xff";
            DataInfos.Add(info2);

            DataNum = 0;
        }

        /// <summary>
        /// 设置包序号
        /// 用于连续-多包
        /// </summary>
        /// <param name="index"></param>
        public void SetPackageIndex(int index)
        {
            foreach (CanFrameDataInfo info in DataInfos)
            {
                if (info.Name == "包序号")
                {
                    info.Value = index.ToString();
                    return;
                }
            }
        }

        /// <summary>
        /// 设置帧个数
        /// 用于连续-多包
        /// </summary>
        /// <param name="index"></param>
        public void SetFrameNum(int num)
        {
            foreach (CanFrameDataInfo info in DataInfos)
            {
                if (info.Name == "个数")
                {
                    info.Value = num.ToString();
                    return;
                }
            }
        }

        /// <summary>
        /// 设置Crc校验值
        /// 用于连续-多包
        /// </summary>
        /// <param name="val"></param>
        public void SetCrcVal(ushort val)
        {
            foreach (CanFrameDataInfo info in DataInfos)
            {
                if (info.Name == "CRC校验")
                {
                    info.Value = "0x" + val.ToString("X4");
                    return;
                }
            }
        }

        /// <summary>
        /// 获取包序号
        /// 用于连续-多包
        /// </summary>
        /// <returns></returns>
        public string GetPackageIndex()
        {
            foreach (CanFrameDataInfo info in DataInfos)
            {
                if (info.Name == "包序号")
                {
                    return info.Value;
                }
            }

            return "-1";
        }

        /// <summary>
        /// 获取数据字节长度
        /// </summary>
        /// <returns></returns>
        public int GetLen()
        {
            int count = 0;
            foreach (CanFrameDataInfo info in dataInfos)
            {
                count += info.ByteRange;
            }

            return count;
        }

        /// <summary>
        /// 设置包序号
        /// 用于连续-多包
        /// </summary>
        /// <param name="index"></param>
        public static void SetPackageIndex(ObservableCollection<CanFrameDataInfo> dataInfos, int index)
        {
            foreach (CanFrameDataInfo info in dataInfos)
            {
                if (info.Name == "包序号")
                {
                    info.Value = index.ToString();
                    return;
                }
            }
        }

        /// <summary>
        /// 设置帧个数
        /// 用于连续-多包
        /// </summary>
        /// <param name="dataInfos"></param>
        /// <param name="num"></param>
        public static void SetFrameNum(ObservableCollection<CanFrameDataInfo> dataInfos, int num)
        {
            foreach (CanFrameDataInfo info in dataInfos)
            {
                if (info.Name == "个数")
                {
                    info.Value = num.ToString();
                    return;
                }
            }
        }

        /// <summary>
        /// 设置Crc校验值
        /// 用于连续-多包
        /// </summary>
        /// <param name="val"></param>
        public static void SetCrcVal(ObservableCollection<CanFrameDataInfo> dataInfos, ushort val)
        {
            CanFrameDataInfo info = dataInfos.Last();
            if (info.Name == "CRC校验")
            {
                info.Value = "0x" + val.ToString("X4");
                return;
            }
        }

    }//class

    /// <summary>
    /// 表示一包数据中，单个字段的数据
    /// </summary>
    public class CanFrameDataInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public CanFrameDataInfo()
        {
            Name = "数据";
            Type = "U16";
            ByteRange = 2;
            Value = "0x0";
            Precision = 1;
            Unit = "";
            Hide = false;
        }

        /// <summary>
        /// 复制构造
        /// </summary>
        /// <param name="other"></param>
        public CanFrameDataInfo(CanFrameDataInfo other)
        {
            Name = other.Name;
            Type = other.Type;
            ByteRange = other.ByteRange;
            Value = other.Value;
            Precision = other.Precision;
            Unit = other.Unit;
            Hide = other.Hide;
        }

        public CanFrameDataInfo(string name, string type, int byteRange, string value, decimal precision, string unit, bool hide)
        {
            Name = name;
            Type = type;
            ByteRange = byteRange;
            Value = value;
            Precision = precision;
            Unit = unit;
            Hide = hide;
        }

        public string Name { get; set; }        //数据名称

        private string type = "0";
        public string Type                      //数据类型（如uint16）
        {
            get => type;
            set
            {
                type = value;
                if (type.Contains("8"))
                {
                    ByteRange = 1;
                }
                else if (type.Contains("16"))
                {
                    ByteRange = 2;
                }
                else if (type.Contains("32"))
                {
                    ByteRange = 4;
                }
                else if (type.Contains("char"))
                {
                    ByteRange = 1;
                }
                else if (type.Contains("float"))
                {
                    ByteRange = 4;
                }
                else if (type.Contains("string"))
                {
                    ByteRange = Value.Length;
                }
            }
        }
        public int ByteRange { get; set; }      //字节范围（如2，3~4）

        private string value = "";
        public string Value                     //值
        {
            get => value;
            set
            {
                this.value = value;
                OnPropertyChanged();

                if (type == "string")
                {
                    ByteRange = Value.Length;
                }
            }
        }
        public decimal? Precision { get; set; } //精度
        public string Unit { get; set; }        //单位
        public bool Hide { get; set; }          //隐藏


        /// <summary>
        /// 是否为有效数据
        /// </summary>
        /// <returns></returns>
        public bool IsValidData()
        {
            if (Name.Contains("包序号") ||
                Name.Contains("起始地址") ||
                Name.Contains("个数") ||
                Name.Contains("校验"))
            {
                return false;
            }

            return true;
        }

    }//class
}
