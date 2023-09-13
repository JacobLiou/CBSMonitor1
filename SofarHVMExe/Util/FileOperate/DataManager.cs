using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CanProtocol.ProtocolModel;
using NPOI.POIFS.NIO;
using NPOI.SS.Formula.Functions;
using SofarHVMExe.DbModels;
using SofarHVMExe.Model;
using SofarHVMExe.Util;

namespace SofarHVMExe.Utilities
{
    public class DataManager
    {
        /// <summary>
        /// 写入数据库数据
        /// </summary>
        /// <param name="ret"></param>
        public static bool WriteData(FileConfigModel? ret)
        {
            bool result = true;
            try
            {
                // 插入 McuConfigModel
                SetFileConfigMcuModels(ret.McuModels);

                SetFileConfigMonModels(ret.MonModels);

                SetFileConfigPrjModel(ret.PrjModel);

                SetFileConfigFrameModel(ret.FrameModel);

                SetFileConfigEventModels(ret.EventModels);

                SetFileConfigCmdModels(ret.CmdModels);

                SetFileConfigMemModels(ret.MemModels);

                SetFileConfigOscilloscope(ret.OscilloscopeModel);
            }
            catch (Exception ex)
            {
                throw;
                return false;
            }
            finally
            {
            }

            return result;
        }

