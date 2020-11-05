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

    JNDalnoboy iJNDalnoboy = new JNDalnoboy();
    /// <summary>
    /// On-board system for smart rover Dalnoboy.
    /// Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2190992795
    /// </summary>
    class JNDalnoboy : InitSubP
    {
        public JNDalnoboy() : base("DALNOBOY on-board computer", new MyVersion(1, 0)) { }

        public override SdSubP Start(ushort id) { return OS.CSP<TP>() ? null : new TP(id, this); }

        public class TP : SdSubPCmd
        {
            IMyShipController Controller;
            IMyMotorStator RotorSusp, RotorSolar;
            IMyMotorAdvancedStator HingeNeck, HingeSolar;
            List<IMyMotorAdvancedStator> HingesSolar = new List<IMyMotorAdvancedStator>();
            List<IMyShipController> Controllers = new List<IMyShipController>();
            List<IMyMotorAdvancedStator> Hinges = new List<IMyMotorAdvancedStator>();
            List<IMyMotorSuspension> Wheels = new List<IMyMotorSuspension>();
            float whangle, Tangle;
            bool Solar = true;

            CAct MA = new CAct(), GC = new CAct(), TS = new CAct();

            public TP(ushort id, SubP p) : base(id, p)
            {
                OS.GTS.GetBlocksOfType(Controllers, x => x.CanControlShip);
                RotorSusp = OS.GTS.GetBlockWithName("Suspension Rotor") as IMyMotorStator;
                HingeNeck = OS.GTS.GetBlockWithName("Neck Hinge") as IMyMotorAdvancedStator;
                RotorSolar = OS.GTS.GetBlockWithName("Solar Rotor") as IMyMotorStator;
                HingeSolar = OS.GTS.GetBlockWithName("Solar Hinge") as IMyMotorAdvancedStator;
                OS.GTS.GetBlocksOfType(HingesSolar, x => x.CustomName.StartsWith("Solar Hinge") && x.CustomName != "Solar Hinge");
                OS.GTS.GetBlocksOfType(Hinges, x => x.CustomName.StartsWith("Suspension"));
                OS.GTS.GetBlocksOfType(Wheels);
                SetCmd("tsp", new Cmd(CmdTurnSolar, "Turn solar panels"));

                if (Controllers.Count == 0 || RotorSusp == null || HingeNeck == null)
                    Terminate("Dalnoboy blocks not found.");
                else
                {
                    if (HingeSolar == null || RotorSolar == null)
                        Solar = false;
                    AddAct(ref GC, GetController, 20);
                    AddAct(ref MA, Control, 5, 1);
                }
            }

            void Control()
            {

                var TargetVecLoc = CustVectorTransform(Controller.GetTotalGravity(), HingeNeck.WorldMatrix.GetOrientation());
                var Roll = Math.Atan2(-TargetVecLoc.X, TargetVecLoc.Z);
                var Pitch = Math.Atan2(TargetVecLoc.Y, TargetVecLoc.Z);
                HingeNeck.TargetVelocityRad = Turn(-(float)Roll, HingeNeck.Angle);

                DampSuspRot(RotorSusp);
                foreach (var h in Hinges) DampSuspHinge(h, Roll, Pitch);
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

                if (Controller.GetShipSpeed() >= 12 && Controller.RollIndicator == 0)
                    if (Controller.GetShipSpeed() >= 40) whangle = .17f;
                    else whangle = .26f;
                else whangle = .38f;
                if (Tangle != whangle)
                {
                    Tangle = whangle;
                    foreach (IMyMotorSuspension w in Wheels) w.MaxSteerAngle = Tangle;
                }
            }
            void DampSuspRot(IMyMotorStator r)
            {
                r.TargetVelocityRPM = -r.Angle * 180 / 3.14f;
                r.Torque = Math.Abs(r.Angle / r.UpperLimitRad * 120000);
            }
            void DampSuspHinge(IMyMotorAdvancedStator h, double r, double p)
            {
                var T =
                30000
                + h.Angle / Math.Abs(h.LowerLimitRad) * 30000
                + (float)(2 * p / Math.PI) * Math.Sign(Vector3D.Dot(h.GetPosition() - HingeNeck.GetPosition(), HingeNeck.WorldMatrix.Up)) * 20000
                + (float)(2 * r / Math.PI) * Math.Sign(Vector3D.Dot(h.GetPosition() - HingeNeck.GetPosition(), HingeNeck.WorldMatrix.Forward)) * 20000
                ;
                byte upLegs = (byte)Convert.ToSingle(Controller.MoveIndicator.Y < 0 && ((Vector3D.Dot(h.GetPosition() - HingeNeck.GetPosition(), HingeNeck.WorldMatrix.Up) < 0 && Controller.MoveIndicator.Z <= 0) || (Vector3D.Dot(h.GetPosition() - HingeNeck.GetPosition(), HingeNeck.WorldMatrix.Up) > 0 && Controller.MoveIndicator.Z > 0)));
                h.Torque = Convert.ToSingle(T > 0) * T + upLegs * 100000;
                h.TargetVelocityRPM = upLegs * 80 - 40;
            }
            /// <summary>Transfer of coordinates of Vec to Orientation coordinate system.</summary>
            Vector3D CustVectorTransform(Vector3D Vec, MatrixD Orientation)
            {
                // standart
                //return new Vector3D(Vec.Dot(Orientation.Right), Vec.Dot(Orientation.Up), Vec.Dot(Orientation.Forward));
                return new Vector3D(Vec.Dot(Orientation.Backward), Vec.Dot(Orientation.Up), Vec.Dot(Orientation.Right));
            }

            float Turn(float DesiredAngle, float CurrentAngle)
            {
                float Turn = DesiredAngle - CurrentAngle;
                Turn = Normalize(Turn);
                return Turn;
            }
            float Normalize(float Angle)
            {
                if (Angle < -Math.PI) Angle += 2 * (float)Math.PI;
                else if (Angle > Math.PI) Angle -= 2 * (float)Math.PI;
                return Angle;
            }

            public void TurnSolar()
            {
                if (RotorSolar.Angle < .3 || RotorSolar.Angle > 2 * Math.PI - .45)
                {
                    HingeSolar.TargetVelocityRad *= -1;
                    RotorSolar.TargetVelocityRad = 0;
                    RemAct(ref TS);
                }
                else
                    RotorSolar.TargetVelocityRad = 1;
            }

            string CmdTurnSolar(List<string> a)
            {
                if (!Solar)
                    return "Dalnoboy solar panels not available!";
                foreach (var i in HingesSolar)
                    i.TargetVelocityRad *= -1;
                AddAct(ref TS, TurnSolar, 30);
                return "";
            }
        }
    }

    //======-SUBPROGRAM ENDING-======
}