using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Health health;
    public PlayerWeaponsManager weaponsManager;
    public Image healthBarImage;
    public Text AmmoTxt;
    public Text HPTxt;

    public Text EXPTxt;

    Attributes attributes;

    // Start is called before the first frame update
    void Start()
    {
        attributes = GetComponentInParent<Attributes>();
    }

    // Update is called once per frame
    void Update()
    {
        healthBarImage.fillAmount = health.currentHealth / health.maxHealth;
        HPTxt.text = health.currentHealth + "/" + health.maxHealth;

        if(weaponsManager.GetActiveWeapon())
            AmmoTxt.text = "Ammo: " + weaponsManager.GetActiveWeapon().m_CurrentAmmo + "/" + weaponsManager.GetActiveWeapon().maxAmmo;

        EXPTxt.text = "EXP: " + attributes.getExp().ToString();
    }

}
