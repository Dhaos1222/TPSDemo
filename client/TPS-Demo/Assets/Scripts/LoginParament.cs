using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginParament : MonoBehaviour
{
    public InputField  AccountInput;
    public InputField  PasswordInput;
    public Button LoginBtn;

    NetworkSocket1 network;

    public string pid;

    void Awake()
    {
        network = GetComponent<NetworkSocket1>();
        if(!network)
        {
            network = GetComponentInParent<NetworkSocket1>();
        }
    }

    public void Login()
    {
        string account = AccountInput.text;
        string password = PasswordInput.text;
        
        network.SendAuthIdentity(account, password);
        // gameObject.SetActive(false);
    }

    // public void shutdown()
    // {
    //     network.ShutdownServer();
    // }
}
