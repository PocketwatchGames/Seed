using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seed
{
	public partial class World
	{
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
						float groundWaterSaturation = GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], sf * Data.MaxSoilPorousness);
						float surfaceWater = state.SurfaceWater[index];
						freshWaterAvailability = GetFreshWaterAvailability(surfaceWater, groundWaterSaturation);

						float desiredCanopy = sf * Math.Min(groundWaterSaturation + surfaceWater, 1.0f) * Math.Max(0, (t - Data.MinTemperature) / (Data.MaxTemperature - Data.MinTemperature));
						float canopyGrowth = (desiredCanopy - canopy) * Data.canopyGrowthRate;
						canopy += canopyGrowth;

						float expansion = canopy * canopyGrowth * 0.25f;
						for (int i = 0; i < 4; i++)
						{
							var n = GetNeighbor(x, y, i);
							int neighborIndex = GetIndex(n.X, n.Y);
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


							float populationPercentDied = Data.speciesDeathRate;
							populationPercentDied += GetTemperatureDeath(ref state, temperature, s);
							populationPercentDied += GetStarvation(populationDensity, canopy);
							populationPercentDied += GetDehydration(populationDensity, freshWaterAvailability);
							populationPercentDied = Math.Min(1.0f, populationPercentDied);

							float populationDelta = 1.0f + Data.speciesGrowthRate - populationPercentDied;
							if (populationDensity > Data.minPopulationDensityForExpansion)
							{
								float expansion = Data.speciesGrowthRate * Data.populationExpansionPercent * 0.25f;
								float neighborGrowthRate = Data.speciesMaxPopulation * populationDensity * expansion;
								for (int i = 0; i < 4; i++)
								{
									var n = GetNeighbor(x, y, i);
									if (state.Elevation[GetIndex(n.X, n.Y)] > state.SeaLevel)
									{
										int nIndex = GetSpeciesIndex(n.X, n.Y, s);
										nextState.Population[nIndex] += neighborGrowthRate;
										populationDelta -= expansion;
									}
								}
							}
							float newPopDensity = populationDensity * populationDelta;

							float newPopulation = newPopDensity * Data.speciesMaxPopulation;
							if (newPopulation < 1 && newPopulation < state.Population[speciesIndex])
							{
								newPopulation = 0;
							}
							nextState.SpeciesStats[s].Population += newPopulation;
							nextState.Population[speciesIndex] = newPopulation;
							canopyEaten += populationDensity * Data.speciesEatRate;
						}
					}
					nextState.Canopy[index] = Math.Max(0, canopy - canopyEaten);
				}
			}

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
			return (float)population / Data.speciesMaxPopulation;
		}
		public float GetStarvation(float populationDensity, float canopy)
		{
			return Math.Max(0, (populationDensity - canopy) * Data.starvationSpeed);
		}
		public float GetTemperatureDeath(ref State state, float temperature, int species)
		{
			return Math.Abs((temperature - state.Species[species].RestingTemperature) / state.Species[species].TemperatureRange);
		}
		public float GetDehydration(float populationDensity, float freshWaterAvailability)
		{
			return Math.Max(0, (populationDensity - freshWaterAvailability / Data.freshWaterMaxAvailability) * Data.dehydrationSpeed);
		}



	}
}
