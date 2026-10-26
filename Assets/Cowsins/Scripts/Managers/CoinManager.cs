using UnityEngine;
using System;

namespace cowsins2D
{
    public class CoinManager : MonoBehaviour
    {
        //Stores the player's current coin count.
        public int coins { get; private set; }

        public static event Action<int> OnCoinsChanged;

        public static CoinManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void AddCoins(int amount)
        {
            // Increase the player's coin count by the specified amount.
            coins += amount;

            // Update the coins UI to reflect the player's new balance.
            OnCoinsChanged?.Invoke(coins);
        }

        public void RemoveCoins(int amount)
        {
            // Decrease the player's coin count by the specified amount.
            coins -= amount;

            // If the player's coin count is less than zero, set it to zero.
            if (coins < 0) coins = 0;

            // Update the coins UI to reflect the player's new balance.
            OnCoinsChanged?.Invoke(coins);
        }
    }
}

