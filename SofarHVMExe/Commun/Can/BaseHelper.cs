using CanProtocol.ProtocolModel;
using Communication.Can;
using SofarHVMExe.Commun;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml.Linq;

namespace SofarHVMExe
{
    public class BaseHelper_old
    {
        //private static int _maxPackageNum = 100; //接收数据时，允许的最大包数量，超出此数量则不在进行接收
        ////上一次匹配的点表地址（用于连续多包的地址匹配）
        //private static string _lastTableAddr = "";

        ////接收的多包数据缓存集合
        //private static List<MultyPackage> s_packageBufferList = new List<MultyPackage>();


        //public static CanFrameModel? GetAnalysisData(FrameConfigModel frameCfgModel, uint id, byte[] data)
        //{
        //    if (data.Length != 8)
        //        return null;

        //    byte selectAddr = DeviceManager.Instance().GetSelectDev();
        //    CanFrameID frameId = new CanFrameID();

        //    foreach (CanFrameModel frame in frameCfgModel.CanFrameModels)
        //    {
        //        //匹配id
        //        {
        //            frameId.ID = frame.Id;
        //            if (selectAddr != 0)
        //            {
        //                frameId.SrcAddr = selectAddr;
        //            }

        //            if (frameId.ID != id)
        //                continue;
        //        }

        //        CanFrameModel newFrame = new CanFrameModel(frame);
        //        newFrame.FrameId.ID = frameId.ID;

        //        //查找相同点表起始地址
        //        if (newFrame.FrameId.ContinuousFlag == 0)
        //        {
        //            //非连续
        //            int addr = data[0];
        //            addr |= (int)(data[1]) << 8;
        //            CanFrameData canFrameData = newFrame.FrameDatas.Find(t =>
        //            {
        //                //匹配点表起始地址
        //                string strAddr = t.DataInfos[0].Value;
        //                if (strAddr.Contains("0x") || strAddr.Contains("0X"))
        //                {
        //                    strAddr = strAddr.Replace("0x", "").Replace("0X", "");
        //                    int tmpAddr = int.Parse(strAddr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        //                    return tmpAddr == addr;
        //                }
        //                else
        //                {
        //                    return int.Parse(strAddr) == addr;
        //                }
        //            });

        //            if (canFrameData == null)
        //            {
        //                //id相同，而点表地址不相同，仍表示不匹配
        //                continue;
        //            }

        //            //帧字节数据转字段值
        //            canFrameData.Data = data;

        //            try
        //            {
        //                ByteDataToDataInfo(canFrameData);
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"接收解析非连续帧错误 id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
        //            }
        //            return newFrame;
        //        }
        //        else
        //        {
        //            ////连续
        //            //CanFrameData fstFrameData = newFrame.FrameDatas[0];
        //            //byte _packNum = data[0];

        //            /////当前frameData转多包
        //            //List<CanFrameData> frameDataList = ToMulPackage(fstFrameData.DataInfos);
        //            //string addr = frameDataList[0].DataInfos[1].Value;

        //            ////匹配点表地址
        //            //if (_lastTableAddr != "" && addr != _lastTableAddr)
        //            //{
        //            //    continue;
        //            //}

        //            /////匹配当前帧是哪一包数据，解析显示数据
        //            //CanFrameData? frameData = frameDataList.Find((o) =>
        //            //{
        //            //    string strPackNum = o.DataInfos[0].Value;
        //            //    byte packNum = GetPackageIndex(strPackNum);

        //            //    //匹配包序号
        //            //    if (_packNum == packNum)
        //            //    {
        //            //        _lastTableAddr = addr; //记录本次地址，用作下次匹配
        //            //        return true;
        //            //    }
        //            //    else
        //            //        return false;
        //            //});

        //            //if (frameData != null)
        //            //{
        //            //    frameData.Data = data;
        //            //    ByteDataToDataInfo(frameData);
        //            //    newFrame.FrameDatas.Clear();
        //            //    newFrame.FrameDatas.Add(frameData);
        //            //    return newFrame;
        //            //}
        //        }//else


        //    }//foreach

        //    return null;
        //}//func

        //public static CanFrameModel? GetAnalysisMultyData(FrameConfigModel frameCfgModel, uint id, byte[] data)
        //{
        //    CanFrameModel? findFrame = null;
        //    int packageIndex = data[0];
        //    int addr = data[1] | (data[2]) << 8;

