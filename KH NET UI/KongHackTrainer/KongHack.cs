using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Reflection;

namespace KongHackTrainer
{
    [ObfuscationAttribute(Exclude = true)]
    internal static class KongHack
    {


        //#if DEBUG        
        //public static readonly string loginUrl = "https://konghack.com/khut/login";
        //public static readonly string HackUrl = "https://konghack.com/khut/hacks";
        //public static readonly string SearchUrl = "https://konghack.com/games_legacy/game_search_json.php?request_type=advanced&term=";
        ////public static readonly string SearchUrl = "https://dev.konghack.com/khut/search?request_type=advanced&term=";
        //public static readonly string KeepAliveURL = "https://konghack.com/keep_alive.php";
        //public static readonly string VoteUrl = "https://konghack.com/hack_vote.php";
        //public static readonly string CrashUrl = "https://konghack.com/khut/crash-report";
        //public static readonly string UCPUrl = "https://konghack.com/ucp/";
        //public static readonly string PointsUrl = "https://konghack.com/ucp/points/store";
        //#else
        public static readonly string loginUrl = "https://konghack.com/khut/login";
        public static readonly string HackUrl = "https://konghack.com/khut/hacks";
        public static readonly string SearchUrl = "https://konghack.com/games_legacy/game_search_json.php?request_type=advanced&term=";
        public static readonly string KeepAliveURL = "https://konghack.com/keep_alive.php";
        public static readonly string VoteUrl = "https://konghack.com/hack_vote.php";
        public static readonly string CrashUrl = "https://konghack.com/khut/crash-report";
        public static readonly string UCPUrl = "https://konghack.com/ucp/";
        public static readonly string PointsUrl = "https://konghack.com/ucp/points/store";
        //#endif

        public static readonly string useragent = "KH-NET";
        public static readonly string preg_loginReturn = @".authenticated.:(\w+),.username.:.(\w+).,.success.:(\w+)";



        private static readonly string preg_cookieCleaner = @"(?:path=/\S*?|expires=\w{3}, \d\d-\w{3,}-\d{4} \d\d:\d\d:\d\d GMT|HttpOnly)[;,]|expires=\w{3}, \d\d-\w{3,}-\d{4} \d\d:\d\d:\d\d GMT|kong_flash_messages=\S*?;|\w+?=;|Max-Age=\d+;\s*|domain=.konghack.com";
        // private static readonly string sessionCookieName = "auth";
        private static readonly string ErrorTitle = "Critical HTTP Error!";

        internal static bool LoginRequest(string uname, string upass)
        {

            bool ret = false;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(loginUrl);
            request.KeepAlive = true;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Referer = "http://konghack.com/";
            request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.UserAgent = useragent;
            request.Timeout = 1200000;
            request.Method = "POST";

            request.ProtocolVersion = HttpVersion.Version11;
            request.AllowAutoRedirect = false;

            string curver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            string postData = string.Format("username={0}&password={1}&version={2}", uname, System.Web.HttpUtility.UrlEncode(upass), curver);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);//new ASCIIEncoding().GetBytes(postData);

            foreach (byte b in postData)
            {
                System.Diagnostics.Debug.Write((char)b);
            }
            System.Diagnostics.Debug.WriteLine(" ");

            request.ContentType = "application/x-www-form-urlencoded";

            request.ContentLength = byteArray.Length;


            try
            {
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                ServicePointManager.ServerCertificateValidationCallback = delegate
                {
                    return true;
                };
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(LoginJson));


                using (Stream stream = response.GetResponseStream())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        byte[] buffer = new byte[16 * 1024];
                        byte[] data;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        data = ms.ToArray();
                        ms.Seek(0, SeekOrigin.Begin);

                        try
                        {
                            LoginJson lj = (LoginJson)js.ReadObject(ms);
                            if (lj.Result)
                            {
                                KHUserInfo.SetInfoFromJson(lj);
                                //set the returned cookie as an application-scope var so we can use it in other windows
                                KHUserInfo.Cookies = CleanCookies(response.Headers.Get("Set-Cookie"));
                                ret = true;
                                var c2 = response.Headers.Get("Set-Cookie");
                                var c = KHUserInfo.Cookies;
                            }
                            else
                            {
                                ret = false;
                            }
                        }
                        catch (Exception err)
                        {
                            System.Diagnostics.Trace.WriteLine("login failed:: " + err.Message);
                            string exception_extradata = "HTTP Response:: " + response.StatusCode + " HTTP Response Length::" + response.ContentLength + " HTTP Response::" + System.Text.Encoding.UTF8.GetString(data);
                            //ExceptionHandling.LogException(err, exception_extradata);
                            ret = false;
                        }
                    }

