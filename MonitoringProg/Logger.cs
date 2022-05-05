using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringProg
{
    class Logger
    {
        public static void WriteLogs(string message)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs.txt", true))
                {
                    sw.WriteLine(String.Format("{0,-23} {1}", DateTime.Now.ToString() + ":", message));
                }
            }
            catch
            {

            }

        }

        public static void WriteErrors(string message)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\error_log.txt", true))
                {
                    sw.WriteLine(String.Format("{0,-23} {1}", DateTime.Now.ToString() + ":", message));
                }
            }
            catch
            {

            }
        }
    }
}
