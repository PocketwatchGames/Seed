using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Seed
{
	public class ToolTectonic : Tool
	{
		public float BrushSize = 1;
		public float DeltaPerSecond = 100;
		public bool Active;
		public Point Start;
		public int StartPlate;

		override public void OnSelect() { Active = false; }
		override public void OnDeselect() { Active = false; }
		override public void DrawWorld(SpriteBatch spriteBatch, World.State state)
		{
			if (!Active)
			{
				var p = Gui.TileInfoPoint;
				StartPlate = Gui.World.States[Gui.World.CurStateIndex].Plate[Gui.World.GetIndex(p.X, p.Y)];
			}
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
			for (int i = 0; i < Gui.World.Size; i++)
			{
				for (int j = 0; j < Gui.World.Size; j++)
				{
					if (Gui.World.States[Gui.World.CurStateIndex].Plate[Gui.World.GetIndex(i, j)] == StartPlate)
					{
						Rectangle rect = new Rectangle(i * Gui.World.tileRenderSize, j * Gui.World.tileRenderSize, Gui.World.tileRenderSize, Gui.World.tileRenderSize);
						spriteBatch.Draw(Gui.whiteTex, rect, Color.White * 0.2f);
					}
				}
			}
			spriteBatch.End();
		}
		override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
		{
		}
		override public void Update(GameTime gameTime, Point p)
		{
			if (Active)
			{
				lock (Gui.World.InputLock)
				{
					if (Gui.TileInfoPoint != Start)
					{
						int nextStateIndex = Gui.World.AdvanceState();

						Vector2 diff = new Vector2(Gui.TileInfoPoint.X - Start.X, Gui.TileInfoPoint.Y - Start.Y);
						Point move;
						if (Math.Abs(diff.X) > Math.Abs(diff.Y))
						{
							move = new Point(Math.Sign(diff.X), 0);
						} else
						{
							move = new Point(0, Math.Sign(diff.Y));
						}
						Gui.World.MovePlate(Gui.World.States[Gui.World.CurStateIndex], Gui.World.States[nextStateIndex], StartPlate, move);

						Start = Gui.TileInfoPoint;

						Gui.World.CurStateIndex = nextStateIndex;
					}



				}
			}
		}
		override public void OnMouseDown(Point p)
		{
			Active = true;
			Start =  Gui.TileInfoPoint;
			StartPlate = Gui.World.States[Gui.World.CurStateIndex].Plate[Gui.World.GetIndex(p.X, p.Y)];
		}
		override public void OnMouseUp(Point p)
		{
			Active = false;
		}
		override public void OnMouseWheel(float delta)
		{
			BrushSize = MathHelper.Clamp(BrushSize + delta / 100, 0, 50);
		}
	}

}
