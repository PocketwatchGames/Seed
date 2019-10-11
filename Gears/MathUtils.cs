using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Gears {
	public struct Transform {
		public Vector3 Position;
		public Vector3 Rotation;
//		public Vector3 scale;
	}

	public struct Range<T>
	{
		public T value;
		public T range;

	}

	public struct Point2Const
	{
		public static Point2 Zero = new Point2(0, 0);
	}
	public struct Point2 : IEquatable<Point2>
	{
		public int X;
		public int Z;

		public Point2(int x, int z)
		{
			X = x;
			Z = z;
		}
		public Point2(Vector2 v)
		{
			X = (int)Math.Floor(v.X);
			Z = (int)Math.Floor(v.Y);
		}
		public Point2(Vector3 v)
		{
			X = (int)Math.Floor(v.X);
			Z = (int)Math.Floor(v.Z);
		}
		public override int GetHashCode()
		{
			// TODO: replace with a real hash function
			return X + Z *10000;
		}
		public static Point2 operator +(Point2 a, Point2 b)
			=> new Point2(a.X + b.X, a.Z + b.Z);
		public static Point2 operator -(Point2 a, Point2 b)
			=> new Point2(b.X - a.X, b.Z - a.Z);
		public static bool operator ==(Point2 a, Point2 b)
			=> a.X == b.X && a.Z == b.Z;
		public static bool operator !=(Point2 a, Point2 b)
			=> a.X != b.X || a.Z != b.Z;
		override public bool Equals(object obj)
		{
			if (obj == null) return false;
			Point2 p = (Point2)obj;

			return p.X == X && p.Z == Z;
		}

		public bool Equals(Point2 p)
		{
			return p.X == X && p.Z == Z;
		}

		public override string ToString()
		{
			return X + ", " + Z;
		}
	}

	public struct Point3
	{
		public Point3(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}
		public Point3(Vector3 v)
		{
			X = (int)Math.Floor(v.X);
			Y = (int)Math.Floor(v.Y);
			Z = (int)Math.Floor(v.Z);
		}
		public int X;
		public int Y;
		public int Z;
	}

	public class MathUtils {
		public static int Wrap(int val, int maxVal)
		{
			while (val < 0) {
				val += maxVal;
			}
			while (val >= maxVal) {
				val -= maxVal;
			}
			return val;
		}


		public static uint LerpPackedColor(uint a, uint b, float t)
		{
			return
				(uint)(((byte)b - (byte)a) * t + (byte)a) +
				(((uint)(((byte)(b >> 8) - (byte)(a >> 8)) * t) + (byte)(a >> 8)) << 8) +
				(((uint)(((byte)(b >> 16) - (byte)(a >> 16)) * t) + (byte)(a >> 16)) << 16) +
				(((uint)(((byte)(b >> 24) - (byte)(a >> 24)) * t) + (byte)(a >> 24)) << 24);
		}

		public static float Lerp(float a, float b, float t)
		{
			return (b - a) * t + a;
		}
	}
}
