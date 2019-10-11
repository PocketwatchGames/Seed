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
			Pressure = 1 << 15,
			Humidity = 1 << 16,
			Rainfall = 1 << 17,
			Probes = 1 << 18,

		}

		public int tileRenderSize = 10;
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
						Rectangle rect = new Rectangle(x * tileRenderSize, y * tileRenderSize, tileRenderSize, tileRenderSize);

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
								color = Color.Lerp(color, Color.Green, (float)Math.Sqrt(MathHelper.Clamp(state.Canopy[index], 0.01f, 1.0f)));
							}
						}

						if (showLayers.HasFlag(Layers.TemperatureSubtle))
						{
							float temperature = state.Temperature[index];
							if (temperature > FreezingTemperature)
							{
								color = Color.Lerp(color, Color.Red, Math.Min(1.0f, (temperature - FreezingTemperature) / (10 * (MaxTemperature - FreezingTemperature))));
							}
							else
							{
								color = Color.Lerp(Color.LightBlue, color, Math.Max(0, 1.0f - (FreezingTemperature - temperature) / (10 * (FreezingTemperature - MinTemperature))));
							}
						}


						spriteBatch.Draw(whiteTex, rect, color);

						if (showLayers.HasFlag(Layers.Water))
						{
							float sw = state.SurfaceWater[index];
							if (sw > 0)
							{
								int width = (int)(Math.Min(1.0f, sw) * (tileRenderSize - 2));
								Rectangle surfaceWaterRect = new Rectangle(x * tileRenderSize + 1, y * tileRenderSize + 1, width, width);
								spriteBatch.Draw(whiteTex, surfaceWaterRect, Color.Lerp(Color.Blue, Color.Teal, (elevation - state.SeaLevel) / (MaxElevation - state.SeaLevel)) * 0.75f);
							}
						}

						if (showLayers.HasFlag(Layers.Temperature))
						{
							color = Color.Lerp(Color.LightBlue, Color.Red, Math.Min(1.0f, (state.Temperature[index] - MinTemperature) / (MaxTemperature - MinTemperature)));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (showLayers.HasFlag(Layers.Pressure))
						{
							float minPressure = StaticPressure - 40000;
							float maxPressure = StaticPressure + 10000;
							color = Color.Lerp(Color.Black, Color.White, MathHelper.Clamp((state.Pressure[index] - minPressure) / (maxPressure - minPressure), 0, 1));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (showLayers.HasFlag(Layers.Humidity))
						{
							float maxHumidity = 5;
							color = Color.Lerp(Color.Black, Color.Blue, Math.Min(1.0f, state.Humidity[index] / maxHumidity));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (showLayers.HasFlag(Layers.Rainfall))
						{
							float maxRainfall = 5.0f / TicksPerYear;
							color = Color.Lerp(Color.Black, Color.Blue, Math.Min(1.0f, state.Rainfall[index] / maxRainfall));
							spriteBatch.Draw(whiteTex, rect, color);
						}
						else if (showLayers.HasFlag(Layers.GroundWater))
						{
							color = Color.Lerp(Color.DarkBlue, Color.Gray, Math.Min(1.0f, state.GroundWater[index] / (MaxWaterTableDepth * MaxSoilPorousness)));
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
						if (showLayers.HasFlag(Layers.Wind))
						{
							//							var wind = state.Wind[index];
							float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
							var wind = GetWindAtElevation(state, state.CloudElevation[index], elevationOrSeaLevel, index, GetLatitude(y), state.Normal[index]);
							//var wind = GetWindAtElevation(state, elevationOrSeaLevel, elevationOrSeaLevel, index, GetLatitude(y), state.Normal[index]);
							float maxWindSpeed = 0.03f;
							Color windColor;
							if (wind.Z < 0)
							{
								windColor = Color.Lerp(Color.White, Color.Blue, -wind.Z / maxWindSpeed);
							} else
							{
								windColor = Color.Lerp(Color.White, Color.Red, wind.Z / maxWindSpeed);
							}
							spriteBatch.Draw(whiteTex, new Rectangle(rect.X + tileRenderSize / 2 - 1, rect.Y + tileRenderSize / 2 - 1, 3, 3), null, Color.White * 0.5f);
							spriteBatch.Draw(whiteTex, new Rectangle(rect.X + tileRenderSize / 2, rect.Y + tileRenderSize / 2, (int)(tileRenderSize * wind.Length() / maxWindSpeed), 1), null, windColor, (float)Math.Atan2(wind.Y, wind.X), new Vector2(0, 0.5f), SpriteEffects.None, 0);
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
									Rectangle rect = new Rectangle(x * tileRenderSize, y * tileRenderSize, tileRenderSize, tileRenderSize);
									int p = MathHelper.Clamp((int)Math.Ceiling(tileRenderSize * (float)pop / speciesMaxPopulation), 0, tileRenderSize);
									for (int i = 0; i < p; i++)
									{
										int screenX = rect.X + i * 2;
										int size = i == p - 1 ? 1 : 3;
										spriteBatch.Draw(whiteTex, new Rectangle(screenX, rect.Y + (int)(tileRenderSize * (Math.Cos(screenX + rect.Y + (float)gameTime.TotalGameTime.Ticks/TimeSpan.TicksPerSecond * Math.Sqrt(TimeScale)) / 2 + 0.5f)), size, size), state.Species[s].Color);
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
								float minCloudsToDraw = 0.01f;
								float maxCloudsToDraw = 1.0f;
								float cloudCover = (float)MathHelper.Clamp(state.CloudCover[index] - minCloudsToDraw, 0.0f, maxCloudsToDraw);
								if (cloudCover > 0)
								{
									float normalizedCloudCover = cloudCover / maxCloudsToDraw;
									int width = (int)(normalizedCloudCover * tileRenderSize);
									Rectangle rect = new Rectangle(x * tileRenderSize, y * tileRenderSize, width, width);
									spriteBatch.Draw(whiteTex, rect, Color.Lerp(Color.White, Color.Black, normalizedCloudCover));
								}
							}

						}
					}
				}

				if (showLayers.HasFlag(Layers.Probes))
				{
					for (int i=0;i<ProbeCount;i++)
					{
						var probe = Probes[i];
						spriteBatch.Draw(
							whiteTex, 
							new Vector2(probe.Position.X * tileRenderSize + tileRenderSize / 2, probe.Position.Y * tileRenderSize + tileRenderSize / 2), 
							null, 
							Color.Purple, 
							(float)gameTime.TotalGameTime.Ticks / TimeSpan.TicksPerSecond,
							new Vector2(0.5f, 0.5f), 
							5, 
							SpriteEffects.None, 
							0);
					}
				}

				spriteBatch.End();
			}
		}
	}
}
