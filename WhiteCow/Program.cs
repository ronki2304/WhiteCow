using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.RuntimeMode;

namespace WhiteCow
{
    class MainClass
    {
#if !DEBUG
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new TicToc() };
                ServiceBase.Run(ServicesToRun);
            }
            else if (args[0] == "Test")
            {
                using (Test test = new Test())
                {
                    test.StartToMooh();
                }
            }

        }
#endif


    }
}
