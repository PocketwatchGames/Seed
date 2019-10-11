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
		public void TickEarth(State state, State nextState)
		{
			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);
					float elevation = state.Elevation[index];
					Vector2 newFlowDirection;
					Vector3 newNormal;
					UpdateFlowDirectionAndNormal(state, nextState, x, y, index, elevation, out newFlowDirection, out newNormal);
					nextState.FlowDirection[index] = newFlowDirection;
					nextState.Normal[index] = newNormal;
				}
			}
		}

	}
}
