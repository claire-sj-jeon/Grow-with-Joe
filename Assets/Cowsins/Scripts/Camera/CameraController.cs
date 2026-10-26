using UnityEngine;
using UnityEditor;

namespace cowsins2D
{
    public class CameraController : MonoBehaviour
    {
        [System.Serializable]
        public enum CameraControllerMethod
        {
            Static,
            FollowTarget,
            FollowTargetWhenCloseToBounds
        };

        [Tooltip("Configures the style of the camera, the way it behaves")] public CameraControllerMethod cameraMethod;

        [SerializeField, Tooltip("What object does this camera follow.")] private Transform target;

        [SerializeField, Tooltip("Camera position variation")] private Vector3 cameraOffset;

        [SerializeField, Tooltip("Size of the camera vision. Once the target is outside of these boundaries, it will start following the target.")] private Vector2 boundary;

        [SerializeField, Tooltip("The lower, the faster it will reach the destination.")] private float cameraLaziness;

        [SerializeField] private float fovSmoothness;


        private float fov;
        private Camera cam;
        Vector3 savecameraOffset;

        // 시간제 포커스의 복귀 타이머. 호출자가 아니라 카메라가 들고 있어야
        // 아이템처럼 곧 파괴될 오브젝트가 시점을 그대로 굳혀 놓지 않는다.
        RegisterFunction.Element focusTimer;

        public  void ChangeCameraOffset(Vector3 offset )
        {
            // 새 오프셋이 들어오면 이전 포커스의 복귀 예약은 무효다
            CancelFocusTimer();
            cameraOffset = offset;
        }
        public void RecoverCameraOffset()
        {
            CancelFocusTimer();
            cameraOffset = savecameraOffset;
        }

        // 지정한 월드 좌표가 화면 중앙에 오도록 오프셋을 잡는다.
        // 카메라는 target.position + cameraOffset 을 따라가므로 두 위치의 차이가 그대로 오프셋이 된다.
        // z 는 카메라 거리라 연출과 무관하므로 원래 값을 유지한다.
        public void FocusOnPosition(Vector3 worldPosition)
        {
            if (target == null) return;

            Vector3 offset = worldPosition - target.position;
            offset.z = savecameraOffset.z;
            ChangeCameraOffset(offset);
        }

        // duration 초 동안 보여준 뒤 원래 시점으로 돌아온다.
        public void FocusOnPosition(Vector3 worldPosition, float duration)
        {
            if (target == null) return;

            // ChangeCameraOffset 이 이전 타이머를 먼저 지우므로 여기서 새로 걸어도 안전하다
            FocusOnPosition(worldPosition);

            focusTimer = RegisterFunction.SingleCall(duration, () =>
            {
                focusTimer = null;
                RecoverCameraOffset();
            });
        }

        void CancelFocusTimer()
        {
            if (focusTimer == null) return;

            focusTimer.Remove();
            focusTimer = null;
        }

        private void Awake()
        {
            cam = transform.GetChild(0).GetComponent<Camera>();
            fov = cam.fieldOfView;
            savecameraOffset = cameraOffset;
        }

        // 파괴된 카메라를 콜백이 건드리지 않도록 예약을 정리한다
        private void OnDestroy() => CancelFocusTimer();

        private void Update()
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, fovSmoothness * Time.deltaTime);
        }


        // Called in LateUpdate for proper smoothness
        private void LateUpdate()
        {
            // Depending on the camera method, call different functions
            switch (cameraMethod)
            {
                case CameraControllerMethod.FollowTarget:
                    SimpleFollowTarget();
                    break;
                case CameraControllerMethod.FollowTargetWhenCloseToBounds:
                    BoundsFollow();
                    break;
            }
        }

        // Simply lerp the position of this object from the current one to the target and add the camera offset
        private void SimpleFollowTarget() => transform.position = Vector3.Lerp(transform.position, target.position + (Vector3)cameraOffset, Time.deltaTime * 1 / cameraLaziness);

        // Only follow if out of bounds
        private void BoundsFollow()
        {
            Vector2 dif = Vector2.zero;

            // Grab the distance in the horizontal axis from this to the target
            float difX = target.position.x - transform.position.x;


            // If this distance is greater than the boundary horizontal size ( left or right )
            if (difX > boundary.x || difX < -boundary.x)
            {
                if (transform.position.x < target.position.x) dif.x = difX - boundary.x;
                else dif.x = difX + boundary.x;
            }

            // Same process for the vertical axis (y)
            float difY = target.position.y - transform.position.y;

            if (difY > boundary.y || difY < -boundary.y)
            {
                if (transform.position.y < target.position.y) dif.y = difY - boundary.y;
                else dif.y = difY + boundary.x;
            }

            // Lerp the position
            transform.position = Vector3.Lerp(transform.position, transform.position + (Vector3)dif, Time.deltaTime * 1 / cameraLaziness);
        }

        public void CameraToStatic() => cameraMethod = CameraControllerMethod.Static;

        public void CameraToSimple() => cameraMethod = CameraControllerMethod.FollowTarget;

        public void CameraToBoundary() => cameraMethod = CameraControllerMethod.FollowTargetWhenCloseToBounds;
    }


#if UNITY_EDITOR
    [System.Serializable]
    [CustomEditor(typeof(CameraController))]
    public class CameraControllerEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            serializedObject.Update();
            CameraController myScript = target as CameraController;

            #region variables
            GUILayout.Space(20);
            EditorGUILayout.LabelField("CAMERA CONTROLLER", EditorStyles.boldLabel);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(8) });
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMethod"));
            if (myScript.cameraMethod == CameraController.CameraControllerMethod.FollowTargetWhenCloseToBounds)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boundary"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraLaziness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fovSmoothness"));
            #endregion



            serializedObject.ApplyModifiedProperties();

        }
    }
#endif
}
