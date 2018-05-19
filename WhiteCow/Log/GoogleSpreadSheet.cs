using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace WhiteCow.Log
{
    internal class GoogleSpreadSheet
    {
		readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
		 const string ApplicationName = "WhiteCow";

        String spreadsheetId;
        UserCredential credential;
        SheetsService service;

		public GoogleSpreadSheet()
        {
            spreadsheetId= ConfigurationManager.AppSettings["Google.spreadsheetId"];


			using (var stream =
			  new FileStream(ConfigurationManager.AppSettings["Google.Secret"], FileMode.Open, FileAccess.Read))
			{
				string credPath = System.Environment.GetFolderPath(
					System.Environment.SpecialFolder.Personal);
				credPath = Path.Combine(credPath, ".credentials/WhiteCow.json");

				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(credPath, true)).Result;
				Console.WriteLine("Credential file saved to: " + credPath);
			}

			service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});
        }

        /// <summary>
        /// Adds the new position.
        /// </summary>
        /// <param name="dt">Dt.</param>
        /// <param name="currency">Currency.</param>
        /// <param name="BrHighName">Br high name.</param>
        /// <param name="BrHighLast">Br high last.</param>
        /// <param name="BrHighAsk">Br high ask.</param>
        /// <param name="BrHighBid">Br high bid.</param>
        /// <param name="BrLowName">Br low name.</param>
        /// <param name="BrLowLast">Br low last.</param>
        /// <param name="BrLowAsk">Br low ask.</param>
        /// <param name="BrLowBid">Br low bid.</param>
        /// <param name="state">State.</param>
        public void AddNewPosition( String currency, String BrHighName, Double BrHighLast
                                   ,Double BrHighAsk, Double BrHighBid, String  BrLowName
                                   ,Double BrLowLast, Double BrLowAsk, Double  BrLowBid , String state)
        {
			const String rangePositions = "Positions!A2:K";

			IList<Object> obj = new List<Object>();
            obj.Add(DateTime.Now);
            obj.Add(currency);
            obj.Add(BrHighName);
            obj.Add(BrHighLast);
            obj.Add(BrHighAsk);
            obj.Add(BrHighBid);
            obj.Add(BrLowName);
            obj.Add(BrLowLast);
            obj.Add(BrLowAsk);
            obj.Add(BrLowBid);
            obj.Add(state);

			IList<IList<Object>> myvalues = new List<IList<Object>>();
			myvalues.Add(obj);

			ValueRange vr = new ValueRange();
			vr.Values = myvalues;

			SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(vr, spreadsheetId, rangePositions);
			request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            var response = request.Execute();
		}

        public void UpdateWalletData(List<String> Titles, List<Object> Value)
        {
            if (Titles.First() != String.Empty)
                Titles.Insert(0, String.Empty);

			String rangeWallet = "Wallet!A:C";

			
            IList<IList<Object>> Wallet = new List<IList<Object>>();
             Wallet.Add(Titles.Select(p=> (Object)p).ToList());
            Wallet.Add(Value);

			ValueRange vrWallet = new ValueRange();
            vrWallet.Values = Wallet;

			var request = service.Spreadsheets.Values.Update(vrWallet, spreadsheetId, rangeWallet);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			request.Execute();
		}
    }
}
