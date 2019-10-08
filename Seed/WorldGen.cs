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
			state.Species[0].RestingTemperature = 50;
			state.Species[0].TemperatureRange = 5000;
			state.Species[0].Color = new Color(100, 60, 20);

			state.Species[1].Name = "Basic Beast";
			state.Species[1].Food = SpeciesType.FoodType.Herbivore;
			state.Species[1].RestingTemperature = 35;
			state.Species[1].TemperatureRange = 3000;
			state.Species[1].Color = new Color(120, 100, 20);

			state.Species[2].Name = "Supacold";
			state.Species[2].Food = SpeciesType.FoodType.Herbivore;
			state.Species[2].RestingTemperature = 20;
			state.Species[2].TemperatureRange = 3000;
			state.Species[2].Color = new Color(60, 20, 100);

			for (int x = 0; x < Size; x++)
			{
				for (int y = 0; y < Size; y++)
				{
					int index = GetIndex(x, y);
					var e =
						GetPerlinMinMax(noise, x, y, 0.25f, 0, MinElevation, MaxElevation) * 0.4f +
						GetPerlinMinMax(noise, x, y, 0.5f, 10, MinElevation, MaxElevation) * 0.3f +
						GetPerlinMinMax(noise, x, y, 1.0f, 20, MinElevation, MaxElevation) * 0.2f +
						GetPerlinMinMax(noise, x, y, 2.0f, 30, MinElevation, MaxElevation) * 0.1f;
					state.Elevation[index] = e;
					float latitude = GetLatitude(y);
					state.Temperature[index] = (1.0f - MathHelper.Clamp(e - state.SeaLevel, 0, MaxElevation) / (MaxElevation - state.SeaLevel)) * (1.0f - latitude * latitude) * (MaxTemperature - MinTemperature) + MinTemperature;
					state.CloudCover[index] = GetPerlinNormalized(noise, x, y, 3.0f, 2000);
					if (e > 0)
					{
						state.SurfaceWater[index] = GetPerlinNormalized(noise, x, y, 1.0f, 100);
						state.WaterTableDepth[index] = GetPerlinMinMax(noise, x, y, 1.0f, 200, MinWaterTableDepth, MaxWaterTableDepth);
						state.GroundWater[index] = GetPerlinNormalized(noise, x, y, 1.0f, 300);
						state.SoilFertility[index] = GetPerlinNormalized(noise, x, y, 1.0f, 400);
						state.Canopy[index] = GetPerlinNormalized(noise, x, y, 2.0f, 1000);

						for (int s = 0; s < numSpecies; s++)
						{
							int speciesIndex = GetSpeciesIndex(x, y, s);
							state.Population[speciesIndex] = (short)Math.Max(0, GetPerlinMinMax(noise, x, y, 1.0f, 10000 + 1000 * s, -speciesMaxPopulation, speciesMaxPopulation));
						}

					}
				}
			}
			for (int x = 0; x < Size; x++)
			{
				for (int y = 0; y < Size; y++)
				{
					int index = GetIndex(x, y);
					float e = state.Elevation[index];
					float west = state.Elevation[GetIndex(WrapX(x - 1), y)];
					float east = state.Elevation[GetIndex(WrapX(x + 1), y)];
					float north = state.Elevation[GetIndex(x, WrapY(y - 1))];
					float south = state.Elevation[GetIndex(x, WrapY(y + 1))];
					if (west < e && west < east && west < north && west < south)
					{
						state.Gradient[index] = new Vector2(west - e, 0);
					}
					else if (east < e && east < west && east < north && east < south)
					{
						state.Gradient[index] = new Vector2(e - east, 0);
					}
					else if (north < e && north < west && north < east && north < south)
					{
						state.Gradient[index] = new Vector2(0, north - e);
					}
					else if (south < e && south < west && south < north && south < east)
					{
						state.Gradient[index] = new Vector2(0, e - south);
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
