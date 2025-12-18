/*
 * Please note the following ASSUMPTIONS:
 * - the maze is basically a grid of equally sized cells
 * - the starting cell is fully colored (for helping determine size of a cell)
 */

using System;
using System.Diagnostics;
using System.Drawing;

namespace MazeSolver
{
	internal class MazeSolver
	{
		private enum PixelType : byte
		{
			Unknown = 0,
			BorderColor,
			StartColor,
			EndColor,
			PathColor
		}

		// color constants
		private static readonly Color _startColor = Color.Red;
		private static readonly Color _endColor = Color.Blue;
		private static readonly Color _pathColor = Color.LimeGreen;

		// these 2 arrays define 4 directions = North, South, East, West
		private static readonly int[] _dirX = { 0, 0, 1, -1 };
		private static readonly int[] _dirY = { -1, 1, 0, 0 };

		// private objects used during recursion
		private Bitmap _bitmap;
		private PixelType[,] _pixelTypes;
		private Graphics _graphics;
		private Pen _pathPen;
		private bool[,] _currentPath;
		private int _cellSize = 0;

		// returns true if RGB of 2 colors match
		private static bool IsMatchingColor(Color color1, Color color2) => color1.R == color2.R && color1.G == color2.G && color1.B == color2.B;

		// returns true if RGB is considered a border color (black to dark gray)
		private static bool IsBorderColor(Color color) => color.R < 128 && color.G < 128 && color.B < 128;

		// converts pixel to 0-based cell number
		private int PixelToCell(int pixel) => pixel / _cellSize;

		// converts 0-based cell number to pixel
		private int CellToPixel(int cell) => cell * _cellSize + _cellSize / 2;

		// returns number of rows and columns in the grid of cells
		private int NumGridColumns() => _bitmap.Width / _cellSize;
		private int NumGridRows() => _bitmap.Height / _cellSize;

		// publicly called function to solve the maze
		public bool Solve(Bitmap bitmap)
		{
			var sw = Stopwatch.StartNew();
			_bitmap = bitmap;

			// allocate _pixelTypes grid as same size as bitmap in order
			// to store/retrieve the pixel types most efficiently
			_pixelTypes = new PixelType[_bitmap.Width, _bitmap.Height];

			// find the starting cell in the maze
			// (for efficiency, start with a course pixel search and
			// work down to finer searches until find the right color)
			var startCell = new Point(-1, -1);
			for (var i = 64; i > 0 && startCell.X < 0; i /= 2)
				startCell = FindStartCell(i);

			if (startCell.X < 0)
			{
				Console.WriteLine("ERROR: No starting point in the maze was found!");
				return false;
			}
			Debug.WriteLine($"StartCell = [{startCell.X},{startCell.Y}], CellSize = {_cellSize}");

			// allocate matrix to track the current path while walking through the maze
			_currentPath = new bool[NumGridColumns(), NumGridRows()];

			using (_graphics = Graphics.FromImage(_bitmap))
			using (_pathPen = new Pen(_pathColor, 4))
			{
				// call the recursive function to walk through the maze
				var isSolved = SolveRecursively(startCell, startCell);
				sw.Stop();
#if DEBUG
				// display the current path (if not too wide)
				if (NumGridColumns() < 50)
				{
					_currentPath[startCell.X, startCell.Y] = true;
					for (var y = 0; y < NumGridRows(); y++)
					{
						for (var x = 0; x < NumGridColumns(); x++)
							Console.Write(_currentPath[x, y] ? " #" : " -");
						Console.WriteLine("");
					}
				}
				Console.WriteLine($"Start cell = [{startCell.X},{startCell.Y}], cell size = {_cellSize}");
#endif
				Console.WriteLine($"Solved = {isSolved} (finished in {sw.ElapsedMilliseconds} ms)");
				return isSolved;
			}
		}

