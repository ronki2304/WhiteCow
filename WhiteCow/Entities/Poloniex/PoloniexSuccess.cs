using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WhiteCow.Entities.Poloniex.Success
{
    public partial class PoloniexSuccess
    {
        [JsonProperty("success")]
        public long SuccessSuccess { get; set; }
    }

    public partial class PoloniexSuccess
    {
        public static PoloniexSuccess FromJson(string json) => JsonConvert.DeserializeObject<PoloniexSuccess>(json);
    }

    public static class Serialize
    {
        public static string ToJson(this PoloniexSuccess self) => JsonConvert.SerializeObject(self);
    }


}

