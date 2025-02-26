using Microsoft.Extensions.Configuration;
using System;
using System.Timers;
using Gma.System.MouseKeyHook;
using Newtonsoft.Json;

namespace WorkTrackerWPFApp.Services
{
    public class MouseKeyboardTracker : IDisposable
    {
        private DateTime _lastInputTime;
        private bool _isIdle;
        private int _idleCheckTime;
        private int _mouseClickCount;
        private int _keyPressCount;
        private List<string> _keyInputs;
        private IKeyboardMouseEvents _globalHook;
        private System.Timers.Timer _idleCheckTimer;

        public MouseKeyboardTracker(IConfiguration config)
        {
            _lastInputTime = DateTime.Now;
            _isIdle = false;
            _mouseClickCount = 0;
            _keyPressCount = 0;
            _keyInputs = new List<string>();

            _idleCheckTime = int.TryParse(config["IdleTimeCheckForScreenshot"], out var idleCheckTime) ? idleCheckTime : 2;

            // Initialize global hook for keyboard and mouse events
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += OnKeyPress;
            _globalHook.MouseMove += OnMouseClick;

            // Timer to periodically check idle status
            _idleCheckTimer = new System.Timers.Timer(1000); // Check every second
            _idleCheckTimer.Elapsed += CheckIdleStatus;
            _idleCheckTimer.Start();
        }

        private void OnKeyPress(object sender, KeyEventArgs e)
        {
            _lastInputTime = DateTime.Now;
            _isIdle = false;
            _keyPressCount++;
            _keyInputs.Add(e.KeyCode.ToString());
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            _lastInputTime = DateTime.Now;
            _isIdle = false;
            _mouseClickCount++;
        }

        private void CheckIdleStatus(object sender, ElapsedEventArgs e)
        {
            if ((DateTime.Now - _lastInputTime).TotalMinutes >= _idleCheckTime)
            {
                _isIdle = true;
            }
            if (_mouseClickCount < 10 || _keyPressCount < 10)
            {
                _isIdle = true;
            }
            ResetCounts();
        }
        private void ResetCounts()
        {
            _mouseClickCount = 0;
            _keyPressCount = 0;
            _keyInputs.Clear();
        }
        public string GetUserInputData()
        {
            var data = new
            {
                IsIdle = _isIdle,
                MouseClicks = _mouseClickCount,
                KeyPresses = _keyPressCount,
                KeyInputs = _keyInputs
            };
            return JsonConvert.SerializeObject(data);
        }
        public bool IsIdle() => _isIdle;

        public void Dispose()
        {
            _globalHook.KeyDown -= OnKeyPress;
            _globalHook.MouseMove -= OnMouseClick;
            _globalHook.Dispose();
            _idleCheckTimer.Stop();
            _idleCheckTimer.Dispose();
        }
    }
}
