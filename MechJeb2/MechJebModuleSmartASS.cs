using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSmartASS : DisplayModule
    {
        public enum Mode
        {
            ORBITAL,
            SURFACE,
            TARGET,
            ADVANCED,
            AUTO
        }
        
        public enum Target
        {
            OFF,
            KILLROT,
            NODE,
            SURFACE,
            PROGRADE,
            RETROGRADE,
            NORMAL_PLUS,
            NORMAL_MINUS,
            RADIAL_PLUS,
            RADIAL_MINUS,
            RELATIVE_PLUS,
            RELATIVE_MINUS,
            TARGET_PLUS,
            TARGET_MINUS,
            PARALLEL_PLUS,
            PARALLEL_MINUS,
            ADVANCED,
            AUTO,
            TARGET_HOVER
        }
        
        public static Mode[] Target2Mode = { Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.SURFACE, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.ORBITAL, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.TARGET, Mode.ADVANCED, Mode.AUTO, Mode.TARGET };
        public static string[] ModeTexts = { "OBT", "SURF", "TGT", "ADV", "AUTO" };
        public static string[] TargetTexts = { "OFF", "KILL\nROT", "NODE", "SURF", "PRO\nGRAD", "RETR\nGRAD", "NML\n+", "NML\n-", "RAD\n+", "RAD\n-", "RVEL\n+", "RVEL\n-", "TGT\n+", "TGT\n-", "PAR\n+", "PAR\n-", "ADV", "AUTO", "Hover" };

        public static GUIStyle btNormal, btActive, btAuto;
        public float hoverAlt = 50;

        [Persistent(pass = (int)Pass.Local)]
        public Mode mode = Mode.ORBITAL;
        [Persistent(pass = (int)Pass.Local)]
        public Target target = Target.OFF;
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfHdg = new EditableDouble(90);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfPit = new EditableDouble(90);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble srfRol = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble rol = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public AttitudeReference advReference = AttitudeReference.INERTIAL;
        [Persistent(pass = (int)Pass.Local)]
        public Vector6.Direction advDirection = Vector6.Direction.FORWARD;
        [Persistent(pass = (int)Pass.Local)]
        public Boolean forceRol = false;


        public MechJebModuleSmartASS(MechJebCore core) : base(core) { }

        protected void ModeButton(Mode bt)
        {
            if (GUILayout.Button(ModeTexts[(int)bt], (mode == bt) ? btActive : btNormal, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                mode = bt;
            }
        }

        protected void TargetButton(Target bt)
        {
            if (GUILayout.Button(TargetTexts[(int)bt], (target == bt) ? btActive : btNormal, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                Engage(bt);
            }
        }

        protected void ForceRoll()
        {
            GUILayout.BeginHorizontal();
            forceRol = GUILayout.Toggle(forceRol, "Force Roll :", GUILayout.ExpandWidth(false));
            {
            	Engage(core.GetComputerModule<MechJebModuleSmartASS>().target);
            }
            rol.text = GUILayout.TextField(rol.text, GUILayout.Width(30));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        protected override void WindowGUI(int windowID)
        {
            if (btNormal == null)
            {
                btNormal = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor = btNormal.focused.textColor = Color.white;
                btNormal.hover.textColor = btNormal.active.textColor = Color.yellow;
                btNormal.onNormal.textColor = btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive = new GUIStyle(btNormal);
                btActive.active = btActive.onActive;
                btActive.normal = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover = btActive.onHover;

                btAuto = new GUIStyle(btNormal);
                btAuto.normal.textColor = Color.red;
                btAuto.onActive = btAuto.onFocused = btAuto.onHover = btAuto.onNormal = btAuto.active = btAuto.focused = btAuto.hover = btAuto.normal;
            }

            // If any other module use the attitude controler then let them do it
            if (core.attitude.enabled && core.attitude.users.Count(u => !this.Equals(u)) > 0)
            {
                GUILayout.Button("AUTO", btAuto, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                TargetButton(Target.OFF);
                TargetButton(Target.KILLROT);
                TargetButton(Target.NODE);
                GUILayout.EndHorizontal();

                GUILayout.Label("Mode:");
                GUILayout.BeginHorizontal();
                ModeButton(Mode.ORBITAL);
                ModeButton(Mode.SURFACE);
                ModeButton(Mode.TARGET);
                ModeButton(Mode.ADVANCED);
                GUILayout.EndHorizontal();

                switch (mode)
                {
                    case Mode.ORBITAL:
                        GUILayout.BeginHorizontal();
                        TargetButton(Target.PROGRADE);
                        TargetButton(Target.NORMAL_PLUS);
                        TargetButton(Target.RADIAL_PLUS);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        TargetButton(Target.RETROGRADE);
                        TargetButton(Target.NORMAL_MINUS);
                        TargetButton(Target.RADIAL_MINUS);
                        GUILayout.EndHorizontal();

                        ForceRoll();

                        break;
                    case Mode.SURFACE:
                        GuiUtils.SimpleTextBox("HDG:", srfHdg);
                        GuiUtils.SimpleTextBox("PIT:", srfPit);
                        GuiUtils.SimpleTextBox("ROL:", srfRol);

                        if (GUILayout.Button("EXECUTE", GUILayout.ExpandWidth(true)))
                        {
                            Engage(Target.SURFACE);
                        }
                        break;
                    case Mode.TARGET:
                        if (core.target.NormalTargetExists)
                        {
                            GUILayout.BeginHorizontal();
                            TargetButton(Target.TARGET_PLUS);
                            TargetButton(Target.RELATIVE_PLUS);
                            TargetButton(Target.PARALLEL_PLUS);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            TargetButton(Target.TARGET_MINUS);
                            TargetButton(Target.RELATIVE_MINUS);
                            TargetButton(Target.PARALLEL_MINUS);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Alt:");
                            hoverAlt = float.Parse(GUILayout.TextField(hoverAlt.ToString("F1")));
                            TargetButton(Target.TARGET_HOVER);
                            GUILayout.EndHorizontal();

                            ForceRoll();
                        }
                        else
                        {
                            GUILayout.Label("Please select a target");
                        }
                        break;
                    case Mode.ADVANCED:
                        GUILayout.Label("Reference:");
                        advReference = (AttitudeReference)GuiUtils.ArrowSelector((int)advReference, Enum.GetValues(typeof(AttitudeReference)).Length, advReference.ToString());

                        GUILayout.Label("Direction:");
                        advDirection = (Vector6.Direction)GuiUtils.ArrowSelector((int)advDirection, Enum.GetValues(typeof(Vector6.Direction)).Length, advDirection.ToString());

                        ForceRoll();

                        if (GUILayout.Button("EXECUTE", btNormal, GUILayout.ExpandWidth(true)))
                        {
                            Engage(Target.ADVANCED);
                        }
                        break;
                    case Mode.AUTO:
                        break;
                }

                GUILayout.EndVertical();
            }

            base.WindowGUI(windowID);
        }

        public void Engage(Target newTarget)
        {
            Vector3d direction = Vector3d.zero;
            Quaternion attitude = new Quaternion();
            AttitudeReference reference = AttitudeReference.ORBIT;

        	if (target == Target.TARGET_HOVER && newTarget != Target.TARGET_HOVER) {
        		hoverAlt = Mathf.Max(hoverAlt, 50);
        		core.thrust.tmode = MechJebModuleThrustController.TMode.OFF;
        		core.thrust.trans_kill_h = false;
        		core.thrust.trans_spd_act = 0;
        		core.thrust.users.Remove(this);
        	}
        	target = newTarget;
//    		if (!core.target.NormalTargetExists) { target = Target.OFF; } // is this even needed?

            switch (target)
            {
                case Target.OFF:
                    core.attitude.attitudeDeactivate();
                    return;
                case Target.KILLROT:
                    core.attitude.attitudeKILLROT = true;
                    attitude = Quaternion.LookRotation(part.vessel.GetTransform().up, -part.vessel.GetTransform().forward);
                    reference = AttitudeReference.INERTIAL;
                    break;
                case Target.NODE:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.MANEUVER_NODE;
                    break;
                case Target.SURFACE:
                    attitude = Quaternion.AngleAxis((float)srfHdg, Vector3.up)
                                 * Quaternion.AngleAxis(-(float)srfPit, Vector3.right)
                                 * Quaternion.AngleAxis(-(float)srfRol, Vector3.forward);
                    reference = AttitudeReference.SURFACE_NORTH;
                    break;
                case Target.PROGRADE:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RETROGRADE:
                    direction = Vector3d.back;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.NORMAL_PLUS:
                    direction = Vector3d.left;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.NORMAL_MINUS:
                    direction = Vector3d.right;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RADIAL_PLUS:
                    direction = Vector3d.up;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RADIAL_MINUS:
                    direction = Vector3d.down;
                    reference = AttitudeReference.ORBIT;
                    break;
                case Target.RELATIVE_PLUS:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.RELATIVE_VELOCITY;
                    break;
                case Target.RELATIVE_MINUS:
                    direction = Vector3d.back;
                    reference = AttitudeReference.RELATIVE_VELOCITY;
                    break;
                case Target.TARGET_PLUS:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.TARGET;
                    break;
                case Target.TARGET_MINUS:
                    direction = Vector3d.back;
                    reference = AttitudeReference.TARGET;
                    break;
                case Target.PARALLEL_PLUS:
                    direction = Vector3d.forward;
                    reference = AttitudeReference.TARGET_ORIENTATION;
                    break;
                case Target.PARALLEL_MINUS:
                    direction = Vector3d.back;
                    reference = AttitudeReference.TARGET_ORIENTATION;
                    break;
                case Target.ADVANCED:
                    direction = Vector6.directions[advDirection];
                    reference = advReference;
                    break;
                case Target.TARGET_HOVER:
                    core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                    core.thrust.users.Add(this);
                    break;
                default:
                    return;
            }

            if (forceRol && direction != Vector3d.zero)
            {
                attitude = Quaternion.LookRotation(direction, Vector3d.up) * Quaternion.AngleAxis(-(float)rol, Vector3d.forward);
                direction = Vector3d.zero;
            }

            if (direction != Vector3d.zero)
                core.attitude.attitudeTo(direction, reference, this);
            else
                core.attitude.attitudeTo(attitude, reference, this);
        }
        
		public override void Drive(FlightCtrlState s)
		{
			if (target == MechJebModuleSmartASS.Target.TARGET_HOVER)
			{
				if (FlightGlobals.fetch.VesselTarget == null)
				{
					Engage(Target.OFF);
//					core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
//					core.thrust.trans_kill_h = true;
//					core.thrust.trans_spd_act = 0;
					return;
				}
				
				hoverAlt += (float)((GameSettings.THROTTLE_DOWN.GetKey() ? -1f : 0f) + (GameSettings.THROTTLE_UP.GetKey() ? 1f : 0f)) * TimeWarp.deltaTime * 2f;
				
				var tgtpos = FlightGlobals.fetch.vesselTargetTransform.position;
				var tgtalt = mainBody.GetAltitude(tgtpos);
				var tgtdir = Vector3d.Exclude(vesselState.up, (tgtpos - vessel.GetWorldPos3D()));
				var movedir = Vector3d.Exclude(vesselState.up, vesselState.horizontalSurface).normalized * vesselState.speedSurfaceHorizontal;
				float dist = (float)tgtdir.magnitude;
				float myAlt = Mathf.Max((float)Math.Min(vesselState.altitudeTrue, vessel.altitude), 0);

				float vadjust = (float)Math.Min(((hoverAlt - myAlt) - (vesselState.speedVertical) * 3f), 5);
				
				float maxTilt = 1f - Mathf.Clamp(1 / (float)(vesselState.currentTWR * 0.90f), 0.05f, 0.95f);
				float brakepower = (float)(vesselState.maxThrustAccel * (1f - maxTilt) / (vesselState.localg * 9.81));
				float speed = (float)vesselState.speedSurfaceHorizontal;
				float speedlimit = Mathf.Min(new float[] { 100f, hoverAlt, (dist / 10f), (dist - (speed * (speed / brakepower))) });
				
				//speed: 100m/s
				//brake force: 250m/s
				//d=(x^2/f)+x
				//d=(100^2/250)+100
				
				//(spd^2)/(2bf)
	
				Vector3 att = Vector3.ClampMagnitude(vesselState.up, 1f - maxTilt) + Vector3.ClampMagnitude((tgtdir.normalized * speedlimit - movedir) / 50f, maxTilt);
				
				if (Input.GetKey(KeyCode.F2)) {
					print("dist: " + dist + "\nmyAlt: " + myAlt + "\nspeed: " + vesselState.speedSurfaceHorizontal.value + "\nspeedlimit: " + speedlimit + "\ncurrentTWR: " + vesselState.currentTWR
					      + "\nmaxTilt: " + maxTilt + "\ng: " + mainBody.GeeASL);
				}
				
				core.thrust.trans_kill_h = (dist / 10f < vesselState.speedSurfaceHorizontal) || (dist < 5);
				core.thrust.trans_spd_act = vadjust /* (float)(vadjust > 0 ? Math.Sqrt(mainBody.GeeASL) : 1)*/ ;//* Mathf.Clamp(1f / (float)core.attitude.attitudeError, 0.25f, 1f);
				
				if (dist < 5) {
//					core.thrust.trans_kill_h = true;
					var rcs = (tgtdir * 2 - movedir * 5) * Mathf.Clamp01(dist * 2);
					s.X = (s.X != 0 ? s.X : (float)(-Vector3d.Dot(rcs, vessel.transform.right)));
					s.Y = (s.Y != 0 ? s.Y : (float)(-Vector3d.Dot(rcs, vessel.transform.forward)));
//					s.Z = (s.Z != 0 ? s.Z : (float)(-Vector3d.Dot(rcs, vessel.transform.up)));
				}
				else {
//					core.thrust.trans_kill_h = false;
					core.attitude.attitudeTo(att, AttitudeReference.INERTIAL, this);
				}
			}
        }
        
        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(180), GUILayout.Height(100) };
        }

        public override string GetName()
        {
            return "Smart A.S.S.";
        }
    }
}
