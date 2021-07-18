using System;
using System.Threading;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Resources;
using System.Text.RegularExpressions;

namespace make_dashboard.GoogleCalendarAccess
{
    abstract public class GoogleCalendar
    {
        protected string[] _scope = new string[1];
        protected CalendarService _service;
        protected string _cretifiation_path;   //json path
        protected string _application_name;  //Showed Apprication Name


        public struct ReadingRequest
        {  //EventsResource.ListRequest
            public string calendar_id;
            public bool enabled_filter;
            public DateTime start_filter;
            public DateTime end_filter;
            public int number_of_event;
        }

        public struct CalendarEvent
        {  //v3.Data.Event.items Response 
            public string summary;
            public string id;
            public bool dateonly;
            public DateTime start;
            public DateTime end;
            public string color;
            public string description;
            public string location;
            public string participation;
        }

        public struct Calendarlist
        {    //v3.Data.CalendarList.items Response
            public string summary;
            public string id;
        }

        public enum Access
        {
            FULLACCSESS,
            READONLY
        }

        /// <summary>
        /// GoogleCalendarAPIの簡易的なアクセスを提供します
        /// </summary>
        /// <param name="app_name">ユーザー認証時に表示するアプリケーション名</param>
        /// <param name="scope">カレンダーへのアクセススコープ</param>
        /// <param name="jsonpath">
        /// <para>認証時に必要なJSONファイルのフルパス</para>
        /// <para>JSONファイルの存在するディレクトリ内に認証省略のためのファイルが作成されるため、サブディレクトリの作成を推奨</para>
        /// </param>
        public GoogleCalendar(string app_name, Access scope, string jsonpath)
        {
            _application_name = app_name;
            _cretifiation_path = jsonpath;
            switch (scope)
            {    //definition scope
                case Access.FULLACCSESS:
                    _scope[0] = CalendarService.Scope.Calendar;
                    break;
                case Access.READONLY:
                    _scope[0] = CalendarService.Scope.CalendarReadonly;
                    break;
            }
            Authenticate();
        }

        protected abstract void Authenticate();  //Authenticate for API Method

        /// <summary>
        /// GoogleCalendarからカレンダー内のイベントリストを取得する
        /// </summary>
        /// <returns>カレンダー内のイベント</returns>
        public CalendarEvent[] GetEventList(ReadingRequest reading_request)
        {
            CalendarEvent[] eventlist;
            //Ensure Consistency
            if (reading_request.enabled_filter)
            {  //Filter Consistency
                if (reading_request.start_filter >= reading_request.end_filter)
                {
                    reading_request.enabled_filter = false;
                }
            }
            if (reading_request.number_of_event <= 0 || reading_request.number_of_event > 2500)
            { //Number Obtained Consistency
                reading_request.number_of_event = 250;
            }
            if (reading_request.calendar_id == null)
            { //CalendarID Consistency
                reading_request.calendar_id = "primary";
            }
            //--------------------------
            EventsResource.ListRequest request = _service.Events.List(reading_request.calendar_id);
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime; //Fixed
            request.SingleEvents = true;    //Fixed
            if (reading_request.enabled_filter)
            {  //Filter Settings
                request.TimeMin = reading_request.start_filter;
                request.TimeMax = reading_request.end_filter;
            }
            request.MaxResults = reading_request.number_of_event;

            Events list = request.Execute();    //Request EventsList
            if (list.Items == null && list.Items.Count <= 0) { return null; }    //Check Items
            eventlist = new CalendarEvent[list.Items.Count];   //Redefinition Array
            for (int i = 0; i < list.Items.Count; i++)
            {    //Storage ItemData
                Event item = list.Items[i];

                eventlist[i].summary = item.Summary;
                eventlist[i].id = item.Id;
                eventlist[i].color = item.ColorId;
                eventlist[i].description = item.Description;
                eventlist[i].location = item.Location;
                //DateTime Consistency
                if (item.Start.DateTime != null)
                {
                    eventlist[i].start = DateTime.Parse(item.Start.DateTime.ToString());
                    eventlist[i].end = DateTime.Parse(item.End.DateTime.ToString());
                }
                else
                {
                    eventlist[i].start = DateTime.Parse(item.Start.Date.ToString());
                    eventlist[i].end = DateTime.Parse(item.End.Date.ToString());
                    eventlist[i].dateonly = true;
                }
                if (item.Attendees == null) { continue; }
                foreach(var iitem in item.Attendees)
                {
                    if (iitem.Email != reading_request.calendar_id)
                    {
                        continue;
                    }
                    eventlist[i].participation = iitem.ResponseStatus;
                }
            }
            return eventlist;
        }

