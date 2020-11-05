using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Text.RegularExpressions;

public partial class Program : MyGridProgram
{
    //======-SCRIPT BEGINING-======

    /// <summary>OS NELBRUS class.</summary>
    sealed partial class NLB : SdSubPCmd
    {
        #region Properties
        /// <summary>Reference to Program class to get access to it`s functions. Example: OS.P.Me.</summary>
        public Program P { get; private set; }
        /// <summary>Reference to Grid Terminal System of this Program to get access to it`s functions. Example: OS.GTS.GetBlockWithName().</summary>
        public IMyGridTerminalSystem GTS { get; private set; }
        /// <summary>Initialised subprograms.</summary>
        public List<InitSubP> InitSP { get; private set; }
        /// <summary>ID for new subprogram to start.</summary>
        public ushort K { get; private set; }
        /// <summary>Started subprograms. Mean [id, subprogram].</summary>
        Dictionary<ushort, SdSubP> SP; // todo public?
        /// <summary>Started subprograms to close indexes.</summary>
        List<ushort> SP2C = new List<ushort>();
        /// <summary>Internal time measurement unit.</summary>
        public uint Tick { get; private set; }
        /// <summary>Echo controller.</summary>
        public EchoController EchoCtrl { get; private set; }
        #endregion Properties

        public NLB() : base(0, "NELBRUS", new MyVersion(0, 5, 0, new DateTime(2020, 11, 5)), "Your operation system.")
        {
            InitSP = new List<InitSubP>();
            Tick = 0;
            SP = new Dictionary<ushort, SdSubP>() { { 0, this } };
            K = 1;
        }

