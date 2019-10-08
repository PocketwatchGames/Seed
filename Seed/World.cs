using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Seed
{
	public struct SpeciesType
	{
		public enum FoodType
		{
			Herbivore,
			Omnivore,
			Carnivore
		}
		public String Name;
		public FoodType Food;
		public Color Color;
		public float RestingTemperature;
		public float TemperatureRange;
	}

	public struct SpeciesStat
	{
		public float Population;
	}
	public partial class World
	{
		public const int TicksPerYear = 360;
		public const int MaxSpecies = 4;

		public float canopyGrowthRate = 2.0f / TicksPerYear;
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

		const float MaxElevation = 10.0f;
		const float MinElevation = -10.0f;
		const float MinTemperature = -50;
		const float MaxTemperature = 50;
		const float EvapRate =  0.0001f / TicksPerYear;
		const float tradeWindSpeed = 1.0f / TicksPerYear;
		const float RainfallRate = 1.0f / TicksPerYear;
		const float FlowSpeed = 1.0f / TicksPerYear;
		const float MaxWaterTableDepth = 2.0f;
		const float MinWaterTableDepth = 0.2f;
		const float MaxGroundWater = MaxWaterTableDepth;
//		float MaxCloudElevation = ;

		public int Size;
		public float TimeTillTick = 0.00001f;
		public float TimeScale = 1.0f;
		public float TicksPerSecond = 1.0f;
		public const int StateCount = 4;
		public State[] States = new State[StateCount];
		public int CurStateIndex;
		public int LastRenderStateIndex;
		public int NextRenderStateIndex;
		public object DrawLock = new object();
		public class State : ICloneable
		{
			public int Ticks;
			public float AtmosphereCO2;
			public float AtmosphereO2;
			public float AtmosphereN;
			public float GlobalTemperature;
			public float SeaLevel;

			public SpeciesType[] Species;
			public SpeciesStat[] SpeciesStats;

			public float[] Elevation;
			public float[] Temperature;
			public float[] CloudCover;
			public float[] CloudElevation;
			public float[] WaterTableDepth;
			public float[] GroundWater;
			public float[] SurfaceWater;
			public float[] WaterSalinity;
			public float[] SurfaceIce;
			public float[] SubmergedIce;
			public float[] SoilFertility;
			public float[] Canopy;
			public float[] Population;
			public Vector2[] Wind;
			public Vector2[] Gradient;

			public object Clone()
			{
				return MemberwiseClone();
			}
		}

		public void Init(int size)
		{
			Size = size;
			int s = Size * Size;

			for (int i = 0; i < StateCount; i++)
			{
				States[i] = new State();
				States[i].Elevation = new float[s];
				States[i].Temperature = new float[s];
				States[i].CloudCover = new float[s];
				States[i].WaterTableDepth = new float[s];
				States[i].GroundWater = new float[s];
				States[i].SurfaceWater = new float[s];
				States[i].WaterSalinity = new float[s];
				States[i].SurfaceIce = new float[s];
				States[i].SubmergedIce = new float[s];
				States[i].SoilFertility = new float[s];
				States[i].Canopy = new float[s];
				States[i].Wind = new Vector2[s];
				States[i].Gradient = new Vector2[s];

				States[i].Population = new float[s * MaxSpecies];
				States[i].Species = new SpeciesType[MaxSpecies];
				States[i].SpeciesStats = new SpeciesStat[MaxSpecies];
			}
		}

		Task _simTask;
		public void Update(GameTime gameTime)
		{
			TimeTillTick -= TimeScale * (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;

			if (_simTask != null)
			{
				_simTask.Wait();
			}

			_simTask = Task.Run(() =>
			{
				while (TimeTillTick <= 0)
				{
					TimeTillTick += TicksPerSecond;
					int nextStateIndex = CurStateIndex;
					while (nextStateIndex == LastRenderStateIndex || nextStateIndex == NextRenderStateIndex)
					{
						nextStateIndex = (nextStateIndex + 1) % StateCount;
					}

					Tick(States[CurStateIndex], States[nextStateIndex]);
					CurStateIndex = nextStateIndex;
				}
				lock (DrawLock)
				{
					LastRenderStateIndex = NextRenderStateIndex;
					NextRenderStateIndex = CurStateIndex;
				}
			});
		}

		public int GetIndex(int x, int y)
		{
			return y * Size + x;
		}
		public int GetSpeciesIndex(int x, int y, int s)
		{
			return y * Size + x + s * Size*Size;
		}

		public float GetTimeOfYear(ref State state)
		{
			float t = (float)state.Ticks / TicksPerYear;
			return t - (int)t;
		}

		public int GetYear(ref State state)
		{
			return state.Ticks / TicksPerYear;
		}

		public float GetLatitude(int y)
		{
			return ((float)y / Size) * 2 - 1.0f;
		}

	}
}
