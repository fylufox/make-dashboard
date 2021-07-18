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
    public class Myaccount : GoogleCalendar
    {
        protected UserCredential _credential;

        public Myaccount(string app_name, Access scorp, string jsonpath) : base(app_name, scorp, jsonpath) { }

        override protected void Authenticate()  //Authenticate for API Method
        {
            string credPath = Path.GetFullPath(Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location));
            credPath = Path.Combine(Path.GetDirectoryName(_cretifiation_path), ".credentials/quickstart.json");

            using (var stream =
               new FileStream(_cretifiation_path, FileMode.Open, FileAccess.Read))
            {    //Read json file
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    _scope,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }
            _service = new CalendarService(new BaseClientService.Initializer()
            {    //Request using apis
                HttpClientInitializer = _credential,
                ApplicationName = _application_name,
            });
        }

        /// <summary>
        /// 再認証を行う
        /// </summary>
        public void ReAuthenticate()
        {
            GoogleWebAuthorizationBroker.ReauthorizeAsync(_credential, CancellationToken.None);
            _service = new CalendarService(new BaseClientService.Initializer()
            {    //Request using apis
                HttpClientInitializer = _credential,
                ApplicationName = _application_name,
            });
        }

        /// <summary>
        /// イベント用のカラーリストを取得する
        /// カラーIDは配列のインテックス＋１
        /// </summary>
        /// <returns>カラーリスト</returns>
        public string[,] GetColorList()
        {
            Colors color = _service.Colors.Get().Execute();
            string[,] replay = new string[color.Event__.Count, 2];
            int i = 0;
            foreach (var item in color.Event__)
            {
                replay[i, 0] = item.Value.Background;
                replay[i, 1] = item.Value.Foreground;
                i++;
            }
            return replay;
        }
    }
}
