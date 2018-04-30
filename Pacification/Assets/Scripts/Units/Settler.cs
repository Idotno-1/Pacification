﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settler : Unit
{
    public Settler(Player owner, int id)
    {
        this.owner = owner;
        this.id = id;
        type = UnitType.WORKER;
        mvtSPD = 2;
        hp = 100;

        // TODO : couleur du joueur
    }

    public void Settle()
    {
        owner.AddCity(hexUnit.location);
        owner.RemoveUnit(this);
    }
}