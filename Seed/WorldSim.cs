using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Seed
{
	public partial class World
	{
		private int WrapX(int x)
		{
			if (x < 0)
			{
				x += Size;
			}
			else if (x >= Size)
			{
				x -= Size;
			}
			return x;
		}
		private int WrapY(int y)
		{
			return MathHelper.Clamp(y, 0, Size - 1);
		}
		private Tuple<int, int> GetNeighbor(int x, int y, int neighborIndex)
		{
			switch (neighborIndex)
			{
				case 0:
					x--;
					if (x < 0)
					{
						x += Size;
					}
					break;
				case 1:
					x++;
					if (x >= Size)
					{
						x -= Size;
					}
					break;
				case 2:
					y++;
					if (y >= Size)
					{
						y = Size - 1;
						//						x = (x + Size / 2) % Size;
					}
					break;
				case 3:
					y--;
					if (y < 0)
					{
						y = 0;
						//						x = (x + Size / 2) % Size;
					}
					break;
			}
			return new Tuple<int, int>(x, y);
		}

		public float GetGroundWaterSaturation(float groundWater, float waterTableDepth)
		{
			return groundWater / waterTableDepth;
		}
		public float GetFreshWaterAvailability(float surfaceWater, float groundWaterSaturation)
		{
			return surfaceWater > 0 ? 1.0f : Math.Min(1.0f, groundWaterSaturation);
		}
		public float GetPopulationDensity(float population)
		{
			return (float)population / speciesMaxPopulation;
		}
		public float GetStarvation(float populationDensity, float canopy)
		{
			return Math.Max(0, (populationDensity - canopy) * starvationSpeed);
		}
		public float GetTemperatureDeath(ref State state, float temperature, int species)
		{
			return Math.Abs((temperature - state.Species[species].RestingTemperature) / state.Species[species].TemperatureRange);
		}
		public float GetDehydration(float populationDensity, float freshWaterAvailability)
		{
			return Math.Max(0, (populationDensity - freshWaterAvailability / freshWaterMaxAvailability) * dehydrationSpeed);
		}
		public void Tick(State state, State nextState)
		{
			nextState = state;
			nextState.SpeciesStats = new SpeciesStat[MaxSpecies];
			nextState.Ticks++;

			List<Task> simTasks = new List<Task>();
			simTasks.Add(Task.Run(() =>
			{
				TickEarth(state, nextState);
			}));
			simTasks.Add(Task.Run(() =>
			{
				TickAnimals(state, nextState);
			}));
			Task.WaitAll(simTasks.ToArray());
		}

		public void TickEarth(State state, State nextState)
		{
			float timeOfYear = GetTimeOfYear(ref state);
			float timeOfYearTempDelta = (float)-Math.Cos(timeOfYear * Math.PI * 2);
			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);



					float c = state.Canopy[index];
					float elevation = state.Elevation[index];
					float cloudCover = state.CloudCover[index];
					float latitude = GetLatitude(y);
					float temperature = (1.0f - MathHelper.Clamp(elevation - state.SeaLevel, 0, MaxElevation) / (MaxElevation - state.SeaLevel)) * (1.0f - latitude * latitude) * (MaxTemperature - MinTemperature) + MinTemperature;


					// Earth

					Vector2 wind = Vector2.Zero;
					wind.X = (float)-Math.Cos(latitude * Math.PI * 1.5f);
					wind.Y = (float)-Math.Sin(latitude * Math.PI * 2.5f) * 0.5f;
					wind *= tradeWindSpeed;
					nextState.Wind[index] = wind;

					int coriolisDir = latitude > 0 ? 1 : -1;
					float tempDifferentialWindSpeed = 0.001f;
					wind.Y += tempDifferentialWindSpeed * coriolisDir * (state.Temperature[GetIndex(WrapX(x + 1), y)] - temperature);
					wind.Y -= tempDifferentialWindSpeed * coriolisDir * (state.Temperature[GetIndex(WrapX(x - 1), y)] - temperature);
					wind.X += tempDifferentialWindSpeed * coriolisDir * (state.Temperature[GetIndex(x, WrapY(y + 1))] - temperature);
					wind.X -= tempDifferentialWindSpeed * coriolisDir * (state.Temperature[GetIndex(x, WrapY(y - 1))] - temperature);

					temperature += 20 * latitude * timeOfYearTempDelta;
					//temperature -= Math.Min(1.0f, cloudCover) * 20.0f;
					nextState.Temperature[index] = temperature;
					if (elevation <= state.SeaLevel)
					{
						cloudCover += Math.Max(0, temperature * EvapRate);
					}
					else
					{
						float surfaceWater = state.SurfaceWater[index];
						var groundWater = state.GroundWater[index];
						var waterTableDepth = state.WaterTableDepth[index];
						var soilFertility = state.SoilFertility[index];
						if (surfaceWater > 0)
						{
							float evap = Math.Min(surfaceWater, surfaceWater * surfaceWater * temperature * EvapRate);
							surfaceWater -= evap;
							cloudCover += evap;

							var gradient = state.Gradient[index];
							if (gradient != Vector2.Zero)
							{
								float flow = Math.Min(surfaceWater, (Math.Abs(gradient.X) + Math.Abs(gradient.Y)) * FlowSpeed);
								surfaceWater -= flow;
								float flowXPercent = Math.Abs(gradient.X) / (Math.Abs(gradient.X) + Math.Abs(gradient.Y));
								int neighborX = GetIndex(WrapX(x + Math.Sign(gradient.X)), y);
								int neighborY = GetIndex(x, WrapY(y + Math.Sign(gradient.Y)));
								if (state.Elevation[neighborX] > state.SeaLevel)
								{
									// TODO: this is no good in the double buffering
									nextState.SurfaceWater[neighborX] += flow * flowXPercent;
								}
								if (state.Elevation[neighborY] > state.SeaLevel)
								{
									nextState.SurfaceWater[neighborY] += flow * (1.0f - flowXPercent);
								}
							}
						}
						var groundWaterEvap = Math.Min(groundWater, groundWater / waterTableDepth * temperature * EvapRate);
						groundWater -= groundWaterEvap;
						cloudCover += groundWaterEvap;
						float maxGroundWater = soilFertility * waterTableDepth;
						float seepage = Math.Min(surfaceWater, maxGroundWater - groundWater) * soilFertility;
						groundWater += seepage;
						surfaceWater -= seepage;
						nextState.SurfaceWater[index] = surfaceWater;
						nextState.GroundWater[index] = groundWater;
					}
					float rainfall = 0;
					if (cloudCover > 0)
					{
						if (temperature > 0 && cloudCover > temperature / MaxTemperature)
						{
							rainfall = (cloudCover - temperature / MaxTemperature) * RainfallRate;
							cloudCover -= rainfall;
							if (elevation > state.SeaLevel)
							{
								nextState.SurfaceWater[index] += rainfall;
							}
						}
						if (wind != Vector2.Zero)
						{
							float windXPercent = Math.Abs(wind.X) / (Math.Abs(wind.X) + Math.Abs(wind.Y));
							float cloudMove = Math.Min(cloudCover, Math.Abs(wind.X) + Math.Abs(wind.Y));
							cloudCover -= cloudMove;
							if (wind.X > 0)
							{
								nextState.CloudCover[GetIndex(WrapX(x + 1), y)] += cloudMove * windXPercent;
							}
							else
							{
								nextState.CloudCover[GetIndex(WrapX(x - 1), y)] += cloudMove * windXPercent;
							}
							if (wind.Y > 0)
							{
								nextState.CloudCover[GetIndex(x, WrapY(y + 1))] += cloudMove * windXPercent;
							}
							else
							{
								nextState.CloudCover[GetIndex(x, WrapY(y - 1))] += cloudMove * windXPercent;
							}
						}
					}
					nextState.CloudCover[index] = cloudCover;

				}
			}

		}

		public void TickAnimals(State state, State nextState)
		{
			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);

					// Foliage
					float freshWaterAvailability = 0;
					float canopy = state.Canopy[index];
					float temperature = state.Temperature[index];
					if (canopy > 0)
					{
						float t = state.Temperature[index];
						float sf = state.SoilFertility[index];
						float groundWaterSaturation = GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index]);
						float surfaceWater = state.SurfaceWater[index];
						freshWaterAvailability = GetFreshWaterAvailability(surfaceWater, groundWaterSaturation);

						float desiredCanopy = sf * Math.Min(groundWaterSaturation + surfaceWater, 1.0f) * Math.Max(0, t / MaxTemperature);
						float canopyGrowth = (desiredCanopy - canopy) * canopyGrowthRate;
						canopy += canopyGrowth;

						float expansion = canopy * canopyGrowthRate * 0.25f;
						for (int i = 0; i < 4; i++)
						{
							var n = GetNeighbor(x, y, i);
							int neighborIndex = GetIndex(n.Item1, n.Item2);
							if (state.Elevation[neighborIndex] > state.SeaLevel)
							{
								nextState.Canopy[neighborIndex] = Math.Min(1.0f, nextState.Canopy[neighborIndex] + expansion);
							}
						}
					}

					// ANIMALS
					float canopyEaten = 0;
					for (int s = 0; s < MaxSpecies; s++)
					{
						int speciesIndex = GetSpeciesIndex(x, y, s);
						float p = state.Population[speciesIndex];
						if (p > 0)
						{
							float populationDensity = GetPopulationDensity(p);


							float populationPercentDied = speciesDeathRate;
							populationPercentDied += GetTemperatureDeath(ref state, temperature, s);
							populationPercentDied += GetStarvation(populationDensity, canopy);
							populationPercentDied += GetDehydration(populationDensity, freshWaterAvailability);
							populationPercentDied = Math.Min(1.0f, populationPercentDied);

							float populationDelta = 1.0f + speciesGrowthRate - populationPercentDied;
							if (populationDensity > minPopulationDensityForExpansion)
							{
								float expansion = speciesGrowthRate * populationExpansionPercent * 0.25f;
								float neighborGrowthRate = speciesMaxPopulation * populationDensity * expansion;
								for (int i = 0; i < 4; i++)
								{
									var n = GetNeighbor(x, y, i);
									if (state.Elevation[GetIndex(n.Item1, n.Item2)] > state.SeaLevel)
									{
										int nIndex = GetSpeciesIndex(n.Item1, n.Item2, s);
										nextState.Population[nIndex] += neighborGrowthRate;
										populationDelta -= expansion;
									}
								}
							}
							float newPopDensity = populationDensity * populationDelta;

							float newPopulation = newPopDensity * speciesMaxPopulation;
							if (newPopulation < 1 && newPopulation < state.Population[speciesIndex])
							{
								newPopulation = 0;
							}
							nextState.SpeciesStats[s].Population += newPopulation;
							nextState.Population[speciesIndex] = newPopulation;
							canopyEaten += populationDensity * speciesEatRate;
						}
					}
					nextState.Canopy[index] = Math.Max(0, canopy - canopyEaten);
				}
			}

		}
	}
}