        //    byte selectAddr = DeviceManager.Instance().GetSelectDev();
        //    CanFrameID frameId = new CanFrameID();

        //    //1、查询当前帧是否在帧配置中
        //    foreach (CanFrameModel frame in frameCfgModel.CanFrameModels)
        //    {
        //        //匹配id
        //        {
        //            if (frame.FrameId.ContinuousFlag != 1)
        //                continue;

        //            if (frame.Id != id)
        //                continue;

        //            //frameId.ID = frame.Id;
        //            //if (selectAddr != 0)
        //            //{
        //            //    frameId.SrcAddr = selectAddr;
        //            //}

        //            //if (frameId.ID != id)
        //            //    continue;
        //        }

        //        if (packageIndex == 0)
        //        {
        //            //第一包 匹配点表起始地址
        //            int tmpAddr = frame.GetAddrInt();
        //            if (tmpAddr == addr)
        //            {
        //                findFrame = frame;
        //                //findFrame.FrameId.SrcAddr = frameId.SrcAddr;
        //                break;
        //            }
        //        }
        //        else
        //        {
        //            //不是第一包 只匹配id
        //            findFrame = frame;
        //            //findFrame.FrameId.SrcAddr = frameId.SrcAddr;
        //            break;
        //        }
        //    }//foreach

        //    if (findFrame == null)
        //        return null;


        //    //2、将当前帧加入到包缓存中
        //    //为第一包或中间包，则加入到缓存中
        //    //为最后一包或超出包数量限制，则返回此多包显示到界面上
        //    MultyPackage? findMulPackage = s_packageBufferList.Find((o) =>
        //    {
        //        return (id == o.frame.Id);
        //    });

        //    if (findMulPackage == null)
        //    {
        //        //不在缓存中
        //        if (packageIndex == 0)
        //        {
        //            MultyPackage newMulPackage = new MultyPackage();
        //            newMulPackage.packageNum++;
        //            newMulPackage.datas.AddRange(data);
        //            newMulPackage.frame = findFrame;
        //            s_packageBufferList.Add(newMulPackage);
        //        }
        //        else
        //        {
        //            //不是第一包则丢弃
        //        }
        //    }
        //    else
        //    {
        //        //在缓存中，为最后一包或超出包数限制，则出包
        //        if (packageIndex == 0)
        //        {
        //            //第一包丢弃
        //        }
        //        else if (packageIndex == 0xff)
        //        {
        //            //最后一包 出包
        //            findMulPackage.datas.AddRange(data.Skip(1));
        //            findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
        //            CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);
        //            try
        //            {
        //                ByteDataToDataInfo(resultFrame.FrameDatas[0]);
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"接收解析连续帧错误 id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
        //            }

        //            s_packageBufferList.Remove(findMulPackage);
        //            return resultFrame;
        //        }
        //        else
        //        {
        //            if (findMulPackage.packageNum > _maxPackageNum)
        //            {
        //                //包超出范围 出包
        //                findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
        //                CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);
        //                try
        //                {
        //                    ByteDataToDataInfo(resultFrame.FrameDatas[0]);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Debug.WriteLine($"接收解析连续帧错误 id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
        //                }

        //                s_packageBufferList.Remove(findMulPackage);
        //                return resultFrame;
        //            }
        //            else
        //            {
        //                findMulPackage.packageNum++;
        //                findMulPackage.datas.AddRange(data.Skip(1)); //去除包序号
        //            }
        //        }
        //    }

        //    return null;
        //    //CanFrameModel newFrame = new CanFrameModel(findFrame);

        //    ////帧字节数据转字段值
        //    //canFrameData.Data = data;

