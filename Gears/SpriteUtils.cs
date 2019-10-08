using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Gears {
	class SpriteConstants {
		public static Matrix InvertY = Matrix.CreateScale(1, -1, 1);
	}
	class SpriteUtils {

		public static void DrawStandingBillboard(SpriteBatch spriteBatch, Texture2D tex, Rectangle srcRect, Vector2 pivot, Matrix view, Vector3 position, float scale, SpriteEffects effect)
		{
			Vector3 viewSpaceSpritePosition = Vector3.Transform(position, view * SpriteConstants.InvertY);
			spriteBatch.Draw(tex, new Vector2(viewSpaceSpritePosition.X, viewSpaceSpritePosition.Y), srcRect, Color.White, 0, pivot, scale, effect, viewSpaceSpritePosition.Z);
		}
	}
}
