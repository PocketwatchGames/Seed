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
		public const int TicksPerYear = 360;
		public const float SecondsPerTick = 365 * 24 * 60 * 60 / (float)TicksPerYear;
		public const int MaxSpecies = 4;

		public const float tileSize = 400000;

		public float canopyGrowthRate = 100.0f / TicksPerYear;
		public float canopyDeathRate = 0.2f / TicksPerYear;
		public float speciesGrowthRate = 1.0f / TicksPerYear;
		public float speciesDeathRate = 0.025f / TicksPerYear;
		public float speciesMaxPopulation = 10000;
		public float speciesEatRate = 1.0f / TicksPerYear;
		public float freshWaterMaxAvailability = 1.0f / 10.0f;
		public float populationExpansionPercent = 0.2f;
		public float minPopulationDensityForExpansion = 0.1f;
		public float starvationSpeed = 12.0f / TicksPerYear;
		public float dehydrationSpeed = 12.0f / TicksPerYear;
		public float carbonDioxide = 0.001f;

		public const float MaxElevation = 10000.0f;
		public const float MinElevation = -10000.0f;
		public const float EvapRateWind = 2.0f / TicksPerYear;
		public const float EvapRateTemperature = 5.0f / TicksPerYear;
		public const float tradeWindSpeed = 3.0f / TicksPerYear;
		public const float pressureDifferentialWindSpeed = 0.0005f / TicksPerYear;
		public const float RainfallRate = 10.0f / TicksPerYear;
		public const float FlowSpeed = 0.01f / TicksPerYear;
		public const float MaxWaterTableDepth = 100.0f;
		public const float MinWaterTableDepth = 10.0f;
		public const float MaxSoilPorousness = 0.1f;
		public const float SoilOsmosisSpeed = 10.0f / TicksPerYear;
		public const float FreezingTemperature = 273.15f;
		public const float MinTemperature = FreezingTemperature - 50;
		public const float MaxTemperature = FreezingTemperature + 50;
		public float planetTiltAngle = MathHelper.ToRadians(-23.5f);
		public const float evapMinTemperature = 253; // -20 celsius
		public const float evapMaxTemperature = 373; // 140 celsius
		public const float evapTemperatureRange = evapMaxTemperature - evapMinTemperature;
		public const float localSunHeat = 5; // sun can add about 10 degrees farenheit


		public float heatLoss = 0.0002f; // how fast a cell loses heat an min elevation, no cloud cover
		public float heatGainFromSun = 25.0f / TicksPerYear; // how fast a cell gains heat with no cloud cover, modified by sun height
		public float heatReflectionWater = 0.0f; // How much heat is reflected back by the water
		public float heatReflectionIce = 0.5f; // How much heat is reflected back by the water
		public float HeatReflectionLand = 0.05f;
		public float heatLossPreventionCarbonDioxide = 200;
		public float cloudContentFullAbsorption = 5.0f; // how much heat gain/loss is caused by cloud cover
		public float cloudAbsorptionRate = 0.06f; // 6% absorbed by clouds
		public float cloudReflectionRate = 0.20f; // 20% reflected back to space
		public float troposphereElevation = 10000;
		public float stratosphereElevation = 50000;
		public float troposphereAtmosphereContent = 0.8f;
		public float dewPointZero = 213.0f;
		public float dewPointTemperatureRange = 100.0f;
		public float dewPointRange = 6.0f;
		public float rainPointTemperatureMultiplier = 0.00075f; // adjustment for temperature
		public float temperatureLapseRate = -0.0065f;
		public float EvaporativeCoolingRate = 1.0f;
		public float temperatureLossFromWind = 1000.0f / TicksPerYear;
		public float humidityLossFromWind = 10.0f / TicksPerYear;
		public float windInertia = 0.1f;
		public const float StaticPressure = 101325;
		public const float StdTemp = 288.15f;
		public const float StdTempLapseRate = -0.0065f;
		public const float GravitationalAcceleration = 9.80665f;
		public const float MolarMassEarthAir = 0.0289644f;
		public const float UniversalGasConstant = 8.3144598f;
		public const float humidityCloudAbsorptionRate = 10000.0f / TicksPerYear;
		public const float humidityToCloudWindSpeed = 100000.0f / TicksPerYear;
		public const float upperAtmosphereCoolingRate = 100.0f / TicksPerYear;
		public const float MaxTropopauseElevation = 17000f;
		public const float MinTropopauseElevation = 9000f;
		public const float TropopauseElevationSeason = 1000f;

	}
}
