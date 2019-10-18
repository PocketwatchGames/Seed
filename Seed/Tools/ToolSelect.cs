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
		bool _selecting;
		Point _start;
		Point _end;

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
			if (_selecting)
			{
				_end = p;
				UpdateSelection();
			}
		}
		override public void OnMouseDown(Point p) {
			_start = p;
			_end = p;
			_selecting = true;
			Gui.AnimalsSelected.Clear();
		}
		override public void OnMouseUp(Point p) {
			UpdateSelection();
			_selecting = false;
		}
		override public void OnMouseWheel(float delta) { }

		void UpdateSelection()
		{
			Gui.AnimalsSelected.Clear();
			Rectangle marquee = new Rectangle(Math.Min(_end.X, _start.X), Math.Min(_end.Y, _start.Y), 0, 0);
			marquee.Width = Math.Max(_end.X, _start.X) - marquee.X;
			marquee.Height = Math.Max(_end.Y, _start.Y) - marquee.Y;
			var state = Gui.World.States[Gui.World.CurStateIndex];
			for (int i = 0; i < Gui.World.MaxAnimals; i++)
			{
				if (state.Animals[i].Population > 0 && marquee.Contains(state.Animals[i].Position))
				{
					Gui.AnimalsSelected.Add(i);
				}
			}
		}
	}

}
