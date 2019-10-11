﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Seed
{
	public class WorldGUI
	{
		KeyboardState _lastKeyboardState;
		MouseState _lastMouseState;
		List<float> _timeScales = new List<float> { 0, 5, 20, 100 };
		int _timeScaleIndex = 0;
		Point viewport;

		RenderTarget2D _worldRenderTarget;

		public World.Layers ShowLayers = World.Layers.Probes | World.Layers.ElevationSubtle | World.Layers.TemperatureSubtle | World.Layers.Water | World.Layers.SoilFertility | World.Layers.Vegetation | World.Layers.Animals;
		public List<Tuple<World.Layers, Keys>> LayerDisplayKeys = new List<Tuple<World.Layers, Keys>>()
		{
			new Tuple<World.Layers, Keys>(World.Layers.Water, Keys.F1),
			new Tuple<World.Layers, Keys>(World.Layers.Elevation, Keys.F2),
			new Tuple<World.Layers, Keys>(World.Layers.GroundWater, Keys.F3),
			new Tuple<World.Layers, Keys>(World.Layers.CloudCoverage, Keys.F4),
			new Tuple<World.Layers, Keys>(World.Layers.Temperature, Keys.F5),
			new Tuple<World.Layers, Keys>(World.Layers.Pressure, Keys.F6),
			new Tuple<World.Layers, Keys>(World.Layers.Humidity, Keys.F7),
			new Tuple<World.Layers, Keys>(World.Layers.Rainfall, Keys.F8),
			new Tuple<World.Layers, Keys>(World.Layers.Wind, Keys.F9),
		};


		public SpriteFont Font;
		public Texture2D whiteTex;

		public int ActiveTool = -1;

		public List<Tool> Tools = new List<Tool>();
		public Point TileInfoPoint;

		public World World;

		public Vector2 CameraPos = new Vector2(50,50);
		public float Zoom { get { return 0.5f + 0.5f * (float)Math.Pow(ZoomLevel + 0.5f, 4); } }
		public float ZoomLevel = 0.5f;

		public WorldGUI(World world)
		{
			World = world;
			world.TimeScale = _timeScales[_timeScaleIndex];

			Tools.Add(new ToolSelect() { Gui = this, Name = "Info", HotKey = Keys.D1 });
			Tools.Add(new ToolElevation() { Gui = this, Name = "Elevation Up", HotKey = Keys.D2, DeltaPerSecond = 1000.0f });
			Tools.Add(new ToolElevation() { Gui = this, Name = "Elevation Down", HotKey = Keys.D3, DeltaPerSecond = -1000.0f });
			Tools.Add(new ToolProbe() { Gui = this, Name = "Probe 1", HotKey = Keys.D4, ProbeIndex = 0 });
			Tools.Add(new ToolProbe() { Gui = this, Name = "Probe 2", HotKey = Keys.D5, ProbeIndex = 1 });
			Tools.Add(new ToolProbe() { Gui = this, Name = "Probe 3", HotKey = Keys.D6, ProbeIndex = 2 });
			SelectTool(0);

		}

		public void LoadContent(GraphicsDevice graphics, ContentManager content)
		{
			viewport = new Point(graphics.Viewport.Width, graphics.Viewport.Height);

			Font = content.Load<SpriteFont>("fonts/infofont");
			whiteTex = new Texture2D(graphics, 1, 1);
			Color[] c = new Color[] { Color.White };
			whiteTex.SetData(c);

			_worldRenderTarget = new RenderTarget2D(graphics, 1000, 1000);
		}

		bool WasKeyJustPressed(Keys k, ref KeyboardState state, ref KeyboardState lastState)
		{
			return state.IsKeyDown(k) && !lastState.IsKeyDown(k);
		}
		bool IsKeyPressed(Keys k, ref KeyboardState state)
		{
			return state.IsKeyDown(k);
		}

		Point ScreenToWorld(Point screenPoint)
		{
			return new Point((int)MathHelper.Clamp((screenPoint.X - viewport.X / 2) / (Zoom * World.tileRenderSize) + CameraPos.X, 0, World.Size - 1), (int)MathHelper.Clamp((screenPoint.Y - viewport.Y / 2) / (Zoom * World.tileRenderSize) + CameraPos.Y, 0, World.Size - 1));
		}
		public void Update(GameTime gameTime)
		{
			var keyboardState = Keyboard.GetState();
			var mouseState = Mouse.GetState();
			TileInfoPoint = ScreenToWorld(mouseState.Position);

			foreach (var k in LayerDisplayKeys)
			{
				if (WasKeyJustPressed(k.Item2, ref keyboardState, ref _lastKeyboardState))
				{
					ShowLayers ^= k.Item1;
				}
			}
			for (int i=0;i<Tools.Count;i++)
			{
				if (WasKeyJustPressed(Tools[i].HotKey, ref keyboardState, ref _lastKeyboardState))
				{
					SelectTool(i);
					break;
				}
			}
			if (WasKeyJustPressed(Keys.OemOpenBrackets, ref keyboardState, ref _lastKeyboardState))
			{

				_timeScaleIndex = _timeScaleIndex - 1;
				if (_timeScaleIndex < 0)
				{
					_timeScaleIndex = 0;
					World.TimeTillTick = 0;
				}
				World.TimeScale = _timeScales[_timeScaleIndex];
			}
			if (WasKeyJustPressed(Keys.OemCloseBrackets, ref keyboardState, ref _lastKeyboardState))
			{
				_timeScaleIndex = MathHelper.Min(_timeScales.Count - 1, _timeScaleIndex + 1);
				World.TimeScale = _timeScales[_timeScaleIndex];
			}

			if (WasKeyJustPressed(Keys.Q, ref keyboardState, ref _lastKeyboardState))
			{
				ZoomLevel = Math.Max(0, ZoomLevel - 0.25f);
			}
			if (WasKeyJustPressed(Keys.E, ref keyboardState, ref _lastKeyboardState))
			{
				ZoomLevel = Math.Min(1, ZoomLevel + 0.25f);
			}
			Vector2 cameraMove = Vector2.Zero;
			if (IsKeyPressed(Keys.W, ref keyboardState))
			{
				cameraMove.Y--;
			}
			if (IsKeyPressed(Keys.A, ref keyboardState))
			{
				cameraMove.X--;
			}
			if (IsKeyPressed(Keys.S, ref keyboardState))
			{
				cameraMove.Y++;
			}
			if (IsKeyPressed(Keys.D, ref keyboardState))
			{
				cameraMove.X++;
			}
			float cameraSpeed = 100 / Zoom;
			CameraPos += cameraSpeed * cameraMove * (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;
			CameraPos.X = MathHelper.Clamp(CameraPos.X, 0, World.Size);
			CameraPos.Y = MathHelper.Clamp(CameraPos.Y, 0, World.Size);

			var curTool = GetTool(ActiveTool);
			if (curTool != null)
			{
				if (mouseState.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton != ButtonState.Pressed)
				{
					curTool.OnMouseDown(TileInfoPoint);
				} else if (mouseState.LeftButton != ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Pressed)
				{
					curTool.OnMouseUp(TileInfoPoint);
				}
				float wheelDelta = mouseState.ScrollWheelValue - _lastMouseState.ScrollWheelValue;
				if (wheelDelta != 0)
				{
					curTool.OnMouseWheel(wheelDelta);
				}
				curTool.Update(gameTime, TileInfoPoint);
			}

			_lastKeyboardState = keyboardState;
			_lastMouseState = mouseState;

			World.Update(gameTime);

		}

		Tool GetTool(int index)
		{
			return (ActiveTool >= 0 && ActiveTool < Tools.Count) ? Tools[ActiveTool] : null;
		}

		public void SelectTool(int t)
		{
			if (t != ActiveTool)
			{
				var curTool = GetTool(ActiveTool);
				curTool?.OnDeselect();

				ActiveTool = t;
				curTool = GetTool(ActiveTool);
				curTool?.OnSelect();
			}
		}

		public void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics )
		{
			graphics.SetRenderTarget(_worldRenderTarget);
			World.Draw(gameTime, spriteBatch, ShowLayers, whiteTex);


			World.State state = World.States[World.NextRenderStateIndex];

			var curTool = GetTool(ActiveTool);
			if (curTool != null)
			{
				curTool.DrawWorld(spriteBatch, state);
			}
			graphics.SetRenderTarget(null);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
			Vector2 cameraPos = new Vector2(CameraPos.X * World.tileRenderSize, CameraPos.Y * World.tileRenderSize);

			spriteBatch.Draw(_worldRenderTarget, new Rectangle((int)(viewport.X/2-cameraPos.X*Zoom), (int)(viewport.Y / 2 - cameraPos.Y * Zoom), (int)(Zoom*_worldRenderTarget.Width), (int)(Zoom*_worldRenderTarget.Height)), null, Color.White);

			spriteBatch.Draw(whiteTex, new Rectangle(0, 0, 150, 15 * LayerDisplayKeys.Count + 20), null, Color.Black * 0.5f);

			int textY = 5;

			for (int i=0;i<Tools.Count;i++)
			{
				spriteBatch.DrawString(Font, Tools[i].HotKey + " - [" + ((i == ActiveTool) ? "X" : " ") + "] " + Tools[i].Name, new Vector2(5, textY), Color.White);
				textY += 15;
			}
			textY += 20;

			spriteBatch.DrawString(Font, "[ x" + ((int)World.TimeScale) + " ]", new Vector2(5, textY), Color.White);
			textY += 20;

			foreach (var k in LayerDisplayKeys)
			{
				spriteBatch.DrawString(Font, "[" + (ShowLayers.HasFlag(k.Item1) ? "X" : " ") + "] - " + k.Item2 + " - " + k.Item1.ToString(), new Vector2(5, textY), Color.White);
				textY += 15;
			}

			spriteBatch.DrawString(Font, (int)(World.GetTimeOfYear(state)*12+1) + "/" + World.GetYear(state), new Vector2(5, textY), Color.White);
			textY += 20;


			textY += 15;
			for (int s = 0; s < World.MaxSpecies; s++)
			{
				if (state.SpeciesStats[s].Population > 0)
				{
					var species = state.Species[s];
					spriteBatch.Draw(whiteTex, new Rectangle(5, textY, 10, 10), null, species.Color);
					spriteBatch.DrawString(Font, species.Name + " [" + species.Food.ToString().Substring(0, 1) + "] " + ((float)state.SpeciesStats[s].Population / 1000000).ToString("0.00"), new Vector2(20, textY), Color.White);
					textY += 15;
				}
			}
			textY += 15;

			if (curTool != null)
			{
				curTool.DrawTooltip(spriteBatch, state);
			}

			spriteBatch.End();
		}
	}
}
