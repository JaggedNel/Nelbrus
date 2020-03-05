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

public sealed class Program : MyGridProgram {
    //======-НАЧАЛО СКРИПТА-======

    Program()
    {
        OS.Ready(this);

        //SetEchoCtrl(new CEcho()) // Place to initialize custom echo controller
        OS.ISP(new JNTicker());
        OS.ISP(new JNTimer());
        OS.ISP(new JNLST());

        OS.Go();
    } 

    #region Core zone
    /// <summary>Operation System NELBRUS instance.</summary>
    readonly static NLB OS = new NLB(); // Initializing OS

    void Save()
    {

    }
    void Main(string arg, UpdateType uT)
    {
        OS.Main(arg, uT);
    }

    #region Global properties
    /// <summary>Not available message.</summary>
    public const string NA = "N/A";
    /// <summary>Action.</summary>
    delegate void Act();
    /// <summary>Request without arguments.</summary>
    delegate string Req();
    /// <summary>String request with string arguments used for commands.</summary>
    /// <returns>Answer of the executed command.</returns>
    delegate string ReqA(List<string> a);
    /// <summary>Integer request without arguments.</summary>
    delegate int ReqI();
    /// <summary>Custom Action used to do it later or with frequency.</summary>
    struct CAct
    {
        public readonly uint ID;
        public readonly Act Act;

        /// <summary>New Custom Action.</summary>
        /// <param name="a">Action.</param>
        public CAct(uint id, Act a) { ID = id; Act = a; }
    }
    /// <summary>Command.</summary>
    struct Cmd
    {
        /// <summary>Command request.</summary>
        public ReqA C { get; }
        /// <summary>Help info.</summary>
        public string H { get; set; }
        /// <summary>Details.</summary>
        public string D { get; set; }

        /// <param name="c">Command request.</param>
        /// <param name="h">Help info.</param>
        /// <param name="d">Details.</param>
        public Cmd(ReqA c, string h, string d) { C = c; H = h; D = d; }
        /// <param name="c">Command request.</param>
        /// <param name="h">Help info.</param>
        public Cmd(ReqA c, string h) : this(c, h, NA) { }
        /// <param name="c">Command request.</param>
        public Cmd(ReqA c) : this(c, NA) { }
    }
    /// <summary>Help message.</summary>
    public const string mTUH = "Try use /help to fix your problem.";
    /// <summary>Argument exception message.</summary>
    public const string mAE = "Argument exception. " + mTUH;

    /// <summary>Represents the version number of an component or complex. Example: 2.0.3-[23.12.2019].</summary>
    class MyVersion
    {
        /// <summary>Version Generation</summary>
        public byte G { get; }
        /// <summary>Version Edition.</summary>
        public byte M { get; }
        /// <summary>Version Revision.</summary>
        public byte R { get; }
        /// <summary>Version Date.</summary>
        public DateTime D { get; }

        public MyVersion(byte M, byte m) { G = M; this.M = m; R = 0; D = DateTime.MinValue; }
        public MyVersion(byte M, byte m, DateTime d) : this(M, m) { D = d; }
        public MyVersion(byte M, byte m, byte r) : this(M, m) { R = r; }
        public MyVersion(byte M, byte m, byte r, DateTime d) : this(M, m, r) { D = d; }

        public static implicit operator string(MyVersion value)
        { 
            if (value == null) return null;
            var res = value.R == 0 ? $"{value.G}.{value.M}" : $"{value.G}.{value.M}.{value.R}";
            return value.D != DateTime.MinValue ? res + $"-[{NLB.F.Date(value.D)}]" : res;
        }
        public static implicit operator MyVersion(string version)
        {
            int i;
            var date = DateTime.MinValue;
            if ((i = version.IndexOf("-")) > 0 && i < version.Count())
            {
                var h = version.Substring(i + 1);
                if (!DateTime.TryParseExact(h, "[dd.MM.yyyy]", new System.Globalization.CultureInfo("de-De"), System.Globalization.DateTimeStyles.None, out date)) throw new ArgumentException($"Variable is not a version (date) type: {h}.");
                version = version.Remove(i);
            }
            var v = version.Split('.');
            var w = Array.ConvertAll(v, Byte.Parse);
            if (w.Count() < 2 || w.Count() > 3) throw new ArgumentException($"Variable is not a version type.");
            return new MyVersion(w[0], w[1], w.Count() > 2 ? w[2] : (byte)0, date);
        }
    }
    /// <summary>Basic subprogram class.</summary>
    abstract class SubP
    {
        public string Name { get; protected set; }
        public MyVersion V { get; }
        public string Info { get; protected set; }
        
