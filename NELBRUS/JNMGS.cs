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
    //======-SUBPROGRAM BEGINING-======

    class JNMGS : SubP
    {
        // Some fragments of this program are adopted from
        // "SWCS | Whip's Subgrid Wheel Control Script"
        // by Whiplash141
        public JNMGS() : base("Multigrid Suspension", new MyVersion(1, 0), "Allows you to control wheels placed on any subgrid using regular movement keys.") { }

        public override SdSubP Start(ushort id) { return OS.CSP<TP>() ? null : new TP(id, this); }

        class TP : SdSubPCmd
        {
            string IgnoreNameTag = "Ignore";
            float brakingConstant = 0.4f;
            bool detectBlocksOverConnectors = false;

            IMyShipController Controller = null;
            Vector3D avgWheelPosition;

            List<IMyMotorSuspension> SubgridWheels = new List<IMyMotorSuspension>();
            List<IMyMotorSuspension> Wheels = new List<IMyMotorSuspension>();
            List<IMyShipController> Controllers = new List<IMyShipController>();

            CAct GB = new CAct(), MA = new CAct(), GC = new CAct();

            public TP(ushort id, SubP p) : base(id, p)
            {
                AddAct(ref GB, GetBlocks, 600, 30);
                SetCmd(new Dictionary<string, Cmd>
                {
                    { "int", new Cmd(CmdINT, "Get or set ignore name tag.", "/int - View current ignore name tag;\n/int <string> - Set new ignore name tag.") },
                    { "bc", new Cmd(CmdBC, "Get or set breaking constant.", "/bc - View current breaking constant;\n/bc <float> - Set new breaking constant.")},
                    { "dboc", new Cmd(CmdDBOC, "Get or set detection blocks over connectors.", "/dboc - View current detection blocks over connectors;\n/dboc <bool> - Set new detection blocks over connectors")}
                });
            }

            void Control()
            {
                var brakes = Controller.MoveIndicator.Y > 0 || Controller.HandBrake;
                var velocity = Vector3D.TransformNormal(Controller.GetShipVelocities().LinearVelocity, MatrixD.Transpose(Controller.WorldMatrix)) * brakingConstant;
                avgWheelPosition = Vector3D.Zero;
                foreach (var w in Wheels) avgWheelPosition += w.GetPosition();
                avgWheelPosition /= Wheels.Count;

                foreach (var w in SubgridWheels)
                {
                    w.SetValue("Propulsion override", -Math.Sign(Math.Round(Vector3D.Dot(w.WorldMatrix.Up, Controller.WorldMatrix.Right), 2)) * (Convert.ToSingle(brakes && Controller.GetShipSpeed() > 1) * (float)velocity.Z + Convert.ToSingle(!brakes) * w.Power * 0.01f * -Controller.MoveIndicator.Z));
                    w.SetValue("Steer override", Math.Sign(Math.Round(Vector3D.Dot(w.WorldMatrix.Forward, Controller.WorldMatrix.Up), 2)) * Math.Sign(Vector3D.Dot(w.GetPosition() - avgWheelPosition, Controller.WorldMatrix.Forward)) * Controller.MoveIndicator.X + Controller.RollIndicator);
                }
            }
            void GetBlocks()
            {
                OS.GTS.GetBlocksOfType(Controllers, x => !x.CustomName.Contains(IgnoreNameTag) && (detectBlocksOverConnectors || OS.P.Me.IsSameConstructAs(x)));
                OS.GTS.GetBlocksOfType(Wheels, x => !x.CustomName.Contains(IgnoreNameTag) && (detectBlocksOverConnectors || OS.P.Me.IsSameConstructAs(x)));
                if (Controllers.Count != 0 && Wheels.Count != 0)
                {
                    GetController();

                    GetSubgridWheels(Controller);
                    if (SubgridWheels.Count != 0 && MA.ID == 0)
                    {
                        AddAct(ref GC, GetController, 15, 17);
                        AddAct(ref MA, Control, 5, 1);
                    }
                }
                else
                {
                    RemAct(ref GC);
                    RemAct(ref MA);
                }
            }
            void GetController()
            {
                if (!(Controller ?? (Controller = Controllers[0])).IsUnderControl || !Controller.CanControlShip)
                    for (int i = 1; i < Controllers.Count; i++)
                        if (Controllers[i].IsUnderControl && Controllers[i].CanControlShip)
                        {
                            Controller = Controllers[i];
                            break;
                        }
                SynchronizeHandBrakes(Controller);
            }
            void SynchronizeHandBrakes(IMyShipController c)
            {
                foreach (var b in Controllers) b.HandBrake = c.HandBrake;
            }
            void GetSubgridWheels(IMyTerminalBlock reference)
            {
                SubgridWheels.Clear();

                foreach (var w in Wheels)
                {
                    if (reference.CubeGrid != w.CubeGrid) SubgridWheels.Add(w);
                    else
                    {
                        w.SetValue("Propulsion override", 0f);
                        w.SetValue("Steer override", 0f);
                    }
                }
            }

            #region Commands
            string CmdINT(List<string> a)
            {
                if (a.Count == 0) return $"Current ignore name tag is {NLB.F.Brckt(IgnoreNameTag)}";
                else if (string.IsNullOrWhiteSpace(a[0])) return mAE;
                else return $"New ignore name tag is {NLB.F.Brckt(IgnoreNameTag = a[0])}";
            }
            string CmdBC(List<string> a)
            {
                if (a.Count == 0) return $"Current breaking constant is {NLB.F.Brckt(brakingConstant.ToString())}";
                else
                {
                    float t;
                    if (float.TryParse(a[0], out t)) return $"New breaking constant is {NLB.F.Brckt((brakingConstant = t).ToString())}";
                    else return mAE;
                }
            }
            string CmdDBOC(List<string> a)
            {
                if (a.Count == 0) return $"Current detection blocks over connectors is {NLB.F.Brckt(detectBlocksOverConnectors.ToString())}";
                else
                {
                    bool t;
                    if (bool.TryParse(a[0], out t)) return $"New detection blocks over connectors is {NLB.F.Brckt((detectBlocksOverConnectors = t).ToString())}";
                    else return mAE;
                }

            }
            #endregion Commands
        }
    }

    //======-SUBPROGRAM ENDING-======
}