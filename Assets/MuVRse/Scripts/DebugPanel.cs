using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static OVRInput;


namespace VRSYS.MuVRse.Scripts
{
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private InputActionReference _activateCanvas;
        [SerializeField] private InputActionReference _scrollInput;
        [SerializeField] private TextMeshProUGUI _debugTextMesh;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private float _scrollSpeed = 0.1f;
        [SerializeField] private bool _isLogging = true;
        
        private const int MAX_LOGS = 500;
        private const int CLEANUP_AMOUNT = 500;
        
        private List<string> _logLines = new List<string>();
        private float _scrollValue;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _scrollInput.action.Enable();
            _activateCanvas.action.Enable();
        }

        private void OnDestroy()
        {
            _scrollInput.action.Disable();
            _activateCanvas.action.Disable();
        }

        private void OnEnable()
        {
            _scrollInput.action.performed += OnUIScrollAction;
            _activateCanvas.action.performed += OnToggleCanvas;
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            _scrollInput.action.performed -= OnUIScrollAction;
            _activateCanvas.action.performed -= OnToggleCanvas;
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            Debug.Log("Log received");
            // Format log message based on type
            string prefix = type switch
            {
                LogType.Error => "<color=red>[ERROR]</color>",
                LogType.Warning => "<color=yellow>[WARN]</color>",
                LogType.Exception => "<color=red>[EXCEPTION]</color>",
                _ => "<color=white>[INFO]</color>"
            };

            AddLogLine($"{prefix} {logString}");
        }
        
        private void AddLogLine(string newLine)
        {
            // Add timestamp
            string timestamp = Time.time.ToString("F1");
            string formattedLine = $"[{timestamp}] {newLine}";
        
            _logLines.Add(formattedLine);
            if (_logLines.Count > MAX_LOGS)
            {
                // Remove the first CLEANUP_AMOUNT logs
                _logLines.RemoveRange(0, CLEANUP_AMOUNT);
                AddLogLine("<color=yellow>[SYSTEM] Cleaned up old logs</color>");
            }
            // Update TextMeshPro
            if (_debugTextMesh != null && _isLogging)
            {
                _debugTextMesh.text = string.Join("\n", _logLines);
                Canvas.ForceUpdateCanvases();
            }
        }
        
        
        private void OnUIScrollAction(InputAction.CallbackContext context)
        {
        
            _scrollValue = context.action.ReadValue<Vector2>().y;
            float newScrollPos = _scrollRect.verticalNormalizedPosition + (_scrollValue * _scrollSpeed);
            _scrollRect.verticalNormalizedPosition = newScrollPos;
        }

        private void OnToggleCanvas(InputAction.CallbackContext context)
        {
            _canvas.enabled = !_canvas.enabled;
        }
    }
}