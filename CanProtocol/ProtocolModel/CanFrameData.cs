using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

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
        /// 增加文件传输子设备交互报文-BCU运行数据日志
        /// 用于连续-多包
        /// </summary>
        public void AddInitMultiDataToBCU()
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

            #region BCU汇总数据
            //时间
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-年",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-月",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-日",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-时",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-分/秒",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-秒",
                Type = "U8",
                Value = "0"
            });

            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇电压",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇SOC",
                Type = "U8",
                Value = "0",
                Unit = "%"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇SOH",
                Type = "U8",
                Value = "0",
                Unit = "%"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇电流",
                Type = "I16",
                Value = "0",
                Unit = "mA",
                Precision = Convert.ToDecimal(10)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "继电器状态",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "风扇状态",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "继电器切断请求",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "BCU状态",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "充放电使能",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "保护信息1",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "保护信息2",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "保护信息3",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "保护信息4",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "告警信息1",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "告警信息2",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最高PACK电压",
                Type = "U16",
                Value = "0",
                Unit = "V",

            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最低PACK电压",
                Type = "U16",
                Value = "0",
                Unit = "V",

            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最高PACK电压序号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最低PACK电压序号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "簇号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "簇内电池包数量",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "高压绝缘阻抗",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "保险丝后电压",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "功率侧电压",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "负载端电压",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "辅助电源电压",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇的充电电压",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇充电电流上限",
                Type = "U16",
                Value = "0",
                Unit = "A",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇放电电流上限",
                Type = "U16",
                Value = "0",
                Unit = "A",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池簇的放电截止电压",
                Type = "U16",
                Value = "0",
                Unit = "V",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池包均衡状态",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最高功率端子温度",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "环境温度",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计充电安时",
                Type = "U32",
                Value = "0",
                Unit = "Ah"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计放电安时",
                Type = "U32",
                Value = "0",
                Unit = "Ah"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计充电瓦时",
                Type = "U32",
                Value = "0",
                Unit = "Wh"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计放电瓦时",
                Type = "U32",
                Value = "0",
                Unit = "Wh"
            });

            //16串电芯 BMU_NO和电芯电压1~16
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "BMU编号",
                Type = "U8",
                Value = "0"
            });

            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压1",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压2",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压3",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压4",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压5",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压6",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压7",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压8",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压9",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压10",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压11",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压12",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压13",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压14",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压15",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电芯电压16",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });

            //PACK1~10最大单体电压
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK1最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK2最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK3最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK4最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK5最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK6最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK7最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK8最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK9最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK10最大单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });

            //PACK1~10最小单体电压
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK1最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK2最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK3最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK4最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK5最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK6最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK7最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK8最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK9最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK10最小单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });

            //PACK1~10平均单体电压
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK1平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK2平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK3平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK4平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK5平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK6平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK7平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK8平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK9平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK10平均单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });

            //PACK1~10最大单体温度
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK1最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK2最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK3最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK4最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK5最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK6最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK7最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK8最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK9最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK10最大单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });

            //PACK1~10最小单体温度
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK1最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK2最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK3最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK4最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK5最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK6最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK7最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK8最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK9最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "PACK10最小单体温度",
                Type = "I8",
                Value = "0",
                Unit = "℃"
            });

            //预留1-2
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "预留1",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "预留2",
                Type = "U32",
                Value = "0"
            });
            #endregion

            CanFrameDataInfo info4 = new CanFrameDataInfo();
            info4.Name = "CRC校验";
            info4.Type = "U16";
            info4.Value = "0x0";
            DataInfos.Add(info4);
        }

        /// <summary>
        /// 增加文件传输子设备交互报文-BMU运行数据日志
        /// 用于连续-多包
        /// </summary>
        public void AddInitMultiDataToBMU()
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

            #region BMU汇总数据
            //时间
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-年",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-月",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-日",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-时",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-分",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "时间-秒",
                Type = "U8",
                Value = "0"
            });

            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池采样电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池累计电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "SOC显示值",
                Type = "U8",
                Value = "0",
                Unit = "%"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "SOC显示值",
                Type = "U8",
                Value = "0",
                Unit = "%"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "SOC计算值",
                Type = "U32",
                Value = "0",
                Unit = "%",
                Precision = Convert.ToDecimal(0.001)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "SOH计算值",
                Type = "U32",
                Value = "0",
                Unit = "%",
                Precision = Convert.ToDecimal(0.001)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "电池电流",
                Type = "I16",
                Value = "0",
                Unit = "mA",
                Precision = Convert.ToDecimal(10)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最高单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最低单体电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最高单体电压序号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最低单体电压序号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最高单体温度",
                Type = "U16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最低单体温度",
                Type = "U16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最高单体温度序号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "最低单体温度序号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "BMU编号",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "系统状态",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "充放电使能",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "切断请求",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "关机请求",
                Type = "U8",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "充电电流上限",
                Type = "U16",
                Value = "0",
                Unit = "A"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "放电电流上限",
                Type = "U16",
                Value = "0",
                Unit = "A"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "保护1",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "保护2",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "告警1",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "告警2",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "故障1",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "故障2",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "主动均衡状态",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "均衡母线电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "均衡母线电流",
                Type = "I16",
                Value = "0",
                Unit = "mA"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "辅助供电电压",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "满充容量",
                Type = "U16",
                Value = "0",
                Unit = "Ah"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "循环次数",
                Type = "U16",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计放电安时",
                Type = "U32",
                Value = "0",
                Unit = "Ah"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计充电安时",
                Type = "U32",
                Value = "0",
                Unit = "Ah"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计放电瓦时",
                Type = "U32",
                Value = "0",
                Unit = "Wh"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "累计充电瓦时",
                Type = "U32",
                Value = "0",
                Unit = "Wh"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "环境温度",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "dcdc温度1",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "dcdc温度2",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "均衡温度1",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "均衡温度2",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "1-16串均衡状态",
                Type = "U16",
                Value = "0"
            });

            //单体电压1~16
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压1",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压2",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压3",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压4",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压5",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压6",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压7",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压8",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压9",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压10",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压11",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压12",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压13",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压14",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压15",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体电压16",
                Type = "U16",
                Value = "0",
                Unit = "mV"
            });

            //单体温度1~16
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度1",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度2",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度3",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度4",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度5",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度6",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度7",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度8",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度9",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度10",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度11",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度12",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度13",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度14",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度15",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "单体温度16",
                Type = "I16",
                Value = "0",
                Unit = "℃",
                Precision = Convert.ToDecimal(0.1)
            });

            //RSV1~10
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV1",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV2",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV3",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV4",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV5",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV6",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV7",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV8",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV9",
                Type = "U32",
                Value = "0"
            });
            DataInfos.Add(new CanFrameDataInfo()
            {
                Name = "RSV10",
                Type = "U32",
                Value = "0"
            });
            #endregion

            CanFrameDataInfo info4 = new CanFrameDataInfo();
            info4.Name = "CRC校验";
            info4.Type = "U16";
            info4.Value = "0x0";
            DataInfos.Add(info4);
        }

        /// <summary>
        /// 增加文件传输子设备交互报文-故障录播数据
        /// 用于连续-多包
        /// </summary>
        public void AddInitMultiDataToFaultInfo()
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

            #region 故障录播信息
            CanFrameDataInfo data1 = new CanFrameDataInfo();
            data1.Name = "电流";
            data1.Type = "I16";
            data1.Value = "0";
            data1.Unit = "A";
            DataInfos.Add(data1);

            CanFrameDataInfo data2 = new CanFrameDataInfo();
            data2.Name = "最大电压";
            data2.Type = "U16";
            data2.Value = "0";
            data2.Unit = "V";
            DataInfos.Add(data2);

            CanFrameDataInfo data3 = new CanFrameDataInfo();
            data3.Name = "最小电压";
            data3.Type = "U16";
            data3.Value = "0";
            data3.Unit = "V";
            DataInfos.Add(data3);

            CanFrameDataInfo data4 = new CanFrameDataInfo();
            data4.Name = "最大温度";
            data4.Type = "I8";
            data4.Value = "0";
            data4.Unit = "℃";
            DataInfos.Add(data4);

            CanFrameDataInfo data5 = new CanFrameDataInfo();
            data5.Name = "最小温度";
            data5.Type = "I8";
            data5.Value = "0";
            data5.Unit = "℃";
            DataInfos.Add(data5);
            #endregion

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
                else if (type.Contains("16")|| type.Contains("hex"))
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
