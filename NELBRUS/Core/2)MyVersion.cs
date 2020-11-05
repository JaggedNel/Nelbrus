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

    //======-SCRIPT ENDING-======
}