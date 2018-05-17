using System;
using System.Collections.Generic;
using WhiteCow.Entities;

namespace WhiteCow.Broker
{
    public class Kraken : Broker
    {
      

        public Kraken(string Platform) : base(Platform)
        {
            RefreshWallet();
        }

        public override bool CancelOpenOrder(string OrderId)
        {
            throw new NotImplementedException();
        }

        public override bool ClosePosition(string currency)
        {
            throw new NotImplementedException();
        }

        public override List<Tuple<string, double>> GetOpenOrders(string currency)
        {
            throw new NotImplementedException();
        }

        public override double MarginBuy(string currency)
        {
            throw new NotImplementedException();
        }

        public override double MarginBuy(string currency, double Amount, string unit)
        {
            throw new NotImplementedException();
        }

        public override double MarginSell(string currency)
        {
            throw new NotImplementedException();
        }

        public override double MarginSell(string currency, double Amount, string unit)
        {
            throw new NotImplementedException();
        }

        public override bool RefreshWallet()
        {
            throw new NotImplementedException();
        }

        protected override Ticker GetTick(string currency)
        {
            throw new NotImplementedException();
        }

        protected override Dictionary<string, Ticker> GetTicks()
        {
            throw new NotImplementedException();
        }

        protected override string Pair(string Currency)
        {
            throw new NotImplementedException();
        }
    }
}
