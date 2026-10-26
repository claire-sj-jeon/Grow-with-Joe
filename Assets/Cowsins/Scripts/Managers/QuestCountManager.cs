using UnityEngine;
using System;

namespace cowsins2D
{
    public class QuestCountManager : MonoBehaviour
    {        
        public int questCount { get; private set; }

        public UiObject uiQuestText;

        public static event Action<int> OnQuestChange;
        public static QuestCountManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }
        void UpdateToUI()
        {
            uiQuestText.Text.text = $"{questCount}/5";
        }


        public void Reset()
        {
            questCount = 0;
            UpdateToUI();

            OnQuestChange?.Invoke(questCount);
        }
        public void AddQuest(int amount)
        {
            questCount += amount;
            UpdateToUI();

            OnQuestChange?.Invoke(questCount);
        }

        public void RemoveCoins(int amount)
        {
            questCount -= amount;
            UpdateToUI();

            if (questCount < 0) questCount = 0;
            OnQuestChange?.Invoke(questCount);
        }
    }
}

