using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Bitfinex.PostRequest.V1
{
    public class BitFinexAccountFees
    {
        [JsonProperty("withdraw")]
        public Dictionary<String, Double> Withdraw { get; set; }

        public static BitFinexAccountFees FromJson(String json) => JsonConvert.DeserializeObject<BitFinexAccountFees>(json);

    }

}
