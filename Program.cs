/*
 *  Copyright 2012 Tamme Schichler <tammeschichler@googlemail.com>
 * 
 *  This file is part of TQ Grid Extractor.
 *
 *  TQ Grid Extractor is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  TQ Grid Extractor is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with TQ Grid Extractor.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using TQ.Mesh;

namespace GridExtractor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                return;
            }

            string[] paths = args.Where(File.Exists).ToArray();
            paths =
                paths.Concat(
                    args.Where(Directory.Exists).SelectMany(
                        x => Directory.GetFiles(x, "*.msh", SearchOption.AllDirectories))).ToArray();

            int[] size = args.Select(x =>
                                         {
                                             int r;
                                             return Int32.TryParse(x, out r) ? r : 0;
                                         }
                ).Where(x => x > 0).ToArray();

            size = size.Length < 2 ? new[] { 512, 512 } : size.Take(2).ToArray();

            string[] colorstrings =
                args.Select(x => x.ToLowerInvariant()).Where(x => x.Contains("color:")).Select(
                    x => x.Replace("color:", "")).ToArray();
            string[] backgoundstrings =
                args.Select(x => x.ToLowerInvariant()).Where(x => x.Contains("backgound:")).Select(
                    x => x.Replace("backgound:", "")).ToArray();
            string[] widthstrings =
                args.Select(x => x.ToLowerInvariant()).Where(x => x.Contains("penwidth:")).Select(
                    x => x.Replace("penwidth:", "")).ToArray();
            var pen = new Pen(Color.Black, 1);
            Color background = Color.Transparent;

            foreach (string s in colorstrings)
            {
                try
                {
                    pen = new Pen(Color.FromName(s));
                    break;
                }
                catch (Exception)
                {
                    try
                    {
                        pen = new Pen(Color.FromArgb(Int32.Parse(s, NumberStyles.AllowHexSpecifier)));
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Couldn't parse pen color from {0}.", s);
                    }
                }
            }

            foreach (string s in backgoundstrings)
            {
                try
                {
                    background = Color.FromName(s);
                    break;
                }
                catch (Exception)
                {
                    try
                    {
                        background = Color.FromArgb(Int32.Parse(s, NumberStyles.AllowHexSpecifier));
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Couldn't parse background color from {0}.", s);
                    }
                }
            }

            foreach (string s in widthstrings)
            {
                try
                {
                    pen.Width = float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("Couldn't parse pen width from {0}.", s);
                }
            }

            foreach (string p in paths)
            {
                Console.WriteLine(p);
                try
                {
                    Mesh m = Mesh.Parse(File.ReadAllBytes(p));
                    Bitmap b = DrawTexGrid(m, size, pen, background);

                    b.Save(p + ".grid.png", ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }
        }

        private static Bitmap DrawTexGrid(Mesh m, IList<int> size, Pen pen, Color background)
        {
            var b = new Bitmap(size[0], size[1], PixelFormat.Format32bppArgb);

            Graphics g = Graphics.FromImage(b);
            g.Clear(background);

            for (int i = 0; i < m.Triangles.GetLength(0); i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Vertex v1 = m.Vertices[m.Triangles[i, k]];
                    Vertex v2 = m.Vertices[m.Triangles[i, (k + 1) % 3]];

                    g.DrawLine(pen, v1.UV[0] * size[0], v1.UV[1] * size[1], v2.UV[0] * size[0], v2.UV[1] * size[1]);
                }
            }

            g.Dispose();

            return b;
        }
    }
}