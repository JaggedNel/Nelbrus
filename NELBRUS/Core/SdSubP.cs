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
    //======-SCRIPT BEGINNING-======

    /// <summary>Basic class for running subprogram.</summary>
    class SdSubP : SubP
    {
        public readonly ushort ID;
        /// <summary>The start time of the program.</summary>
        public readonly DateTime ST;
        /// <summary>Action.</summary>
        public delegate void Act();
        /// <summary>Custom Action used to do it later or with frequency.</summary>
        public struct CAct
        {
            public readonly uint ID;
            public readonly Act Act;

            /// <summary>New Custom Action.</summary>
            /// <param name="a">Action.</param>
            public CAct(uint id, Act a) { ID = id; Act = a; }
        }
        /// <summary>Every tick actions.</summary>
        public Act EAct { get; private set; }
        /// <summary>Actions with frequency registry. Mean [tick, [frequency, actions]].</summary>
        public Dictionary<uint, Dictionary<uint, Act>> Acts { get; private set; }

        struct ActToAdd
        {
            public CAct cact { get; private set; }
            public uint f { get; private set; }
            public uint s { get; private set; }

            public ActToAdd(ref CAct cact, uint f, uint s) { this.cact = cact; this.f = f; this.s = s; }
        }

        List<ActToAdd> ActsToAdd = new List<ActToAdd>();

        List<CAct> ActsToRem = new List<CAct>();
        List<CAct> DefAToRem = new List<CAct>();

        /// <summary>Deferred Actions registry. Mean [tick, actions].</summary>
        public Dictionary<uint, Act> DefA { get; private set; }
        /// <summary>Action Adress used to find it in registry.</summary>
        struct Ad
        {
            /// <summary>When Started</summary>
            public uint S { get; }
            /// <summary>Action Frequency</summary>
            public uint F { get; }
            public bool Add { get; set; }
            public bool Remove { get; set; }

            /// <summary>New action Adress</summary>
            /// <param name="s">Time when started</param>
            /// <param name="f">Frequency of action</param>
            public Ad(uint s, uint f) { S = s; F = f; Add = true; Remove = false; }
        }
        /// <summary>Key for new action.</summary>
        uint AK;
        /// <summary>Actions Archive. Mean [id, action adress].</summary>
        Dictionary<uint, Ad> AA;
        /// <summary> Terminate message container. Used to stop unworkable subprogram when its run. </summary>
        public string TerminateMsg { get; private set; }

        public SdSubP(ushort id, string name, MyVersion v = null, string info = NA) : base(name, v, info)
        {
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
        public SdSubP(ushort id, SubP p) : this(id, p.Name, p.V, p.Description) { }

        #region Actions management
        /// <summary> OS function to add and delete custom actions. Do not use it. </summary>
        public void UpdActions()
        {
            // Add new
            foreach (var i in ActsToAdd)
            {
                if (i.cact.ID == 0)
                    continue;
                if (i.f < 2)
                {
                    if (i.f == 1)
                    {
                        // Every tick action
                        EAct += i.cact.Act;
                        i.cact.Act();
                    }
                    else
                    {
                        // Deferred Action
                        if (DefA.ContainsKey(OS.Tick + i.s))
                            DefA[OS.Tick + i.s] += i.cact.Act;
                        else
                            DefA.Add(OS.Tick + i.s, i.cact.Act);
                    }
                }
                else
                {
                    uint t; // When starts
                    if (i.s == 0)
                    {
                        t = OS.Tick + i.f;
                        i.cact.Act();
                    }
                    else
                        t = OS.Tick + i.s;
                    if (!Acts.ContainsKey(t))
                        Acts.Add(t, new Dictionary<uint, Act>() { { i.f, i.cact.Act } });
                    else if (!Acts[t].ContainsKey(i.f))
                        Acts[t].Add(i.f, i.cact.Act);
                    else
                        Acts[t][i.f] += i.cact.Act;
                }
            }
            ActsToAdd.Clear();

            // Remove
            foreach (var i in ActsToRem)
            {
                if (AA.ContainsKey(i.ID))
                {
                    if (AA[i.ID].F < 2)
                        EAct -= i.Act;
                    else
                    {
                        var intercept = OS.Tick < AA[i.ID].S ? AA[i.ID].S : (((OS.Tick - AA[i.ID].S) / AA[i.ID].F) + 1) * AA[i.ID].F + AA[i.ID].S;
                        Acts[intercept][AA[i.ID].F] -= i.Act;
                        if (Acts[intercept][AA[i.ID].F] == null)
                        {
                            Acts[intercept].Remove(AA[i.ID].F);
                            if (Acts[intercept].Count == 0)
                                Acts.Remove(intercept);
                        }
                    }

                    AA.Remove(i.ID);
                }
            }
            ActsToRem.Clear();
            foreach (var i in DefAToRem)
            {
                if (DefA.ContainsKey(i.ID))
                {
                    DefA[i.ID] -= i.Act;
                    if (DefA[i.ID] == null)
                        DefA.Remove(i.ID);
                }
            }
            DefAToRem.Clear();
        }
        /// <summary>Add new action triggered by the frequency freq and that will be runned first time with tick span.</summary>
        /// <param name="ca">Action storage in subprogram</param>
        protected void AddAct(ref CAct ca, Act act, uint freq, uint span = 0)
        {
            ca = new CAct(AK == uint.MaxValue ? 1 : AK, act);
            freq = freq < 1 ? 1 : freq;
            AA.Add(AK == uint.MaxValue ? 1 : AK++, new Ad(OS.Tick + (span == 0 ? freq : span), freq));
            ActsToAdd.Add(new ActToAdd(ref ca, freq, span));
        }
        /// <summary>Remove action triggered by the frequency.</summary>
        protected void RemAct(ref CAct a)
        {
            if (a.ID == 0)
                return;
            ActsToRem.Add(new CAct(a.ID, a.Act));
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
        protected void AddDefA(ref CAct ca, Act act, uint span)
        {
            ca = new CAct(OS.Tick + span, act);
            ActsToAdd.Add(new ActToAdd(ref ca, 0, span));
        }
        /// <summary>Remove deferred action.</summary>
        protected void RemDefA(ref CAct a)
        {
            if (DefA.ContainsKey(a.ID) && DefA[a.ID] != null)
            {
                DefA[a.ID] -= a.Act;
            }
            a = new CAct(); // Removed actions have default id value 0
        }
        #endregion Actions management

        /// <summary>Stop started subprogram.</summary>
        public virtual void Stop() { OS.SSP(this); }
        /// <summary>Returns true to let OS stop this subprogram. WARNING: Do not forget stop child subprograms there too.</summary>
        public virtual bool MayStop() { return true; }
        /// <summary> Stop subprogram immediately. /// </summary>
        /// <param name="msg">Message about termination reason.</param>
        public void Terminate(string msg = "")
        {
            TerminateMsg = string.IsNullOrEmpty(msg) ? "OS> Subprogram can not continue to work." : msg;
            OS.SSP(this);
            Stop();
        }
    }

    //======-SCRIPT ENDING-======
}