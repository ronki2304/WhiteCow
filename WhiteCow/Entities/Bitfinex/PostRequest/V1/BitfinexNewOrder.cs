using System;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Bitfinex.PostRequest.V1
{
	public class BitfinexNewOrder : BitfinexPostBase
	{
		[JsonProperty("symbol")]
		public string Symbol { get; set; }

		[JsonProperty("amount")]
		public string Amount { get; set; }

		[JsonProperty("price")]
		public String Price { get; set; }

        [JsonProperty("exchange")]
        public const string Exchange = "bitfinex";

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

        [JsonProperty("use_all_available")]
        public String use_all_available { get; set; }

	}

}
