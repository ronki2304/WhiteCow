﻿using System;
using Newtonsoft.Json;

namespace WhiteCow.Entities.Bitfinex.PostRequest.V1
{
	public class BitfinexOrderStatusResponse
	{

		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("symbol")]
		public string Symbol { get; set; }

		[JsonProperty("exchange")]
		public string Exchange { get; set; }

		[JsonProperty("price")]
		public string Price { get; set; }

		[JsonProperty("avg_execution_price")]
		public string AvgExecutionPrice { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("timestamp")]
		public string Timestamp { get; set; }

		[JsonProperty("is_live")]
		public bool IsLive { get; set; }

		[JsonProperty("is_cancelled")]
		public bool IsCancelled { get; set; }

		[JsonProperty("was_forced")]
		public bool WasForced { get; set; }

		[JsonProperty("original_amount")]
		public string OriginalAmount { get; set; }

		[JsonProperty("remaining_amount")]
		public string RemainingAmount { get; set; }

		[JsonProperty("executed_amount")]
		public string ExecutedAmount { get; set; }

		public override string ToString()
		{
			var str = string.Format("(Id: {0}) Symb:{1} {2} Sz:{3} - Px:{4}. (Type:{5}, IsLive:{6}, Executed Amt:{7})" +
				  "(IsCancelled: {8}, WasForced: {9}, RemainingAmount: {10}, ExecutedAmount: {11})",
				  Id, Symbol, Side, OriginalAmount, Price, Type, IsLive, ExecutedAmount, IsCancelled,
				  WasForced, RemainingAmount, ExecutedAmount);
			return str;
		}
	}

}
