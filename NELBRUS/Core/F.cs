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
    sealed partial class NLB : SdSubPCmd
    {
        //======-SCRIPT BEGINNING-======
        
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
            public static string SPI(SubP p, bool i = false)
            {
                string r = p.V == null ? $"{Brckt(p.Name)}" : $"{Brckt(p.Name)} v.{(string)p.V}";
                return i ? r + $"\n{p.Description}{(p is SdSubP ? $"\nWas launched at [{(p as SdSubP).ST.ToString()}].\nCommands support: {p is SdSubPCmd}." : "")}" : r;
            }
        }

        //======-SCRIPT ENDING-======
    }
}