        /// <summary>
        /// イベントIDから詳細なイベント情報を取得する
        /// </summary>
        /// <param name="event_id">イベントID</param>
        /// <param name="calendar_id">カレンダーID</param>
        /// <returns>イベントデータ</returns>
        public Event ReadEvent(string event_id, string calendar_id)
        {
            return _service.Events.Get(calendar_id, event_id).Execute();
        }

        /// <summary>
        /// イベントIDから詳細なイベント情報を取得する
        /// </summary>
        /// <param name="event_id">イベントID</param>
        public Event ReadEvent(string event_id)
        {
            return ReadEvent(event_id, "primary");
        }

        /// <summary>
        /// 新しいイベントをGoogleCalendarに追加する
        /// </summary>
        /// <param name="add_item">追加するイベントアイテム</param>
        /// <param name="calendar_id">イベントを追加するカレンダー</param>
        /// <returns>追加したイベント</returns>
        public CalendarEvent AddCalendarEvent(CalendarEvent add_item, string calendar_id)
        {
            Event insert = new Event();
            EventDateTime items_datetime = new EventDateTime();
            insert.Summary = add_item.summary;
            if (add_item.color != null) { insert.ColorId = add_item.color; }
            EventDateTime start = new EventDateTime();
            EventDateTime end = new EventDateTime();
            if (add_item.dateonly == true)
            {
                start.Date = add_item.start.Date.ToString();
                end.Date = add_item.end.Date.ToString();
            }
            else
            {
                start.DateTime = add_item.start;
                end.DateTime = add_item.end;
            }
            insert.Start = start;
            insert.End = end;
            insert.Description = add_item.description;
            insert.Location = add_item.location;
            Event.RemindersData remindersData = new Event.RemindersData();
            remindersData.UseDefault = false;
            insert.Reminders = null;
            add_item.id = _service.Events.Insert(insert, calendar_id).Execute().Id;
            return add_item;
        }

        /// <summary>
        /// 新しいイベントをGoogleCalendarに追加する
        /// </summary>
        /// <param name="add_item">追加するイベントアイテム</param>
        /// <returns>追加したイベント</returns>
        public CalendarEvent AddCalendarEvent(CalendarEvent add_item)
        {
            return AddCalendarEvent(add_item, "primary");
        }

        /// <summary>
        /// 指定したイベントの内容を変更します
        /// </summary>
        /// <param name="calendar_id">変更するイベントが存在するカレンダーのID</param>
        /// <param name="event_id">内容を変更するイベントのID</param>
        /// <param name="new_event">変更内容のイベント</param>
        public void UpdateEvent(string calendar_id, string event_id, CalendarEvent new_event)
        {
            Event apis_event = _service.Events.Get(calendar_id, event_id).Execute();

            apis_event.Summary = new_event.summary;
            if (new_event.color != null) { apis_event.ColorId = new_event.color; }
            if (new_event.dateonly == true)
            {
                apis_event.Start.Date = new_event.start.Date.ToString("yyyy-MM-dd");
                apis_event.Start.DateTime = null;
                apis_event.End.Date = new_event.end.Date.ToString("yyyy-MM-dd");
                apis_event.End.DateTime = null;
            }
            else
            {
                apis_event.Start.DateTime = new_event.start;
                apis_event.Start.Date = null;
                apis_event.End.DateTime = new_event.end;
                apis_event.End.Date = null;
            }
            apis_event.Description = new_event.description;
            apis_event.Location = new_event.location;
            _service.Events.Update(apis_event, calendar_id, event_id).Execute();
        }

        /// <summary>
        /// 指定したイベントの内容を変更します
        /// </summary>
        /// <param name="event_id">内容を変更するイベントのID</param>
        /// <param name="new_event">変更内容のイベント</param>
        public void UpdateEvent(string event_id, CalendarEvent new_event)
        {
            string calendar_id = "primary";
            UpdateEvent(calendar_id, event_id, new_event);
        }

        /// <summary>
        /// 指定したイベントの削除
        /// </summary>
        /// <param name="calendar_id">イベントが存在するカレンダーのID</param>
        /// <param name="event_id">削除するイベントID</param>
        public void DeleteEvent(string calendar_id, string event_id)
        {
            _service.Events.Delete(calendar_id, event_id).Execute();
        }

        /// <summary>
        /// 指定したイベントの削除
        /// </summary>
        /// <param name="event_id">削除するイベントID</param>
        public void DeleteEvent(string event_id)
        {
            string calendar_id = "primary";
            DeleteEvent(calendar_id, event_id);
        }
    }


}
