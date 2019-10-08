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


		public int tileSize = 10;


		public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Layers showLayers, Texture2D whiteTex)
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
						bool drawOcean = elevation <= state.SeaLevel && showLayers.HasFlag(Layers.Water);

						// Base color

						if (drawOcean)
						{
							if (showLayers.HasFlag(Layers.ElevationSubtle))
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
							if (showLayers.HasFlag(Layers.SoilFertility))
							{
								color = Color.Lerp(Color.Gray, Color.Brown, state.SoilFertility[index]);
							}

							if (showLayers.HasFlag(Layers.ElevationSubtle))
							{
								if (normalizedElevation < 0)
								{
									color = Color.Lerp(Color.Black, color, (normalizedElevation - 0.5f) * 5);
								}
								else
								{
									color = Color.Lerp(color, Color.White, (normalizedElevation - 0.5f) * 5);
								}
							}

							if (showLayers.HasFlag(Layers.Vegetation))
							{
								color = Color.Lerp(color, Color.Green, state.Canopy[index]);
							}
						}

						if (showLayers.HasFlag(Layers.TemperatureSubtle))
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

						if (showLayers.HasFlag(Layers.Water))
						{
							float sw = state.SurfaceWater[index];
							if (sw > 0)
							{
								int width = (int)(Math.Min(1.0f, sw) * (tileSize - 2));
								Rectangle surfaceWaterRect = new Rectangle(x * tileSize + 1, y * tileSize + 1, width, width);
								spriteBatch.Draw(whiteTex, surfaceWaterRect, Color.Lerp(Color.Blue, Color.Teal, (elevation - state.SeaLevel) / (MaxElevation - state.SeaLevel)) * 0.75f);
							}
						}

						if (showLayers.HasFlag(Layers.Temperature))
						{
							color = Color.Lerp(Color.LightBlue, Color.Red, Math.Min(1.0f, (state.Temperature[index] - MinTemperature) / (MaxTemperature - MinTemperature)));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (showLayers.HasFlag(Layers.GroundWater))
						{
							color = Color.Lerp(Color.DarkBlue, Color.Gray, Math.Min(1.0f, state.GroundWater[index] / MaxGroundWater));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (showLayers.HasFlag(Layers.WaterTableDepth))
						{
							color = Color.Lerp(Color.Black, Color.White, (state.WaterTableDepth[index] - MinWaterTableDepth) / (MaxWaterTableDepth - MinWaterTableDepth));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (showLayers.HasFlag(Layers.Elevation))
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
						else if (showLayers.HasFlag(Layers.Wind))
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

				if (showLayers.HasFlag(Layers.Animals))
				{
					for (int x = 0; x < Size; x++)
					{
						for (int y = 0; y < Size; y++)
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
										spriteBatch.Draw(whiteTex, new Rectangle(screenX, rect.Y + (int)(tileSize * (Math.Cos(screenX + rect.Y + (float)gameTime.TotalGameTime.Ticks/TimeSpan.TicksPerSecond * Math.Sqrt(TimeScale)) / 2 + 0.5f)), size, size), state.Species[s].Color);
									}
								}
							}
						}
					}
				}

				if (showLayers.HasFlag(Layers.CloudHeight) || showLayers.HasFlag(Layers.CloudCoverage)) {
					for (int x = 0; x < Size; x++)
					{
						for (int y = 0; y < Size; y++)
						{
							int index = GetIndex(x, y);
							if (showLayers.HasFlag(Layers.CloudHeight))
							{
								//					spriteBatch.Draw(whiteTex, rect, Color.Lerp(Color.Black, Color.White, CloudElevation[index] / MaxCloudElevation) * CloudCover[index]);
							}
							else if (showLayers.HasFlag(Layers.CloudCoverage))
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


				spriteBatch.End();
			}
		}
	}
}
