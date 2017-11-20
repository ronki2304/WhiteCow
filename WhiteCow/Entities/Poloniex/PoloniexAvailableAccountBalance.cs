using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WhiteCow.Entities.Poloniex
{
	public class PoloniexAvailableAccountBalance
	{
        [JsonProperty("exchange")]
        public Dictionary<String, String> exchange;

		[JsonProperty("margin")]
		public Dictionary<String, String> margin;

		[JsonProperty("lending")]
		public Dictionary<String, String> lending;

		public static PoloniexAvailableAccountBalance FromJson(string json) => JsonConvert.DeserializeObject<PoloniexAvailableAccountBalance>(json);

		public String serialize()
		{
			return JsonConvert.SerializeObject(this);
		}
	}

}
