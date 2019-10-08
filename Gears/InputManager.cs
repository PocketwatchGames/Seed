using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Gears.Input {

	public enum InputSource {
		Keyboard,
		Mouse,
		GamepadButton,
		GamepadAxis
	}

	public enum GamepadAxis {
		LA_Left,
		LA_Right,
		LA_Up,
		LA_Down,
		LA_Any,
		RA_Left,
		RA_Right,
		RA_Up,
		RA_Down,
		RA_Any,
		LeftTrigger,
		RightTrigger,
	}

	public enum MouseButton
	{
		Left,
		Right,
		Middle,
		X1,
		X2,
		WheelUp,
		WheelDown,
		PosLeft,
		PosRight,
		PosUp,
		PosDown
	}

	public struct InputState {
		public GamePadState gamepadState;
		public KeyboardState keyboardState;
		public MouseState mouseState;
	}

	public struct InputBinding {
		public InputSource source;
		public int button;

		public InputBinding(GamepadAxis axis)
		{
			source = InputSource.GamepadAxis;
			button = (int)axis;
		}


		public InputBinding(MouseButton b)
		{
			source = InputSource.Mouse;
			button = (int)b;
		}

		public InputBinding(Buttons b)
		{
			source = InputSource.GamepadButton;
			button = (int)b;
		}

		public InputBinding(Keys k)
		{
			source = InputSource.Keyboard;
			button = (int)k;
		}

	}

	public enum InputMode {
		Gamepad,
		KBM
	}

	public abstract class InputManager<T, S> where T : struct, IConvertible {

		public Dictionary<T, InputBinding> bindingsGamepad;
		public Dictionary<T, InputBinding> bindingsKBM;

		InputState[] _lastInput;
		private int _maxLocalPlayers;

		public InputManager(int maxLocalPlayers)
		{
			_maxLocalPlayers = maxLocalPlayers;
			_lastInput = new InputState[_maxLocalPlayers];

			ResetDefaultBindings();
		}

		private Dictionary<T, InputBinding> GetInputBindings(InputMode mode)
		{
			if (mode == InputMode.Gamepad) {
				return bindingsGamepad;
			}else {
				return bindingsKBM;
			}

		}

		virtual public void ResetDefaultBindings()
		{
			bindingsGamepad = new Dictionary<T, InputBinding>();
			bindingsKBM = new Dictionary<T, InputBinding>();
		}

		public void Set(T action, GamepadAxis b)
		{
			bindingsGamepad[action] = new InputBinding(b);
		}
		public void Set(T action, Buttons b)
		{
			bindingsGamepad[action] = new InputBinding(b);
		}
		public void Set(T action, Keys b)
		{
			bindingsKBM[action] = new InputBinding(b);
		}
		public void Set(T action, MouseButton b)
		{
			bindingsKBM[action] = new InputBinding(b);
		}

		public bool IsPressed(InputMode mode, T t, ref InputState inputState)
		{
			var bm = GetInputBindings(mode);
			InputBinding b;
			if (!bm.TryGetValue(t, out b)) {
				return false;
			}
			return IsPressed(mode, b, ref inputState);
		}
		public bool IsPressed(InputMode mode, InputBinding binding, ref InputState inputState)
		{
			var bm = GetInputBindings(mode);
			switch (binding.source) {
				case InputSource.GamepadAxis:
					if (!inputState.gamepadState.IsConnected) {
						return false;
					}
					switch ((GamepadAxis)binding.button) {
						case GamepadAxis.LA_Up:
							return inputState.gamepadState.ThumbSticks.Left.Y > 0;
						case GamepadAxis.LA_Down:
							return inputState.gamepadState.ThumbSticks.Left.Y < 0;
						case GamepadAxis.LA_Left:
							return inputState.gamepadState.ThumbSticks.Left.X < 0;
						case GamepadAxis.LA_Right:
							return inputState.gamepadState.ThumbSticks.Left.X > 0;
						case GamepadAxis.LA_Any:
							return inputState.gamepadState.ThumbSticks.Left.X != 0 || inputState.gamepadState.ThumbSticks.Left.Y != 0;
						case GamepadAxis.RA_Up:
							return inputState.gamepadState.ThumbSticks.Right.Y < 0;
						case GamepadAxis.RA_Down:
							return inputState.gamepadState.ThumbSticks.Right.Y > 0;
						case GamepadAxis.RA_Left:
							return inputState.gamepadState.ThumbSticks.Right.X < 0;
						case GamepadAxis.RA_Right:
							return inputState.gamepadState.ThumbSticks.Right.X > 0;
						case GamepadAxis.RA_Any:
							return inputState.gamepadState.ThumbSticks.Right.X != 0 || inputState.gamepadState.ThumbSticks.Right.Y != 0;
						case GamepadAxis.LeftTrigger:
							return inputState.gamepadState.Triggers.Left > 0;
						case GamepadAxis.RightTrigger:
							return inputState.gamepadState.Triggers.Right > 0;
					}
					return false;
				case InputSource.GamepadButton:
					if (!inputState.gamepadState.IsConnected) {
						return false;
					}
					return inputState.gamepadState.IsButtonDown((Buttons)binding.button);
				case InputSource.Keyboard:
					return inputState.keyboardState.IsKeyDown((Keys)binding.button);
				case InputSource.Mouse:
					return IsMouseButtonPressed(ref inputState.mouseState, (MouseButton)binding.button);
			}
			return false;
		}

		public bool WasJustPressed(InputMode mode, T t, ref InputState inputState, ref InputState lastState)
		{
			var bm = GetInputBindings(mode);
			InputBinding b;
			if (!bm.TryGetValue(t, out b)) {
				return false;
			}

			if (b.source == InputSource.Mouse) {
				if (b.button == (int)MouseButton.WheelUp) {
					return inputState.mouseState.ScrollWheelValue < lastState.mouseState.ScrollWheelValue;
				} else if (b.button == (int)MouseButton.WheelDown) {
					return inputState.mouseState.ScrollWheelValue > lastState.mouseState.ScrollWheelValue;
				}
			}
			return IsPressed(mode, b, ref inputState) && !IsPressed(mode, b, ref lastState);
		}

		public bool WasJustReleased(InputMode mode, T t, ref InputState inputState, ref InputState lastState)
		{
			var bm = GetInputBindings(mode);
			InputBinding b;
			if (!bm.TryGetValue(t, out b)) {
				return false;
			}
			return !IsPressed(mode, b, ref inputState) && IsPressed(mode, b, ref lastState);
		}

		public float GetAnalogValue(InputMode mode, T t, ref InputState inputState)
		{
			var bm = GetInputBindings(mode);
			InputBinding b;
			if (!bm.TryGetValue(t, out b)) {
				return 0f;
			}
			switch (b.source) {
				case InputSource.GamepadAxis:
					if (!inputState.gamepadState.IsConnected) {
						return 0f;
					}

					switch ((GamepadAxis)b.button) {
						case GamepadAxis.LA_Up:
							return Math.Max(0,inputState.gamepadState.ThumbSticks.Left.Y);
						case GamepadAxis.LA_Down:
							return -Math.Min(0,inputState.gamepadState.ThumbSticks.Left.Y);
						case GamepadAxis.LA_Left:
							return -Math.Min(0,inputState.gamepadState.ThumbSticks.Left.X);
						case GamepadAxis.LA_Right:
							return Math.Max(0,inputState.gamepadState.ThumbSticks.Left.X);
						case GamepadAxis.RA_Up:
							return -Math.Min(0, inputState.gamepadState.ThumbSticks.Right.Y);
						case GamepadAxis.RA_Down:
							return Math.Max(0, inputState.gamepadState.ThumbSticks.Right.Y);
						case GamepadAxis.RA_Left:
							return -Math.Min(0, inputState.gamepadState.ThumbSticks.Right.X);
						case GamepadAxis.RA_Right:
							return Math.Max(0, inputState.gamepadState.ThumbSticks.Right.X);
						case GamepadAxis.LeftTrigger:
							return inputState.gamepadState.Triggers.Left;
						case GamepadAxis.RightTrigger:
							return inputState.gamepadState.Triggers.Right;
					}
					return 0f;

				case InputSource.GamepadButton:
					if (!inputState.gamepadState.IsConnected) {
						return 0f;
					}
					return inputState.gamepadState.IsButtonDown((Buttons)b.button) ? 1f : 0f;
				case InputSource.Keyboard:
					return inputState.keyboardState.IsKeyDown((Keys)b.button) ? 1f : 0f;
				case InputSource.Mouse:
					return IsMouseButtonPressed(ref inputState.mouseState, (MouseButton)b.button) ? 1f : 0f;
			}
			return 0f;
		}

		private bool IsMouseButtonPressed(ref MouseState state, MouseButton button)
		{
			switch (button) {
				case MouseButton.Left:
					return state.LeftButton == ButtonState.Pressed;
				case MouseButton.Right:
					return state.RightButton == ButtonState.Pressed;
				case MouseButton.Middle:
					return state.MiddleButton == ButtonState.Pressed;
				case MouseButton.X1:
					return state.XButton1 == ButtonState.Pressed;
				case MouseButton.X2:
					return state.XButton2 == ButtonState.Pressed;
			}
			return false;
		}

		private float GetAxis(ref GamePadState state, GamepadAxis axis)
		{
			switch (axis) {
				case GamepadAxis.LA_Up:
				case GamepadAxis.LA_Down:
					return state.ThumbSticks.Left.Y;
				case GamepadAxis.LA_Left:
				case GamepadAxis.LA_Right:
					return state.ThumbSticks.Left.X;
				case GamepadAxis.RA_Up:
				case GamepadAxis.RA_Down:
					return state.ThumbSticks.Right.Y;
				case GamepadAxis.RA_Left:
				case GamepadAxis.RA_Right:
					return state.ThumbSticks.Right.X;
				case GamepadAxis.LeftTrigger:
					return state.Triggers.Left;
				case GamepadAxis.RightTrigger:
					return state.Triggers.Right;
			}
			return 0f;
		}

		public S Update(GameTime gameTime, int playerIndex)
		{
			InputState state = new InputState();
			state.gamepadState = GamePad.GetState((PlayerIndex)playerIndex);
			if (playerIndex == 0) {
				state.mouseState = Mouse.GetState();
				state.keyboardState = Keyboard.GetState();
			}

			var s = ProcessInput(playerIndex, gameTime, ref state, ref _lastInput[playerIndex]);

			_lastInput[playerIndex] = state;
			return s;
		}

		protected abstract S ProcessInput(int playerIndex, GameTime gameTime, ref InputState state, ref InputState lastState);
	}
}
