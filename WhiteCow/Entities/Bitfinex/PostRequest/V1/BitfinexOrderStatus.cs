using System;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Bitfinex.PostRequest.V1
{
    public class BitfinexOrderStatus: BitfinexPostBase
    {
		[JsonProperty("order_id")]
        public Int64 OrderId { get; set; }
    }
}
