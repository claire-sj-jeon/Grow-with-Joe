using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace cowsins2D
{
    public class ItemQuest : Trigger
    {        
        [SerializeField, Tooltip("Sound on picking the coin. ")] private AudioClip pickUpSFX;
        public UnityEvent onPickUp;

        [SerializeField] 
        float moveSpeed = 1.0f; // Speed of the movement
        
        [SerializeField] 
        float moveDistance = 1.0f; // Distance to move up and down        
        
        [SerializeField] 
        float needQuestCount = 1.0f; // Speed of the movement
        
        [SerializeField] 
        List<DialogForm> listDialog;

        private Vector3 startPos;

        bool activity
        {
            get { return gameObject.activeSelf; }
            set { gameObject.SetActive(value); }
        }

        void QuestChange( int count )
        {
            Debug.Log($" Quest ==> {count} ");

            if (needQuestCount <= QuestCountManager.Instance.questCount)
                if(activity == false)
                    activity = true;                
        }
        private void Start()
        {   
            activity = needQuestCount <= QuestCountManager.Instance.questCount;
        }
        private void OnEnable()
        {
            startPos = transform.position;
            StopAllCoroutines();
            StartCoroutine(StartMovingWithDelay());            
        }
        private void OnDisable()
        {
            transform.position = startPos;
            StopAllCoroutines();            
        }
        void Awake()
        {
            QuestCountManager.OnQuestChange += QuestChange;
        }
        void OnDestroy()
        {
            QuestCountManager.OnQuestChange -= QuestChange;
        }
        private IEnumerator StartMovingWithDelay()
        {
            float offset = Random.Range(.9f, 1.4f);

            // Start moving
            while (true)
            {   // Calculate vertical movement using a sine wave
                float verticalMovement = Mathf.Sin(Time.time * moveSpeed * offset) * moveDistance;
                transform.position = startPos + new Vector3(0, verticalMovement, 0);
                yield return null;
            }
        }
        public override void EnterTrigger(GameObject target)
        {
            // Perform custom events
            onPickUp?.Invoke();

            // Add the coins to the player
            //CoinManager.Instance.AddCoins(Random.Range(minimumAmount, maximumAmount));
            DialogMenu.Instance.ShowDialog(listDialog);

            // Play SFX
            SoundManager.Instance.PlaySound(pickUpSFX, 1);
            Destroy(this.gameObject);
            base.EnterTrigger(target);
        }
    }
}