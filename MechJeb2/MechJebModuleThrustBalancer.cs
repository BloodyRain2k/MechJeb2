/*
 * Created by SharpDevelop.
 * User: Bernhard
 * Date: 29.03.2014
 * Time: 02:36
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleThrustBalancer : ComputerModule
	{
		Dictionary<int, float> originalLimits = new Dictionary<int, float>();
		string[] btns = new string[] { "-P", "P", "-Y", "Y", "-R", "R" };
		int xpyr = 0;
		int ypyr = 3;
		int zpyr = 4;
		
		[GeneralInfoItem("Thrust Balancer", InfoItem.Category.Thrust)]
		public void ThrustBalancerInfoItem()
		{
			enabled = GUILayout.Toggle(enabled, "Balance Center of Thrust");
			xpyr = GUILayout.SelectionGrid(xpyr, btns, 6);
			ypyr = GUILayout.SelectionGrid(ypyr, btns, 6);
			zpyr = GUILayout.SelectionGrid(zpyr, btns, 6);
		}

		public MechJebModuleThrustBalancer(MechJebCore core) : base(core) { }
		
		private void recurseParts(Part part, ref Vector3 CoT, ref Vector3 DoT, ref float t)
		{
			CenterOfThrustQuery centerOfThrustQuery = new CenterOfThrustQuery();
			
			var send = false; // because CoT queries don't care whenever the engine is active or has fuel
			if (part.FindModulesImplementing<AtmosphericEngine>().FindAll(e => e.engineEnabled && e.EngineHasFuel()).Count > 0) { send = true; }
			if (part.FindModulesImplementing<LiquidEngine>().FindAll(e => e.enabled && e.EngineHasFuel()).Count > 0) { send = true; }
			if (part.FindModulesImplementing<LiquidFuelEngine>().FindAll(e => e.enabled && e.EngineHasFuel()).Count > 0) { send = true; }
			if (part.FindModulesImplementing<ModuleEngines>().FindAll(e => e.getIgnitionState && e.part.EngineHasFuel()).Count > 0) { send = true; }
			if (part.FindModulesImplementing<ModuleEnginesFX>().FindAll(e => e.getIgnitionState && e.part.EngineHasFuel()).Count > 0) { send = true; }
			
			if (send)
			{
				part.SendMessage("OnCenterOfThrustQuery", centerOfThrustQuery, SendMessageOptions.DontRequireReceiver);
				CoT += centerOfThrustQuery.pos * centerOfThrustQuery.thrust;
				DoT += centerOfThrustQuery.dir * centerOfThrustQuery.thrust;
				t += centerOfThrustQuery.thrust;
			}
			
			foreach (var p in part.children)
			{
				this.recurseParts(p, ref CoT, ref DoT, ref t);
			}
		}
		
		private CenterOfThrustQuery centerOfThrust()
		{
			var CoT = new CenterOfThrustQuery();
			
			recurseParts(vessel.rootPart, ref CoT.pos, ref CoT.dir, ref CoT.thrust);
			
			CoT.pos /= CoT.thrust;
			CoT.dir = (CoT.dir / CoT.thrust).normalized;
			
			return CoT;
		}
		
		private Vector3 thrustOffset()
		{
			var CoT = centerOfThrust();
			var offset = vessel.transform.worldToLocalMatrix.MultiplyVector(Vector3.Exclude(CoT.dir, vessel.CoM - CoT.pos));
			var factor = (float)(vesselState.mass / 1000 / vesselState.maxThrustAccel);
			switch (xpyr) {
				case 0: offset.x -= factor * vessel.ctrlState.pitch; break;
				case 1: offset.x += factor * vessel.ctrlState.pitch; break;
				case 2: offset.x -= factor * vessel.ctrlState.yaw; break;
				case 3: offset.x += factor * vessel.ctrlState.yaw; break;
				case 4: offset.x -= factor * vessel.ctrlState.roll; break;
				case 5: offset.x += factor * vessel.ctrlState.roll; break;
			}
			switch (ypyr) {
				case 0: offset.y -= factor * vessel.ctrlState.pitch; break;
				case 1: offset.y += factor * vessel.ctrlState.pitch; break;
				case 2: offset.y -= factor * vessel.ctrlState.yaw; break;
				case 3: offset.y += factor * vessel.ctrlState.yaw; break;
				case 4: offset.y -= factor * vessel.ctrlState.roll; break;
				case 5: offset.y += factor * vessel.ctrlState.roll; break;
			}
			switch (zpyr) {
				case 0: offset.z -= factor * vessel.ctrlState.pitch; break;
				case 1: offset.z += factor * vessel.ctrlState.pitch; break;
				case 2: offset.z -= factor * vessel.ctrlState.yaw; break;
				case 3: offset.z += factor * vessel.ctrlState.yaw; break;
				case 4: offset.z -= factor * vessel.ctrlState.roll; break;
				case 5: offset.z += factor * vessel.ctrlState.roll; break;
			}
			return offset;
		}

		public override void OnFixedUpdate()
		{
			var CoT = centerOfThrust();
			var lastOffset = thrustOffset();
			var step = 0.1f;

			var eng = vessel.FindPartModulesImplementing<ModuleEngines>();
			var engFX = vessel.FindPartModulesImplementing<ModuleEnginesFX>();
			
			for (int run = 0; run < 5; run++)
			{
				foreach (var e in eng)
				{
					var pos = Vector3.zero;
					e.thrustTransforms.ForEach(t => pos += t.position);
					pos /= e.thrustTransforms.Count;
					
					var ang = Vector3.Angle(vessel.transform.worldToLocalMatrix.MultiplyVector(Vector3.Exclude(CoT.dir, pos - CoT.pos)), lastOffset);
					
					if (ang < 90) // we're on the heavy side, set 100% and continue
					{
						if (e.thrustPercentage + step < 100f)
							e.thrustPercentage += step;
						else
							e.thrustPercentage = 100f;
						continue;
					}
					
					var perc = e.thrustPercentage;
					
					if (e.thrustPercentage > 0f) // not 0% so try decreasing
					{
						e.thrustPercentage -= step;
						var offset = thrustOffset().magnitude;
						if (offset < lastOffset.magnitude) { continue; } // improvement! don't reset back
						e.thrustPercentage = perc;
					}

					if (e.thrustPercentage < 100f) // not 100% so try increasing
					{
						e.thrustPercentage += step;
						var offset = thrustOffset().magnitude;
						if (offset < lastOffset.magnitude) { continue; } // improvement! don't reset back
						e.thrustPercentage = perc;
					}
				}

				foreach (var e in engFX)
				{
					var pos = Vector3.zero;
					e.thrustTransforms.ForEach(t => pos += t.position);
					pos /= e.thrustTransforms.Count;
					
					var ang = Vector3.Angle(vessel.transform.worldToLocalMatrix.MultiplyVector(Vector3.Exclude(CoT.dir, pos - CoT.pos)), lastOffset);
					
					if (ang < 90) // we're on the heavy side, set 100% and continue
					{
						if (e.thrustPercentage + step < 100f)
							e.thrustPercentage += step;
						else
							e.thrustPercentage = 100f;
						continue;
					}
					
					var perc = e.thrustPercentage;
					
					if (e.thrustPercentage > 0f) // not 0% so try decreasing
					{
						e.thrustPercentage -= step;
						var offset = thrustOffset().magnitude;
						if (offset < lastOffset.magnitude) { continue; } // improvement! don't reset back
						e.thrustPercentage = perc;
					}

					if (e.thrustPercentage < 100f) // not 100% so try increasing
					{
						e.thrustPercentage += step;
						var offset = thrustOffset().magnitude;
						if (offset < lastOffset.magnitude) { continue; } // improvement! don't reset back
						e.thrustPercentage = perc;
					}
				}
			}
		}
		
		public override void OnModuleEnabled()
		{
			originalLimits.Clear();
			vessel.FindPartModulesImplementing<ModuleEngines>().ForEach(e => originalLimits.Add(e.GetHashCode(), e.thrustPercentage));
			vessel.FindPartModulesImplementing<ModuleEnginesFX>().ForEach(e => originalLimits.Add(e.GetHashCode(), e.thrustPercentage));
		}
		
		public override void OnModuleDisabled()
		{
			vessel.FindPartModulesImplementing<ModuleEngines>().ForEach(e => e.thrustPercentage = originalLimits[e.GetHashCode()]);
			vessel.FindPartModulesImplementing<ModuleEnginesFX>().ForEach(e => e.thrustPercentage = originalLimits[e.GetHashCode()]);
		}
	}
}