using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Seed
{
	public partial class World
	{
		[Flags]
		public enum Layers
		{
			Elevation = 1 << 0,
			ElevationSubtle = 1 << 1,
			Gradient = 1 << 2,
			Water = 1 << 3,
			SoilFertility = 1 << 4,
			WaterTableDepth = 1 << 5,
			GroundWater = 1 << 6,
			Vegetation = 1 << 7,
			Animals = 1 << 8,
			CloudCoverage = 1 << 9,
			CloudHeight = 1 << 11,
			Temperature = 1 << 12,
			TemperatureSubtle = 1 << 13,
			Wind = 1 << 14,

//			Rainfall,
		}

		Texture2D whiteTex;
		SpriteFont font;

		public int tileSize = 10;
		public bool TileInfoActive = true;
		public Point TileInfoPoint;
		public Layers ShowLayers = Layers.ElevationSubtle | Layers.Water | Layers.SoilFertility | Layers.Vegetation | Layers.Animals;

		public List<Tuple<Layers, Keys>> LayerDisplayKeys = new List<Tuple<Layers, Keys>>()
		{
			new Tuple<Layers, Keys>(Layers.Water, Keys.F1),
			new Tuple<Layers, Keys>(Layers.Elevation, Keys.F2),
			new Tuple<Layers, Keys>(Layers.GroundWater, Keys.F3),
			new Tuple<Layers, Keys>(Layers.CloudCoverage, Keys.F4),
			new Tuple<Layers, Keys>(Layers.Temperature, Keys.F5),
			new Tuple<Layers, Keys>(Layers.Wind, Keys.F6),
		};

		public void LoadContent(GraphicsDevice graphics, ContentManager content)
		{
			whiteTex = new Texture2D(graphics, 1, 1);
			Color[] c = new Color[] { Color.White };
			whiteTex.SetData(c);

			font = content.Load<SpriteFont>("fonts/infofont");
		}

		public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
		{
			lock (DrawLock) {
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

				ref var state = ref States[NextRenderStateIndex];

				for (int x = 0; x < Size; x++)
				{
					for (int y = 0; y < Size; y++)
					{
						int index = GetIndex(x, y);
						Rectangle rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

						Color color = Color.White;
						float elevation = state.Elevation[index];
						float normalizedElevation = (elevation - MinElevation) / (MaxElevation - MinElevation);
						bool drawOcean = elevation <= state.SeaLevel && ShowLayers.HasFlag(Layers.Water);

						// Base color

						if (drawOcean)
						{
							if (ShowLayers.HasFlag(Layers.ElevationSubtle))
							{
								color = Color.Lerp(Color.DarkBlue, Color.Blue, (state.SeaLevel - elevation) / MinElevation);
							}
							else
							{
								color = Color.Blue;
							}
						}
						else
						{
							if (ShowLayers.HasFlag(Layers.SoilFertility))
							{
								color = Color.Lerp(Color.Gray, Color.Brown, state.SoilFertility[index]);
							}

							if (ShowLayers.HasFlag(Layers.ElevationSubtle))
							{
								if (normalizedElevation < 0)
								{
									color = Color.Lerp(Color.Black, color, (normalizedElevation - 0.5f) * 2);
								}
								else
								{
									color = Color.Lerp(color, Color.White, (normalizedElevation - 0.5f) * 2);
								}
							}

							if (ShowLayers.HasFlag(Layers.Vegetation))
							{
								color = Color.Lerp(color, Color.Green, state.Canopy[index]);
							}
						}

						if (ShowLayers.HasFlag(Layers.TemperatureSubtle))
						{
							float temperature = state.Temperature[index];
							if (temperature > 0)
							{
								color = Color.Lerp(color, Color.Red, Math.Min(1.0f, temperature / (2 * MaxTemperature)));
							}
							else
							{
								color = Color.Lerp(Color.LightBlue, color, Math.Max(0, 1.0f - temperature / (2 * MinTemperature)));
							}
						}


						spriteBatch.Draw(whiteTex, rect, color);

						if (ShowLayers.HasFlag(Layers.Water))
						{
							float sw = state.SurfaceWater[index];
							if (sw > 0)
							{
								int width = (int)(Math.Min(1.0f, sw) * (tileSize - 2));
								Rectangle surfaceWaterRect = new Rectangle(x * tileSize + 1, y * tileSize + 1, width, width);
								spriteBatch.Draw(whiteTex, surfaceWaterRect, Color.Lerp(Color.Blue, Color.Teal, (elevation - state.SeaLevel) / (MaxElevation - state.SeaLevel)) * 0.75f);
							}
						}

						if (ShowLayers.HasFlag(Layers.Temperature))
						{
							color = Color.Lerp(Color.LightBlue, Color.Red, Math.Min(1.0f, (state.Temperature[index] - MinTemperature) / (MaxTemperature - MinTemperature)));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (ShowLayers.HasFlag(Layers.GroundWater))
						{
							color = Color.Lerp(Color.DarkBlue, Color.Gray, Math.Min(1.0f, state.GroundWater[index] / MaxGroundWater));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (ShowLayers.HasFlag(Layers.WaterTableDepth))
						{
							color = Color.Lerp(Color.Black, Color.White, (state.WaterTableDepth[index] - MinWaterTableDepth) / (MaxWaterTableDepth - MinWaterTableDepth));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (ShowLayers.HasFlag(Layers.Elevation))
						{
							if (elevation < state.SeaLevel)
							{
								color = Color.Blue * normalizedElevation;
							}
							else
							{
								color = Color.White * normalizedElevation;
							}
							color.A = 255;
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (ShowLayers.HasFlag(Layers.Wind))
						{
							var wind = state.Wind[index];
							float maxWindSpeed = 0.003f;
							color.R = (byte)(255 * (float)MathHelper.Clamp((wind.X + maxWindSpeed) / (2 * maxWindSpeed), 0.0f, 1.0f));
							color.G = (byte)(255 * (float)MathHelper.Clamp((wind.Y + maxWindSpeed) / (2 * maxWindSpeed), 0.0f, 1.0f));
							color.B = 0;
							spriteBatch.Draw(whiteTex, rect, color);
						}
					}
				}

				for (int x = 0; x < Size; x++)
				{
					for (int y = 0; y < Size; y++)
					{

						if (ShowLayers.HasFlag(Layers.Animals))
						{
							for (int s = 0; s < MaxSpecies; s++)
							{
								int sIndex = GetSpeciesIndex(x, y, s);
								float pop = state.Population[sIndex];
								if (pop > 0)
								{
									Rectangle rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
									int p = MathHelper.Clamp((int)Math.Ceiling(tileSize * (float)pop / speciesMaxPopulation), 0, tileSize);
									for (int i = 0; i < p; i++)
									{
										int screenX = rect.X + i * 2;
										int size = i == p - 1 ? 1 : 3;
										spriteBatch.Draw(whiteTex, new Rectangle(screenX, rect.Y + (int)(tileSize * (Math.Cos(screenX + rect.Y + (float)gameTime.TotalGameTime.Ticks/TimeSpan.TicksPerSecond) / 2 + 0.5f)), size, size), state.Species[s].Color);
									}
								}
							}
						}
					}
				}

				if (ShowLayers.HasFlag(Layers.CloudHeight) || ShowLayers.HasFlag(Layers.CloudCoverage)) {
					for (int x = 0; x < Size; x++)
					{
						for (int y = 0; y < Size; y++)
						{
							int index = GetIndex(x, y);
							if (ShowLayers.HasFlag(Layers.CloudHeight))
							{
								//					spriteBatch.Draw(whiteTex, rect, Color.Lerp(Color.Black, Color.White, CloudElevation[index] / MaxCloudElevation) * CloudCover[index]);
							}
							else if (ShowLayers.HasFlag(Layers.CloudCoverage))
							{
								float minCloudsToDraw = 0.05f;
								float maxCloudsToDraw = 3.0f;
								float maxCloudAlpha = 0.5f;
								float cloudCover = (float)MathHelper.Clamp(state.CloudCover[index] - minCloudsToDraw, 0.0f, maxCloudsToDraw);
								if (cloudCover > 0)
								{
									Rectangle rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
									int width = (int)(cloudCover * tileSize);
									float normalizedCloudCover = cloudCover / maxCloudsToDraw;
									spriteBatch.Draw(whiteTex, rect, Color.Lerp(Color.White, Color.DarkGray, normalizedCloudCover * normalizedCloudCover) * normalizedCloudCover * maxCloudAlpha);
								}
							}

						}
					}
				}

				spriteBatch.Draw(whiteTex, new Rectangle(0, 0, 150, 15 * LayerDisplayKeys.Count + 20), null, Color.Black * 0.5f);
				int textY = 5;

				spriteBatch.DrawString(font, "[ x" + ((int)TimeScale) + " ]", new Vector2(5, textY), Color.White);
				textY += 20;

				foreach (var k in LayerDisplayKeys)
				{
					spriteBatch.DrawString(font, "[" + (ShowLayers.HasFlag(k.Item1) ? "X" : " ") + "] - " + k.Item2 + " - " + k.Item1.ToString(), new Vector2(5, textY), Color.White);
					textY += 15;
				}

				textY += 15;
				for (int s = 0; s < MaxSpecies; s++)
				{
					if (state.SpeciesStats[s].Population > 0)
					{
						var species = state.Species[s];
						spriteBatch.Draw(whiteTex, new Rectangle(5, textY, 10, 10), null, species.Color);
						spriteBatch.DrawString(font, species.Name + " [" + species.Food.ToString().Substring(0, 1) + "] " + ((float)state.SpeciesStats[s].Population / 1000000).ToString("0.00"), new Vector2(20, textY), Color.White);
						textY += 15;
					}
				}
				textY += 15;

				if (TileInfoActive)
				{
					int index = GetIndex(TileInfoPoint.X, TileInfoPoint.Y);

					//public float[] Temperature;
					//public float[] CloudCover;
					//public float[] CloudElevation;
					//public float[] WaterTableDepth;
					//public float[] GroundWater;
					//public float[] SurfaceWater;
					//public float[] WaterSalinity;
					//public float[] SurfaceIce;
					//public float[] SubmergedIce;
					//public float[] SoilFertility;
					//public float[] Canopy;
					//public float[] Population;
					//public Vector2[] Wind;
					//public Vector2[] Gradient;

					spriteBatch.DrawString(font, "Elevation: " + (int)(state.Elevation[index] * 1000), new Vector2(5, textY += 15), Color.White);
					spriteBatch.DrawString(font, "Temperature: " + (int)(state.Temperature[index]), new Vector2(5, textY += 15), Color.White);
					spriteBatch.DrawString(font, "CloudCover: " + (int)(state.CloudCover[index] * 100), new Vector2(5, textY += 15), Color.White);
					spriteBatch.DrawString(font, "WaterTableDepth: " + state.WaterTableDepth[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
					spriteBatch.DrawString(font, "GroundWater: " + state.GroundWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
					spriteBatch.DrawString(font, "SurfaceWater: " + state.SurfaceWater[index].ToString("0.00"), new Vector2(5, textY += 15), Color.White);
					spriteBatch.DrawString(font, "SoilFertility: " + (int)(state.SoilFertility[index] * 100), new Vector2(5, textY += 15), Color.White);
					spriteBatch.DrawString(font, "Canopy: " + (int)(state.Canopy[index] * 100), new Vector2(5, textY += 15), Color.White);
					//	spriteBatch.DrawString(font, "Wind: " + Wind[index], new Vector2(5, textY += 15), Color.White);
					for (int s = 0; s < MaxSpecies; s++)
					{
						int speciesIndex = GetSpeciesIndex(TileInfoPoint.X, TileInfoPoint.Y, s);
						if (state.Population[speciesIndex] > 0) {
							spriteBatch.Draw(whiteTex, new Rectangle(5, textY += 15, 10, 10), null, state.Species[s].Color);
							float population = state.Population[speciesIndex];
							spriteBatch.DrawString(font,
								state.Species[s].Name + ": " + ((int)population) +
								" +" + speciesGrowthRate.ToString("0.000") +
								" Starvation: " + GetStarvation(GetPopulationDensity(population), state.Canopy[index]).ToString("0.000") +
								" Dehydration: " + GetDehydration(GetPopulationDensity(population), GetFreshWaterAvailability(state.SurfaceWater[index], GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index]))).ToString("0.000") +
								" TemperatureDeath: " + GetTemperatureDeath(ref state, state.Temperature[index], s).ToString("0.000"),
								new Vector2(25, textY), Color.White);
						}
					}
				}

				spriteBatch.End();
			}
		}
	}
}
