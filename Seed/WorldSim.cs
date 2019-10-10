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
		public int WrapX(int x)
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
		public int WrapY(int y)
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

		public float GetGroundWaterSaturation(float groundWater, float waterTableDepth, float soilPorousness)
		{
			return groundWater / (waterTableDepth * soilPorousness);
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

		public float GetPressureAtElevation(State state, int index, float elevation, float temperatureDifferential)
		{
			// Units: Pascals
			// Barometric Formula
			// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
			// https://en.wikipedia.org/wiki/Barometric_formula
			// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

			const float PressureExponent = GravitationalAcceleration * MolarMassEarthAir / (UniversalGasConstant * StdTempLapseRate);
			float standardPressure = StaticPressure * (float)Math.Pow(StdTemp / (StdTemp + StdTempLapseRate * elevation), PressureExponent);

			const float verticalWindPressureAdjustment = 10000;
			const float temperatureGradientPressureAdjustment = 100;
			float pressure = standardPressure - verticalWindPressureAdjustment * state.Wind[index].Z + temperatureGradientPressureAdjustment * temperatureDifferential;
			return pressure;
		}
		public void Tick(State state, State nextState)
		{
			nextState = (State)state.Clone();
			nextState.SpeciesStats = new SpeciesStat[MaxSpecies];

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
			for (int x = 0; x < Size; x++)
			{
				for (int y = 0; y < Size; y++)
				{
					int index = GetIndex(x, y);



					float c = state.Canopy[index];
					float elevation = state.Elevation[index];
					float cloudCover = state.CloudCover[index];
					float temperature = state.Temperature[index];
					float latitude = GetLatitude(y);

					//float atmosphereThickness = 1.0f - elevation / (100000 - (elevation - state.SeaLevel));
					//float heatTransfer = atmosphereThickness * (1.0f - Math.Min(state.CloudCover[index], 10.0f) * 0.1f);
					//float heatEntered = heatEnteredSpeed * heatTransfer * (1.0f + 0.5f * (latitude * timeOfYearTempDelta) * (1.0f - latitude * latitude));
					//float heatLeft = heatLeftSpeed * heatTransfer * absoluteTemperature;

					//nextState.Temperature[index] = temperature + (heatEntered - heatLeft);

					Vector3 wind = Vector3.Zero;
					float pitch = (float)(latitude * Math.PI * 3f);
					if (latitude < 0.3333 && latitude > -0.3333)
					{
						float yaw = (float)(latitude * Math.PI * 1.5f);
						wind.X = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Cos(yaw));
						wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Sin(yaw));
						wind.Z = (float)Math.Cos(pitch);
					}
					else if (latitude < 0.667 && latitude > -0.667)
					{
						float yaw = (float)(latitude * Math.PI * 1.5f);
						wind.X = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Cos(yaw));
						wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * Math.Sin(yaw));
						wind.Z = (float)Math.Cos(pitch);
					}
					else
					{
						float yaw = (float)(latitude * Math.PI * 1.5f);
						wind.X = (float)(Math.Abs(Math.Sin(pitch)) * Math.Cos(yaw));
						wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * Math.Sin(yaw));
						wind.Z = (float)Math.Cos(pitch);
					}
					wind *= tradeWindSpeed;

					Vector2 pressureDifferential = Vector2.Zero;
					float pressure = state.Pressure[index];
					pressureDifferential.X -= state.Pressure[GetIndex(WrapX(x + 1), y)] - pressure;
					pressureDifferential.X += state.Pressure[GetIndex(WrapX(x - 1), y)] - pressure;
					pressureDifferential.Y -= state.Pressure[GetIndex(x, WrapY(y + 1))] - pressure;
					pressureDifferential.Y += state.Pressure[GetIndex(x, WrapY(y - 1))] - pressure;


					float temperatureDifferential = 0;
					temperatureDifferential += state.Temperature[GetIndex(WrapX(x + 1), y)] - temperature;
					temperatureDifferential += state.Temperature[GetIndex(WrapX(x - 1), y)] - temperature;
					temperatureDifferential += state.Temperature[GetIndex(x, WrapY(y + 1))] - temperature;
					temperatureDifferential += state.Temperature[GetIndex(x, WrapY(y - 1))] - temperature;
					temperatureDifferential /= 4;
					nextState.Pressure[index] = GetPressureAtElevation(state, index, Math.Max(state.SeaLevel, elevation), temperatureDifferential);

					float coriolisPower = (float)Math.Sqrt(Math.Abs(latitude));
					int coriolisDir = latitude > 0 ? 1 : -1;

					Vector2 windXY = pressureDifferentialWindSpeed * (coriolisDir * coriolisPower * Vector2.Transform(pressureDifferential, Matrix.CreateRotationZ((float)Math.PI / 2)) + (1.0f - coriolisPower) * pressureDifferential);
					wind += new Vector3(windXY.X, windXY.Y, (pressureDifferential.X + pressureDifferential.Y) / 4 * pressureDifferentialWindSpeed);

					Vector3 nWind = Vector3.Zero;
					for (int i = 0; i < 4; i++)
					{
						var neighbor = GetNeighbor(x, y, i);
						int nIndex = GetIndex(neighbor.Item1, neighbor.Item2);
						var neighborWind = state.Wind[nIndex];
						nWind += neighborWind;
					}
					wind = wind * 0.9f + nWind / 4 * 0.1f; 

					nextState.Wind[index] = wind;





					float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
					float humidity = Math.Min(1.0f, cloudCover / 2.0f);
					float airPressure = MathHelper.Clamp(1.0f - troposphereAtmosphereContent * elevationOrSeaLevel / troposphereElevation, 1.0f - troposphereAtmosphereContent, 1.0f);
					float airPressureInverse = 1.0f / airPressure;
					float atmosphereMass;
					if (elevation <= troposphereElevation) {
						atmosphereMass = (troposphereElevation - elevationOrSeaLevel) / troposphereAtmosphereContent;
					} else
					{
						atmosphereMass = troposphereElevation + (stratosphereElevation - elevationOrSeaLevel) * (1.0f - troposphereAtmosphereContent) * troposphereElevation;
					}

					var sunVector = GetSunAngle(ref state, latitude);
					float sunAngle = Math.Max(0, Vector3.Dot(new Vector3(0,0,1), sunVector));
					var gradient = state.Gradient[index];
					var terrainNormal = state.Normal[index];

					// TEMPERATURE
					float cloudMass = Math.Min(1.0f, cloudCover / cloudContentFullAbsorption);
					float cloudAbsorptionFactor = cloudAbsorptionRate * cloudMass;
					float cloudReflectionFactor = cloudReflectionRate * cloudMass;
					float humidityPercentage = humidity / atmosphereMass;

					float surfaceWater = state.SurfaceWater[index];
					var groundWater = state.GroundWater[index];



					float heatLossFactor = (1.0f - carbonDioxide * heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
					float loss = temperature * (1.0f - cloudReflectionFactor) * (heatLoss * heatLossFactor * airPressureInverse);
					//		float cloudLoss = 0;
					float gain = 0;
					float cloudGain = 0;
					float cloudReflection = 0;
					float reflection = 0;
					if (sunVector.Z > 0)
					{
						float slope = sunAngle; // use this for cloud, and water if necessary
						cloudGain = slope * heatGainFromSun * cloudAbsorptionFactor;
						cloudReflection = slope * heatGainFromSun * cloudReflectionFactor;

						// gain any heat not absorbed on first pass through the clouds
						float sunGain = slope * heatGainFromSun - cloudGain - cloudReflection;
						if (elevation > state.SeaLevel) // land
						{
							slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
							//							reflection = mineralTypes[cells[i, j].mineral].heatReflection;
							reflection = HeatReflectionLand;
						}
						else // water
						{
							reflection = heatReflectionWater;
						}
						gain += sunGain * (1.0f - reflection) * (1.0f - humidityPercentage);

						// trap some heat in
						gain += cloudMass * cloudReflectionFactor * loss;
					}


					float windSpeed = wind.Length();
					float tempWithSun = temperature;
					float evapTemperature = 1.0f - MathHelper.Clamp((tempWithSun - evapMinTemperature) / evapTemperatureRange, 0, 1);
					float evapRate = (EvapRateTemperature * (1.0f - evapTemperature * evapTemperature) + EvapRateWind * windSpeed) * airPressureInverse * MathHelper.Clamp(1.0f - (humidity * airPressureInverse / (dewPointRange * atmosphereMass)), 0, 1);

					float totalEvap = 0;
					if (elevation <= state.SeaLevel)
					{
						cloudCover += evapRate;
						totalEvap += evapRate;
					}
					else
					{
						var waterTableDepth = state.WaterTableDepth[index];
						if (surfaceWater > 0)
						{
							float waterSurfaceArea = Math.Min(1.0f, (float)Math.Sqrt(surfaceWater));
							float evap = Math.Max(0, Math.Min(surfaceWater, waterSurfaceArea * evapRate));
							surfaceWater -= evap;
							cloudCover += evap;
							totalEvap += evap;
						}
						var groundWaterEvap = Math.Max(0, Math.Min(groundWater, groundWater / waterTableDepth * evapRate));
						groundWater -= groundWaterEvap;
						cloudCover += groundWaterEvap;
						totalEvap += groundWaterEvap;
					}

					loss += totalEvap * EvaporativeCoolingRate;



					if (wind.X != 0 || wind.Y != 0)
					{
						loss += state.Temperature[index] * (Math.Abs(wind.X)+Math.Abs(wind.Y)) * temperatureLossFromWind;
					}
					for (int i = 0; i < 4; i++)
					{
						var neighbor = GetNeighbor(x, y, i);
						int nIndex = GetIndex(neighbor.Item1, neighbor.Item2);
						var neighborWind = state.Wind[nIndex];
						float neighborTemp = state.Temperature[nIndex];
						float windXPercent = Math.Abs(neighborWind.X) / (Math.Abs(neighborWind.X) + Math.Abs(neighborWind.Y));
						if (neighborWind.X > 0 && i==0)
						{
							gain += neighborTemp * Math.Abs(neighborWind.X) * temperatureLossFromWind;
						}
						else if (neighborWind.X < 0 && i==1)
						{
							gain += neighborTemp * Math.Abs(neighborWind.X) * temperatureLossFromWind;
						}
						if (neighborWind.Y > 0 && i==3)
						{
							gain += neighborTemp * Math.Abs(neighborWind.Y) * temperatureLossFromWind;
						}
						else if (neighborWind.Y < 0 && i==2)
						{
							gain += neighborTemp * Math.Abs(neighborWind.Y) * temperatureLossFromWind;
						}
					}

					nextState.Temperature[index] = temperature + gain - loss;
					nextState.CloudElevation[index] = Math.Max(state.SeaLevel, state.Elevation[index]) + 1000;






					// Earth


					//temperature -= Math.Min(1.0f, cloudCover) * 20.0f;

					if (elevation > state.SeaLevel)
					{
						var waterTableDepth = state.WaterTableDepth[index];
						var soilFertility = state.SoilFertility[index];
						if (surfaceWater > 0)
						{
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
						float maxGroundWater = soilFertility * waterTableDepth * MaxSoilPorousness;
						float seepage = Math.Min(surfaceWater, maxGroundWater - groundWater) * (soilFertility * MaxSoilPorousness * SoilOsmosisSpeed);
						groundWater += seepage;
						surfaceWater -= seepage;
						nextState.SurfaceWater[index] = surfaceWater;
						nextState.GroundWater[index] = groundWater;
					}

					float rainfall = 0;
					if (cloudCover > 0)
					{
						float cloudElevation = state.CloudElevation[index];
						float temperatureAtCloudElevation = cloudElevation * temperatureLapseRate + temperature;
						float rainPoint = Math.Max(0, (temperatureAtCloudElevation - dewPointZero) * rainPointTemperatureMultiplier);
						if (cloudCover > rainPoint)
						{
							rainfall = (cloudCover - rainPoint) * RainfallRate;
							cloudCover -= rainfall;
							if (elevation > state.SeaLevel)
							{
								nextState.SurfaceWater[index] += rainfall;
							}
						}
						if (wind.X != 0 || wind.Y != 0)
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
								nextState.CloudCover[GetIndex(x, WrapY(y + 1))] += cloudMove * (1.0f - windXPercent);
							}
							else
							{
								nextState.CloudCover[GetIndex(x, WrapY(y - 1))] += cloudMove * (1.0f - windXPercent);
							}
						}
					}
					nextState.Rainfall[index] = rainfall;
					nextState.Evaporation[index] = totalEvap;
					nextState.CloudCover[index] = cloudCover;

					if (elevation <= state.SeaLevel)
					{
						nextState.Gradient[index] = Vector2.Zero;
						nextState.Normal[index] = Vector3.Up;
					}
					else
					{
						int indexW = GetIndex(WrapX(x - 1), y);
						int indexE = GetIndex(WrapX(x + 1), y);
						int indexN = GetIndex(x, WrapY(y - 1));
						int indexS = GetIndex(x, WrapY(y + 1));
						float e = state.Elevation[index];
						float west = state.Elevation[indexW];
						float east = state.Elevation[indexE];
						float north = state.Elevation[indexN];
						float south = state.Elevation[indexS];

						e += state.SurfaceWater[index];
						west += state.SurfaceWater[indexW];
						east += state.SurfaceWater[indexE];
						north += state.SurfaceWater[indexN];
						south += state.SurfaceWater[indexS];

						Vector2 g;
						if (west < e && west < east && west < north && west < south)
						{
							g = new Vector2(west - e, 0);
						}
						else if (east < e && east < west && east < north && east < south)
						{
							g = new Vector2(e - east, 0);
						}
						else if (north < e && north < west && north < east && north < south)
						{
							g = new Vector2(0, north - e);
						}
						else if (south < e && south < west && south < north && south < east)
						{
							g = new Vector2(0, e - south);
						} else
						{
							g = Vector2.Zero;
						}

						nextState.Gradient[index] = g;
						nextState.Normal[index] = Vector3.Normalize(new Vector3(g.X, g.Y, tileSize));

					}
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
						float groundWaterSaturation = GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], sf * MaxSoilPorousness);
						float surfaceWater = state.SurfaceWater[index];
						freshWaterAvailability = GetFreshWaterAvailability(surfaceWater, groundWaterSaturation);

						float desiredCanopy = sf * Math.Min(groundWaterSaturation + surfaceWater, 1.0f) * Math.Max(0, (t - MinTemperature) / (MaxTemperature-MinTemperature));
						float canopyGrowth = (desiredCanopy - canopy) * canopyGrowthRate;
						canopy += canopyGrowth;

						float expansion = canopy * canopyGrowth * 0.25f;
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