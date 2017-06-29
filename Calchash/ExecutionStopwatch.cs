using System;
using System.Runtime.InteropServices;

namespace Calchash
{
    class ExecutionStopwatch
    {
        [DllImport("kernel32.dll")]
        private static extern long GetThreadTimes(
            IntPtr threadHandle, 
            out long createionTime,
            out long exitTime, 
            out long kernelTime, 
            out long userTime);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        private long endTimeStamp;
        private long startTimeStamp;

        public void Start()
        {
            var timestamp = GetThreadTimes();
            startTimeStamp = timestamp;
        }

        public void Stop()
        {
            var timestamp = GetThreadTimes();
            endTimeStamp = timestamp;
        }

        public long Elapsed
        {
            get
            {
                var elapsed = (endTimeStamp - startTimeStamp) / 10000;
                return elapsed;
            }
        }

        private long GetThreadTimes()
        {
            IntPtr threadHandle = GetCurrentThread();

            long notIntersting;
            long kernelTime, userTime;

            long retcode = GetThreadTimes
            (threadHandle, out notIntersting,
                out notIntersting, out kernelTime, out userTime);

            bool success = Convert.ToBoolean(retcode);
            if (!success)
            {
                throw new Exception($"failed to get timestamp. error code: {retcode}");
            }

            long result = kernelTime + userTime;
            return result;
        }
    }
}