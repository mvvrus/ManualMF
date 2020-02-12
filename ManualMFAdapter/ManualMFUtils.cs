using System;
using System.IO;
namespace ManualMF
{
    static class ManualMFUtils
    {
        const String LOGPATH = @"C:\ManualMF\";
        const String LOGFILE = @"debug.log";
        internal static void DebugLog(string msg)
        {
            using (StreamWriter f = File.AppendText(LOGPATH + LOGFILE))
            {
                f.WriteLine("{0:s} {1}", DateTime.Now, msg);
                f.Close();
            }
        }
    }
}