using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Seed
{
	public class ToolElevation : Tool
	{
		public float BrushSize = 1;
		public float DeltaPerSecond = 100;
		public bool Active;

		override public void OnSelect() { Active = false; }
		override public void OnDeselect() { Active = false; }
		override public void DrawWorld(SpriteBatch spriteBatch, World.State state)
		{
			var p = Gui.TileInfoPoint;
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
			for (int i = (int)-Math.Ceiling(BrushSize); i <= Math.Ceiling(BrushSize); i++)
			{
				for (int j = (int)-Math.Ceiling(BrushSize); j <= Math.Ceiling(BrushSize); j++)
				{
					float dist = (float)Math.Sqrt(i * i + j * j);
					if (dist <= BrushSize)
					{
						float distT = (BrushSize == 0) ? 1.0f : (1.0f - (float)Math.Pow(dist / BrushSize, 2));
						int x = Gui.World.WrapX(p.X + i);
						int y = p.Y + j;
						if (y < 0 || y >= Gui.World.Size)
						{
							continue;
						}
						Rectangle rect = new Rectangle(x * Gui.World.tileRenderSize, y * Gui.World.tileRenderSize, Gui.World.tileRenderSize, Gui.World.tileRenderSize);
						spriteBatch.Draw(Gui.whiteTex, rect, Color.White * 0.2f);
					}
				}
			}
			spriteBatch.End();
		}
		override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
		{
			int index = Gui.World.GetIndex(Gui.TileInfoPoint.X, Gui.TileInfoPoint.Y);
			int textY = 300;
			spriteBatch.DrawString(Gui.Font, "Elevation: " + (int)(state.Elevation[index]), new Vector2(5, textY += 15), Color.White);
		}
		override public void Update(GameTime gameTime, Point p)
		{
			if (Active)
			{
				lock (Gui.World.InputLock)
				{
					float dt = (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;
					var state = Gui.World.States[Gui.World.CurStateIndex];
					for (int i = (int)-Math.Ceiling(BrushSize); i <= Math.Ceiling(BrushSize); i++)
					{
						for (int j = (int)-Math.Ceiling(BrushSize); j <= Math.Ceiling(BrushSize); j++)
						{
							float dist = (float)Math.Sqrt(i * i + j * j);
							if (dist <= BrushSize)
							{
								float distT = (BrushSize == 0) ? 1.0f : (1.0f - (float)Math.Pow(dist / BrushSize, 2));
								int x = Gui.World.WrapX(p.X + i);
								int y = p.Y + j;
								if (y < 0 || y >= Gui.World.Size)
								{
									continue;
								}
								int index = Gui.World.GetIndex(x, y);
								state.Elevation[index] += distT * DeltaPerSecond * dt;
							}
						}
					}

				}
			}
		}
		override public void OnMouseDown(Point p)
		{
			Active = true;
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
