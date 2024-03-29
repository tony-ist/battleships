﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace battleships
{
    public enum Cell
    {
        Empty,
        Ship,
        DeadOrWoundedShip,
        Miss
    }

    public enum ShotEffect
    {
        Miss,
        Wound,
        Kill
    }

    public class Ship
    {
        public int Size { get; private set; }

        public Direction ShipDirection { get; private set; }

        public HashSet<Vector> AliveCells { get; private set; }

        public bool IsAlive { get { return AliveCells.Any(); } }

        public Vector Location { get; private set; }

        public Ship(Vector location, int size, Direction shipDirection)
        {
            Location = location;
            Size = size;
            ShipDirection = shipDirection;
            AliveCells = new HashSet<Vector>(GetOccupiedCells());
        }

        public enum Direction
        {
            Vertical,
            Horizontal
        } 

        public IEnumerable<Vector> GetOccupiedCells()
        {
            var directionVector = GetDirectionVector(ShipDirection);
            return from i in Enumerable.Range(0, Size)
                select directionVector.Mult(i).Add(Location);
        }

        public static Vector GetDirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.Horizontal: return new Vector(1, 0);
                case Direction.Vertical: return new Vector(0, 1);
                default: throw new ArgumentException("Unknown ship direction.");
            }
        }
    }

    public class Map
    {
        public Cell[,] Cells { get; private set; }

        public Ship[,] ShipsMap { get; private set; }

        public List<Ship> Ships { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new Cell[width, height];
            ShipsMap = new Ship[width, height];
            Ships = new List<Ship>();
        }

        public Cell this[Vector p]
        {
            get { return Cells[p.X, p.Y]; }

            private set { Cells[p.X, p.Y] = value; }
        }

        public bool PlaceShip(Ship ship)
        {
            if (!CanPlaceShip(ship)) return false;

            var shipCells = ship.GetOccupiedCells().ToList();

            shipCells.ForEach(cell =>
            {
                this[cell] = Cell.Ship;
                ShipsMap[cell.X, cell.Y] = ship;
            });

            Ships.Add(ship);

            return true;
        }

        public ShotEffect Shoot(Vector target)
        {
            var withinBoard = CheckBounds(target);
            if (withinBoard)
            {
                var hitShip = this[target] == Cell.Ship;
                if (hitShip)
                {
                    var ship = ShipsMap[target.X, target.Y];
                    ship.AliveCells.Remove(target);
                    this[target] = Cell.DeadOrWoundedShip;
                    return ship.IsAlive ? ShotEffect.Wound : ShotEffect.Kill;
                }

                var miss = this[target] == Cell.Empty;
                if (miss)
                {
                    this[target] = Cell.Miss;
                }
            }
            return ShotEffect.Miss;
        }

        public IEnumerable<Vector> GetNeighbours(Vector cell)
        {
            return
                from x in new[] { -1, 0, 1 }
                from y in new[] { -1, 0, 1 }
                let c = cell.Add(new Vector(x, y))
                where CheckBounds(c)
                select c;
        }

        public bool CheckBounds(Vector p)
        {
            return p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
        }

        public bool HasAliveShips()
        {
            return Ships.Any(s => s.IsAlive);
        }

        public bool CanPlaceShip(Ship ship)
        {
            var shipCells = ship.GetOccupiedCells().ToList();
            return CellsFit(shipCells) && AdjacentCellsAreEmpty(shipCells);
        }

        private bool CellsFit(IEnumerable<Vector> cells)
        {
            return cells.All(CheckBounds);
        }

        private bool AdjacentCellsAreEmpty(IEnumerable<Vector> cells)
        {
            return cells.SelectMany(GetNeighbours).All(c => this[c] == Cell.Empty);
        }
    }
}