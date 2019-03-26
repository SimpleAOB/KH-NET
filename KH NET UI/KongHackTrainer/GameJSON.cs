using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KongHackTrainer
{
    [ObfuscationAttribute(Exclude = true)]
    internal class GameSearchJSON
    {
        public GameJSON[] Games { get; set; }
    }

    [ObfuscationAttribute(Exclude = true)]
    internal class GameJSON
    {
        private string _image;
        private string _desc;
        private string _name;
        public int id { get; set; }
        public string name
        {
            get { return _name; }
            set
            {
                _name = System.Web.HttpUtility.HtmlDecode(value);
            }
        }
        public string desc
        {
            get { return _desc; }
            set
            {
                _desc = System.Web.HttpUtility.HtmlDecode(value);
            }
        }
        public string image
        {
            get { return _image; }
            set
            {
                _image = System.Web.HttpUtility.HtmlDecode(value);
            }
        }
        public string created { get; set; }
    }
}
