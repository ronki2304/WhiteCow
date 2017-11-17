using System;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Bitfinex.PostRequest.V1
{
    public class BitFinexWithDrawal : BitfinexPostBase
    {
		[JsonProperty("withdraw_type")]
		public string WithDrawType { get; set; }

		[JsonProperty("walletselected")]
		public string WalletSelected { get; set; }
		
        [JsonProperty("amount")]
		public string Amount { get; set; }

		[JsonProperty("address")]
		public string Address { get; set; }
	}
}
