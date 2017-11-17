using System;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Bitfinex.PostRequest.V1
{
	public class BitfinexPostBase
	{
		[JsonProperty("request")]
		public string Request { get; set; }

		[JsonProperty("nonce")]
		public String Nonce { get; set; }


        public String serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
	}
}
