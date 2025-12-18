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
		// displays the program usage
		private static void DisplayUsage() => Console.WriteLine("USAGE:  MazeSolver <InputImageFile(JPG/PNG/BMP)> <OutputImageFile>");

		// Main program
		private static void Main(string[] args)
		{
			string outFile;
			Bitmap bitmap;

			// verify an input image file was passed in, and load it
			try
			{
				if (args.Length < 2)
				{
					DisplayUsage();
					return;
				}
				var inFile = args[0].Trim();
				outFile = args[1].Trim();

				// verify input file and load into bitmap
				inFile = Path.GetFullPath(inFile);
				if (!File.Exists(inFile))
				{
					DisplayUsage();
					return;
				}
				bitmap = new Bitmap(inFile);

				// verify output file
				outFile = Path.GetFullPath(outFile);
				switch (Path.GetExtension(outFile).ToLower())
				{
					case ".bmp":
					case ".png":
					case ".jpg":
						if (File.Exists(outFile))
							File.Delete(outFile);
						break;

					default:
						DisplayUsage();
						return;
				}
			}
			catch (Exception ex)
			{
				// error here if the input file wasn't a valid image file
				Console.WriteLine($"ERROR: {ex.Message}");
				return;
			}

			// use the MazeSolver class to solve the maze
			var mazeSolver = new MazeSolver();
			if (mazeSolver.Solve(bitmap))
			{
				try
				{
					// save the solution with the path between the start and end colored
					switch (Path.GetExtension(outFile).ToLower())
					{
						case ".bmp":
							bitmap.Save(outFile, ImageFormat.Bmp);
							break;
						case ".png":
							bitmap.Save(outFile, ImageFormat.Png);
							break;
						case ".jpg":
							bitmap.Save(outFile, ImageFormat.Jpeg);
							break;
					}
					Console.WriteLine($"Solution saved in {outFile}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"ERROR saving output: {ex.Message}");
				}
			}
			else
			{
				Console.WriteLine("No solution to the maze was found!");
			}
		}
	}
}
