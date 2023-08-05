using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace make_dashboard
{
    class IssueList
    {
        const string _URL = "https://aws-plus.backlog.jp/api/v2/issues";
        public struct Issue
        {
            public string issuekey;
            public string summary;
            public string status;
            public string dueDate;
        }

        List<Issue> _issues;

        public List<Issue> Issues {
            get { return _issues; }
        }

        public IssueList(string apikey,string assignee_id)
        {
            _issues = new List<Issue>();
            string fullurl = string.Format("{0}?apiKey={1}&assigneeId[]={2}&statusId[0]=1&statusId[1]=2&statusId[2]=3&count=100",
                _URL, apikey, assignee_id);
            var client = new HttpClient();
            var res = client.GetAsync(fullurl).Result;
            var body = res.Content.ReadAsStringAsync().Result;
            dynamic json = JsonConvert.DeserializeObject(body);

            foreach(var item in json)
            {
                var issue = new Issue();
                issue.issuekey = item.issueKey;
                issue.summary = item.summary;
                issue.status = item.status.name;
                if (item.dueDate != null)
                {
                    issue.dueDate = item.dueDate;
                }
                _issues.Add(issue);
            }
        }
    }
}
