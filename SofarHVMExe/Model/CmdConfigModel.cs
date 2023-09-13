using CanProtocol.ProtocolModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    /// <summary>
    /// 命令组配置Model
    /// </summary>
    public class CmdGrpConfigModel
    {
        public CmdGrpConfigModel() { }
        public CmdGrpConfigModel(string name, int index, bool broadcast = false)
        {
            Name = name;
            Index = index;
            IsBroadcast = false;
        }

        public int Index { get; set; } = 0; //编号
        public string Name { get; set; } //命令组名称
        public bool IsBroadcast { get; set; } = false;  //是否为广播命令

        public List<CmdConfigModel> cmdConfigModels = new List<CmdConfigModel>();
    }

    /// <summary>
    /// 命令配置model
    /// </summary>
    public class CmdConfigModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public CmdConfigModel()
        {
            CmdType = 1;
        }

        //public CmdConfigModel(int id, int cmdType, int canID, string param)
        //{
        //    Id = id;
        //    CmdType = cmdType;
        //    CanID = canID;
        //    Param = param;
        //}

        /// <summary>
        /// 命令类型
        /// 0:禁用命令 1: 可设命令 2: 固定命令
        /// </summary>
        public int CmdType { get; set; }
        public string FrameGuid { get; set; }   //Can帧的唯一id标识

        private string setValue = "";
        public string SetValue
        {
            get => setValue;
            set
            {
                if (setValue != value)
                {
                    setValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private CanFrameModel? frameModel = new CanFrameModel();
        private string guid;

        public CanFrameModel? FrameModel
        {
            get => frameModel;
            set
            {
                frameModel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// guid 唯一识别
        /// </summary>
        public string Guid
        {
            get
            {
                if (guid == string.Empty)
                {
                    guid = System.Guid.NewGuid().ToString();
                }
                return guid;
            }
            set
            {
                guid = value;
            }
        }


        /// <summary>
        /// 帧字段信息转设置值字符串
        /// </summary>
        public void FrameDatas2SetValue()
        {
            if (frameModel == null)
                return;

            string datas = "";
            CanFrameData frameData = frameModel.FrameDatas[0];
            foreach (CanFrameDataInfo dataInfo in frameData.DataInfos)
            {
                if (dataInfo.IsValidData())
                {
                    datas += $"{dataInfo.Value}, ";
                }
            }

            datas = datas.TrimEnd().TrimEnd(',');

            //首次设置值的赋值
            //if (SetValue == "")
            SetValue = datas;
        }

        /// <summary>
        /// 设置值字符串转帧字段信息
        /// </summary>
        public void SetValue2FrameDatas()
        {
            if (frameModel == null)
                return;

            string[] valueArr = setValue.Split(',');
            if (valueArr.Length <= 0)
                return;

            if (!setValue.Contains(',') && !double.TryParse(setValue, out _) && !setValue.StartsWith("0x") && !setValue.StartsWith("0X"))
            {
                //字符串拆分
                valueArr = Enumerable.Range(0, (int)Math.Ceiling(setValue.Length / 2.0))
                    .Select(i => setValue.Substring(i * 2, Math.Min(2, setValue.Length - i * 2)))
                    .ToArray();
            }

            List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>();
            CanFrameData frameData = frameModel.FrameDatas[0];
            foreach (CanFrameDataInfo dataInfo in frameData.DataInfos)
            {
                if (dataInfo.IsValidData())
                {
                    dataInfos.Add(dataInfo);
                }
            }

            //将设置值赋给字段信息
            int num = Math.Min(valueArr.Length, dataInfos.Count);
            for (int i = 0; i < num; i++)
            {
                string val = valueArr[i].Trim();
                CanFrameDataInfo dataInfo = dataInfos[i];
                dataInfo.Value = val;
            }
        }
    }//class

    /// <summary>
    /// 一条命令信息Model
    /// </summary>
    public class CommandInfoModel
    {
        public CommandInfoModel() { }

        public string Type { get; set; }
        public string CanId { get; set; }
        public string Param { get; set; }
    }//class
}
