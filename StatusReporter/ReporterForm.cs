using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using SocketLib;
using SocketLib.Enums;

namespace StatusReporter
{
    public partial class ReporterForm : Form
    {
        private SystemInfo systemInfo = new SystemInfo();

        public ReporterForm()
        {
            InitializeComponent();
        }

        private void ReporterForm_Load(object sender, EventArgs e)
        {
            if (!ReporterConfig.CheckConfig())
            {
                ReporterConfig.CreateConfig();
            }
            ReporterConfig.LoadConfig();
            Task.Run(() => 
            {
                while (true)
                {
                    ReportStatus();
                    Thread.Sleep(ReporterConfig.Interval);
                }
            });
        }


        private void ReportStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("<{0}>, ", ReporterConfig.ClientName.PadRight(6)));
            //int cpu_percent = Convert.ToInt32(getCPUCounter());
            //sb.Append("CPU: " + cpu_percent.ToString() + "%, ");
            sb.Append(getStatusComsol());
            /// RichTextBox
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            this.BeginInvoke(new Action(() => 
            {
                /// 将光标位置设置到当前内容的末尾
                this.richTextBox1.SelectionStart = this.richTextBox1.Text.Length;
                this.richTextBox1.SelectionLength = 0;
                /// 滚动到光标位置
                this.richTextBox1.ScrollToCaret();

                /// append text
                this.richTextBox1.SelectionColor = Color.Blue;
                this.richTextBox1.AppendText(time + " ");
                this.richTextBox1.SelectionColor = this.richTextBox1.ForeColor;
                this.richTextBox1.AppendText(sb.ToString() + "\n");

            }));
            try
            {
                SocketClient client = new SocketClient(ReporterConfig.ServerIP, ReporterConfig.ServerPort);
                client.Connect();
                client.SendBytes(SocketPacketFlag.StatusReport, sb.ToString());
                client.Close();
            }
            catch (Exception) {; }
        }

        private string getStatusComsol()
        {
            
            //float cpu = 0;
            float ram = 0;
            foreach(Process ps in Process.GetProcessesByName("comsol"))
            {
                //var ramCounter = new PerformanceCounter("Process", "Working Set", ps.ProcessName);
                //ram += ramCounter.NextValue();
                ram += ps.WorkingSet64;
                //var cpuCounter = new PerformanceCounter("Process", "% Processor Time", ps.ProcessName);
                //cpu += cpuCounter.NextValue();
            }
            
            string cpu_str = ((int)systemInfo.CpuLoad).ToString().PadLeft(3) + "%";
            string ram_str = string.Format("{0:F2}", ram / (1 << 30)).PadLeft(7) + "G";
            //string ram_str = string.Format("{0:F2}", ram).PadLeft(7) + "G";
            return string.Format("CPU:{0}, COMSOL RAM:{1}", cpu_str, ram_str);
            //string cpu_str = ((int)systemInfo.CpuLoad).ToString().PadLeft(3) + "%";
            //return string.Format("CPU:{0}", cpu_str);
        }


        /// <summary>
        /// 获取CPU信息
        /// </summary>
        /// <returns></returns>
        private object getCPUCounter()
         {
             PerformanceCounter cpuCounter = new PerformanceCounter();
             cpuCounter.CategoryName = "Processor";
             cpuCounter.CounterName = "% Processor Time";
             cpuCounter.InstanceName = "_Total";
             // will always start at 0 
             dynamic firstValue = cpuCounter.NextValue();
             System.Threading.Thread.Sleep(1000);
             // now matches task manager reading 
             dynamic secondValue = cpuCounter.NextValue();
             return secondValue;
         }
}
}
