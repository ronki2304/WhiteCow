using System;
using System.Collections.Generic;
using System.Net;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace WhiteCow.Entities.Poloniex.Close
{
	public partial class PoloniexCloseResult
	{
		[JsonProperty("success")]
		public long Success { get; set; }

		[JsonProperty("message")]
		public string Message { get; set; }

		[JsonProperty("resultingTrades")]
		public Dictionary<string, List<ResultingTrade>> ResultingTrades { get; set; }
	}

	public partial class ResultingTrade
	{
		[JsonProperty("amount")]
		public string Amount { get; set; }

		[JsonProperty("date")]
		public System.DateTimeOffset Date { get; set; }

		[JsonProperty("rate")]
		public string Rate { get; set; }

		[JsonProperty("total")]
		public string Total { get; set; }

		[JsonProperty("tradeID")]
		public string TradeId { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }
	}

	public partial class PoloniexCloseResult
	{
		public static PoloniexCloseResult FromJson(string json) => JsonConvert.DeserializeObject<PoloniexCloseResult>(json, Converter.Settings);
	}

	public static class Serialize
	{
		public static string ToJson(this PoloniexCloseResult self) => JsonConvert.SerializeObject(self, Converter.Settings);
	}

	internal class Converter
	{
		public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
			DateParseHandling = DateParseHandling.None,
			Converters = {
				new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
			},
		};
	}
}
