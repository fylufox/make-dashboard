using System;
using System.Threading;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace make_dashboard.GoogleCalendarAccess
{

    public class Serviceaccount:GoogleCalendar
    {
        protected ICredential _credential;

        public Serviceaccount(string app_name, Access scorp, string jsonpath) : base(app_name, scorp, jsonpath) { }

        override protected void Authenticate()  //Authenticate for API Method
        {
            using (var stream = new FileStream(_cretifiation_path, FileMode.Open, FileAccess.Read))
            {
                _credential = GoogleCredential.FromStream(stream)
                     .CreateScoped(_scope).UnderlyingCredential;
            }

            _service = new CalendarService(new BaseClientService.Initializer()
            {    //Request using apis
                HttpClientInitializer = _credential,
                ApplicationName = _application_name,
            });
        }
    }
}
