﻿using System;
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

					float newCanopy = canopy;
					if (canopy > 0)
					{
						float t = state.Temperature[index];
						float sf = state.SoilFertility[index];
						float groundWaterSaturation = GetGroundWaterSaturation(state.GroundWater[index], state.WaterTableDepth[index], sf * Data.MaxSoilPorousness);
						float surfaceWater = state.SurfaceWater[index];
						freshWaterAvailability = GetFreshWaterAvailability(surfaceWater, groundWaterSaturation);

						float desiredCanopy = sf * Math.Min(groundWaterSaturation + surfaceWater, 1.0f) * Math.Max(0, (t - Data.MinTemperature) / (Data.MaxTemperature - Data.MinTemperature));
						float canopyGrowth = (desiredCanopy - canopy) * Data.canopyGrowthRate;
						newCanopy += canopyGrowth;

						//float expansion = canopy * canopyGrowth * 0.25f;
						//for (int i = 0; i < 4; i++)
						//{
						//	var n = GetNeighbor(x, y, i);
						//	int neighborIndex = GetIndex(n.X, n.Y);
						//	if (state.Elevation[neighborIndex] > state.SeaLevel)
						//	{
						//		nextState.Canopy[neighborIndex] = Math.Min(1.0f, nextState.Canopy[neighborIndex] + expansion);
						//	}
						//}
						nextState.Canopy[index] = Math.Max(0, newCanopy);

					}
				}
			}

			for (int i = 0;i<MaxAnimals;i++)
			{
				float population = state.Animals[i].Population;
				if (population > 0)
				{
					int tileIndex = GetIndex((int)state.Animals[i].Position.X, (int)state.Animals[i].Position.Y);
					var species = state.Species[state.Animals[i].Species];

					float newPopulation = population;
					float populationDensity = population / species.speciesMaxPopulation;

					float babiesBorn = population * species.speciesGrowthRate;
					newPopulation += babiesBorn;

					float diedOfOldAge = Math.Max(0, population * 1.0f / (species.Lifespan * Data.TicksPerYear));
					newPopulation -= diedOfOldAge;

					float diedFromTemperature = population * Math.Abs((state.Temperature[tileIndex] - species.RestingTemperature) / species.TemperatureRange);
					newPopulation -= diedFromTemperature;

					float freshWaterAvailability = GetFreshWaterAvailability(state.SurfaceWater[tileIndex], GetGroundWaterSaturation(state.GroundWater[tileIndex], state.WaterTableDepth[tileIndex], state.SoilFertility[tileIndex] * Data.MaxSoilPorousness));
					float diedOfDehydration = Math.Max(0, population * (populationDensity - freshWaterAvailability / Data.freshWaterMaxAvailability) * species.dehydrationSpeed);
					newPopulation -= diedOfDehydration;

					if (species.Food == SpeciesType.FoodType.Herbivore)
					{
						float canopy = nextState.Canopy[tileIndex];
						float diedOfStarvation = Math.Max(0, population * (populationDensity - canopy) * species.starvationSpeed);
						newPopulation -= diedOfStarvation;
						canopy -= population * species.speciesEatRate;
						nextState.Canopy[tileIndex] = canopy;
					} else
					{
						float availableMeat = 0;
						for (int m = 0; m < MaxGroupsPerTile; m++)
						{
							int meatGroupIndex = state.AnimalsPerTile[tileIndex * MaxGroupsPerTile + m];
							if (meatGroupIndex >= 0 && state.Species[state.Animals[meatGroupIndex].Species].Food == SpeciesType.FoodType.Herbivore)
							{
								availableMeat += state.Animals[meatGroupIndex].Population;
							}
						}
						float diedOfStarvation = Math.Max(0, population * (population - availableMeat) * species.starvationSpeed);
						newPopulation -= diedOfStarvation;

						float meatEaten = Math.Min(availableMeat, population * species.speciesEatRate);
						for (int m = 0; m < MaxGroupsPerTile; m++)
						{
							int meatGroupIndex = state.AnimalsPerTile[tileIndex * MaxGroupsPerTile + m];
							if (meatGroupIndex >= 0 && state.Species[state.Animals[meatGroupIndex].Species].Food == SpeciesType.FoodType.Herbivore)
							{
								float meatPop = nextState.Animals[meatGroupIndex].Population;
								nextState.Animals[meatGroupIndex].Population = Math.Max(0, meatPop - meatEaten * meatPop / availableMeat);
							}
						}

					}


					nextState.Animals[i].Population = Math.Max(0, newPopulation);
					if (newPopulation <= 0)
					{
						for (int j=0;j<MaxGroupsPerTile;j++)
						{
							int groupIndex = tileIndex * MaxGroupsPerTile + j;
							if (nextState.AnimalsPerTile[groupIndex] == i)
							{
								nextState.AnimalsPerTile[groupIndex] = -1;
							}
						}
					}
				}
			}
			//		// ANIMALS
			//		for (int s = 0; s < MaxSpecies; s++)
			//		{
			//			int speciesIndex = GetSpeciesIndex(x, y, s);
			//			float p = population[s];
			//			if (p > 0)
			//			{
			//				var species = state.Species[s];

			//				if (state.Species[s].Food == SpeciesType.FoodType.Herbivore)
			//				{
			//					float populationDensity = p / species.speciesMaxPopulation;
			//					float diedOfStarvation = Math.Max(0, p * (populationDensity - canopy) * species.starvationSpeed);
			//					population[s] -= diedOfStarvation;

			//					newCanopy -= population[s] * species.speciesEatRate;
			//				}
			//				else
			//				{
			//					float availableMeat = 0;
			//					for (int m = 0; m < MaxSpecies; m++)
			//					{
			//						if (state.Species[m].Food == SpeciesType.FoodType.Herbivore)
			//						{
			//							availableMeat += population[m];
			//						}
			//					}
			//					float diedOfStarvation = Math.Max(0, p * (p - availableMeat) * species.starvationSpeed);
			//					population[s] -= diedOfStarvation;

			//					float meatEaten = Math.Min(availableMeat, p * species.speciesEatRate);
			//					for (int m = 0; m < MaxSpecies; m++)
			//					{
			//						if (state.Species[m].Food == SpeciesType.FoodType.Herbivore)
			//						{
			//							population[m] -= meatEaten * (float)population[m] / availableMeat;
			//						}
			//					}
			//				}

			//			}
			//		}

			//		for (int s = 0; s < MaxSpecies; s++)
			//		{
			//			int speciesIndex = GetSpeciesIndex(x, y, s);
			//			var species = state.Species[s];
			//			if (population[s] < 1)
			//			{
			//				population[s] = 0;
			//			}
			//			nextState.Population[speciesIndex] = population[s];
			//			nextState.SpeciesStats[s].Population += population[s];
			//		}


			//	}
			//}
		}

		public float GetGroundWaterSaturation(float groundWater, float waterTableDepth, float soilPorousness)
		{
			return groundWater / (waterTableDepth * soilPorousness);
		}

		public float GetFreshWaterAvailability(float surfaceWater, float groundWaterSaturation)
		{
			return surfaceWater > 0 ? 1.0f : Math.Min(1.0f, groundWaterSaturation);
		}



	}
}
