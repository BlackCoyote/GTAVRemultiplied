﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;

namespace GTAVRemultiplied.ClientSystem
{
    public class VehicleInfo
    {
        public Vector3 lPos = Vector3.Zero;

        public Vector3 lGoal = Vector3.Zero;

        public Quaternion lRot = Quaternion.Identity;

        public Quaternion lRotGoal = Quaternion.Identity;

        public Vector3 lRotVel = Vector3.Zero;

        public float speed = 0;

        public Vehicle vehicle;

        public void SetVehiclePosition(Vector3 pos)
        {
            // TODO: Restore this. Probably requires some extra cleverness.
            /*if (World.RaycastCapsule(pos + new Vector3(0, 0, 1), Vector3.WorldUp, 0.01f, 0.3f, IntersectOptions.Map | IntersectOptions.MissionEntities | IntersectOptions.Objects, vehicle).DidHit)
            {
                return;
            }*/
            vehicle.PositionNoOffset = pos;
        }

        public void Tick()
        {
            Vector3 rel = lGoal - lPos;
            float rlen = rel.Length();
            if (rlen > 0.01f && speed > 0.01f)
            {
                rel /= rlen;
                if (speed * GTAVFrenetic.cDelta > rlen || rlen > 10)
                {
                    lPos = lGoal;
                    SetVehiclePosition(lGoal);
                }
                else
                {
                    lPos = lPos + rel * speed * GTAVFrenetic.cDelta;
                    SetVehiclePosition(lPos);
                }
            }
            else
            {
                lPos = lGoal;
                SetVehiclePosition(lGoal);
            }
            vehicle.Quaternion = lRot = Quaternion.Slerp(lRot, lRotGoal, GTAVFrenetic.cDelta * Math.Max(lRotVel.Length() * 10f, 0.5f)); // TODO: Deal with constants here!
        }
    }
}