using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    [JsonObject(MemberSerialization.OptOut)]   //排除不需要进行Json序列化的
    public class FileConfigModel
    {
        public List<McuConfigModel> McuModels { get; set; } = new List<McuConfigModel>();

        public List<MonitorConfigModel> MonModels { get; set; } = new List<MonitorConfigModel>();

        public PrjConfigModel PrjModel { get; set; } = new PrjConfigModel();

        public FrameConfigModel FrameModel { get; set; } = new FrameConfigModel();
        
        public List<EventGroupModel> EventModels { get; set; } = new List<EventGroupModel>();

        public List<CmdGrpConfigModel> CmdModels { get; set; } = new List<CmdGrpConfigModel>();

        public List<MemoryModel> MemModels { get; set; } = new List<MemoryModel>(); //用于map操作

        public OscilloscopeModel OscilloscopeModel { get; set; } = new OscilloscopeModel(); 

        /// <summary>
        /// 初始化model
        /// 注意：此步不能在构造中做，不然会影响Json反序列化，造成反序列化出来的集合元素变多
        /// </summary>
        public void InitModels()
        {
            //InitMcuModels();
            //InitMonModels();

            InitEventModels();
            InitCmdModels();
        }

        /// <summary>
        /// 初始化MCU配置model
        /// </summary>
        private void InitMcuModels()
        {
            for (int i = 0; i < 16; i++)
            {
                McuConfigModel model = new McuConfigModel();
                model.McuName = $"MCU{i.ToString()}";
                McuModels.Add(model);
            }
        }

        /// <summary>
        /// 初始化主参数配置model
        /// </summary>
        private void InitMonModels()
        {
            for (int i = 0; i < 2; i++)
            {
                string name = $"主参数{(i + 1).ToString()}";
                MonitorConfigModel model = new MonitorConfigModel(name, "", "", 0, 0, 0, 0);
                MonModels.Add(model);
            }
        }

        /// <summary>
        /// 初始化事件配置model
        /// </summary>
        private void InitEventModels()
        {
            //添加10个事件组
            for (int i = 0; i < 50; i++)
            {
                EventGroupModel eventGrpModel = new EventGroupModel(i);
                EventModels.Add(eventGrpModel);
            }
        }

        /// <summary>
        /// 初始化命令配置model
        /// </summary>
        private void InitCmdModels()
        {
            for (int i = 0; i < 10; i++)
            {
                CmdGrpConfigModel cmdGrpConfigModel = new CmdGrpConfigModel($"命令组{i}", i);
                CmdModels.Add(cmdGrpConfigModel);
            }
            CmdModels.Add(new CmdGrpConfigModel("广播命令", 11, true));
            CmdModels.AddRange(new List<CmdGrpConfigModel>()
            {
                new CmdGrpConfigModel("启动参数组", 12),
                new CmdGrpConfigModel("电压参数组", 13),
                new CmdGrpConfigModel("频率参数组", 14),
                new CmdGrpConfigModel("DCI保护参数组", 15),
                new CmdGrpConfigModel("远程及有功参数组", 16),
                new CmdGrpConfigModel("频率有功参数组", 17),
                new CmdGrpConfigModel("无功参数组", 18),
                new CmdGrpConfigModel("电压穿越参数组", 19),
                new CmdGrpConfigModel("ISO孤岛参数", 20)
            });
        }

        /// <summary>
        /// 更新命令组索引号
        /// </summary>
        /// 旧的json配置未更新，手动调用此方法更新，一次就可以
        public void UpdateCmdIndex()
        {
            //增加索引
            int index = 0;
            CmdModels.ForEach((model) => model.Index = index++);

            ////最后一个是广播
            //CmdModels.Last().IsBroadcast= true;

            //第11个是广播
            for (int i = 0; i < CmdModels.Count; i++)
            {
                CmdModels[i].IsBroadcast = i == 10;
            }
        }


    }//class
}