        //    //try
        //    //{
        //    //    ByteDataToDataInfo(canFrameData);
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    Debug.WriteLine($"接收解析非连续帧错误 id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
        //    //}
        //    //return newFrame;






        //}

        ///// <summary>
        ///// 字节数据转字段信息
        ///// </summary>
        ///// <param name="frameData"></param>
        //private static void ByteDataToDataInfo(CanFrameData frameData)
        //{
        //    int index = 0;
        //    int count = frameData.DataInfos.Count;
        //    byte[] datas = frameData.Data;

        //    for (int i = 0; i < count; i++)
        //    {
        //        CanFrameDataInfo dataInfo = frameData.DataInfos[i];
        //        long realVal = 0;
        //        float fRealVal = 0.0f;

        //        //数据类型
        //        if (dataInfo.Type == "U8" || dataInfo.Type == "I8" || dataInfo.Type == "char")
        //        {
        //            realVal = datas[index++];
        //        }
        //        else if (dataInfo.Type == "U16")
        //        {
        //            int byte1 = datas[index++];
        //            int byte2 = datas[index++] << 8;
        //            realVal = byte1 + byte2;
        //        }
        //        else if (dataInfo.Type == "I16")
        //        {
        //            string byte1 = datas[index++].ToString("X2");
        //            string byte2 = datas[index++].ToString("X2");
        //            realVal = Convert.ToInt16(byte2 + byte1, 16);
        //        }
        //        else if (dataInfo.Type == "U32")
        //        {
        //            long byte1 = datas[index++];
        //            long byte2 = datas[index++] << 8;
        //            long byte3 = datas[index++] << 16;
        //            long byte4 = datas[index++] << 24;

        //            realVal = byte1 + byte2 + byte3 + byte4;
        //        }
        //        else if (dataInfo.Type == "I32")
        //        {
        //            string byte1 = datas[index++].ToString("X2");
        //            string byte2 = datas[index++].ToString("X2");
        //            string byte3 = datas[index++].ToString("X2");
        //            string byte4 = datas[index++].ToString("X2");

        //            realVal = Convert.ToInt32(byte4 + byte3 + byte2 + byte1, 16);
        //        }
        //        else if (dataInfo.Type == "float")
        //        {
        //            byte[] arr = new byte[4];
        //            arr[0] = datas[index++];
        //            arr[1] = datas[index++];
        //            arr[2] = datas[index++];
        //            arr[3] = datas[index++];
        //            fRealVal = BitConverter.ToSingle(arr);
        //        }

        //        //处理精度问题
        //        string type = dataInfo.Type;

        //        if (type != "float" && type != "char")
        //        {
        //            if (dataInfo.Precision != null)
        //            {
        //                realVal = (long)((dataInfo.Precision ?? 1) * (decimal)realVal);
        //            }

        //            if (dataInfo.Value.Contains("0x") || dataInfo.Value.Contains("0X"))
        //            {
        //                //十六进制值
        //                dataInfo.Value = "0x" + realVal.ToString("X");
        //            }
        //            else
        //            {
        //                //十进制值
        //                dataInfo.Value = realVal.ToString();
        //            }
        //        }
        //        else if (type == "float")
        //        {
        //            if (dataInfo.Precision != null)
        //            {
        //                fRealVal = (float)(dataInfo.Precision ?? 1) * (fRealVal);
        //            }

        //            dataInfo.Value = fRealVal.ToString();
        //        }
        //        else if (type == "char")
        //        {
        //            dataInfo.Value = realVal.ToString();
        //        }
        //    }//for
        //}//func

        //public static bool AnalysisData(CanFrameModel canFrameModel)
        //{
        //    return AnalysisData(canFrameModel.FrameDatas);
        //}

        //public static bool AnalysisData(List<CanFrameData> frameDatas)
        //{
        //    if (frameDatas == null || frameDatas.Count == 0)
        //        return false;

        //    int count = frameDatas.Count;
        //    for (int i = 0; i < count; ++i)
        //    {
        //        CanFrameData frameData = frameDatas[i];
        //        List<byte> DateByte = AnalysisData(frameData);

        //        //不足8个字节补0
        //        {
        //            if (DateByte.Count < 8)
        //            {
        //                int num = 8 - DateByte.Count;
        //                DateByte.AddRange(new byte[num]);
        //            }
        //        }

        //        //超过长度截取
        //        {
        //            //if (DateByte.Count > 8)
        //            //{
        //            //    DateByte = DateByte.Take(8).ToList();
        //            //}
        //        }
        //        frameData.Data = DateByte.ToArray();
        //    }

        //    return true;
        //}//func

        //public static List<byte> AnalysisData(CanFrameData frameData)
        //{
        //    List<byte> DateByte = new List<byte>();
        //    foreach (CanFrameDataInfo dataInfo in frameData.DataInfos)
        //    {
        //        byte[] writeByte;
        //        string strVal = dataInfo.Value;
        //        if (strVal.Contains("0x") || strVal.Contains("0X"))
        //        {
        //            //十六进制数转十进制数处理
        //            long value = 0;
        //            strVal = strVal.Replace("0x", "").Replace("0X", "");
        //            strVal = long.Parse(strVal, NumberStyles.HexNumber).ToString();
        //        }


        //        //处理精度问题
        //        if (dataInfo.Type != "ASCII" && dataInfo.Type != "BCD16")
        //        {
        //            if (dataInfo.Precision != null)
        //            {
        //                strVal = (decimal.Parse(strVal) / (dataInfo.Precision ?? 1)).ToString();
        //            }
        //        }

        //        //数据类型
        //        switch (dataInfo.Type)
        //        {
        //            case "ASCII":
        //                writeByte = Encoding.ASCII.GetBytes(strVal);
        //                DateByte.AddRange(writeByte);
        //                break;
        //            case "U8":
        //            case "I8":
        //                {
        //                    byte value;
        //                    byte.TryParse(strVal, out value);
        //                    DateByte.Add(value);
        //                }
        //                break;
        //            case "BCD16":
        //                {
        //                    int value;
        //                    int.TryParse(strVal, out value);
        //                    byte[] data = new byte[2];
        //                    data[0] = (byte)(value & 0xff);
        //                    data[1] = (byte)(value >> 8);
        //                    DateByte.AddRange(data);
        //                }
        //                break;
        //            case "U16":
        //                {
        //                    ushort value;
        //                    ushort.TryParse(strVal, out value);
        //                    writeByte = HexDataHelper.ShortToByte(value);
        //                    DateByte.AddRange(writeByte);
        //                }
        //                break;
        //            case "I16":
        //                {
        //                    short value;
        //                    short.TryParse(strVal, out value);
        //                    writeByte = HexDataHelper.ShortToByte(value);
        //                    DateByte.AddRange(writeByte);
        //                }
        //                break;
        //            case "U32":
        //            case "I32":
        //                {
        //                    int value;
        //                    int.TryParse(strVal, out value);
        //                    writeByte = HexDataHelper.IntToByte(value, false);
        //                    DateByte.AddRange(writeByte);
        //                }
        //                break;
        //            default:
        //                //writeByte = HexDataHelper.HexStringToByte(dataInfo.Value);
        //                break;
        //        }
        //    }

        //    return DateByte;
        //}

        //public static List<byte> AnalysisData(ObservableCollection<CanFrameDataInfo> dataInfos)
        //{
        //    List<byte> DateByte = new List<byte>();
        //    foreach (CanFrameDataInfo dataInfo in dataInfos)
        //    {
        //        DateByte.AddRange(AnalysisData(dataInfo));
        //    }

        //    return DateByte;
        //}

        //public static List<byte> AnalysisData(CanFrameDataInfo dataInfo)
        //{
        //    List<byte> DateByte = new List<byte>();
        //    byte[] writeByte;
        //    string strVal = dataInfo.Value;
        //    if (strVal.Contains("0x") || strVal.Contains("0X"))
        //    {
        //        //十六进制数转十进制数处理
        //        long value = 0;
        //        strVal = strVal.Replace("0x", "").Replace("0X", "");
        //        strVal = long.Parse(strVal, NumberStyles.HexNumber).ToString();
        //    }


        //    //处理精度问题
        //    if (dataInfo.Type != "ASCII" && dataInfo.Type != "BCD16")
        //    {
        //        if (dataInfo.Precision != null)
        //        {
        //            strVal = (decimal.Parse(strVal) / (dataInfo.Precision ?? 1)).ToString();
        //        }
        //    }

        //    //数据类型
        //    switch (dataInfo.Type)
        //    {
        //        case "ASCII":
        //            writeByte = Encoding.ASCII.GetBytes(strVal);
        //            DateByte.AddRange(writeByte);
        //            break;
        //        case "U8":
        //        case "I8":
        //            {
        //                byte value;
        //                byte.TryParse(strVal, out value);
        //                DateByte.Add(value);
        //            }
        //            break;
        //        case "BCD16":
        //            {
        //                int value;
        //                int.TryParse(strVal, out value);
        //                byte[] data = new byte[2];
        //                data[0] = (byte)(value & 0xff);
        //                data[1] = (byte)(value >> 8);
        //                DateByte.AddRange(data);
        //            }
        //            break;
        //        case "U16":
        //            {
        //                ushort value;
        //                ushort.TryParse(strVal, out value);
        //                writeByte = HexDataHelper.ShortToByte(value);
        //                DateByte.AddRange(writeByte);
        //            }
        //            break;
        //        case "I16":
        //            {
        //                short value;
        //                short.TryParse(strVal, out value);
        //                writeByte = HexDataHelper.ShortToByte(value);
        //                DateByte.AddRange(writeByte);
        //            }
        //            break;
        //        case "U32":
        //        case "I32":
        //            {
        //                long value;
        //                long.TryParse(strVal, out value);
        //                writeByte = HexDataHelper.IntToByte((int)value, true);
        //                DateByte.AddRange(writeByte);
        //            }
        //            break;
        //        default:
        //            //writeByte = HexDataHelper.HexStringToByte(dataInfo.Value);
        //            break;
        //    }//switch

        //    return DateByte;
        //}//func

        ///// <summary>
        ///// 字段信息转多包字节
        ///// </summary>
        ///// <param name="dataInfos"></param>
        ///// <returns></returns>
        //public static List<List<byte>> ToMulPackage2(ObservableCollection<CanFrameDataInfo> dataInfos)
        //{
        //    List<List<byte>> resultList = new List<List<byte>>();

        //    //字段转连续字节
        //    List<byte> continueBytes = AnalysisData(dataInfos);
        //    //byte crcHigh = continueBytes.Last();
        //    //continueBytes.Remove(crcHigh);
        //    //byte crcLow = continueBytes.Last();
        //    //continueBytes.Remove(crcLow);

        //    //起始帧
        //    {
        //        List<byte> stBytes = new List<byte>(continueBytes.Take(8));
        //        resultList.Add(stBytes);
        //    }

        //    continueBytes.RemoveRange(0, 8);
        //    int num = continueBytes.Count / 7;
        //    byte packageIndex = 1;

        //    //中间帧、结束帧
        //    {
        //        for (int i = 0; i < num; i++)
        //        {
        //            List<byte> midBytes = new List<byte>();
        //            midBytes.Add(packageIndex++);
        //            midBytes.AddRange(continueBytes.Skip(7 * i).Take(7));
        //            resultList.Add(midBytes);
        //        }
        //    }

        //    //结束帧
        //    //{
        //    //    List<byte> endBytes = new List<byte>();
        //    //    endBytes.Add(packageIndex);


        //    //    endBytes.Add(crcLow);
        //    //    endBytes.Add(crcHigh);
        //    //    resultList.Add(endBytes);
        //    //}

        //    return resultList;
        //}//func

        ////public static List<CanFrameData> ToMulPackage(ObservableCollection<CanFrameDataInfo> dataInfos)
        ////{
        ////    List<CanFrameData> resultList = new List<CanFrameData>();
        ////    List<FrameDataStruct> tmpDataList = new List<FrameDataStruct>();

        ////    foreach (CanFrameDataInfo dataInfo in dataInfos)
        ////    {
        ////        List<byte> bytes = AnalysisData(dataInfo);
        ////        FrameDataStruct dataStruct = new FrameDataStruct();
        ////        dataStruct.bytes = bytes;
        ////        dataStruct.dataInfo = dataInfo;
        ////        tmpDataList.Add(dataStruct);
        ////    }

        ////    int dataStructCount = tmpDataList.Count;

        ////    //起始帧
        ////    {
        ////        CanFrameData frameData = new CanFrameData();
        ////        resultList.Add(frameData);

        ////        List<byte> dataList = new List<byte>();
        ////        frameData.DataInfos.Add(tmpDataList[0].dataInfo); //包序号
        ////        frameData.DataInfos.Add(tmpDataList[1].dataInfo); //起始地址
        ////        frameData.DataInfos.Add(tmpDataList[2].dataInfo); //个数
        ////        dataList.AddRange(tmpDataList[0].bytes);
        ////        dataList.AddRange(tmpDataList[1].bytes);
        ////        dataList.AddRange(tmpDataList[2].bytes);

        ////        FrameDataStruct fstData = new FrameDataStruct();
        ////        FrameDataStruct secData = new FrameDataStruct();
        ////        bool hasAcross = false;
        ////        int index;
        ////        for (index = 3; index < dataStructCount; index++)
        ////        {
        ////            FrameDataStruct dataStruct = tmpDataList[index];
        ////            if (AddDataInfo(frameData.DataInfos, dataStruct, out fstData, out secData))
        ////            {
        ////                dataList.AddRange(dataStruct.bytes);

        ////                if (dataList.Count == 8)
        ////                    break;
        ////            }
        ////            else
        ////            {
        ////                //有字段信息跨包情况
        ////                dataList.AddRange(fstData.bytes);
        ////                hasAcross = true;
        ////                break;
        ////            }
        ////        }

        ////        //添加跨包字段的第二个子字段信息
        ////        if (hasAcross)
        ////        {
        ////            tmpDataList.RemoveRange(0, index); //移除起始帧数据
        ////            tmpDataList[0] = secData;
        ////        }
        ////        else
        ////        {
        ////            tmpDataList.RemoveRange(0, index + 1); //移除起始帧数据
        ////        }

        ////        frameData.Data = dataList.ToArray();
        ////    }

        ////    byte packageIndex = 1;

        ////    //中间帧、结束帧
        ////    {
        ////        ///遍历剩余的字段信息，按8个进行分包
        ////        ///有跨包的字段则拆分成两个子字段，第二个子字段划分到下一个包中
        ////        dataStructCount = tmpDataList.Count;
        ////        CanFrameData frameData = new CanFrameData();
        ////        List<byte> dataList = new List<byte>();
        ////        resultList.Add(frameData);
        ////        ///添加包序号
        ////        CanFrameDataInfo info1 = new CanFrameDataInfo();
        ////        info1.Name = "包序号";
        ////        info1.Type = "U8";
        ////        info1.ByteRange = 1;
        ////        info1.Value = $"{packageIndex}";
        ////        dataList.Add(packageIndex);
        ////        frameData.DataInfos.Add(info1);
        ////        packageIndex++;

        ////        for (int i = 0; i < dataStructCount; i++)
        ////        {
        ////            //添加当前字段信息
        ////            FrameDataStruct dataStruct = tmpDataList[i];
        ////            FrameDataStruct fstData = new FrameDataStruct();
        ////            FrameDataStruct secData = new FrameDataStruct();
        ////            if (AddDataInfo(frameData.DataInfos, dataStruct, out fstData, out secData))
        ////            {
        ////                //没有超字节
        ////                dataList.AddRange(dataStruct.bytes);
        ////                frameData.Data = dataList.ToArray();
        ////            }
        ////            else
        ////            {
        ////                //超出字节范围，当前frameData已满8个字节数据
        ////                ///添加当前frameData
        ////                dataList.AddRange(fstData.bytes);
        ////                frameData.Data = dataList.ToArray();
        ////                //resultList.Add(frameData);

        ////                ///初始化下一个frameData
        ////                frameData = new CanFrameData();
        ////                dataList = new List<byte>();
        ////                resultList.Add(frameData);
        ////                ///添加包序号
        ////                info1 = new CanFrameDataInfo();
        ////                info1.Name = "包序号";
        ////                info1.Type = "U8";
        ////                info1.ByteRange = 1;
        ////                info1.Value = $"{packageIndex}";
        ////                dataList.Add(packageIndex);
        ////                frameData.DataInfos.Add(info1);
        ////                ///添加当前字段信息的第二个子字段信息
        ////                dataList.AddRange(secData.bytes);
        ////                frameData.Data = dataList.ToArray();
        ////                frameData.DataInfos.Add(secData.dataInfo);

        ////                packageIndex++;
        ////            }
        ////        }//for
        ////    }

        ////    //结束帧-处理包序号和补满8个字节
        ////    {
        ////        if (resultList.Count >= 2)
        ////        {
        ////            //包序号
        ////            CanFrameData end = resultList.Last();
        ////            end.Data[0] = 0xff;
        ////            end.DataInfos[0].Value = "0xff";

        ////            //补满8字节
        ////            List<byte> tmpList = new List<byte>(end.Data);
        ////            byte[] arr = new byte[8];
        ////            for (int i = 0; i < tmpList.Count; i++)
        ////            {
        ////                arr[i] = tmpList[i];
        ////            }
        ////            end.Data = arr;
        ////        }
        ////    }

        ////    return resultList;
        ////}//func

        //private static bool AddDataInfo(ObservableCollection<CanFrameDataInfo> dataInfos, FrameDataStruct inData,
        //                                out FrameDataStruct fstData, out FrameDataStruct secData)
        //{
        //    int totalLen = 0;
        //    fstData = new FrameDataStruct();
        //    secData = new FrameDataStruct();
        //    int dataLen = inData.bytes.Count;

        //    foreach (CanFrameDataInfo dataInfo in dataInfos)
        //    {
        //        totalLen += dataInfo.ByteRange;
        //    }

        //    if ((totalLen + dataLen) <= 8)
        //    {
        //        dataInfos.Add(inData.dataInfo);
        //        return true;
        //    }
        //    else
        //    {
        //        //将字段拆分成两个子字段，第一个字段存到当前包，第二个字段传给下一个包
        //        int secLen = totalLen + dataLen - 8;
        //        int fstLen = dataLen - secLen;

        //        //第一个字段
        //        {
        //            CanFrameDataInfo dataInfo = new CanFrameDataInfo(inData.dataInfo);
        //            string byteNum = "";
        //            for (int i = 0; i < fstLen; i++)
        //            {
        //                byteNum += $"{i + 1},";
        //            }
        //            byteNum = byteNum.TrimEnd(',');
        //            dataInfo.Name = inData.dataInfo.Name + $"({byteNum}B)";
        //            dataInfo.ByteRange = fstLen;
        //            UpdateDataType(dataInfo, fstLen);

        //            byte[] bytes = inData.bytes.Take(fstLen).ToArray();
        //            dataInfo.Value = HexDataHelper.ByteArrToStrSmallend(bytes);
        //            dataInfos.Add(dataInfo);

        //            fstData.bytes = new List<byte>(bytes);
        //            fstData.dataInfo = dataInfo;
        //        }

        //        //第二个字段
        //        {
        //            CanFrameDataInfo dataInfo = new CanFrameDataInfo(inData.dataInfo);
        //            string byteNum = "";
        //            for (int i = 0; i < secLen; i++)
        //            {
        //                byteNum += $"{i + fstLen},";
        //            }
        //            byteNum = byteNum.TrimEnd(',');
        //            dataInfo.Name = inData.dataInfo.Name + $"({byteNum}B)";
        //            dataInfo.ByteRange = secLen;
        //            UpdateDataType(dataInfo, fstLen);

        //            byte[] bytes = inData.bytes.Skip(fstLen).ToArray();
        //            dataInfo.Value = HexDataHelper.ByteArrToStrSmallend(bytes);
        //            secData.bytes = new List<byte>(bytes);
        //            secData.dataInfo = dataInfo;
        //        }

        //        return false;
        //    }//else
        //}//func


        //private static byte GetPackageIndex(string packageIndex)
        //{
        //    packageIndex = packageIndex.Replace("0x", "").Replace("0X", "");
        //    if (packageIndex.Contains("ff"))
        //    {
        //        return 0xff;
        //    }
        //    else
        //    {
        //        byte v;
        //        if (byte.TryParse(packageIndex, out v))
        //        {
        //            return v;
        //        }

        //        return 0;
        //    }
        //}

        //private static void UpdateDataType(CanFrameDataInfo dataInfo, int newRange)
        //{
        //    string oldType = dataInfo.Type;
        //    string newType = "";
        //    if (oldType.Contains("I"))
        //    {
        //        if (newRange == 1)
        //        {
        //            newType = "I8";
        //        }
        //        else if (newRange == 2)
        //        {
        //            newType = "I16";
        //        }
        //    }
        //    else if (oldType.Contains("U"))
        //    {
        //        if (newRange == 1)
        //        {
        //            newType = "U8";
        //        }
        //        else if (newRange == 2)
        //        {
        //            newType = "U16";
        //        }
        //    }

        //    dataInfo.Type = newType;
        //}
    }//class

    //public struct FrameDataStruct
    //{
    //    public FrameDataStruct() { }

    //    public List<byte> bytes = null;
    //    public CanFrameDataInfo dataInfo = null;
    //}//struct

    /// <summary>
    /// 多包暂存数据
    /// 用于接收时多包数据暂存
    /// </summary>
    //public class MultyPackage
    //{
    //    public int packageNum = 0; //报数量
    //    public List<byte> datas = new List<byte>();
    //    public CanFrameModel frame = new CanFrameModel();
    //}
}
