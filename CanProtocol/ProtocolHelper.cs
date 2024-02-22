using CanProtocol.ProtocolModel;
using CanProtocol.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CanProtocol
{
    /// <summary>
    /// CAN协议帮助类
    /// </summary>
    public class ProtocolHelper
    {
        //接收数据时，允许的最大包数量，超出此数量则不在进行接收
        private static int _maxPackageNum = 100;
        //上一次匹配的点表地址（用于连续多包的地址匹配）
        private static string _lastTableAddr = "";
        //接收的多包数据缓存集合
        private static List<MultyPackage> s_packageBufferList = new List<MultyPackage>();


        #region 接收解析（字节数据转字段信息）
        public static CanFrameModel? FrameToModel2(uint id, byte[] data, byte devAddr = 0)
        {
            try
            {
                CanFrameData data1 = new CanFrameData();
                data1.AddInitDataTofile();

                //定义响应的CAN帧信息的集合
                List<CanFrameModel> frameModels = new List<CanFrameModel>()
                {
                    {
                        new CanFrameModel()
                        {
                            Id = 0x1B280081,Name ="BCU响应返回文件相关信息 ",FrameDatas = new List<CanFrameData>() { { data1 } }
                        }
                    },
                    {
                        new CanFrameModel()
                        {
                            Id = 0x1B2800A1,Name ="BMU响应返回文件相关信息",FrameDatas = new List<CanFrameData>(){ { data1 } }
                        }
                    }
                };

                if (data.Length == 8)
                {
                    CanFrameID frameId = new CanFrameID();

                    foreach (CanFrameModel frame in frameModels)
                    {
                        //1、匹配id
                        frameId.ID = frame.Id;
                        if (frameId.ContinuousFlag != 0)
                            continue;

                        if (devAddr != 0)
                        {
                            frameId.SrcAddr = devAddr;
                        }

                        if (frameId.ID != id)
                            continue;

                        CanFrameModel newFrame = new CanFrameModel(frame);
                        newFrame.FrameId.ID = frameId.ID;

                        //2、匹配相同点表起始地址/设备地址
                        int addr = 0x0;

                        CanFrameData? frameData = newFrame.FrameDatas.Find(t =>
                        {
                            //匹配点表起始地址
                            string strAddr = t.DataInfos[0].Value;
                            if (strAddr.Contains("0x") || strAddr.Contains("0X"))
                            {
                                strAddr = strAddr.Replace("0x", "").Replace("0X", "");
                                int tmpAddr = int.Parse(strAddr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                return tmpAddr == addr;
                            }
                            else
                            {
                                return int.Parse(strAddr) == addr;
                            }
                        });

                        //id相同，而点表地址不相同，仍表示不匹配
                        if (frameData == null)
                            continue;

                        //帧字节数据转字段信息
                        frameData.Data = data;
                        ByteDataToDataInfo(frameData);

                        return newFrame;
                    }//foreach
                }
            }
            catch (Exception ex)
            {
                string strError = System.DateTime.Now.ToString() + "接收解析【非连续帧错误】出现异常，错误信息" + ex.Message;
                MultiLanguages.Common.LogHelper.WriteLog(strError);

                //throw new Exception(strError);
            }

            return null;
        }//func

        public static CanFrameModel? MultiFrameToModel2(int obj, uint id, byte[] data, byte devAddr = 0, bool bcu = true)
        {
            try
            {
                //定义帧格式
                CanFrameData data1 = new CanFrameData();
                if (obj == 3)
                {
                    if (bcu)
                    {
                        data1.AddInitMultiDataToBCU();
                    }
                    else
                    {
                        data1.AddInitMultiDataToBMU();
                    }
                }
                else if (obj < 3)
                {
                    data1.AddInitMultiDataToFaultInfo();
                }
                else if (obj > 3)
                {
                    data1.AddInitMultiData();
                }

                //定义响应的CAN帧信息的集合
                List<CanFrameModel> frameModels = new List<CanFrameModel>()
            {
                {
                    new CanFrameModel()
                    {
                        Id = 0x19A90081,Name ="BCU响应方返回文件数据内容",FrameDatas = new List<CanFrameData>() { { data1 } }
                    }
                },
                {
                    new CanFrameModel()
                    {
                        Id = 0x19A900A1,Name ="BMU响应方返回文件数据内容",FrameDatas = new List<CanFrameData>(){ { data1 } }
                    }
                }
            };

                CanFrameModel? findFrame = null;
                int devIndex = data[0];
                int packageIndex = data[1];

                CanFrameID frameId = new CanFrameID();

                //1、查询当前帧
                foreach (CanFrameModel frame in frameModels)
                {
                    //匹配id
                    frameId.ID = frame.Id;
                    if (frameId.ContinuousFlag != 1)
                        continue;

                    if (devAddr != 0)
                    {
                        frameId.SrcAddr = devAddr;
                    }

                    if (frameId.ID != id)
                        continue;

                    if (packageIndex == 0)
                    {
                        //第一包 匹配点表起始地址
                        int addr = data[1];// | (data[2]) << 8;
                        int tmpAddr = frame.GetAddrInt();
                        if (tmpAddr == addr)
                        {
                            findFrame = frame;
                            if (devAddr != 0)
                            {
                                findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                            }
                            break;
                        }
                    }
                    else
                    {
                        //不是第一包 只匹配id
                        findFrame = frame;
                        if (devAddr != 0)
                        {
                            findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                        }
                        break;
                    }
                }//foreach

                if (findFrame != null && s_packageBufferList != null)
                {
                    //2、将当前帧加入到包缓存中
                    //为第一包或中间包，则加入到缓存中
                    //为最后一包或超出包数量限制，则此多包收集结束
                    MultyPackage? findMulPackage = s_packageBufferList.Find((o) =>
                    {
                        return (id == o.frame.Id);
                    });

                    if (findMulPackage == null)
                    {
                        //不在缓存中，是第一包->加入；否则丢弃
                        if (packageIndex == 0)
                        {
                            MultyPackage newMulPackage = new MultyPackage();
                            newMulPackage.packageNum++;
                            newMulPackage.datas.AddRange(data);//Add(data[7]);
                            newMulPackage.frame = findFrame;
                            s_packageBufferList.Add(newMulPackage);
                        }
                    }
                    else
                    {
                        //在缓存中，为最后一包或超出包数限制，则出包
                        if (packageIndex == 0)
                        {
                            //第一包丢弃
                        }
                        else if (packageIndex == 0xff)
                        {
                            //最后一包 出包
                            findMulPackage.datas.AddRange(data.Skip(2));
                            findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                            CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);

                            if (obj < 4)
                            {
                                ByteDataToDataInfo(resultFrame.FrameDatas[0]);
                            }

                            s_packageBufferList.Remove(findMulPackage);
                            return resultFrame;
                        }
                        else
                        {
                            if (findMulPackage.packageNum > _maxPackageNum)
                            {
                                //包超出范围 出包
                                findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                                CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);

                                ByteDataToDataInfo(resultFrame.FrameDatas[0]);

                                s_packageBufferList.Remove(findMulPackage);
                                return resultFrame;
                            }
                            else if (packageIndex != findMulPackage.packageNum)
                            {
                                return null; //包非连续，直接清除
                            }
                            else
                            {
                                findMulPackage.packageNum++;
                                findMulPackage.datas.AddRange(data.Skip(2)); //去除设备ID，包序号
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string strError = System.DateTime.Now.ToString() + "接收解析【连续帧错误】出现异常，错误信息" + ex.Message;
                MultiLanguages.Common.LogHelper.WriteLog(strError);

                //throw new Exception(strError);
            }

            return null;
        }//func

        public static CanFrameModel? MultiFrameToModelToPSCFile(int obj, uint id, byte[] data, byte devAddr = 0, bool bcu = true)//func
        {
            try
            {
                //定义帧格式
                CanFrameData data1 = new CanFrameData();
                data1.AddInitMultiDataToPCS();

                //定义响应的CAN帧信息的集合
                List<CanFrameModel> frameModels = new List<CanFrameModel>()
                {
                    {
                        new CanFrameModel()
                        {
                            //0x19A90081
                            Id = 0x0F294126,Name ="PCS响应方返回文件数据内容",FrameDatas = new List<CanFrameData>() { { data1 } }
                        }
                    },
                    {
                        new CanFrameModel()
                        {
                            //0x19A900A1
                            Id = 0x0DA94126,Name ="PCS响应方返回文件数据内容",FrameDatas = new List<CanFrameData>(){ { data1 } }
                        }
                    }
                };

                CanFrameModel? findFrame = null;
                //int devIndex = data[0];
                int packageIndex = data[0];

                CanFrameID frameId = new CanFrameID();

                //1、查询当前帧
                foreach (CanFrameModel frame in frameModels)
                {
                    //匹配id
                    frameId.ID = frame.Id;
                    if (frameId.ContinuousFlag != 1)
                        continue;

                    if (devAddr != 0)
                    {
                        frameId.SrcAddr = devAddr;
                    }

                    if (frameId.ID != id)
                        continue;

                    if (false)//packageIndex == 0
                    {
                        /*
                        //第一包 匹配点表起始地址
                        int addr = data[1]; //| (data[2]) << 8;
                        int tmpAddr = frame.GetAddrInt();
                        if (tmpAddr == addr)
                        {
                            findFrame = frame;
                            if (devAddr != 0)
                            {
                                findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                            }
                            break;
                        }*/
                    }
                    else
                    {
                        //不是第一包 只匹配id
                        findFrame = frame;
                        if (devAddr != 0)
                        {
                            findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                        }
                        break;
                    }
                }//foreach

                if (findFrame != null && s_packageBufferList != null)
                {
                    //2、将当前帧加入到包缓存中
                    //为第一包或中间包，则加入到缓存中
                    //为最后一包或超出包数量限制，则此多包收集结束
                    MultyPackage? findMulPackage = s_packageBufferList.Find((o) =>
                    {
                        return (id == o.frame.Id);
                    });

                    if (findMulPackage == null)
                    {
                        //不在缓存中，是第一包->加入；否则丢弃
                        if (packageIndex == 0)
                        {
                            MultyPackage newMulPackage = new MultyPackage();
                            newMulPackage.packageNum++;
                            newMulPackage.datas.AddRange(data);//Add(data[7]);
                            newMulPackage.frame = findFrame;
                            s_packageBufferList.Add(newMulPackage);
                        }
                    }
                    else
                    {
                        //在缓存中，为最后一包或超出包数限制，则出包
                        if (packageIndex == 0)
                        {
                            //第一包丢弃
                        }
                        else if (packageIndex == 0xff)
                        {
                            //最后一包 出包
                            findMulPackage.datas.AddRange(data.Skip(1));
                            findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                            CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);

                            if (obj < 4)
                            {
                                ByteDataToDataInfo(resultFrame.FrameDatas[0]);
                            }
                            else if (obj == 200)
                            {
                                ByteDataToDataInfoToPCSFile(resultFrame.FrameDatas[0]);
                            }

                            s_packageBufferList.Remove(findMulPackage);
                            return resultFrame;
                        }
                        else
                        {
                            if (findMulPackage.packageNum > _maxPackageNum)
                            {
                                //包超出范围 出包
                                findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                                CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);

                                ByteDataToDataInfo(resultFrame.FrameDatas[0]);

                                s_packageBufferList.Remove(findMulPackage);
                                return resultFrame;
                            }
                            else if (packageIndex != findMulPackage.packageNum)
                            {
                                return null; //包非连续，直接清除
                            }
                            else
                            {
                                findMulPackage.packageNum++;
                                findMulPackage.datas.AddRange(data.Skip(1)); //去除设备ID，包序号
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string strError = System.DateTime.Now.ToString() + "接收解析【连续帧错误】出现异常，错误信息" + ex.Message;
                MultiLanguages.Common.LogHelper.WriteLog(strError);

                //throw new Exception(strError);
            }

            return null;
        }

        /// <summary>
        /// 帧转FrameModel
        /// 用于单帧（非连续）
        /// </summary>
        /// <param name="frameModels">已配置的model集合</param>
        /// <param name="id">帧id</param>
        /// <param name="data">帧数据</param>
        /// <param name="devAddr">设备地址 [0]：不指定设备地址  [其他值]：指定设备地址</param>
        /// <returns>帧模型对象</returns>
        public static CanFrameModel? FrameToModel(List<CanFrameModel> frameModels, uint id, byte[] data, byte devAddr = 0)
        {
            try
            {
                if (data.Length == 8)
                {
                    CanFrameID frameId = new CanFrameID();

                    foreach (CanFrameModel frame in frameModels)
                    {
                        //1、匹配id
                        frameId.ID = frame.Id;
                        if (frameId.ContinuousFlag != 0)
                            continue;

                        if (devAddr != 0)
                        {
                            frameId.SrcAddr = devAddr;
                        }

                        if (frameId.ID != id)
                            continue;

                        CanFrameModel newFrame = new CanFrameModel(frame);
                        newFrame.FrameId.ID = frameId.ID;

                        //2、匹配相同点表起始地址
                        int addr = data[0];
                        addr |= (int)(data[1]) << 8;
                        CanFrameData? frameData = newFrame.FrameDatas.Find(t =>
                        {
                            //匹配点表起始地址
                            string strAddr = t.DataInfos[0].Value;
                            if (strAddr.Contains("0x") || strAddr.Contains("0X"))
                            {
                                strAddr = strAddr.Replace("0x", "").Replace("0X", "");
                                int tmpAddr = int.Parse(strAddr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                return tmpAddr == addr;
                            }
                            else
                            {
                                return int.Parse(strAddr) == addr;
                            }
                        });

                        //id相同，而点表地址不相同，仍表示不匹配
                        if (frameData == null)
                            continue;

                        //帧字节数据转字段信息
                        frameData.Data = data;
                        ByteDataToDataInfo(frameData);

                        return newFrame;
                    }//foreach
                }
            }
            catch (Exception ex)
            {
                string strError = System.DateTime.Now.ToString() + "接收解析非连续帧【转化FrameModel】出现异常，错误信息" + ex.Message;
                MultiLanguages.Common.LogHelper.WriteLog(strError);

                //throw new Exception(strError);
            }

            return null;
        }//func

        /// <summary>
        /// 帧转FrameModel
        /// 用于多帧（连续/多包）
        /// </summary>
        /// <param name="frameModels">已配置的model集合</param>
        /// <param name="id">帧id</param>
        /// <param name="data">帧数据</param>
        /// <param name="devAddr">设备地址 [0]：不指定设备地址  [其他值]：指定设备地址</param>
        /// <returns>帧模型对象</returns>
        public static CanFrameModel? MultiFrameToModel(List<CanFrameModel> frameModels, uint id, byte[] data, byte devAddr = 0)
        {
            try
            {
                CanFrameModel? findFrame = null;
                int packageIndex = data[0];

                CanFrameID frameId = new CanFrameID();

                //1、查询当前帧
                foreach (CanFrameModel frame in frameModels)
                {
                    //匹配id
                    frameId.ID = frame.Id;
                    if (frameId.ContinuousFlag != 1)
                        continue;

                    if (devAddr != 0)
                    {
                        frameId.SrcAddr = devAddr;
                    }

                    if (frameId.ID != id)
                        continue;

                    if (packageIndex == 0)
                    {
                        //第一包 匹配点表起始地址
                        int addr = data[1] | (data[2]) << 8;
                        int tmpAddr = frame.GetAddrInt();
                        if (tmpAddr == addr)
                        {
                            findFrame = frame;
                            if (devAddr != 0)
                            {
                                findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                            }
                            break;
                        }
                    }
                    else
                    {
                        //不是第一包 只匹配id
                        findFrame = frame;
                        if (devAddr != 0)
                        {
                            findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                        }
                        break;
                    }
                }//foreach

                if (findFrame != null && s_packageBufferList != null)
                {
                    //2、将当前帧加入到包缓存中
                    //为第一包或中间包，则加入到缓存中
                    //为最后一包或超出包数量限制，则此多包收集结束
                    MultyPackage? findMulPackage = s_packageBufferList.Find((o) =>
                    {
                        return (id == o.frame.Id);
                    });

                    if (findMulPackage == null)
                    {
                        //不在缓存中，不是第一包则丢弃
                        if (packageIndex == 0)
                        {
                            MultyPackage newMulPackage = new MultyPackage();
                            newMulPackage.packageNum++;
                            newMulPackage.datas.AddRange(data);
                            newMulPackage.frame = findFrame;
                            s_packageBufferList.Add(newMulPackage);
                        }
                    }
                    else
                    {
                        //在缓存中，为最后一包或超出包数限制，则出包
                        if (packageIndex == 0)
                        {
                            //第一包丢弃
                        }
                        else if (packageIndex == 0xff)
                        {
                            //最后一包 出包
                            findMulPackage.datas.AddRange(data.Skip(1));
                            findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                            CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);

                            ByteDataToDataInfo(resultFrame.FrameDatas[0]);

                            s_packageBufferList.Remove(findMulPackage);
                            return resultFrame;
                        }
                        else
                        {
                            if (findMulPackage.packageNum > _maxPackageNum)
                            {
                                //包超出范围 出包
                                findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                                CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);

                                ByteDataToDataInfo(resultFrame.FrameDatas[0]);

                                s_packageBufferList.Remove(findMulPackage);
                                return resultFrame;
                            }
                            else
                            {
                                findMulPackage.packageNum++;
                                findMulPackage.datas.AddRange(data.Skip(1)); //去除包序号
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string strError = System.DateTime.Now.ToString() + "接收解析连续帧【转化FrameModel】出现异常，错误信息" + ex.Message;
                MultiLanguages.Common.LogHelper.WriteLog(strError);

                //throw new Exception(strError);
            }

            return null;
        }//func

        /// <summary>
        /// 字节数据转字段信息
        /// </summary>
        /// <param name="frameData"></param>
        private static void ByteDataToDataInfo(CanFrameData frameData)
        {
            int index = 0;
            int count = frameData.DataInfos.Count;
            byte[] datas = frameData.Data;

            try
            {
                for (int i = 0; i < count; i++)
                {
                    CanFrameDataInfo dataInfo = frameData.DataInfos[i];
                    long realVal = 0;
                    float fRealVal = 0.0f;

                    string values = "";

                    //数据类型
                    if (dataInfo.Type == "U8" || dataInfo.Type == "char8")
                    {
                        byte byte1;
                        realVal = byte1 = datas[index++];

                        if (byte1 == 0xff)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "I8")
                    {
                        byte byte1;
                        realVal = byte1 = datas[index++];
                        if (realVal > 127)
                            realVal = (256 - realVal) * -1;

                        if (byte1 == 0x7f)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "U16")
                    {
                        byte[] bytes = new byte[2];

                        int byte1 = bytes[0] = datas[index++];
                        int byte2 = bytes[1] = datas[index++];

                        realVal = byte1 + (byte2 << 8);

                        if (realVal == 0xffff)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "I16")
                    {
                        string byte1 = datas[index++].ToString("X2");
                        string byte2 = datas[index++].ToString("X2");
                        realVal = Convert.ToInt16(byte2 + byte1, 16);

                        if (realVal == 0x7fff)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "U32")
                    {
                        byte[] buffer = new byte[4];
                        buffer[0] = datas[index++];
                        buffer[1] = datas[index++];
                        buffer[2] = datas[index++];
                        buffer[3] = datas[index++];

                        realVal = BitConverter.ToUInt32(buffer, 0);
                    }
                    else if (dataInfo.Type == "I32")
                    {
                        string byte1 = datas[index++].ToString("X2");
                        string byte2 = datas[index++].ToString("X2");
                        string byte3 = datas[index++].ToString("X2");
                        string byte4 = datas[index++].ToString("X2");

                        realVal = Convert.ToInt32(byte4 + byte3 + byte2 + byte1, 16);
                    }
                    else if (dataInfo.Type == "float32")
                    {
                        byte[] arr = new byte[4];
                        arr[0] = datas[index++];
                        arr[1] = datas[index++];
                        arr[2] = datas[index++];
                        arr[3] = datas[index++];
                        fRealVal = BitConverter.ToSingle(arr);
                    }
                    else if (dataInfo.Type == "string")
                    {
                        List<byte> list = new List<byte>();
                        //int len = dataInfo.Value.Length; len=3???
                        for (int j = 0; j < 2; j++)
                        {
                            list.Add(datas[index++]);
                        }
                        string value = Encoding.ASCII.GetString(list.ToArray());
                        dataInfo.Value = value;
                    }
                    else if (dataInfo.Type == "U24")
                    {
                        realVal = datas[index++] | (datas[index++] << 8) | (datas[index++] << 16); ;
                    }

                    if (!string.IsNullOrEmpty(values))
                    {
                        dataInfo.Value = values;//检查非法数据，默认显示为NA
                    }
                    else
                    {
                        //处理精度问题
                        string type = dataInfo.Type;
                        if (type == "float32")
                        {
                            if (dataInfo.Precision != null)
                            {
                                fRealVal = (float)(dataInfo.Precision ?? 1) * (fRealVal);
                            }

                            dataInfo.Value = fRealVal.ToString();
                        }
                        else if (type == "char8")
                        {
                            dataInfo.Value = realVal.ToString();
                        }
                        else if (type != "string")
                        {
                            if (dataInfo.Precision == 1 || dataInfo.Precision == null)
                            {
                                if (dataInfo.Value.Contains("0x") || dataInfo.Value.Contains("0X"))
                                {
                                    //十六进制值
                                    dataInfo.Value = "0x" + realVal.ToString("X");
                                }
                                else
                                {
                                    //十进制值
                                    dataInfo.Value = realVal.ToString();
                                }
                            }
                            else
                            {
                                //有精度统一按照
                                double dVlaue = (double)((dataInfo.Precision * (decimal)realVal));

                                dataInfo.Value = dVlaue.ToString();//十进制值
                            }
                        }
                    }
                }//for
            }
            catch (Exception ex)
            {
                //try-catch
                throw new Exception("配置长度与响应数值长度不匹配错误，导致数组越界解析" + ex.Message);
            }
        }//func
        private static void ByteDataToDataInfoToPCSFile(CanFrameData frameData)
        {
            int index = 0;
            int count = frameData.DataInfos.Count;
            byte[] datas = frameData.Data;

            try
            {
                int number = 0;

                for (int i = 0; i < count - 1; i++)
                {
                    if (i >= (number * 17) + 3)
                    {
                        break;
                    }
                    CanFrameDataInfo dataInfo = frameData.DataInfos[i];
                    long realVal = 0;
                    float fRealVal = 0.0f;

                    string values = "";

                    //数据类型
                    if (dataInfo.Type == "U8" || dataInfo.Type == "char8")
                    {
                        byte byte1;
                        realVal = byte1 = datas[index++];

                        if (byte1 == 0xff)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "I8")
                    {
                        byte byte1;
                        realVal = byte1 = datas[index++];
                        if (realVal > 127)
                            realVal = (256 - realVal) * -1;

                        if (byte1 == 0x7f)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "U16")
                    {
                        byte[] bytes = new byte[2];

                        int byte1 = bytes[0] = datas[index++];
                        int byte2 = bytes[1] = datas[index++];

                        realVal = byte1 + (byte2 << 8);

                        if (realVal == 0xffff)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "I16")
                    {
                        string byte1 = datas[index++].ToString("X2");
                        string byte2 = datas[index++].ToString("X2");
                        realVal = Convert.ToInt16(byte2 + byte1, 16);

                        if (realVal == 0x7fff)
                            values = "NA";
                    }
                    else if (dataInfo.Type == "U32")
                    {
                        byte[] buffer = new byte[4];
                        buffer[0] = datas[index++];
                        buffer[1] = datas[index++];
                        buffer[2] = datas[index++];
                        buffer[3] = datas[index++];

                        realVal = BitConverter.ToUInt32(buffer, 0);
                    }
                    else if (dataInfo.Type == "I32")
                    {
                        string byte1 = datas[index++].ToString("X2");
                        string byte2 = datas[index++].ToString("X2");
                        string byte3 = datas[index++].ToString("X2");
                        string byte4 = datas[index++].ToString("X2");

                        realVal = Convert.ToInt32(byte4 + byte3 + byte2 + byte1, 16);
                    }
                    else if (dataInfo.Type == "float32")
                    {
                        byte[] arr = new byte[4];
                        arr[0] = datas[index++];
                        arr[1] = datas[index++];
                        arr[2] = datas[index++];
                        arr[3] = datas[index++];
                        fRealVal = BitConverter.ToSingle(arr);
                    }
                    else if (dataInfo.Type == "string")
                    {
                        List<byte> list = new List<byte>();
                        //int len = dataInfo.Value.Length; len=3???
                        for (int j = 0; j < 2; j++)
                        {
                            list.Add(datas[index++]);
                        }
                        string value = Encoding.ASCII.GetString(list.ToArray());
                        dataInfo.Value = value;
                    }
                    else if (dataInfo.Type == "U24")
                    {
                        realVal = datas[index++] | (datas[index++] << 8) | (datas[index++] << 16); ;
                    }

                    if (!string.IsNullOrEmpty(values))
                    {
                        dataInfo.Value = values;//检查非法数据，默认显示为NA
                    }
                    else
                    {
                        //处理精度问题
                        string type = dataInfo.Type;
                        if (type == "float32")
                        {
                            if (dataInfo.Precision != null)
                            {
                                fRealVal = (float)(dataInfo.Precision ?? 1) * (fRealVal);
                            }

                            dataInfo.Value = fRealVal.ToString();
                        }
                        else if (type == "char8")
                        {
                            dataInfo.Value = realVal.ToString();
                        }
                        else if (type != "string")
                        {
                            if (dataInfo.Precision == 1 || dataInfo.Precision == null)
                            {
                                if (dataInfo.Value.Contains("0x") || dataInfo.Value.Contains("0X"))
                                {
                                    //十六进制值
                                    dataInfo.Value = "0x" + realVal.ToString("X");
                                }
                                else
                                {
                                    //十进制值
                                    dataInfo.Value = realVal.ToString();
                                }
                            }
                            else
                            {
                                //有精度统一按照
                                double dVlaue = (double)((dataInfo.Precision * (decimal)realVal));

                                dataInfo.Value = dVlaue.ToString();//十进制值
                            }
                        }
                    }

                    if (i == 2)
                    {
                        number = Convert.ToInt32(frameData.DataInfos[2].Value) / 26;
                    }

                }//for

            }
            catch (Exception ex)
            {
                //try-catch
                throw new Exception("配置长度与响应数值长度不匹配错误，导致数组越界解析" + ex.Message);
            }
        }//func
        #endregion


        #region 发送解析（字段信息转字节数据）
        public static bool AnalyseFrameModel(CanFrameModel frameModel)
        {
            return AnalyseFrameData(frameModel.FrameDatas);
        }

        public static bool AnalyseFrameData(List<CanFrameData> frameDatas)
        {
            if (frameDatas == null || frameDatas.Count == 0)
                return false;

            int count = frameDatas.Count;
            for (int i = 0; i < count; ++i)
            {
                CanFrameData frameData = frameDatas[i];
                List<byte> datas = AnalyseFrameData(frameData);

                //不足8个字节补0
                if (datas.Count < 8)
                {
                    int num = 8 - datas.Count;
                    datas.AddRange(new byte[num]);
                }
                //及时变更/更新数据
                frameData.Data = datas.Take(8).ToArray();
            }

            return true;
        }//func

        private static List<byte> AnalyseFrameData(CanFrameData frameData)
        {
            return AnalyseDataInfo(frameData.DataInfos);
        }

        public static List<byte> AnalyseDataInfo(IEnumerable<CanFrameDataInfo> dataInfos)
        {
            List<byte> datas = new List<byte>();
            foreach (CanFrameDataInfo dataInfo in dataInfos)
            {
                datas.AddRange(AnalyseDataInfo(dataInfo));
            }

            return datas;
        }

        /// <summary>
        /// 解析单个字段信息
        /// </summary>
        /// <param name="dataInfo"></param>
        /// <returns></returns>
        public static List<byte> AnalyseDataInfo(CanFrameDataInfo dataInfo)
        {
            List<byte> datas = new List<byte>();

            try
            {
                string strVal = dataInfo.Value;

                if (strVal == "") { return datas; }
                else
                {
                    byte[] byteArr;

                    //十六进制数转十进制数处理
                    if (strVal.Contains("0x") || strVal.Contains("0X"))
                    {
                        strVal = strVal.Replace("0x", "").Replace("0X", "");

                        if (dataInfo.Type.Contains("I") || dataInfo.Type.Contains("U"))
                        {
                            strVal = long.Parse(strVal, NumberStyles.HexNumber).ToString();
                        }
                        else if (dataInfo.Type.Contains("float"))
                        {
                            float value;
                            if (float.TryParse(strVal, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                            {
                                strVal = value.ToString();
                            }
                            else
                            {
                                strVal = "0";
                            }
                        }
                    }

                    //处理精度问题
                    if (dataInfo.Type != "char8" && dataInfo.Type != "BCD16" && dataInfo.Type != "string"
                        && dataInfo.Precision != null)
                    {
                        strVal = (decimal.Parse(strVal) / (dataInfo.Precision ?? 1)).ToString();
                    }

                    //数据类型
                    if (dataInfo.Type == "char8")
                    {
                        byteArr = Encoding.ASCII.GetBytes(strVal);

                        if (byteArr.Length > 0)
                        {
                            datas.Add(byteArr[0]);
                        }
                    }
                    else if (dataInfo.Type == "U8" || dataInfo.Type == "I8")
                    {
                        byte value = Convert.ToByte(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //byte.TryParse(strVal, out value);
                        datas.Add(value);
                    }
                    else if (dataInfo.Type == "U16")
                    {
                        //decimal.TryParse(strVal, out decimal val);

                        ushort value = Convert.ToUInt16(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //ushort.TryParse(strVal, out value);
                        byteArr = HexDataHelper.UShortToByte((ushort)value);
                        datas.AddRange(byteArr);
                    }
                    else if (dataInfo.Type == "I16")
                    {
                        short value = Convert.ToInt16(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //short.TryParse(strVal, out value);
                        byteArr = HexDataHelper.ShortToByte(value);
                        datas.AddRange(byteArr);
                    }
                    else if (dataInfo.Type == "BCD16")
                    {
                        int value = Convert.ToInt32(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //int.TryParse(strVal, out value);

                        byte[] data = new byte[2];
                        data[0] = (byte)(value & 0xff);
                        data[1] = (byte)(value >> 8);
                        datas.AddRange(data);
                    }
                    else if (dataInfo.Type == "U32")
                    {
                        long value = Convert.ToUInt32(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //long.TryParse(strVal, out value);
                        byteArr = HexDataHelper.IntToByte((int)value, true);
                        datas.AddRange(byteArr);
                    }
                    else if (dataInfo.Type == "I32")
                    {
                        long value = Convert.ToInt64(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //long.TryParse(strVal, out value);
                        byteArr = HexDataHelper.IntToByte((int)value, true);
                        datas.AddRange(byteArr);
                    }
                    else if (dataInfo.Type == "float32")
                    {
                        float value = 0.0f;
                        float.TryParse(strVal, out value);
                        byteArr = BitConverter.GetBytes(value);
                        datas.AddRange(byteArr);
                    }
                    else if (dataInfo.Type == "string")
                    {
                        byteArr = Encoding.ASCII.GetBytes(strVal);
                        datas.AddRange(byteArr);
                    }
                    else
                    {
                        //byteArr = HexDataHelper.HexStringToByte(dataInfo.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                string strError = System.DateTime.Now.ToString() + "发送解析【单个字段信息】出现异常，错误信息" + ex.Message;
                MultiLanguages.Common.LogHelper.WriteLog(strError);
            }

            return datas;
        }

        /// <summary>
        /// 解析多帧
        /// </summary>
        /// <param name="canFrameData"></param>
        /// <returns></returns>
        public static List<CanFrameData> AnalyseMultiPackage(CanFrameData canFrameData)
        {
            List<CanFrameData> result = new List<CanFrameData>();

            try
            {
                List<byte> byteList = new List<byte>();
                IEnumerable<CanFrameDataInfo> dataInfos = canFrameData.DataInfos;
                //收集所有字节
                foreach (CanFrameDataInfo dataInfo in dataInfos)
                {
                    List<byte> bytes = AnalyseDataInfo(dataInfo);
                    byteList.AddRange(bytes);
                }

                //计算CRC
                {
                    int validLen = byteList.Count - 6; //包序号1+起始地址2+个数1+CRC2
                    List<byte> validDatas = byteList.Skip(4).Take(validLen).ToList();
                    ushort crcVal = CRCHelper.ComputeCrc16(validDatas.ToArray(), validDatas.Count);
                    byteList[byteList.Count - 2] = (byte)(crcVal & 0xff);
                    byteList[byteList.Count - 1] = (byte)((crcVal >> 8) & 0xff);
                }

                //1、起始帧
                {
                    CanFrameData start = new CanFrameData();
                    start.Data = byteList.Take(8).ToArray();
                    result.Add(start);
                }

                //2、中间帧、结束帧
                {
                    byteList.RemoveRange(0, 8);
                    int len = byteList.Count / 7;
                    len += (byteList.Count % 7) > 0 ? 1 : 0;
                    for (int i = 0; i < len; i++)
                    {
                        byte[] datas = byteList.Skip(i * 7).Take(7).ToArray();
                        byte index = (byte)(i + 1);
                        if (i == (len - 1)) //最后帧
                        {
                            index = 0xff;
                        }

                        List<byte> tmp = new List<byte>();
                        tmp.Add(index);
                        tmp.AddRange(datas);

                        //补满8字节
                        byte[] arr = new byte[8];
                        for (int j = 0; j < tmp.Count; j++)
                        {
                            arr[j] = tmp[j];
                        }

                        CanFrameData fd = new CanFrameData();
                        fd.Data = arr;
                        result.Add(fd);
                    }
                }

            }
            catch (Exception ex)
            {
                string strError = System.DateTime.Now.ToString() + "发送解析【多帧/多个字段信息】出现异常，错误信息" + ex.Message;
                MultiLanguages.Common.LogHelper.WriteLog(strError);
            }
            return result;
        }

        #endregion

    }//class

    /// <summary>
    /// 帧数据结构
    /// </summary>
    public struct FrameDataStruct
    {
        public FrameDataStruct() { }

        public List<byte> bytes = null; //字段信息对应的数据
        public CanFrameDataInfo dataInfo = null; //字段信息
    }//struct

    /// <summary>
    /// 多包数据类
    /// 用于接收时暂存多包数据
    /// </summary>
    public class MultyPackage
    {
        public int packageNum = 0; //包数量
        public List<byte> datas = new List<byte>();
        public CanFrameModel frame = new CanFrameModel();
    }
}
