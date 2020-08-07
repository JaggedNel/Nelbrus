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

/// Nelbrus OS v.0.3.11-[07.08.20]

public class Program : MyGridProgram {
    //======-SCRIPT BEGINING-======

    Program()
    {
        OS.Ready(this);

        //SetEchoCtrl(new CEcho()) // Place to initialize custom echo controller
        OS.ISP(new JNTicker());
        OS.ISP(new JNTimer());
        OS.ISP(new JNSolarTracker());
        OS.ISP(new JNKonturC());

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
    /// <summary>Help message.</summary>
    public const string mTUH = "Try use /help to fix your problem.";
    /// <summary>Argument exception message.</summary>
    public const string mAE = "Argument exception. " + mTUH;

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
        // todo RepAct (Replace Action)
        /// <summary>Add new action triggered by the frequency freq and that will be runned first time with tick span.</summary>
        /// <param name="ca">Action storage in subprogram</param>
        protected void AddAct(ref CAct ca, Act act, uint freq, uint span = 0)
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
            ca = new CAct(AK++, act);
        }
        /// <summary>Remove action triggered by the frequency.</summary>
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
        /// <summary>Change action triggered by the frequency.</summary>
        /// <param name="ca">Editable action storage in subprogram</param>
        /// <param name="freq">New frequency.</param>
        /// <param name="span">Span to first run in ticks.</param>
        protected void ChaAct(ref CAct ca, uint freq, uint span = 0)
        {
            CAct t = new CAct();
            AddAct(ref t, ca.Act, freq, span);
            RemAct(ref ca);
            ca = t;
        }
        /// <summary>Add new deferred action that will run once after time span.</summary>
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
            // todo Run only runned the last time subprograms
            for (int i = 0; i < InitSP.Count; i++) RSP(InitSP[i]); // Run all initialized subprograms
        }
        /// <summary>Nobody cant stop it. :P</summary>
        public override bool MayStop() { return false; }
        /// <summary>Initialise new subprogram. Use it between Ready and Go methods.</summary>
        public void ISP(SubP p)
        {
            if (!InitSP.Contains(p)) InitSP.Add(p);
        }
        /// <summary>Returns true if subprogram of T type is currently started. Example: OS.CSP<NELBRUS>().</summary> 
        public bool CSP<T>()where T : SdSubP //todo check
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
            public uint DT { get; set; }

            public EchoController(string n, MyVersion v = null, string i = NA) : base(1, n, v, i) { }

            /// <summary>Echo controller works with NELBRUS. Do not stop it.</summary>
            public override bool MayStop() { return false; }
            /// <summary>Refresh information at echo.</summary>
            public virtual void Refresh()
            {
                OS.P.Echo("OS NELBRUS and used echo controller working but not configured.");
            }
            /// <summary>Show custom info at echo.</summary>
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
                AddAct(ref R, Refresh + (Act)OInd.Update, 30, 1);
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
            /// <summary>Show custom info at echo.</summary>
            public override void CShow(string s)
            {
                RemDefA(ref C);
                Fields[1] = new string[] { s };
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
            /// <summary>Ticks to Time. Returns string with converted time "hours:minutes:seconds".</summary>
            /// <param name="t">Time in ticks.</param>
            public static string TTT(uint t)
            {
                return $"{(int)(t / 3600)}:{(int)(t / 60) - (int)(t / 3600) * 60}:{(t - ((int)(t / 60) * 60)) * 10 / 6}";
            }
            /// <summary>Ticks to Time.</summary>
            /// <param name="t">Time in ticks.</param>
            /// <param name="s">Seconds.</param>
            /// <param name="m">Minutes</param>
            /// <param name="h">Hours.</param>
            public static void TTT(uint t, out byte s, out byte m, out byte h)
            {
                s = (byte)((t - ((int)(t / 60) * 60)) * 10 / 6);
                m = (byte)((int)(t / 60) - (int)(t / 3600) * 60);
                h = (byte)(t / 3600);
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
        public JNTicker() : base("Ticker", new MyVersion(1, 0), "First subprogram for NELBRUS system. This subprogram takes LCD with name \"Ticker\" and show current tick on it.") { } // Used for initialisation of subprogram

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
                AddAct(ref Act, Show, 20);
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
            void MePlay() { if (Act.ID == 0) AddAct(ref Act, Show, 20); }

            #region Commands
            string CmdPause(List<string> a) { MePause(); return null; }
            string CmdPlay(List<string> a) { MePlay(); return null; }
            #endregion Commands
        }
    }

    class JNTimer : SubP
    {
        public JNTimer() : base("Timer", "Shows the elapsed time on \"LCD timer\" when using the command \"ss\".") { }

        public override SdSubP Start(ushort id) { return new TP(id, this); }

        class TP : SdSubPCmd
        {
            IMyTextPanel LCD;
            uint start;
            bool s = false;
            CAct S;

            public TP(ushort id, SubP p) : base(id, p)
            {
                if ((LCD = OS.P.GridTerminalSystem.GetBlockWithName("LCD Timer") as IMyTextPanel) == null)
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
                    AddAct(ref S, Show, 10);
                }
                s = !s;
                return "";
            }
            #endregion Commands
        }
    }

    class JNSolarTracker : SubP
    {
        public JNSolarTracker() : base("Solar Tracking", new MyVersion(1, 0), "Solar Tracker by JN") { }

        public override SdSubP Start(ushort id)
        {
            return OS.CSP<TP>() ? null : new TP(id, this);
        }

        class TP : SdSubP
        {
            static float SolarAlign = 0.04f;
            List<IMyMotorStator> Rotors = new List<IMyMotorStator>(); 
            List<IMySolarPanel> Panels = new List<IMySolarPanel>(); 
            List<IMyOxygenFarm> Farms = new List<IMyOxygenFarm>(); 
            List<SolarArray> SolarArrays = new List<SolarArray>(); 
            CAct MA; // Main Action

            public TP(ushort id, SubP p) : base(id, p)
            {
                BuildSolarArrays();
                AddAct(ref MA, Main, 120, 60 * 3);
            }

            public void Main()
            {
                for (int i = 0; i < SolarArrays.Count; i++)
                {
                    if (SolarArrays[i].Update())
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
                        SolarArrays.Add(new SolarArray(v, Panels, Farms));
                    }
                }
            }

            class SolarArray
            {
                public IMyMotorStator Rotor { get; set; }
                public List<IMySolarPanel> Panels { get; set; }
                public List<IMyOxygenFarm> Farms { get; set; }
                public float Power { get; set; }
                public float Oxygen { get; set; }
                public float PowerOld { get; set; }
                public float OxygenOld { get; set; }

                public SolarArray(IMyMotorStator r, List<IMySolarPanel> p, List<IMyOxygenFarm> f)
                {
                    Rotor = r;
                    Panels = new List<IMySolarPanel>(p);
                    Power = 0f;
                    PowerOld = 0f;
                    Farms = new List<IMyOxygenFarm>(f);
                    Oxygen = 0f;
                    OxygenOld = 0f;
                }

                public bool Closed(IMyTerminalBlock b)
                {
                    return b == null || b.WorldMatrix == MatrixD.Identity;
                }
                public bool Update()
                {
                    if (Closed(Rotor)) return true;
                    PowerOld = Power;
                    Power = 0f;
                    OxygenOld = Oxygen;
                    Oxygen = 0f;
                    foreach (var v in Panels)
                    {
                        if (Closed(v)) return true;
                        Power += v.MaxOutput;
                    }
                    foreach (var v in Farms)
                    {
                        if (Closed(v)) return true;
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
                        Rotor.TargetVelocityRPM = 0f;
                    }
                    else if (Rotor.TargetVelocityRPM != 0 && current < old)
                    {
                        Rotor.TargetVelocityRPM = -Rotor.TargetVelocityRPM; // Moving, power < old = reverse direction
                    }
                    else if (Rotor.TargetVelocityRPM == 0)
                    {
                        Rotor.TargetVelocityRPM = SolarAlign;
                    }
                    return false;
                }
            }
        }
    }

    class JNKonturC : SubP
    {
        public JNKonturC() : base("Controller for KONTUR C") { }

        public override SdSubP Start(ushort id) { return OS.CSP<TP>() ? null : new TP(id, this); }

        class TP : SdSubPCmd
        {
            bool start, loopturn;
            float NormWhAng, Tangle;

            float carspeed;
            float strengthSuspModifer, heightSuspModifer;
            bool switchingsusp;

            SuspensionModes SuspensionMode;
            DriveModes DriveMode;

            string FirstStr, SecondStr, LCDriveMode, LCDSuspMode;

            List<IMyMotorSuspension> Wheels;
            IMyMotorSuspension WheelLF, WheelRF, WheelLB, WheelRB;

            IMyRemoteControl RemoteDriver;
            IMyMotorStator RotorRull;
            IMyTextPanel DriverLCD;

            enum DriveModes : byte
            {
                full, front, rear
            }
            enum SuspensionModes : byte
            {
                sport, offroad
            }

            CAct MA;

            public TP(ushort id, SubP p) : base(id, p)
            {
                start = true; loopturn = false;
                NormWhAng = 35; Tangle = 0;
                switchingsusp = false;
                SuspensionMode = SuspensionModes.sport; DriveMode = DriveModes.full;
                FirstStr = ""; SecondStr = "";
                Wheels = new List<IMyMotorSuspension>();
                
                if (
                    (DriverLCD = OS.GTS.GetBlockWithName("Screen Driver") as IMyTextPanel) == null ||
                    (WheelLF = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 LF") as IMyMotorSuspension) == null ||
                    (WheelRF = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 RF") as IMyMotorSuspension) == null ||
                    (WheelLB = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 LB") as IMyMotorSuspension) == null ||
                    (WheelRB = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 RB") as IMyMotorSuspension) == null ||
                    (RemoteDriver = OS.GTS.GetBlockWithName("Control car") as IMyRemoteControl) == null ||
                    (RotorRull = OS.GTS.GetBlockWithName("Rotor rull") as IMyMotorStator) == null
                )
                {
                    AddDefA(Stop, 1);
                    return;
                }
                Wheels = new List<IMyMotorSuspension>() { WheelLF, WheelRF, WheelLB, WheelRB };
                
                string saved = DriverLCD.GetText();
                if (saved != "")
                {
                    if (saved.Contains("front")) DriveMode = DriveModes.front;
                    else if (saved.Contains("rear")) DriveMode = DriveModes.rear;
                    if (saved.Contains("off-road")) SuspensionMode = SuspensionModes.offroad;
                    if (saved.Contains("looper")) loopturn = true;
                }

                ChangeFirst();

                AddAct(ref MA, Main, 1);

                SetCmd(new Dictionary<string, Cmd>
                {
                    { "sdm", new Cmd(CmdSDM, "Switch drive mode Full/Front/Rear.") },
                    { "ssm", new Cmd(CmdSSM, "Switch suspension mode Sport/Off road.") },
                    { "sl", new Cmd(CmdSL, "Switch loopturn mode.")}
                });
            }

            void Main()
            {
                carspeed = Convert.ToSingle(RemoteDriver.GetShipSpeed());
                if (RemoteDriver.IsUnderControl)
                {
                    RotateRull();
                    float whangle;
                    if (carspeed >= 18)
                        if (carspeed >= 40) whangle = 15;
                        else whangle = 20;
                    else whangle = NormWhAng;
                    if (Tangle != whangle)
                    {
                        Tangle = whangle;
                        foreach (IMyMotorSuspension ThisWheel in Wheels) ThisWheel.MaxSteerAngle = Tangle;
                    }

                }
                if (switchingsusp)
                {
                    switchingsusp = false;
                    foreach (IMyMotorSuspension Motor in Wheels)
                    {
                        Motor.Strength = Smoothing(Motor, "Strength", strengthSuspModifer, 0.15f);
                        Motor.Height = Smoothing(Motor, "Height", heightSuspModifer, 0.0042f);
                        if (Motor.GetValueFloat("Strength") != strengthSuspModifer || Motor.GetValueFloat("Height") != heightSuspModifer) switchingsusp = true;
                    }
                }
            }
            void ChangeFirst()
            {
                switch (DriveMode)
                {
                    case DriveModes.full:
                        LCDriveMode = "full";
                        break;
                    case DriveModes.front:
                        LCDriveMode = "front";
                        break;
                    case DriveModes.rear:
                        LCDriveMode = "rear";
                        break;
                }
                FirstStr = " –––Suspension manager–––\n Drive mode: " + LCDriveMode;
                if (loopturn) FirstStr += "–looper";
                FirstStr += "\n";
                switch (SuspensionMode)
                {
                    case SuspensionModes.sport:
                        LCDSuspMode = "sport";
                        break;
                    case SuspensionModes.offroad:
                        LCDSuspMode = "off-road";
                        break;
                }
                FirstStr += " Suspension mode: " + LCDSuspMode + "\n";

                DriverLCD.WriteText(FirstStr + SecondStr, false);
            }
            void RotateRull()
            {
                float mult = 1, destangl, temp;
                if (RemoteDriver.MoveIndicator.X == 0) destangl = 0;
                else
                {
                    if (RemoteDriver.MoveIndicator.X < 0) mult = -1;
                    temp = carspeed;
                    if (temp > 50) temp = 50;
                    destangl = Convert.ToSingle((120 - 4 * temp * temp / 100) * Math.PI / 180) * mult;
                }
                float turn = destangl - RotorRull.Angle;
                RotorRull.TargetVelocityRad = turn * 5;
            }
            float Smoothing(IMyMotorSuspension ThisMotor, string variable, float needed, float Step)
            {
                float TempFloat = ThisMotor.GetValueFloat(variable), mult;
                if (Math.Abs(TempFloat - needed) > Step)
                {
                    if (TempFloat < needed) mult = 1;
                    else mult = -1;
                    TempFloat = TempFloat + mult * Step;
                }
                else TempFloat = needed;
                return TempFloat;
            }

            #region Commands
            string CmdSDM(List<string> a)
            {
                if (DriveMode == DriveModes.rear) DriveMode = DriveModes.full;
                else DriveMode++;
                switch (DriveMode)
                {
                    case DriveModes.full:
                        WheelLB.Propulsion = true;
                        WheelRB.Propulsion = true;
                        WheelLF.Propulsion = true;
                        WheelRF.Propulsion = true;
                        break;
                    case DriveModes.front:
                        WheelLB.Propulsion = false;
                        WheelRB.Propulsion = false; //вырубаем задний привод
                        WheelLF.Propulsion = true;
                        WheelRF.Propulsion = true; //включаем передний
                        break;
                    case DriveModes.rear:
                        WheelLB.Propulsion = true;
                        WheelRB.Propulsion = true; //врубаем задний
                        WheelLF.Propulsion = false;
                        WheelRF.Propulsion = false; //вырубаем передний
                        break;
                }
                ChangeFirst();
                return "";
            }
            string CmdSSM(List<string> a)
            {
                if (SuspensionMode == SuspensionModes.offroad) SuspensionMode = SuspensionModes.sport;
                else SuspensionMode++;
                switchingsusp = true;
                switch (SuspensionMode)
                {
                    case SuspensionModes.sport: 
                        strengthSuspModifer = 25;
                        heightSuspModifer = -0.1080F;
                        break;
                    case SuspensionModes.offroad: 
                        strengthSuspModifer = 16;
                        heightSuspModifer = -0.36F;
                        break;
                }
                ChangeFirst();
                return "";
            }
            string CmdSL(List<string> a)
            {
                float angle;
                if (loopturn)
                {//выключаем
                    angle = NormWhAng;
                    loopturn = false;
                }
                else
                {//включаем
                    angle = 45;
                    loopturn = true;
                }
                WheelRF.InvertSteer = loopturn;
                WheelRF.InvertPropulsion = loopturn;
                WheelRB.Steering = loopturn;
                WheelLB.Steering = loopturn;
                WheelRB.InvertSteer = loopturn;
                WheelRB.InvertPropulsion = loopturn;
                foreach (IMyMotorSuspension ThisWheel in Wheels) ThisWheel.MaxSteerAngle = angle;
                ChangeFirst();
                return "";
            }
            #endregion Commands
        }        
    }

    class JNDalnoboy : SubP
    {
        public JNDalnoboy() : base("DALNOBOY on-board computer", new MyVersion(1, 0)) { }

        public override SdSubP Start(ushort id) { return OS.CSP<TP>() ? null : new TP(id, this); }

        class TP : SdSubPCmd
        {
            IMyShipController ShipCtrler;
            List<IMyMotorSuspension> LeftSusps = new List<IMyMotorSuspension>(), RightSusps = new List<IMyMotorSuspension>();

            CAct MA;

            public TP(ushort id, SubP p) : base(id, p)
            {
                // Collect suspensions
                IMyMotorSuspension L, R;
                for(int i = 1; i < 5; i++)
                {
                    if ((L = OS.GTS.GetBlockWithName($"Suspension Wheel L{i}") as IMyMotorSuspension) == null || (R = OS.GTS.GetBlockWithName($"Suspension Wheel R{i}") as IMyMotorSuspension) == null)
                    {
                        AddDefA(Stop, 1);
                        return;
                    }
                    LeftSusps.Add(L);
                    RightSusps.Add(R);
                }

                AddAct(ref MA, FindCtrler, 20, 1);
            }

            public void FindCtrler()
            {
                List<IMyShipController> SCs = new List<IMyShipController>();
                OS.GTS.GetBlocksOfType(SCs);

                foreach(var x in SCs)
                    if (x.IsUnderControl)
                    {
                        ShipCtrler = x;
                        RemAct(ref MA);
                        AddAct(ref MA, Control, 1);
                        break;
                    }
            }
            public void Control()
            {
                // Check if user leave
                if (!ShipCtrler.IsUnderControl)
                {
                    RemAct(ref MA);
                    AddAct(ref MA, FindCtrler, 30);
                    return;
                }

            }
        }
    }

    class JNew : SubP
    {
        public JNew() : base("", new MyVersion(1, 0)) { }

        public override SdSubP Start(ushort id) { return new TP(id, this); }

        class TP : SdSubP
        {


            public TP(ushort id, SubP p) : base(id, p) { }


        }
    }

    #endregion Test subprograms

    //======-SCRIPT ENDING-======
}
