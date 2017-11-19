using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using WhiteCow.Broker;
 
//miaou
namespace WhiteCow
{
    public class TicToc : ServiceBase
    {
		string fileName = "history.csv";
        Timer tm;
         public static void Main(String[] args)
    {
        (new TicToc()).OnStart(new string[1]);
        ServiceBase.Run(new TicToc());
        Console.ReadLine();

    }


        protected override void OnStart(string[] args)
        {
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["history"]))
            {
                String Header = "Date;Bitfinex;Cex.io;Poloniex";
                if (!File.Exists(fileName))
                    File.AppendAllText(fileName, Header + Environment.NewLine);

                tm = new Timer(HandleTimerCallback, null, 0, Convert.ToInt32(ConfigurationManager.AppSettings["Interval"]) * 1000);
            }
            else
            {
                var polo = new Poloniex();


			}
                
        }
		void HandleTimerCallback(object state)
		{
            BitFinex btf = new BitFinex();
            Poloniex polo = new Poloniex();
            String bitfinextick, cextick, polotick;
            try
            {
                var btfTicker = btf.GetTick();
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

    }
}
