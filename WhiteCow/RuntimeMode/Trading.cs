﻿using System;
using System.Configuration;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.Interface;
using WhiteCow.Entities.Trading;
using WhiteCow.Log;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            LogToFile = Convert.ToBoolean(ConfigurationManager.AppSettings["Runtime.LogPositions"]);

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




#if DEBUG
            Poloniex polo = new Poloniex();
            BitFinex btx = new BitFinex();


          
                //TickGapAnalisys(polo, btx);
			SetPosition(polo, btx, "XRP");

            return;
#else
            Poloniex polo = new Poloniex();
            BitFinex btx = new BitFinex();
            //first refresh spreadsheet
            Logger.Instance.UpdateWallet(new List<string>() { polo.Name.ToString(), btx.Name.ToString() },
                                       new List<object>() { DateTime.Now, polo.BaseWallet.amount, btx.BaseWallet.amount });

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

                Task t1 = Task.Run(() => { var toto = polo.LastTicks == null; });
                Task t2 = Task.Run(() => { var toto = btx.LastTicks == null; });

                t1.Wait();
                t2.Wait();

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

            //log new wallet amount
            Logger.Instance.UpdateWallet(new List<string>() { polo.Name.ToString(), btx.Name.ToString() },
                                         new List<object>() { DateTime.Now, polo.BaseWallet.amount, btx.BaseWallet.amount });

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


            if (Double.IsNaN(QuoteAmount))
                return;

            //control in another thread for performance issue
            //need to be the speedier we can
            Task t1 = Task.Run(() => { CheckMarginOperation(BrHigh, currency, "Sell"); });



            //buying the same as as we shorted earlier
            Brlow.MarginBuy(currency, QuoteAmount, currency);


            //control two
            //this time it is different we have to be sure that we are in position
            //if not enough money in market then close open position and reopen new ones
            CheckMarginOperation(Brlow, currency, "Buy");
            t1.Wait();

            if (LogToFile)
                LogPositions(BrHigh, Brlow, currency, "init position");

            Logger.Instance.LogInfo("Position done");

            //# debug for avoiding to forget the real function
#if DEBUG
            ClosePosition(Brlow, BrHigh, currency, QuoteAmount);
#else
            CheckClosePosition(Brlow, BrHigh, currency, amount);
#endif

        }

        /// <summary>
        /// check if all all operations are traded to avoid endless open order
        /// if some are found then cancel open order and replace new trade
        /// </summary>
        /// <param name="brok">broker where operation are done</param>
        /// <param name="currency">Currency.</param>
        /// <param name="state">Sell for the brHigh Buy for the brLow</param>
        public void CheckMarginOperation(Broker.Broker brok, String currency, String state)
        {
            List<Tuple<String, Double>> openorders;
            Double amount = 0.0;

            do
            {
                Thread.Sleep(1000);

                openorders = brok.GetOpenOrders(currency);

                //cancel order
                if (openorders != null && openorders.Count != 0)
                {
                    foreach (var order in openorders)
                    {
                        brok.CancelOpenOrder(order.Item1);
                        amount += order.Item2;
                    }
                    if (state == "Sell")
                        brok.MarginSell(currency, amount, currency);
                    else
                        brok.MarginBuy(currency, amount, currency);
                }
                amount = 0.0;
            } while (openorders != null && openorders.Count != 0);

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

            } while (gap > Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.Closegap"]));

            //Now close position
            ClosePosition(Brlow, BrHigh, currency, amount);
        }

        /// <summary>
        /// once the cross is done closing position...
        /// </summary>
        /// <param name="Brlow">Brlow.</param>
        /// <param name="BrHigh">Br high.</param>
        /// <param name="currency">Currency.</param>
        private void ClosePosition(Broker.Broker Brlow, Broker.Broker BrHigh, string currency, Double amount)
        {
            Logger.Instance.LogInfo($"Now closing position");

            if (LogToFile)
                LogPositions(BrHigh, Brlow, currency, "close position");

            while (!BrHigh.ClosePosition(currency))
                Thread.Sleep(1000);

            while (!Brlow.ClosePosition(currency))
                Thread.Sleep(1000);


            Logger.Instance.LogInfo("position closed");
        }

        /// <summary>
        /// Logs the positions in google spreadsheet
        /// </summary>
        /// <param name="BrHigh">Br high.</param>
        /// <param name="BrLow">Br low.</param>
        /// <param name="currency">Currency.</param>
        /// <param name="state">State.</param>
        private void LogPositions(Broker.Broker BrHigh, Broker.Broker BrLow, String currency, String state)
        {
            try
            {
                Logger.Instance.LogPositions(currency
                   , BrHigh.Name.ToString()
                   , BrHigh.LastTicks[currency].Last
                   , BrHigh.LastTicks[currency].Ask
                   , BrHigh.LastTicks[currency].Bid
                   , BrLow.Name.ToString()
                   , BrLow.LastTicks[currency].Last
                   , BrLow.LastTicks[currency].Ask
                   , BrLow.LastTicks[currency].Bid
                   , state);
            }
            catch
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
        }

        public void Dispose()
        {

        }
    }
}
