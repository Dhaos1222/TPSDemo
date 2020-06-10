using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attributes : MonoBehaviour
{

    Health health;
    PlayerWeaponsManager playerWeaponsManager;
    float exp;
    // Start is called before the first frame update
    void Awake()
    {
        health = GetComponent<Health>();
        playerWeaponsManager = GetComponent<PlayerWeaponsManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setAttributes(float hp, float ammo, float exp)
    {
        health.currentHealth = hp;
        WeaponController activeWeapon = playerWeaponsManager.GetActiveWeapon();
        if(activeWeapon)
            activeWeapon.m_CurrentAmmo = ammo;
        this.exp = exp;
    }

    public void fullstate()
    {
        float max_hp = health.maxHealth;
        WeaponController activeWeapon = playerWeaponsManager.GetActiveWeapon();
        float ammo = 30;
        if(activeWeapon)
            ammo = activeWeapon.maxAmmo;
        setAttributes(max_hp, ammo, this.exp);
    }

    public float getHp()
    {
        return health.currentHealth;
    }

    public float getAmmo()
    {
        WeaponController activeWeapon = playerWeaponsManager.GetActiveWeapon();
        if(activeWeapon)
            return activeWeapon.m_CurrentAmmo;
        else
            return 30f;
    }

    public float getExp()
    {
        return exp;
    }
}
