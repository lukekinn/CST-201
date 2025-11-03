using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleShip
{
    class Program
    {
        static void Main(string[] args)
        {
            var game = new Game();
            game.Setup();
            game.Play();
        }
    }

    // Simple immutable point (value type so Point? is Nullable<Point> and has .Value)
    record struct Point(int R, int C);

    enum Cell { Empty, Miss, Hit }
    enum ShipType { Destroyer, Submarine, Cruiser }

    class Ship
    {
        public ShipType Type;
        public List<Point> Cells = new();
        public HashSet<(int r, int c)> Hits = new();
        public bool IsSunk => Hits.Count >= Cells.Count;
    }

    class Board
    {
        public const int Size = 10;
        public int[,] ShipIndex = new int[Size, Size];   // -1 = no ship, otherwise index into Ships
        public Cell[,] Shots = new Cell[Size, Size];
        public List<Ship> Ships = new();

        public Board()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    ShipIndex[r, c] = -1;
        }

        public bool AllSunk() => Ships.All(s => s.IsSunk);
    }

    class Game
    {
        Board player = new();
        Board computer = new();
        Random rng = new();

        public void Setup()
        {
            Console.WriteLine("Welcome to BattleShip: Face off against the computer in a game of strategy and luck!");
            Console.WriteLine("The objective is to sink all of your opponent's ships before they sink yours.");
            Console.WriteLine("***********************************************************************************************************");
            Console.WriteLine("Proverbs 15:18 A hot-tempered person stirs up conflict, but the one who is patient calms a quarrel");
            Console.WriteLine("***********************************************************************************************************");
            Console.WriteLine("You and the computer will each place three ships on a 10x10 grid.");
            Console.WriteLine("Place your ships. You have a Destroyer (a 2x2 cube), Submarine (diagonal 3), and Cruiser (straight 3 line).");
            PlacePlayerShips();
            PlaceComputerShipsRandom();
            Console.WriteLine("Prepare for battle!");
        }

        void PlacePlayerShips()
        {
            PlaceShipInteractive(player, ShipType.Destroyer);
            PlaceShipInteractive(player, ShipType.Submarine);
            PlaceShipInteractive(player, ShipType.Cruiser);
        }

        void PlaceShipInteractive(Board b, ShipType kind)
        {
            Console.WriteLine($"\nPlacing {kind}:");
            while (true)
            {
                Console.Write("Enter start coordinate (e.g., A1 or '1 1' (this will place your ships onto the game board)):");
                var p = ReadCoord();
                if (p == null) { Console.WriteLine("Invalid coordinate. Try again."); continue; }

                List<Point>? cells = kind switch
                {
                    ShipType.Destroyer => GetDestroyerCells(p.Value),
                    ShipType.Submarine => GetSubmarineCellsInteractive(p.Value),
                    ShipType.Cruiser   => GetCruiserCellsInteractive(p.Value),
                    _ => null
                };

                if (cells == null) continue; // preventing ship misplacement from user error
                if (cells.Any(pt => !InBounds(pt))) { Console.WriteLine("Ship would be out of bounds. Try again.");  continue; }
                if (cells.Any(pt => b.ShipIndex[pt.R, pt.C] != -1)) { Console.WriteLine("Collides with an existing ship. Try again."); continue; }

                AddShipToBoard(b, kind, cells);
                Console.WriteLine($"{kind} placed.");
                break;
            }
        }

        // Destroyer: 2x2 square using top-left
        List<Point> GetDestroyerCells(Point topLeft)
        {
            return new List<Point>
            {
                new(topLeft.R, topLeft.C),
                new(topLeft.R, topLeft.C + 1),
                new(topLeft.R + 1, topLeft.C),
                new(topLeft.R + 1, topLeft.C + 1)
            };
        }

        // Submarine: choose SE or SW diagonal from start
        List<Point>? GetSubmarineCellsInteractive(Point start)
        {
            Console.Write("Direction (1 = SE, 2 = SW): ");
            var dir = Console.ReadLine()?.Trim();
            if (dir != "1" && dir != "2") { Console.WriteLine("Invalid direction."); return null; }

            if (dir == "1")
                return new List<Point> { start, new(start.R + 1, start.C + 1), new(start.R + 2, start.C + 2) };
            else
                return new List<Point> { start, new(start.R + 1, start.C - 1), new(start.R + 2, start.C - 2) };
        }

        // Cruiser: straight 3 in N/S/E/W from start
        List<Point>? GetCruiserCellsInteractive(Point start)
        {
            Console.Write("Orientation (N/S/E/W): ");
            var o = Console.ReadLine()?.Trim().ToUpper();
            if (o is not ("N" or "S" or "E" or "W")) { Console.WriteLine("Invalid orientation."); return null; }

            return o switch
            {
                "N" => new List<Point> { start, new(start.R - 1, start.C), new(start.R - 2, start.C) },
                "S" => new List<Point> { start, new(start.R + 1, start.C), new(start.R + 2, start.C) },
                "E" => new List<Point> { start, new(start.R, start.C + 1), new(start.R, start.C + 2) },
                "W" => new List<Point> { start, new(start.R, start.C - 1), new(start.R, start.C - 2) },
                _ => null
            };
        }

        void PlaceComputerShipsRandom()
        {
            // Destroyer
            while (true)
            {
                int r = rng.Next(0, Board.Size - 1), c = rng.Next(0, Board.Size - 1);
                var cells = new List<Point> { new(r, c), new(r, c + 1), new(r + 1, c), new(r + 1, c + 1) };
                if (cells.Any(p => computer.ShipIndex[p.R, p.C] != -1)) continue;
                AddShipToBoard(computer, ShipType.Destroyer, cells);
                break;
            }

            // Submarine (SE or SW)
            while (true)
            {
                int r = rng.Next(0, Board.Size), c = rng.Next(0, Board.Size);
                bool se = rng.Next(2) == 0;
                var cells = se
                    ? new List<Point> { new(r, c), new(r + 1, c + 1), new(r + 2, c + 2) }
                    : new List<Point> { new(r, c), new(r + 1, c - 1), new(r + 2, c - 2) };

                if (cells.Any(p => !InBounds(p) || computer.ShipIndex[p.R, p.C] != -1)) continue;
                AddShipToBoard(computer, ShipType.Submarine, cells);
                break;
            }

            // Cruiser
            while (true)
            {
                int r = rng.Next(0, Board.Size), c = rng.Next(0, Board.Size), dir = rng.Next(4);
                List<Point> cells = dir switch
                {
                    0 => new List<Point> { new(r, c), new(r - 1, c), new(r - 2, c) },
                    1 => new List<Point> { new(r, c), new(r + 1, c), new(r + 2, c) },
                    2 => new List<Point> { new(r, c), new(r, c + 1), new(r, c + 2) },
                    _ => new List<Point> { new(r, c), new(r, c - 1), new(r, c - 2) },
                };

                if (cells.Any(p => !InBounds(p) || computer.ShipIndex[p.R, p.C] != -1)) continue;
                AddShipToBoard(computer, ShipType.Cruiser, cells);
                break;
            }

            Console.WriteLine("Computer ships placed.");
        }

        void AddShipToBoard(Board b, ShipType kind, List<Point> cells)
        {
            var ship = new Ship { Type = kind, Cells = cells };
            b.Ships.Add(ship);
            int idx = b.Ships.Count - 1;
            foreach (var p in cells) b.ShipIndex[p.R, p.C] = idx;
        }

        public void Play()
        {
            bool playerTurn = true;
            while (!player.AllSunk() && !computer.AllSunk())
            {
                if (playerTurn)
                {
                    PrintOpponentView();
                    Console.Write("Enter shot coordinate: ");
                    var coord = ReadCoord();
                    if (coord == null) { Console.WriteLine("Invalid coordinate. Try again."); continue; }
                    if (computer.Shots[coord.Value.R, coord.Value.C] != Cell.Empty) { Console.WriteLine("Already tried that cell."); continue; }

                    bool hit = ApplyShot(computer, coord.Value, isPlayerShot: true);
                    if (!hit) playerTurn = false;
                    else if (computer.AllSunk()) { Console.WriteLine("You win!  "); break; }
                }
                else
                {
                    // naive AI: first untried on player's board
                    Point shot = FirstUntriedOnPlayer();
                    Console.WriteLine($"Computer shoots {CoordToString(shot)}");
                    bool hit = ApplyShot(player, shot, isPlayerShot: false);
                    if (!hit) playerTurn = true;
                    else if (player.AllSunk()) { Console.WriteLine("Computer wins."); break; }
                }
            }

            Console.WriteLine("Game over. Final boards:");
            Console.WriteLine("I have fought the good fight, I have finished the race, I have kept the faith – 2 Timothy 4:7");
            PrintBoard(player, showShips: true);
            PrintBoard(computer, showShips: true);
        }

        Point FirstUntriedOnPlayer()
        {
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                    if (player.Shots[r, c] == Cell.Empty)
                        return new Point(r, c);

            // fallback (shouldn’t hit)
            while (true)
            {
                int r = rng.Next(Board.Size), c = rng.Next(Board.Size);
                if (player.Shots[r, c] == Cell.Empty) return new Point(r, c);
            }
        }

        bool ApplyShot(Board target, Point shot, bool isPlayerShot)
        {
            if (target.ShipIndex[shot.R, shot.C] == -1)
            {
                target.Shots[shot.R, shot.C] = Cell.Miss;
                Console.WriteLine(isPlayerShot ? "Miss." : "Computer missed.");
                return false;
            }

            int idx = target.ShipIndex[shot.R, shot.C];
            var ship = target.Ships[idx];
            ship.Hits.Add((shot.R, shot.C));
            target.Shots[shot.R, shot.C] = Cell.Hit;

            Console.WriteLine(isPlayerShot ? $"Hit {ship.Type}!" : $"Computer hit your {ship.Type}! Blessed is the one who endures trials, because when he has stood the test he will receive the crown of life that God has promised to those who love him – James 1:12");
            if (ship.IsSunk) Console.WriteLine(isPlayerShot ? $"{ship.Type} sunk!" : $"Your {ship.Type} was sunk! But as for you, be strong; don’t give up, for your work has a reward – 2 Chronicles 15:7");
            return true;
        }

        // I/O + rendering helpers
        Point? ReadCoord()
        {
            var raw = Console.ReadLine()?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // A1 style
            if (char.IsLetter(raw[0]))
            {
                char row = raw[0];
                if (!int.TryParse(raw.Substring(1), out var col)) return null;
                int r = row - 'A', c = col - 1;
                var p = new Point(r, c);
                return InBounds(p) ? p : null;
            }

            // "r c" style
            var parts = raw.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var r0) && int.TryParse(parts[1], out var c0))
            {
                var p = new Point(r0 - 1, c0 - 1);
                return InBounds(p) ? p : null;
            }

            return null;
        }

        bool InBounds(Point p) => p.R >= 0 && p.R < Board.Size && p.C >= 0 && p.C < Board.Size;

        void PrintOpponentView()
        {
            Console.WriteLine("\nOpponent board (what you know):");
            PrintBoard(computer, showShips: false);
        }

        void PrintBoard(Board b, bool showShips)
        {
            Console.Write("   ");
            for (int c = 1; c <= Board.Size; c++) Console.Write($"{c,2} ");
            Console.WriteLine();

            for (int r = 0; r < Board.Size; r++)
            {
                Console.Write((char)('A' + r) + "  ");
                for (int c = 0; c < Board.Size; c++)
                {
                    char ch = '.';
                    if (b.Shots[r, c] == Cell.Miss) ch = 'o';
                    else if (b.Shots[r, c] == Cell.Hit) ch = 'X';
                    else if (showShips && b.ShipIndex[r, c] != -1) ch = 'S';
                    Console.Write($" {ch} ");
                }
                Console.WriteLine();
            }
        }

        string CoordToString(Point p) => $"{(char)('A' + p.R)}{p.C + 1}";
    }
}
