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
        Poloniex polo ;
		BitFinex btx ;
		string fileName = "history.csv";
		Timer tm;

        public void StartToMooh()
        {
            
            polo = new Poloniex();
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
                
                String content = String.Concat($"{DateTime.Now.ToString()}"
                  ,$";{currency}" 
                  ,$";{btx.LastTicks[currency].Last.ToString("F20").TrimEnd('0')}" 
                  ,$";{btx.LastTicks[currency].Ask.ToString("F20").TrimEnd('0')}" 
                  ,$";{btx.LastTicks[currency].Bid.ToString("F20").TrimEnd('0')}" 
                  ,$";{polo.LastTicks[currency].Last.ToString("F20").TrimEnd('0')}"
                  ,$";{polo.LastTicks[currency].Ask.ToString("F20").TrimEnd('0')}" 
                  ,$";{polo.LastTicks[currency].Bid.ToString("F20").TrimEnd('0')}");


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
