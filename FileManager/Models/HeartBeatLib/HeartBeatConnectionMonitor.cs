using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Events;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Static;


namespace FileManager.Models
{
    public class HeartBeatConnectionStatusRecord
    {
        public bool Status { get; set; }
        public DateTime TimeStamp { get; set; }

        public void CopyFrom(HeartBeatConnectionStatusRecord src)
        {
            this.Status = src.Status;
            this.TimeStamp = src.TimeStamp;
        }
    }


    public class HeartBeatConnectionMonitor : HeartBeatBase
    {

        public event UpdateUIEventHandler HeartBeatUnitCallback = null;

        public List<HeartBeatConnectionStatusRecord> StatusRecords = new List<HeartBeatConnectionStatusRecord>();

        public DateTime StartTime = DateTime.Now;

        public int DifSeconds
        {
            get
            {
                return (int)(StatusRecords.Last().TimeStamp - StartTime).TotalSeconds;
            }
        }

        public void Init()
        {
            StatusRecords.Clear();
            this.Interval = Config.Instance.ConnectionMonitorRecordInterval;
            StartTime = DateTime.Now;
        }

        private void AddRecord(bool status)
        {
            if (StatusRecords.Count < Config.Instance.ConnectionMonitorRecordCount)
            {
                StatusRecords.Add(new HeartBeatConnectionStatusRecord
                {
                    Status = status,
                    TimeStamp = DateTime.Now
                });
            }
            else
            {
                StartTime = StatusRecords[0].TimeStamp;
                for (int i = 1; i < StatusRecords.Count; ++i)
                {
                    StatusRecords[i - 1].CopyFrom(StatusRecords[i]);
                }
                StatusRecords[StatusRecords.Count - 1].Status = status;
                StatusRecords[StatusRecords.Count - 1].TimeStamp = DateTime.Now;
            }
        }

        public override void HeartBeat()
        {
            try
            {
                Thread.Sleep(StartInterval);
                while (true)
                {
                    HeartBeatUnit();
                    Thread.Sleep(Interval);
                }
            }
            catch(ThreadAbortException ex)
            {
                /// https://www.cnblogs.com/jackson0714/p/AbortThread.html
                LoggerStatic.Log("HeartBeatConnection aborted : " + ex.Message, LogLevel.Info);
            }
            finally
            {
                StatusRecords.Clear();
                HeartBeatUnitCallback(this, EventArgs.Empty);
            }
        }



        public override void HeartBeatUnit()
        {
            try
            {
                SocketClient client = SocketFactory.Instance.GenerateConnectedSocketClient(1, Interval);
                
                client.Close();
                AddRecord(true);
            }
            catch (Exception)
            {
                //FailCallback();
                AddRecord(false);
            }
            finally
            {
                HeartBeatUnitCallback(this, EventArgs.Empty);
            }
            //throw new NotImplementedException();
        }
    }
}
