namespace WhiteCow.Entities
{
	using System;
	using System.Net;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	public partial class Ticker
	{
		[JsonProperty("last")]
		public Double Last { get; set; }

		[JsonProperty("bid")]
		public Double Bid { get; set; }

		[JsonProperty("ask")]
		public double Ask { get; set; }

		[JsonProperty("high")]
		public Double High { get; set; }

		[JsonProperty("timestamp")]
		public Int64 Timestamp { get; set; }

		[JsonProperty("low")]
		public Double Low { get; set; }

		[JsonProperty("volume")]
		public Double Volume { get; set; }

		[JsonProperty("volume30d")]
		public Double Volume30d { get; set; }
	}

	public partial class Ticker
	{
		public static Ticker FromJson(string json) => JsonConvert.DeserializeObject<Ticker>(json, Converter.Settings);
	}

	public static class Serialize
	{
		public static string ToJson(this Ticker self) => JsonConvert.SerializeObject(self, Converter.Settings);
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