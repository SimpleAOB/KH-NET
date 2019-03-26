using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KH_NET_UI.KHScriptMemoryServices;

namespace KH_NET_UI.KHScript
{
    class KHScript
    {
        //https://konghack.com/page/25-konghack-script
        //http://wiki.cheatengine.org/index.php?title=Cheat_Engine:Auto_Assembler
        //Implement DEFINE?. Names are replaced with given input. Whitespace not stripped

        /// <summary>
        /// Holds all scripts that have been enabled at least once
        /// </summary>
        Dictionary<long, KHScriptContainer> KHScripts = new Dictionary<long, KHScriptContainer>();

        int TargetHandle;
        public KHScript(int _TargetID) { TargetHandle = _TargetID; }
        
        public void AddScript(long hid, string Input)
        {
            KHScriptContainer KHSC = new KHScriptContainer();
            if (!KHSC.ParseScript(Input))
            {
                throw new Exception("KHScript::Parse Failed");
            } else
            {
                KHScripts.Add(hid, KHSC);
            }
        }
        
        public bool DoEnable(long hid)
        {
            KHScriptContainer KHSC = KHScripts[hid];
            if (KHSC == null) return false;
            else
            {
                return KHSC.ExecuteEnable(TargetHandle);
            }
        }
        public bool DoDisable(long hid)
        {
            KHScriptContainer KHSC = KHScripts[hid];
            if (KHSC == null) return false;
            else
            {
                return KHSC.ExecuteDisable(TargetHandle);
            }
        }
        public bool CheckEnableStatus(long hid)
        {
            return (KHScripts[hid]).IsEnabled;
        }

        private class KHScriptContainer
        {
            Dictionary<string, object> SymbolContainer = new Dictionary<string, object>();
            string PreBlock;
            string EnableBlock;
            string DisableBlock;
            public bool IsEnabled { get; private set; } = false;

            public string LastError { get; private set; } = "";

            public KHScriptContainer() { }

