using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Poloniex
{
    public class PoloniexMarketOrderBook
    {
			[JsonProperty(PropertyName = "asks")]
			public IList<List<Double>> Raw_asks;
			[JsonProperty(PropertyName = "bids")]
			public IList<List<Double>> Raw_bids;
			public Int32 isFrozen;
			public Int32 seq;
		public static PoloniexMarketOrderBook FromJson(string json) => JsonConvert.DeserializeObject<PoloniexMarketOrderBook>(json);

	}
}