		// This function is called recursively to find a solution by walking
		// through the maze one cell at a time and looking in all directions.
		// We first need to see if moving to this cell from the previous cell
		// is a valid move, and if so we will add it to the current path,
		// then attempt to continue moving to adjacent cells from there.
		private bool SolveRecursively(Point fromCell, Point toCell)
		{
			// first check if out-of-bounds
			if (toCell.X < 0 || toCell.X >= NumGridColumns() || toCell.Y < 0 || toCell.Y >= NumGridRows())
				return false;

			// and check if this cell is already in the current path
			// (an invalid direction, since would be either backtracking or looping)
			if (_currentPath[toCell.X, toCell.Y])
				return false;

			// then check if a border exists between the 2 cells
			if (fromCell.X < toCell.X && HasBorderBetweenXCells(fromCell.X, toCell.X, toCell.Y))
				return false;
			if (fromCell.X > toCell.X && HasBorderBetweenXCells(toCell.X, fromCell.X, toCell.Y))
				return false;
			if (fromCell.Y < toCell.Y && HasBorderBetweenYCells(fromCell.Y, toCell.Y, toCell.X))
				return false;
			if (fromCell.Y > toCell.Y && HasBorderBetweenYCells(toCell.Y, fromCell.Y, toCell.X))
				return false;

			// otherwise add this cell to the current path
			_currentPath[toCell.X, toCell.Y] = true;

			// return true if we're at the ending cell
			if (GetPixelType(CellToPixel(toCell.X), CellToPixel(toCell.Y)) == PixelType.EndColor)
				return true;

			// else continue looking at the adjacent cells in the 4 possible directions from here
			for (var d = 0; d < 4; d++)
			{
				var nextCell = new Point(toCell.X + _dirX[d], toCell.Y + _dirY[d]);

				if (SolveRecursively(toCell, nextCell))
				{
					// once we've hit the end cell, draw the green path line
					// (will be recursively drawn backwards from end to start)
					_graphics.DrawLine(_pathPen, CellToPixel(toCell.X), CellToPixel(toCell.Y),
						CellToPixel(nextCell.X), CellToPixel(nextCell.Y));
					Debug.WriteLine($"[{toCell.X},{toCell.Y}] -> [{nextCell.X},{nextCell.Y}]");
					return true;
				}
			}

			// remove from the current path if not part of the solution
			_currentPath[toCell.X, toCell.Y] = false;
			return false;
		}

		// returns true if a border exists between cells that are side by side
		private bool HasBorderBetweenXCells(int fromX, int toX, int y)
		{
			// check every pixel on the X-axis between the cells
			var pixelY = CellToPixel(y);
			for (var pixelX = CellToPixel(fromX); pixelX <= CellToPixel(toX); pixelX++)
			{
				if (GetPixelType(pixelX, pixelY) == PixelType.BorderColor)
					return true;
			}
			return false;
		}

		// returns true if a border exists between cells that are above/below each other
		private bool HasBorderBetweenYCells(int fromY, int toY, int x)
		{
			// check every pixel on the Y-axis between the cells
			var pixelX = CellToPixel(x);
			for (var pixelY = CellToPixel(fromY); pixelY <= CellToPixel(toY); pixelY++)
			{
				if (GetPixelType(pixelX, pixelY) == PixelType.BorderColor)
					return true;
			}
			return false;
		}

		// Search through the bitmap for the starting cell
		private Point FindStartCell(int pixelIncrement)
		{
			_cellSize = 0;
			for (var x = pixelIncrement / 2; x < _bitmap.Width; x += pixelIncrement)
			{
				for (var y = pixelIncrement / 2; y < _bitmap.Height; y += pixelIncrement)
				{
					if (GetPixelType(x, y) == PixelType.StartColor)
					{
						// find starting/ending pixels of the starting cell
						var xStart = x;
						var yStart = y;
						var yEnd = y;
						while (xStart > 0 && GetPixelType(xStart - 1, y) == PixelType.StartColor)
							xStart--;
						while (yStart > 0 && GetPixelType(x, yStart - 1) == PixelType.StartColor)
							yStart--;
						while (yEnd < _bitmap.Height - 1 && GetPixelType(x, yEnd + 1) == PixelType.StartColor)
							yEnd++;

						// determine the cell size and location of the starting cell in the grid
						_cellSize = yEnd - yStart + 2;
						return new Point(PixelToCell(xStart + _cellSize / 2), PixelToCell(yStart + _cellSize / 2));
					}
				}
			}
			return new Point(-1, -1);
		}

		// To efficiently return the pixel type for a specific x,y in the bitmap
		// (will use what's stored if had already retrieved the type for this pixel)
		private PixelType GetPixelType(int pixelX, int pixelY)
		{
			var pixelType = _pixelTypes[pixelX, pixelY];
			if (pixelType == PixelType.Unknown)
			{
				// if pixel not yet retrieved, get it now and store it
				var pixelColor = _bitmap.GetPixel(pixelX, pixelY);
				if (IsMatchingColor(pixelColor, _startColor))
					pixelType = PixelType.StartColor;
				else if (IsMatchingColor(pixelColor, _endColor))
					pixelType = PixelType.EndColor;
				else if (IsBorderColor(pixelColor))
					pixelType = PixelType.BorderColor;
				else
					pixelType = PixelType.PathColor;

				_pixelTypes[pixelX, pixelY] = pixelType;
			}
			return pixelType;
		}
	}
}
