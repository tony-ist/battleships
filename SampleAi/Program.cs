﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SampleAi
{
    class Program
    {
        // Управляющая программа battleships.exe будет запускать этот файл и перенаправлять стандартные потоки ввода и вывода.
        //
        // Вам нужно читать информацию с консоли и писать команды на консоль.
        // Конец ввода — это сигнал к завершению программы.

        private static readonly IEnumerable<Size> diagonals = 
            new List<Size> { new Size(1, 1), new Size(1, -1), new Size(-1, -1), new Size(-1, 1) };

        private static readonly IEnumerable<Size> adjacency = 
            new List<Size> { new Size(0, 1), new Size(1, 0), new Size(0, -1), new Size(-1, 0) };

        private static readonly IEnumerable<Size> neighbours = diagonals.Union(adjacency);

        static void Main()
        {
            var random = new Random();
            var sequence = new Stack<Point>();
            var aim = new Point(0, 0);
            var boardSize = new Size(0, 0);
            var nonTargetCells = new HashSet<Point>();
            var hotspots = new Stack<Point>();
            var woundedCells = new HashSet<Point>();
            var shipSizes = new List<int>();

            while (true)
            {
                var line = Console.ReadLine();
                if (line == null) return;
                // line имеет один из следующих форматов:
                // Init <map_width> <map_height> <ship1_size> <ship2_size> ...
                // Wound <last_shot_X> <last_shot_Y>
                // Kill <last_shot_X> <last_shot_Y>
                // Miss <last_shot_X> <last_shot_Y>
                // Один экземпляр вашей программы может быть использван для проведения нескольких игр подряд.

                // Сообщение Init сигнализирует о том, что началась новая игра.

                var message = line.Split(' ').ToList();
                switch (message[0])
                {
                    case "Init":
                        boardSize.Width = int.Parse(message[1]);
                        boardSize.Height = int.Parse(message[2]);
                        shipSizes = message.GetRange(3, message.Count - 3).ConvertAll(int.Parse);
                        shipSizes.Sort();

                        nonTargetCells.Clear();
                        hotspots.Clear();
                        woundedCells.Clear();
                        sequence = new Stack<Point> (GenerateRandomSequence(boardSize, random));
                        break;

                    case "Miss":
                        nonTargetCells.Add(aim);
                        break;

                    case "Wound":
                        GetOffsetCells(aim, boardSize, diagonals).ToList().ForEach(cell => nonTargetCells.Add(cell));
                        nonTargetCells.Add(aim);
                        hotspots.Push(aim);
                        woundedCells.Add(aim);
                        break;

                    case "Kill":
                        GetOffsetCells(aim, boardSize, neighbours).ToList().ForEach(cell => nonTargetCells.Add(cell));
                        nonTargetCells.Add(aim);
                        woundedCells.Add(aim);
                        MarkDeadShipAdjacency(aim, boardSize, woundedCells, nonTargetCells);
                        shipSizes.Remove(GetWoundedShipSize(boardSize, aim, woundedCells));
                        break;
                }

                aim = GetNextCell(boardSize, shipSizes, nonTargetCells, hotspots, sequence);
                Console.WriteLine("{0} {1}", aim.X, aim.Y);
            }
        }

        private static IEnumerable<Point> GenerateRandomSequence(Size boardSize, Random random)
        {
            return (from x in Enumerable.Range(0, boardSize.Width)
                    from y in Enumerable.Range(0, boardSize.Height)
                    select new Point(x, y)).OrderBy(point => random.Next());
        }

        private static bool CheckBounds(Point cell, Size boardSize)
        {
            var withinVertically = 0 <= cell.X && cell.X < boardSize.Width;
            var withinHorizontally = 0 <= cell.Y && cell.Y < boardSize.Height;
            return withinVertically && withinHorizontally;
        }

        private static void MarkDeadShipAdjacency(Point aim, Size boardSize, 
            ICollection<Point> woundedCells, ICollection<Point> nonTargetCells)
        {
            GetOffsetCells(aim, boardSize, adjacency).Where(woundedCells.Contains).ToList().ForEach(toMark =>
            {
                var direction = new Size(toMark) - new Size(aim);
                while (woundedCells.Contains(toMark) && CheckBounds(toMark, boardSize))
                {
                    toMark += direction;
                }
                if (CheckBounds(toMark, boardSize))
                    nonTargetCells.Add(toMark);
            });
        }

        private static Point GetNextCell(Size boardSize, List<int> shipSizes, 
            ICollection<Point> excluded, Stack<Point> hotspots, Stack<Point> sequence)
        {
            while (hotspots.Count > 0)
            {
                var hotspot = hotspots.Peek();
                var candidates = GetOffsetCells(hotspot, boardSize, adjacency).Where(cell => !excluded.Contains(cell)).ToList();
                if (candidates.Any()) return candidates[0];
                hotspots.Pop();
            }

            Point nextCell;
            do
            {
                nextCell = sequence.Pop();
            } while (excluded.Contains(nextCell) || !IsShipPossible(boardSize, shipSizes[0], nextCell, excluded));
            return nextCell;
        }

        private static bool IsShipPossible(Size boardSize, int shipSize, Point where, ICollection<Point> excluded)
        {
            return FindMaxShipSize(boardSize, where, point => !excluded.Contains(point)) >= shipSize;
        }

        private static int GetWoundedShipSize(Size boardSize, Point where, ICollection<Point> woundedCells)
        {
            return FindMaxShipSize(boardSize, where, woundedCells.Contains);
        }

        private static int FindMaxShipSize(Size boardSize, Point where, Func<Point, bool> included)
        {
            var directions = new List<Size> {new Size(1, 0), new Size(0, 1)};

            return directions.Select(direction =>
            {
                var firstPoint = where;
                while (included.Invoke(firstPoint) && CheckBounds(firstPoint, boardSize))
                {
                    firstPoint += direction;
                }

                var secondPoint = where;
                var oppositeDirection = new Size(-direction.Width, -direction.Height);
                while (included.Invoke(secondPoint) && CheckBounds(secondPoint, boardSize))
                {
                    secondPoint += oppositeDirection;
                }

                return Math.Max(Math.Abs(secondPoint.X - firstPoint.X), Math.Abs(secondPoint.Y - firstPoint.Y)) - 1;
            }).Max();
        }

        private static IEnumerable<Point> GetOffsetCells(Point cell, Size size, IEnumerable<Size> offsets)
        {
            return from offset in offsets
                   let point = Point.Add(cell, offset)
                   where CheckBounds(point, size)
                   select point;
        } 
    }
}
