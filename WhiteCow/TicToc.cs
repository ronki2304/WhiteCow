using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Newtonsoft.Json;
using WhiteCow.Broker;

using System.Collections.Generic;
using WhiteCow.Entities;


//miaou
namespace WhiteCow
{
    public class TicToc : ServiceBase
    {
        string fileName = "history.csv";
        Timer tm;
        Poloniex polo;
        BitFinex btx;
        Double ThresholdGap;
        public static void Main(String[] args)
        {
            (new TicToc()).OnStart(new string[1]);
            ServiceBase.Run(new TicToc());
            Console.ReadLine();

        }


        protected override void OnStart(string[] args)
        {
            WhiteCowMode servicemode = (WhiteCowMode)Enum.Parse(typeof(WhiteCowMode), ConfigurationManager.AppSettings["Mode"]);
            ThresholdGap = Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.ThresholdGap"]);

			polo = new Poloniex();
			btx = new BitFinex();

            switch (servicemode)
            {
                case WhiteCowMode.History:
                    Console.WriteLine("History mode enable");
                    String Header = "Date;Bitfinex;Cex.io;Poloniex";
                    if (!File.Exists(fileName))
                        File.AppendAllText(fileName, Header + Environment.NewLine);

                    tm = new Timer(GenerateHistory, null, 0, Convert.ToInt32(ConfigurationManager.AppSettings["History.Interval"]) * 1000);
                    break;
                case WhiteCowMode.Runtime:
                   
                    StartToMooh();
                    break;
                case WhiteCowMode.Test:
                default:
                    Console.WriteLine("no mode selected service is stoping");
                    this.Stop();
                    break;
            }


        }
        void GenerateHistory(object state)
        {
           
            String bitfinextick, cextick, polotick;
            try
            {
                var btfTicker = btx.GetTick();
                bitfinextick = btfTicker.Last.ToString();
            }
            catch
            { bitfinextick = String.Empty; }

            try
            {
                var cexTicker = CexIO.GetTick(ConfigurationManager.AppSettings["Cex.io.Pair"]);
                cextick = cexTicker.Last.ToString();
            }
            catch
            { cextick = String.Empty; }

            try
            {
                var PoloTicker = polo.GetTick();
                polotick = PoloTicker.Last.ToString();
            }
            catch
            { polotick = String.Empty; }

            String content = $"{DateTime.Now.ToString()};{bitfinextick};{cextick};{polotick}";
            Console.WriteLine(content);

            File.AppendAllText(fileName, content + Environment.NewLine);


        }

        protected override void OnStop()
        {
            tm.Dispose();
        }

        /// <summary>
        /// start trading engine
        /// </summary>
        void StartToMooh()
        {
            TickGapAnalisys();


        }

        /// <summary>
        /// Analyse if the gap is enough to start trading
        /// if not 
        /// </summary>
        private void TickGapAnalisys()
        {
            //tick analisys
            Double gap = 0.0;
            do
            {
                Thread.Sleep(10000);

                var polotick = polo.GetTick();
                if (polotick == null)
                    continue;

                var btxtick = btx.GetTick();
                if (btxtick == null)
                    continue;
                
                if (polotick.Last > btxtick.Last)
                    gap = polotick.Last / btxtick.Last - 100.0;
                else
                    gap = btxtick.Last / polotick.Last - 100.0;
            } while (gap < ThresholdGap);
            SetPosition();
        }

        /// <summary>
        /// put all positions in both platform
        /// </summary>
        private void SetPosition()
        {
            if (polo.LastTick.Last > btx.LastTick.Last)
            {
                polo.MarginSell();
                btx.MarginBuy();
            }
            else
            {
                polo.MarginBuy();
                btx.MarginSell();
            }
        }
    }
}
