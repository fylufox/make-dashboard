using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace make_dashboard.slack
{
    class Message
    {
        private string _channle;

        public Message(string channle)
        {
            _channle = channle;
        }

        public string AcceptedEventList(List<EventList.CalendarEvent> events)
        {
            var color = "05A136";
            var block_message = "本日の予定";
            if (events.Count == 0)
            {
                color = "05A136";
            }
            return eventlist(block_message, events, color);
        }

        public string UndecidedEventList(List<EventList.CalendarEvent> events)
        {
            var color = "05A136";
            var block_message = "招待に未回答の予定(未定を含む)";
            if (events.Count == 0)
            {
                color = "05A136";
            }
            return eventlist(block_message, events, color);
        }

        private string eventlist(string block_message, List<EventList.CalendarEvent> events, string color)
        {
            List<string> message = new List<string>();
            foreach (var item in events)
            {
                string time = "";
                if (item.Allday == true) { time = "終日"; }
                else
                {
                    string start = item.start.AddHours(9).ToString("t");
                    string end = item.end.AddHours(9).ToString("t");
                    time = string.Format("{0} - {1}", start, end);
                }
                message.Add(string.Format("*<{3}|{0}>* ({1})\n場所：{2}", item.Title, time, item.Location,item.link));
            }
            if (message.Count == 0) { message.Add("0件"); }

            var attachments = GgetAttachments(message,color);
            var blocks = this.GetBlocks(block_message);
            var slack_message = new JObject
            {
                ["channel"] = _channle,
                ["attachments"] = attachments,
                ["blocks"] = blocks
            };
            return slack_message.ToString();
        }

        public string Issuelist(List<IssueList.Issue> issues)
        {
            var color = "05A136";
            var block_message = "アサインされている課題";
            List<string> message = new List<string>();
            foreach (var item in issues)
            {
                string str = "";
                string dueDate = "";
                string status = "";
                if (item.dueDate == null)
                {
                    dueDate = "未設定";
                }
                else
                {
                    dueDate = DateTime.Parse(item.dueDate).ToString("d");
                }
                switch (item.status)
                {
                    case "未対応":
                        status = ":large_orange_circle:未対応";
                        break;
                    case "処理中":
                        status = ":large_blue_circle:処理中";
                        break;
                    case "処理済み":
                        status = ":large_green_circle:処理済み";
                        break;
                }
                str = string.Format("*<https://aws-plus.backlog.jp/view/{0}|{0}>* \n{1}\n状態：{2}\n期日：{3}", item.issuekey,item.summary,status,dueDate);
                message.Add(str);
            }
            if (message.Count == 0) { message.Add("0件"); }

            var attachments = GgetAttachments(message, color);
            var blocks = this.GetBlocks(block_message);
            var slack_message = new JObject
            {
                ["channel"] = _channle,
                ["attachments"] = attachments,
                ["blocks"] = blocks
            };
            return slack_message.ToString();
        }

        private JArray GgetAttachments(List<string> message, string color)
        {
            var list_blocks = MakeMessageBlock(message);
            var blocks = JArray.FromObject(list_blocks);
            return new JArray
                {
                    new JObject
                    {
                        ["color"]=color,
                        ["blocks"]=blocks
                    }
                };
        }

        private List<JObject> MakeMessageBlock(List<string> message)
        {
            List<JObject> j_events = new List<JObject>();
            foreach(var item in message)
            {
                j_events.Add(GetPlaintextSection(item));
            }
            return j_events;
        }

        private JObject GetPlaintextSection(string message)
        {
            return new JObject
            {
                ["type"] = "section",
                ["text"] = new JObject
                {
                    ["type"] = "mrkdwn",
                    ["text"] = message
                }
            };
        }

        private JArray GetBlocks(string message)
        {
            return new JArray
            {
                new JObject{
                    ["type"]="section",
                    ["text"]=new JObject
                    {
                        ["type"]="plain_text",
                        ["text"]=message
                    }
                }
            };
        }
    }

}
