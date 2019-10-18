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

		public void MovePlate(State state, State nextState, int plateIndex, Point direction)
		{
			// TODO: enforce conservation of mass

			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);
					if (state.Plate[index] == plateIndex) {
						Point newPoint = new Point(WrapX(x + direction.X), WrapY(y + direction.Y));
						int newIndex = GetIndex(newPoint.X, newPoint.Y);
						if (state.Plate[newIndex] == plateIndex)
						{
							MoveTile(state, nextState, index, newIndex);

							Point divergentPoint = new Point(WrapX(x - direction.X), WrapY(y - direction.Y));
							int divergentIndex = GetIndex(divergentPoint.X, divergentPoint.Y);
							if (state.Plate[divergentIndex] != plateIndex)
							{
								// divergent zone
//								if (state.Elevation[index] > state.Elevation[divergentIndex])
								{
									nextState.Plate[index] = state.Plate[divergentIndex];
								}
								nextState.Elevation[index] = (state.Elevation[index] + state.Elevation[divergentIndex]) / 2 - 100;
								nextState.WaterTableDepth[index] = (state.WaterTableDepth[index] + state.WaterTableDepth[divergentIndex]) / 2;
								nextState.SoilFertility[index] = (state.SoilFertility[index] + state.SoilFertility[divergentIndex]) / 2;
							}
						}
						else
						{
							float startElevation = state.Elevation[index];
							float endElevation = state.Elevation[newIndex];
							if (startElevation > state.SeaLevel && endElevation > state.SeaLevel)
							{
								// continental collision
								nextState.Elevation[newIndex] += 50;
								nextState.Elevation[index] += 50;
								nextState.Plate[newIndex] = plateIndex;
							}
							else
							{
								// subduction
								if (startElevation > state.SeaLevel)
								{
									// We are moving OVER the adjacent tile
									MoveTile(state, nextState, index, newIndex);
									Point subductionPoint = new Point(WrapX(newPoint.X + direction.X), WrapY(newPoint.Y + direction.Y));
									int subductionIndex = GetIndex(subductionPoint.X, subductionPoint.Y);
									nextState.Elevation[newIndex] = (state.Elevation[newIndex] + state.Elevation[subductionIndex]) / 2;
									nextState.Elevation[subductionIndex] -= 100;
								}
								else
								{
									// we are moving UNDER the adjacent tile
									nextState.Elevation[newIndex] = (state.Elevation[newIndex] + state.Elevation[index]) / 2;
									nextState.Elevation[index] -= 100;
								}
							}
						}
					}
				}
			}

			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					int index = GetIndex(x, y);
					float elevation = state.Elevation[index];
					Vector2 newFlowDirection;
					Vector3 newNormal;
					UpdateFlowDirectionAndNormal(nextState, nextState, x, y, index, elevation, out newFlowDirection, out newNormal);
					nextState.FlowDirection[index] = newFlowDirection;
					nextState.Normal[index] = newNormal;

					if (nextState.SurfaceWater[index] > 0 && nextState.Elevation[index] <= nextState.SeaLevel)
					{
						nextState.SurfaceWater[index] = 0;
					}
				}
			}

		}

		public void MoveTile(State state, State nextState, int index, int newIndex)
		{
			nextState.Plate[newIndex] = state.Plate[index];
			nextState.Elevation[newIndex] = state.Elevation[index];
			nextState.Canopy[newIndex] = state.Canopy[index];
			nextState.WaterTableDepth[newIndex] = state.WaterTableDepth[index];
			nextState.GroundWater[newIndex] = state.GroundWater[index];
			nextState.SurfaceWater[newIndex] = state.SurfaceWater[index];
			nextState.WaterSalinity[newIndex] = state.WaterSalinity[index];
			nextState.SurfaceIce[newIndex] = state.SurfaceIce[index];
			nextState.SubmergedIce[newIndex] = state.SubmergedIce[index];
			nextState.SoilFertility[newIndex] = state.SoilFertility[index];

		}

	}
}
