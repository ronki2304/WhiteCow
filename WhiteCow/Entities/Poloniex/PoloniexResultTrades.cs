using System;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Poloniex
{
    public class PoloniexResultTrades
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonProperty("resultingTrades")]
        public ResultingTrade[] ResultingTrades { get; set; }

		public static PoloniexResultTrades FromJson(string json) => JsonConvert.DeserializeObject<PoloniexResultTrades>(json);


		public String serialize()
		{
			return JsonConvert.SerializeObject(this);
		}
    }

    public class ResultingTrade
    {
        [JsonProperty("amount")]
        public Double Amount { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("rate")]
        public Double Rate { get; set; }

        [JsonProperty("total")]
        public Double Total { get; set; }

        [JsonProperty("tradeID")]
        public string TradeID { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}