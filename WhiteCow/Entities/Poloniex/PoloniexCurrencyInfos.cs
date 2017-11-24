using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace WhiteCow.Entities.Poloniex
{
    public class PoloniexCurrencyInfos
    {
        public static Dictionary<String,PoloniexCurrencyInfos> FromJson(string json) => JsonConvert.DeserializeObject<Dictionary<String, PoloniexCurrencyInfos>>(json);

		public String serialize()
		{
			return JsonConvert.SerializeObject(this);
		}
   
		[JsonProperty("delisted")]
		public long Delisted { get; set; }

		[JsonProperty("depositAddress")]
		public string DepositAddress { get; set; }

		[JsonProperty("disabled")]
        public Boolean Disabled { get; set; }

		[JsonProperty("frozen")]
        public Boolean Frozen { get; set; }

		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("minConf")]
		public long MinConf { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("txFee")]
		public Double TxFee { get; set; }
	}
}
