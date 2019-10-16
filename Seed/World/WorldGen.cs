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

		private float GetPerlinMinMax(FastNoise noise, int x, int y, float frequency, float hash, float min, float max)
		{
			return (noise.GetPerlin((float)x / Size * frequency + hash, (float)y / Size * frequency) + 1.0f) * (max - min) / 2 + min;
		}
		private float GetPerlinNormalized(FastNoise noise, int x, int y, float frequency, float hash)
		{
			return (noise.GetPerlin((float)x / Size * frequency + hash, (float)y / Size * frequency) + 1.0f) / 2;
		}
		private float GetPerlin(FastNoise noise, int x, int y, float frequency, float hash)
		{
			return noise.GetPerlin((float)x / Size * frequency + hash, (float)y / Size * frequency);
		}
		public void Generate()
		{
			ref var state = ref States[0];
			FastNoise noise = new FastNoise(67687);
			noise.SetFrequency(10);
			state.SeaLevel = 0;

			int numSpecies = 3;
			state.Species[0].Name = "Hot Herb";
			state.Species[0].Food = SpeciesType.FoodType.Herbivore;
			state.Species[0].RestingTemperature = Data.FreezingTemperature + 50;
			state.Species[0].TemperatureRange = 5000;
			state.Species[0].Color = new Color(100, 60, 20);

			state.Species[1].Name = "Basic Beast";
			state.Species[1].Food = SpeciesType.FoodType.Herbivore;
			state.Species[1].RestingTemperature = Data.FreezingTemperature + 35;
			state.Species[1].TemperatureRange = 3000;
			state.Species[1].Color = new Color(120, 100, 20);

			state.Species[2].Name = "Supacold";
			state.Species[2].Food = SpeciesType.FoodType.Herbivore;
			state.Species[2].RestingTemperature = Data.FreezingTemperature + 20;
			state.Species[2].TemperatureRange = 3000;
			state.Species[2].Color = new Color(60, 20, 100);

			for (int x = 0; x < Size; x++)
			{
				for (int y = 0; y < Size; y++)
				{
					int index = GetIndex(x, y);
					var e =
						GetPerlinMinMax(noise, x, y, 0.25f, 0, Data.MinElevation, Data.MaxElevation) * 0.4f +
						GetPerlinMinMax(noise, x, y, 0.5f, 10, Data.MinElevation, Data.MaxElevation) * 0.3f +
						GetPerlinMinMax(noise, x, y, 1.0f, 20, Data.MinElevation, Data.MaxElevation) * 0.2f +
						GetPerlinMinMax(noise, x, y, 2.0f, 30, Data.MinElevation, Data.MaxElevation) * 0.1f;
					state.Elevation[index] = e;
					float latitude = GetLatitude(y);
					state.Temperature[index] = (1.0f - MathHelper.Clamp(e - state.SeaLevel, 0, Data.MaxElevation) / (Data.MaxElevation - state.SeaLevel)) * (1.0f - latitude * latitude) * (Data.MaxTemperature - Data.MinTemperature) + Data.MinTemperature;
					state.CloudCover[index] = GetPerlinMinMax(noise, x, y, 3.0f, 2000, 0, 2);
					state.Humidity[index] = GetPerlinMinMax(noise, x, y, 3.0f, 3000, 0, 2);
					state.CloudElevation[index] = state.Elevation[index] + 1000;
					state.WaterTableDepth[index] = GetPerlinMinMax(noise, x, y, 1.0f, 200, Data.MinWaterTableDepth, Data.MaxWaterTableDepth);
					state.SoilFertility[index] = GetPerlinNormalized(noise, x, y, 1.0f, 400);
					state.Pressure[index] = GetPressureAtElevation(state, index, Math.Max(state.SeaLevel, e), 0);
					if (e > 0)
					{
						state.SurfaceWater[index] = GetPerlinMinMax(noise, x, y, 1.0f, 100, 0, 10.0f);
						state.GroundWater[index] = GetPerlinMinMax(noise, x, y, 1.0f, 300, 0, state.WaterTableDepth[index] * state.SoilFertility[index] * Data.MaxSoilPorousness);
						state.Canopy[index] = GetPerlinNormalized(noise, x, y, 2.0f, 1000);

						for (int s = 0; s < numSpecies; s++)
						{
							int speciesIndex = GetSpeciesIndex(x, y, s);
							state.Population[speciesIndex] = (short)Math.Max(0, GetPerlinMinMax(noise, x, y, 1.0f, 10000 + 1000 * s, -Data.speciesMaxPopulation, Data.speciesMaxPopulation));
						}

					}
				}
			}

			for (int i=1;i<StateCount;i++)
			{
				States[i] = (State)States[0].Clone();
			}
		}
	}
}
