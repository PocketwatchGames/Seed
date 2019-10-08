using System;
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

		public World.Layers ShowLayers = World.Layers.ElevationSubtle | World.Layers.Water | World.Layers.SoilFertility | World.Layers.Vegetation | World.Layers.Animals;
		public List<Tuple<World.Layers, Keys>> LayerDisplayKeys = new List<Tuple<World.Layers, Keys>>()
		{
			new Tuple<World.Layers, Keys>(World.Layers.Water, Keys.F1),
			new Tuple<World.Layers, Keys>(World.Layers.Elevation, Keys.F2),
			new Tuple<World.Layers, Keys>(World.Layers.GroundWater, Keys.F3),
			new Tuple<World.Layers, Keys>(World.Layers.CloudCoverage, Keys.F4),
			new Tuple<World.Layers, Keys>(World.Layers.Temperature, Keys.F5),
			new Tuple<World.Layers, Keys>(World.Layers.Wind, Keys.F6),
		};


		public SpriteFont Font;
		public Texture2D whiteTex;

		public int ActiveTool = -1;

		public List<Tool> Tools = new List<Tool>();
		public Point TileInfoPoint;

		public World World;
		public WorldGUI(World world)
		{
			World = world;
			world.TimeScale = _timeScales[_timeScaleIndex];

			Tools.Add(new ToolSelect() { Name = "Info", HotKey = Keys.D1 });
			Tools.Add(new ToolElevation() { Name = "Elevation Up", HotKey = Keys.D2, DeltaPerSecond = 1.0f });
			Tools.Add(new ToolElevation() { Name = "Elevation Down", HotKey = Keys.D3, DeltaPerSecond = -1.0f });
			SelectTool(0);
		}

		public void LoadContent(GraphicsDevice graphics, ContentManager content)
		{
			Font = content.Load<SpriteFont>("fonts/infofont");
			whiteTex = new Texture2D(graphics, 1, 1);
			Color[] c = new Color[] { Color.White };
			whiteTex.SetData(c);

		}

		bool WasKeyJustPressed(Keys k, ref KeyboardState state, ref KeyboardState lastState)
		{
			return state.IsKeyDown(k) && !lastState.IsKeyDown(k);
		}

		public void Update(GameTime gameTime)
		{
			var keyboardState = Keyboard.GetState();
			var mouseState = Mouse.GetState();
			TileInfoPoint = new Point(MathHelper.Clamp(mouseState.X / World.tileSize, 0, World.Size - 1), MathHelper.Clamp(mouseState.Y / World.tileSize, 0, World.Size - 1));

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
				curTool.Update(gameTime, TileInfoPoint, this);
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

		public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
		{
			World.Draw(gameTime, spriteBatch, ShowLayers, whiteTex);

			World.State state = World.States[World.NextRenderStateIndex];

			var curTool = GetTool(ActiveTool);
			if (curTool != null)
			{
				curTool.DrawWorld(spriteBatch, this, state);
			}

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
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
				curTool.DrawTooltip(spriteBatch, this, state);
			}

			spriteBatch.End();
		}
	}
}
