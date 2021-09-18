using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Timers;
using System.Web;
using System.Windows.Forms;

namespace ArrowsCursorGUI
{
    public class Controller
    {
        public event EventHandler SpeedChanged;
        private readonly GlobalKeyboardHook Hook;
        private readonly CursorMovementUpdateLoop Loop; 

        // We could create interfaces and private constructors with Functions to expose only the things that
        // the world is allowed to modify, but let's save ourselves the 100 lines of boilerplate.
        public readonly ArrowsObj Arrows = new ArrowsObj();
        public readonly MainKeyObj MainKey = new MainKeyObj();
        public readonly ClickKeyObj ClickKey = new ClickKeyObj();
        public readonly SpeedObj Speed = new SpeedObj();
        public bool Consume = false;

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        

        public Controller()
        {
            Loop = new CursorMovementUpdateLoop(Arrows, Speed);
            Hook = new GlobalKeyboardHook(code =>
            {
                bool isArrow = Arrows.Arrows.Any(tuple => tuple.code == code);
                
                return (isArrow && MainKey.Pressed) || (Consume && (code == MainKey.Code || code == ClickKey.Code));
            });
        }

        public class SpeedObj
        {
            public readonly int[] Levels = { 7, 15, 21 };
            public int CurrentLevel { get; private set; } = 1;
            public int CurrentSpeed { get; private set; }

            public void ChangeLevel(int index, int value)
            {
                Levels[index] = value;
                
                if (CurrentLevel == index) 
                    CurrentSpeed = Levels[CurrentLevel];
            }

            public SpeedObj() => CurrentSpeed = Levels[CurrentLevel];

            public void SetNextLevel()
            {
                if (++CurrentLevel >= Levels.Length)
                    CurrentLevel = 0;
                
                CurrentSpeed = Levels[CurrentLevel];
            }
        }

        public class MainKeyObj
        {
            public const byte DoubleClickTimeMs = 200;
            public int LastPressTime;
            public int Code = 164;
            public bool Pressed;
        }

        public class ClickKeyObj
        {
            public int Code = 163;
            public bool Pressed = false;
        }
        
        public class ArrowsObj
        {
            public enum Arrow {Left = 0, Up = 1, Right = 2, Down = 3}

            public readonly (int code, bool pressed)[] Arrows =
            {
                (37, false), // Left
                (38, false), // Up
                (39, false), // Right
                (40, false), // Down 
            };

            public (int code, bool pressed) GetArrow(Arrow arrow) => Arrows[(int)arrow];
        }
        
        private class CursorMovementUpdateLoop
        {
            private readonly ArrowsObj Arrows;
            private readonly SpeedObj Speed;
            private readonly System.Timers.Timer Timer = new System.Timers.Timer(13)
                { AutoReset = true, Enabled = false };

            public CursorMovementUpdateLoop(ArrowsObj arrows, SpeedObj speed)
            {
                Arrows = arrows;
                Speed = speed;
                Timer.Elapsed += (obj, e) => OnTimerTick();
            }

            public void Resume() => Timer.Enabled = true;
            public void Pause() => Timer.Enabled = false;

            private void OnTimerTick()
            {
                int x = 0, y = 0;

                if (Arrows.GetArrow(ArrowsObj.Arrow.Down).pressed)
                    y += Speed.CurrentSpeed;
                if (Arrows.GetArrow(ArrowsObj.Arrow.Up).pressed)
                    y -= Speed.CurrentSpeed;
                if (Arrows.GetArrow(ArrowsObj.Arrow.Left).pressed)
                    x -= Speed.CurrentSpeed;
                if (Arrows.GetArrow(ArrowsObj.Arrow.Right).pressed)
                    x += Speed.CurrentSpeed;

                Cursor.Position = new Point(Cursor.Position.X + x, Cursor.Position.Y + y);
            }
        }

        public void Run()
        {
            Hook.KeyboardPressed += (_, e) => HandleKeyboardPressed(e);
            SpeedChanged?.Invoke(this, EventArgs.Empty);
        }
        
        private void HandleKeyboardPressed(GlobalKeyboardHookEventArgs e)
        {
            bool isKeyDown = e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown || e.KeyboardState == GlobalKeyboardHook.KeyboardState.SysKeyDown;
            Debug.WriteLine(e.KeyboardState + " isdown: " + isKeyDown);
            int code = e.KeyboardData.VirtualCode;
            bool isMainKey = code == MainKey.Code;
            bool isClickKey = code == ClickKey.Code;
            
            if (isClickKey && (!ClickKey.Pressed || !isKeyDown))
            {
                Debug.WriteLine(isKeyDown);
                ClickMouse(isKeyDown);
            }

            if (isMainKey && isKeyDown && !MainKey.Pressed) 
                HandleMainKeyDownPressed();

            if (!UpdatePressedState(code, isKeyDown, isMainKey, isClickKey))
                return;

            if (!MainKey.Pressed || !Arrows.Arrows.Any(arrow => arrow.pressed))
                Loop.Pause();
            else if (MainKey.Pressed && Arrows.Arrows.Any(arrow => arrow.pressed)) 
                Loop.Resume();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        private static void ClickMouse(bool down) =>
            mouse_event((down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP), Cursor.Position.X, Cursor.Position.Y, 0, 0);

        private void HandleMainKeyDownPressed()
        {
            if (Environment.TickCount - MainKey.LastPressTime <= MainKeyObj.DoubleClickTimeMs)
            {
                ExecuteMainKeyDoubleClickAction();
                MainKey.LastPressTime = 0;
            }
            else
                MainKey.LastPressTime = Environment.TickCount;
        }

        private bool UpdatePressedState(int code, bool isKeyDown, bool isMainKey, bool isClickKey)
        {
            if (isMainKey)
            {
                MainKey.Pressed = isKeyDown;
                return true;
            }

            if (isClickKey)
            {
                ClickKey.Pressed = isKeyDown;
                return true;
            }

            for (byte i = 0; i < Arrows.Arrows.Length; ++i)
                if (Arrows.Arrows[i].code == code)
                {
                    Arrows.Arrows[i].pressed = isKeyDown;
                    return true;
                }

            return false;
        }
        
        private void ExecuteMainKeyDoubleClickAction()
        {
            Speed.SetNextLevel();
            SpeedChanged?.Invoke(this, EventArgs.Empty);
            // TODO: Flash the level number on the screen.
        }
    }
}