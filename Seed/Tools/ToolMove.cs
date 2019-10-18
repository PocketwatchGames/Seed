using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Seed
{
	public class ToolMove : Tool
	{

		override public void OnSelect() { }
		override public void OnDeselect() { }
		override public void DrawWorld(SpriteBatch spriteBatch, World.State state) {
			DrawSelection(spriteBatch, Gui, state);
		}
		override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
		{
			Tool.DrawInfoTooltip(spriteBatch, Gui, state);
		}
		override public void Update(GameTime gameTime, Point p) {
		}
		override public void OnMouseDown(Point p)
		{
			for (int i = 0; i < Gui.AnimalsSelected.Count; i++)
			{
				int animalIndex = Gui.AnimalsSelected[i];
				Gui.World.States[Gui.World.CurStateIndex].Animals[animalIndex].Destination = new Vector2(p.X + 0.5f, p.Y + 0.5f);
			}
		}
		override public void OnMouseUp(Point p) {
		}
		override public void OnMouseWheel(float delta) { }

	}

}
