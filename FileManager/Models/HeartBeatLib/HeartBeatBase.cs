using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Static;

namespace FileManager.Models.HeartBeatLib
{
    public abstract class HeartBeatBase
    {
        public int StartInterval { get; set; } = 200;

        public int Interval { get; set; } = 3000;

        private Thread HeartBeatThread { get; set; }

        public ThreadState HeartBeatState
        {
            get
            {
                return HeartBeatThread.ThreadState;
            }
        }


        public virtual void HeartBeat()
        {
            Thread.Sleep(StartInterval);
            while (true)
            {
                HeartBeatUnit();
                Thread.Sleep(Interval);
            }
        }

        public abstract void HeartBeatUnit();

        public void StartHeartBeat()
        {
            HeartBeatThread = new Thread(HeartBeat);
            HeartBeatThread.IsBackground = true;
            HeartBeatThread.Start();
        }

        public void StopHeartBeat()
        {
            if ((HeartBeatThread.ThreadState & ThreadState.WaitSleepJoin) != 0 ||
                HeartBeatThread.ThreadState == ThreadState.Running)
            {
                HeartBeatThread.Abort();
                HeartBeatThread.Join();
            }
        }
    }
}
