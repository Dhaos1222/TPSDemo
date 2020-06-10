using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawn : MonoBehaviour
{

    NetworkSocket1 network;

    public GameFlowManager gameFlowManager;
    void Awake()
    {
        network = GetComponent<NetworkSocket1>();
        if(!network)
        {
            network = GetComponentInParent<NetworkSocket1>();
        }
    }



    public void RespawnPlayer()
    {
        network.isAlive = true;
        network.isRespawning = true;


        gameFlowManager.gameIsEnding = false;
        gameObject.SetActive(false);
        Debug.Log("Respawn");
    }
}
