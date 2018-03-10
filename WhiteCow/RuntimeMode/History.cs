using System;
using System.Configuration;
using System.IO;
using System.Linq;
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
		BitFinex polo ;
		BitFinex btx ;
		string fileName = "history.csv";
		Timer tm;

        public void StartToMooh()
        {
            
			polo = new BitFinex();
			btx = new BitFinex();

			Console.WriteLine("History mode enable");
            String Header = "Date;currency;Bitfinex Last;BitFinex Ask;BitFinex Bid;Poloniex Last;Poloniex Ask;Poloniex Bid";
			if (!File.Exists(fileName))
				File.AppendAllText(fileName, Header + Environment.NewLine);

			tm = new Timer(GenerateHistory, null, 0, Convert.ToInt32(ConfigurationManager.AppSettings["History.Interval"]) * 1000);

		}

        public void GenerateHistory(object state)
		{
			
		   //check if we have a troube to connect to get services
            //broker have multiple issues per day
            // if the time gap is too large the data are inefficient
            if (Math.Abs(polo.LastTicks.Values.First().Timestamp - btx.LastTicks.Values.First().Timestamp) >= 4000)
                return;
            foreach (String currency in polo.LastTicks.Keys)
            {
                if (!btx.LastTicks.ContainsKey(currency))
                    continue;
                
                String content = $"{DateTime.Now.ToString()};{currency};{btx.LastTicks[currency].Last};{btx.LastTicks[currency].Ask};{btx.LastTicks[currency].Bid};{polo.LastTicks[currency].Last};{polo.LastTicks[currency].Ask};{polo.LastTicks[currency].Bid}";


                Console.WriteLine(content);

                File.AppendAllText(fileName, content + Environment.NewLine);
            }

		}

        public void Dispose()
        {
            tm.Dispose();
        }
    }
}
