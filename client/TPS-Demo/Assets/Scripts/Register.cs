using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Register : MonoBehaviour
{
    public InputField  AccountInput;
    public InputField  PasswordInput;
    public Button RegisterBtn;

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

    public void RegisterUser()
    {
        string account = AccountInput.text;
        string password = PasswordInput.text;
        network.CreatAccount(account, password);
    }
}
