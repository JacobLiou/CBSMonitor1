using System;
using System.Collections.Generic;
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
using ScottPlot;
using SofarHVMExe.ViewModel;

namespace SofarHVMExe.View
{
    /// <summary>
    /// Interaction logic for OscilloscopePage.xaml
    /// </summary>
    public partial class OscilloscopePage : UserControl
    {
        public OscilloscopePageVM? OscilloscopePageVm { get; set; }

        public OscilloscopePage()
        {
            InitializeComponent();
            
        }

        private void OscilloscopeTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OscilloscopePlotPanel.Visibility = OscilloTabItem.IsSelected ? Visibility.Visible : Visibility.Hidden;
            FaultWavePlotPanel.Visibility = FaultWaveTabItem.IsSelected ? Visibility.Visible : Visibility.Hidden;
        }


        private void OscilloscopePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            OscilloscopePageVm = this.DataContext as OscilloscopePageVM;
            SetupPlotPanels();
            OscilloscopePageVm.OnPageLoadedJobs();

        }

        private void SetupPlotPanels()
        {
            //var dataContext = this.DataContext as OscilloscopePageVM;
            OscilloscopePlotContainer.Children.Add(OscilloscopePageVm.OscilloscopeVm.PlotCtrl);
            FaultWavePlotContainer.Children.Add(OscilloscopePageVm.FaultWaveRecordVm.PlotCtrl);
        }

        private void OscilloscopePage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            OscilloscopePlotContainer.Children.Clear();
            FaultWavePlotContainer.Children.Clear();

            OscilloscopePageVm.OnPagesUnloadedJobs();
        }


        private void CoffPathText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
           CoffPathText.ScrollToHorizontalOffset(5000);
        }
    }
}