        public SubP(string name, MyVersion v = null, string info = "Description " + NA + ".") { Name = name; V = v; Info = info; }
        public SubP(string name, string info) { Name = name; V = null; Info = info; }

        /// <summary>Run new subprogram.</summary>
        /// <param name="id">Identificator of new subprogram.</param>
        /// <returns>Started subprogram.</returns>
        public virtual SdSubP Start(ushort id) { return null; }
    }
    /// <summary>Basic started subprogram.</summary>
    class SdSubP : SubP 
    {
        public readonly ushort ID;
        /// <summary>Time when subprogram started.</summary>
        public readonly DateTime ST;
        /// <summary>Every tick actions.</summary>
        public Act EAct { get; private set; }
        /// <summary>Actions with frequency registry. Mean [tick, [frequency, actions]].</summary>
        public Dictionary<uint, Dictionary<uint, Act>> Acts { get; private set; }
        /// <summary>Deffered Actions registry. Mean [tick, actions].</summary>
        public Dictionary<uint, Act> DefA { get; private set; }
        /// <summary>Action Adress used to find it in registry.</summary>
        struct Ad
        {
            /// <summary>When Started</summary>
            public uint S { get; } 
            /// <summary>Action Frequency</summary>
            public uint F { get; }

            /// <summary>New action Adress</summary>
            /// <param name="s">Time when started</param>
            /// <param name="f">Frequency of action</param>
            public Ad(uint s, uint f) { S = s; F = f; }
        }
        /// <summary>Key for new action.</summary>
        uint AK;
        /// <summary>Actions Archive. Mean [id, action adress].</summary>
        Dictionary<uint, Ad> AA;

        public SdSubP(ushort id, string name, MyVersion v = null, string info = NA) : base(name, v, info) {
            ID = id;
            ST = DateTime.Now;
            EAct = delegate { };
            Acts = new Dictionary<uint, Dictionary<uint, Act>>();
            DefA = new Dictionary<uint, Act>();
            AK = 1;
            AA = new Dictionary<uint, Ad>();
        }
        public SdSubP(ushort id, string name, string info) : this(id, name, null, info) { }
        /// <summary>Used by NELBRUS in start to run new subprogram.</summary>
        public SdSubP(ushort id, SubP p) : this(id, p.Name, p.V, p.Info) { }

