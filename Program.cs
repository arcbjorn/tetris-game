using System;
using System.Collections.Generic;
using System.Threading;

namespace TetrisGame
{
    class Program
    {
        private const int Width = 12;
        private const int Height = 20;
        private const int GameSpeed = 500;

        private static int _score;
        private static int[,] _board;
        private static bool _gameOver;
        private static Tetromino _currentPiece;
        private static readonly Random _random = new Random();
        private static readonly object _lock = new object();

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            InitializeGame();
            DrawBorders();
            DrawBoard();

            var inputThread = new Thread(ReadInput);
            inputThread.Start();

            RunGameLoop();

            inputThread.Join();

            ShowGameOver(args);
        }

        private static void InitializeGame()
        {
            _board = new int[Height, Width];
            _score = 0;
            _gameOver = false;
            SpawnNewPiece();
        }

        private static void SpawnNewPiece()
        {
            var shapes = new int[][][]
            {
                new int[][] { new int[] {1,1,1,1} }, // I
                new int[][] { new int[] {1,1}, new int[] {1,1} }, // O
                new int[][] { new int[] {0,1,0}, new int[] {1,1,1} }, // T
                new int[][] { new int[] {1,0,0}, new int[] {1,1,1} }, // L
                new int[][] { new int[] {0,0,1}, new int[] {1,1,1} }, // J
                new int[][] { new int[] {0,1,1}, new int[] {1,1,0} }, // S
                new int[][] { new int[] {1,1,0}, new int[] {0,1,1} }  // Z
            };

            var shape = shapes[_random.Next(shapes.Length)];
            _currentPiece = new Tetromino(shape, Width / 2 - 2, 0);

            if (CheckCollision(_currentPiece.X, _currentPiece.Y, _currentPiece.Shape))
            {
                _gameOver = true;
            }
        }

        private static void RunGameLoop()
        {
            var lastDrop = DateTime.Now;

            while (!_gameOver)
            {
                if ((DateTime.Now - lastDrop).TotalMilliseconds >= GameSpeed)
                {
                    lock (_lock)
                    {
                        MovePieceDown();
                    }
                    lastDrop = DateTime.Now;
                }
                Thread.Sleep(50);
            }
        }

        private static void MovePieceDown()
        {
            if (!CheckCollision(_currentPiece.X, _currentPiece.Y + 1, _currentPiece.Shape))
            {
                ErasePiece();
                _currentPiece.Y++;
                DrawPiece();
            }
            else
            {
                LockPiece();
                ClearLines();
                SpawnNewPiece();
                DrawBoard();
            }
        }