                    //using (StreamReader reader = new StreamReader(stream))
                    //{
                    //    string str = reader.ReadToEnd();
                    //    string str2 = response.Headers.ToString();
                    //    reader.Close();
                    //    response.Close();
                    //    stream.Close();
                    //    System.Diagnostics.Debug.WriteLine("LOGIN RESPONSE" + Environment.NewLine + str);

                    //    ret = str;

                    //    System.Diagnostics.Debug.WriteLine("COOKIES LOGIN RESPONSE" + Environment.NewLine);
                    //    //set the returned cookie as an application-scope var so we can use it in other windows
                    //    Application.Current.Properties[KongHack.COOKIE_KEY] = CleanCookies(response.Headers.Get("Set-Cookie"));

                    //    System.Diagnostics.Debug.WriteLine(response.Headers.Get("Set-Cookie"));
                    //    System.Diagnostics.Debug.WriteLine(ret);

                    //}
                }
                return ret;
            }
            catch (Exception err)
            {
                MessageBox.Show("Login Error:: " + Environment.NewLine + err.Message, "Login error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
        }

        public static string HackRequestByID(int id)
        {
            string curver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return HackRequest(string.Format("id={0}&version={1}", id, curver));
        }

        public static string HackRequestByName(string name)
        {
            string curver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return HackRequest(string.Format("name={0}&version={1}", System.Web.HttpUtility.UrlEncode(name), curver));
        }

        public static string HackRequestByUrl(string url)
        {
            string curver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return HackRequest(string.Format("url={0}&version={1}", System.Web.HttpUtility.UrlEncode(url), curver));
        }

        private static string HackRequest(string requestdata)
        {

            string ret = string.Empty;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HackUrl);
                request.KeepAlive = true;
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Referer = "http://konghack.com/";
                request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.UserAgent = useragent;

                request.Headers.Add("Cookie", KHUserInfo.Cookies);

                request.Timeout = 1200000;
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.AllowAutoRedirect = false;

                byte[] byteArray = Encoding.UTF8.GetBytes(requestdata);

                request.ContentType = "application/x-www-form-urlencoded";

                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                ServicePointManager.ServerCertificateValidationCallback = delegate
                {
                    return true;
                };
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                byte[] data;
                using (Stream stream = response.GetResponseStream())
                {
                    //read the stream as a memory stream
                    byte[] buffer = new byte[16 * 1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        data = ms.ToArray();
                    }


                    if (data[0] == 0x7b)
                    {
                        //dont decrypt
                    }
                    else
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = (byte)~data[i];
                            byte x = data[i];
                            byte y = (byte)~data[i];
                        }
                    }
                    string str = Encoding.UTF8.GetString(data, 0, data.Length);
                    //str = str.Substring(0, str.Length - 1);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("HACK REQUEST RESPONSE" + Environment.NewLine + str);