            /// <summary>
            /// Tries to add a symbol to the SymbolContainer. Returns false if symbol already exists
            /// </summary>
            /// <param name="SymbolName"></param>
            /// <param name="SymbolData"></param>
            /// <returns></returns>
            private bool ReplaceSymbolData(string SymbolName, object SymbolData)
            {
                if (SymbolContainer.ContainsKey(SymbolName))
                    SymbolContainer[SymbolName] = SymbolData;
                else
                    SymbolContainer.Add(SymbolName, SymbolData);
                return true;
            }
            /// <summary>
            /// Retrieves Symbol Data. Returns false if symbol does not exist. Returns true if data retrieval was a success
            /// </summary>
            /// <param name="SymbolName">Symbol Name</param>
            /// <param name="DataOut">Variable to pass the data to</param>
            /// <returns></returns>
            private bool GetSymbolData(string SymbolName, out object DataOut)
            {
                if (SymbolContainer.ContainsKey(SymbolName))
                    DataOut = SymbolContainer[SymbolName];
                else
                {
                    DataOut = null;
                    return false;
                }
                return true;
            }
            /// <summary>
            /// Pre-execution parsing. Removes comments and separates blocks. Does not do error checking
            /// </summary>
            /// <param name="ScriptText"></param>
            /// <returns></returns>
            public bool ParseScript(string ScriptText)
            {
                string NoComments = StripComments(ScriptText);
                string[] Blocks = GetBlocks(NoComments);
                if (Blocks != null)
                {
                    PreBlock = Blocks[0];
                    EnableBlock = Blocks[1];
                    DisableBlock = Blocks[2];
                } else
                    return false;
                return true;
            }
            private string StripComments(string Input)
            {
                string MultilineCommentPattern = @"[^}]+(?![^{]*\})";
                Regex RMC = new Regex(MultilineCommentPattern);
                MatchCollection MC = RMC.Matches(Input);

                string NoMultiline = "";
                foreach (Match g in MC)
                {
                    NoMultiline += g.Value;
                }

                string InlineCommentPattern = "(//.+?)\\r\\n";
                Regex RIC = new Regex(InlineCommentPattern);
                string Output = RIC.Replace(NoMultiline, "");
                return Output;
            }
            private string[] GetBlocks(string Input)
            {
                string[] Blocks = null;
                if (Input.IndexOf("[ENABLE]") != -1)
                {
                    if (Input.IndexOf("[DISABLE]") != -1)
                    {
                        Blocks = new string[3];
                        //PreEnable Block
                        Blocks[0] = CleanBlock(Input.Substring(0, Input.IndexOf("[ENABLE]")).Trim(new char[] { }));
                        //Enable Block
                        Blocks[1] = CleanBlock(Input.Substring(Input.IndexOf("[ENABLE]") + 8, (Input.Length - Input.IndexOf("[ENABLE]")) - (Input.Length - Input.IndexOf("[DISABLE]")) - 8));
                        //Disable Block
                        Blocks[2] = CleanBlock(Input.Substring(Input.IndexOf("[DISABLE]") + 9));
                    }
                }

                return Blocks;
            }
            public bool ExecuteEnable(int _TargetID)
            {
                if (ExecuteBlock(_TargetID, PreBlock) && ExecuteBlock(_TargetID, EnableBlock))
                {
                    IsEnabled = true;
                    return true;
                } else
                {
                    IsEnabled = false;
                    return false;
                }
            }
            public bool ExecuteDisable(int _TargetID)
            {
                if (ExecuteBlock(_TargetID, PreBlock) && ExecuteBlock(_TargetID, DisableBlock))
                {
                    IsEnabled = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            private bool ExecuteBlock(int _TargetID, string BlockText)
            {
                if (BlockText.Length == 0) return true;

                string AOBSCANPattern = @"^aobscan\((.+)\)$";
                string READMEMPattern = @"^readmem\((.+)\)$";
                string WRITEPattern   = @"(.+)\:$";

                Regex RASP = new Regex(AOBSCANPattern);
                Regex RRMP = new Regex(READMEMPattern);
                Regex RWP = new Regex(WRITEPattern);

                string[] Lines = BlockText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                for (int i = 0; i < Lines.Length; i++)
                {
                    string CurrentLine = Lines[i];
                    Match MASP = RASP.Match(CurrentLine);
                    Match MWP = RWP.Match(CurrentLine);
                    if (MASP.Success)
                    {
                        string ParenthsisPattern = @"\((.+),(.+)\)";
                        Regex Reg = new Regex(ParenthsisPattern);
                        Match MP = Reg.Match(MASP.Value);
                        if (MP.Groups.Count != 3)
                        {
                            return false;
                        }
                        else
                        {
                            string SymbolName = MP.Groups[1].Value.Trim();
                            string ByteArgument = MP.Groups[2].Value.Trim();
                            if (CheckAOBString(ByteArgument))
                            {
                                long[] Results;
                                if (ExecuteAOBSCAN(_TargetID, ByteArgument, out Results))
                                {
                                    ReplaceSymbolData(SymbolName, Results);
                                } else
                                {
                                    return false;
                                }
                            }
                        }
                    } else if (MWP.Success)
                    {
                        string NextLine = Lines[i + 1];
                        Match MRMP = RRMP.Match(NextLine);
                        if (MRMP.Success)
                        {
                            string[] MRMPParts = MRMP.Groups[0].Value.Split(',');
                            object SymbolData;
                            byte[] BytesToWrite;
                            if (GetSymbolData(MRMPParts[0], out SymbolData))
                            {

                            } else
                            {
                                try
                                {
                                    long Address = Convert.ToInt64(MRMPParts[0]);
                                    int ReadLength = Convert.ToInt32(MRMPParts[1]);
                                    KHScriptMemoryHelper MH = new KHScriptMemoryHelper();
                                    
                                }
                                catch (FormatException) { return false; }
                            }
                        }
                        else if (CheckAOBString(NextLine))
                        {
                            //Write these bytes to the address(s) found in symbol.
                            string SymbolText = MWP.Groups[0].Value;
                            string SymbolName = "";
                            int SymbolAddressOffset = 0; 
                            if (SymbolText.IndexOf('+') != -1)
                            {
                                string[] SymbolParts = SymbolText.Split('+');
                                SymbolName = SymbolParts[0];
                                for (int j = 1; j < SymbolParts.Length; j++)
                                {
                                    try
                                    {
                                        SymbolAddressOffset += (int)(Convert.ToByte(SymbolParts[j], 16));
                                    } catch (FormatException e) { Console.WriteLine("Argument is not valid"); return false; }
                                }
                            }
                            object SymbolData;
                            if (!GetSymbolData(SymbolName, out SymbolData))
                                return false;
                            else
                            {
                                KHScriptMemoryHelper MemoryService = new KHScriptMemoryHelper();
                                foreach (long addr in (long[])SymbolData)
                                {
                                    Console.WriteLine(MemoryService.WriteBytes(_TargetID, addr+SymbolAddressOffset, NextLine));
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                        i++;
                    }
                    else
                    {
                        throw new Exception("Invalid script line:" + CurrentLine);
                    }
                }
                return true;
            }
            private bool ExecuteAOBSCAN(int _TargetID, string Bytes, out long[] ResultsArray)
            {
                
                KHScriptMemoryHelper MemoryService = new KHScriptMemoryHelper();
                List<long> Results = MemoryService.PatternScan(_TargetID, Bytes);
                ResultsArray = Results.ToArray();
                return true;
            }
            private bool ExecuteREADMEM(IntPtr _TargetHandle, long AddressLocation, long length)
            {

                return true;
            }
            private bool CheckAOBString(string input)
            {
                string[] SplitAOB = input.Split(' ');
                for (int i = 0; i < SplitAOB.Length; i++)
                {
                    if (SplitAOB[i] == "??") continue;
                    try
                    {
                        Convert.ToInt32(SplitAOB[i], 16);
                        if (SplitAOB[i].Length != 2) return false;
                    } catch (FormatException)
                    {
                        return false;
                    }
                }
                return true;
            }
            /// <summary>
            /// Removes empty new lines
            /// </summary>
            /// <param name="BlockText"></param>
            /// <returns></returns>
            private string CleanBlock(string BlockText)
            {
                return String.Join("\r\n",
                    (BlockText.Split(new[] { "\r\n" }, StringSplitOptions.None)
                    .Where(x => x.Length > 0)));
            }
        }
    }
}
