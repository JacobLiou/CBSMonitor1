using CanProtocol;
using CanProtocol.ProtocolModel;
using SofarHVMExe.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.Xml;
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
    /// </summary>
    public partial class MultyFrameDataControl : UserControl
    {
        public MultyFrameDataControl()
        {
            InitializeComponent();
        }


        private int currPackIndex = 0; //当前操作的包序号
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

        public CanFrameDataInfo SelectDataInfo { get; set; }

        #region 依赖属性
        public bool AllowAddOrDel
        {
            get { return (bool)GetValue(AllowAddOrDelProperty); }
            set { SetValue(AllowAddOrDelProperty, value); }
        }
        public static readonly DependencyProperty AllowAddOrDelProperty =
            DependencyProperty.Register("AllowAddOrDel", typeof(bool), typeof(MultyFrameDataControl), new PropertyMetadata(true));

        public ObservableCollection<CanFrameDataInfo> DataSource
        {
            get { return (ObservableCollection<CanFrameDataInfo>)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(ObservableCollection<CanFrameDataInfo>), typeof(MultyFrameDataControl),
                new PropertyMetadata(new ObservableCollection<CanFrameDataInfo>()));
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

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            //CanFrameData? frameData = this.DataContext as CanFrameData;
            //if (frameData == null)
            //    return;

            //获取当前包，在当前包增加字段信息，超出个数则增加一个新的中间帧，在新包中增加
            ObservableCollection<CanFrameDataInfo> dataInfos = DataSource;
            CanFrameDataInfo crcInfo = dataInfos.Last();
            dataInfos.Remove(crcInfo);

            CanFrameDataInfo newInfo = new CanFrameDataInfo();
            dataInfos.Add(newInfo);
            dataInfos.Add(crcInfo);

            ///更新个数
            CanFrameDataInfo numInfo = dataInfos[2];
            numInfo.Value = (dataInfos.Count - 4).ToString();

            ///更新CRC校验值
            UpdateCrc(dataInfos);

            dataGrid.SelectedItem = newInfo;

            //List<ObservableCollection<CanFrameDataInfo>> multyDataInfos = SplitPackage(dataInfos);
            //ObservableCollection<CanFrameDataInfo> currDataInfos = multyDataInfos[currPackIndex];

            //string newType = "";
            //CanFrameDataInfo newInfo = new CanFrameDataInfo();

            //if (HasOutOfRange(currDataInfos, out newType))
            //{
            //    //MessageBox.Show("长度最大8个字节，无法增加！", "提示");
            //    ///超出当前包字段个数，增加一个新包-中间帧
            //    {
            //        CanFrameData mid = new CanFrameData();
            //        mid.AddMidFrameDefData();
            //        newInfo.Type = newType;
            //        mid.DataInfos.Add(newInfo);
            //        mid.SetPackageIndex(multyDataInfos.Count-1);

            //        ObservableCollection<CanFrameDataInfo> edDataInfos = multyDataInfos.Last();
            //        multyDataInfos.Remove(edDataInfos);
            //        multyDataInfos.Add(mid.DataInfos);
            //        multyDataInfos.Add(edDataInfos);
            //    }

            //    ///更新个数
            //    ObservableCollection<CanFrameDataInfo> stDataInfos = multyDataInfos.First();
            //    CanFrameData.SetFrameNum(stDataInfos, multyDataInfos.Count);

            //    currPackIndex++;
            //}
            //else
            //{
            //    newInfo.Type = newType;
            //    currDataInfos.Add(newInfo);
            //}


        }//func

        private void InsertNewButton_Click(object sender, RoutedEventArgs e)
        {
            //获取当前包，在当前包增加字段信息，超出个数则增加一个新的中间帧，在新包中增加
            ObservableCollection<CanFrameDataInfo> dataInfos = DataSource;
            CanFrameDataInfo crcInfo = dataInfos.Last();
            dataInfos.Remove(crcInfo);

            CanFrameDataInfo newInfo = new CanFrameDataInfo();
            int selectIndex = dataGrid.SelectedIndex;
            if (selectIndex == 0)//如果没有选中行就默认添加，添加到最后一行
            {
                dataInfos.Add(newInfo);
            }
            else//有选中行，把获取过来的选中索引基础上+1
            {
                dataInfos.Insert(selectIndex + 1, newInfo);
            }
            dataInfos.Add(crcInfo);

            ///更新个数
            CanFrameDataInfo numInfo = dataInfos[2];
            numInfo.Value = (dataInfos.Count - 4).ToString();

            ///更新CRC校验值
            UpdateCrc(dataInfos);

            dataGrid.SelectedItem = newInfo;

        }//func

        private void DeleteSelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectDataInfo == null)
            {
                MessageBox.Show("请选择一条数据！", "提示");
                return;
            }

            //检查是否为点表起始地址和个数字段
            {
                string name = SelectDataInfo.Name;
                if (name == "点表起始地址" ||
                    name == "个数" ||
                    name == "包序号" ||
                    name == "CRC校验"
                    )
                {
                    MessageBox.Show("必要字段不允许删除！", "提示");
                    return;
                }
            }

            ObservableCollection<CanFrameDataInfo> dataInfos = DataSource;

            //删除选择的字段信息
            int index =  DataSource.IndexOf(SelectDataInfo);
            DataSource.RemoveAt(index);
            CanFrameDataInfo dataInfo = DataSource[index - 1];
            dataGrid.SelectedItem = dataInfo;

            ///更新个数
            CanFrameDataInfo numInfo = dataInfos[2];
            numInfo.Value = (dataInfos.Count - 4).ToString();

            ///更新CRC校验值
            UpdateCrc(dataInfos);
        }//func


        /// <summary>
        /// 表格单元格编辑完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            //if (!(e.EditingElement is TextBox tb))
            //    return;

            //CanFrameData? frameData = this.DataContext as CanFrameData;
            //string newValue = tb.Text;

            //更新CRC校验值
            string colName = e.Column.Header.ToString();
            if (colName.Contains("值") ||
                colName.Contains("类型"))
            {
                ObservableCollection<CanFrameDataInfo> dataInfos = DataSource;
                UpdateCrc(dataInfos);
            }
        }

        #endregion

        private string GetPackageIndex(ObservableCollection<CanFrameDataInfo> dataInfos)
        {
            foreach (CanFrameDataInfo info in dataInfos)
            {
                if (info.Name == "包序号")
                {
                    return info.Value;
                }
            }

            return "-1";
        }
        private void UpdatePackageIndex()
        {
            //更新中间帧的包序号
            int count = DataSource.Count;
            for (int i = 1; i < count - 1; i++)
            {
                //CanFrameData frameData= DataSource[i];
                //frameData.SetPackageIndex(i);
            }
        }

        private void UpdateCrc(ObservableCollection<CanFrameDataInfo> dataInfos)
        {
            ushort crcVal = CalcCrcVal(dataInfos);
            CanFrameData.SetCrcVal(dataInfos, crcVal);
        }

        private ushort CalcCrcVal(ObservableCollection<CanFrameDataInfo> dataInfos)
        {
            List<byte> dataList = new List<byte>();
            ObservableCollection<CanFrameDataInfo> tmpDataInfos = new ObservableCollection<CanFrameDataInfo>(dataInfos);
            tmpDataInfos.RemoveAt(0); //包序号0
            tmpDataInfos.RemoveAt(0); //起始地址
            tmpDataInfos.RemoveAt(0); //个数
            tmpDataInfos.Remove(tmpDataInfos.Last()); //crc校验

            //没有数据了
            if (tmpDataInfos.Count == 0)
                return 0;

            //字段解析到数据
            List<byte> bytes = ProtocolHelper.AnalyseDataInfo(tmpDataInfos);

            //计算crc
            CRCHelper helper = new CRCHelper();
            return CRCHelper.ComputeCrc16(bytes.ToArray(), bytes.Count);
        }

        private void UpdateCrc(List<ObservableCollection<CanFrameDataInfo>> multyDataInfos)
        {
            ObservableCollection<CanFrameDataInfo> edDataInfos = multyDataInfos.Last();
            ushort crcVal = CalcCrcVal(multyDataInfos);
            CanFrameData.SetCrcVal(edDataInfos, crcVal);
        }
        private ushort CalcCrcVal(List<ObservableCollection<CanFrameDataInfo>> multyDataInfos)
        {
            List<byte> dataList = new List<byte>();
            List<CanFrameData> frameDataList = new List<CanFrameData>();

            //收集包数据
            foreach (var dataInfos in multyDataInfos)
            {
                CanFrameData frameData = new CanFrameData();
                frameData.DataInfos = dataInfos;
                frameDataList.Add(frameData);
            }

            //字段解析到数据
            ProtocolHelper.AnalyseFrameData(frameDataList);

            //收集包数据
            foreach (CanFrameData frameData in frameDataList)
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
        }

        /// <summary>
        /// 将一组多包字段信息拆分成多组多包信息
        /// </summary>
        /// <param name="multyInfos"></param>
        /// <returns></returns>
        private List<ObservableCollection<CanFrameDataInfo>> SplitPackage(ObservableCollection<CanFrameDataInfo> multyInfos)
        {
            List<ObservableCollection<CanFrameDataInfo>> frameDataList = new List<ObservableCollection<CanFrameDataInfo>>();
            ObservableCollection<CanFrameDataInfo> frameDatas = null;
            foreach (CanFrameDataInfo info in multyInfos)
            {
                if (info.Name == "包序号")
                {
                    frameDatas = new ObservableCollection<CanFrameDataInfo>();
                    frameDataList.Add(frameDatas);
                }

                frameDatas?.Add(info);
            }

            return frameDataList;
        }//func

        private ObservableCollection<CanFrameDataInfo> MergePackage(List<ObservableCollection<CanFrameDataInfo>> dataInfoList)
        {
            List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>();
            foreach (var d in dataInfoList)
            {
                dataInfos.AddRange(d);
            }

            return new ObservableCollection<CanFrameDataInfo>(dataInfos.ToArray());
        }

        private bool HasOutOfRange(ObservableCollection<CanFrameDataInfo> dataInfos, out string newType)
        {
            newType = "U16";
            //判断数据是否超出8个字节长度
            int count = 0;
            foreach (CanFrameDataInfo info in dataInfos)
            {
                if (info.Type.Contains("8"))
                {
                    count += 1;
                }
                else if (info.Type.Contains("16"))
                {
                    count += 2;
                }
                else if (info.Type.Contains("32"))
                {
                    count += 4;
                }
            }

            if (count == 8)
            {
                return true;
            }
            else if (count == 7)
            {
                newType = "U8";
            }

            return false;
        }//func

    }//class
}