        #region Actions management
        /// <summary>Add new action running with frequency freq and that will be runned first time with tick span. Returns custom action to manage it.</summary>
        protected CAct AddAct(Act act, uint freq, uint span = 0)
        {
            if (freq < 2) // Zero regarded like one
            {
                EAct += act;
                AA.Add(AK, new Ad(OS.Tick, 1));
            }
            else
            {
                uint t; // When starts
                if (span == 0)
                {
                    t = OS.Tick + freq;
                    act();
                }
                else t = OS.Tick + span; 
                if (!Acts.ContainsKey(t)) Acts.Add(t, new Dictionary<uint, Act>() { { freq, act } });
                else if (!Acts[t].ContainsKey(freq)) Acts[t].Add(freq, act);
                else Acts[t][freq] += act;
                AA.Add(AK, new Ad(t, freq));
            }
            return new CAct(AK++, act);
        }
        /// <summary>Remove action called with frequency.</summary>
        protected void RemAct(ref CAct a)
        {
            if (AA.ContainsKey(a.ID))
            {
                if (AA[a.ID].F == 1) EAct -= a.Act;
                else
                {
                    var temp = OS.Tick < AA[a.ID].S ? AA[a.ID].S : (OS.Tick - AA[a.ID].S) % AA[a.ID].F == 0 && Acts.ContainsKey(OS.Tick) && Acts[OS.Tick].ContainsKey(AA[a.ID].F) ? OS.Tick : ((OS.Tick - AA[a.ID].S) / AA[a.ID].F + 1) * AA[a.ID].F + AA[a.ID].S; // Intercept of the next run-time
                    Acts[temp][AA[a.ID].F] -= a.Act;
                    if (Acts[temp][AA[a.ID].F] == null)
                    {
                        if (Acts[temp].Count() == 1) Acts.Remove(temp);
                        else Acts[temp].Remove(AA[a.ID].F);
                    }
                }
                AA.Remove(a.ID);
            }
            a = new CAct(); // Removed actions have default id value 0
        }
        /// <summary>Change action called with frequency.</summary>
        /// <param name="a">Editable action.</param>
        /// <param name="freq">New frequency.</param>
        /// <param name="span">Span to first run in ticks.</param>
        protected CAct ChaAct(CAct a, uint freq, uint span = 0)
        {
            var t = AddAct(a.Act, freq, span);
            RemAct(ref a);
            return t;
        }
        /// <summary>Add new deferred action that will run after time span.</summary>
        protected CAct AddDefA(Act act, uint span)
        {
            if (DefA.ContainsKey(OS.Tick + span)) DefA[OS.Tick + span] += act;
            else DefA.Add(OS.Tick + span, act);
            return new CAct(OS.Tick + span, act);
        }
        /// <summary>Remove deferred action.</summary>
        protected void RemDefA(ref CAct a)
        {
            if (DefA.ContainsKey(a.ID))
            {
                DefA[a.ID] -= a.Act;
                if (DefA[a.ID] == null) DefA.Remove(a.ID);
            }
            a = new CAct(); // Removed actions have default id value 0
        }
        #endregion Actions management

        /// <summary>Stop started subprogram.</summary>
        public virtual void Stop() { OS.SSP(this); }
        /// <summary>Returns true to let OS stop this subprogram. WARNING: Do not forget stop child subprograms there too.</summary>
        public virtual bool MayStop() { return true; }
    }
    /// <summary>Started subprogram with console commands support.</summary>
    class SdSubPCmd : SdSubP
    {
        /// <summary>Command registry.</summary>
        public Dictionary<string, Cmd> CmdR { get; private set; }

        public SdSubPCmd(ushort id, string name, MyVersion v = null, string info = NA) : base(id, name, v, info)
        {
            CmdR = new Dictionary<string, Cmd> { { "help", new Cmd(CmdHelp, "View commands help.", "/help - show available commands;\n/help <command> - show command information.") } };
        }
        public SdSubPCmd(ushort id, string name, string info) : this(id, name, null, info) { }
        /// <summary>Used by NELBRUS in start method to run new subprogram.</summary>
        public SdSubPCmd(ushort id, SubP p) : this(id, p.Name, p.V, p.Info) { }

        /// <summary>Set new console command.</summary>
        /// <param name="n">Command name.</param>
        /// <param name="c">Method of command.</param>
        public void SetCmd(string n, Cmd c) { CmdR.Add(n, c); }
        /// <summary>Set collection of new console commands.</summary>
        /// <param name="c">Collection.</param>
        public void SetCmd(Dictionary<string, Cmd> c) { foreach(var i in c) { CmdR.Add(i.Key, i.Value); } }

        #region Default commands
        string CmdHelp(List<string> a)
        {
            var r = new StringBuilder();
            if (a.Count() == 0)
            {
                r.Append("Available commands:");
                foreach (var i in CmdR) r.Append($"\n{NLB.F.Brckt(i.Key)} - {i.Value.H}");
            }
            else return CmdR.ContainsKey(a[0]) ? $"{NLB.F.Brckt(a[0])} - {CmdR[a[0]].H}\nDetails:\n{CmdR[a[0]].D}" : $"Command {NLB.F.Brckt(a[0])} not found. {mTUH}";
            return r.ToString();
        }
        #endregion Default commands
    }
    #endregion Global properties

