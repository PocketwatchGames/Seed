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
		private Point GetNeighbor(int x, int y, int neighborIndex)
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
			return new Point(x, y);
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


		Vector3 GetWindAtElevation(State state, float windElevation, float landElevation, int index, float latitude, Vector3 normal)
		{
			float tropopauseElevation = (1.0f - Math.Abs(latitude)) * (MaxTropopauseElevation - MinTropopauseElevation) + MinTropopauseElevation + TropopauseElevationSeason * latitude * (GetTimeOfYear(state.Ticks) * 2 - 1);
			float hadleyCellHeight = Math.Min(1.0f, (windElevation - landElevation) / (tropopauseElevation - landElevation));

			Vector3 wind = Vector3.Zero;
			float pitch = (float)(latitude * Math.PI * 3f);
			if (latitude < 0.3333 && latitude > -0.3333)
			{
				float yaw = (float)(latitude * Math.PI * 1.5f);
				wind.X = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Cos(yaw));
				wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Sin(yaw + Math.PI * hadleyCellHeight));
				wind.Z = (float)Math.Cos(pitch) * (float)Math.Sqrt(Math.Sin(hadleyCellHeight * Math.PI));
			}
			else if (latitude < 0.667 && latitude > -0.667)
			{
				float yaw = (float)(latitude * Math.PI * 1.5f);
				wind.X = (float)(Math.Abs(Math.Sin(pitch)) * -Math.Cos(yaw));
				wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * Math.Sin(yaw + Math.PI * hadleyCellHeight));
				wind.Z = (float)Math.Cos(pitch) * (float)Math.Sqrt(Math.Sin(hadleyCellHeight * Math.PI));
			}
			else
			{
				float yaw = (float)(latitude * Math.PI * 1.5f);
				wind.X = (float)(Math.Abs(Math.Sin(pitch)) * Math.Cos(yaw));
				wind.Y = (float)(Math.Abs(Math.Sin(pitch)) * Math.Sin(yaw + Math.PI * hadleyCellHeight));
				wind.Z = (float)Math.Cos(pitch) * (float)Math.Sqrt(Math.Sin(hadleyCellHeight * Math.PI));
			}
			wind *= tradeWindSpeed;

			// TODO: Should I be simulating pressure differentials at the tropopause to distribute heat at the upper atmosphere?
			wind += state.Wind[index];

			float windElevationFactor = 1.0f / 2000;
			float maxWindFrictionElevation = 1000;
			float friction = Math.Max(0.0f, (1.0f - (windElevation - landElevation) / maxWindFrictionElevation));
			Vector3 up = new Vector3(0, 0, 1);
			friction = friction * friction * (1.0f - Vector3.Dot(normal, up));
			float altitudeSpeedMultiplier = 1.0f + (float)Math.Pow(Math.Min(1.0f, windElevation * windElevationFactor), 2) * (1.0f - friction);
			wind *= altitudeSpeedMultiplier;

			return wind;
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
			simTasks.Add(Task.Run(() =>
			{
				for (int i = 0; i < ProbeCount; i++)
				{
					Probes[i].Update(this, state);
				}
			}));
			Task.WaitAll(simTasks.ToArray());
		}

	}
}