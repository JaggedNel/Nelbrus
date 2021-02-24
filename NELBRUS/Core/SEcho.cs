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

        /// <summary> Standart echo controller. </summary>
        public class SEcho : EchoController
        {
            /// <summary>Fields to write at echo. Mean [id, field (string, integer, request, StringBuilder ...)].</summary>
            protected Dictionary<FieldNames, List<object>> Fields;
            /// <summary>Operation indicator.</summary>
            protected Ind OInd;
            /// <summary>Refresh action.</summary>
            CAct R = new CAct();
            /// <summary>Custom information remover.</summary>
            CAct[] C = new CAct[10];
            public enum FieldNames : byte { Base, Msg };

            public SEcho() : base("Standart echo controller")
            {
                OInd = new Ind(Ind.UpdateTurn, "(._.)            ", "   ( l: )         ", "      (.–.)      ", "         ( :l )   ", "            (._.)");
                Fields = new Dictionary<FieldNames, List<object>> {
                    { FieldNames.Base, new List<object> { new List<object> { $"OS NELBRUS v.{(string)OS.V}\nIs worked ",  (Req)OInd.Get, "\nInitialized subprograms: ", (ReqI)OS.GetCountISP, "\nRunned subprograms: ", (ReqI)OS.GetCountRSP } } },
                    { FieldNames.Msg, new List<object>() } // Custom information
                };
                AddAct(ref R, (Act)Refresh + OInd.Update, 30, 1);
                DT = F.TTT(45);
            }

            /// <summary>Refresh information at echo.</summary>
            public override void Refresh()
            {
                var t = new StringBuilder();
                foreach (var f in Fields.Values)
                {
                    for (int i = 0; i < f.Count(); i++)
                    {
                        if (f[i] is List<object>)
                            t.Append(Parse(f[i] as List<object>));
                        else
                            t.Append(GetObj(f[i]));
                        t.Append("\n");
                    }
                }
                t.Append("\n\n\n\n\n\n\n");
                OS.P.Echo(t.ToString());
            }
            StringBuilder Parse(List<object> line)
            {
                StringBuilder res = new StringBuilder();
                for (int i = 0; i < line.Count(); i++)
                    res.Append(GetObj(line[i]));
                return res;
            }
            string GetObj(object obj)
            {
                return obj is Req ? ((Req)obj)() : obj is ReqI ? ((ReqI)obj)().ToString() : obj.ToString();
            }
            /// <summary>Show custom info at echo.</summary>
            public override void CShow(string s)
            {
                Fields[FieldNames.Msg].Insert(0, s);
                if (Fields[FieldNames.Msg].Count > C.Count())
                    Fields[FieldNames.Msg].RemoveAt(Fields[FieldNames.Msg].Count - 1);
                for (int i = C.Count() - 1; i > 0; i--)
                    C[i] = C[i - 1];
                C[0] = new CAct();
                AddDefA(ref C[0], CTimeRemove, DT);
                Refresh();
            }
            /// <summary> Remove custom information after the time has elapsed. </summary>
            void CTimeRemove()
            {
                var i = Fields[FieldNames.Msg].Count;
                if (i > 0)
                {
                    Fields[FieldNames.Msg].RemoveAt(i - 1);
                    C[i - 1] = new CAct();
                }
            }
            /// <summary>Remove custom info in echo.</summary>
            public override void CClr()
            {
                for (int i = 0; i < C.Count() && C[i].ID != 0; i++)
                {
                    RemDefA(ref C[i]);
                    C[i] = new CAct();
                }
                Fields[FieldNames.Msg].Clear();
            }
        }


        //======-SCRIPT ENDING-======
    }
}