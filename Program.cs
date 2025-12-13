/*
 * Please note the following ASSUMPTIONS:
 * - the maze is basically a grid of equally sized cells
 * - the starting cell is fully colored (for helping determine size of a cell)
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Console = System.Console;

namespace MazeSolver
{
	internal class Program
	{
		// color constants
		private static readonly Color _borderColor = Color.Black;
		private static readonly Color _startColor = Color.Red;
		private static readonly Color _endColor = Color.Blue;
		private static readonly Color _pathColor = Color.LimeGreen;

		// these 2 arrays define 4 directions = up, down, left, right
		private static readonly int[] _dirX = { 0, 0, 1, -1 };
		private static readonly int[] _dirY = { -1, 1, 0, 0 };

		// private objects used during recursion
		private static Bitmap _bitmap;
		private static Graphics _graphics;
		private static Pen _pathPen;
		private static bool[,] _currentPath;
		private static int _cellWidth = 0;

		// returns true if RGB of 2 colors match
		private static bool IsMatchingColor(Color color1, Color color2) => color1.R == color2.R && color1.G == color2.G && color1.B == color2.B;

		// converts pixel to 0-based cell number
		private static int PixelToCell(int pixel) => pixel / _cellWidth;
		
		// converts 0-based cell number to pixel
		private static int CellToPixel(int cell) => cell * _cellWidth + _cellWidth / 2;

		// displays the program usage
		private static void DisplayUsage() => Console.WriteLine("USAGE:  MazeSolver <InputImageFile(JPG/PNG/BMP)> <OutputImageFile>");

		// Main program
		private static void Main(string[] args)
		{
			string outFile;

			// verify an input image file was passed in, and load it
			try
			{
#if DEBUG
				var inFile = AppDomain.CurrentDomain.BaseDirectory + @"..\..\Maze.png";
				outFile = AppDomain.CurrentDomain.BaseDirectory + @"..\..\Solved.png";
#else
				if (args.Length < 2)
				{
					DisplayUsage();
					return;
				}
				var inFile = args[0].Trim();
				outFile = args[1].Trim();
#endif
				inFile = Path.GetFullPath(inFile);
				outFile = Path.GetFullPath(outFile);
				if (!File.Exists(inFile))
				{
					DisplayUsage();
					return;
				}
				if (File.Exists(outFile))
					File.Delete(outFile);

				// load the image file into a bitmap
				_bitmap = new Bitmap(inFile);
			}
			catch
			{
				// error here if the input file wasn't a valid image file
				DisplayUsage();
				return;
			}

			// find the starting cell in the maze
			var startX = -1;
			var startY = -1;
			for (var x = 0; x < _bitmap.Width && startX < 0; x++)
			{
				_cellWidth = 0;
				for (var y = 0; y < _bitmap.Height; y++)
				{
					// and determine the width of a cell
					while (IsMatchingColor(_bitmap.GetPixel(x, y), _startColor))
					{
						_cellWidth++;
						y++;
					}
					if (_cellWidth > 0)
					{
						_cellWidth++; // add 1 more pixel for the border
						startX = PixelToCell(x + _cellWidth / 2);
						startY = PixelToCell(y - _cellWidth / 2);
						break;
					}
				}
			}
			if (startX < 0)
			{
				Console.WriteLine("ERROR: No starting point in the maze was found!");
				return;
			}
			Debug.WriteLine($"StartPoint = {startX}/{startY}, Width = {_cellWidth}");

			// allocate matrix to track the current path while walking through the maze
			_currentPath = new bool[_bitmap.Width / _cellWidth, _bitmap.Height / _cellWidth];

			using (_graphics = Graphics.FromImage(_bitmap))
			using (_pathPen = new Pen(_pathColor, 3))
			{
				// recursively walk through the maze to see if a solution exists
				if (SolveMaze(startX, startY, startX, startY))
				{
					// save the solution with the path between the start and end colored
					_bitmap.Save(outFile, ImageFormat.Png);
					Console.WriteLine($"Solution saved in {outFile}");
				}
				else
				{
					Console.WriteLine("No solution to the maze was found!");
				}
			}
		}

		// This function is called recursively to find a solution by walking
		// through the maze one cell at a time and looking in all directions.
		static bool SolveMaze(int fromX, int fromY, int toX, int toY)
		{
			// first check if out-of-bounds
			if (toX < 0 || toX >= _bitmap.Width / _cellWidth || toY < 0 || toY >= _bitmap.Height / _cellWidth)
				return false;

			// and check if this cell is already in the current path
			// (an invalid direction, since would be backtracking from here)
			if (_currentPath[toX, toY])
				return false;

			// then check if a border exists between the 2 cells
			if (fromX < toX)
			{
				if (HasBorderBetweenXCells(fromX, toX, toY))
					return false;
			}
			if (fromX > toX)
			{
				if (HasBorderBetweenXCells(toX, fromX, toY))
					return false;
			}
			if (fromY < toY)
			{
				if (HasBorderBetweenYCells(fromY, toY, toX))
					return false;
			}
			if (fromY > toY)
			{
				if (HasBorderBetweenYCells(toY, fromY, toX))
					return false;
			}

			// return true if we're at the ending cell
			if (IsMatchingColor(_bitmap.GetPixel(CellToPixel(toX), CellToPixel(toY)), _endColor))
				return true;

			// otherwise add this cell to the current path
			_currentPath[toX, toY] = true;

			// and continue looking at the adjacent cells in the 4 possible directions from here
			for (var d = 0; d < 4; d++)
			{
				var newX = toX + _dirX[d];
				var newY = toY + _dirY[d];
				if (SolveMaze(toX, toY, newX, newY))
				{
					// once we've hit the end cell, draw the green path line
					// (will be recursively drawn backwards from end to start)
					_graphics.DrawLine(_pathPen, CellToPixel(toX), CellToPixel(toY),
						CellToPixel(newX), CellToPixel(newY));
					Debug.WriteLine($"{toX}/{toY} -> {newX}/{newY}");
					return true;
				}
			}

			// remove from the current path if not part of the solution
			_currentPath[toX, toY] = false;
			return false;
		}

		// returns true if a border exists between cells that are side by side
		static bool HasBorderBetweenXCells(int fromX, int toX, int y)
		{
			// check every pixel on the X-axis between the cells
			var pixelY = CellToPixel(y);
			for (var pixelX = CellToPixel(fromX); pixelX <= CellToPixel(toX); pixelX++)
			{
				if (IsMatchingColor(_bitmap.GetPixel(pixelX, pixelY), _borderColor))
					return true;
			}
			return false;
		}
		
		// returns true if a border exists between cells that are above/below each other
		static bool HasBorderBetweenYCells(int fromY, int toY, int x)
		{
			// check every pixel on the Y-axis between the cells
			var pixelX = CellToPixel(x);
			for (var pixelY = CellToPixel(fromY); pixelY <= CellToPixel(toY); pixelY++)
			{
				if (IsMatchingColor(_bitmap.GetPixel(pixelX, pixelY), _borderColor))
					return true;
			}
			return false;
		}
	}
}
