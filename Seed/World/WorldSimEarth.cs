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
		public void TickEarth(State state, State nextState)
		{
			float timeOfYear = GetTimeOfYear(state.Ticks);
			for (int y = 0; y < Size; y++)
			{
				float latitude = GetLatitude(y);
				var sunVector = GetSunAngle(state.Ticks, latitude);
				float sunAngle = Math.Max(0, sunVector.Z);

				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);

					float elevation = state.Elevation[index];
					float elevationOrSeaLevel = Math.Max(state.SeaLevel, elevation);
					float cloudCover = state.CloudCover[index];
					float temperature = state.Temperature[index];
					float pressure = state.Pressure[index];
					var gradient = state.Gradient[index];
					var terrainNormal = state.Normal[index];
					float surfaceWater = state.SurfaceWater[index];
					float groundWater = state.GroundWater[index];
					float humidity = state.Humidity[index];
					float cloudElevation = state.CloudElevation[index];
					float waterTableDepth = state.WaterTableDepth[index];
					float soilFertility = state.SoilFertility[index];

					float atmosphereMass = GetAtmosphereMass(elevation, elevationOrSeaLevel);
					Vector3 windAtSurface = GetWindAtElevation(state, elevationOrSeaLevel, elevationOrSeaLevel, index, latitude, terrainNormal);
					float airPressureInverse = StaticPressure / pressure;

					float newGroundWater = groundWater;
					float newSurfaceWater = FlowSurfaceWater(state, nextState, x, y, gradient, surfaceWater);
					float newHumidity = humidity;
					float newCloudCover = cloudCover;
					float newTemperature = UpdateTemperature(state.SeaLevel, elevation, cloudCover, temperature, terrainNormal, humidity, atmosphereMass, sunAngle, sunVector, airPressureInverse);

					MoveAtmosphereOnWind(state, x, y, temperature, humidity, windAtSurface, ref newHumidity, ref newTemperature);

					float evapRate = GetEvaporationRate(cloudCover, temperature, humidity, atmosphereMass, ref windAtSurface, airPressureInverse, sunAngle);
					nextState.Evaporation[index] = UpdateEvaporation(evapRate, elevation, state.SeaLevel, waterTableDepth, ref newHumidity, ref newTemperature, ref newGroundWater, ref newSurfaceWater);
					nextState.Pressure[index] = UpdatePressure(state, x, y, index, elevation, temperature);
					nextState.Wind[index] = UpdateWind(state, x, y, latitude, pressure);
					nextState.Temperature[index] = newTemperature;

					SeepWaterIntoGround(elevation, state.SeaLevel, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);

					MoveHumidityToClouds(elevation, humidity, cloudElevation, windAtSurface, ref newHumidity, ref newCloudCover);

					float rainfall = 0;
					float newCloudElevation = cloudElevation;
					if (cloudCover > 0)
					{
						newCloudElevation = UpdateCloudElevation(elevationOrSeaLevel, temperature, humidity, atmosphereMass);

						rainfall = UpdateRainfall(state, elevation, cloudCover, temperature, cloudElevation, ref newSurfaceWater, ref newCloudCover);
						newCloudCover = MoveClouds(state, nextState, x, y, index, latitude, elevationOrSeaLevel, cloudCover, terrainNormal, cloudElevation, newCloudCover);
					}
					UpdateLandmass(state, nextState, x, y, index, elevation);

					nextState.SurfaceWater[index] = newSurfaceWater;
					nextState.GroundWater[index] = newGroundWater;
					nextState.Humidity[index] = newHumidity;
					nextState.Rainfall[index] = rainfall;
					nextState.CloudCover[index] = newCloudCover;
					nextState.CloudElevation[index] = newCloudElevation;

				}
			}

		}

		private static void MoveHumidityToClouds(float elevation, float humidity, float cloudElevation, Vector3 windAtSurface, ref float newHumidity, ref float newCloudCover)
		{
			float humidityToCloud = MathHelper.Clamp((humidityCloudAbsorptionRate + windAtSurface.Z * humidityToCloudWindSpeed) * humidity / (cloudElevation - elevation), 0, humidity);
			newHumidity -= humidityToCloud;
			newCloudCover += humidityToCloud;
		}

		private float UpdateCloudElevation(float elevationOrSeaLevel, float temperature, float humidity, float atmosphereMass)
		{
			float newCloudElevation;
			float dewPointTemp = (float)Math.Pow(humidity / (dewPointRange * atmosphereMass), 0.25f) * dewPointTemperatureRange + dewPointZero;
			float dewPointElevation = Math.Max(0, (dewPointTemp - temperature) / temperatureLapseRate) + elevationOrSeaLevel;
			newCloudElevation = dewPointElevation;
			return newCloudElevation;
		}

		private float MoveClouds(State state, State nextState, int x, int y, int index, float latitude, float elevationOrSeaLevel, float cloudCover, Vector3 terrainNormal, float cloudElevation, float newCloudCover)
		{
			var windAtCloudElevation = GetWindAtElevation(state, cloudElevation, elevationOrSeaLevel, index, latitude, terrainNormal);
			if (windAtCloudElevation.X != 0 || windAtCloudElevation.Y != 0)
			{
				float windXPercent = Math.Abs(windAtCloudElevation.X) / (Math.Abs(windAtCloudElevation.X) + Math.Abs(windAtCloudElevation.Y));
				float cloudMove = Math.Min(cloudCover, Math.Abs(windAtCloudElevation.X) + Math.Abs(windAtCloudElevation.Y));
				newCloudCover -= cloudMove;
				if (windAtCloudElevation.X > 0)
				{
					nextState.CloudCover[GetIndex(WrapX(x + 1), y)] += cloudMove * windXPercent;
				}
				else
				{
					nextState.CloudCover[GetIndex(WrapX(x - 1), y)] += cloudMove * windXPercent;
				}
				if (windAtCloudElevation.Y > 0)
				{
					nextState.CloudCover[GetIndex(x, WrapY(y + 1))] += cloudMove * (1.0f - windXPercent);
				}
				else
				{
					nextState.CloudCover[GetIndex(x, WrapY(y - 1))] += cloudMove * (1.0f - windXPercent);
				}
			}

			return newCloudCover;
		}

		private float UpdateRainfall(State state, float elevation, float cloudCover, float temperature, float cloudElevation, ref float newSurfaceWater, ref float newCloudCover)
		{
			float temperatureAtCloudElevation = cloudElevation * temperatureLapseRate + temperature;
			float rainPoint = Math.Max(0, (temperatureAtCloudElevation - dewPointZero) * rainPointTemperatureMultiplier);
			if (cloudCover > rainPoint)
			{
				float rainfall = (cloudCover - rainPoint) * RainfallRate;
				newCloudCover -= rainfall;
				if (elevation > state.SeaLevel)
				{
					newSurfaceWater += rainfall;
				}
				return rainfall;
			}
			return 0;
		}

		private float FlowSurfaceWater(State state, State nextState, int x, int y, Vector2 gradient, float surfaceWater)
		{
			float newSurfaceWater = surfaceWater;
			if (surfaceWater > 0)
			{
				if (gradient != Vector2.Zero)
				{
					float flow = Math.Min(surfaceWater, (Math.Abs(gradient.X) + Math.Abs(gradient.Y)) * FlowSpeed);
					newSurfaceWater -= flow;

					for (int i=0;i<4;i++)
					{
						var neighborPoint = GetNeighbor(x, y, i);
						int neighborIndex = GetIndex(neighborPoint.X, neighborPoint.Y);
						float nWater = state.SurfaceWater[neighborIndex];
						if (nWater > 0) {
							var nGradient = state.Gradient[neighborIndex];
							switch (i)
							{
								case 0:
									if (nGradient.X > 0)
									{
										newSurfaceWater += nGradient.X * nWater * FlowSpeed;
									}
									break;
								case 1:
									if (nGradient.X < 0)
									{
										newSurfaceWater += nGradient.X * nWater * FlowSpeed;
									}
									break;
								case 2:
									if (nGradient.Y < 0)
									{
										newSurfaceWater += nGradient.X * nWater * FlowSpeed;
									}
									break;
								case 3:
									if (nGradient.Y > 0)
									{
										newSurfaceWater += nGradient.X * nWater * FlowSpeed;
									}
									break;
							}
						}
					}
				}
			}

			return newSurfaceWater;
		}

		private static void SeepWaterIntoGround(float elevation, float seaLevel, float soilFertility, float waterTableDepth, ref float groundWater, ref float surfaceWater)
		{
			float maxGroundWater = soilFertility * waterTableDepth * MaxSoilPorousness;
			if (elevation > seaLevel)
			{
				float seepage = Math.Min(surfaceWater, maxGroundWater - groundWater) * (soilFertility * MaxSoilPorousness * SoilOsmosisSpeed);
				groundWater += seepage;
				surfaceWater -= seepage;
			}
			else
			{
				groundWater = maxGroundWater;
				surfaceWater = 0;
			}
		}

		private float GetEvaporationRate(float cloudCover, float temperature, float humidity, float atmosphereMass, ref Vector3 windAtSurface, float airPressureInverse, float sunAngle)
		{
			float windSpeed = windAtSurface.Length();
			float tempWithSunAtGround = temperature * (1.0f - Math.Min(cloudCover / cloudContentFullAbsorption, 1.0f)) * sunAngle * localSunHeat;
			float evapTemperature = 1.0f - MathHelper.Clamp((tempWithSunAtGround - evapMinTemperature) / evapTemperatureRange, 0, 1);
			float evapRate = (EvapRateTemperature * (1.0f - evapTemperature * evapTemperature) + EvapRateWind * windSpeed) * airPressureInverse * MathHelper.Clamp(1.0f - (humidity * airPressureInverse / (dewPointRange * atmosphereMass)), 0, 1);
			return evapRate;
		}

		private float UpdateEvaporation(float evapRate, float elevation, float seaLevel, float waterTableDepth, ref float humidity, ref float temperature, ref float groundWater, ref float surfaceWater)
		{
			float evaporation;

			evaporation = 0;
			if (elevation <= seaLevel)
			{
				humidity += evapRate;
				evaporation += evapRate;
			}
			else
			{
				if (surfaceWater > 0)
				{
					float waterSurfaceArea = Math.Min(1.0f, (float)Math.Sqrt(surfaceWater));
					float evap = Math.Max(0, Math.Min(surfaceWater, waterSurfaceArea * evapRate));
					surfaceWater -= evap;
					humidity += evap;
					evaporation += evap;
				}
				var groundWaterEvap = Math.Max(0, Math.Min(groundWater, groundWater / waterTableDepth * evapRate));
				groundWater -= groundWaterEvap;
				humidity += groundWaterEvap;
				evaporation += groundWaterEvap;
			}

			temperature -= evaporation * EvaporativeCoolingRate;
			return evaporation;
		}

		private void MoveAtmosphereOnWind(State state, int x, int y, float temperature, float humidity, Vector3 windAtSurface, ref float newHumidity, ref float newTemperature)
		{
			// in high pressure systems, air from the upper atmosphere will cool us
			if (windAtSurface.Z < 0)
			{
				newTemperature += upperAtmosphereCoolingRate * windAtSurface.Z;
			}
			if (windAtSurface.X != 0 || windAtSurface.Y != 0)
			{
				newHumidity += humidity * (Math.Abs(windAtSurface.X) + Math.Abs(windAtSurface.Y)) * humidityLossFromWind;
			}
			for (int i = 0; i < 4; i++)
			{
				var neighbor = GetNeighbor(x, y, i);
				int nIndex = GetIndex(neighbor.X, neighbor.Y);
				var neighborWind = state.Wind[nIndex];
				float neighborTemp = state.Temperature[nIndex];
				float neighborHum = state.Humidity[nIndex];
				if (neighborWind.X > 0 && i == 0)
				{
					newTemperature += (neighborTemp - temperature) * Math.Min(1.0f, Math.Abs(neighborWind.X) * temperatureLossFromWind);
					newHumidity += neighborHum * Math.Abs(neighborWind.X) * humidityLossFromWind;
				}
				else if (neighborWind.X < 0 && i == 1)
				{
					newTemperature += (neighborTemp - temperature) * Math.Min(1.0f, Math.Abs(neighborWind.X) * temperatureLossFromWind);
					newHumidity += neighborHum * Math.Abs(neighborWind.X) * humidityLossFromWind;
				}
				if (neighborWind.Y > 0 && i == 3)
				{
					newTemperature += (neighborTemp - temperature) * Math.Min(1.0f, Math.Abs(neighborWind.Y) * temperatureLossFromWind);
					newHumidity += neighborHum * Math.Abs(neighborWind.Y) * humidityLossFromWind;
				}
				else if (neighborWind.Y < 0 && i == 2)
				{
					newTemperature += (neighborTemp - temperature) * Math.Min(1.0f, Math.Abs(neighborWind.Y) * temperatureLossFromWind);
					newHumidity += neighborHum * Math.Abs(neighborWind.Y) * humidityLossFromWind;
				}
			}
		}

		private float UpdateTemperature(float seaLevel, float elevation, float cloudCover, float temperature, Vector3 terrainNormal, float humidity, float atmosphereMass, float sunAngle, Vector3 sunVector, float airPressureInverse)
		{

			// TEMPERATURE
			float cloudMass = Math.Min(1.0f, cloudCover / cloudContentFullAbsorption);
			float cloudAbsorptionFactor = cloudAbsorptionRate * cloudMass;
			float cloudReflectionFactor = cloudReflectionRate * cloudMass;
			float humidityPercentage = humidity / atmosphereMass;


			float heatLossFactor = (1.0f - carbonDioxide * heatLossPreventionCarbonDioxide) * (1.0f - humidityPercentage);
			float loss = temperature * (1.0f - cloudReflectionFactor) * (heatLoss * heatLossFactor * airPressureInverse);
			//		float cloudLoss = 0;
			float gain = 0;
			float cloudGain = 0;
			float cloudReflection = 0;
			float reflection = 0;
			if (sunAngle > 0)
			{
				cloudGain = sunAngle * heatGainFromSun * cloudAbsorptionFactor;
				cloudReflection = sunAngle * heatGainFromSun * cloudReflectionFactor;

				// gain any heat not absorbed on first pass through the clouds
				float slope = 1;
				if (elevation > seaLevel) // land
				{
					slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
					//							reflection = mineralTypes[cells[i, j].mineral].heatReflection;
					reflection = HeatReflectionLand;
				}
				else // water
				{
					reflection = heatReflectionWater;
				}
				float sunGain = slope * heatGainFromSun - cloudGain - cloudReflection;
				gain += sunGain * (1.0f - reflection) * (1.0f - humidityPercentage);

				// trap some heat in
				gain += cloudMass * cloudReflectionFactor * loss;
			}

			float newTemp = temperature + gain - loss;
			return newTemp;
		}

		private float GetAtmosphereMass(float elevation, float elevationOrSeaLevel)
		{
			float atmosphereMass;
			if (elevation <= troposphereElevation)
			{
				atmosphereMass = (troposphereElevation - elevationOrSeaLevel) / troposphereAtmosphereContent;
			}
			else
			{
				atmosphereMass = troposphereElevation + (stratosphereElevation - elevationOrSeaLevel) * (1.0f - troposphereAtmosphereContent) * troposphereElevation;
			}

			return atmosphereMass;
		}

		private void UpdateLandmass(State state, State nextState, int x, int y, int index, float elevation)
		{
			if (elevation <= state.SeaLevel)
			{
				nextState.Gradient[index] = Vector2.Zero;
				nextState.Normal[index] = new Vector3(0, 0, 1);
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
				}
				else
				{
					g = Vector2.Zero;
				}

				nextState.Gradient[index] = g;
				nextState.Normal[index] = Vector3.Normalize(new Vector3(g.X, g.Y, tileSize));

			}
		}

		private float UpdatePressure(State state, int x, int y, int index, float elevation, float temperature)
		{
			float temperatureDifferential = 0;
			temperatureDifferential += state.Temperature[GetIndex(WrapX(x + 1), y)] - temperature;
			temperatureDifferential += state.Temperature[GetIndex(WrapX(x - 1), y)] - temperature;
			temperatureDifferential += state.Temperature[GetIndex(x, WrapY(y + 1))] - temperature;
			temperatureDifferential += state.Temperature[GetIndex(x, WrapY(y - 1))] - temperature;
			temperatureDifferential /= 4;
			return GetPressureAtElevation(state, index, Math.Max(state.SeaLevel, elevation), temperatureDifferential);
		}
		private Vector3 UpdateWind(State state, int x, int y, float latitude, float pressure)
		{
			Vector2 pressureDifferential = Vector2.Zero;
			pressureDifferential.X -= state.Pressure[GetIndex(WrapX(x + 1), y)] - pressure;
			pressureDifferential.X += state.Pressure[GetIndex(WrapX(x - 1), y)] - pressure;
			pressureDifferential.Y -= state.Pressure[GetIndex(x, WrapY(y + 1))] - pressure;
			pressureDifferential.Y += state.Pressure[GetIndex(x, WrapY(y - 1))] - pressure;

			float coriolisPower = (float)Math.Sqrt(Math.Abs(latitude));
			int coriolisDir = latitude > 0 ? 1 : -1;

			Vector2 windXY = pressureDifferentialWindSpeed * (coriolisDir * coriolisPower * Vector2.Transform(pressureDifferential, Matrix.CreateRotationZ((float)Math.PI / 2)) + (1.0f - coriolisPower) * pressureDifferential);
			Vector3 pressureWind = new Vector3(windXY.X, windXY.Y, (pressureDifferential.X + pressureDifferential.Y) / 4 * pressureDifferentialWindSpeed);

			Vector3 nWind = Vector3.Zero;
			for (int i = 0; i < 4; i++)
			{
				var neighbor = GetNeighbor(x, y, i);
				int nIndex = GetIndex(neighbor.X, neighbor.Y);
				var neighborWind = state.Wind[nIndex];
				nWind += neighborWind;
			}
			pressureWind = pressureWind * 0.9f + nWind / 4 * 0.1f;

			return pressureWind;
		}

	}
}
