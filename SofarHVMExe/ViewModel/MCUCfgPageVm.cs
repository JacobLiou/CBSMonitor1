using Newtonsoft.Json.Linq;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Input;

namespace SofarHVMExe.ViewModel
{
    class MCUCfgPageVm : ViewModelBase
    {
        public MCUCfgPageVm()
        {
            Init();
            SaveCommand = new SimpleCommand(Save);
        }


        private FileConfigModel? fileCfgModel = null;
        private List<McuConfigModel> mcuModels = null;

        public McuConfigModel Model0
        {
            get => GetModel(0);
            set { SetModel(value, 0); }
        }

        public McuConfigModel Model1
        {
            get => GetModel(1);
            set { SetModel(value, 1); }
        }

        public McuConfigModel Model2
        {
            get => GetModel(2);
            set { SetModel(value, 2); }
        }

        public McuConfigModel Model3
        {
            get => GetModel(3);
            set { SetModel(value, 3); }
        }

        public McuConfigModel Model4
        {
            get => GetModel(4);
            set { SetModel(value, 4); }
        }

        public McuConfigModel Model5
        {
            get => GetModel(5);
            set { SetModel(value, 5); }
        }

        public McuConfigModel Model6
        {
            get => GetModel(6);
            set { SetModel(value, 6); }
        }

        public McuConfigModel Model7
        {
            get => GetModel(7);
            set { SetModel(value, 7); }
        }

        public McuConfigModel Model8
        {
            get => GetModel(8);
            set { SetModel(value, 8); }
        }

        public McuConfigModel Model9
        {
            get => GetModel(9);
            set { SetModel(value, 9); }
        }

        public McuConfigModel Model10
        {
            get => GetModel(10);
            set { SetModel(value, 10); }
        }

        public McuConfigModel Model11
        {
            get => GetModel(11);
            set { SetModel(value, 11); }
        }

        public McuConfigModel Model12
        {
            get => GetModel(12);
            set { SetModel(value, 12); }
        }

        public McuConfigModel Model13
        {
            get => GetModel(13);
            set { SetModel(value, 13); }
        }

        public McuConfigModel Model14
        {
            get => GetModel(14);
            set { SetModel(value, 14); }
        }

        public McuConfigModel Model15
        {
            get => GetModel(15);
            set { SetModel(value, 15); }
        }



        public ICommand SaveCommand { get; set; }


        private void Init()
        {
            ////从配置文件读取mcu配置数据
            //configModel = new McuConfigModel();
            //List<McuConfigModel> infoModels = configModel.infoModels;

            ////假数据
            //{
            //    McuConfigModel model = new McuConfigModel(true, "MCU0", "1(hex)", 0x333, 0x444, 2, 0xFFFFEEEE);
            //    infoModels.Add(model);
            //}
        }

        private McuConfigModel GetModel(int num)
        {
            if (mcuModels.Count > num)
            {
                return mcuModels[num];
            }

            return null;
        }

        private void SetModel(McuConfigModel model, int num)
        {
            if (mcuModels.Count > num)
            {
                mcuModels[num] = model;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 更新model数据
        /// </summary>
        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel == null)
                return;

            mcuModels = fileCfgModel.McuModels;
        }

        private void Save(object o)
        {
            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }

    }//class
}
