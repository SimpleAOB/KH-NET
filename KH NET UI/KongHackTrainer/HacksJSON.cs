using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KongHackTrainer
{
    [ObfuscationAttribute(Exclude = true)]
    public class HacksJSON
    {
        //private bool _success;
        //public bool success { get { return _success; } set { _success = "true".Equals(value) ? true : false; } }
        public bool success { get; set; }
        public string message { get; set; }
        public string game_name { get; set; }
        public string game_image { get; set; }
        public string game_desc { get; set; }
        public char game_platform { get; set; }
        public HackJSON[] aobs { get; set; }
        public Int64 other_count { get; set; }
        public string game_url { get; set; }

    }

    [ObfuscationAttribute(Exclude = true)]
    public class HackJSON
    {
        public Int64 hid { get; set; }
        public char type { get; set; }
        public int votes { get; set; }
        public string hack_title { get; set; }
        public string hack_desc { get; set; }
        public string[] services { get; set; }
        public AoBJSON[] aob { get; set; }
        public string script { get; set; }
    }

    [ObfuscationAttribute(Exclude = true)]
    public class AoBJSON
    {
        public string B { get; set; } //Original (Before)
        public string A { get; set; } //Change (After)
        bool _Applied = false;
        public bool Applied
        {
            get
            {
                return _Applied;
            }
            private set
            {
                _Applied = value;
            }
        }
    }

}
