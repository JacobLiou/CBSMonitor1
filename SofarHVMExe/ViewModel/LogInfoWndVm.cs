﻿using CanProtocol.ProtocolModel;
using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.FileOperate;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static SofarHVMExe.Utilities.Global.GlobalManager;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using MessageBox = System.Windows.MessageBox;
using SofarHVMExe.SubPubEvent.Events;
using SofarHVMExe.SubPubEvent;
using SofarHVMExe.Model;
using System.Windows.Interop;

namespace SofarHVMExe.ViewModel
{
    partial class LogInfoWndVm : ObservableObject, IHandle<string>
    {
        IEventAggregator eventAggregator;

        public LogInfoWndVm()
        {
            Init();
        }

        public LogInfoWndVm(IEventAggregator eventAggregator)
        {
            Init();
            this.eventAggregator = eventAggregator;
            eventAggregator.Subscribe(this, new string[] { "FaultAndWarning" });
        }

        public void Handle(string msg)
        {
            UpdateMsg(msg);
        }

        ~LogInfoWndVm()
        {
            eventAggregator.Unsubscribe(this, new string[] { "FaultAndWarning" });
        }

        private List<CurrentEventModel> allEventsList = new List<CurrentEventModel>();
        public List<CurrentEventModel> AllEventsList
        {
            get { return allEventsList; }
            set { allEventsList = value; OnPropertyChanged(); }
        }

        static ReaderWriterLockSlim logWriteLock = new ReaderWriterLockSlim();

        [ObservableProperty]
        private FlowDocument flowDoc = new FlowDocument();

        [RelayCommand]
        private void Clear()
        {
            var ret = MessageBox.Show("清除故障告警历史记录操作不可恢复，确定清除？", "提示", MessageBoxButton.YesNo);
            if (ret != MessageBoxResult.Yes)
                return;

            List<string> logInfos = new List<string>();
            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            string directory = exePath + @"log\FaultAndWarning\";
            if (!Directory.Exists(directory))
            {
                MessageBox.Show("未找到故障告警文件目录", "错误");
                return;
            }

            //1、清除所有log文件
            DirectoryInfo folder = new DirectoryInfo(directory);
            string todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            foreach (FileInfo logFile in folder.GetFiles("*.log"))
            {
                if (!logFile.Name.Contains(".log"))
                    continue;

                if (!logFile.Name.Contains(todayDate))
                {
                    //删除其他日期log文件
                    logFile.Delete();
                }
                else
                {
                    //清空今天的故障告警信息
                    TxtFileHelper.ClearFile(logFile.FullName);
                }
            }

            //2、清除界面信息
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                FlowDoc.Blocks.Clear();
            });
        }

        [RelayCommand]
        private void Save()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "保存到文件";
            dlg.Filter = "(*.txt)|*.txt";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string filePath = dlg.FileName;
            TextRange range = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd);
            string content = range.Text;
            TxtFileHelper.Save2File(content, filePath);
        }

        private void Init()
        {
            //Task.Run(() =>
            //{
            //1、读取log文件内容
            string[] logInfos = ReadFaultFile();

            //2、初始化log信息
            UpdateMsg(logInfos);
            //});
        }

        private string[] ReadFaultFile()
        {
            List<string> logInfos = new List<string>();
            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            string directory = exePath + @"log\FaultAndWarning\";
            if (!Directory.Exists(directory))
                return logInfos.ToArray();

            //读取所有log文件
            DirectoryInfo folder = new DirectoryInfo(directory);
            foreach (FileInfo logFile in folder.GetFiles("*.log"))
            {
                string date = $"日期：{logFile.Name}";
                logInfos.Add(date);
                string[] temp = TxtFileHelper.ReadFileLines(logFile.FullName);
                logInfos.AddRange(temp);
            }

            return logInfos.ToArray();
        }
        private string[] ReadFaultFile2()
        {
            List<string> logInfos = new List<string>();
            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            string directory = exePath + @"log\FaultAndWarning\";
            if (!Directory.Exists(directory))
                return logInfos.ToArray();

            //读取所有log文件
            DirectoryInfo folder = new DirectoryInfo(directory);
            foreach (FileInfo logFile in folder.GetFiles("*.log"))
            {
                string date = $"日期：{logFile.Name}";
                logInfos.Add(date);
                string[] temp = TxtFileHelper.ReadFileLines(logFile.FullName);
                logInfos.AddRange(temp);
            }

            return logInfos.ToArray();
        }
        private void UpdateMsg(string[] msgs)
        {
            if (msgs.Length == 0)
                return;

            Paragraph pg = new Paragraph();
            //for (int i=0; i< msgs.Length; i++)
            for (int i = msgs.Length - 1; i > 0; i--)
            {
                string msg = msgs[i];
                Color color = Colors.Gray;
                if (i == msgs.Length - 1 && msgs[0].StartsWith("日期"))
                {
                    msg = msg.Replace("故障告警日志-", "").Replace(".log", "");
                    msg = $"******** {msgs[0]} ********";
                    color = Colors.Orange;
                }
                else if (msg.Contains("[故障]"))
                {
                    color = Colors.Red;
                }
                else if (msg.Contains("[告警]"))
                {
                    color = Colors.Blue;
                }

                msg += "\r\n";
                AddMsg(pg, msg, color);
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                FlowDoc.Blocks.Add(pg);
            });
        }

        private void UpdateMsg(string msg)
        {
            if (msg.Length == 0)
                return;
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                Paragraph pg = new Paragraph();
                Color color = Colors.Gray;

                if (msg.Contains("[故障]"))
                {
                    color = Colors.Red;
                }
                else if (msg.Contains("[告警]"))
                {
                    color = Colors.Blue;
                }

                msg += "\r\n";
                AddMsg(pg, msg, color);
                if (FlowDoc.Blocks.FirstBlock == null)
                {
                    FlowDoc.Blocks.Add(pg);
                }
                else
                {
                    FlowDoc.Blocks.InsertBefore(FlowDoc.Blocks.FirstBlock, pg);
                }
            });
        }

        private void AddMsg(Paragraph pg, string msg, Color color)
        {
            Run text = new Run(msg);
            text.Foreground = new SolidColorBrush(color);
            pg.Inlines.Add(text);
        }

        //public void Handle(LogInfiEvent message)
        //{
        //    //CurrentEventList = message.CurrentEventList;
        //}
    }//class
}