        #region Methods
        /// <summary>This method used to initialize OS in RSG stage. Do not use it for other.</summary>
        public void Ready(Program p)
        {
            if (P != null) return; // The method will be run once
            P = p;
            GTS = P.GridTerminalSystem;
            SetCmd(new Dictionary<string, Cmd>
            {
                { "start", new Cmd(CmdRun, "Start initialized subprogram by id.", "/start <id> - Start new subprogram, check id by /isp.") },
                { "stop", new Cmd(CmdStop, "Stop runned subprogram by id.", "/stop <id> - Stop subprogram, check id by /sp.") },
                { "sp", new Cmd(CmdSP, "View runned subprograms or run the subprogram command.", "/sp - View runned subprograms;\n/sp <id> - View runned subprogram information;\n/sp <id> <command> [arguments] - Run the subprogram command.") },
                { "isp", new Cmd(CmdISP, "View initilized subprograms information.", "/isp - View initilized subprograms;\n/isp <id> - View initilized subprogram information.") },
                { "clr", new Cmd(CmdClearC, "Clearing the command interface.") }, // todo переделать под отправителя
            });
            if (EchoCtrl == null) EchoCtrl = new SEcho(); // Initialize default echo controller
            P.Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        /// <summary>Initialize other echo controller. Use it between Ready and Go methods.</summary>
        /// <param name="c">New custom echo controller.</param>
        public void SetEchoCtrl(SEcho c) // 
        {
            if (!CSP<SEcho>()) EchoCtrl = c; // The method will be run when Echo Controller is not runned
        }
        /// <summary>This method used to initialize OS in RSG stage. Do not use it for other.</summary>
        public void Go()
        {
            if (CSP<SEcho>()) return; // The method will be run once
            // todo Run only runned the last time subprograms
            SP.Add(1, EchoCtrl);
            for (int i = 0; i < InitSP.Count; i++) RSP(InitSP[i]); // Run all initialized subprograms
        }
        /// <summary>Nobody cant stop it. :P</summary>
        public override bool MayStop() { return false; }
        /// <summary>Initialise new subprogram.</summary>
        public void ISP(InitSubP p)
        {
            if (!InitSP.Contains(p)) InitSP.Add(p);
        }
        /// <summary>Returns true if subprogram of T type is currently started. Example: OS.CSP<NELBRUS>().</summary> 
        public bool CSP<T>() where T : SdSubP
        {
            //todo check
            foreach (var i in SP.Values)
            {
                if (i is T) return true;
            }
            return false;
        }
        /// <summary>Returns true if subprogram of this type is currently started. Example: OS.CSP(OS).</summary>
        public bool CSP<T>(T p) where T : SdSubP { return CSP<T>(); }
        /// <summary>Run new subprogram.</summary>
        public SdSubP RSP(InitSubP p)
        {
            unchecked
            {
                while (SP.ContainsKey(K)) K++;
                var t = p.Start(K);
                if (t != null)
                {
                    if (string.IsNullOrEmpty(t.TerminateMsg))
                    {
                        SP.Add(K++, t);
                        return t;
                    }
                    else
                        EchoCtrl.CShow($"Subprogram {t.Name} can not start by cause:\n{t.TerminateMsg}");
                }
                return null;
            }
        }
        /// <summary>Stop subprogram. Returns true if subprogram successfully stopped.</summary>
        public bool SSP(SdSubP p)
        {
            //&& SP.Remove(SP.FirstOrDefault(x => x.Value == p).Key
            if ((!string.IsNullOrEmpty(p.TerminateMsg) || p.MayStop()) && SP.ContainsKey(p.ID) && p == SP[p.ID] && !SP2C.Contains(p.ID))
            {
                if (!string.IsNullOrEmpty(p.TerminateMsg))
                    EchoCtrl.CShow($"Subprogram ID:{p.ID} \"{p.Name}\" terminated by cause:\n{p.TerminateMsg}");
                SP2C.Add(p.ID);
                return true;
            }
            return false;
        }
        /// <summary>This method used to process running of mother block (this programmable block). Do not use it.</summary>
        /// <param name="a">If a is command, then it should start with '/'.</param>
        public void Main(string a, UpdateType uT)
        {
            switch (uT)
            {
                case UpdateType.Update1:
                    #region Update1
                    foreach (var p in SP.Values) // Iterate runned subprograms
                    { // Do actions
                        p.EAct(); // Do every tick actions
                        if (p.Acts.ContainsKey(Tick))
                        {
                            Dictionary<uint, Act> t = new Dictionary<uint, Act>();
                            Dictionary<uint, Dictionary<uint, Act>> t2 = new Dictionary<uint, Dictionary<uint, Act>>();
                            foreach (uint j in p.Acts[Tick].Keys) // Iterate frequencies
                            {
                                p.Acts[Tick][j](); // Do actions with frequence
                                if (p.Acts.ContainsKey(Tick + j))
                                {
                                    if (p.Acts[Tick + j].ContainsKey(j))
                                        p.Acts[Tick + j][j] += p.Acts[Tick][j];
                                    else
                                        t.Add(j, p.Acts[Tick][j]);
                                }
                                else
                                    t2.Add(j, p.Acts[Tick]);
                            }
                            foreach (var i in t)
                            {
                                p.Acts[Tick + i.Key].Add(i.Key, i.Value);
                            }
                            foreach (var i in t2)
                            {
                                p.Acts.Add(Tick + i.Key, new Dictionary<uint, Act> { { i.Key, i.Value[i.Key] }, });
                            }
                            p.Acts.Remove(Tick); // Remove old
                        }
                        if (p.DefA.ContainsKey(Tick)) // Deferred Actions
                        {
                            p.DefA[Tick]();
                            p.DefA.Remove(Tick);
                        }
                        p.UpdActions();
                    }
                    // Close started subprograms
                    foreach (var i in SP2C)
                        SP.Remove(i);
                    SP2C.Clear();
                    unchecked { Tick++; }
                    break;
                #endregion Update1
                case UpdateType.Update10:
                    P.Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    break;
                case UpdateType.Update100:
                    goto case UpdateType.Update10;
                default:
                    if (a.StartsWith("/"))
                        EchoCtrl.CShow($"> {Cmd(a.Substring(1), CmdR)}");
                    break;
            }
        }
        public int GetCountISP() { return OS.InitSP.Count; }
        public int GetCountRSP() { return OS.SP.Count; }
        ///<summary>Run parsed console command.</summary>
        /// <param name="r">Command registry.</param>
        /// <param name="n">Command name.</param>
        /// <param name="a">Console command arguments.</param>
        /// <returns></returns>
        public static string Cmd(Dictionary<string, Cmd> r, string n, List<string> a) // todo sender
        {
            Cmd c; // Tip: the ad can not be embedded in C# 6.0
            return r.TryGetValue(n, out c) ? c.C(a) : $"Command {F.Brckt(n)} not found. {mTUH}";
        }
        /// <summary>Run single line console command.</summary>
        /// <param name="s">Single line console command.</param>
        /// <param name="r">Command registry.</param>
        public static string Cmd(string s, Dictionary<string, Cmd> r) // todo sender
        {
            string n, m;
            List<string> a;
            CP(s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList(), out n, out a);
            if ((m = Cmd(r, n, a)) == null || string.IsNullOrEmpty(m))
                return "Done.";
            return m;
        }
        /// <summary>Command parse. Returns command name and arguments. Arguments marked as "separated words" (in quotation marks) will be considered as a single argument.</summary>
        /// <param name="s">Splitted words of command.</param>
        /// <param name="n">Command name.</param>
        /// <param name="a">Command arguments.</param>
        static public void CP(List<string> s, out string n, out List<string> a)
        {
            a = new List<string>();
            if (s.Count == 0) { n = ""; return; }
            var f = false;
            for (int i = 0; i < s.Count; i++)
            {
                if (f)
                    if (f = !s[i].EndsWith("\"")) a[a.Count - 1] += $" {s[i]}";
                    else a[a.Count - 1] += $" {s[i].Remove(s[i].Length - 1)}";
                else
                    if (f = s[i].StartsWith("\"")) a.Add(s[i].Substring(1));
                else a.Add(s[i]);
            }
            n = a[0];
            a.RemoveAt(0);
        }
        #endregion Methods

        #region Commands
        string CmdRun(List<string> a)
        {
            ushort i;
            if (a.Count() > 0 && ushort.TryParse(a[0], out i))
                if (InitSP.Count > i)
                    return OS.RSP(InitSP[i]) == null ? $"Attempt to run new subprogram {F.Brckt(InitSP[i].Name)} failed." : $"New subprogram {F.Brckt(InitSP[i].Name)} runned.";
                else return $"Initialized subprogram with id [{i}] not exist. {mTUH}";
            else return mAE;
        }
        string CmdStop(List<string> a)
        {
            ushort i;
            if (a.Count() > 0 && ushort.TryParse(a[0], out i))
                if (SP.ContainsKey(i))
                {
                    string r = $"Subprogram {F.Brckt(SP[i].Name)} ";
                    return r + (SSP(SP[i]) ? "successfully stopped." : $"can`t be stopped now.");
                }
                else return $"Subprogram with id [{i}] not exist. {mTUH}";
            else return mAE;
        }
        string CmdSP(List<string> a)
        {
            if (a.Count == 0)
            {
                var r = "Runned subprograms [id - info]: ";
                foreach (var i in SP.Keys) r += $"\n{i} - {F.SPI(SP[i])}";
                return r;
            }
            ushort k;
            if (ushort.TryParse(a[0], out k))
                if (SP.ContainsKey(k))
                    if (a.Count == 1) return $"Runned subprogram information:\n{F.SPI(SP[k], true)}";
                    else return SP[k] is SdSubPCmd ? Cmd((SP[k] as SdSubPCmd).CmdR, a[1], a.GetRange(2, a.Count - 2)) : $"Subprogram {SP[k].Name} does not support commands.";
                else return $"Subprogram with id [{k}] not exist.";
            else return mAE;
        }
        string CmdISP(List<string> a)
        {
            if (a.Count == 0)
            {
                var r = "Initialized subprograms:";
                for (int i = 0; i < InitSP.Count; i++) r += $"\n{i} - {F.SPI(InitSP[i])}";
                return r;
            }
            ushort k;
            if (ushort.TryParse(a[0], out k))
                if (InitSP.Count > k) return $"Initialized subprogram information:\n{F.SPI(InitSP[k], true)}";
                else return $"Initialized subprogram with id [{k}] not exist.";
            else return mAE;
        }
        string CmdClearC(List<string> a) { EchoCtrl.CClr(); return " "; } // todo переделать под отправителя
        #endregion Commands

        /// #INSERT EchoController
        /// #INSERT SEcho
        /// #INSERT F
    }

    //======-SCRIPT ENDING-======
}