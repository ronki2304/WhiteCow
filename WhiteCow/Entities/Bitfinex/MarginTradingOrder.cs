using System;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Bitfinex
{
    public class bfxMarginTradingOrder
    {
        public String request;
        public long nonce;
        public String Symbol;
        public double amount;
        public double price;
        public const String exchange = "bitfinex";
        public String side;
        public String type="market";

        public String ConstructPayload()
        {
            return String.Concat(
                "{"
                ,$"request: '{request}'"
                ,$"nonce: '{nonce}'"
                ,$"symbol: '{Symbol}'"
                ,$"amount: '{amount}'"
                ,$"price: '{price}'"
                ,$"exchange: '{exchange}'"
                ,$"side: '{side}'"
                ,$"type: '{type}'"
                ,"}"
            );
        }
    }
}
