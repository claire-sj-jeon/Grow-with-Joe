using UnityEngine;

namespace cowsins2D
{
    [System.Serializable]
    public class WeaponState
    {
        [Tooltip("The weapon Scriptable Object")]
        public Weapon_SO weapon;
        
        [Tooltip("Current amount of bullets in the magazine")]
        public int currentBullets;

        [Tooltip("Total amount of bullets available to this weapon")]
        public int totalBullets;
        
        public WeaponState(Weapon_SO weapon, int currentBullets, int totalBullets)
        {
            this.weapon = weapon;
            this.currentBullets = currentBullets;
            this.totalBullets = totalBullets;
        }
    }
}
