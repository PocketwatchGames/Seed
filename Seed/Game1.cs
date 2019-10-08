using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Seed
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		KeyboardState _lastKeyboardState;
		World world;
		List<float> _timeScales = new List<float> { 0, 5, 20, 100 };
		int _timeScaleIndex = 0;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			graphics.PreferredBackBufferWidth = 1000;
			graphics.PreferredBackBufferHeight = 1000;
			Content.RootDirectory = "Content";
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			IsMouseVisible = true;

			// TODO: Add your initialization logic here
			world = new World();
			world.Init(100);
			world.TimeScale = _timeScales[_timeScaleIndex];


			world.Generate();

			base.Initialize();

		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);
			world.LoadContent(GraphicsDevice, Content);

			// TODO: use this.Content to load your game content here
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			var keyboardState = Keyboard.GetState();
			var mouseState = Mouse.GetState();
			foreach (var k in world.LayerDisplayKeys)
			{
				if (WasKeyJustPressed(k.Item2, ref keyboardState, ref _lastKeyboardState))
				{
					world.ShowLayers ^= k.Item1;
				}
			}
			if (WasKeyJustPressed(Keys.OemOpenBrackets, ref keyboardState, ref _lastKeyboardState))
			{

				_timeScaleIndex = _timeScaleIndex-1;
				if (_timeScaleIndex < 0)
				{
					_timeScaleIndex = 0;
					world.TimeTillTick = 0;
				}
				world.TimeScale = _timeScales[_timeScaleIndex];
			}
			if (WasKeyJustPressed(Keys.OemCloseBrackets, ref keyboardState, ref _lastKeyboardState))
			{
				_timeScaleIndex = MathHelper.Min(_timeScales.Count-1, _timeScaleIndex + 1);
				world.TimeScale = _timeScales[_timeScaleIndex];
			}

			world.TileInfoPoint = new Point(MathHelper.Clamp(mouseState.X / world.tileSize, 0, world.Size-1), MathHelper.Clamp(mouseState.Y / world.tileSize, 0, world.Size-1));

			_lastKeyboardState = keyboardState;


			world.Update(gameTime);

			base.Update(gameTime);
		}

		bool WasKeyJustPressed(Keys k, ref KeyboardState state, ref KeyboardState lastState )
		{
			return state.IsKeyDown(k) && !lastState.IsKeyDown(k);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			world.Draw(gameTime, spriteBatch);

			base.Draw(gameTime);
		}
	}
}
