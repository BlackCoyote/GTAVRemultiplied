﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using FreneticScript;
using GTA;
using GTA.Math;
using GTA.Native;
using GTAVRemultiplied.ServerSystem.PacketsOut;

namespace GTAVRemultiplied.ServerSystem
{
    public class GTAVServerConnection
    {
        public TcpListener Listener;

        public List<GTAVServerClientConnection> Connections = new List<GTAVServerClientConnection>();

        public void CheckForConnections()
        {
            while (Listener.Pending())
            {
                GTAVServerClientConnection client = new GTAVServerClientConnection();
                try
                {
                    client.Sock = Listener.AcceptSocket();
                    client.Sock.Blocking = false;
                    client.Sock.SendBufferSize = 1024 * 1024;
                    client.Sock.ReceiveBufferSize = 1024 * 1024;
                    Connections.Add(client);
                    client.Spawn();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
            for (int i = 0; i < Connections.Count; i++)
            {
                try
                {
                    Connections[i].Tick();
                    Connections[i].SendPacket(new PlayerUpdatePacketOut(Game.Player));
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    Log.Error("Dropping a client!");
                    Connections[i].Sock.Close();
                    Connections.RemoveAt(i);
                    i--;
                }
            }
            WeaponHash cweap = Game.Player.Character.Weapons.Current.Hash;
            int cammo = Game.Player.Character.Weapons.Current.AmmoInClip;
            if (cweap != weap)
            {
                weap = cweap;
            }
            else
            {
                if (ammo > cammo)
                {
                    for (int i = 0; i < Connections.Count; i++)
                    {
                        Connections[i].SendPacket(new FiredShotPacketOut(Game.Player));
                    }
                }
                // TODO: Reload, etc.
            }
            ammo = cammo;
            bool tjump = Game.Player.Character.IsJumping;
            if (tjump && !pjump)
            {
                for (int i = 0; i < Connections.Count; i++)
                {
                    Connections[i].SendPacket(new JumpPacketOut(Game.Player));
                }
            }
            pjump = tjump;
            HashSet<int> ids = new HashSet<int>(Vehicles);
            // TODO: Network vehicle updates more cleverly.
            bool needsVehUpdate = DateTime.Now.Subtract(nextVehicleUpdate).TotalMilliseconds > 100;
            if (needsVehUpdate)
            {
                nextVehicleUpdate = DateTime.Now;
            }
            foreach (Vehicle vehicle in World.GetAllVehicles())
            {
                if (Vehicles.Add(vehicle.Handle))
                {
                    foreach (GTAVServerClientConnection connection in Connections)
                    {
                        connection.SendPacket(new AddVehiclePacketOut(vehicle));
                    }
                }
                ids.Remove(vehicle.Handle);
                if (needsVehUpdate)
                {
                    foreach (GTAVServerClientConnection connection in Connections)
                    {
                        connection.SendPacket(new UpdateVehiclePacketOut(vehicle));
                    }
                }
            }
            foreach (int id in ids)
            {
                Vehicles.Remove(id);
                foreach (GTAVServerClientConnection connection in Connections)
                {
                    connection.SendPacket(new RemoveVehiclePacketOut(id));
                }
            }
            bool isInVehicle = Game.Player.Character.IsSittingInVehicle();
            if (isInVehicle && (!wasInVehicle || DateTime.Now.Subtract(nextVehicleReminder).TotalSeconds > 1.0))
            {
                nextVehicleReminder = DateTime.Now;
                foreach (GTAVServerClientConnection connection in Connections)
                {
                    connection.SendPacket(new EnterVehiclePacketOut(Game.Player.Character.CurrentVehicle, Game.Player.Character.SeatIndex));
                }
            }
            else if (!isInVehicle && wasInVehicle)
            {
                foreach (GTAVServerClientConnection connection in Connections)
                {
                    connection.SendPacket(new ExitVehiclePacketOut());
                }
            }
            wasInVehicle = isInVehicle;
            bool hasModel = ModelEnforcementScript.WantedModel.HasValue;
            if (hasModel)
            {
                int cModel = ModelEnforcementScript.WantedModel.Value.Hash;
                if (pModel != cModel)
                {
                    foreach (GTAVServerClientConnection connection in Connections)
                    {
                        connection.SendPacket(new SetModelPacketOut(cModel));
                    }
                    pModel = cModel;
                }
            }
            pHadModel = hasModel;
        }

        bool pHadModel;

        int pModel;

        bool wasInVehicle = false;

        DateTime nextVehicleReminder = DateTime.Now;
        
        DateTime nextVehicleUpdate = DateTime.Now;
        
        int ammo = 0;
        WeaponHash weap = WeaponHash.Unarmed;

        bool pjump = false;
        
        public static HashSet<int> Vehicles = new HashSet<int>();

        public void Listen(ushort port)
        {
            Listener = new TcpListener(IPAddress.IPv6Any, port);
            Listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            Listener.Start();
        }
    }
}
