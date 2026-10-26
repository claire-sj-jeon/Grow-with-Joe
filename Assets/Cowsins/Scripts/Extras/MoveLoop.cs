using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace cowsins2D
{
    public class MoveLoop : Trigger
    {
        [SerializeField]
        bool ismoveX;

        [SerializeField]
        float moveSpeed = 1.0f; // Speed of the movement

        [SerializeField]
        float moveDistance = 1.0f; // Distance to move up and down                

        private void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(StartMovingWithDelay());
        }
        private void OnDisable()
        {            
            StopAllCoroutines();
         
        }        
        private IEnumerator StartMovingWithDelay()
        {
            float offset = Random.Range(.9f, 1.4f);

            // Start moving
            while (true)
            {   // Calculate vertical movement using a sine wave
                float verticalMovement = Mathf.Sin(Time.time * moveSpeed * offset) * moveDistance;

                transform.localPosition =  new Vector3(
                    ismoveX == true ? verticalMovement :0, 
                    ismoveX != true ? verticalMovement :0, 
                    0);
                yield return null;
            }
        }
    }
}