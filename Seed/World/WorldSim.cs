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

		public void Tick(State state, State nextState)
		{
			nextState.SpeciesStats = new SpeciesStat[MaxSpecies];

			List<Task> simTasks = new List<Task>();
			simTasks.Add(Task.Run(() =>
			{
				TickEarth(state, nextState);
			}));
			simTasks.Add(Task.Run(() =>
			{
				TickBarometricPressure(state, nextState);
			}));
			simTasks.Add(Task.Run(() =>
			{
				TickWind(state, nextState);
			}));
			simTasks.Add(Task.Run(() =>
			{
				TickAtmosphere(state, nextState);
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