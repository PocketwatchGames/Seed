using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Gears;

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
			RelativeHumidity = 1 << 16,
			Rainfall = 1 << 17,
			Probes = 1 << 18,
			WaterVapor = 1 << 19,

		}

		struct CVP
		{
			public Color Color;
			public float Value;
			public CVP(Color c, float v) { Color = c; Value = v; }
		};
		Color Lerp(List<CVP> colors, float value)
		{
			for (int i = 0; i < colors.Count - 1; i++)
			{
				if (value < colors[i + 1].Value)
				{
					return Color.Lerp(colors[i].Color, colors[i + 1].Color, (value - colors[i].Value) / (colors[i + 1].Value - colors[i].Value));
				}
			}
			return colors[colors.Count - 1].Color;
		}

		float stateLerpT = 0;
		public int tileRenderSize = 10;
		public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Layers showLayers, Texture2D whiteTex)
		{
			lock (DrawLock)
			{
				LastRenderStateIndex = CurRenderStateIndex;
				CurRenderStateIndex = CurStateIndex;
			}

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

			ref var state = ref States[CurRenderStateIndex];
			ref var lastState = ref States[LastRenderStateIndex];
			stateLerpT = Math.Max(1.0f, (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond * 10);

			for (int x = 0; x < Size; x++)
			{
				for (int y = 0; y < Size; y++)
				{
					int index = GetIndex(x, y);
					Rectangle rect = new Rectangle(x * tileRenderSize, y * tileRenderSize, tileRenderSize, tileRenderSize);

					Color color = Color.White;
					float elevation = state.Elevation[index];
					float ice = state.SurfaceIce[index];
					float normalizedElevation = (elevation - Data.MinElevation) / (Data.MaxElevation - Data.MinElevation);
					bool drawOcean = elevation <= state.SeaLevel && showLayers.HasFlag(Layers.Water);

					// Base color

					if (drawOcean)
					{
						if (showLayers.HasFlag(Layers.ElevationSubtle))
						{
							color = Color.Lerp(Color.DarkBlue, Color.Blue, (state.SeaLevel - elevation) / Data.MinElevation);
						}
						else
						{
							color = Color.Blue;
						}
						if (ice > 0)
						{
							color = Color.Lerp(color, Color.LightSteelBlue, Math.Min(1.0f, ice / Data.maxIce));
						}
					}
					else
					{
						if (showLayers.HasFlag(Layers.SoilFertility))
						{
							color = Color.Lerp(Color.Gray, Color.Brown, MathUtils.Lerp(lastState.SoilFertility[index], state.SoilFertility[index], stateLerpT));
						}

						if (showLayers.HasFlag(Layers.ElevationSubtle))
						{
							color = Lerp(new List<CVP> { new CVP(Color.Black, -2000), new CVP(color, 0), new CVP(Color.White, 2000) }, elevation);
						}

						if (showLayers.HasFlag(Layers.Vegetation))
						{
							color = Color.Lerp(color, Color.Green, (float)Math.Sqrt(MathHelper.Clamp(MathUtils.Lerp(lastState.Canopy[index], state.Canopy[index], stateLerpT), 0.01f, 1.0f)));
						}
					}


					if (showLayers.HasFlag(Layers.TemperatureSubtle))
					{
						color = Lerp(new List<CVP>() { new CVP(Color.LightBlue, -500 + Data.FreezingTemperature), new CVP(color, Data.FreezingTemperature), new CVP(Color.Red, 500 + Data.FreezingTemperature) }, state.Temperature[index]);
					}


					spriteBatch.Draw(whiteTex, rect, color);

					if (showLayers.HasFlag(Layers.Water))
					{
						float sw = MathUtils.Lerp(lastState.SurfaceWater[index], state.SurfaceWater[index], stateLerpT);
						if (sw > 0 || ice > 0)
						{
							int width = (int)(Math.Min(1.0f, sw + ice) * (tileRenderSize - 2));
							Rectangle surfaceWaterRect = new Rectangle(x * tileRenderSize + 1, y * tileRenderSize + 1, width, width);
							Color waterColor = Color.Lerp(Color.Blue, Color.Teal, (elevation - state.SeaLevel) / (Data.MaxElevation - state.SeaLevel));
							if (ice > 0)
							{
								waterColor = Color.Lerp(waterColor, Color.LightSteelBlue, Math.Min(1.0f, ice / Data.maxIce));
							}

							spriteBatch.Draw(whiteTex, surfaceWaterRect, waterColor * 0.75f);
						}
					}

					if (showLayers.HasFlag(Layers.Temperature))
					{
						color = Lerp(new List<CVP> {
								new CVP(Color.Black, -45+Data.FreezingTemperature),
								new CVP(Color.Blue, -15 + Data.FreezingTemperature),
								new CVP(Color.Green, 15+Data.FreezingTemperature),
								new CVP(Color.Red, 45+Data.FreezingTemperature),
								new CVP(Color.White, 75+Data.FreezingTemperature) },
							state.Temperature[index]);
						spriteBatch.Draw(whiteTex, rect, color);
					}
					else if (showLayers.HasFlag(Layers.Pressure))
					{
						float minPressure = Data.StaticPressure - 40000;
						float maxPressure = Data.StaticPressure + 10000;
						color = Lerp(new List<CVP> { new CVP(Color.Pink, minPressure), new CVP(Color.White, (maxPressure + minPressure) / 2), new CVP(Color.LightBlue, maxPressure) }, state.Pressure[index]);
						spriteBatch.Draw(whiteTex, rect, color);
					}
					else if (showLayers.HasFlag(Layers.WaterVapor))
					{
						float maxHumidity = 15;
						color = Color.Lerp(Color.Black, Color.Blue, Math.Min(1.0f, state.Humidity[index] / maxHumidity));
						spriteBatch.Draw(whiteTex, rect, color);
					}
					else if (showLayers.HasFlag(Layers.RelativeHumidity))
					{
						color = Color.Lerp(Color.Black, Color.Blue, Math.Min(1.0f, GetRelativeHumidity(GetLocalTemperature(Math.Max(0,GetSunVector(state.Ticks, GetLatitude(y)).Z), state.CloudCover[index], state.Temperature[index]), state.Humidity[index], state.CloudElevation[index], Math.Max(elevation, state.SeaLevel)) / Data.dewPointRange));
						spriteBatch.Draw(whiteTex, rect, color);
					}
					else if (showLayers.HasFlag(Layers.Rainfall))
					{
						float maxRainfall = 5.0f / Data.TicksPerYear;
						color = Color.Lerp(Color.Black, Color.Blue, Math.Min(1.0f, state.Rainfall[index] / maxRainfall));
						spriteBatch.Draw(whiteTex, rect, color);
					}
					else if (showLayers.HasFlag(Layers.GroundWater))
					{
						color = Color.Lerp(Color.DarkBlue, Color.Gray, Math.Min(1.0f, state.GroundWater[index] / (Data.MaxWaterTableDepth * Data.MaxSoilPorousness)));
						spriteBatch.Draw(whiteTex, rect, color);
					}
					else if (showLayers.HasFlag(Layers.WaterTableDepth))
					{
						color = Color.Lerp(Color.Black, Color.White, (state.WaterTableDepth[index] - Data.MinWaterTableDepth) / (Data.MaxWaterTableDepth - Data.MinWaterTableDepth));
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
								int p = MathHelper.Clamp((int)Math.Ceiling(tileRenderSize * (float)pop / Data.speciesMaxPopulation), 0, tileRenderSize);
								for (int i = 0; i < p; i++)
								{
									int screenX = rect.X + i * 2;
									int size = i == p - 1 ? 1 : 3;
									spriteBatch.Draw(whiteTex, new Rectangle(screenX, rect.Y + (int)(tileRenderSize * (Math.Cos(screenX + rect.Y + (float)gameTime.TotalGameTime.Ticks / TimeSpan.TicksPerSecond * Math.Sqrt(TimeScale)) / 2 + 0.5f)), size, size), state.Species[s].Color);
								}
							}
						}
					}
				}
			}

			if (showLayers.HasFlag(Layers.CloudHeight) || showLayers.HasFlag(Layers.CloudCoverage) || showLayers.HasFlag(Layers.Wind))
			{
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
							float maxCloudsWidth = 0.5f;
							float maxCloudsToDraw = 1.0f;
							float cloudCover = (float)MathHelper.Clamp(state.CloudCover[index] - minCloudsToDraw, 0.0f, maxCloudsToDraw);
							if (cloudCover > 0)
							{
								float normalizedCloudCover = cloudCover / maxCloudsToDraw;
								int width = (int)(Math.Min(1.0f, cloudCover / maxCloudsWidth) * (tileRenderSize - 2));
								Rectangle rect = new Rectangle(x * tileRenderSize + 1, y * tileRenderSize + 1, width, width);
								spriteBatch.Draw(whiteTex, rect, Color.Lerp(Color.White, Color.Black, normalizedCloudCover) * (float)Math.Sqrt(normalizedCloudCover) * 0.9f);
							}
						}
						if (showLayers.HasFlag(Layers.Wind))
						{
							//							var wind = state.Wind[index];
							float elevationOrSeaLevel = Math.Max(state.SeaLevel, state.Elevation[index]);
							var wind = state.WindCloud[index];
							//var wind = GetWindAtElevation(state, elevationOrSeaLevel, elevationOrSeaLevel, index, GetLatitude(y), state.Normal[index]);
							float maxWindSpeed = 40;
							float maxWindSpeedVertical = 2;
							Color windColor;
							if (wind.Z < 0)
							{
								windColor = Color.Lerp(Color.White, Color.Blue, -wind.Z / maxWindSpeedVertical);
							}
							else
							{
								windColor = Color.Lerp(Color.White, Color.Red, wind.Z / maxWindSpeedVertical);
							}
							float windXYSpeed = (float)Math.Sqrt(wind.X * wind.X + wind.Y * wind.Y);
							Rectangle rect = new Rectangle(x * tileRenderSize, y * tileRenderSize, tileRenderSize, tileRenderSize);
							float windAngle = (float)Math.Atan2(wind.Y, wind.X);
							if (index == 2100)
							{
								Console.WriteLine(wind);
							}
							spriteBatch.Draw(whiteTex, new Rectangle(rect.X + tileRenderSize / 2 - 1, rect.Y + tileRenderSize / 2 - 1, 3, 3), null, Color.White * 0.5f);
							spriteBatch.Draw(whiteTex, new Rectangle(rect.X + tileRenderSize / 2, rect.Y + tileRenderSize / 2, (int)(tileRenderSize * windXYSpeed / maxWindSpeed), 1), null, windColor, windAngle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
						}

					}
				}
			}

			if (showLayers.HasFlag(Layers.Probes))
			{
				for (int i = 0; i < ProbeCount; i++)
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
