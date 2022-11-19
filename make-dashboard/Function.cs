using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DotNetEnv;

using Amazon.Lambda.Core;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace make_dashboard
{
    public class Function
    {
        const string _URL = "https://slack.com/api/";
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string FunctionHandler(JObject input, ILambdaContext context)
        {
            LoadOptions loadOptions = new LoadOptions(true, false, true);
            Env.Load("./env/.env",loadOptions);
            string TOKEN = System.Environment.GetEnvironmentVariable("TOKEN");
            string EMAIL = System.Environment.GetEnvironmentVariable("EMAIL");
            string JSONPATH = System.Environment.GetEnvironmentVariable("JSONPATH");
            string CHANNEL = System.Environment.GetEnvironmentVariable("CHANNEL");
            string APIKEY = System.Environment.GetEnvironmentVariable("APIKEY");
            string USERID = System.Environment.GetEnvironmentVariable("USERID");
            string DEBUG = System.Environment.GetEnvironmentVariable("DEBUG");
            string TZ = System.Environment.GetEnvironmentVariable("TZ");
            string slack_ts="";

            
            EventList eventList = new EventList(EMAIL, JSONPATH);
            if (DEBUG != "true")
            {
                if (eventList.notify == false) { return "Today is holiday!"; }
            }
            IssueList issueList = new IssueList(APIKEY, USERID);

            try
            {
                var api_body = input["body"].ToString();
                api_body = api_body.Replace("payload=", "");
                api_body = System.Web.HttpUtility.UrlDecode(api_body);
                var slack_responce = JObject.Parse(api_body);
                slack_ts = slack_responce["message"]["ts"].ToString();
                CHANNEL = slack_responce["channel"]["id"].ToString();
            }
            catch
            {
                
            }

            slack.Message slackMessage = new slack.Message(CHANNEL);
            if (slack_ts == "")
            {
                var acceptedEventMessage = PushSlackMessageAsync(slackMessage.AcceptedEventList(eventList.AcceptedEvents), TOKEN).Result;
                var undecidedEventMessage = PushSlackMessageAsync(slackMessage.UndecidedEventList(eventList.UndecidedEvents), TOKEN).Result;
                var issuelistMessage = PushSlackMessageAsync(slackMessage.Issuelist(issueList.Issues), TOKEN).Result;
                Console.WriteLine("From Event Bridge.");
            }
            else
            {
                var issuelistMessage = PushSlackMessageUpdateAsync(slackMessage.Issuelist(issueList.Issues,slack_ts), TOKEN).Result;
                Console.WriteLine("From API Gateway.");
            }

            return "Function complete!";
        }

        private Task<HttpResponseMessage> PushSlackMessageAsync(string message,string token)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _URL+ "chat.postMessage");
            request.Content = new StringContent(message, Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"Bearer {token}");
            return client.SendAsync(request);
        }

        private Task<HttpResponseMessage> PushSlackMessageUpdateAsync(string message, string token)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _URL+ "chat.update");
            request.Content = new StringContent(message, Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"Bearer {token}");
            return client.SendAsync(request);
        }
    }
}
