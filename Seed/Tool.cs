using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Seed
{
	public abstract class Tool
	{
		public String Name;
		public Keys HotKey;

		public abstract void OnSelect();
		public abstract void OnDeselect();
		public abstract void DrawWorld(SpriteBatch spriteBatch, WorldGUI gui, World.State state);
		public abstract void DrawTooltip(SpriteBatch spriteBatch, WorldGUI gui, World.State state);
		public abstract void Update(GameTime gameTime, Point p, WorldGUI gui);
		public abstract void OnMouseDown(Point p);
		public abstract void OnMouseUp(Point p);
		public abstract void OnMouseWheel(float delta);
	}



	public class ToolSelect : Tool {
		override public void OnSelect() { }
		override public void OnDeselect() { }
		override public void DrawWorld(SpriteBatch spriteBatch, WorldGUI gui, World.State state) { }
		override public void DrawTooltip(SpriteBatch spriteBatch, WorldGUI gui, World.State state)
		{
			int index = gui.World.GetIndex(gui.TileInfoPoint.X, gui.TileInfoPoint.Y);
			int textY = 300;

			spriteBatch.DrawString(gui.Font, "Elevation: " + (int)(state.Elevation[index] * 1000), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Temperature: " + (int)(state.Temperature[index]), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "CloudCover: " + (int)(state.CloudCover[index] * 100), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "WaterTableDepth: " + state.WaterTableDepth[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "GroundWater: " + state.GroundWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "SurfaceWater: " + state.SurfaceWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "SoilFertility: " + (int)(state.SoilFertility[index] * 100), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Canopy: " + (int)(state.Canopy[index] * 100), new Vector2(5, textY += 15), Color.White);
			//	spriteBatch.DrawString(font, "Wind: " + Wind[index], new Vector2(5, textY += 15), Color.White);
			for (int s = 0; s < World.MaxSpecies; s++)
			{
				int speciesIndex = gui.World.GetSpeciesIndex(gui.TileInfoPoint.X, gui.TileInfoPoint.Y, s);
				if (state.Population[speciesIndex] > 0)
				{
					spriteBatch.Draw(gui.whiteTex, new Rectangle(5, textY += 15, 10, 10), null, state.Species[s].Color);
					float population = state.Population[speciesIndex];
					spriteBatch.DrawString(gui.Font,
						state.Species[s].Name + ": " + ((int)population) +
						" +" + gui.World.speciesGrowthRate.ToString("0.000") +
						" Starvation: " + gui.World.GetStarvation(gui.World.GetPopulationDensity(population), state.Canopy[index]).ToString("0.000") +
						" Dehydration: " + gui.World.GetDehydration(gui.World.GetPopulationDensity(population), gui.World.GetFreshWaterAvailability(state.SurfaceWater[index], gui.World.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index]))).ToString("0.000") +
						" TemperatureDeath: " + gui.World.GetTemperatureDeath(ref state, state.Temperature[index], s).ToString("0.000"),
						new Vector2(25, textY), Color.White);
				}
			}
		}
		override public void Update(GameTime gameTime, Point p, WorldGUI gui) { }
		override public void OnMouseDown(Point p) { }
		override public void OnMouseUp(Point p) { }
		override public void OnMouseWheel(float delta) { }
	}

	public class ToolElevation : Tool
	{
		public float BrushSize = 1;
		public float DeltaPerSecond = 100;
		public bool Active;

		override public void OnSelect() { Active = false; }
		override public void OnDeselect() { Active = false; }
		override public void DrawWorld(SpriteBatch spriteBatch, WorldGUI gui, World.State state)
		{
			var p = gui.TileInfoPoint;
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
			for (int i = (int)-Math.Ceiling(BrushSize); i <= Math.Ceiling(BrushSize); i++)
			{
				for (int j = (int)-Math.Ceiling(BrushSize); j <= Math.Ceiling(BrushSize); j++)
				{
					float dist = (float)Math.Sqrt(i * i + j * j);
					if (dist <= BrushSize)
					{
						float distT = (BrushSize == 0) ? 1.0f : (1.0f - (float)Math.Pow(dist / BrushSize, 2));
						int x = gui.World.WrapX(p.X + i);
						int y = p.Y + j;
						if (y < 0 || y >= gui.World.Size)
						{
							continue;
						}
						Rectangle rect = new Rectangle(x * gui.World.tileSize, y * gui.World.tileSize, gui.World.tileSize, gui.World.tileSize);
						spriteBatch.Draw(gui.whiteTex, rect, Color.White * 0.2f);
					}
				}
			}
			spriteBatch.End();
		}
		override public void DrawTooltip(SpriteBatch spriteBatch, WorldGUI gui, World.State state)
		{
			int index = gui.World.GetIndex(gui.TileInfoPoint.X, gui.TileInfoPoint.Y);
			int textY = 300;
			spriteBatch.DrawString(gui.Font, "Elevation: " + (int)(state.Elevation[index] * 1000), new Vector2(5, textY += 15), Color.White);
		}
		override public void Update(GameTime gameTime, Point p, WorldGUI gui)
		{
			if (Active)
			{
				lock (gui.World.InputLock)
				{
					float dt = (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;
					var state = gui.World.States[gui.World.CurStateIndex];
					for (int i = (int)-Math.Ceiling(BrushSize); i <= Math.Ceiling(BrushSize); i++)
					{
						for (int j = (int)-Math.Ceiling(BrushSize); j <= Math.Ceiling(BrushSize); j++)
						{
							float dist = (float)Math.Sqrt(i * i + j * j);
							if (dist <= BrushSize)
							{
								float distT = (BrushSize == 0) ? 1.0f : (1.0f - (float)Math.Pow(dist / BrushSize, 2));
								int x = gui.World.WrapX(p.X + i);
								int y = p.Y + j;
								if (y < 0 || y >= gui.World.Size)
								{
									continue;
								}
								int index = gui.World.GetIndex(x, y);
								state.Elevation[index] += distT * DeltaPerSecond * dt;
							}
						}
					}

					for (int i = (int)-Math.Ceiling(BrushSize)-1; i < Math.Ceiling(BrushSize)+1; i++)
					{
						for (int j = (int)-Math.Ceiling(BrushSize)-1; j < Math.Ceiling(BrushSize) + 1; j++)
						{
							int x = gui.World.WrapX(p.X + i);
							int y = p.Y + j;
							if (y < 0 || y >= gui.World.Size)
							{
								continue;
							}

							gui.World.UpdateGradient(x, y, state);
						}
					}

				}
			}
		}
		override public void OnMouseDown(Point p)
		{
			Active = true;
		}
		override public void OnMouseUp(Point p)
		{
			Active = false;
		}
		override public void OnMouseWheel(float delta) {
			BrushSize = MathHelper.Clamp(BrushSize + delta / 100, 0, 50);
		}
	}

}
