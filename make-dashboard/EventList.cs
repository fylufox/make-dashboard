using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using make_dashboard.GoogleCalendarAccess;

namespace make_dashboard
{
    class EventList
    {
        const string HOLIDAY_CALENDAR_ID = "ja.japanese#holiday@group.v.calendar.google.com";

        public enum Participation
        {
            needsAction,    //未回答
            declined,       //不参加
            tentative,      //未定
            accepted        //参加
        }

        public struct CalendarEvent
        {
            public string Title;
            public string Location;
            public DateTime start;
            public DateTime end;
            public bool Allday;
            public Participation Participation;
            public string link;
        }

        private List<CalendarEvent> _acceptedEvents;
        private List<CalendarEvent> _undecidedEvents;
        private bool _notify;

        public List<CalendarEvent> AcceptedEvents {
            get { return _acceptedEvents; }
        }

        public List<CalendarEvent> UndecidedEvents {
            get { return _undecidedEvents; }
        }

        public bool notify {
            get { return _notify; }
        }

        public EventList(string email, string jsonpath)
        {
            var today = DateTime.Now.Date;
            //today = new DateTime(2022, 11, 16);
            _acceptedEvents = new List<CalendarEvent>();
            _undecidedEvents = new List<CalendarEvent>();
            _notify = false;

            if (today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday) { return; }

            var service = new Serviceaccount("time-dashboard", GoogleCalendar.Access.READONLY, jsonpath);
            Serviceaccount.ReadingRequest request = new Serviceaccount.ReadingRequest
            {
                calendar_id = HOLIDAY_CALENDAR_ID,
                enabled_filter = true,
                start_filter = today,
                end_filter = today.AddDays(1)
            };
            var holiday = service.GetEventList(request);
            if (holiday.Length != 0) { return; }
            else { _notify = true; }

            request.calendar_id = email;
            var events = service.GetEventList(request);
            foreach (var item in events)
            {
                if (Regex.IsMatch(item.summary, ".*有給.*"))
                {
                    _notify = false;
                    Console.WriteLine("Today is paid holiday!");
                    break;
                }
                CalendarEvent calendarEvent = new CalendarEvent();
                calendarEvent.Title = item.summary;
                calendarEvent.Location = item.location;
                calendarEvent.start = item.start;
                calendarEvent.end = item.end;
                calendarEvent.Allday = item.dateonly;
                calendarEvent.link = item.link;
                if (item.participation == null)
                {
                    calendarEvent.Participation = Participation.accepted;
                }
                else
                {
                    calendarEvent.Participation = (Participation)Enum.Parse(typeof(Participation), item.participation, true);
                }

                if (calendarEvent.Participation == Participation.accepted)
                {
                    _acceptedEvents.Add(calendarEvent);
                }
                else if (calendarEvent.Participation == Participation.declined)
                {

                }
                else
                {
                    _undecidedEvents.Add(calendarEvent);
                }
            }
        }
    }
}