        /// <summary>
        /// 写入数据库数据
        /// </summary>
        /// <param name="ret"></param>
        public static void WriteData111(FileConfigModel? ret)
        {
            return;
            try
            {
                foreach (var item in ret.CmdModels)
                {
                    Dictionary<string, string> keys = ProperyConvert(item);
                    SqliteHelper.ExecuteInsert("CmdGrpConfig", keys);

                    int sort = 100;
                    foreach (var frame in item.cmdConfigModels)
                    {
                        keys = ProperyConvert(frame);
                        keys.Add("CmdID", item.Index.ToString());
                        keys["Sort"] = sort.ToString();
                        keys["Guid"] = System.Guid.NewGuid().ToString();
                        keys.Remove(nameof(frame.FrameModel));
                        SqliteHelper.ExecuteInsert("CmdConfig", keys);
                        sort += 100;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// 读取数据库数据
        /// </summary>
        /// <param name="ret"></param>
        public static FileConfigModel ReadData()
        {
            FileConfigModel model = new FileConfigModel();
            try
            {

                GetFileConfigMcuModels(model.McuModels);

                GetFileConfigMonModels(model.MonModels);

                GetFileConfigPrjModel(model.PrjModel);

                GetFileConfigFrameModel(model.FrameModel);

                GetFileConfigEventModels(model.EventModels);

                GetFileConfigCmdModels(model.FrameModel.CanFrameModels, model.CmdModels);

                GetFileConfigMemModels(model.MemModels);

                GetFileConfigOscilloscope(model.OscilloscopeModel);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                //var cfgFilePath = @"D:\Users\Git\AutoTest\A5_CBS电池上位机\SofarHVMExe\bin\Debug\net6.0-windows\Config\Config10086.json";
                //if (JsonConfigHelper.WirteConfigFile(model, cfgFilePath))
                //{
                //}
            }

            return model;
        }

        #region 实体转数据库

        private static void SetFileConfigMcuModels(List<McuConfigModel> mcuConfigs)
        {
            foreach (var item in mcuConfigs)
            {
                Dictionary<string, string> keys = ProperyConvert(item);
                SqliteHelper.ExecuteInsert("McuConfig", keys);
            }
        }

        private static void SetFileConfigMonModels(List<MonitorConfigModel> monitors)
        {
            foreach (var item in monitors)
            {
                Dictionary<string, string> keys = ProperyConvert(item);
                SqliteHelper.ExecuteInsert("MonitorConfig", keys);
            }
        }

        /// <summary>
        /// 设置序列号配置信息的 PrjModel
        /// </summary>
        /// <param name="monitorConfig"></param>
        /// <param name="prjModel"></param>
        private static void SetFileConfigPrjModel(PrjConfigModel prjModel)
        {
            Dictionary<string, string> keys = ProperyConvert(prjModel);
            SqliteHelper.ExecuteInsert("PrjConfig", keys);
        }

        /// <summary>
        /// 设置序列号配置信息的 FrameModel
        /// </summary>
        /// <param name="FrameConfig"></param>
        /// <param name="FrameModel"></param>
        private static void SetFileConfigFrameModel(FrameConfigModel FrameModel)
        {
            Dictionary<string, string> keys = ProperyConvert(FrameModel);
            SqliteHelper.ExecuteInsert("FrameConfig", keys);

            int sort = 100;

            List<string> guids = new List<string>();
            foreach (var item in FrameModel.CanFrameModels)
            {
                keys = ProperyConvert(item);
                keys.Add("CanID", item.Id.ToString());

                var guidValue = keys["Guid"];
                if (guids.Contains(guidValue))
                {
                    guidValue = System.Guid.NewGuid().ToString();
                    keys["Guid"] = guidValue;
                }
                else
                {
                    guids.Add(guidValue);
                }

                keys.Remove(nameof(item.FrameId));
                keys.Remove(nameof(item.Id));
                var keys1 = ProperyConvert(item.FrameId);
                keys["Sort"] = sort.ToString();
                keys = keys.Concat(keys1).ToDictionary(postParK => postParK.Key, PostParV => PostParV.Value);

                SqliteHelper.ExecuteInsert("CanFrame", keys);
                foreach (var frame in item.FrameDatas)
                {
                    var dataGuid = Guid.NewGuid().ToString();
                    keys = ProperyConvert(frame);
                    keys.Add("CanGuid", guidValue);
                    keys.Add("Guid", dataGuid);
                    keys.Remove(nameof(frame.DataInfos));
                    SqliteHelper.ExecuteInsert("CanFrameData", keys);

                    foreach (var info in frame.DataInfos)
                    {
                        keys = ProperyConvert(info);
                        keys.Add("DataGuid", dataGuid);
                        SqliteHelper.ExecuteInsert("CanFrameDataInfo", keys);
                    }
                }

                sort += 100;
            }
        }

        /// <summary>
        /// 设置序列号配置信息的 FrameModel
        /// </summary>
        /// <param name="eventGroup"></param>
        /// <param name="eventModels"></param>
        private static void SetFileConfigEventModels(List<EventGroupModel> eventModels)
        {
            foreach (var item in eventModels)
            {
                Dictionary<string, string> keys = ProperyConvert(item);
                SqliteHelper.ExecuteInsert("EventGroup", keys);

                foreach (var frame in item.InfoModels)
                {
                    keys = ProperyConvert(frame);
                    keys.Add("GroupID", item.Group.ToString());
                    SqliteHelper.ExecuteInsert("EventInfo", keys);
                }
            }
        }

        /// <summary>
        /// 设置序列号配置信息的 CmdModels
        /// </summary>
        /// <param name="cmdGrpConfig"></param>
        /// <param name="cmdModels"></param>
        private static void SetFileConfigCmdModels(List<CmdGrpConfigModel> cmdModels)
        {
            foreach (var item in cmdModels)
            {
                Dictionary<string, string> keys = ProperyConvert(item);
                SqliteHelper.ExecuteInsert("CmdGrpConfig", keys);

                int sort = 100;
                foreach (var frame in item.cmdConfigModels)
                {
                    keys = ProperyConvert(frame);
                    keys.Add("CmdID", item.Index.ToString());
                    keys["Sort"] = sort.ToString();
                    keys["Guid"] = System.Guid.NewGuid().ToString();
                    keys.Remove(nameof(frame.FrameModel));
                    SqliteHelper.ExecuteInsert("CmdConfig", keys);
                    sort += 100;
                }
            }
        }

        /// <summary>
        /// 设置序列号配置信息的 MemModels
        /// </summary>
        /// <param name="memoryHostory"></param>
        /// <param name="memModels"></param>
        private static void SetFileConfigMemModels(List<MemoryModel> memModels)
        {
            foreach (var item in memModels)
            {
                Dictionary<string, string> keys = ProperyConvert(item);
                SqliteHelper.ExecuteInsert("MemoryHostory", keys);
            }
        }

        /// <summary>
        /// 设置序列号配置信息的 OscilloscopeModel
        /// </summary>
        /// <param name="oscilloscope"></param>
        /// <param name="oscilloscopeModel"></param>
        private static void SetFileConfigOscilloscope(OscilloscopeModel oscilloscopeModel)
        {
            {
                Dictionary<string, string> keys = ProperyConvert(oscilloscopeModel);
                keys.Remove(nameof(oscilloscopeModel.FilesPath));
                var keys1 = ProperyConvert(oscilloscopeModel.FilesPath);
                keys = keys.Concat(keys1).ToDictionary(postParK => postParK.Key, PostParV => PostParV.Value);
                SqliteHelper.ExecuteInsert("Oscilloscope", keys);

                foreach (var item in oscilloscopeModel.ChannelInfoList)
                {
                    keys = ProperyConvert(item);
                    SqliteHelper.ExecuteInsert("ChannelInfo", keys);
                }
            }
        }
        #endregion

        #region 数据库转实体

        /// <summary>
        /// 获取序列号配置信息的 McuModels
        /// </summary>
        /// <param name="mcuConfig"></param>
        /// <param name="mcuModels"></param>
        private static void GetFileConfigMcuModels(List<McuConfigModel> mcuModels)
        {
            var sqlStr = "select McuEnable,McuName,FileType,AddrMin,AddrMax,MemWidth,FwCode from McuConfig";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var mcuConfig = TableToListModel<McuConfigDal>(dt);
            foreach (var item in mcuConfig)
            {
                var mcu = new McuConfigModel();
                var temp = MapperToModel<McuConfigModel, McuConfigDal>(mcu, item);
                mcuModels.Add(mcu);
            }
        }

        /// <summary>
        /// 获取序列号配置信息的 MonModels
        /// </summary>
        /// <param name="monitorConfig"></param>
        /// <param name="monModels"></param>
        private static void GetFileConfigMonModels(List<MonitorConfigModel> monModels)
        {
            var sqlStr = "select Title,CanText,MemberText,AlertVal1,AlertVal2,AlertVal3,AlertVal4 from MonitorConfig";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var monitorConfig = TableToListModel<MonitorConfigDal>(dt);
            foreach (var item in monitorConfig)
            {
                var monitor = new MonitorConfigModel();
                var temp = MapperToModel<MonitorConfigModel, MonitorConfigDal>(monitor, item);
                monModels.Add(monitor);
            }
        }

        /// <summary>
        /// 获取序列号配置信息的 PrjModel
        /// </summary>
        /// <param name="monitorConfig"></param>
        /// <param name="prjModel"></param>
        private static void GetFileConfigPrjModel(PrjConfigModel prjModel)
        {
            var sqlStr = "select ProjectName,WorkPath,DeviceInx,Baudrate1,Baudrate2,SendInterval,AddrMark,HeartbeatFrame,StartFrame,DataFrame,RouteTimeout,ThreadTimeout,HostFrameGuid,ModuleFrameGuid from PrjConfig";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var prjConfig = TableToListModel<PrjConfigDal>(dt);
            MapperToModel<PrjConfigModel, PrjConfigDal>(prjModel, prjConfig.FirstOrDefault());
        }

        /// <summary>
        /// 获取序列号配置信息的 FrameModel
        /// </summary>
        /// <param name="FrameConfig"></param>
        /// <param name="FrameModel"></param>
        private static void GetFileConfigFrameModel(FrameConfigModel FrameModel)
        {
            var sqlStr = "select SrcIdBitNum,SrcAddrBitNum,TargetIdBitNum,TargetAddrBitNum from FrameConfig";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var FrameConfig = TableToListModel<FrameConfigDal>(dt);

            sqlStr = "select CanID as Id,Guid,Name,AutoTx,Sort,SrcAddr,SrcType,DstAddr,DstType,FC,PF,ContinuousFlag,FrameType,Priority from CanFrame order by sort ";
            dt = SqliteHelper.ExecuteTable(sqlStr);
            var CanFrame = TableToListModel<CanFrameDal>(dt);

            sqlStr = "select Guid,CanGuid,DataLen,Data,DataNum from CanFrameData";
            dt = SqliteHelper.ExecuteTable(sqlStr);
            var CanFrameData = TableToListModel<CanFrameDataDal>(dt);

            sqlStr = "select DataGuid,Name,Type,ByteRange,Value,Precision,Unit,Hide from CanFrameDataInfo";
            dt = SqliteHelper.ExecuteTable(sqlStr);
            var CanFrameDataInfo = TableToListModel<CanFrameDataInfoDal>(dt);

            MapperToModel<FrameConfigModel, FrameConfigDal>(FrameModel, FrameConfig.FirstOrDefault());

            foreach (var item in CanFrame)
            {
                var can = new CanFrameModel();
                var temp = MapperToModel<CanFrameModel, CanFrameDal>(can, item);
                FrameModel.CanFrameModels.Add(can);
                temp.FrameId = new CanFrameID()
                {
                    ID = temp.Id,
                    SrcAddr = item.SrcAddr,
                    DstAddr = item.DstAddr,
                    DstType = item.DstType,
                    FC = item.FC,
                    PF = item.PF,
                    ContinuousFlag = item.ContinuousFlag,
                    FrameType = item.FrameType,
                    Priority = item.Priority,
                };

                var canFrameDatas = CanFrameData.Where(t => t.CanGuid == temp.Guid).ToList();
                foreach (var config in canFrameDatas)
                {
                    var data = new CanFrameData()
                    {
                        DataLen = config.DataLen,
                        Data = new byte[] { },
                        DataNum = config.DataNum
                    };
                    temp.FrameDatas.Add(data);

                    var dataInfoes = CanFrameDataInfo.Where(t => t.DataGuid == config.Guid).ToList();
                    foreach (var info in dataInfoes)
                    {
                        data.DataInfos.Add(new CanProtocol.ProtocolModel.CanFrameDataInfo()
                        {
                            Name = info.Name,
                            Type = info.Type,
                            ByteRange = info.ByteRange,
                            Value = info.Value,
                            Precision = info.Precision,
                            Unit = info.Unit == null ? string.Empty : info.Unit,
                            Hide = info.Hide,
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 获取序列号配置信息的 FrameModel
        /// </summary>
        /// <param name="eventGroup"></param>
        /// <param name="eventModels"></param>
        private static void GetFileConfigEventModels(List<EventGroupModel> eventModels)
        {
            var sqlStr = "select [Group],Enable,FrameGuid,MemberIndex from EventGroup";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var eventGroup = TableToListModel<EventGroupDal>(dt);

            sqlStr = "select GroupID,Name,Enable,Type,Bit,Mark from EventInfo";
            dt = SqliteHelper.ExecuteTable(sqlStr);
            var eventInfo = TableToListModel<EventInfoDal>(dt);


            foreach (var item in eventGroup)
            {
                var @event = new EventGroupModel();
                var temp = MapperToModel<EventGroupModel, EventGroupDal>(@event, item);
                eventModels.Add(@event);

                var eventInfoes = eventInfo.Where(t => t.GroupID == temp.Group).ToList();
                foreach (var config in eventInfoes)
                {
                    var info = new EventInfoModel();
                    var cmd = MapperToModel<EventInfoModel, EventInfoDal>(info, config);
                    @event.InfoModels.Add(cmd);
                }
            }
        }

        /// <summary>
        /// 获取序列号配置信息的 CmdModels
        /// </summary>
        /// <param name="cmdGrpConfig"></param>
        /// <param name="cmdModels"></param>
        private static void GetFileConfigCmdModels(List<CanFrameModel> CanFrameModels, List<CmdGrpConfigModel> cmdModels)
        {
            var sqlStr = "select Name,IsBroadcast,[Index] from CmdGrpConfig";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var cmdGrpConfig = TableToListModel<CmdGrpConfigDal>(dt);

            sqlStr = "select CmdID,CmdType,FrameGuid,SetValue,Sort,Guid from CmdConfig";
            dt = SqliteHelper.ExecuteTable(sqlStr);
            var cmdConfig = TableToListModel<CmdConfigDal>(dt);

            foreach (var item in cmdGrpConfig)
            {
                var grp = new CmdGrpConfigModel();
                var temp = MapperToModel<CmdGrpConfigModel, CmdGrpConfigDal>(grp, item);
                cmdModels.Add(grp);

                var cmdConfigs = cmdConfig.Where(t => t.CmdID == temp.Index).ToList();
                foreach (var config in cmdConfigs)
                {
                    var info = new CmdConfigModel();
                    var cmd = MapperToModel<CmdConfigModel, CmdConfigDal>(info, config);
                    cmd.FrameModel = CanFrameModels.FirstOrDefault(t => t.Guid == cmd.FrameGuid);
                    grp.cmdConfigModels.Add(cmd);
                }
            }
        }

        /// <summary>
        /// 获取序列号配置信息的 MemModels
        /// </summary>
        /// <param name="memoryHostory"></param>
        /// <param name="memModels"></param>
        private static void GetFileConfigMemModels(List<MemoryModel> memModels)
        {
            var sqlStr = "select AddressOrName,Type,Value,Remark,Address from MemoryHostory";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var memoryHostory = TableToListModel<MemoryDal>(dt);
            foreach (var item in memoryHostory)
            {
                var mcu = new MemoryModel();
                var temp = MapperToModel<MemoryModel, MemoryDal>(mcu, item);
                memModels.Add(mcu);
            }
        }

        /// <summary>
        /// 获取序列号配置信息的 OscilloscopeModel
        /// </summary>
        /// <param name="oscilloscope"></param>
        /// <param name="oscilloscopeModel"></param>
        private static void GetFileConfigOscilloscope(OscilloscopeModel oscilloscopeModel)
        {
            var sqlStr = "select UnderSampleScale,TrigMode,TrigSource,TrigYLevel,TrigXPercent,CoffPath,DwarfXmlPath from Oscilloscope";
            var dt = SqliteHelper.ExecuteTable(sqlStr);
            var oscilloscope = TableToListModel<OscilloscopeDal>(dt);

            sqlStr = "select VariableName,DataType,FloatDataScale,Comment from ChannelInfo";
            dt = SqliteHelper.ExecuteTable(sqlStr);
            var channelInfo = TableToListModel<ChannelInfoDal>(dt);

            MapperToModel<OscilloscopeModel, OscilloscopeDal>(oscilloscopeModel, oscilloscope.FirstOrDefault());
            foreach (var item in channelInfo)
            {
                var info = new ChannelInfoModel();
                var temp = MapperToModel<ChannelInfoModel, ChannelInfoDal>(info, item);
                oscilloscopeModel.ChannelInfoList.Add(info);
            }
        }

        #endregion

        #region 清除数据

        public static bool ClearDataBaseData()
        {
            List<string> sqlList = new List<string>()
            {
                "delete from McuConfig;",
                "delete from MonitorConfig;",
                "delete from PrjConfig;",
                "delete from FrameConfig;",
                "delete from CanFrame;",
                "delete from CanFrameData;",
                "delete from CanFrameDataInfo;",
                "delete from CmdConfig;",
                "delete from CmdGrpConfig;",
                "delete from Oscilloscope;",
                "delete from ChannelInfo;",
                "delete from EventGroup;",
                "delete from EventInfo;",
                "delete from MemoryHostory;",
            };

            foreach (var item in sqlList)
            {
                SqliteHelper.ExecuteNonQuery(item);
            }

            return true;
        }
        #endregion

        #region 读取数据

        /// <summary>
        /// 实时读取帧配置数据
        /// </summary>
        /// <returns></returns>
        public static List<McuConfigModel> GetMcuConfigModel()
        {
            var model = new List<McuConfigModel>();
            GetFileConfigMcuModels(model);
            return model;
        }

        /// <summary>
        /// 实时读取帧配置数据
        /// </summary>
        /// <returns></returns>
        public static List<MonitorConfigModel> GetMonitorConfigModel()
        {
            var model = new List<MonitorConfigModel>();
            GetFileConfigMonModels(model);
            return model;
        }

        /// <summary>
        /// 实时读取 项目配置数据
        /// </summary>
        /// <returns></returns>
        public static PrjConfigModel GetPrjConfigModel()
        {
            var model = new PrjConfigModel();
            GetFileConfigPrjModel(model);
            return model;
        }

        /// <summary>
        /// 实时读取帧配置数据
        /// </summary>
        /// <returns></returns>
        public static FrameConfigModel GetFrameConfigModel()
        {
            var model = new FrameConfigModel();
            GetFileConfigFrameModel(model);
            return model;
        }

        /// <summary>
        /// 实时读取帧配置数据
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static List<CmdGrpConfigModel> GetFCmdGrpConfigModel(FrameConfigModel frame)
        {
            var model = new List<CmdGrpConfigModel>();
            GetFileConfigCmdModels(frame.CanFrameModels, model);
            return model;
        }

        /// <summary>
        /// 实时读取帧配置数据
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static List<EventGroupModel> GetEventGroupModel()
        {
            var model = new List<EventGroupModel>();
            GetFileConfigEventModels(model);
            return model;
        }
        #endregion

        #region 转换方法

        public static Dictionary<string, string> ProperyConvert<T>(T t) where T : new()
        {
            // 定义集合    
            Dictionary<string, string> dir = new Dictionary<string, string>();

            // 获得此模型的类型   
            Type type = typeof(T);

            // 获得此模型的公共属性      
            PropertyInfo[] propertys = t.GetType().GetProperties();
            foreach (PropertyInfo pi in propertys)
            {
                var type1 = pi.PropertyType;
                if (type1 != null && type1.Name == "List`1")
                {
                    continue;
                }
                else if (type1 == typeof(byte[]))
                {
                    dir.Add(pi.Name, "");
                    continue;
                }

                if (pi.Name != "ID")
                {
                    var value = pi.GetValue(t);
                    string str = value == null ? "" : value.ToString();
                    dir.Add(pi.Name, str);
                }
            }


            return dir;
        }

        public static List<T> TableToListModel<T>(DataTable dt) where T : new()
        {
            // 定义集合    
            List<T> ts = new List<T>();

            // 获得此模型的类型   
            Type type = typeof(T);
            string tempName = "";

            foreach (DataRow dr in dt.Rows)
            {
                T t = new T();
                // 获得此模型的公共属性      
                PropertyInfo[] propertys = t.GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    tempName = pi.Name;  // 检查DataTable是否包含此列    

                    if (dt.Columns.Contains(tempName))
                    {
                        // 判断此属性是否有Setter      
                        if (!pi.CanWrite) continue;

                        object value = dr[tempName];
                        if (value != DBNull.Value && value != null && !string.IsNullOrEmpty(value.ToString()))
                        {
                            switch (pi.PropertyType.Name)
                            {
                                case "Int32":
                                    pi.SetValue(t, Convert.ToInt32(value), null);
                                    break;
                                case "UInt32":
                                    pi.SetValue(t, Convert.ToUInt32(value), null);
                                    break;
                                case "Single":
                                    pi.SetValue(t, Convert.ToSingle(value), null);
                                    break;
                                case "Decimal":
                                    pi.SetValue(t, Convert.ToDecimal(value), null);
                                    break;
                                case "Double":
                                    pi.SetValue(t, Convert.ToDouble(value), null);
                                    break;
                                case "Boolean":
                                    pi.SetValue(t, value.ToString() == "1" || value.ToString() == "True", null);
                                    break;
                                case "String":
                                    pi.SetValue(t, value.ToString(), null);
                                    break;
                                case "DateTime":
                                    pi.SetValue(t, Convert.ToDateTime(value), null);
                                    break;
                                case "Byte":
                                    pi.SetValue(t, Convert.ToByte(value), null);
                                    break;
                                default:
                                    break;
                            }
                        }

                    }
                }
                ts.Add(t);
            }
            return ts;
        }


        /// <summary>
        /// 反射实现两个类的对象之间相同属性的值的复制
        /// 适用于没有新建实体之间
        /// </summary>
        /// <typeparam name="R">返回的实体</typeparam>
        /// <typeparam name="S">数据源实体</typeparam>
        /// <param name="r">返回的实体</param>
        /// <param name="s">数据源实体</param>
        /// <param name="isContainsNull">字段为null是否的跳过</param>
        /// <param name="isContainsEmpty">字段为空字符串的是否的跳过</param>
        /// <returns></returns>
        public static R MapperToModel<R, S>(R r, S s, bool isContainsNull = true, bool isContainsEmpty = true)
        {
            try
            {
                var Types = s.GetType();//获得类型  
                var Typed = typeof(R);
                foreach (PropertyInfo sp in Types.GetProperties())//获得类型的属性字段  
                {
                    foreach (PropertyInfo dp in Typed.GetProperties())
                    {
                        if (dp.Name == sp.Name && dp.PropertyType == sp.PropertyType && dp.Name != "Error" && dp.Name != "Item")//判断属性名是否相同  
                        {
                            object val = sp.GetValue(s, null);
                            if (!isContainsNull && val == null)
                            {
                                continue;
                            }
                            if (!isContainsEmpty && val != null &&
                                (val is string) && string.IsNullOrWhiteSpace(val.ToString()))
                            {
                                continue;
                            }

                            if (val == null && sp.PropertyType == typeof(string))
                            {
                                val = string.Empty;
                            }
                            dp.SetValue(r, val, null);//获得s对象属性的值复制给r对象的属性  
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return r;
        }

        #endregion

        #region MCU 配置
        public static bool UpdateMonitorConfigModel(List<McuConfigModel> models, bool isUpdate = true)
        {
            // 删除所有的
            var str = $"delete from McuConfig";
            SqliteHelper.ExecuteNonQuery(str);

            // 插入数据
            SetFileConfigMcuModels(models);

            if (isUpdate)
                JsonConfigHelper.FileConfig.McuModels = models;

            return true;
        }
        #endregion

        #region 主参数配置

        public static bool UpdateMonitorConfigModel(List<MonitorConfigModel> models, bool isUpdate = true)
        {
            // 删除所有的
            var str = $"delete from MonitorConfig";
            SqliteHelper.ExecuteNonQuery(str);

            // 插入数据
            SetFileConfigMonModels(models);
            if (isUpdate)
                JsonConfigHelper.FileConfig.MonModels = models;
            return true;
        }
        #endregion

        #region 项目配置

        public static bool SelectPrjConfigModel()
        {
            var str = $"select 1 from  PrjConfig";
            if (SqliteHelper.ExecuteTable(str).Rows.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static bool UpdatePrjConfigModel(PrjConfigModel prjCfgModel)
        {
            var strSql = "select id from PrjConfig order by id desc limit 1 ";
            var query = SqliteHelper.ExecuteScalar(strSql);
            if (query != null && Convert.ToInt32(query) > 0)
            {
                var id = Convert.ToInt32(query);
                // 保存当前 Can 配置信息
                strSql = $"update PrjConfig set  SendInterval = {prjCfgModel.SendInterval},DeviceInx={prjCfgModel.DeviceInx},Baudrate1={prjCfgModel.Baudrate1},Baudrate2={prjCfgModel.Baudrate2} where id = {id}";
                query = SqliteHelper.ExecuteNonQuery(strSql);
                return query != null && Convert.ToInt32(query) > 0;
            }

            return false;
        }
        #endregion

        #region Can 参数配置

        public static bool UpdatePrjConfigModel_DeviceInx(uint index, bool isUpdate = true)
        {
            var strSql = "select id from PrjConfig order by id desc limit 1 ";
            var query = SqliteHelper.ExecuteScalar(strSql);
            if (query != null && Convert.ToInt32(query) > 0)
            {
                var id = Convert.ToInt32(query);
                var str = $"update PrjConfig set DeviceInx={index} where id = {id}";
                if (query != null && Convert.ToInt32(query) > 0)
                {
                    if (isUpdate)
                        JsonConfigHelper.FileConfig.PrjModel.DeviceInx = index;
                    return true;
                }
            }

            return false;
        }

        public static bool UpdatePrjConfigModel_Baudrate1(int index, bool isUpdate = true)
        {
            var strSql = "select id from PrjConfig order by id desc limit 1 ";
            var query = SqliteHelper.ExecuteScalar(strSql);
            if (query != null && Convert.ToInt32(query) > 0)
            {
                var id = Convert.ToInt32(query);
                var str = $"update PrjConfig set Baudrate1={index} where id = {id}";
                if (query != null && Convert.ToInt32(query) > 0)
                {
                    if (isUpdate)
                        JsonConfigHelper.FileConfig.PrjModel.Baudrate1 = index;
                    return true;
                }
            }

            return false;
        }

        public static bool UpdatePrjConfigModel_Baudrate2(int index, bool isUpdate = true)
        {
            var strSql = "select id from PrjConfig order by id desc limit 1 ";
            var query = SqliteHelper.ExecuteScalar(strSql);
            if (query != null && Convert.ToInt32(query) > 0)
            {
                var id = Convert.ToInt32(query);
                var str = $"update PrjConfig set Baudrate2={index} where id = {id}";
                if (query != null && Convert.ToInt32(query) > 0)
                {
                    if (isUpdate)
                        JsonConfigHelper.FileConfig.PrjModel.Baudrate2 = index;
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region 帧的 增加和修改

        /// <summary>
        /// 更新行信息
        /// </summary>
        /// <param name="sort">更新后的行索引</param>
        public static bool UpdateCanFrame(int sort, string guid)
        {
            var str = $"Update CanFrame set Sort = {sort} where Guid = '{guid}'";
            if (DataManager.UpdateCanFrameModels(str))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新行信息
        /// </summary>
        /// <param name="currSort">当前更新的行索引</param>
        /// <param name="afterSort">更新后的行索引</param>
        public static bool DeleteCanFrame(string guid)
        {
            var str = $"delete from CanFrame Where Guid = '{guid}' ";
            return SqliteHelper.ExecuteNonQuery(str) > 0;
        }

        /// <summary>
        /// 插入帧数据
        /// </summary>
        /// <param name="model"></param>
        /// <param name="guid"></param>
        public static void InsertCanFrameDataModels(CanFrameModel model)
        {
            string guid = model.Guid;
            // 新增帧数据
            Dictionary<string, string> keys = DataManager.ProperyConvert(model);
            keys.Add("CanID", model.Id.ToString());
            keys.Remove(nameof(model.FrameId));
            keys.Remove(nameof(model.Id));
            var keys1 = DataManager.ProperyConvert(model.FrameId);
            keys["Sort"] = "0";
            keys = keys.Concat(keys1).ToDictionary(postParK => postParK.Key, PostParV => PostParV.Value);
            if (SqliteHelper.ExecuteInsert("CanFrame", keys) > 0)
            {
                // 插入层级数据
                InsertCanFrameData(model, guid);
            }

            // 排序
            DataManager.ReSortCanFrame(JsonConfigHelper.FileConfig.FrameModel.CanFrameModels.OrderBy(t => t.Sort).ToList());
        }

        /// <summary>
        /// 更新帧数据
        /// </summary>
        /// <param name="model"></param>
        /// <param name="guid"></param>
        public static void UpdateCanFrameDataModels(CanFrameModel model)
        {
            string guid = model.Guid;
            // 删除层级数据
            var flag = DataManager.DeleteCanFrameDataModels(guid);

            // 更新帧数据
            var strSql = $"update CanFrame set AutoTx = {model.AutoTx},Name = '{model.Name}',SrcAddr = {model.FrameId.SrcAddr},SrcType = {model.FrameId.SrcType},DstAddr = {model.FrameId.DstAddr},DstType = {model.FrameId.DstType},FC = {model.FrameId.FC},PF = {model.FrameId.PF},ContinuousFlag = {model.FrameId.ContinuousFlag},FrameType = {model.FrameId.FrameType},Priority = {model.FrameId.Priority} where Guid = '{guid}'";

            if (SqliteHelper.ExecuteNonQuery(strSql) > 0)
            {
                // 插入层级数据
                InsertCanFrameData(model, guid);
            }
        }

        /// <summary>
        /// 插入帧关联数据
        /// </summary>
        private static void InsertCanFrameData(CanFrameModel model, string guid)
        {
            foreach (var frame in model.FrameDatas)
            {
                var dataGuid = Guid.NewGuid().ToString();
                var keys = DataManager.ProperyConvert(frame);
                keys.Add("CanGuid", guid);
                keys.Add("Guid", dataGuid);
                keys.Remove(nameof(frame.DataInfos));
                SqliteHelper.ExecuteInsert("CanFrameData", keys);

                foreach (var info in frame.DataInfos)
                {
                    keys = DataManager.ProperyConvert(info);
                    keys.Add("DataGuid", dataGuid);
                    SqliteHelper.ExecuteInsert("CanFrameDataInfo", keys);
                }
            }
        }
        #endregion

        #region 命令组 增加和修改

        /// <summary>
        /// 更新行信息
        /// </summary>
        /// <param name="sort">更新后的行索引</param>
        public static bool UpdateCmdGrpConfig(int index, string name, bool isUpdate = true)
        {
            var str = $"update CmdGrpConfig  set name = '{name}' where [index] = {index} ";
            if (DataManager.UpdateCanFrameModels(str))
            {
                if (isUpdate)
                {
                    var item = JsonConfigHelper.FileConfig.CmdModels.FirstOrDefault(t => t.Index == index);
                    if (item != null)
                    {
                        item.Name = name;
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新帧数据
        /// </summary>
        /// <param name="model"></param>
        /// <param name="guid"></param>
        public static bool UpdateCmdGrpConfig(CmdGrpConfigModel model, bool isUpdate = true)
        {
            bool result = true;
            // 删除层级数据
            var strSql_1 = $"delete from CmdConfig where CmdID = {model.Index} ";
            var flag = SqliteHelper.ExecuteNonQuery(strSql_1) > 0;

            if (flag)
            {
                // 插入层级数据
                int sort = 100;
                foreach (var frame in model.cmdConfigModels)
                {
                    var keys = ProperyConvert(frame);
                    keys.Add("CmdID", model.Index.ToString());
                    keys["Sort"] = sort.ToString();
                    keys["Guid"] = System.Guid.NewGuid().ToString();
                    keys.Remove(nameof(frame.FrameModel));
                    if (SqliteHelper.ExecuteInsert("CmdConfig", keys) <= 0)
                    {
                        result = false;
                        break;
                    }
                    sort += 100;
                }
            }
            else
            {
                strSql_1 = $"select FrameGuid from CmdConfig where CmdID = {model.Index} ";
                if (SqliteHelper.ExecuteTable(strSql_1).Rows.Count == 0)
                {
                    // 插入层级数据
                    int sort = 100;
                    foreach (var frame in model.cmdConfigModels)
                    {
                        var keys = ProperyConvert(frame);
                        keys.Add("CmdID", model.Index.ToString());
                        keys["Sort"] = sort.ToString();
                        keys["Guid"] = System.Guid.NewGuid().ToString();
                        keys.Remove(nameof(frame.FrameModel));
                        if (SqliteHelper.ExecuteInsert("CmdConfig", keys) <= 0)
                        {
                            result = false;
                            break;
                        }
                        sort += 100;
                    }
                }
                else
                {
                    result = false;
                }
            }

            if (result && isUpdate)
            {
                var item = JsonConfigHelper.FileConfig.CmdModels.FirstOrDefault(t => t.Index == model.Index);
                if (item != null)
                {
                    item.cmdConfigModels = model.cmdConfigModels;
                }
            }
            return result;
        }

        /// <summary>
        /// 更新行信息
        /// </summary>
        /// <param name="sort">更新后的行索引</param>
        public static bool UpdateCmdConfig(int sort, string guid, bool isUpdate = true)
        {
            var str = $"Update CmdConfig set Sort = {sort} where Guid = '{guid}'";
            if (DataManager.UpdateCanFrameModels(str))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region 事件组 增加和修改

        public static bool SelectEventGroupModel(int group, bool isUpdate = true)
        {
            var str = $"select 1 from  EventGroup where [Group] = {group} ";
            if (SqliteHelper.ExecuteTable(str).Rows.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static bool UpdateEventGroupModel(EventGroupModel group, bool isUpdate = true)
        {
            bool result = true;

            // 1. 先查找数据库中是否有记录
            if (SelectEventGroupModel(group.Group))
            {
                // 2. 更新事件组
                var str = $"update EventGroup set Enable = {group.Enable},FrameGuid = '{group.FrameGuid}',MemberIndex = {group.MemberIndex}  where [Group] = {group.Group} ";
                if (SqliteHelper.ExecuteNonQuery(str) > 0)
                {
                    str = $"delete from EventInfo where GroupID = {group.Group} ";
                    SqliteHelper.ExecuteNonQuery(str);

                    // 3. 插入事件组数据信息
                    foreach (var frame in group.InfoModels)
                    {
                        var keys = ProperyConvert(frame);
                        keys.Add("GroupID", group.Group.ToString());
                        if (SqliteHelper.ExecuteInsert("EventInfo", keys) <= 0)
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                {
                    result = false;
                }
            }
            else
            {
                // 2. 更新事件组
                var keys = ProperyConvert(group);
                if (SqliteHelper.ExecuteInsert("EventGroup", keys) > 0)
                {
                    // 3. 插入事件组数据信息
                    foreach (var frame in group.InfoModels)
                    {
                        keys = ProperyConvert(frame);
                        keys.Add("GroupID", group.Group.ToString());
                        if (SqliteHelper.ExecuteInsert("EventInfo", keys) <= 0)
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                {
                    result = false;
                }
            }

            if (result && isUpdate)
            {
                var item = JsonConfigHelper.FileConfig.EventModels.FirstOrDefault(t => t.Group == group.Group);
                if (item != null)
                {
                    item = group;
                }
            }

            return result;
        }
        #endregion

        #region Map 配置

        /// <summary>
        /// 更新 Map 配置
        /// </summary>
        /// <param name="models"></param>
        /// <param name="isUpdate"></param>
        /// <returns></returns>
        public static bool UpdateMemoryModel(List<MemoryModel> models, bool isUpdate = true)
        {
            // 删除所有的
            var str = $"delete from MemoryHostory";
            SqliteHelper.ExecuteNonQuery(str);

            // 插入数据
            SetFileConfigMemModels(models);

            if (isUpdate)
            {
                JsonConfigHelper.FileConfig.MemModels = models;
            }

            return true;
        }
        #endregion

        #region 业务代码

        /// <summary>
        /// 重新排版 CanFrame
        /// (请在外层进行排序操作)
        /// </summary>
        /// <param name="canFrameModels"></param>
        public static void ReSortCanFrame(List<CanFrameModel> canFrameModels)
        {
            // 重新排版
            int sort = 100;
            foreach (var item in canFrameModels)
            {
                try
                {
                    item.Sort = sort;
                    string strSql = $"update CanFrame set Sort = {sort} where Guid = '{item.Guid}'";
                    SqliteHelper.ExecuteNonQuery(strSql);
                }
                catch (Exception ex)
                {
                }
                sort += 100;
            }

            JsonConfigHelper.ReadConfigFile().FrameModel.ReSortCanFrameModels();
        }

        /// <summary>
        /// 更新 CanFrameModels
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static bool UpdateCanFrameModels(string strSql)
        {
            return SqliteHelper.ExecuteNonQuery(strSql) > 0;
        }

        /// <summary>
        /// 插入 CanFrameModels
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static bool InsertCanFrameModels(string tbName, Dictionary<String, String> insertData)
        {
            return SqliteHelper.ExecuteInsert("CanFrame", insertData) > 0;
        }

        /// <summary>
        /// 更新 CanFrameData
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static bool DeleteCanFrameDataModels(string guid)
        {
            string strSql = $"select guid from CanFrameData where CanGuid = '{guid}' ";
            var data = SqliteHelper.ExecuteScalar(strSql);
            if (data == null || string.IsNullOrWhiteSpace(data.ToString()))
            {
                return false;
            }

            var strSql_1 = $"delete from CanFrameData where CanGuid = '{guid}' ";
            var strSql_2 = $"delete from CanFrameDataInfo where DataGuid = '{data}' ";
            return SqliteHelper.ExecuteNonQuery(strSql_1) > 0 && SqliteHelper.ExecuteNonQuery(strSql_2) > 0;
        }
        #endregion
    }
}
