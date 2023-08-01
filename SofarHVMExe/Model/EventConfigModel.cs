using SofarHVMExe.Utilities;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    /// <summary>
    /// 事件组类
    /// </summary>
    public class EventGroupModel
    {
        public EventGroupModel() { }

        public EventGroupModel(int group, bool enable = false)
        {
            Group = group;
            Enable = enable;
        }

        public int Group { get; set; } = 0; //组号
        public bool Enable { get; set; } = false; //事件使能
        public string FrameGuid { get; set; } = ""; //Can帧Guid
        public int MemberIndex { get; set; } = -1;

        public List<EventInfoModel> InfoModels = new List<EventInfoModel>();

        public void Init()
        {
            for (int i = 0; i < 32; i++)
            {
                EventInfoModel model = new EventInfoModel(i);
                InfoModels.Add(model);
            }
        }
    }//class


    /// <summary>
    /// 单个事件类
    /// </summary>
    public class EventInfoModel : ViewModelBase
    {
        public EventInfoModel() { }

        public EventInfoModel(int bit)
        {
            Bit = bit;
        }

        public EventInfoModel(int bit, EventType type, string name, string mark)
        {
            Bit = bit;
            Type = type;
            Name = name;
            Mark = mark;
        }

        public bool enable = false;
        public bool Enable
        {
            get => enable;
            set
            {
                if (enable != value)
                {
                    enable = value;
                    OnPropertyChanged();
                }
            }
        }
        public int Bit { get; set; } = 0;
        public EventType Type { get; set; } = EventType.None;
        public string Name { get; set; } = "";
        public string Mark { get; set; } = "";
        public static List<EventInfoModel> PreviEvent { get; set; } = new List<EventInfoModel>();
    }

    /// <summary>
    /// 事件类型枚举
    /// </summary>
    public enum EventType
    {
        None = 0,
        Status,     //状态
        Exception,  //异常
        Fault       //故障
    }
}
