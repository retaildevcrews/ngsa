// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ngsa.Middleware
{
    public static class AsciiArt
    {
        /// <summary>
        /// Display the ASCII art file if it exists
        /// </summary>
        /// <param name="file">file with the ascii art</param>
        /// <param name="color">ConsoleColor</param>
        /// <param name="animation">animate the art</param>
        /// <returns>Task</returns>
        public static async Task DisplayAsciiArt(string file, ConsoleColor color, Animation animation = Animation.None)
        {
            if (File.Exists(file))
            {
                string txt = File.ReadAllText(file);

                if (animation == Animation.None)
                {
                    DisplayArt(txt, color);
                    Console.ResetColor();

                    return;
                }

                string[] lines = File.ReadAllLines(file);

                Console.CursorVisible = false;

                // scroll the window
                for (int i = 0; i < Console.WindowHeight; i++)
                {
                    Console.WriteLine();
                }

                // determine top of screen
                int top = Console.CursorTop - Console.WindowHeight;
                int row = top + Console.WindowHeight - lines.Length - 5;

                int key = 0;
                Random rnd = new Random(DateTime.Now.Millisecond);

                SortedList<int, ConsoleCharacter> lrandom = new SortedList<int, ConsoleCharacter>();
                List<ConsoleCharacter> list = new List<ConsoleCharacter>();

                // create the random list
                for (int r = 0; r < lines.Length; r++)
                {
                    string line = lines[r];

                    for (int c = 0; c < line.Length; c++)
                    {
                        if (!char.IsWhiteSpace(line[c]))
                        {
                            while (key < 1 || lrandom.ContainsKey(key))
                            {
                                key = rnd.Next(1, int.MaxValue);
                            }

                            ConsoleCharacter l = new ConsoleCharacter
                            {
                                Color = color,
                                Row = top + r,
                                Col = c,
                                Value = line[c],
                            };

                            // add to random list and in-order list
                            list.Add(l);
                            lrandom.Add(key, l);
                        }
                    }
                }

                switch (animation)
                {
                    case Animation.None:
                        break;
                    case Animation.Dissolve:
                        await Dissolve(lrandom).ConfigureAwait(false);
                        break;
                    case Animation.Fade:
                        await Fade(txt, lines, top).ConfigureAwait(false);
                        break;
                    case Animation.Scroll:
                        await Scroll(txt, color, lines, top).ConfigureAwait(false);
                        break;
                    case Animation.TwoColor:
                        await TwoColor(list).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }

                Console.SetCursorPosition(0, top + lines.Length + 1);
                Console.CursorVisible = true;
                Console.ResetColor();
            }
        }

        private static void DisplayArt(string txt, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(txt);
        }

        private static async Task TwoColor(List<ConsoleCharacter> list)
        {
            // change art to two color
            int end = list.Count - 1;
            int last = end;

            for (int i = 0; i <= end; i++)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.SetCursorPosition(list[i].Col, list[i].Row);
                Console.Write(list[i].Value);

                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.SetCursorPosition(list[end].Col, list[end].Row);
                Console.Write(list[end].Value);
                end--;

                await Task.Delay(30);
            }
        }

        private static async Task Dissolve(SortedList<int, ConsoleCharacter> list)
        {
            // show the art - random dissolve
            foreach (ConsoleCharacter l in list.Values)
            {
                Console.SetCursorPosition(l.Col, l.Row);
                Console.ForegroundColor = l.Color;
                Console.Write(l.Value);
                await Task.Delay(2);
            }
        }

        private static async Task Scroll(string txt, ConsoleColor color, string[] lines, int top)
        {
            Console.SetCursorPosition(0, top);
            DisplayArt(txt, color);

            int row = top + Console.WindowHeight - lines.Length - 2;

            // scroll the art down
            for (int i = top; i < row; i++)
            {
                Console.MoveBufferArea(0, i, Console.BufferWidth, lines.Length, 0, i + 1);
                await Task.Delay(100);
            }

            // scroll the art up
            for (int i = row; i > top; i--)
            {
                Console.MoveBufferArea(0, i, Console.BufferWidth, lines.Length, 0, i - 1);
                await Task.Delay(100);
            }
        }

        private static async Task Fade(string txt, string[] lines, int top)
        {
            Console.SetCursorPosition(0, top);

            // show the logo from top left
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            foreach (char c in txt)
            {
                Console.Write(c);

                if (!char.IsWhiteSpace(c))
                {
                    await Task.Delay(15);
                }
            }

            // change art color from bottom right
            await Task.Delay(200);
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            for (int r = lines.Length - 1 + top; r >= top; r--)
            {
                string line = lines[r - top];

                for (int c = line.Length - 1; c >= 0; c--)
                {
                    Console.SetCursorPosition(c, r);
                    Console.Write(line[c]);

                    if (!char.IsWhiteSpace(line[c]))
                    {
                        await Task.Delay(15);
                    }
                }
            }

            // change art color from top left
            //Console.SetCursorPosition(0, top);
            //Console.ForegroundColor = ConsoleColor.DarkMagenta;
            //foreach (char c in txt)
            //{
            //    Console.Write(c);

            //    if (!char.IsWhiteSpace(c))
            //    {
            //        await Task.Delay(20);
            //    }
            //}
        }

        public enum Animation { None, Dissolve, Fade, Scroll, TwoColor }

        internal class ConsoleCharacter
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public char Value { get; set; }
            public ConsoleColor Color { get; set; } = ConsoleColor.DarkMagenta;
        }
    }
}
