﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Seed
{
	public partial class World
	{

		public void TickBarometricPressure(State state, State nextState)
		{
			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);
					float elevation = state.Elevation[index];
					float temperature = state.Temperature[index];

					nextState.Pressure[index] = UpdateBarometricPressure(state, x, y, index, elevation, temperature);
				}
			}
		}
		public void TickWind(State state, State nextState)
		{
			for (int y = 0; y < Size; y++)
			{
				float latitude = GetLatitude(y);
				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);
					float pressure = state.Pressure[index];
					var newWind = UpdateWind(state, x, y, latitude, pressure);
					nextState.Wind[index] = newWind;
				}
			}
		}

		public void TickAtmosphere(State state, State nextState)
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
					var gradient = state.FlowDirection[index];
					var terrainNormal = state.Normal[index];
					float surfaceWater = state.SurfaceWater[index];
					float groundWater = state.GroundWater[index];
					float humidity = state.Humidity[index];
					float cloudElevation = state.CloudElevation[index];
					float waterTableDepth = state.WaterTableDepth[index];
					float soilFertility = state.SoilFertility[index];
					float surfaceIce = state.SurfaceIce[index];

					float newEvaporation;
					float newGroundWater = groundWater;
					float newHumidity = humidity;
					float newCloudCover = cloudCover;
					float newSurfaceWater = surfaceWater;
					float newSurfaceIce = surfaceIce;
					float newTemperature = temperature;
					float newCloudElevation = cloudElevation;
					float rainfall = 0;


					float atmosphereMass = GetAtmosphereMass(elevation, elevationOrSeaLevel);
					Vector3 windAtSurface = GetWindAtElevation(state, elevationOrSeaLevel, elevationOrSeaLevel, index, latitude, terrainNormal);
					float windSpeedAtSurface = windAtSurface.Length();
					float airPressureInverse = StaticPressure / pressure;
					float tempWithSunAtGround = temperature + (1.0f - Math.Min(cloudCover / cloudContentFullAbsorption, 1.0f)) * sunAngle * localSunHeat;
					float evapRate = GetEvaporationRate(surfaceIce, tempWithSunAtGround, humidity, atmosphereMass, windSpeedAtSurface, airPressureInverse, sunAngle);

					UpdateTemperature(state.SeaLevel, elevation, surfaceIce, cloudCover, temperature, terrainNormal, humidity, atmosphereMass, sunAngle, sunVector, airPressureInverse, ref newTemperature);
					MoveAtmosphereOnWind(state, x, y, temperature, humidity, windAtSurface, ref newHumidity, ref newTemperature);
					SimulateIce(elevation, state.SeaLevel, tempWithSunAtGround, ref newSurfaceWater, ref newSurfaceIce);
					FlowWater(state, x, y, gradient, soilFertility, ref newSurfaceWater, ref newGroundWater);
					SeepWaterIntoGround(elevation, state.SeaLevel, soilFertility, waterTableDepth, ref newGroundWater, ref newSurfaceWater);
					EvaporateWater(evapRate, elevation, state.SeaLevel, groundWater, waterTableDepth, ref newHumidity, ref newTemperature, ref newGroundWater, ref newSurfaceWater, out newEvaporation);
					MoveHumidityToClouds(elevation, humidity, cloudElevation, windAtSurface, ref newHumidity, ref newCloudCover);
					if (cloudCover > 0)
					{
						var windAtCloudElevation = GetWindAtElevation(state, cloudElevation, elevationOrSeaLevel, index, latitude, terrainNormal);
						UpdateCloudElevation(elevationOrSeaLevel, temperature, humidity, atmosphereMass, windAtCloudElevation, ref newCloudElevation);
						MoveClouds(nextState, x, y, windAtCloudElevation, cloudCover, ref newCloudCover);
						rainfall = UpdateRainfall(state, elevation, cloudCover, temperature, cloudElevation, ref newSurfaceWater, ref newCloudCover);
					}

					if (float.IsNaN(newTemperature) || float.IsNaN(newEvaporation) || float.IsNaN(newSurfaceWater) || float.IsNaN(newSurfaceIce) || float.IsNaN(newGroundWater) || float.IsNaN(newHumidity) || float.IsNaN(newCloudCover) || float.IsNaN(newCloudElevation))
					{
						break;
					}
					nextState.Temperature[index] = newTemperature;
					nextState.Evaporation[index] = newEvaporation;
					nextState.SurfaceWater[index] = newSurfaceWater;
					nextState.SurfaceIce[index] = newSurfaceIce;
					nextState.GroundWater[index] = newGroundWater;
					nextState.Humidity[index] = newHumidity;
					nextState.Rainfall[index] = rainfall;
					nextState.CloudCover[index] = newCloudCover;
					nextState.CloudElevation[index] = newCloudElevation;

				}
			}

		}

		Vector3 GetWindAtElevation(State state, float windElevation, float landElevation, int index, float latitude, Vector3 normal)
		{
			float tropopauseElevation = (1.0f - Math.Abs(latitude)) * (MaxTropopauseElevation - MinTropopauseElevation) + MinTropopauseElevation + TropopauseElevationSeason * latitude * (GetTimeOfYear(state.Ticks) * 2 - 1);
			float hadleyCellHeight = Math.Min(1.0f, (windElevation - landElevation) / (tropopauseElevation - landElevation));

			Vector3 wind = Vector3.Zero;
			float yaw = (float)(latitude * Math.PI * 1.5f);
			float pitch = (float)(latitude * Math.PI * 3f);
			if (latitude < 0.3333 && latitude > -0.3333)
			{
				wind.X = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Cos(yaw));
				wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Sin(yaw + Math.PI * hadleyCellHeight));
			}
			else if (latitude < 0.667 && latitude > -0.667)
			{
				wind.X = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Cos(yaw));
				wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * Math.Sin(yaw + Math.PI * hadleyCellHeight));
			}
			else
			{
				wind.X = (float)(Math.Abs(Math.Sin(pitch)) * Math.Cos(yaw));
				wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * Math.Sin(yaw + Math.PI * hadleyCellHeight));
			}
			wind.Z = (float)Math.Cos(pitch);
			if (wind.Z > 0)
			{
				wind.Z *= 1.0f - hadleyCellHeight;
			}
			else
			{
				wind.Z *= hadleyCellHeight;
			}
			wind *= tradeWindSpeed;


			const float windElevationFactor = 1.0f / 2000;
			float maxWindFrictionElevation = 1000;
			float friction = Math.Max(0.0f, (1.0f - (windElevation - landElevation) / maxWindFrictionElevation));
			Vector3 up = new Vector3(0, 0, 1);
			friction = friction * friction * (1.0f - normal.Z*0.75f);


			float coriolisPower = (float)Math.Sqrt(Math.Abs(latitude)) * (1.0f - friction);
			int coriolisDir = latitude > 0 ? 1 : -1;
			Vector3 pWind = Vector3.Transform(state.Wind[index], Matrix.CreateRotationZ((float)Math.PI / 2 * coriolisDir * coriolisPower));
			wind += pWind;

			// TODO: Should I be simulating pressure differentials at the tropopause to distribute heat at the upper atmosphere?

			float altitudeSpeedMultiplier = 1.0f + (float)Math.Pow(Math.Min(1.0f, windElevation * windElevationFactor), 2) * (1.0f - friction);
			wind *= altitudeSpeedMultiplier;

			return wind;
		}

		public float GetPressureAtElevation(State state, int index, float elevation, float temperatureDifferential)
		{
			// Units: Pascals
			// Barometric Formula
			// Pressure = StaticPressure * (StdTemp / (StdTemp + StdTempLapseRate * (Elevation - ElevationAtBottomOfAtmLayer)) ^ (GravitationalAcceleration * MolarMassOfEarthAir / (UniversalGasConstant * StdTempLapseRate))
			// https://en.wikipedia.org/wiki/Barometric_formula
			// For the bottom layer of atmosphere ( < 11000 meters), ElevationAtBottomOfAtmLayer == 0)

			float standardPressure = StaticPressure * (float)Math.Pow(StdTemp / (StdTemp + StdTempLapseRate * elevation), PressureExponent);

			float pressure = standardPressure - verticalWindPressureAdjustment * state.Wind[index].Z + temperatureGradientPressureAdjustment * temperatureDifferential;
			return pressure;
		}


		Vector3 GetSunAngle(int ticks, float latitude)
		{

			float angleOfInclination = planetTiltAngle * (float)Math.Sin(Math.PI * 2 * (GetTimeOfYear(ticks) - 0.25f));
			//float timeOfDay = (-sunPhase + 0.5f) * Math.PI * 2;
			float timeOfDay = (float)0;
			float azimuth = (float)Math.Atan2(Math.Sin(timeOfDay), Math.Cos(timeOfDay) * Math.Sin(latitude * Math.PI) - Math.Tan(angleOfInclination) * Math.Cos(latitude * Math.PI));
			float elevation = (float)Math.Asin((Math.Sin(latitude) * Math.Sin(angleOfInclination) + Math.Cos(latitude) * Math.Cos(angleOfInclination) * Math.Cos(timeOfDay)));

			float cosOfElevation = (float)Math.Cos(elevation);
			Vector3 sunVec = new Vector3((float)Math.Sin(azimuth) * cosOfElevation, (float)Math.Cos(azimuth) * cosOfElevation, (float)Math.Sin(elevation));
			return sunVec;
		}



		private static void MoveHumidityToClouds(float elevation, float humidity, float cloudElevation, Vector3 windAtSurface, ref float newHumidity, ref float newCloudCover)
		{
			float humidityToCloud = MathHelper.Clamp((humidityCloudAbsorptionRate + windAtSurface.Z * humidityToCloudWindSpeed) * humidity / Math.Max(1.0f, cloudElevation - elevation), 0, humidity);
			newHumidity -= humidityToCloud;
			newCloudCover += humidityToCloud;
		}

		private void UpdateCloudElevation(float elevationOrSeaLevel, float temperature, float humidity, float atmosphereMass, Vector3 windAtCloudElevation, ref float newCloudElevation)
		{
			float dewPointTemp = (float)Math.Pow(humidity / (dewPointRange * atmosphereMass), 0.25f) * dewPointTemperatureRange + dewPointZero;
			float dewPointElevation = Math.Max(0, (dewPointTemp - temperature) / temperatureLapseRate) + elevationOrSeaLevel;

			float desiredDeltaZ = dewPointElevation - newCloudElevation;
			newCloudElevation = newCloudElevation + desiredDeltaZ* cloudElevationDeltaSpeed + windAtCloudElevation.Z * windVerticalCloudSpeedMultiplier;
			newCloudElevation = Math.Max(newCloudElevation, elevationOrSeaLevel);
		}

		private void MoveClouds(State nextState, int x, int y, Vector3 windAtCloudElevation, float cloudCover, ref float newCloudCover)
		{
			if (cloudCover > 0)
			{
				if (windAtCloudElevation.X != 0 || windAtCloudElevation.Y != 0)
				{
					float windXPercent = Math.Abs(windAtCloudElevation.X) / (Math.Abs(windAtCloudElevation.X) + Math.Abs(windAtCloudElevation.Y));
					float cloudMove = Math.Min(cloudCover, Math.Abs(windAtCloudElevation.X) + Math.Abs(windAtCloudElevation.Y)) * cloudMovementFromWind;
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


			}
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

		private void FlowWater(State state, int x, int y, Vector2 gradient, float soilFertility, ref float surfaceWater, ref float groundWater)
		{
			float flow = Math.Min(surfaceWater, (Math.Abs(gradient.X) + Math.Abs(gradient.Y)));
			surfaceWater = Math.Max(surfaceWater - flow * FlowSpeed, 0);
			groundWater = Math.Max(groundWater - GroundWaterFlowSpeed * soilFertility, 0);


			for (int i = 0; i < 4; i++)
			{
				var neighborPoint = GetNeighbor(x, y, i);
				int neighborIndex = GetIndex(neighborPoint.X, neighborPoint.Y);
				float nWater = state.SurfaceWater[neighborIndex];
				float nGroundWater = state.GroundWater[neighborIndex];
				if (nWater > 0)
				{
					var nGradient = state.FlowDirection[neighborIndex];
					var nGroundFlow = GroundWaterFlowSpeed * state.SoilFertility[neighborIndex];
					switch (i)
					{
						case 0:
							if (nGradient.X > 0)
							{
								surfaceWater += nGradient.X * nWater * FlowSpeed;
								groundWater += nGroundWater * nGroundFlow;
							}
							break;
						case 1:
							if (nGradient.X < 0)
							{
								surfaceWater += nGradient.X * nWater * FlowSpeed;
							}
							break;
						case 2:
							if (nGradient.Y < 0)
							{
								surfaceWater += nGradient.X * nWater * FlowSpeed;
							}
							break;
						case 3:
							if (nGradient.Y > 0)
							{
								surfaceWater += nGradient.X * nWater * FlowSpeed;
							}
							break;
					}
				}
			}
			
			
		}

		private void SimulateIce(float elevation, float seaLevel, float localTemperature, ref float surfaceWater, ref float surfaceIce)
		{
			if (localTemperature <= FreezingTemperature)
			{
				float frozen = iceFreezeRate * (FreezingTemperature - localTemperature) * (1.0f - (float)Math.Pow(Math.Min(1.0f, surfaceIce / maxIce), 2));
				if (elevation > seaLevel)
				{
					frozen = Math.Min(frozen, surfaceWater);
					surfaceWater -= frozen;
				}
				surfaceIce += frozen;
			} else if (surfaceIce > 0)
			{
				float meltRate = (localTemperature - FreezingTemperature) * iceMeltRate;
				float melted = Math.Min(surfaceIce, meltRate);
				surfaceIce -= melted;
				if (elevation > seaLevel)
				{
					surfaceWater += melted;
				}
			}
		}
		private void SeepWaterIntoGround(float elevation, float seaLevel, float soilFertility, float waterTableDepth, ref float groundWater, ref float surfaceWater)
		{
			float maxGroundWater = soilFertility * waterTableDepth * MaxSoilPorousness;
			if (elevation > seaLevel)
			{
				float seepage = Math.Min(surfaceWater * soilFertility * GroundWaterReplenishmentSpeed, maxGroundWater - groundWater);
				groundWater += seepage;
				surfaceWater -= seepage;
			}
			else
			{
				groundWater = maxGroundWater;
				surfaceWater = 0;
			}
		}

		private float GetEvaporationRate(float ice, float localTemperature, float humidity, float atmosphereMass, float windSpeedAtSurface, float airPressureInverse, float sunAngle)
		{
			if (ice > 0)
			{
				return 0;
			}
			float evapTemperature = 1.0f - MathHelper.Clamp((localTemperature - evapMinTemperature) / evapTemperatureRange, 0, 1);
			float evapRate = EvapRateTemperature * (1.0f - evapTemperature * evapTemperature);
			evapRate += EvapRateWind * windSpeedAtSurface;
			evapRate *= airPressureInverse * MathHelper.Clamp(1.0f - (humidity * airPressureInverse / (dewPointRange * atmosphereMass)), 0, 1);
			return evapRate;
		}

		private void EvaporateWater(float evapRate, float elevation, float seaLevel, float groundWater, float waterTableDepth, ref float humidity, ref float temperature, ref float newGroundWater, ref float surfaceWater, out float evaporation)
		{
			evaporation = 0;
			if (evapRate <= 0)
			{
				return;
			}
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
				var groundWaterEvap = Math.Max(0, Math.Min(newGroundWater, groundWater / waterTableDepth * evapRate));
				newGroundWater -= groundWaterEvap;
				humidity += groundWaterEvap;
				evaporation += groundWaterEvap;
			}

			temperature -= evaporation * EvaporativeCoolingRate;
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
				newHumidity -= humidity * Math.Min(1.0f, (Math.Abs(windAtSurface.X) + Math.Abs(windAtSurface.Y)) * humidityLossFromWind);
			}
			for (int i = 0; i < 4; i++)
			{
				var neighbor = GetNeighbor(x, y, i);
				int nIndex = GetIndex(neighbor.X, neighbor.Y);
				var neighborWind = state.Wind[nIndex];
				switch (i)
				{
					case 0:
						if (neighborWind.X > 0)
						{
							float absX = Math.Abs(neighborWind.X);
							newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absX * temperatureLossFromWind);
							newHumidity += state.Humidity[nIndex] * absX * humidityLossFromWind;
						}
						break;
					case 1:
						if (neighborWind.X < 0)
						{
							float absX = Math.Abs(neighborWind.X);
							newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absX * temperatureLossFromWind);
							newHumidity += state.Humidity[nIndex] * absX * humidityLossFromWind;
						}
						break;
					case 2:
						if (neighborWind.Y < 0)
						{
							float absY = Math.Abs(neighborWind.Y);
							newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absY * temperatureLossFromWind);
							newHumidity += state.Humidity[nIndex] * absY * humidityLossFromWind;
						}
						break;
					case 3:
						if (neighborWind.Y > 0)
						{
							float absY = Math.Abs(neighborWind.Y);
							newTemperature += (state.Temperature[nIndex] - temperature) * Math.Min(1.0f, absY * temperatureLossFromWind);
							newHumidity += state.Humidity[nIndex] * absY * humidityLossFromWind;
						}
						break;
				}				
			}
		}

		private void UpdateTemperature(float seaLevel, float elevation, float ice, float cloudCover, float temperature, Vector3 terrainNormal, float humidity, float atmosphereMass, float sunAngle, Vector3 sunVector, float airPressureInverse, ref float newTemperature)
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
				if (ice > 0)
				{
					reflection = heatReflectionIce;
				}
				else if (elevation <= seaLevel) // ocean
				{
					reflection = heatReflectionWater;
				}
				else // land
				{
					slope = Math.Max(0, Vector3.Dot(terrainNormal, sunVector));
					// reflection = mineralTypes[cells[i, j].mineral].heatReflection;
					reflection = HeatReflectionLand;
				}
				float sunGain = slope * heatGainFromSun - cloudGain - cloudReflection;
				gain += sunGain * (1.0f - reflection) * (1.0f - humidityPercentage);

				// trap some heat in
				gain += cloudMass * cloudReflectionFactor * loss;
			}

			newTemperature += gain - loss;
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

		private void UpdateFlowDirectionAndNormal(State state, State nextState, int x, int y, int index, float elevation, out Vector2 flowDirection, out Vector3 normal)
		{
			if (elevation <= state.SeaLevel)
			{
				flowDirection = Vector2.Zero;
				normal = new Vector3(0, 0, 1);
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

				flowDirection = new Vector2(Math.Sign(g.X) * (1.0f +(float)Math.Pow(Math.Abs(g.X) / tileSize, FlowSpeedExponent)), Math.Sign(g.Y) * (1.0f + (float)Math.Pow(Math.Abs(g.X) / tileSize, FlowSpeedExponent)));

				// TODO: this is wong, gradient is just steepest downhill direction
				normal = Vector3.Normalize(new Vector3(g.X, g.Y, tileSize));

			}
		}

		private float UpdateBarometricPressure(State state, int x, int y, int index, float elevation, float temperature)
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
			Vector3 nWind = Vector3.Zero;
			for (int i = 0; i < 4; i++)
			{
				var neighbor = GetNeighbor(x, y, i);
				int nIndex = GetIndex(neighbor.X, neighbor.Y);
				//var neighborWind = state.Wind[nIndex];
				//nWind += neighborWind;

				switch (i)
				{
					case 0:
						pressureDifferential.X += state.Pressure[nIndex] - pressure;
						break;
					case 1:
						pressureDifferential.X -= state.Pressure[nIndex] - pressure;
						break;
					case 2:
						pressureDifferential.Y -= state.Pressure[nIndex] - pressure;
						break;
					case 3:
						pressureDifferential.Y += state.Pressure[nIndex] - pressure;
						break;
				}
			}
			Vector3 newWind = new Vector3(pressureDifferential.X, pressureDifferential.Y, (pressureDifferential.X + pressureDifferential.Y) / 4) * pressureDifferentialWindSpeed;
			return newWind * ( 1.0f - windInertia) + nWind / 4 * windInertia;

		}

	}
}
