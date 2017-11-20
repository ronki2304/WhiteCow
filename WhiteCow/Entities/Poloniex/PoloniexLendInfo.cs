using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Poloniex
{

	public class PoloniexLendInfo
	{
		[JsonProperty("demands")]
		public List<Demand> Demands { get; set; }

		[JsonProperty("offers")]
		public List<Demand> Offers { get; set; }

        public static PoloniexLendInfo FromJson(string json) => JsonConvert.DeserializeObject<PoloniexLendInfo>(json);

		public String serialize()
		{
			return JsonConvert.SerializeObject(this);
		}
	}

	public class Demand
	{
		[JsonProperty("amount")]
		public string Amount { get; set; }

		[JsonProperty("rangeMax")]
		public long RangeMax { get; set; }

		[JsonProperty("rangeMin")]
		public long RangeMin { get; set; }

		[JsonProperty("rate")]
		public Double Rate { get; set; }
	}
}