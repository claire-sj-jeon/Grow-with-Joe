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

        [SerializeField, Tooltip("퀘스트 조건이 채워져 나타날 때 카메라가 이 아이템을 비추는 시간(초). 0 이면 비추지 않는다.")]
        float revealFocusTime = 2.0f;

        [SerializeField, Tooltip("dialogs.json 의 quests 키. 비워두면 게임오브젝트 이름을 사용한다.")]
        string dialogId;

        [SerializeField]
        List<DialogForm> listDialog;

        // 익스포트와 런타임 조회가 반드시 같은 키를 만들어야 하므로 폴백 규칙을 여기 한 곳에 둔다.
        // 이름을 쓰는 동안에는 오브젝트를 renaming 하면 JSON 매핑이 끊긴다.
        // 이름을 고정하고 싶으면 Tools > Dialog > Assign dialogId from Object Name 으로 값을 박아 둔다.
        //
        // Trim: id 가 곧 파일 이름이 된다. 오브젝트 이름 끝의 공백은 하이어라키에서 보이지 않는데
        // 파일 이름에 섞이면 Resources.Load 가 찾지 못한다. 양쪽 다 Trim 해 항상 같은 키를 만든다.
        public string DialogId =>
            (string.IsNullOrWhiteSpace(dialogId) ? gameObject.name : dialogId).Trim();

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
                {
                    activity = true;
                    ShowSelfWithCamera();
                }
        }

        // 새로 나타난 아이템이 어디에 생겼는지 잠깐 비춰 준다.
        // 복귀 타이머는 카메라가 들고 있으므로, 비추는 동안 이 오브젝트를 먹어서
        // 파괴하더라도 시점은 정상적으로 되돌아온다.
        void ShowSelfWithCamera()
        {
            if (revealFocusTime <= 0.0f) return;

            var gameCamera = DialogMenu.Instance != null ? DialogMenu.Instance.GameCamera : null;
            if (gameCamera == null) return;

            gameCamera.FocusOnPosition(transform.position, revealFocusTime);
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
            ResolveDialogFromJson();
        }

        // JSON 에 해당 dialogId 가 없으면 인스펙터 값을 유지한다. 인스턴스별로 점진 이행이 가능하다.
        void ResolveDialogFromJson()
        {
            var fromJson = DialogJsonLoader.Load(DialogId);
            if (fromJson != null && fromJson.Count > 0) listDialog = fromJson;
        }

        // DialogMenu.ReloadAllDialogs (개발용 R 키) 가 호출한다.
        // 캐시를 비우는 쪽이 호출자이므로 여기서는 다시 읽기만 한다.
        public void ReloadDialog() => ResolveDialogFromJson();

#if UNITY_EDITOR
        // 익스포터 전용 접근자
        public List<DialogForm> Editor_Dialog => listDialog;
        public bool Editor_HasExplicitDialogId => !string.IsNullOrWhiteSpace(dialogId);
#endif
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