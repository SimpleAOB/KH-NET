using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KongHackTrainer
{
    [ObfuscationAttribute(Exclude = true)]
    public class GAME
    {
        public IntPtr pid;
        public string name;


        public GAME(IntPtr p, string n)
        {
            pid = p;
            name = n;
        }
        public GAME()
        {

        }
    }
    [ObfuscationAttribute(Exclude = true)]
    public class Hack
    {
        public GAME game { get; set; }
        public Int64 Hid { get; set; }
        public int votes { get; set; }
        public char type { get; set; }
        public string HackName { get; set; }
        public string Description { get; set; }
        public string HackListItemID { get; set; }
        public string[] Services { get; protected set; }

        /// <summary>
        /// the number of individual hacks that make up this hack
        /// </summary>
        public int HackCount;

        public Hack()
        {
            game = new GAME();
            HackCount = 0;
        }

        public Hack(char type, string name, IntPtr p, string n, string[] services) : this()
        {
            HackName = name;
            Services = services;
            this.type = type;
            game.name = n;
            game.pid = p;
        }

        internal void setgameInfo(IntPtr p, string n)
        {
            game.name = n;
            game.pid = p;
        }
    }
}
