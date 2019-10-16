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
		public WorldGUI Gui;

		public abstract void OnSelect();
		public abstract void OnDeselect();
		public abstract void DrawWorld(SpriteBatch spriteBatch, World.State state);
		public abstract void DrawTooltip(SpriteBatch spriteBatch, World.State state);
		public abstract void Update(GameTime gameTime, Point p);
		public abstract void OnMouseDown(Point p);
		public abstract void OnMouseUp(Point p);
		public abstract void OnMouseWheel(float delta);

		public static void DrawInfoTooltip(SpriteBatch spriteBatch, WorldGUI gui, World.State state)
		{
			int index = gui.World.GetIndex(gui.TileInfoPoint.X, gui.TileInfoPoint.Y);
			int textY = 300;

			spriteBatch.DrawString(gui.Font, "Index: " + index, new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Elevation: " + (int)(state.Elevation[index]), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Temperature: " + (int)(state.Temperature[index] - gui.World.Data.FreezingTemperature), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Pressure: " + (state.Pressure[index] / gui.World.Data.StaticPressure).ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Humidity: " + state.Humidity[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "CloudCover: " + state.CloudCover[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Rainfall: " + (state.Rainfall[index] * gui.World.Data.TicksPerYear).ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "Evaporation: " + (state.Evaporation[index] * gui.World.Data.TicksPerYear).ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "WaterTableDepth: " + (int)state.WaterTableDepth[index], new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "GroundWater: " + state.GroundWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "SurfaceWater: " + state.SurfaceWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
			spriteBatch.DrawString(gui.Font, "SurfaceIce: " + state.SurfaceIce[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
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
						" +" + gui.World.Data.speciesGrowthRate.ToString("0.000") +
						" Starvation: " + gui.World.GetStarvation(gui.World.GetPopulationDensity(population), state.Canopy[index]).ToString("0.000") +
						" Dehydration: " + gui.World.GetDehydration(gui.World.GetPopulationDensity(population), gui.World.GetFreshWaterAvailability(state.SurfaceWater[index], gui.World.GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], state.SoilFertility[index]))).ToString("0.000") +
						" TemperatureDeath: " + gui.World.GetTemperatureDeath(ref state, state.Temperature[index], s).ToString("0.000"),
						new Vector2(25, textY), Color.White);
				}
			}

		}

	}


}
