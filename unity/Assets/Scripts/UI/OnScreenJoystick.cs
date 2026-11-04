using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace MobControl.UI
{
    /// <summary>
    /// Left-Right Steering Joystick for endless runner lane dodging.
    /// Supports direct knob dragging, screen-wide lower touch dragging, and keyboard (A/D & Arrows).
    /// </summary>
    public class OnScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static OnScreenJoystick Instance { get; private set; }

        [Header("UI Rects")]
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knobRect;
        [SerializeField] private TextMeshProUGUI hintText;

        [Header("Tuning")]
        [SerializeField] private float handleRange = 85.0f;
        [SerializeField] private float returnSpeed = 16.0f;
        [SerializeField] private float sensitivity = 1.0f;

        private float horizontalAxis = 0.0f;
        private bool isDragging = false;
        private Vector2 knobAnchoredPos;
        private Canvas parentCanvas;

        public float HorizontalAxis => horizontalAxis;
        public bool IsDragging => isDragging;
        public bool IsActive => gameObject.activeInHierarchy;

        private void Awake()
        {
            // Destroy visual joystick as per user request
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void Start()
        {
            if (knobRect != null) knobAnchoredPos = knobRect.anchoredPosition;
        }

        private void OnEnable()
        {
            ResetJoystick();
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        public void ResetJoystick()
        {
            horizontalAxis = 0f;
            isDragging = false;
            if (knobRect != null) knobRect.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            // Only active during gameplay
            if (ZombieRunnerUIManager.Instance != null &&
                ZombieRunnerUIManager.Instance.CurrentState != ZombieRunnerUIManager.ScreenState.Gameplay)
            {
                if (horizontalAxis != 0f) ResetJoystick();
                return;
            }

            // Keyboard Override (A/D or Left/Right Arrow)
            float kbInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(kbInput) > 0.05f)
            {
                horizontalAxis = Mathf.Clamp(kbInput, -1.0f, 1.0f);
                if (knobRect != null)
                {
                    knobRect.anchoredPosition = new Vector2(horizontalAxis * handleRange, 0f);
                }
                return;
            }

            // Screen-wide lower touch/mouse drag fallback for seamless mobile/editor play
            HandleScreenWideDrag();

            // Smooth return to center when released
            if (!isDragging && Mathf.Abs(kbInput) <= 0.05f)
            {
                if (Mathf.Abs(horizontalAxis) > 0.001f)
                {
                    horizontalAxis = Mathf.Lerp(horizontalAxis, 0f, returnSpeed * Time.deltaTime);
                    if (knobRect != null)
                    {
                        knobRect.anchoredPosition = new Vector2(horizontalAxis * handleRange, 0f);
                    }
                }
                else
                {
                    horizontalAxis = 0f;
                    if (knobRect != null) knobRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        private float touchOriginX = 0f;
        private bool screenDragActive = false;

        private void HandleScreenWideDrag()
        {
            // If user is already dragging the joystick directly, don't double process
            if (isDragging) return;

            // Touch screen drag
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                // Lower 55% of screen
                if (touch.phase == TouchPhase.Began && touch.position.y < Screen.height * 0.58f)
                {
                    touchOriginX = touch.position.x;
                    screenDragActive = true;
                }
                else if (screenDragActive && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    float deltaX = touch.position.x - touchOriginX;
                    float normalized = Mathf.Clamp(deltaX / (Screen.width * 0.22f), -1f, 1f);
                    horizontalAxis = normalized;
                    if (knobRect != null)
                    {
                        knobRect.anchoredPosition = new Vector2(horizontalAxis * handleRange, 0f);
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    screenDragActive = false;
                }
            }
            // Mouse drag (Editor)
            else if (Input.GetMouseButtonDown(0) && Input.mousePosition.y < Screen.height * 0.58f)
            {
                touchOriginX = Input.mousePosition.x;
                screenDragActive = true;
            }
            else if (screenDragActive && Input.GetMouseButton(0))
            {
                float deltaX = Input.mousePosition.x - touchOriginX;
                float normalized = Mathf.Clamp(deltaX / (Screen.width * 0.22f), -1f, 1f);
                horizontalAxis = normalized;
                if (knobRect != null)
                {
                    knobRect.anchoredPosition = new Vector2(horizontalAxis * handleRange, 0f);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                screenDragActive = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            UpdateHandlePosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateHandlePosition(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void UpdateHandlePosition(PointerEventData eventData)
        {
            if (baseRect == null || knobRect == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out localPoint);

            float clampedX = Mathf.Clamp(localPoint.x, -handleRange, handleRange);
            knobRect.anchoredPosition = new Vector2(clampedX, 0f);
            horizontalAxis = Mathf.Clamp(clampedX / handleRange, -1.0f, 1.0f) * sensitivity;
        }

        /// <summary>
        /// Creates a high-definition Joystick UI element inside the given parent.
        /// </summary>
        public static OnScreenJoystick CreateJoystick(Transform parent)
        {
            // Removed visual joystick banner as per user request
            if (parent != null)
            {
                Transform existing = parent.Find("OnScreenJoystick_Steering");
                if (existing != null)
                {
                    if (Application.isPlaying) Destroy(existing.gameObject);
                    else DestroyImmediate(existing.gameObject);
                }
            }
            return null;
        }
    }
}
