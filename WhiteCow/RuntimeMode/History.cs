using System;
using System.Configuration;
using System.IO;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.Interface;

namespace WhiteCow.RuntimeMode
{
    /// <summary>
    /// retrieving data for analysis before go to trading
    /// </summary>
    public class History: IRuntimeMode
    {
		Poloniex polo ;
		BitFinex btx ;
		string fileName = "history.csv";
		Timer tm;

        public void StartToMooh()
        {
            
			polo = new Poloniex();
			btx = new BitFinex();

			Console.WriteLine("History mode enable");
			String Header = "Date;Bitfinex;Poloniex";
			if (!File.Exists(fileName))
				File.AppendAllText(fileName, Header + Environment.NewLine);

			tm = new Timer(GenerateHistory, null, 0, Convert.ToInt32(ConfigurationManager.AppSettings["History.Interval"]) * 1000);

		}

        public void GenerateHistory(object state)
		{
			
		   //check if we have a troube to connect to get services
            //broker have multiple issues per day
            // if the time gap is too large the data are inefficient
            if (Math.Abs(polo.LastTick.Timestamp - btx.LastTick.Timestamp) >= 4000)
                return;

            String content = $"{DateTime.Now.ToString()};{btx.LastTick.Last};{polo.LastTick.Last}";
           

			Console.WriteLine(content);

			File.AppendAllText(fileName, content + Environment.NewLine);


		}

        public void Dispose()
        {
            tm.Dispose();
        }
    }
}
