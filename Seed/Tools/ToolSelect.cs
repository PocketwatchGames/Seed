using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Seed
{
	public class ToolSelect : Tool
	{
		override public void OnSelect() { }
		override public void OnDeselect() { }
		override public void DrawWorld(SpriteBatch spriteBatch, World.State state) { }
		override public void DrawTooltip(SpriteBatch spriteBatch, World.State state)
		{
			Tool.DrawInfoTooltip(spriteBatch, Gui, state);
		}
		override public void Update(GameTime gameTime, Point p) { }
		override public void OnMouseDown(Point p) { }
		override public void OnMouseUp(Point p) { }
		override public void OnMouseWheel(float delta) { }
	}

}
