using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Newtonsoft.Json;
using WhiteCow.Broker;

using System.Collections.Generic;
using WhiteCow.Entities;
using WhiteCow.RuntimeMode;
using WhiteCow.Interface;


//miaou
namespace WhiteCow
{
    public class TicToc : ServiceBase
    {
        
        IRuntimeMode engine;
#if DEBUG
        public static void Main(String[] args)
        {
            if (args.Length == 0)
            {
                (new TicToc()).OnStart(new string[1]);
                ServiceBase.Run(new TicToc());
            }
            else if (args[0] == "Test")
            {
                using (Test test = new Test())
                {
                    test.StartToMooh();
                }
            }

            Console.ReadLine();

        }
#endif
        
        protected override void OnStart(string[] args)
        {
            
            WhiteCowMode servicemode = (WhiteCowMode)Enum.Parse(typeof(WhiteCowMode), ConfigurationManager.AppSettings["Mode"]);


            switch (servicemode)
            {
                case WhiteCowMode.History:
                    engine = new History();

                    break;
                case WhiteCowMode.Runtime:
                    engine = new Trading();

                    break;
                case WhiteCowMode.Test:
                default:
                    Console.WriteLine("no mode selected service is stoping");
                    this.Stop();
                    break;
            }

            engine.StartToMooh();


        }


        protected override void OnStop()
        {
            engine.Dispose();
        }


    }
}
