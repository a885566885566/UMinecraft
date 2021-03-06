﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiveManager : MonoBehaviour {
    public float live;
    public Const.GameItemID itemId;
    float lastAttackTime = 0;
    public void reset(Const.GameItemID newId)
    {
        itemId = newId;
        live = ItemMap.getLive(itemId);
    }
    public void attack(float power)
    {
        if(Time.time - lastAttackTime > Const.attackTimeInterval) {
            live -= power;
            lastAttackTime = Time.time;
        }
    }
    public void relive()
    {
        live = ItemMap.getLive(itemId);
    }
    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {

    }
}
public class ItemCtrl:LiveManager
{
    int instanceId;
    public ItemCtrl(Const.GameItemID newId, int newInstanceId = 0)
    {
        base.reset(newId);
    }
    public int isAlive(Const.GameItemID newId, int newInstanceId, float attack = 1)
    {
        //Debug.Log(itemId.ToString() + " = " + live.ToString());
        if (newInstanceId != instanceId) {
            instanceId = newInstanceId;
            itemId = newId;
            relive();
            //return -1;
        }
        else
            live -= attack;
        return Mathf.CeilToInt(10f * live / ItemMap.getLive(newId));
    }
}