#endif
                    str = str.Replace("\"aob\":{", "\"aob\":[");
                    str = str.Replace("}}}", "}]}");
                    str = Regex.Replace(str, "\"\\d+\":", "");
                    str = str.Replace("}},\"script\":", "}],\"script\":");
                    str = str.Replace("\"aob\":\"\",", "");
                    ret = str;


                }
            }
            catch (Exception)
            {

            }
            return ret;
        }


        internal static string GameSearchRequest(string gamename)
        {

            string ret = string.Empty;

            try
            {

                //gamename = Regex.Replace(gamename, @"[^A-Za-z0-9]", "%25");
                //gamename = Regex.Replace(gamename, @"%%+", "%");
                //gamename = Regex.Replace(gamename, @"%25(%25)+", "%25");
                gamename = Regex.Replace(gamename, @"[^A-Za-z0-9]", "%");
                gamename = Regex.Replace(gamename, @"%%+", "%");
                gamename = Regex.Replace(gamename, @"%%+", "%");
                string curver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string url = string.Format("{0}{1}&version={2}", SearchUrl, System.Web.HttpUtility.UrlEncode(gamename), curver);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = true;
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Referer = "http://konghack.com/";
                request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.UserAgent = useragent;

                request.Headers.Add("Cookie", KHUserInfo.Cookies);

                request.Timeout = 1200000;
                request.Method = "GET";
                request.ProtocolVersion = HttpVersion.Version11;
                request.AllowAutoRedirect = false;

                request.ContentType = "text/html";


                ServicePointManager.ServerCertificateValidationCallback = delegate
                {
                    return true;
                };
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string str = reader.ReadToEnd();
                        string str2 = response.Headers.ToString();
                        reader.Close();
                        response.Close();
                        stream.Close();
                        System.Diagnostics.Debug.WriteLine("SEARCH REQUEST RESPONSE" + Environment.NewLine + str);
                        //str = Regex.Replace(str, "\"id\":(?<id>\\d+),", @"""id"":""${id}"",");
                        //ret = "\"Games\":"+str;
                        ret = str;

                        System.Diagnostics.Debug.WriteLine(ret);
                    }
                }
            }
            catch (Exception)
            {

            }
            return ret;
        }

        internal static string ErrorReport(string ErrorJson)
        {
            string ret = string.Empty;


            try
            {
                string curver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var uri = new Uri(CrashUrl);
                var p = System.Net.ServicePointManager.FindServicePoint(uri);
                p.Expect100Continue = false;
                HttpWebRequest request = HttpWebRequest.Create(uri) as HttpWebRequest;
                request.KeepAlive = true;
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Referer = "http://konghack.com/";
                request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "application/json,text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.UserAgent = useragent;

                request.Headers.Add("Cookie", KHUserInfo.Cookies);

                request.Timeout = 1200000;
                request.Method = "POST";

                request.ProtocolVersion = HttpVersion.Version11;
                request.AllowAutoRedirect = false;
                request.ContentType = "application/json";

                string postData = ErrorJson;// string.Format("report={0}&version={1}", ErrorJson, curver);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);//new ASCIIEncoding().GetBytes(postData);

                //foreach (byte b in postData)
                //{
                //    System.Diagnostics.Debug.Write((char)b);
                //} System.Diagnostics.Debug.WriteLine(" ");

                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                request.ContentType = "application/json";


                ServicePointManager.ServerCertificateValidationCallback = delegate
                {
                    return true;
                };
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string str = reader.ReadToEnd();
                        string str2 = response.Headers.ToString();
                        ret = response.StatusCode.ToString();
                        reader.Close();
                        response.Close();
                        stream.Close();
                        System.Diagnostics.Trace.WriteLine("ERROR REPORT REQUEST RESPONSE" + Environment.NewLine + str);
                        //str = Regex.Replace(str, "\"id\":(?<id>\\d+),", @"""id"":""${id}"",");
                        //ret = "\"Games\":"+str;                        

                        System.Diagnostics.Trace.WriteLine(ret);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Error sending error report: " + ex.Message);
            }
            return ret;
        }

        internal static string KeepAliveRequest(string _Cookies = "")
        {

            string ret = string.Empty;

            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}?version={1}", KeepAliveURL, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                request.KeepAlive = true;
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Referer = "http://konghack.com/";
                request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.UserAgent = useragent;

                request.Headers.Add("Cookie", (_Cookies == ""? KHUserInfo.Cookies : _Cookies));

                request.Timeout = 1200000;
                request.Method = "GET";
                request.ProtocolVersion = HttpVersion.Version11;
                request.AllowAutoRedirect = false;

                request.ContentType = "text/html";


                ServicePointManager.ServerCertificateValidationCallback = delegate
                {
                    return true;
                };
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(KeepAliveJson));

                using (Stream stream = response.GetResponseStream())
                {
                    try
                    {
                        KeepAliveJson lj = (KeepAliveJson)js.ReadObject(stream);
                        if (lj.load == "")
                        {
                            ret = "false";
                        } else
                        {
                            System.Diagnostics.Trace.WriteLine("KeepAlive::points-> " + lj.points);
                            //update stats i guess
                            KHUserInfo.Points = lj.points;
                            ret = "true";
                        }
                    }
                    catch (Exception err)
                    {
                        System.Diagnostics.Trace.WriteLine("keepalive failed:: " + err.Message);
                        ret = string.Empty;
                    }
                }
            }
            catch (Exception)
            {

            }
            return ret;
        }

        /// <summary>
        /// upvote a hack, this should be asyncronous
        /// </summary>
        /// <param name="hackid">the id of the hack to vote on</param>
        /// <returns></returns>
        internal static void UpVoteRequest(Int64 hackid)
        {
            VoteRequest(hackid, true);
        }
        /// <summary>
        /// down vote a hack, this should be asyncronous
        /// </summary>
        /// <param name="hackid">the id of the hack to vote on</param>
        /// <returns></returns>
        internal static void DownVoteRequest(Int64 hackid)
        {
            VoteRequest(hackid, false);
        }

        /// <summary>
        /// send the vote request to konghack
        /// </summary>
        /// <param name="hackid"></param>
        /// <param name="upvote">true if upvote, false if downvote </param>
        private static string VoteRequest(Int64 hackid, bool upvote)
        {
            string ret = string.Empty;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(VoteUrl);
                request.KeepAlive = true;
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Referer = "http://konghack.com/";
                request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.UserAgent = useragent;

                request.Headers.Add("Cookie", KHUserInfo.Cookies);

                request.Timeout = 1200000;
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.AllowAutoRedirect = false;
                string curver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                byte[] byteArray = Encoding.UTF8.GetBytes(string.Format("direction={0}&hid={1}&version={2}", upvote ? 'U' : 'D', hackid, curver));//new ASCIIEncoding().GetBytes(postData);

                request.ContentType = "application/x-www-form-urlencoded";

                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                ServicePointManager.ServerCertificateValidationCallback = delegate
                {
                    return true;
                };
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string str = reader.ReadToEnd();
                        string str2 = response.Headers.ToString();
                        reader.Close();
                        response.Close();
                        stream.Close();
                        System.Diagnostics.Debug.WriteLine("VOTE REQUEST RESPONSE" + Environment.NewLine + str);
                        ret = str;

                        System.Diagnostics.Debug.WriteLine(ret);
                    }
                }

            }
            catch (Exception)
            {

            }
            return ret;
        }

        internal static void ShowError(string message)
        {
            System.Windows.MessageBox.Show(message, ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }



        private static string CleanCookies(string cookies)
        {
            return Regex.Replace(cookies, preg_cookieCleaner, string.Empty);
        }

        private static HttpWebRequest GetNewRequest(string targetUrl, string cookies)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
            //request.CookieContainer = SessionCookieContainer;
            request.Headers.Add("Cookie", cookies);
            request.AllowAutoRedirect = false;
            return request;
        }
    }//end class

    /// <summary>
    /// this is the user's meta data
    /// </summary>
    [ObfuscationAttribute(Exclude = true)]
    [DataContract]
    public class LoginJson
    {
        [DataMember(Name = "success")]
        private bool _result { get; set; }

        /// <summary>
        /// gets a bool value indicating if the login succeeded
        /// </summary>
        public bool Result
        {
            get
            {
                return _result;
            }
        }

        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "fullname")]
        public string Fullname { get; set; }

        [DataMember(Name = "donation")]
        public float Donation { get; set; }

        [DataMember(Name = "posts")]
        public int Posts { get; set; }

        [DataMember(Name = "elite")]
        public bool Elite { get; set; }

        [DataMember(Name = "l337h4x")]
        public bool Leethax { get; set; }

        [DataMember]
        public double Points { get; set; }
    }

    /// <summary>
    /// this holds the user's data from login
    /// </summary>
    [ObfuscationAttribute(Exclude = true)]
    public static class KHUserInfo
    {
        private static string _Username;
        public static string Username
        {
            get { return _Username; }
            set { _Username = string.IsNullOrWhiteSpace(value) ? _Username : value; }
        }
        private static string _Fullname;
        public static string Fullname
        {
            get { return _Fullname; }
            set { _Fullname = string.IsNullOrWhiteSpace(value) ? _Fullname : value; }
        }
        private static float _Donation;
        public static float Donation
        {
            get { return _Donation; }
            set { _Donation = value; }
        }
        private static int _Posts;
        public static int Posts
        {
            get { return _Posts; }
            set { _Posts = value; }
        }
        private static bool _Elite;
        public static bool Elite
        {
            get { return _Elite; }
            set { _Elite = value != true ? false : true; }
        }
        private static bool _Leethax;
        public static bool Leethax
        {
            get { return _Leethax; }
            set { _Leethax = value != true ? false : true; }
        }
        private static double _Points;
        public static double Points
        {
            get { return _Points; }
            set { _Points = value; }
        }
        private static string _Cookies;
        public static string Cookies
        {
            get { return _Cookies; }
            set { _Cookies = string.IsNullOrWhiteSpace(value) ? _Cookies : value; }
        }

        static KHUserInfo()
        {
            Username = string.Empty;
            Fullname = string.Empty;
            Donation = 0;
            Posts = 0;
            Elite = false;
            Leethax = false;
            Points = 0;
            Cookies = string.Empty;
        }

        public static void SetInfoFromJson(LoginJson j)
        {
            Username = j.Username;
            Fullname = j.Fullname;
            Donation = j.Donation;
            Posts = j.Posts;
            Elite = j.Elite;
            Leethax = j.Leethax;
            Points = j.Points;
        }
        public static void SetInfoFromResources(string _UN, string _FN, float _DN, int _PN, bool _EL, bool _LH, double _Pts)
        {
            Username = _UN;
            Fullname = _FN;
            Donation = _DN;
            Posts = _PN;
            Elite = _EL;
            Leethax = _LH;
            Points = _Pts;
        }
    }

    /// <summary>
    /// data returned by keepalive request
    /// </summary>
    [ObfuscationAttribute(Exclude = true)]
    [DataContract]
    public class KeepAliveJson
    {
        [DataMember]
        public string html { get; set; }

        [DataMember]
        public string load { get; set; }

        [DataMember]
        public int messages { get; set; }

        [DataMember]
        public double points { get; set; }
    }


}