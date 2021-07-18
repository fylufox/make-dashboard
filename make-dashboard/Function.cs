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
        const string _URL = "https://slack.com/api/chat.postMessage";
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

            EventList eventList = new EventList(EMAIL,JSONPATH);
            if (DEBUG != "true")
            {
                if (eventList.notify == false) { return "holiday"; }
            }

            slack.Message slackMessage = new slack.Message(CHANNEL);
            var acceptedEventMessage = PushSlackMessageAsync(slackMessage.AcceptedEventList(eventList.AcceptedEvents), TOKEN).Result;
            var undecidedEventMessage = PushSlackMessageAsync(slackMessage.UndecidedEventList(eventList.UndecidedEvents), TOKEN).Result;

            IssueList issueList = new IssueList(APIKEY, USERID);
            var issuelistMessage = PushSlackMessageAsync(slackMessage.Issuelist(issueList.Issues), TOKEN).Result;

            return "test";
        }

        private Task<HttpResponseMessage> PushSlackMessageAsync(string message,string token)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _URL);
            request.Content = new StringContent(message, Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"Bearer {token}");
            return client.SendAsync(request);
        }
    }
}
