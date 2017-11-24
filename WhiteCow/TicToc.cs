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


//miaou
namespace WhiteCow
{
    public class TicToc : ServiceBase
    {
      
        Timer tm;
        History hist;
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


            switch (servicemode)
            {
                case WhiteCowMode.History:
                    hist = new History();

                    tm = new Timer(hist.GenerateHistory, null, 0, Convert.ToInt32(ConfigurationManager.AppSettings["History.Interval"]) * 1000);
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
       

        protected override void OnStop()
        {
            tm.Dispose();
        }

        /// <summary>
        /// start trading engine
        /// </summary>
        void StartToMooh()
        {
			BitFinex btx = new BitFinex();
            Console.WriteLine(btx.GetWithDrawFees());
            //TickGapAnalisys();
        }

        /// <summary>
        /// Analyse if the gap is enough to start trading
        /// if not then wait
        /// </summary>
        private void TickGapAnalisys()
        {
            Poloniex polo = new Poloniex();
            BitFinex btx = new BitFinex();

            //tick analisys
            Double gap = 0.0;
            do
            {
                Thread.Sleep(10000);

                if (polo.LastTick == null)
                    continue;


                if (btx.LastTick == null)
                    continue;
                
                if (polo.LastTick.Last > btx.LastTick.Last)
                    gap = polo.LastTick.Last / btx.LastTick.Last - 100.0;
                else
                    gap = btx.LastTick.Last / polo.LastTick.Last - 100.0;
            } while (gap < ThresholdGap);

            if (polo.LastTick.Last > btx.LastTick.Last)
                SetPosition(btx, polo);
            else
                SetPosition(polo, btx);
        }

		/// <summary>
		/// put all positions in both platform
		/// </summary>
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
		private void SetPosition(Broker.Broker Brlow, Broker.Broker BrHigh)
        {
            Brlow.MarginBuy();
            BrHigh.MarginSell();
            ClosePosition(Brlow,BrHigh);
        }
		/// <summary>
		/// wait for the cross or nearly the cross for closing position
		/// </summary>
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
		private void ClosePosition(Broker.Broker Brlow, Broker.Broker BrHigh)
        {			
			do
			{
				Thread.Sleep(10000);

				if (Brlow.LastTick == null)
					continue;
                
				if (BrHigh.LastTick == null)
					continue;
            } while (BrHigh.LastTick.Last/Brlow.LastTick.Last-100>Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.Closegap"]));

            BrHigh.ClosePosition();
            Brlow.ClosePosition();
        }
		/// <summary>
		/// Reequilibrate all broker
		/// </summary>
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
		private void EquilibrateFund(Broker.Broker Brlow, Broker.Broker BrHigh)
        {
            
        }
    }
}
