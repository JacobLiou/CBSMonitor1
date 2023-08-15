using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using CanProtocol.ProtocolModel;
using CanProtocol.Utilities;

namespace CanProtocol
{
    /// <summary>
    /// CANЭ�������
    /// </summary>
    public class ProtocolHelper
    {
        //��������ʱ������������������������������ڽ��н���
        private static int _maxPackageNum = 100;
        //��һ��ƥ��ĵ���ַ��������������ĵ�ַƥ�䣩
        private static string _lastTableAddr = "";
        //���յĶ�����ݻ��漯��
        private static List<MultyPackage> s_packageBufferList = new List<MultyPackage>();


        #region ���ս������ֽ�����ת�ֶ���Ϣ��
        public static CanFrameModel? FrameToModel2(uint id, byte[] data, byte devAddr = 0)
        {
            CanFrameData data1 = new CanFrameData();
            data1.AddInitDataTofile();

            List<CanFrameModel> frameModels = new List<CanFrameModel>()
            {
                {
                    new CanFrameModel(){
                        Id = 0x1B280081,
                        Name ="��Ӧ�������ļ������Ϣ",
                        FrameDatas = new List<CanFrameData>()
                        {
                            { data1 }
                        }
                    }
                }
            };

            if (data.Length != 8)
                return null;

            CanFrameID frameId = new CanFrameID();

            foreach (CanFrameModel frame in frameModels)
            {
                //1��ƥ��id
                {
                    frameId.ID = frame.Id;
                    if (frameId.ContinuousFlag != 0)
                        continue;

                    if (devAddr != 0)
                    {
                        frameId.SrcAddr = devAddr;
                    }

                    if (frameId.ID != id)
                        continue;
                }

                CanFrameModel newFrame = new CanFrameModel(frame);
                newFrame.FrameId.ID = frameId.ID;

                //2��ƥ����ͬ�����ʼ��ַ/�豸��ַ
                int addr = 0x0;
                //int addr = data[0];
                //addr |= (int)(data[1]) << 8;
                CanFrameData? frameData = newFrame.FrameDatas.Find(t =>
                {
                    //ƥ������ʼ��ַ
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


                if (frameData == null)
                {
                    //id��ͬ��������ַ����ͬ���Ա�ʾ��ƥ��
                    continue;
                }

                //֡�ֽ�����ת�ֶ���Ϣ
                try
                {
                    frameData.Data = data;
                    ByteDataToDataInfo(frameData);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"���ս���������֡���� id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
                }
                return newFrame;
            }//foreach

            return null;
        }

        public static CanFrameModel? MultiFrameToModel2(int obj, uint id, byte[] data, byte devAddr = 0, bool bcu = true)
        {
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

            List<CanFrameModel> frameModels = new List<CanFrameModel>()
            {
                {
                    new CanFrameModel(){
                        Id = 0x19A90081,
                        Name ="��Ӧ�������ļ���������",
                        FrameDatas = new List<CanFrameData>()
                        {
                            { data1 }
                        }
                    }
                }
            };

            CanFrameModel? findFrame = null;
            int devIndex = data[0];
            int packageIndex = data[1];

            CanFrameID frameId = new CanFrameID();

            //1����ѯ��ǰ֡
            foreach (CanFrameModel frame in frameModels)
            {
                //ƥ��id
                {
                    frameId.ID = frame.Id;
                    if (frameId.ContinuousFlag != 1)
                        continue;

                    if (devAddr != 0)
                    {
                        frameId.SrcAddr = devAddr;
                    }

                    if (frameId.ID != id)
                        continue;
                }

                if (packageIndex == 0)
                {
                    //��һ�� ƥ������ʼ��ַ
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
                    //���ǵ�һ�� ֻƥ��id
                    findFrame = frame;
                    if (devAddr != 0)
                    {
                        findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                    }
                    break;
                }
            }//foreach

            if (findFrame == null || s_packageBufferList == null)
                return null;


            //2������ǰ֡���뵽��������
            //Ϊ��һ�����м��������뵽������
            //Ϊ���һ���򳬳����������ƣ���˶���ռ�����
            MultyPackage? findMulPackage = s_packageBufferList.Find((o) =>
            {
                return (id == o.frame.Id);
            });

            if (findMulPackage == null)
            {
                //���ڻ�����
                if (packageIndex == 0)
                {
                    MultyPackage newMulPackage = new MultyPackage();
                    newMulPackage.packageNum++;
                    newMulPackage.datas.AddRange(data);//Add(data[7]);
                    newMulPackage.frame = findFrame;
                    s_packageBufferList.Add(newMulPackage);
                }
                else
                {
                    //���ǵ�һ������
                }
            }
            else
            {
                //�ڻ����У�Ϊ���һ���򳬳��������ƣ������
                if (packageIndex == 0)
                {
                    //��һ������
                }
                else if (packageIndex == 0xff)
                {
                    //���һ�� ����
                    findMulPackage.datas.AddRange(data.Skip(2));
                    findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                    CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);
                    try
                    {
                        if (obj < 4)
                        {
                            ByteDataToDataInfo(resultFrame.FrameDatas[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"���ս�������֡���� id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
                    }

                    s_packageBufferList.Remove(findMulPackage);
                    return resultFrame;
                }
                else
                {
                    if (findMulPackage.packageNum > _maxPackageNum)
                    {
                        //��������Χ ����
                        findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                        CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);
                        try
                        {
                            ByteDataToDataInfo(resultFrame.FrameDatas[0]);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"���ս�������֡���� id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
                        }

                        s_packageBufferList.Remove(findMulPackage);
                        return resultFrame;
                    }
                    else
                    {
                        findMulPackage.packageNum++;
                        findMulPackage.datas.AddRange(data.Skip(2)); //ȥ���豸ID�������
                    }
                }
            }

            return null;
        }//func

        /// <summary>
        /// ֡תFrameModel
        /// ���ڵ�֡����������
        /// </summary>
        /// <param name="frameModels">�����õ�model����</param>
        /// <param name="id">֡id</param>
        /// <param name="data">֡����</param>
        /// <param name="devAddr">�豸��ַ [0]����ָ���豸��ַ  [����ֵ]��ָ���豸��ַ</param>
        /// <returns>֡ģ�Ͷ���</returns>
        public static CanFrameModel? FrameToModel(List<CanFrameModel> frameModels, uint id, byte[] data, byte devAddr = 0)
        {
            if (data.Length != 8)
                return null;

            CanFrameID frameId = new CanFrameID();

            foreach (CanFrameModel frame in frameModels)
            {
                //1��ƥ��id
                {
                    frameId.ID = frame.Id;
                    if (frameId.ContinuousFlag != 0)
                        continue;

                    if (devAddr != 0)
                    {
                        frameId.SrcAddr = devAddr;
                    }

                    if (frameId.ID != id)
                        continue;
                }

                CanFrameModel newFrame = new CanFrameModel(frame);
                newFrame.FrameId.ID = frameId.ID;

                //2��ƥ����ͬ�����ʼ��ַ
                int addr = data[0];
                addr |= (int)(data[1]) << 8;
                CanFrameData? frameData = newFrame.FrameDatas.Find(t =>
                {
                    //ƥ������ʼ��ַ
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

                if (frameData == null)
                {
                    //id��ͬ��������ַ����ͬ���Ա�ʾ��ƥ��
                    continue;
                }

                //֡�ֽ�����ת�ֶ���Ϣ
                try
                {
                    frameData.Data = data;
                    ByteDataToDataInfo(frameData);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"���ս���������֡���� id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
                }

                return newFrame;
            }//foreach

            return null;
        }//func

        /// <summary>
        /// ֡תFrameModel
        /// ���ڶ�֡������/�����
        /// </summary>
        /// <param name="frameModels">�����õ�model����</param>
        /// <param name="id">֡id</param>
        /// <param name="data">֡����</param>
        /// <param name="devAddr">�豸��ַ [0]����ָ���豸��ַ  [����ֵ]��ָ���豸��ַ</param>
        /// <returns>֡ģ�Ͷ���</returns>
        public static CanFrameModel? MultiFrameToModel(List<CanFrameModel> frameModels, uint id, byte[] data, byte devAddr = 0)
        {
            CanFrameModel? findFrame = null;
            int packageIndex = data[0];

            CanFrameID frameId = new CanFrameID();

            //1����ѯ��ǰ֡
            foreach (CanFrameModel frame in frameModels)
            {
                //ƥ��id
                {
                    frameId.ID = frame.Id;
                    if (frameId.ContinuousFlag != 1)
                        continue;

                    if (devAddr != 0)
                    {
                        frameId.SrcAddr = devAddr;
                    }

                    if (frameId.ID != id)
                        continue;
                }

                if (packageIndex == 0)
                {
                    //��һ�� ƥ������ʼ��ַ
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
                    //���ǵ�һ�� ֻƥ��id
                    findFrame = frame;
                    if (devAddr != 0)
                    {
                        findFrame.FrameId.SrcAddr = frameId.SrcAddr;
                    }
                    break;
                }
            }//foreach

            if (findFrame == null || s_packageBufferList == null)
                return null;


            //2������ǰ֡���뵽��������
            //Ϊ��һ�����м��������뵽������
            //Ϊ���һ���򳬳����������ƣ���˶���ռ�����
            MultyPackage? findMulPackage = s_packageBufferList.Find((o) =>
            {
                return (id == o.frame.Id);
            });

            if (findMulPackage == null)
            {
                //���ڻ�����
                if (packageIndex == 0)
                {
                    MultyPackage newMulPackage = new MultyPackage();
                    newMulPackage.packageNum++;
                    newMulPackage.datas.AddRange(data);
                    newMulPackage.frame = findFrame;
                    s_packageBufferList.Add(newMulPackage);
                }
                else
                {
                    //���ǵ�һ������
                }
            }
            else
            {
                //�ڻ����У�Ϊ���һ���򳬳��������ƣ������
                if (packageIndex == 0)
                {
                    //��һ������
                }
                else if (packageIndex == 0xff)
                {
                    //���һ�� ����
                    findMulPackage.datas.AddRange(data.Skip(1));
                    findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                    CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);
                    try
                    {
                        ByteDataToDataInfo(resultFrame.FrameDatas[0]);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"���ս�������֡���� id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
                    }

                    s_packageBufferList.Remove(findMulPackage);
                    return resultFrame;
                }
                else
                {
                    if (findMulPackage.packageNum > _maxPackageNum)
                    {
                        //��������Χ ����
                        findMulPackage.frame.FrameDatas[0].Data = findMulPackage.datas.ToArray();
                        CanFrameModel resultFrame = new CanFrameModel(findMulPackage.frame);
                        try
                        {
                            ByteDataToDataInfo(resultFrame.FrameDatas[0]);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"���ս�������֡���� id: 0x{id.ToString("X8")}, error: {ex.ToString()}");
                        }

                        s_packageBufferList.Remove(findMulPackage);
                        return resultFrame;
                    }
                    else
                    {
                        findMulPackage.packageNum++;
                        findMulPackage.datas.AddRange(data.Skip(1)); //ȥ�������
                    }
                }
            }

            return null;
        }//func

        /// <summary>
        /// �ֽ�����ת�ֶ���Ϣ
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

                    //��������
                    if (dataInfo.Type == "U8" || dataInfo.Type == "char8")
                    {
                        realVal = datas[index++];
                    }
                    else if (dataInfo.Type == "I8")
                    {
                        realVal = datas[index++];
                        if (realVal > 127)
                            realVal = (256 - realVal) * -1;
                    }
                    else if (dataInfo.Type == "U16")
                    {
                        int byte1 = datas[index++];
                        int byte2 = datas[index++] << 8;
                        realVal = byte1 + byte2;
                    }
                    else if (dataInfo.Type == "I16")
                    {
                        string byte1 = datas[index++].ToString("X2");
                        string byte2 = datas[index++].ToString("X2");
                        realVal = Convert.ToInt16(byte2 + byte1, 16);
                    }
                    else if (dataInfo.Type == "U32")
                    {
                        /*long byte1 = datas[index++];
                        long byte2 = datas[index++] << 8;
                        long byte3 = datas[index++] << 16;
                        long byte4 = datas[index++] << 24;
                        realVal = byte1 + byte2 + byte3 + byte4;*/

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

                    //����������
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
                                //ʮ������ֵ
                                dataInfo.Value = "0x" + realVal.ToString("X");
                            }
                            else
                            {
                                //ʮ����ֵ
                                dataInfo.Value = realVal.ToString();
                            }
                        }
                        else
                        {
                            //�о���ͳһ����
                            double dVlaue = (double)((dataInfo.Precision * (decimal)realVal));

                            dataInfo.Value = dVlaue.ToString();//ʮ����ֵ

                            /*if (dataInfo.Value.Contains("0x") || dataInfo.Value.Contains("0X"))
                            {
                                //ʮ������ֵ
                                dataInfo.Value = "0x" + dVlaue.ToString("X");
                            }
                            else
                            {
                                //ʮ����ֵ
                                dataInfo.Value = dVlaue.ToString();
                            }*/
                        }
                    }
                }//for
            }
            catch (Exception)
            {
                //try-catch���ó�������Ӧ��ֵ���Ȳ�ƥ����󣬵�������Խ�����
            }
        }//func
        #endregion


        #region ���ͽ������ֶ���Ϣת�ֽ����ݣ�
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

                //����8���ֽڲ�0
                {
                    if (datas.Count < 8)
                    {
                        int num = 8 - datas.Count;
                        datas.AddRange(new byte[num]);
                    }
                }

                //�������Ƚ�ȡ
                {
                    //if (DateByte.Count > 8)
                    //{
                    //    DateByte = DateByte.Take(8).ToList();
                    //}
                }
                frameData.Data = datas.ToArray();
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
        /// ���������ֶ���Ϣ
        /// </summary>
        /// <param name="dataInfo"></param>
        /// <returns></returns>
        public static List<byte> AnalyseDataInfo(CanFrameDataInfo dataInfo)
        {
            List<byte> datas = new List<byte>();
            byte[] byteArr;
            string strVal = dataInfo.Value;
            if (strVal == "")
                return datas;

            //if (dataInfo.IsValidData())
            {
                //ʮ��������תʮ����������
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

                //����������
                if (dataInfo.Type != "char8" && dataInfo.Type != "BCD16" && dataInfo.Type != "string")
                {
                    if (dataInfo.Precision != null)
                    {
                        strVal = (decimal.Parse(strVal) / (dataInfo.Precision ?? 1)).ToString();

                    }
                }
            }

            //��������
            switch (dataInfo.Type)
            {
                case "char8":
                    byteArr = Encoding.ASCII.GetBytes(strVal);
                    if (byteArr.Length > 0) datas.Add(byteArr[0]);
                    break;
                case "U8":
                case "I8":
                    {
                        byte value = Convert.ToByte(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //byte.TryParse(strVal, out value);
                        datas.Add(value);
                    }
                    break;
                case "BCD16":
                    {
                        int value = Convert.ToInt32(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //int.TryParse(strVal, out value);
                        byte[] data = new byte[2];
                        data[0] = (byte)(value & 0xff);
                        data[1] = (byte)(value >> 8);
                        datas.AddRange(data);
                    }
                    break;
                case "U16":
                    {
                        ushort value = Convert.ToUInt16(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //ushort.TryParse(strVal, out value);
                        byteArr = HexDataHelper.UShortToByte((ushort)value);
                        datas.AddRange(byteArr);
                    }
                    break;
                case "I16":
                    {
                        short value = Convert.ToInt16(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //short.TryParse(strVal, out value);
                        byteArr = HexDataHelper.ShortToByte(value);
                        datas.AddRange(byteArr);
                    }
                    break;
                case "U32":
                    {
                        long value = Convert.ToUInt32(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //long.TryParse(strVal, out value);
                        byteArr = HexDataHelper.IntToByte((int)value, true);
                        datas.AddRange(byteArr);
                    }
                    break;
                case "I32":
                    {
                        long value = Convert.ToInt64(Math.Round(Convert.ToDecimal(strVal), MidpointRounding.ToZero));
                        //long.TryParse(strVal, out value);
                        byteArr = HexDataHelper.IntToByte((int)value, true);
                        datas.AddRange(byteArr);
                    }
                    break;
                case "float32":
                    {
                        float value = 0.0f;
                        float.TryParse(strVal, out value);
                        byteArr = BitConverter.GetBytes(value);
                        datas.AddRange(byteArr);
                    }
                    break;
                case "string":
                    {
                        //�ַ���ת�ֽ�
                        byteArr = Encoding.ASCII.GetBytes(strVal);
                        datas.AddRange(byteArr);
                    }
                    break;
                default:
                    //byteArr = HexDataHelper.HexStringToByte(dataInfo.Value);
                    break;
            }//switch

            return datas;
        }//func

        /// <summary>
        /// ������֡
        /// </summary>
        /// <param name="canFrameData"></param>
        /// <returns></returns>
        public static List<CanFrameData> AnalyseMultiPackage(CanFrameData canFrameData)
        {
            List<CanFrameData> result = new List<CanFrameData>();
            List<byte> byteList = new List<byte>();
            IEnumerable<CanFrameDataInfo> dataInfos = canFrameData.DataInfos;
            //�ռ������ֽ�
            foreach (CanFrameDataInfo dataInfo in dataInfos)
            {
                List<byte> bytes = AnalyseDataInfo(dataInfo);
                byteList.AddRange(bytes);
            }

            //����CRC
            {
                int validLen = byteList.Count - 6; //�����1+��ʼ��ַ2+����1+CRC2
                List<byte> validDatas = byteList.Skip(4).Take(validLen).ToList();
                //CRCHelper helper = new CRCHelper();
                ushort crcVal = CRCHelper.ComputeCrc16(validDatas.ToArray(), validDatas.Count);
                byteList[byteList.Count - 2] = (byte)(crcVal & 0xff);
                byteList[byteList.Count - 1] = (byte)((crcVal >> 8) & 0xff);
            }

            //1����ʼ֡
            {
                CanFrameData start = new CanFrameData();
                start.Data = byteList.Take(8).ToArray();
                result.Add(start);
            }

            //2���м�֡������֡
            {
                byteList.RemoveRange(0, 8);
                int len = byteList.Count / 7;
                len += (byteList.Count % 7) > 0 ? 1 : 0;
                for (int i = 0; i < len; i++)
                {
                    byte[] datas = byteList.Skip(i * 7).Take(7).ToArray();
                    byte index = (byte)(i + 1);
                    if (i == (len - 1)) //���֡
                    {
                        index = 0xff;
                    }

                    List<byte> tmp = new List<byte>();
                    tmp.Add(index);
                    tmp.AddRange(datas);

                    //����8�ֽ�
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

            return result;
        }

        /// <summary>
        /// ������֡
        /// </summary
        /// <param name="canFrameData">֡����-�������ֶ���Ϣʹ�ö�֡</param>
        /// <returns></returns>
        [Obsolete("�ɷ�������")]
        public static List<CanFrameData> AnalyseMultiPackage_old(CanFrameData canFrameData)
        {
            List<CanFrameData> resultList = new List<CanFrameData>();
            List<FrameDataStruct> tmpDataList = new List<FrameDataStruct>();

            IEnumerable<CanFrameDataInfo> dataInfos = canFrameData.DataInfos;
            foreach (CanFrameDataInfo dataInfo in dataInfos)
            {
                List<byte> bytes = AnalyseDataInfo(dataInfo);
                FrameDataStruct dataStruct = new FrameDataStruct();
                dataStruct.bytes = bytes;
                dataStruct.dataInfo = dataInfo;
                tmpDataList.Add(dataStruct);
            }

            int dataStructCount = tmpDataList.Count;

            //1����ʼ֡
            {
                CanFrameData frameData = new CanFrameData();
                resultList.Add(frameData);

                List<byte> dataList = new List<byte>();
                frameData.DataInfos.Add(tmpDataList[0].dataInfo); //�����
                frameData.DataInfos.Add(tmpDataList[1].dataInfo); //��ʼ��ַ
                frameData.DataInfos.Add(tmpDataList[2].dataInfo); //����
                dataList.AddRange(tmpDataList[0].bytes);
                dataList.AddRange(tmpDataList[1].bytes);
                dataList.AddRange(tmpDataList[2].bytes);

                FrameDataStruct fstData = new FrameDataStruct();
                FrameDataStruct secData = new FrameDataStruct();
                bool hasAcross = false;
                int index;
                for (index = 3; index < dataStructCount; index++)
                {
                    FrameDataStruct dataStruct = tmpDataList[index];
                    if (AddDataInfo(frameData.DataInfos, dataStruct, out fstData, out secData))
                    {
                        dataList.AddRange(dataStruct.bytes);

                        if (dataList.Count == 8)
                            break;
                    }
                    else
                    {
                        //���ֶ���Ϣ������
                        dataList.AddRange(fstData.bytes);
                        hasAcross = true;
                        break;
                    }
                }

                //��ӿ���ֶεĵڶ������ֶ���Ϣ
                if (hasAcross)
                {
                    tmpDataList.RemoveRange(0, index); //�Ƴ���ʼ֡����
                    tmpDataList[0] = secData;
                }
                else
                {
                    tmpDataList.RemoveRange(0, index + 1); //�Ƴ���ʼ֡����
                }

                frameData.Data = dataList.ToArray();
            }

            byte packageIndex = 1;

            //2���м�֡������֡
            {
                ///����ʣ����ֶ���Ϣ����8�����зְ�
                ///�п�����ֶ����ֳ��������ֶΣ��ڶ������ֶλ��ֵ���һ������
                dataStructCount = tmpDataList.Count;
                CanFrameData frameData = new CanFrameData();
                List<byte> dataList = new List<byte>();
                resultList.Add(frameData);
                ///��Ӱ����
                CanFrameDataInfo info1 = new CanFrameDataInfo();
                info1.Name = "�����";
                info1.Type = "U8";
                info1.ByteRange = 1;
                info1.Value = $"{packageIndex}";
                dataList.Add(packageIndex);
                frameData.DataInfos.Add(info1);
                packageIndex++;

                for (int i = 0; i < dataStructCount; i++)
                {
                    //��ӵ�ǰ�ֶ���Ϣ
                    FrameDataStruct dataStruct = tmpDataList[i];
                    FrameDataStruct fstData = new FrameDataStruct();
                    FrameDataStruct secData = new FrameDataStruct();
                    if (AddDataInfo(frameData.DataInfos, dataStruct, out fstData, out secData))
                    {
                        //û�г��ֽ�
                        dataList.AddRange(dataStruct.bytes);
                        frameData.Data = dataList.ToArray();
                    }
                    else
                    {
                        //�����ֽڷ�Χ����ǰframeData����8���ֽ�����
                        ///��ӵ�ǰframeData
                        dataList.AddRange(fstData.bytes);
                        frameData.Data = dataList.ToArray();
                        //resultList.Add(frameData);

                        ///��ʼ����һ��frameData
                        frameData = new CanFrameData();
                        dataList = new List<byte>();
                        resultList.Add(frameData);
                        ///��Ӱ����
                        info1 = new CanFrameDataInfo();
                        info1.Name = "�����";
                        info1.Type = "U8";
                        info1.ByteRange = 1;
                        info1.Value = $"{packageIndex}";
                        dataList.Add(packageIndex);
                        frameData.DataInfos.Add(info1);
                        ///��ӵ�ǰ�ֶ���Ϣ�ĵڶ������ֶ���Ϣ
                        dataList.AddRange(secData.bytes);
                        frameData.Data = dataList.ToArray();
                        frameData.DataInfos.Add(secData.dataInfo);

                        packageIndex++;
                    }
                }//for
            }

            //3������֡-�������źͲ���8���ֽ�
            {
                if (resultList.Count >= 2)
                {
                    //�����
                    CanFrameData end = resultList.Last();
                    end.Data[0] = 0xff;
                    end.DataInfos[0].Value = "0xff";

                    //����8�ֽ�
                    List<byte> tmpList = new List<byte>(end.Data);
                    byte[] arr = new byte[8];
                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        arr[i] = tmpList[i];
                    }
                    end.Data = arr;
                }
            }

            return resultList;
        }//func

        private static bool AddDataInfo(ObservableCollection<CanFrameDataInfo> dataInfos, FrameDataStruct inData,
                                        out FrameDataStruct fstData, out FrameDataStruct secData)
        {
            int totalLen = 0;
            fstData = new FrameDataStruct();
            secData = new FrameDataStruct();
            int dataLen = inData.bytes.Count;

            foreach (CanFrameDataInfo dataInfo in dataInfos)
            {
                totalLen += dataInfo.ByteRange;
            }

            if ((totalLen + dataLen) <= 8)
            {
                dataInfos.Add(inData.dataInfo);
                return true;
            }
            else
            {
                //���ֶβ�ֳ��������ֶΣ���һ���ֶδ浽��ǰ�����ڶ����ֶδ�����һ����
                int secLen = totalLen + dataLen - 8;
                int fstLen = dataLen - secLen;

                //��һ���ֶ�
                {
                    CanFrameDataInfo dataInfo = new CanFrameDataInfo(inData.dataInfo);
                    string byteNum = "";
                    for (int i = 0; i < fstLen; i++)
                    {
                        byteNum += $"{i + 1},";
                    }
                    byteNum = byteNum.TrimEnd(',');
                    dataInfo.Name = inData.dataInfo.Name + $"({byteNum}B)";
                    dataInfo.ByteRange = fstLen;
                    UpdateDataType(dataInfo, fstLen);

                    byte[] bytes = inData.bytes.Take(fstLen).ToArray();
                    dataInfo.Value = HexDataHelper.ByteArrToStrSmallend(bytes);
                    dataInfos.Add(dataInfo);

                    fstData.bytes = new List<byte>(bytes);
                    fstData.dataInfo = dataInfo;
                }

                //�ڶ����ֶ�
                {
                    CanFrameDataInfo dataInfo = new CanFrameDataInfo(inData.dataInfo);
                    string byteNum = "";
                    for (int i = 0; i < secLen; i++)
                    {
                        byteNum += $"{i + fstLen},";
                    }
                    byteNum = byteNum.TrimEnd(',');
                    dataInfo.Name = inData.dataInfo.Name + $"({byteNum}B)";
                    dataInfo.ByteRange = secLen;
                    UpdateDataType(dataInfo, fstLen);

                    byte[] bytes = inData.bytes.Skip(fstLen).ToArray();
                    dataInfo.Value = HexDataHelper.ByteArrToStrSmallend(bytes);
                    secData.bytes = new List<byte>(bytes);
                    secData.dataInfo = dataInfo;
                }

                return false;
            }//else
        }//func

        private static void UpdateDataType(CanFrameDataInfo dataInfo, int newRange)
        {
            string oldType = dataInfo.Type;
            string newType = "";
            if (oldType.Contains("I"))
            {
                if (newRange == 1)
                {
                    newType = "I8";
                }
                else if (newRange == 2)
                {
                    newType = "I16";
                }
            }
            else if (oldType.Contains("U"))
            {
                if (newRange == 1)
                {
                    newType = "U8";
                }
                else if (newRange == 2)
                {
                    newType = "U16";
                }
            }

            dataInfo.Type = newType;
        }
        #endregion

    }//class

    /// <summary>
    /// ֡���ݽṹ
    /// </summary>
    public struct FrameDataStruct
    {
        public FrameDataStruct() { }

        public List<byte> bytes = null; //�ֶ���Ϣ��Ӧ������
        public CanFrameDataInfo dataInfo = null; //�ֶ���Ϣ
    }//struct

    /// <summary>
    /// ���������
    /// ���ڽ���ʱ�ݴ�������
    /// </summary>
    public class MultyPackage
    {
        public int packageNum = 0; //������
        public List<byte> datas = new List<byte>();
        public CanFrameModel frame = new CanFrameModel();
    }


    /// <summary>
    /// ����ݴ�����
    /// ���ڽ���ʱ��������ݴ�
    /// </summary>
    public class MultyPackage2
    {
        public int packageNum = 0; //������
        public uint id = 0x0;
        public List<byte> datas = new List<byte>();
        public CanFrameModel frame = new CanFrameModel();
    }
}
