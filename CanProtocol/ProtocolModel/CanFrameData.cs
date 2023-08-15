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
            CanFrameDataInfo yDate = new CanFrameDataInfo();
            yDate.Name = "时间-年";
            yDate.Type = "U8";
            yDate.Value = "0";
            DataInfos.Add(yDate);

            CanFrameDataInfo mDate = new CanFrameDataInfo();
            mDate.Name = "时间-月";
            mDate.Type = "U8";
            mDate.Value = "0";
            DataInfos.Add(mDate);

            CanFrameDataInfo dDate = new CanFrameDataInfo();
            dDate.Name = "时间-日";
            dDate.Type = "U8";
            dDate.Value = "0";
            DataInfos.Add(dDate);

            CanFrameDataInfo date_hour = new CanFrameDataInfo();
            date_hour.Name = "时间-时";
            date_hour.Type = "U8";
            date_hour.Value = "0";
            DataInfos.Add(date_hour);

            CanFrameDataInfo date_minute = new CanFrameDataInfo();
            date_minute.Name = "时间-分/秒";
            date_minute.Type = "U8";
            date_minute.Value = "0";
            DataInfos.Add(date_minute);

            CanFrameDataInfo date_second = new CanFrameDataInfo();
            date_second.Name = "时间-秒";
            date_second.Type = "U8";
            date_second.Value = "0";
            DataInfos.Add(date_second);

            CanFrameDataInfo data1 = new CanFrameDataInfo();
            data1.Name = "电池簇电压";
            data1.Type = "U16";
            data1.Value = "0";
            data1.Unit = "V";
            DataInfos.Add(data1);

            CanFrameDataInfo data2 = new CanFrameDataInfo();
            data2.Name = "电池簇SOC";
            data2.Type = "U8";
            data2.Value = "0";
            data2.Unit = "%";
            DataInfos.Add(data2);

            CanFrameDataInfo data3 = new CanFrameDataInfo();
            data3.Name = "电池簇SOH";
            data3.Type = "U8";
            data3.Value = "0";
            data3.Unit = "%";
            DataInfos.Add(data3);

            CanFrameDataInfo data4 = new CanFrameDataInfo();
            data4.Name = "电池簇电流";
            data4.Type = "I16";
            data4.Value = "0";
            data4.Unit = "mA";
            DataInfos.Add(data4);

            CanFrameDataInfo data5 = new CanFrameDataInfo();
            data5.Name = "继电器状态";
            data5.Type = "U16";
            data5.Value = "0";
            DataInfos.Add(data5);

            CanFrameDataInfo data6 = new CanFrameDataInfo();
            data6.Name = "风扇状态";
            data6.Type = "U16";
            data6.Value = "0";
            DataInfos.Add(data6);

            CanFrameDataInfo data7 = new CanFrameDataInfo();
            data7.Name = "BCU状态";
            data7.Type = "U16";
            data7.Value = "0";
            DataInfos.Add(data7);

            CanFrameDataInfo data8 = new CanFrameDataInfo();
            data8.Name = "充放电使能";
            data8.Type = "U16";
            data8.Value = "0";
            DataInfos.Add(data8);

            CanFrameDataInfo data9 = new CanFrameDataInfo();
            data9.Name = "保护信息1";
            data9.Type = "U16";
            data9.Value = "0";
            DataInfos.Add(data9);

            CanFrameDataInfo data10 = new CanFrameDataInfo();
            data10.Name = "保护信息2";
            data10.Type = "U16";
            data10.Value = "0";
            DataInfos.Add(data10);

            CanFrameDataInfo data11 = new CanFrameDataInfo();
            data11.Name = "保护信息3";
            data11.Type = "U16";
            data11.Value = "0";
            DataInfos.Add(data11);

            CanFrameDataInfo data12 = new CanFrameDataInfo();
            data12.Name = "保护信息4";
            data12.Type = "U16";
            data12.Value = "0";
            DataInfos.Add(data12);

            CanFrameDataInfo data13 = new CanFrameDataInfo();
            data13.Name = "告警信息1";
            data13.Type = "U16";
            data13.Value = "0";
            DataInfos.Add(data13);

            CanFrameDataInfo data14 = new CanFrameDataInfo();
            data14.Name = "告警信息2";
            data14.Type = "U16";
            data14.Value = "0";
            DataInfos.Add(data14);

            CanFrameDataInfo data15 = new CanFrameDataInfo();
            data15.Name = "最高单体电压";
            data15.Type = "U16";
            data15.Value = "0";
            data15.Unit = "mV";
            DataInfos.Add(data15);

            CanFrameDataInfo data16 = new CanFrameDataInfo();
            data16.Name = "最高单体电压BMU序号";
            data16.Type = "U8";
            data16.Value = "0";
            DataInfos.Add(data16);

            CanFrameDataInfo data17 = new CanFrameDataInfo();
            data17.Name = "最高单体电压所在BMU的第几节";
            data17.Type = "U8";
            data17.Value = "0";
            DataInfos.Add(data17);

            CanFrameDataInfo data18 = new CanFrameDataInfo();
            data18.Name = "最低单体电压";
            data18.Type = "U16";
            data18.Value = "0";
            data18.Unit = "mV";
            DataInfos.Add(data18);

            CanFrameDataInfo data19 = new CanFrameDataInfo();
            data19.Name = "最低单体电压BMU序号";
            data19.Type = "U8";
            data19.Value = "0";
            data19.Unit = "个";
            DataInfos.Add(data19);

            CanFrameDataInfo data20 = new CanFrameDataInfo();
            data20.Name = "最低单体电压所在BMU第几节";
            data20.Type = "U8";
            data20.Value = "0";
            DataInfos.Add(data20);

            CanFrameDataInfo data21 = new CanFrameDataInfo();
            data21.Name = "最高单体温度";
            data21.Type = "U16";
            data21.Value = "0";
            data21.Unit = "℃";
            DataInfos.Add(data21);

            CanFrameDataInfo data22 = new CanFrameDataInfo();
            data22.Name = "最高单体温度BMU序号";
            data22.Type = "U8";
            data22.Value = "0";
            DataInfos.Add(data22);

            CanFrameDataInfo data23 = new CanFrameDataInfo();
            data23.Name = "最高单体温度所在BMU的第几节";
            data23.Type = "U8";
            data23.Value = "0";
            DataInfos.Add(data23);

            CanFrameDataInfo data24 = new CanFrameDataInfo();
            data24.Name = "最低单体温度";
            data24.Type = "U16";
            data24.Value = "0";
            data24.Unit = "℃";
            DataInfos.Add(data24);

            CanFrameDataInfo data25 = new CanFrameDataInfo();
            data25.Name = "最低单体温度BMU序号";
            data25.Type = "U8";
            data25.Value = "0";
            DataInfos.Add(data25);

            CanFrameDataInfo data26 = new CanFrameDataInfo();
            data26.Name = "最低单体温度所在BMU第几节";
            data26.Type = "U8";
            data26.Value = "0";
            DataInfos.Add(data26);

            CanFrameDataInfo data27 = new CanFrameDataInfo();
            data27.Name = "最大Pack总压";
            data27.Type = "U16";
            data27.Value = "0";
            data27.Unit = "V";
            DataInfos.Add(data27);

            CanFrameDataInfo data28 = new CanFrameDataInfo();
            data28.Name = "最小Pack总压";
            data28.Type = "U16";
            data28.Value = "0";
            data28.Unit = "V";
            DataInfos.Add(data28);

            CanFrameDataInfo data29 = new CanFrameDataInfo();
            data29.Name = "最大Pack总压编号";
            data29.Type = "U8";
            data29.Value = "0";
            DataInfos.Add(data29);

            CanFrameDataInfo data30 = new CanFrameDataInfo();
            data30.Name = "最小Pack总压编号";
            data30.Type = "U8";
            data30.Value = "0";
            DataInfos.Add(data30);

            CanFrameDataInfo data31 = new CanFrameDataInfo();
            data31.Name = "簇号";
            data31.Type = "U8";
            data31.Value = "0";
            DataInfos.Add(data31);

            CanFrameDataInfo data32 = new CanFrameDataInfo();
            data32.Name = "簇内电池包数量";
            data32.Type = "U8";
            data32.Value = "0";
            DataInfos.Add(data32);

            CanFrameDataInfo data33 = new CanFrameDataInfo();
            data33.Name = "高压绝缘阻抗";
            data33.Type = "U16";
            data33.Value = "0";
            data33.Unit = "V";
            DataInfos.Add(data33);

            CanFrameDataInfo data34 = new CanFrameDataInfo();
            data34.Name = "保险丝后电压";
            data34.Type = "U16";
            data34.Value = "0";
            data34.Unit = "V";
            DataInfos.Add(data34);

            CanFrameDataInfo data35 = new CanFrameDataInfo();
            data35.Name = "功率侧电压";
            data35.Type = "U16";
            data35.Value = "0";
            data35.Unit = "V";
            DataInfos.Add(data35);

            CanFrameDataInfo data36 = new CanFrameDataInfo();
            data36.Name = "负载端电压";
            data36.Type = "U16";
            data36.Value = "0";
            data36.Unit = "V";
            DataInfos.Add(data36);

            CanFrameDataInfo data37 = new CanFrameDataInfo();
            data37.Name = "辅助电源电压";
            data37.Type = "U16";
            data37.Value = "0";
            data37.Unit = "V";
            DataInfos.Add(data37);

            CanFrameDataInfo data38 = new CanFrameDataInfo();
            data38.Name = "电池簇的充电电压";
            data38.Type = "U16";
            data38.Value = "0";
            data38.Unit = "V";
            DataInfos.Add(data38);

            CanFrameDataInfo data39 = new CanFrameDataInfo();
            data39.Name = "电池簇充电电流上限";
            data39.Type = "U16";
            data39.Value = "0";
            data39.Unit = "A";
            DataInfos.Add(data39);

            CanFrameDataInfo data40 = new CanFrameDataInfo();
            data40.Name = "电池簇放电电流上限";
            data40.Type = "U16";
            data40.Value = "0";
            data40.Unit = "A";
            DataInfos.Add(data40);

            CanFrameDataInfo data41 = new CanFrameDataInfo();
            data41.Name = "电池簇的放电截止电压";
            data41.Type = "U16";
            data41.Value = "0";
            data41.Unit = "V";
            DataInfos.Add(data41);

            CanFrameDataInfo data42 = new CanFrameDataInfo();
            data42.Name = "电池包均衡状态";
            data42.Type = "U16";
            data42.Value = "0";
            DataInfos.Add(data42);

            CanFrameDataInfo data43 = new CanFrameDataInfo();
            data43.Name = "最高功率端子温度";
            data43.Type = "I16";
            data43.Value = "0";
            data43.Unit = "℃";
            DataInfos.Add(data43);

            CanFrameDataInfo data44 = new CanFrameDataInfo();
            data44.Name = "环境温度";
            data44.Type = "I16";
            data44.Value = "0";
            data44.Unit = "℃";
            DataInfos.Add(data44);

            CanFrameDataInfo data45 = new CanFrameDataInfo();
            data45.Name = "累计充电安时";
            data45.Type = "U32";
            data45.Value = "0";
            data45.Unit = "Ah";
            DataInfos.Add(data45);

            CanFrameDataInfo data46 = new CanFrameDataInfo();
            data46.Name = "累计放电安时";
            data46.Type = "U32";
            data46.Value = "0";
            data46.Unit = "Ah";
            DataInfos.Add(data46);

            CanFrameDataInfo data47 = new CanFrameDataInfo();
            data47.Name = "累计充电瓦时";
            data47.Type = "U32";
            data47.Value = "0";
            data47.Unit = "Wh";
            DataInfos.Add(data47);

            CanFrameDataInfo data48 = new CanFrameDataInfo();
            data48.Name = "累计放电瓦时";
            data48.Type = "U32";
            data48.Value = "0";
            data48.Unit = "Wh";
            DataInfos.Add(data48);
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
            CanFrameDataInfo yDate = new CanFrameDataInfo();
            yDate.Name = "时间-年";
            yDate.Type = "U8";
            yDate.Value = "0";
            DataInfos.Add(yDate);

            CanFrameDataInfo mDate = new CanFrameDataInfo();
            mDate.Name = "时间-月";
            mDate.Type = "U8";
            mDate.Value = "0";
            DataInfos.Add(mDate);

            CanFrameDataInfo dDate = new CanFrameDataInfo();
            dDate.Name = "时间-日";
            dDate.Type = "U8";
            dDate.Value = "0";
            DataInfos.Add(dDate);

            CanFrameDataInfo date_hour = new CanFrameDataInfo();
            date_hour.Name = "时间-时";
            date_hour.Type = "U8";
            date_hour.Value = "0";
            DataInfos.Add(date_hour);

            CanFrameDataInfo date_minute = new CanFrameDataInfo();
            date_minute.Name = "时间-分/秒";
            date_minute.Type = "U8";
            date_minute.Value = "0";
            DataInfos.Add(date_minute);

            CanFrameDataInfo date_second = new CanFrameDataInfo();
            date_second.Name = "时间-秒";
            date_second.Type = "U8";
            date_second.Value = "0";
            DataInfos.Add(date_second);

            CanFrameDataInfo data1 = new CanFrameDataInfo();
            data1.Name = "电池电压（PACK总压累计）";
            data1.Type = "U16";
            data1.Value = "0";
            data1.Unit = "mV";
            DataInfos.Add(data1);

            CanFrameDataInfo data2 = new CanFrameDataInfo();
            data2.Name = "累计电池电压（电芯电压累计）";
            data2.Type = "U16";
            data2.Value = "0";
            data2.Unit = "mV";
            DataInfos.Add(data2);

            CanFrameDataInfo data3 = new CanFrameDataInfo();
            data3.Name = "SOC显示值";
            data3.Type = "U8";
            data3.Value = "0";
            data3.Unit = "%";
            DataInfos.Add(data3);

            CanFrameDataInfo data4 = new CanFrameDataInfo();
            data4.Name = "SOC显示值";
            data4.Type = "U8";
            data4.Value = "0";
            data4.Unit = "%";
            DataInfos.Add(data4);

            CanFrameDataInfo data5 = new CanFrameDataInfo();
            data5.Name = "SOC计算值";
            data5.Type = "U32";
            data5.Value = "0";
            data5.Unit = "%";
            DataInfos.Add(data5);

            CanFrameDataInfo data6 = new CanFrameDataInfo();
            data6.Name = "SOH计算值";
            data6.Type = "U32";
            data6.Value = "0";
            data6.Unit = "%";
            DataInfos.Add(data6);

            CanFrameDataInfo data7 = new CanFrameDataInfo();
            data7.Name = "电池电流";
            data7.Type = "I16";
            data7.Value = "0";
            data7.Unit = "mA";
            DataInfos.Add(data7);

            CanFrameDataInfo data8 = new CanFrameDataInfo();
            data8.Name = "最高单体电压";
            data8.Type = "U16";
            data8.Value = "0";
            data8.Unit = "mV";
            DataInfos.Add(data8);

            CanFrameDataInfo data9 = new CanFrameDataInfo();
            data9.Name = "最低单体电压";
            data9.Type = "U16";
            data9.Value = "0";
            data9.Unit = "mV";
            DataInfos.Add(data9);

            CanFrameDataInfo data10 = new CanFrameDataInfo();
            data10.Name = "最高单体电压序号";
            data10.Type = "U8";
            data10.Value = "0";
            DataInfos.Add(data10);

            CanFrameDataInfo data11 = new CanFrameDataInfo();
            data11.Name = "最低单体电压序号";
            data11.Type = "U8";
            data11.Value = "0";
            DataInfos.Add(data11);

            CanFrameDataInfo data12 = new CanFrameDataInfo();
            data12.Name = "最高单体温度";
            data12.Type = "U16";
            data12.Value = "0";
            data12.Unit = "℃";
            DataInfos.Add(data12);

            CanFrameDataInfo data13 = new CanFrameDataInfo();
            data13.Name = "最低单体温度";
            data13.Type = "U16";
            data13.Value = "0";
            data13.Unit = "℃";
            DataInfos.Add(data13);

            CanFrameDataInfo data14 = new CanFrameDataInfo();
            data14.Name = "最高单体温度序号";
            data14.Type = "U8";
            data14.Value = "0";
            DataInfos.Add(data14);

            CanFrameDataInfo data15 = new CanFrameDataInfo();
            data15.Name = "最低单体温度序号";
            data15.Type = "U8";
            data15.Value = "0";
            DataInfos.Add(data15);

            CanFrameDataInfo data16 = new CanFrameDataInfo();
            data16.Name = "BMU编号";
            data16.Type = "U8";
            data16.Value = "0";
            DataInfos.Add(data16);

            CanFrameDataInfo data17 = new CanFrameDataInfo();
            data17.Name = "系统状态";
            data17.Type = "U8";
            data17.Value = "0";
            DataInfos.Add(data17);

            CanFrameDataInfo data18 = new CanFrameDataInfo();
            data18.Name = "充放电使能";
            data18.Type = "U16";
            data18.Value = "0";
            DataInfos.Add(data18);

            CanFrameDataInfo data19 = new CanFrameDataInfo();
            data19.Name = "切断请求";
            data19.Type = "U8";
            data19.Value = "0";
            DataInfos.Add(data19);

            CanFrameDataInfo data20 = new CanFrameDataInfo();
            data20.Name = "关机请求";
            data20.Type = "U8";
            data20.Value = "0";
            DataInfos.Add(data20);

            CanFrameDataInfo data21 = new CanFrameDataInfo();
            data21.Name = "充电电流上限";
            data21.Type = "U16";
            data21.Value = "0";
            data21.Unit = "A";
            DataInfos.Add(data21);

            CanFrameDataInfo data22 = new CanFrameDataInfo();
            data22.Name = "放电电流上限";
            data22.Type = "U16";
            data22.Value = "0";
            data22.Unit = "A";
            DataInfos.Add(data22);

            CanFrameDataInfo data23 = new CanFrameDataInfo();
            data23.Name = "保护1";
            data23.Type = "U16";
            data23.Value = "0";
            DataInfos.Add(data23);

            CanFrameDataInfo data24 = new CanFrameDataInfo();
            data24.Name = "保护2";
            data24.Type = "U16";
            data24.Value = "0";
            DataInfos.Add(data24);

            CanFrameDataInfo data25 = new CanFrameDataInfo();
            data25.Name = "告警1";
            data25.Type = "U16";
            data25.Value = "0";
            DataInfos.Add(data25);

            CanFrameDataInfo data26 = new CanFrameDataInfo();
            data26.Name = "告警2";
            data26.Type = "U16";
            data26.Value = "0";
            DataInfos.Add(data26);

            CanFrameDataInfo data27 = new CanFrameDataInfo();
            data27.Name = "故障1";
            data27.Type = "U16";
            data27.Value = "0";
            DataInfos.Add(data27);

            CanFrameDataInfo data28 = new CanFrameDataInfo();
            data28.Name = "故障2";
            data28.Type = "U16";
            data28.Value = "0";
            DataInfos.Add(data28);

            CanFrameDataInfo data29 = new CanFrameDataInfo();
            data29.Name = "主动均衡状态";
            data29.Type = "U16";
            data29.Value = "0";
            DataInfos.Add(data29);

            CanFrameDataInfo data30 = new CanFrameDataInfo();
            data30.Name = "均衡母线电压";
            data30.Type = "U16";
            data30.Value = "0";
            data30.Unit = "mV";
            DataInfos.Add(data30);

            CanFrameDataInfo data31 = new CanFrameDataInfo();
            data31.Name = "均衡母线电流";
            data31.Type = "I16";
            data31.Value = "0";
            data31.Unit = "mA";
            DataInfos.Add(data31);

            CanFrameDataInfo data32 = new CanFrameDataInfo();
            data32.Name = "辅助供电电压";
            data32.Type = "U16";
            data32.Value = "0";
            data32.Unit = "mV";
            DataInfos.Add(data32);

            CanFrameDataInfo data33 = new CanFrameDataInfo();
            data33.Name = "满充容量";
            data33.Type = "U16";
            data33.Value = "0";
            data33.Unit = "Ah";
            DataInfos.Add(data33);

            CanFrameDataInfo data34 = new CanFrameDataInfo();
            data34.Name = "循环次数";
            data34.Type = "U16";
            data34.Value = "0";
            DataInfos.Add(data34);

            CanFrameDataInfo data35 = new CanFrameDataInfo();
            data35.Name = "累计放电安时";
            data35.Type = "U32";
            data35.Value = "0";
            data35.Unit = "Ah";
            DataInfos.Add(data35);

            CanFrameDataInfo data36 = new CanFrameDataInfo();
            data36.Name = "累计充电安时";
            data36.Type = "U32";
            data36.Value = "0";
            data36.Unit = "Ah";
            DataInfos.Add(data36);

            CanFrameDataInfo data37 = new CanFrameDataInfo();
            data37.Name = "累计放电瓦时";
            data37.Type = "U32";
            data37.Value = "0";
            data37.Unit = "Wh";
            DataInfos.Add(data37);

            CanFrameDataInfo data38 = new CanFrameDataInfo();
            data38.Name = "累计充电瓦时";
            data38.Type = "U32";
            data38.Value = "0";
            data38.Unit = "Wh";
            DataInfos.Add(data38);

            CanFrameDataInfo data39 = new CanFrameDataInfo();
            data39.Name = "环境温度";
            data39.Type = "I16";
            data39.Value = "0";
            data39.Unit = "℃";
            DataInfos.Add(data39);

            CanFrameDataInfo data40 = new CanFrameDataInfo();
            data40.Name = "dcdc温度1";
            data40.Type = "I16";
            data40.Value = "0";
            data40.Unit = "℃";
            DataInfos.Add(data40);

            CanFrameDataInfo data41 = new CanFrameDataInfo();
            data41.Name = "dcdc温度2";
            data41.Type = "I16";
            data41.Value = "0";
            data41.Unit = "℃";
            DataInfos.Add(data41);

            CanFrameDataInfo data42 = new CanFrameDataInfo();
            data42.Name = "均衡温度1";
            data42.Type = "I16";
            data42.Value = "0";
            data42.Unit = "℃";
            DataInfos.Add(data42);

            CanFrameDataInfo data43 = new CanFrameDataInfo();
            data43.Name = "均衡温度2";
            data43.Type = "I16";
            data43.Value = "0";
            data43.Unit = "℃";
            DataInfos.Add(data43);

            CanFrameDataInfo data44 = new CanFrameDataInfo();
            data44.Name = "1-16串均衡状态";
            data44.Type = "U16";
            data44.Value = "0";
            DataInfos.Add(data44);

            CanFrameDataInfo data45 = new CanFrameDataInfo();
            data45.Name = "单体电压1";
            data45.Type = "U16";
            data45.Value = "0";
            data45.Unit = "mV";
            DataInfos.Add(data45);

            CanFrameDataInfo data46 = new CanFrameDataInfo();
            data46.Name = "单体电压2";
            data46.Type = "U16";
            data46.Value = "0";
            data46.Unit = "mV";
            DataInfos.Add(data46);

            CanFrameDataInfo data47 = new CanFrameDataInfo();
            data47.Name = "单体电压3";
            data47.Type = "U16";
            data47.Value = "0";
            data47.Unit = "mV";
            DataInfos.Add(data47);

            CanFrameDataInfo data48 = new CanFrameDataInfo();
            data48.Name = "单体电压4";
            data48.Type = "U16";
            data48.Value = "0";
            data48.Unit = "mV";
            DataInfos.Add(data48);

            CanFrameDataInfo data49 = new CanFrameDataInfo();
            data49.Name = "单体电压5";
            data49.Type = "U16";
            data49.Value = "0";
            data49.Unit = "mV";
            DataInfos.Add(data49);

            CanFrameDataInfo data50 = new CanFrameDataInfo();
            data50.Name = "单体电压6";
            data50.Type = "U16";
            data50.Value = "0";
            data50.Unit = "mV";
            DataInfos.Add(data50);

            CanFrameDataInfo data51 = new CanFrameDataInfo();
            data51.Name = "单体电压7";
            data51.Type = "U16";
            data51.Value = "0";
            data51.Unit = "mV";
            DataInfos.Add(data51);

            CanFrameDataInfo data52 = new CanFrameDataInfo();
            data52.Name = "单体电压8";
            data52.Type = "U16";
            data52.Value = "0";
            data52.Unit = "mV";
            DataInfos.Add(data52);

            CanFrameDataInfo data53 = new CanFrameDataInfo();
            data53.Name = "单体电压9";
            data53.Type = "U16";
            data53.Value = "0";
            data53.Unit = "mV";
            DataInfos.Add(data53);

            CanFrameDataInfo data54 = new CanFrameDataInfo();
            data54.Name = "单体电压10";
            data54.Type = "U16";
            data54.Value = "0";
            data54.Unit = "mV";
            DataInfos.Add(data54);

            CanFrameDataInfo data55 = new CanFrameDataInfo();
            data55.Name = "单体电压11";
            data55.Type = "U16";
            data55.Value = "0";
            data55.Unit = "mV";
            DataInfos.Add(data55);

            CanFrameDataInfo data56 = new CanFrameDataInfo();
            data56.Name = "单体电压12";
            data56.Type = "U16";
            data56.Value = "0";
            data56.Unit = "mV";
            DataInfos.Add(data56);

            CanFrameDataInfo data57 = new CanFrameDataInfo();
            data57.Name = "单体电压13";
            data57.Type = "U16";
            data57.Value = "0";
            data57.Unit = "mV";
            DataInfos.Add(data57);

            CanFrameDataInfo data58 = new CanFrameDataInfo();
            data58.Name = "单体电压14";
            data58.Type = "U16";
            data58.Value = "0";
            data58.Unit = "mV";
            DataInfos.Add(data58);

            CanFrameDataInfo data59 = new CanFrameDataInfo();
            data59.Name = "单体电压15";
            data59.Type = "U16";
            data59.Value = "0";
            data59.Unit = "mV";
            DataInfos.Add(data59);

            CanFrameDataInfo data60 = new CanFrameDataInfo();
            data60.Name = "单体电压16";
            data60.Type = "U16";
            data60.Value = "0";
            data60.Unit = "mV";
            DataInfos.Add(data60);

            CanFrameDataInfo data61 = new CanFrameDataInfo();
            data61.Name = "单体温度1";
            data61.Type = "U16";
            data61.Value = "0";
            data61.Unit = "mV";
            DataInfos.Add(data61);

            CanFrameDataInfo data62 = new CanFrameDataInfo();
            data62.Name = "单体温度2";
            data62.Type = "U16";
            data62.Value = "0";
            data62.Unit = "mV";
            DataInfos.Add(data62);

            CanFrameDataInfo data63 = new CanFrameDataInfo();
            data63.Name = "单体温度3";
            data63.Type = "U16";
            data63.Value = "0";
            data63.Unit = "mV";
            DataInfos.Add(data63);

            CanFrameDataInfo data64 = new CanFrameDataInfo();
            data64.Name = "单体温度4";
            data64.Type = "U16";
            data64.Value = "0";
            data64.Unit = "mV";
            DataInfos.Add(data64);

            CanFrameDataInfo data65 = new CanFrameDataInfo();
            data65.Name = "单体温度5";
            data65.Type = "U16";
            data65.Value = "0";
            data65.Unit = "mV";
            DataInfos.Add(data65);

            CanFrameDataInfo data66 = new CanFrameDataInfo();
            data66.Name = "单体温度6";
            data66.Type = "U16";
            data66.Value = "0";
            data66.Unit = "mV";
            DataInfos.Add(data66);

            CanFrameDataInfo data67 = new CanFrameDataInfo();
            data67.Name = "单体温度7";
            data67.Type = "U16";
            data67.Value = "0";
            data67.Unit = "mV";
            DataInfos.Add(data67);

            CanFrameDataInfo data68 = new CanFrameDataInfo();
            data68.Name = "单体温度8";
            data68.Type = "U16";
            data68.Value = "0";
            data68.Unit = "mV";
            DataInfos.Add(data68);

            CanFrameDataInfo data69 = new CanFrameDataInfo();
            data69.Name = "单体温度9";
            data69.Type = "U16";
            data69.Value = "0";
            data69.Unit = "mV";
            DataInfos.Add(data69);

            CanFrameDataInfo data70 = new CanFrameDataInfo();
            data70.Name = "单体温度10";
            data70.Type = "U16";
            data70.Value = "0";
            data70.Unit = "mV";
            DataInfos.Add(data70);

            CanFrameDataInfo data71 = new CanFrameDataInfo();
            data71.Name = "单体温度11";
            data71.Type = "U16";
            data71.Value = "0";
            data71.Unit = "mV";
            DataInfos.Add(data71);

            CanFrameDataInfo data72 = new CanFrameDataInfo();
            data72.Name = "单体温度12";
            data72.Type = "U16";
            data72.Value = "0";
            data72.Unit = "mV";
            DataInfos.Add(data72);

            CanFrameDataInfo data73 = new CanFrameDataInfo();
            data73.Name = "单体温度13";
            data73.Type = "U16";
            data73.Value = "0";
            data73.Unit = "mV";
            DataInfos.Add(data73);

            CanFrameDataInfo data74 = new CanFrameDataInfo();
            data74.Name = "单体温度14";
            data74.Type = "U16";
            data74.Value = "0";
            data74.Unit = "mV";
            DataInfos.Add(data74);

            CanFrameDataInfo data75 = new CanFrameDataInfo();
            data75.Name = "单体温度15";
            data75.Type = "U16";
            data75.Value = "0";
            data75.Unit = "mV";
            DataInfos.Add(data75);

            CanFrameDataInfo data76 = new CanFrameDataInfo();
            data76.Name = "单体温度16";
            data76.Type = "U16";
            data76.Value = "0";
            data76.Unit = "mV";
            DataInfos.Add(data76);
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
        public void AddInitMultiDataToFaultInfo() {
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
