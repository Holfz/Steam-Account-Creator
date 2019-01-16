using System;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;

namespace SteamAccCreator.Web
{
    public class MailHandler
    {
        private readonly RestClient _client = new RestClient();
        private readonly RestRequest _request = new RestRequest();

        public static string Provider = "@inboxkitten.com";

        private static readonly Uri MailboxUri = new Uri("https://api.xforce.family/api/inboxkitten/GetLatestMails");
        private static readonly Uri MailUri = new Uri("https://api.xforce.family/api/inboxkitten/GetMail");
        private static readonly Uri SteamUri = new Uri("https://store.steampowered.com/account/newaccountverification?");

        //private static readonly Regex FromRegex = new Regex(@".Steam.*");
        private static readonly Regex ConfirmMailRegex = new Regex("stoken=([^&]+).*creationid=([^\"]+)");

        public void ConfirmMail(string address)
        {
            _client.BaseUrl = MailboxUri;
            _request.Method = Method.GET;
            dynamic jsonResponse;

            _request.AddParameter("author", "Holfz");
            _request.AddParameter("recipient", address);

            do
            {
                System.Threading.Thread.Sleep(10000);
                var response = _client.Execute(_request);
                jsonResponse = JsonConvert.DeserializeObject(response.Content);
                System.Console.WriteLine(jsonResponse);
            } while (jsonResponse.totalMails < 1);

            _request.Parameters.Clear();
            try
            {
                foreach (var mail in jsonResponse.mails)
                {
                    System.Console.WriteLine(mail);
                    //if (FromRegex.IsMatch(mail.from.Value))
                    if (mail.from.Value.Contains("noreply@steampowered.com"))
                    {
                        var mailText = ReadMail(mail.id.Value);
                        var confirmUri = GetConfirmUri(mailText);
                        ConfirmSteamAccount(confirmUri);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private Uri GetConfirmUri(string bodyplain)
        {
            var matches = ConfirmMailRegex.Matches(bodyplain);
            var token1 = matches[0].Groups[1].Value;
            var token2 = matches[0].Groups[2].Value;
            var tokenUri = "stoken=" + token1 + "&creationid=" + token2;

            return new Uri(SteamUri + tokenUri);
        }

        private string ReadMail(string mailId)
        {
            _client.BaseUrl = MailUri;
            _request.Method = Method.GET;
            _request.AddParameter("author", "Holfz");
            _request.AddParameter("mailKey", mailId);
            var response = _client.Execute(_request);
            _request.Parameters.Clear();

            dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);

            return jsonResponse["body-plain"];
        }

        private void ConfirmSteamAccount(Uri uri)
        {
            _client.BaseUrl = uri;
            _request.Method = Method.GET;
            var echoresponse = _client.Execute(_request);
            _request.Parameters.Clear();
        }
    }
}