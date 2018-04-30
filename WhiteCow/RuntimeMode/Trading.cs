using System;
using System.Configuration;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.Interface;
using WhiteCow.Entities.Trading;
using WhiteCow.Log;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WhiteCow.RuntimeMode
{
    /// <summary>
    /// the power of white cow
    /// here is the trading intelligence
    /// </summary>
    public class Trading : IRuntimeMode
    {
        readonly Double ThresholdGap;
        public TradingStep Step;
        readonly Boolean LogToFile;
        readonly string fileName = "Runtime.csv";

        public Trading()
        {
            ThresholdGap = Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.ThresholdGap"]);
            LogToFile = Convert.ToBoolean(ConfigurationManager.AppSettings["Runtime.LogTicks"]);

            if (LogToFile)
            {
                String Header = "Date;currency;BrHigh Name;BrHigh Last;BrHigh Ask;BrHigh Bid;BrLow Name;BrLow Last;BrLow Ask;BrLow Bid;state";
                if (!File.Exists(fileName))
                    File.AppendAllText(fileName, Header + Environment.NewLine);
            }
        }
        /// <summary>
        /// start trading engine
        /// </summary>
        public void StartToMooh()
        {

            Poloniex polo = new Poloniex();
            BitFinex btx = new BitFinex();


#if DEBUG
			polo.GetOpenOrders("XRP");
			//SetPosition(polo, btx, "XRP");
			return;
#else
            while (true)
                TickGapAnalisys(polo, btx);
#endif
        }

        /// <summary>
        /// Analyse if the gap is enough to start trading
        /// if not then wait
        /// </summary>
        private void TickGapAnalisys(Poloniex polo, BitFinex btx)
        {

            Logger.Instance.LogInfo("Tick analysis");
            Step = TradingStep.Tick;

            //tick analisys
            var Gap = new Tuple<String, Double>("", 0.0);

            do
            {
                Logger.Instance.LogInfo("Gap is not enough wait 10sec");
                Thread.Sleep(10000);

                Task t1 = Task.Run(() => { polo.GetTicks(); });
                Task t2 = Task.Run(() => { btx.GetTicks(); });

                Task.WaitAll();

                if (polo.LastTicks == null)
                    continue;


                if (btx.LastTicks == null)
                    continue;

                //check if the broker answer in time to be efficient if not play again
                if (Math.Abs(btx.LastTicks.Values.First().Timestamp - polo.LastTicks.Values.First().Timestamp) >= 4000)
                    continue;

                Gap = CheckCurrenciesGap(polo, btx);
            } while (Gap.Item2 < ThresholdGap);

            Logger.Instance.LogInfo($"Gap is large enough take position on {Gap.Item1}");

            if (polo.LastTicks[Gap.Item1].Last > btx.LastTicks[Gap.Item1].Last)
                SetPosition(btx, polo, Gap.Item1);
            else
                SetPosition(polo, btx, Gap.Item1);

            polo.RefreshWallet();
            btx.RefreshWallet();
        }

        /// <summary>
        /// Checks all currencies gap between two broker.
        /// </summary>
        /// <returns>a tupple with the currency and the gap value</returns>
        /// <param name="br1">Br1.</param>
        /// <param name="br2">Br2.</param>
        private Tuple<String, Double> CheckCurrenciesGap(Broker.Broker br1, Broker.Broker br2)
        {
            var finalgap = new Tuple<String, Double>("", 0.0);
            foreach (var currency in br1.LastTicks.Keys)
            {
                double gap = 0.0;
                if (!br2.LastTicks.ContainsKey(currency))
                    continue;

                if (br1.LastTicks[currency].Last > br2.LastTicks[currency].Last)
                    gap = 100.0 * (br1.LastTicks[currency].Bid / br2.LastTicks[currency].Ask - 1.0);
                else
                    gap = 100.0 * (br2.LastTicks[currency].Bid / br1.LastTicks[currency].Ask - 1.0);

                if (gap > finalgap.Item2)
                    finalgap = Tuple.Create<String, Double>(currency, gap);

                Logger.Instance.LogInfo($"for the currency {currency} the Gap is {gap}");
            }
            return finalgap;
        }

        /// <summary>
        /// put all positions in both platform
        /// </summary>
        /// <param name="Brlow">low broker ticker, the long one</param>
        /// <param name="BrHigh">High broker ticker, the short one</param>
        private void SetPosition(Broker.Broker Brlow, Broker.Broker BrHigh, String currency)
        {

            Logger.Instance.LogInfo("taking position..");
            Step = TradingStep.OpenPosition;
            Double amount = Brlow.BaseWallet.amount > BrHigh.BaseWallet.amount ? Brlow.BaseWallet.amount : BrHigh.BaseWallet.amount;
            Logger.Instance.LogInfo($"amount for trading is {amount} {Brlow.BaseWallet.currency}");

         

            Double QuoteAmount = BrHigh.MarginSell(currency, amount, Brlow.BaseWallet.currency);
            //control one
            //check if the orders are passed
            //the amount of margin postion should be the same as QuoteAmount
            //if not then close the possible partially opened ones
            //return

            if (Double.IsNaN(QuoteAmount))
                return;

            //buying the same as as we shorted earlier
            Brlow.MarginBuy(currency, QuoteAmount, currency);

            //control two
            //this time it is different we have to be sure that we are in position
            //if not enough money in market then close open position and reopen new ones


            if (LogToFile)
                LogTicks(BrHigh, Brlow, currency, "init position");

            Logger.Instance.LogInfo("Position done");

            //# debug for avoiding to forget the real function
#if DEBUG
            ClosePosition(Brlow, BrHigh, currency, QuoteAmount);
#else
            CheckClosePosition(Brlow, BrHigh,currency, amount);
#endif

        }
		/// <summary>
		/// wait for the cross or nearly the cross for closing position
		/// </summary>
		/// <param name="Brlow">low broker ticker, the long one</param>
		/// <param name="BrHigh">High broker ticker, the short one</param>
		private void CheckClosePosition(Broker.Broker Brlow, Broker.Broker BrHigh, String currency, Double amount)
        {

            Logger.Instance.LogInfo("Now Waiting for the cross");
            Step = TradingStep.ClosePosition;
            double gap = Double.NaN;
            do
            {
                Logger.Instance.LogInfo($"Gap is is too large retry");

                Thread.Sleep(10000);

                if (Brlow.LastTicks[currency] == null)
                    continue;

                if (BrHigh.LastTicks[currency] == null)
                    continue;

                //check if the broker answer in time to be efficient if not play again
                if (Math.Abs(BrHigh.LastTicks[currency].Timestamp - Brlow.LastTicks[currency].Timestamp) >= 4000)
                    continue;

                gap = 100.0 * (BrHigh.LastTicks[currency].Ask / Brlow.LastTicks[currency].Bid - 1.0);
                Logger.Instance.LogInfo($"Gap is {gap}");
                if (LogToFile)
                    LogTicks(BrHigh, Brlow, currency, "check close");

            } while (gap > Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.Closegap"]));

            //Now close position
            ClosePosition(Brlow, BrHigh, currency,amount);
        }

        /// <summary>
        /// once the cross is done closing position...
        /// </summary>
        /// <param name="Brlow">Brlow.</param>
        /// <param name="BrHigh">Br high.</param>
        /// <param name="currency">Currency.</param>
        private static void ClosePosition(Broker.Broker Brlow, Broker.Broker BrHigh, string currency, Double amount)
        {
			Logger.Instance.LogInfo($"Now closing position");

           
            while (!BrHigh.ClosePosition(currency))
                    Thread.Sleep(1000);
           
            while (!Brlow.ClosePosition(currency))
                    Thread.Sleep(1000);
           
            
            Logger.Instance.LogInfo("position closed");
        }


		private void LogTicks(Broker.Broker BrHigh, Broker.Broker BrLow, String currency, String state)
		{

			String content = String.Concat($"{DateTime.Now.ToString()};{currency};{BrHigh.Name.ToString()}"
			   , $";{BrHigh.LastTicks[currency].Last.ToString("F20").TrimEnd('0')}"
			   , $";{BrHigh.LastTicks[currency].Ask.ToString("F20").TrimEnd('0')}"
			   , $";{BrHigh.LastTicks[currency].Bid.ToString("F20").TrimEnd('0')}"
			   , $";{BrLow.Name.ToString()}"
			   , $";{BrLow.LastTicks[currency].Last.ToString("F20").TrimEnd('0')}"
			   , $";{BrLow.LastTicks[currency].Ask.ToString("F20").TrimEnd('0')}"
			   , $";{BrLow.LastTicks[currency].Bid.ToString("F20").TrimEnd('0')}"
			   , $";{state}");

			File.AppendAllText(fileName, content + Environment.NewLine);

		}

		public void Dispose()
		{

		}
	}
}
