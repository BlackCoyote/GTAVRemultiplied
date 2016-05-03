﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTAVRemultiplied;

public class ModelEnforcementScript : Script
{
    public static Model? WantedModel = null;

    public ModelEnforcementScript()
    {
        Tick += ModelEnforcementScript_Tick;
    }

    bool CanTick = true;

    private void ModelEnforcementScript_Tick(object sender, EventArgs e)
    {
        if (!CanTick)
        {
            return;
        }
        try
        {
            if (Game.Player.IsDead && WantedModel != null && WantedModel.HasValue && Game.Player.Character.Model == WantedModel.Value)
            {
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, "player_zero");
                SetModel(new Model(hash));
                return;
            }
            if (WantedModel != null && WantedModel.HasValue && WantedModel.Value != Game.Player.Character.Model)
            {
                if (!SetModel(WantedModel.Value))
                {
                    WantedModel = Game.Player.Character.Model;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
        }
    }

    bool SetModel(Model mod)
    {
        if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, mod.Hash) || !Function.Call<bool>(Hash.IS_MODEL_VALID, mod.Hash))
        {
            return false;
        }
        CanTick = false;
        Function.Call(Hash.REQUEST_MODEL, mod.Hash);
        while (!Function.Call<bool>(Hash.HAS_MODEL_LOADED, mod.Hash))
        {
            Wait(0);
        }
        Function.Call(Hash.SET_PLAYER_MODEL, Game.Player.Handle, mod.Hash);
        Game.Player.Character.SetDefaultClothes();
        CanTick = true;
        return true;
    }
}