    /// <summary>OS NELBRUS class.</summary>
    sealed class NLB : SdSubPCmd
    {
        #region Properties
        /// <summary>Reference to Program class to get access to it`s functions. Example: OS.P.Me.</summary>
        public Program P { get; private set; }
        /// <summary>Reference to Grid Terminal System of this Program to get access to it`s functions. Example: OS.GTS.GetBlockWithName().</summary>
        public IMyGridTerminalSystem GTS { get; private set; }
        /// <summary>Initialised subprograms.</summary>
        public List<SubP> InitSP { get; private set; }
        /// <summary>ID for new subprogram to start.</summary>
        public ushort K { get; private set; }
        /// <summary>Started subprograms. Mean [id, subprogram].</summary>
        Dictionary<ushort, SdSubP> SP; // todo public?
        /// <summary>Internal time measurement unit.</summary>
        public uint Tick { get; private set; }
        /// <summary>Echo controller.</summary>
        public EchoController EchoCtrl { get; private set; }
        #endregion Properties

        public NLB() : base(0, "NELBRUS", new MyVersion(0, 3, 10, new DateTime(2020, 03, 05)), "Your operation system.")
        {
            InitSP = new List<SubP>();
            Tick = 0;
            SP = new Dictionary<ushort, SdSubP>() { { 0, this } };
            K = 1;
            EchoCtrl = new CEcho(); // Initialize default echo controller
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
                { "start", new Cmd(CmdRun, "Start initialized subprogram by id.", "/start <id> - start new subprogram, check id by /isp.") },
                { "stop", new Cmd(CmdStop, "Stop runned subprogram by id.", "/stop <id> - stop subprogram, check id by /sp.") },
                { "sp", new Cmd(CmdSP, "View runned subprograms or run the subprogram command.", "/sp - view runned subprograms;\n/sp <id> - view runned subprogram information;\n/sp <id> <command> [arguments] - run the subprogram command.") },
                { "isp", new Cmd(CmdISP, "View initilized subprograms information.", "/isp - view initilized subprograms;\n/isp <id> - view initilized subprogram information.") },
                { "clr", new Cmd(CmdClearC, "Clearing the command interface.") }, // todo переделать под отправителя
            });
            P.Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        /// <summary>Initialize other echo controller. Use it between Ready and Go methods.</summary>
        /// <param name="c">New custom echo controller.</param>
        public void SetEchoCtrl(CEcho c) // 
        {
            if (CSP<CEcho>()) return; // The method will be run when Echo Controller is not runned
            EchoCtrl = c;
        }
        /// <summary>This method used to initialize OS in RSG stage. Do not use it for other.</summary>
        public void Go()
        {
            if (CSP<CEcho>()) return; // The method will be run once
            RSP(EchoCtrl); // Run echo controller
            for (int i = 0; i < InitSP.Count; i++) RSP(InitSP[i]); // Run all initialized subprograms
        }
        /// <summary>Nobody cant stop it. :P</summary>
        public override bool MayStop() { return false; }
        /// <summary>Initialise new subprogram. Use it between Ready and Go methods.</summary>
        public void ISP(SubP p)
        {
            if (!InitSP.Contains(p)) InitSP.Add(p);
        }
        /// <summary>Returns true if subprogram of T type is currently started. Example: OS.CSP<NELBRUS>().</summary> //todo check
        public bool CSP<T>()where T : SdSubP
        {
            foreach (var i in SP.Values)
            {
                if (i is T) return true;
            }
            return false;
        } 
        /// <summary>Returns true if subprogram of this type is currently started. Example: OS.CSP(OS).</summary>
        public bool CSP<T>(T p) where T : SdSubP { return CSP<T>(); }
        /// <summary>Run new subprogram.</summary>
        public SdSubP RSP(SubP p)
        {
            unchecked {
                while (SP.ContainsKey(K)) K++;
                var t = p.Start(K);
                if (t != null)
                {
                    SP.Add(K++, t);
                    return t;
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>Stop subprogram. Returns true if subprogram successfully stopped.</summary>
        public bool SSP(SdSubP p)
        {
            if (p.MayStop() && p == SP[p.ID]) return SP.Remove(SP.FirstOrDefault(x => x.Value == p).Key);
            return false;
        }
        /// <summary>This method used to process running of mother block (this programmable block). Do not use it.</summary>
        /// <param name="a">If a is command, then it should start with '/'.</param>
        public void Main(string a, UpdateType uT)
        {
            switch (uT)
            {
                case UpdateType.Update1: // todo refactoring
                    #region Update1
                    var x = new List<ushort>();
                    foreach (ushort i in SP.Keys) x.Add(i);
                    foreach (ushort i in x) // Iterate runned subprograms
                    { // Do actions
                        SP[i].EAct(); // Do every tick actions
                        if (SP.ContainsKey(i) && SP[i].Acts.ContainsKey(Tick))
                        {
                            List<uint> y = new List<uint>();
                            foreach (uint j in SP[i].Acts[Tick].Keys) y.Add(j);
                            foreach (uint j in y) // Iterate frequencies
                            {
                                SP[i].Acts[Tick][j](); // Do actions with frequence
                                if (SP.ContainsKey(i) && SP[i].Acts.ContainsKey(Tick) && SP[i].Acts[Tick].ContainsKey(j) && SP[i].Acts[Tick][j] != null)
                                {
                                    if (SP[i].Acts.ContainsKey(Tick + j)) SP[i].Acts[Tick + j].Add(j, SP[i].Acts[Tick][j]); // Move
                                    else SP[i].Acts.Add(Tick + j, new Dictionary<uint, Act>() { { j, SP[i].Acts[Tick][j] } }); // Move
                                    SP[i].Acts.Remove(Tick); // Remove old
                                }
                            }
                        }
                        if (SP.ContainsKey(i) && SP[i].DefA.ContainsKey(Tick))
                        {
                            SP[i].DefA[Tick]();
                            if (SP.ContainsKey(i) && SP[i].DefA.ContainsKey(Tick)) SP[i].DefA.Remove(Tick);
                        }
                    }
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
            if ((m = Cmd(r, n, a)) == null || m == "")
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
        string CmdClearC(List<string> a) { EchoCtrl.CClr(); return null; } // todo переделать под отправителя
        #endregion Commands

        public abstract class EchoController : SdSubP
        {
            /// <summary>Duration of the custom information show.</summary>
            protected uint DT { get; set; }

            public EchoController(string n, MyVersion v = null, string i = NA) : base(1, n, v, i) { }

            /// <summary>Echo controller works with NELBRUS. Do not stop it.</summary>
            public override bool MayStop() { return false; }
            /// <summary>Refresh information at echo.</summary>
            public virtual void Refresh()
            {
                OS.P.Echo("OS NELBRUS and used echo controller working but not configured.");
            }
            /// <summary>Show custom info at echo for t ticks.</summary>
            public virtual void CShow(string s) { }
            /// <summary>Remove custom info in echo.</summary>
            public virtual void CClr() { }
        }
        public class CEcho : EchoController
        {
            /// <summary>Fields to write at echo. Mean [id, field (string, integer, request, StringBuilder ...)].</summary>
            protected Dictionary<byte, object[]> Fields;
            /// <summary>Operation indicator.</summary>
            protected Ind OInd;
            /// <summary>Refresh action.</summary>
            CAct R;
            /// <summary>Custom information remover.</summary>
            CAct C;

            public CEcho() : base("Custom echo controller") { }

            public override SdSubP Start(ushort id)
            {
                OInd = new Ind(0, 30, new string[] { "(._.)", "   ( l: )", "      (.–.)", "         ( :l )", "            (._.)" });
                Fields = new Dictionary<byte, object[]> {
                    { 0, new object[] { $"OS NELBRUS v.{(string)OS.V}\nIs worked ", (Req)OInd.Get , "\nInitialized subprograms: ", (ReqI)OS.GetCountISP, "\nRunned subprograms: ", (ReqI)OS.GetCountRSP } },
                    { 1, new object[] { } }
                };
                R = AddAct(Refresh + (Act)OInd.Update, 30, 1);
                DT = F.TTT(45);
                return this;
            }
            /// <summary>Refresh information at echo.</summary>
            public override void Refresh()
            {
                var t = new StringBuilder();
                foreach (var f in Fields.Values)
                {
                    for(int i = 0; i < f.Count(); i++) t.Append(f[i] is Req ? ((Req)f[i])() : f[i] is ReqI ? ((ReqI)f[i])().ToString() : f[i].ToString());
                    if (f.Count() != 0) t.Append('\n');
                }
                OS.P.Echo(t.ToString());
            }
            /// <summary>Show custom info at echo for t ticks.</summary>
            public override void CShow(string i)
            {
                RemDefA(ref C);
                Fields[1] = new string[] { i };
                C = AddDefA(CClr, DT);
            }
            /// <summary>Remove custom info in echo.</summary>
            public override void CClr()
            {
                RemDefA(ref C);
                Fields[1] = new string[] { };
            }
        }
        /// <summary>Help functions.</summary>
        public abstract class F
        {
            /// <summary>Returns text with chosen symbols on edges.</summary>
            /// <param name="t">Editable text.</param>
            /// <param name="b">Used brackets.</param>
            public static string Brckt(string t, char b = '[')
            {
                switch (b)
                {
                    case '\0': return t;
                    case '[': return $"[{t}]";
                    case '{': return $"{{{t}}}";
                    case '<': return $"({t})";
                    case '(': return $"({t})";
                    default: return $"{b}{t}{b}";
                }
            }
            public static string Date(DateTime dt) { return dt.ToString("dd.MM.yyyy"); }
            public static string Time(DateTime dt) { return dt.ToString("HH:mm:ss"); }
            public static string CurTime() { return Time(DateTime.Now); }
            /// <summary>Time to ticks.</summary>
            /// <param name="s">Seconds.</param>
            /// <param name="m">Minutes.</param>
            /// <param name="h">Hours.</param>
            public static uint TTT(byte s, byte m = 0, byte h = 0) { return (uint)(s + m * 60 + h * 3600) * 60; }
            /// <summary>Ticks to Time</summary>
            public static string TTT(uint t)
            {
                return $"{(int)(t / 3600)}:{(int)(t / 60) - (int)(t / 3600) * 60}:{(t - ((int)(t / 60) * 60)) * 10 / 6}";
            }
            /// <summary>Get subprogram information.</summary>
            /// <param name="p">Subprogram.</param>
            /// <param name="i">Get advanced information.</param>
            public static string SPI (SubP p, bool i = false)
            {
                string r = p.V == null ? $"{Brckt(p.Name)}" : $"{Brckt(p.Name)} v.{(string)p.V}";
                return i ? r + $"\n{p.Info}{(p is SdSubP ? $"\nWas launched at [{(p as SdSubP).ST.ToString()}].\nCommands support: {p is SdSubPCmd}." : "")}" : r;
            }
    }
    }
    /// <summary>Indicator automat. Iterates through the indicator variants in order.</summary>
    class Ind
    {
        /// <summary>Current variant.</summary>
        protected byte CV;
        /// <summary>Frequency.</summary>
        protected byte F;
        /// <summary>Indicator variants</summary>
        protected string[] Vrnts;
        /// <summary>Iterator.</summary>
        protected int i;
        
        /// <param name="fv">Number of first indicator.</param>
        /// <param name="f">Frequency.</param>
        /// <param name="v">Variants.</param>
        public Ind(byte fv, byte f, string[] v)
        {
            CV = fv;
            F = f;
            Vrnts = v;
            i = 1;
        }
        
        public virtual void Update()
        {
            CV += (byte)i;
            if (CV >= Vrnts.Length - 1 || CV <= 0) i = -i;
        }
        public virtual string Get() { return Vrnts[CV]; }
    }
    #endregion Core zone

    #region Test Subprograms
    class JNTicker : SubP
    {
        public JNTicker() : base("Ticker", new MyVersion(1, 0), "First subprogram for NELBRUS system. This subprogram takes LCD and show current tick in it.") { } // Used for initialisation of subprogram

        public override SdSubP Start(ushort id) { return new TP(id, this); }
        
        class TP /* This Program */ : SdSubPCmd
        {
            IMyTextPanel LCD { get; }
            CAct Act; // Show current tick on text panel

            public TP(ushort id, SubP p) : base(id, p)
            {
                if((LCD = OS.P.GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel) == null)
                {
                    AddDefA(Stop, 1);
                    return;
                }
                Act = AddAct(Show, 20);
                SetCmd(new Dictionary<string, Cmd>
                {
                    { "pause", new Cmd(CmdPause, "Pause show current tick.") },
                    { "play", new Cmd(CmdPlay, "Continue show current tick.") 
}
                });
                //AddPenA(MyPause, 201); // Example
            }

            void Show()
            {
                LCD.WriteText(OS.Tick.ToString());
                //Act = ChaAct(Act, 50, false); // Example
            }
            void MePause() { RemAct(ref Act); }
            void MePlay() { if (Act.ID == 0) Act = AddAct(Show, 20); }

            #region Commands
            string CmdPause(List<string> a) { MePause(); return null; }
            string CmdPlay(List<string> a) { MePlay(); return null; }
            #endregion Commands
        }
    }

    class JNTimer : SubP
    {
        public JNTimer() : base("Timer", "Shows the elapsed time in LCD when using the command ss.") { }

        public override SdSubP Start(ushort id) { return new TP(id, this); }

        class TP : SdSubPCmd
        {
            IMyTextPanel LCD;
            uint start;
            bool s = false;
            CAct S;

            public TP(ushort id, SubP p) : base(id, p)
            {
                if ((LCD = OS.P.GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel) == null)
                {
                    AddDefA(Stop, 1);
                    return;
                }
                SetCmd(new Dictionary<string, Cmd>
                {
                    { "ss", new Cmd(CmdSS, "Start/stop timer") }
                });
            }

            void Show()
            {
                LCD.WriteText((OS.Tick - start).ToString());
            }

            #region Commands
            string CmdSS(List<string> a) {
                if (s)
                {
                    LCD.WriteText(NLB.F.TTT(OS.Tick - start));
                    RemAct(ref S);
                }
                else
                {
                    start = OS.Tick;
                    S = AddAct(Show, 10);
                }
                s = !s;
                return "";
            }
            #endregion Commands
        }
    }

    class JNLST /* Lite Solar Tracking*/ : SubP
    {
        public JNLST() : base("Lite Solar Tracking", new MyVersion(1, 0), "Simplified sctipt Lite Solar Tracking by Elfi Wolfe ported by JN") { }

        public override SdSubP Start(ushort id) // Only one instance of the subprogram will be started
        {
            if (OS.CSP<TP>()) return null;
            else return new TP(id, this);
        }
        
        public class TP /* This program*/ : SdSubP
        {
            float SolarAlign = 0.04f; // Solar Align
            List<IMyMotorStator> Rotors = new List<IMyMotorStator>(); // Rotors
            List<IMySolarPanel> Panels = new List<IMySolarPanel>(); // Panels
            List<IMyOxygenFarm> Farms = new List<IMyOxygenFarm>(); // Farms
            List<SolarArray> SolarArrays = new List<SolarArray>(); // Solar Arrays
            CAct MA; // Main Action
            static IMyTextPanel lcd = OS.P.GridTerminalSystem.GetBlockWithName("lcd") as IMyTextPanel; // todo remove

            public TP(ushort id, SubP p) : base(id, p)
            {
                MA = AddDefA(Ready, 60 * 5); // five seconds span
            }

            public void Ready()
            {
                BuildSolarArrays();
                MA = AddAct(Main, 100);
            }
            public void Main()
            {
                for (int i = 0; i < SolarArrays.Count; ++i)
                {
                    if (!SolarArrays[i].Update())
                    {
                        BuildSolarArrays();
                        break;
                    }
                }
            }
            void BuildSolarArrays()
            {
                SolarArrays.Clear();
                OS.GTS.GetBlocksOfType(Rotors);
                foreach (var v in Rotors)
                {
                    if (!v.IsSameConstructAs(OS.P.Me)) continue;
                    OS.GTS.GetBlocksOfType(Panels, b => b.CubeGrid == v.TopGrid);
                    OS.GTS.GetBlocksOfType(Farms, b => b.CubeGrid == v.TopGrid);
                    if (Panels.Count > 0 || Farms.Count > 0)
                    {
                        SolarArrays.Add(new SolarArray(v, Panels, Farms, SolarAlign));
                    }
                }
            }

            public class SolarArray
            {
                public IMyMotorStator Rotor { get; set; }
                public List<IMySolarPanel> Panels { get; set; }
                public List<IMyOxygenFarm> Farms { get; set; }
                public float SpeedSetting { get; set; } // RPM
                public float Power { get; set; } // MW
                public float PowerOld { get; set; }
                public float Oxygen { get; set; }
                public float OxygenOld { get; set; }
                public float Direction { get; set; }

                public SolarArray(IMyMotorStator r, List<IMySolarPanel> p, List<IMyOxygenFarm> f, float ss)
                {
                    Rotor = r;
                    Panels = new List<IMySolarPanel>(p);
                    SpeedSetting = ss;
                    Power = 0f;
                    PowerOld = 0f;
                    Direction = 1f;
                    Farms = new List<IMyOxygenFarm>(f);
                    Oxygen = 0f;
                    OxygenOld = 0f;
                }
                public SolarArray(IMyMotorStator r, List<IMySolarPanel> p, float ss) : this(r, p, new List<IMyOxygenFarm>(), ss) { }

                public bool Closed(IMyTerminalBlock b)
                {
                    if (b == null || b.WorldMatrix == MatrixD.Identity) return true;
                    return false;
                }
                public bool Update()
                {
                    if (Closed(Rotor)) return false;
                    PowerOld = Power;
                    Power = 0f;
                    OxygenOld = Oxygen;
                    Oxygen = 0f;
                    foreach(var v in Panels)
                    {
                        if (Closed(v)) return false;
                        Power += v.MaxOutput;
                    }
                    foreach (var v in Farms)
                    {
                        if (Closed(v)) return false;
                        Oxygen += v.GetOutput();
                    }
                    float current = Power;
                    float old = PowerOld;
                    if (Panels.Count == 0 && Farms.Count > 0)
                    {
                        current = Oxygen;
                        old = OxygenOld;
                    }
                    if (current == old)
                    {
                        if (!(Rotor.TargetVelocityRPM == 0)) // Moving, power == old = Stop
                        {
                            Direction = Rotor.TargetVelocityRPM < 0 ? -1 : 1;
                            Rotor.TargetVelocityRPM = 0f;
                        }
                    }
                    else if (!(Rotor.TargetVelocityRPM == 0) && current < old)
                    {
                        Rotor.TargetVelocityRPM = -Rotor.TargetVelocityRPM; // Moving, power < old = reverse direction
                    }
                    else if(Rotor.TargetVelocityRPM == 0)
                    {
                        Rotor.TargetVelocityRPM = Direction * SpeedSetting; // Not moving, P < old, P > old = start moving
                    }
                    return true;
                }

            }
        }
    }
    #endregion Test subprograms

    //======-КОНЕЦ СКРИПТА-======
}