        private static void ReadInput()
        {
            while (!_gameOver)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    lock (_lock)
                    {
                        HandleKeyPress(key);
                    }
                }
            }
        }

        private static void HandleKeyPress(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.LeftArrow:
                    if (!CheckCollision(_currentPiece.X - 1, _currentPiece.Y, _currentPiece.Shape))
                    {
                        ErasePiece();
                        _currentPiece.X--;
                        DrawPiece();
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (!CheckCollision(_currentPiece.X + 1, _currentPiece.Y, _currentPiece.Shape))
                    {
                        ErasePiece();
                        _currentPiece.X++;
                        DrawPiece();
                    }
                    break;
                case ConsoleKey.DownArrow:
                    MovePieceDown();
                    break;
                case ConsoleKey.UpArrow:
                    RotatePiece();
                    break;
                case ConsoleKey.Escape:
                    _gameOver = true;
                    break;
            }
        }

        private static void RotatePiece()
        {
            var rotated = RotateMatrix(_currentPiece.Shape);
            if (!CheckCollision(_currentPiece.X, _currentPiece.Y, rotated))
            {
                ErasePiece();
                _currentPiece.Shape = rotated;
                DrawPiece();
            }
        }

        private static int[][] RotateMatrix(int[][] matrix)
        {
            int rows = matrix.Length;
            int cols = matrix[0].Length;
            var rotated = new int[cols][];

            for (int i = 0; i < cols; i++)
            {
                rotated[i] = new int[rows];
                for (int j = 0; j < rows; j++)
                {
                    rotated[i][j] = matrix[rows - 1 - j][i];
                }
            }
            return rotated;
        }

        private static bool CheckCollision(int x, int y, int[][] shape)
        {
            for (int i = 0; i < shape.Length; i++)
            {
                for (int j = 0; j < shape[i].Length; j++)
                {
                    if (shape[i][j] == 1)
                    {
                        int boardX = x + j;
                        int boardY = y + i;

                        if (boardX < 0 || boardX >= Width || boardY >= Height)
                            return true;

                        if (boardY >= 0 && _board[boardY, boardX] == 1)
                            return true;
                    }
                }
            }
            return false;
        }

        private static void LockPiece()
        {
            for (int i = 0; i < _currentPiece.Shape.Length; i++)
            {
                for (int j = 0; j < _currentPiece.Shape[i].Length; j++)
                {
                    if (_currentPiece.Shape[i][j] == 1)
                    {
                        int boardX = _currentPiece.X + j;
                        int boardY = _currentPiece.Y + i;
                        if (boardY >= 0 && boardY < Height && boardX >= 0 && boardX < Width)
                        {
                            _board[boardY, boardX] = 1;
                        }
                    }
                }
            }
        }

        private static void ClearLines()
        {
            int linesCleared = 0;

            for (int y = Height - 1; y >= 0; y--)
            {
                bool fullLine = true;
                for (int x = 0; x < Width; x++)
                {
                    if (_board[y, x] == 0)
                    {
                        fullLine = false;
                        break;
                    }
                }

                if (fullLine)
                {
                    linesCleared++;
                    for (int yy = y; yy > 0; yy--)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            _board[yy, x] = _board[yy - 1, x];
                        }
                    }
                    y++;
                }
            }

            if (linesCleared > 0)
            {
                _score += linesCleared * 100;
                DrawScore();
            }
        }

        private static void ErasePiece()
        {
            DrawPieceInternal(' ');
        }

        private static void DrawPiece()
        {
            DrawPieceInternal('█');
        }

        private static void DrawPieceInternal(char c)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            for (int i = 0; i < _currentPiece.Shape.Length; i++)
            {
                for (int j = 0; j < _currentPiece.Shape[i].Length; j++)
                {
                    if (_currentPiece.Shape[i][j] == 1)
                    {
                        int screenX = _currentPiece.X + j + 1;
                        int screenY = _currentPiece.Y + i + 1;
                        if (screenY > 0 && screenY <= Height && screenX > 0 && screenX <= Width)
                        {
                            Console.SetCursorPosition(screenX, screenY);
                            Console.Write(c);
                        }
                    }
                }
            }
            Console.ResetColor();
        }

        private static void DrawBoard()
        {
            Console.ForegroundColor = ConsoleColor.White;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Console.SetCursorPosition(x + 1, y + 1);
                    Console.Write(_board[y, x] == 1 ? "█" : " ");
                }
            }
            DrawScore();
            Console.ResetColor();
        }

        private static void DrawBorders()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i <= Width + 1; i++)
            {
                Console.SetCursorPosition(i, 0);
                Console.Write("═");
                Console.SetCursorPosition(i, Height + 1);
                Console.Write("═");
            }

            for (int i = 0; i <= Height + 1; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("║");
                Console.SetCursorPosition(Width + 1, i);
                Console.Write("║");
            }

            Console.SetCursorPosition(0, 0);
            Console.Write("╔");
            Console.SetCursorPosition(Width + 1, 0);
            Console.Write("╗");
            Console.SetCursorPosition(0, Height + 1);
            Console.Write("╚");
            Console.SetCursorPosition(Width + 1, Height + 1);
            Console.Write("╝");

            Console.ResetColor();
        }

        private static void DrawScore()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition(Width + 4, 2);
            Console.Write($"Score: {_score}");
            Console.SetCursorPosition(Width + 4, 4);
            Console.Write("← → Move");
            Console.SetCursorPosition(Width + 4, 5);
            Console.Write("↑ Rotate");
            Console.SetCursorPosition(Width + 4, 6);
            Console.Write("↓ Drop");
            Console.SetCursorPosition(Width + 4, 7);
            Console.Write("ESC Quit");
            Console.ResetColor();
        }

        private static void ShowGameOver(string[] args)
        {
            Console.SetCursorPosition(Width / 2 - 3, Height / 2);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("GAME OVER!");
            Console.SetCursorPosition(Width / 2 - 5, Height / 2 + 1);
            Console.WriteLine($"Score: {_score}");
            Console.SetCursorPosition(Width / 2 - 10, Height / 2 + 3);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press R to Restart or ESC to Quit");
            Console.ResetColor();

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.R)
                {
                    Main(args);
                    return;
                }
                else if (key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            Console.SetCursorPosition(0, Height + 3);
            Console.CursorVisible = true;
        }

        class Tetromino
        {
            public int[][] Shape { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

            public Tetromino(int[][] shape, int x, int y)
            {
                Shape = shape;
                X = x;
                Y = y;
            }
        }
    }
}
