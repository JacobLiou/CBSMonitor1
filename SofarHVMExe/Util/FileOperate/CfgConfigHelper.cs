using SofarHVMExe.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMDAL
{
    //[Obsolete("当前config配置类被弃用，改为使用Json配置类；暂时保留，后面完全不需要再删掉")]
    //public class CfgConfigHelper
    //{
    //    public CfgConfigHelper()
    //    {
    //        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    //    }

    //    //private string path = @"C:\Users\admin\Desktop\上海集中式储能\SofarHVM\Test.cfg";
    //    private string path = System.AppDomain.CurrentDomain.BaseDirectory + @"Config\HVM-Config.cfg";
    //    public string Path
    //    {
    //        get { return path; }
    //        set { value = path; }
    //    }
        
    //    //获取所有节点的value
    //    public FileConfigModel GetSettingValue()
    //    {
    //        FileConfigModel @return = new FileConfigModel();

    //        Encoding code = Encoding.GetEncoding("GBK");
    //        //Loading a Configuration
    //        Configuration config = Configuration.LoadFromFile(Path, code);//Load from a text-based file

    //        //Iterating through a Configuration
    //        foreach (Section section in config)
    //        {
    //            Debug.WriteLine("\n\n" + section.Name);

    //            switch (section.Name)
    //            {
    //                case "项目配置":
    //                    List<ProjectConfigModel> proModels = new List<ProjectConfigModel>();

    //                    foreach (var setting in section)
    //                    {
    //                        string[] strData = setting.ToString().Split("=");

    //                        ProjectConfigModel model = new ProjectConfigModel(strData[0], strData[1]);

    //                        proModels.Add(model);
    //                    }
    //                    //@return.ProModels = proModels;
    //                    break;
    //                case "MCU配置":
    //                    McuConfigModel mcuModel = new McuConfigModel();

    //                    foreach (var setting in section)
    //                    {
    //                        string[] strData = setting.ToString().Split("=");

    //                        string[] datas = strData[1].Split(',');

    //                        if (datas.Length >= 7)
    //                        {
    //                            //McuCfgInfoModel infoModel = new McuCfgInfoModel();
    //                            //infoModel.McuEnable = Convert.ToBoolean(Convert.ToInt32(datas[0]));
    //                            //infoModel.McuName = datas[1].ToString();
    //                            //infoModel.FileType = datas[2].ToString();
    //                            //infoModel.AddrMin = Convert.ToUInt32(datas[3], 16);
    //                            //infoModel.AddrMax = Convert.ToUInt32(datas[4], 16);
    //                            //infoModel.MemWidth = Convert.ToInt32(datas[5], 16);
    //                            //infoModel.FwCode = Convert.ToUInt32(datas[6], 16);

    //                            //mcuModel.infoModels.Add(infoModel);
    //                        }
    //                    }
    //                    //@return.McuModel = mcuModel;
    //                    break;
    //                case "监控配置":
    //                    List<MonitorConfigModel> monModels = new List<MonitorConfigModel>();
    //                    foreach (var setting in section)
    //                    {
    //                        string[] strData = setting.ToString().Split("=");

    //                        string[] datas = strData[1].Split(',');

    //                        if (datas.Length >= 6)
    //                        {
    //                            MonitorConfigModel model = new MonitorConfigModel();
    //                            //model.Title = strData[0];
    //                            //model.CanIDText1 = datas[0].ToString();  //这里需要修改
    //                            //model.MemberText1 = datas[1].ToString();
    //                            //model.UpRedAlertVal1 = Convert.ToDouble(datas[2]);
    //                            //model.UpYellowAlertVal1 = Convert.ToDouble(datas[3]);
    //                            //model.DownYellowAlertVal1 = Convert.ToDouble(datas[4]);
    //                            //model.DownRedAlertVal1 = Convert.ToDouble(datas[5]);

    //                            monModels.Add(model);
    //                        }
    //                    }
    //                    break;
    //                case "EVENT":
    //                    List<EventConfigModel> eventModels = new List<EventConfigModel>();
    //                    foreach (var setting in section)
    //                    {
    //                        string[] strData = setting.ToString().Split("=");

    //                        string[] datas = strData[1].Split(',');

    //                        EventConfigModel model = new EventConfigModel();
    //                        model.EventEnable = Convert.ToBoolean(Convert.ToInt32(datas[0]));
    //                        model.Id = Convert.ToInt32(datas[1], 16);
    //                        model.MemberIndex = Convert.ToInt32(datas[2]);
    //                        for (int i = 3; i < datas.Length - 32; i += 2)
    //                        {
    //                            int bitIndex = i / 2;

    //                            model.Remark[bitIndex] = new string[] { datas[i], datas[i + 1] };
    //                        }
    //                    }
    //                    break;
    //                case "CMD":
    //                    List<CmdConfigModel> cmdModels = new List<CmdConfigModel>();
    //                    foreach (var setting in section)
    //                    {
    //                        string[] strData = setting.ToString().Replace("\"", "").Split("=");

    //                        string[] datas = strData[1].Replace(" ", "").Split(',');

    //                        if (datas.Length >= 3 && datas[0] != "")
    //                        {
    //                            CmdConfigModel model = new CmdConfigModel();
    //                            //model.Id = Convert.ToInt32(strData[0]);
    //                            model.CmdType = Convert.ToInt32(datas[0]);
    //                            //model.CanID = Convert.ToInt32(datas[1], 16);

    //                            string[] item = new string[datas.Length - 2];
    //                            for (int i = 0; i < item.Length; i++)
    //                            {
    //                                item[i] = datas[i + 2];
    //                            }
    //                            //model.Param = String.Join(",", item);

    //                            cmdModels.Add(model);
    //                        }
    //                    }
    //                    break;
    //                case "CAN_FRAME":
    //                    //导入后需要执行增删查改
    //                    List<FrameConfigModel> canFarmeModels = new List<FrameConfigModel>();

    //                    break;
    //                default:

    //                    break;
    //            }
    //        }

    //        //Saving a Configuration
    //        config.SaveToFile(Path);

    //        return @return;
    //    }

    //    public ProjectConfigModel ReadProjectConfig()
    //    {
    //        ProjectConfigModel model = new ProjectConfigModel();
    //        Configuration config = null;

    //        try
    //        {
    //            Encoding code = Encoding.GetEncoding("GBK");
    //            config = Configuration.LoadFromFile(Path, code);//Load from a text-based file
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine("ReadProjectConfig: " + ex.Message);
    //            return model;
    //        }

    //        if (config == null)
    //            return model;

    //        Section section = config["项目配置"];
    //        if (section == null)
    //            return model;

    //        int baudRate, sendInterval, addrMark, hearBearFrame, startFrame, dataFrame;
    //        int routeTimeout, threadTimeout, hearBeatId, pingId;
    //        string temp = "";

    //        //model.ProjectName = section["项目名"].StringValue;
    //        //model.WorkPath = section["工作路径"].StringValue;

    //        //int.TryParse(section["波特率号"].StringValue, out baudRate);
    //        //model.BaudRate = baudRate;

    //        //int.TryParse(section["发送间隔"].StringValue, out sendInterval);
    //        //model.SendInterval = sendInterval;

    //        //temp = section["地址掩码"].StringValue.Replace("0x", "").Replace("0X", "");
    //        //int.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addrMark);
    //        //model.AddrMark = addrMark;

    //        //temp = section["心跳帧滤波"].StringValue.Replace("0x", "").Replace("0X", "");
    //        //int.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hearBearFrame);
    //        //model.HeartbeatFrame= hearBearFrame;

    //        //temp = section["心跳帧滤波"].StringValue.Replace("0x", "").Replace("0X", "");
    //        //int.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startFrame);
    //        //model.StartFrame = startFrame;

    //        //temp = section["数据帧滤波"].StringValue.Replace("0x", "").Replace("0X", "");
    //        //int.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out dataFrame);
    //        //model.DataFrame = dataFrame;

    //        //int.TryParse(section["路由超时"].StringValue, out routeTimeout);
    //        //model.RouteTimeout = routeTimeout;

    //        //int.TryParse(section["线程超时"].StringValue, out threadTimeout);
    //        //model.ThreadTimeout = threadTimeout;

    //        //temp = section["心跳帧ID"].StringValue.Replace("0x", "").Replace("0X", "");
    //        //int.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hearBeatId);
    //        //model.HeartbeatID = hearBeatId;

    //        //temp = section["Ping帧ID"].StringValue.Replace("0x", "").Replace("0X", "");
    //        //int.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out pingId);
    //        //model.PingID = pingId;

    //        return model;
    //    }

    //    public void AddSettingValue(string sectionName, string settingName, string val)
    //    {
    //        // Create the configuration.
    //        var myConfig = new Configuration();

    //        // Determine data type
    //        string[] strVal = val.Split(',');

    //        // Set some values.
    //        // This will automatically create the sections and settings.
    //        if (strVal.Length < 0 && strVal.Length == 1)
    //        {
    //            myConfig[sectionName][settingName].StringValue = strVal[0];
    //        }
    //        else
    //        {
    //            myConfig[sectionName][settingName].StringValueArray = strVal;
    //        }

    //        //Saving a Configuration
    //        myConfig.SaveToFile(Path);
    //    }

    //    public void EditSettingValue(string sectionName, string settingName, string val)
    //    {
    //        //Loading a Configuration
    //        Configuration config = Configuration.LoadFromFile(Path);

    //        //Iterating through a Configuration
    //        foreach (Section section in config)
    //        {
    //            if (section.Name == sectionName)
    //            {
    //                section[settingName].StringValue = val;
    //            }
    //        }

    //        //Saving a Configuration
    //        config.SaveToFile(Path);
    //    }

    //    public void WriteProjectConfig(ProjectConfigModel model)
    //    {
    //        if (model == null)
    //            return;

    //        Configuration config = null;

    //        try
    //        {
    //            config = Configuration.LoadFromFile(Path, Encoding.GetEncoding("GBK"));//Load from a text-based file
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine("WriteProjectConfig: " + ex.Message);
    //            return;
    //        }

    //        if (config == null)
    //            return;

    //        Section section = config["项目配置"];
    //        if (section == null)
    //            return;

    //        //section["项目名"].StringValue = model.ProjectName;
    //        //section["工作路径"].StringValue = model.WorkPath;

    //        //section["波特率号"].StringValue = model.BaudRate.ToString();
    //        //section["发送间隔"].StringValue = model.SendInterval.ToString();

    //        //section["地址掩码"].StringValue = "0x" + model.AddrMark.ToString("X");
    //        //section["心跳帧滤波"].StringValue = "0x" + model.HeartbeatFrame.ToString("X");
    //        //section["数据帧滤波"].StringValue = "0x" + model.DataFrame.ToString("X");
    //        //section["路由超时"].StringValue = model.RouteTimeout.ToString();
    //        //section["线程超时"].StringValue = model.ThreadTimeout.ToString();
    //        //section["心跳帧ID"].StringValue = "0x" + model.HeartbeatID.ToString("X");
    //        //section["Ping帧ID"].StringValue = "0x" + model.PingID.ToString("X");

    //        config.SaveToFile(Path, Encoding.GetEncoding("GBK"));
    //    }

    //}//class
}
