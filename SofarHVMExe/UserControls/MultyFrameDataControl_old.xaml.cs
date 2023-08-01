using CanProtocol;
using CanProtocol.ProtocolModel;
using SofarHVMExe.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SofarHVMExe.UserControls
{
    /// <summary>
    /// MultyFrameDataControl.xaml 的交互逻辑
    /// 多包分段显示控件（由于用户不喜欢，但是我觉得不错，暂时保留）
    /// </summary>
    public partial class MultyFrameDataControl_old : UserControl
    {
        public MultyFrameDataControl_old()
        {
            InitializeComponent();
        }

        private int addFrameNum = 1;
        public string AddFrameNum
        {
            get => addFrameNum.ToString();
            set
            {
                int v;
                if (int.TryParse(value, out v) && v < 100)
                {
                    addFrameNum = v;
                }
            }
        }

        #region 依赖属性
        public ObservableCollection<CanFrameData> DataSource
        {
            get { return (ObservableCollection<CanFrameData>)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(ObservableCollection<CanFrameData>), typeof(MultyFrameDataControl),
                new PropertyMetadata(new ObservableCollection<CanFrameData>()));

#endregion


#region 控件事件处理

        /// <summary>
        /// 表格加载一行数据事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //增加序号列
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void AddDataButton_Click(object sender, RoutedEventArgs e)
        {
            //添加一个/多个中间帧
            for (int i = 0; i < addFrameNum; i++) 
            {
                CanFrameData end = DataSource.Last();
                DataSource.Remove(end);
                CanFrameData newMid = new CanFrameData();
                newMid.AddMidFrameDefData();
                int packageIndex = DataSource.Count;
                newMid.SetPackageIndex(packageIndex);
                DataSource.Add(newMid);
                DataSource.Add(end);

                ///更新个数
                CanFrameData st = DataSource.First();
                st.SetFrameNum(DataSource.Count);

                ///更新CRC校验值
                UpdateCrc();
            }
        }

        private void DeleteFrameData(object o)
        {
            CanFrameData? frameData = o as CanFrameData;
            if (frameData != null)
            {
                string packageIndex = frameData.GetPackageIndex();
                if (packageIndex != "-1" && 
                    packageIndex != "0" &&
                    packageIndex != "0xff" 
                    )
                {
                    DataSource.Remove(frameData);

                    ///更新个数
                    CanFrameData st = DataSource.First();
                    st.SetFrameNum(DataSource.Count);

                    ///更新包序号
                    UpdatePackageIndex();

                    ///更新CRC校验值
                    UpdateCrc();
                }
            }
        }//func

        private void ModifyDataInfo(object o)
        {
            UpdateCrc();
        }
        #endregion

        private void UpdatePackageIndex()
        {
            //更新中间帧的包序号
            int count = DataSource.Count;
            for (int i = 1; i < count - 1; i++)
            {
                CanFrameData frameData= DataSource[i];
                frameData.SetPackageIndex(i);
            }
        }
        private void UpdateCrc()
        {
            CanFrameData end = DataSource.Last();
            ushort crcVal = CalcuCrcVal();
            end.SetCrcVal(crcVal);
        }

        private ushort CalcuCrcVal()
        {
            List<byte> dataList = new List<byte>();

            //字段解析到数据
            ProtocolHelper.AnalyseFrameData(DataSource.ToList());

            //收集包数据
            foreach (CanFrameData frameData in DataSource)
            {
                byte[] dataArr = frameData.Data;
                string packageIndex = frameData.GetPackageIndex();
                if (packageIndex == "0") //起始帧
                {
                    byte[] validData = dataArr.Skip(4).ToArray();
                    dataList.AddRange(validData);
                }
                else if (packageIndex == "0xff") //结束帧
                {
                    byte[] validData = dataArr.Skip(1).Take(frameData.DataNum).ToArray();
                    dataList.AddRange(validData.ToArray());
                }
                else //中间帧
                {
                    byte[] validData = dataArr.Skip(1).ToArray(); 
                    dataList.AddRange(validData.ToArray());
                }
            }

            //计算crc
            //CRCHelper helper = new CRCHelper();
            return CRCHelper.ComputeCrc16(dataList.ToArray(), dataList.Count);

        }//func
    }//class
}
