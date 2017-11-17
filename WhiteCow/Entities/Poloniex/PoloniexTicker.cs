using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Poloniex.Ticker
{
	public partial class PoloniexTicker
	{
		[JsonProperty("baseVolume")]
		public Double BaseVolume { get; set; }

		[JsonProperty("high24hr")]
		public Double High24hr { get; set; }

		[JsonProperty("highestBid")]
		public Double HighestBid { get; set; }

		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("isFrozen")]
		public Int32 IsFrozen { get; set; }

		[JsonProperty("last")]
		public Double Last { get; set; }

		[JsonProperty("low24hr")]
		public Double Low24hr { get; set; }

		[JsonProperty("lowestAsk")]
		public Double LowestAsk { get; set; }

		[JsonProperty("percentChange")]
		public Double PercentChange { get; set; }

		[JsonProperty("quoteVolume")]
		public Double QuoteVolume { get; set; }
	}

	public partial class PoloniexTicker
	{
		public static Dictionary<string, PoloniexTicker> FromJson(string json) => JsonConvert.DeserializeObject<Dictionary<string, PoloniexTicker>>(json, Converter.Settings);
	}

	public static class Serialize
	{
		public static string ToJson(this Dictionary<string, PoloniexTicker> self) => JsonConvert.SerializeObject(self, Converter.Settings);
	}

	public class Converter
	{
		public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
			DateParseHandling = DateParseHandling.None,
		};
	}

}
