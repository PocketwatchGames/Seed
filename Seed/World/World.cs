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
		//		float MaxCloudElevation = ;

		public int Size;
		public float TimeTillTick = 0.00001f;
		public float TimeScale = 1.0f;
		public float TicksPerSecond = 1.0f;
		public const int StateCount = 4;
		public State[] States = new State[StateCount];
		const int ProbeCount = 3;
		public Probe[] Probes = new Probe[ProbeCount];
		public int CurStateIndex;
		public int LastRenderStateIndex;
		public int NextRenderStateIndex;
		public object DrawLock = new object();
		public object InputLock = new object();
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
			public float[] Humidity;
			public float[] Rainfall;
			public float[] Evaporation;
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
			public float[] Pressure;
			public Vector3[] Wind;
			public Vector2[] FlowDirection;
			public Vector3[] Normal;

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
				States[i].CloudElevation = new float[s];
				States[i].Temperature = new float[s];
				States[i].Humidity = new float[s];
				States[i].CloudCover = new float[s];
				States[i].WaterTableDepth = new float[s];
				States[i].GroundWater = new float[s];
				States[i].SurfaceWater = new float[s];
				States[i].Rainfall = new float[s];
				States[i].Evaporation = new float[s];
				States[i].WaterSalinity = new float[s];
				States[i].SurfaceIce = new float[s];
				States[i].SubmergedIce = new float[s];
				States[i].SoilFertility = new float[s];
				States[i].Canopy = new float[s];
				States[i].Wind = new Vector3[s];
				States[i].Pressure = new float[s];
				States[i].FlowDirection = new Vector2[s];
				States[i].Normal = new Vector3[s];

				States[i].Population = new float[s * MaxSpecies];
				States[i].Species = new SpeciesType[MaxSpecies];
				States[i].SpeciesStats = new SpeciesStat[MaxSpecies];
			}

			for (int i = 0; i < ProbeCount; i++)
			{
				Probes[i] = new Probe();
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
				lock (InputLock)
				{
					while (TimeTillTick <= 0)
					{
						TimeTillTick += TicksPerSecond;
						int nextStateIndex = (CurStateIndex+1) % StateCount;
						while (nextStateIndex == LastRenderStateIndex || nextStateIndex == NextRenderStateIndex)
						{
							nextStateIndex = (nextStateIndex + 1) % StateCount;
						}

						Tick(States[CurStateIndex], States[nextStateIndex]);

						// TODO: why can't i edit this in the tick call?  it's a class, so it should be pass by reference?
						States[nextStateIndex].Ticks++;
						CurStateIndex = nextStateIndex;

					}
				}
				lock (DrawLock)
				{
					if (States[CurStateIndex].Ticks < States[NextRenderStateIndex].Ticks)
					{
						TimeTillTick = 0;
					}

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

		static public float GetTimeOfYear(int ticks)
		{
			float t = (float)ticks / TicksPerYear;
			return t - (int)t;
		}

		static public int GetYear(int ticks)
		{
			return ticks / TicksPerYear;
		}

		public float GetLatitude(int y)
		{
			return ((float)y / Size) * 2 - 1.0f;
		}
	}
}
