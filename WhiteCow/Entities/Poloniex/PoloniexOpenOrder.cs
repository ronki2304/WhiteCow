using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace WhiteCow.Entities.Poloniex.PoloniexOpenOrder
{
    public partial class PoloniexOpenOrder
    {
        [JsonProperty("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("rate")]
        public string Rate { get; set; }

        [JsonProperty("amount")]
        public Double Amount { get; set; }

        [JsonProperty("total")]
        public string Total { get; set; }
    }

    public partial class PoloniexOpenOrder
    {
        public static List<PoloniexOpenOrder> FromJson(string json) => JsonConvert.DeserializeObject<List<PoloniexOpenOrder>>(json);
    }

    public static class Serialize
    {
        public static string ToJson(this List<PoloniexOpenOrder> self) => JsonConvert.SerializeObject(self);
    }



}
