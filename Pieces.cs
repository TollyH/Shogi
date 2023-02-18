using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;

namespace Shogi.Pieces
{
    public abstract class Piece
    {
        public static readonly Dictionary<Type, Type> PromotionMap = new()
        {
            { typeof(SilverGeneral), typeof(PromotedSilverGeneral) },
            { typeof(Rook), typeof(PromotedRook) },
            { typeof(Bishop), typeof(PromotedBishop) },
            { typeof(Knight), typeof(PromotedKnight) },
            { typeof(Lance), typeof(PromotedLance) },
            { typeof(Pawn), typeof(PromotedPawn) }
        };
        public static readonly Dictionary<Type, Type> DemotionMap = new()
        {
            { typeof(PromotedSilverGeneral), typeof(SilverGeneral) },
            { typeof(PromotedRook), typeof(Rook) },
            { typeof(PromotedBishop), typeof(Bishop) },
            { typeof(PromotedKnight), typeof(Knight) },
            { typeof(PromotedLance), typeof(Lance) },
            { typeof(PromotedPawn), typeof(Pawn) }
        };

        public abstract string Name { get; }
        public abstract char SymbolLetter { get; }
        // TODO: Remove once image rendering is implemented
        public char SymbolSpecial => '?';
        public abstract double Value { get; }
        public abstract bool IsSente { get; }
        public abstract Point Position { get; protected set; }

        /// <summary>
        /// Get a set of all moves that this piece can perform on the given board
        /// </summary>
        /// <param name="enforceCheckLegality">Whether or not moves that would put own king into check are discounted</param>
        public abstract HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality);

        /// <summary>
        /// Create a clone of a shogi piece, allowing modification of the clone without affecting the original
        /// </summary>
        public abstract Piece Clone();

        /// <summary>
        /// Move this piece to a square on the board
        /// </summary>
        /// <returns><see langword="true"/> if the move was valid and executed, <see langword="false"/> otherwise</returns>
        /// <remarks>This method will ensure that the move is valid, unless <paramref name="bypassValidity"/> is <see langword="true"/></remarks>
        public bool Move(Piece?[,] board, Point target, bool bypassValidity = false)
        {
            if (bypassValidity || GetValidMoves(board, true).Contains(target))
            {
                Position = target;
                return true;
            }
            return false;
        }
    }

    public class King : Piece
    {
        public override string Name => "King";
        public override char SymbolLetter { get; }
        // King should not contribute to overall board value, as it always present
        public override double Value => 0;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public King(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
            SymbolLetter = isSente ? '玉' : '王';
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy != 0 || dx != 0)
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente, m));
            }
            return moves;
        }

        public override King Clone()
        {
            return new King(Position, IsSente);
        }
    }

    public class GoldGeneral : Piece
    {
        public override string Name => "Gold General";
        public override char SymbolLetter => '金';
        public override double Value => 11;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public GoldGeneral(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int backwardsY = IsSente ? -1 : 1;
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dy != 0 || dx != 0) && (dy != backwardsY || dx == 0))
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override GoldGeneral Clone()
        {
            return new GoldGeneral(Position, IsSente);
        }
    }

    public class SilverGeneral : Piece
    {
        public override string Name => "Silver General";
        public override char SymbolLetter => '銀';
        public override double Value => 10;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public SilverGeneral(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int backwardsY = IsSente ? -1 : 1;
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dy != 0 || dx != 0) && (dy != backwardsY || dx == 0) && dy != 0)
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override SilverGeneral Clone()
        {
            return new SilverGeneral(Position, IsSente);
        }
    }

    public class PromotedSilverGeneral : Piece
    {
        public override string Name => "Promoted Silver General";
        public override char SymbolLetter => '全';
        public override double Value => 11;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public PromotedSilverGeneral(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int backwardsY = IsSente ? -1 : 1;
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dy != 0 || dx != 0) && (dy != backwardsY || dx == 0))
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override PromotedSilverGeneral Clone()
        {
            return new PromotedSilverGeneral(Position, IsSente);
        }
    }

    public class Rook : Piece
    {
        public override string Name => "Rook";
        public override char SymbolLetter => '飛';
        public override double Value => 19;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public Rook(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            HashSet<Point> moves = new();
            // Right
            for (int dx = Position.X + 1; dx < board.GetLength(0); dx++)
            {
                Point newPos = new(dx, Position.Y);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Left
            for (int dx = Position.X - 1; dx >= 0; dx--)
            {
                Point newPos = new(dx, Position.Y);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Up
            for (int dy = Position.Y + 1; dy < board.GetLength(1); dy++)
            {
                Point newPos = new(Position.X, dy);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Down
            for (int dy = Position.Y - 1; dy >= 0; dy--)
            {
                Point newPos = new(Position.X, dy);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override Rook Clone()
        {
            return new Rook(Position, IsSente);
        }
    }

    public class PromotedRook : Piece
    {
        public override string Name => "Promoted Rook";
        public override char SymbolLetter => '龍';
        public override double Value => 22;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public PromotedRook(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy != 0 || dx != 0)
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            // Right
            for (int dx = Position.X + 1; dx < board.GetLength(0); dx++)
            {
                Point newPos = new(dx, Position.Y);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Left
            for (int dx = Position.X - 1; dx >= 0; dx--)
            {
                Point newPos = new(dx, Position.Y);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Up
            for (int dy = Position.Y + 1; dy < board.GetLength(1); dy++)
            {
                Point newPos = new(Position.X, dy);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Down
            for (int dy = Position.Y - 1; dy >= 0; dy--)
            {
                Point newPos = new(Position.X, dy);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override PromotedRook Clone()
        {
            return new PromotedRook(Position, IsSente);
        }
    }

    public class Bishop : Piece
    {
        public override string Name => "Bishop";
        public override char SymbolLetter => '角';
        public override double Value => 17;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public Bishop(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            HashSet<Point> moves = new();
            // Diagonal Up Right
            for (int dif = 1; Position.X + dif < board.GetLength(0) && Position.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(Position.X + dif, Position.Y + dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Diagonal Up Left
            for (int dif = 1; Position.X - dif >= 0 && Position.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(Position.X - dif, Position.Y + dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Diagonal Down Left
            for (int dif = 1; Position.X - dif >= 0 && Position.Y - dif >= 0; dif++)
            {
                Point newPos = new(Position.X - dif, Position.Y - dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Diagonal Down Right
            for (int dif = 1; Position.X + dif < board.GetLength(0) && Position.Y - dif >= 0; dif++)
            {
                Point newPos = new(Position.X + dif, Position.Y - dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override Bishop Clone()
        {
            return new Bishop(Position, IsSente);
        }
    }

    public class PromotedBishop : Piece
    {
        public override string Name => "Promoted Bishop";
        public override char SymbolLetter => '馬';
        public override double Value => 20;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public PromotedBishop(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy != 0 || dx != 0)
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            // Diagonal Up Right
            for (int dif = 1; Position.X + dif < board.GetLength(0) && Position.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(Position.X + dif, Position.Y + dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Diagonal Up Left
            for (int dif = 1; Position.X - dif >= 0 && Position.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(Position.X - dif, Position.Y + dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Diagonal Down Left
            for (int dif = 1; Position.X - dif >= 0 && Position.Y - dif >= 0; dif++)
            {
                Point newPos = new(Position.X - dif, Position.Y - dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            // Diagonal Down Right
            for (int dif = 1; Position.X + dif < board.GetLength(0) && Position.Y - dif >= 0; dif++)
            {
                Point newPos = new(Position.X + dif, Position.Y - dif);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override PromotedBishop Clone()
        {
            return new PromotedBishop(Position, IsSente);
        }
    }

    public class Knight : Piece
    {
        public override string Name => "Knight";
        public override char SymbolLetter => '桂';
        public override double Value => 6;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public Knight(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int dy = IsSente ? 2 : -2;
            HashSet<Point> moves = new();

            Point newPos = new(Position.X + 1, Position.Y + dy);
            if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
            {
                _ = moves.Add(newPos);
            }

            newPos = new(Position.X - 1, Position.Y + dy);
            if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
            {
                _ = moves.Add(newPos);
            }

            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override Knight Clone()
        {
            return new Knight(Position, IsSente);
        }
    }

    public class PromotedKnight : Piece
    {
        public override string Name => "Promoted Knight";
        public override char SymbolLetter => '圭';
        public override double Value => 11;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public PromotedKnight(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int backwardsY = IsSente ? -1 : 1;
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dy != 0 || dx != 0) && (dy != backwardsY || dx == 0))
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override PromotedKnight Clone()
        {
            return new PromotedKnight(Position, IsSente);
        }
    }

    public class Lance : Piece
    {
        public static readonly ImmutableArray<Point> Moves = new Point[8]
        {
            new(1, 2), new(1, -2), new(-1, 2), new(-1, -2), new(2, 1), new(2, -1), new(-2, 1), new(-2, -1)
        }.ToImmutableArray();

        public override string Name => "Lance";
        public override char SymbolLetter => '香';
        public override double Value => 6;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public Lance(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int yChange = IsSente ? 1 : -1;
            HashSet<Point> moves = new();
            for (int dy = Position.Y + yChange; dy < board.GetLength(1) && dy >= 0; dy += yChange)
            {
                Point newPos = new(Position.X, dy);
                if (board[newPos.X, newPos.Y] is null)
                {
                    _ = moves.Add(newPos);
                }
                else
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != IsSente)
                    {
                        _ = moves.Add(newPos);
                    }
                    break;
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override Lance Clone()
        {
            return new Lance(Position, IsSente);
        }
    }

    public class PromotedLance : Piece
    {
        public static readonly ImmutableArray<Point> Moves = new Point[8]
        {
            new(1, 2), new(1, -2), new(-1, 2), new(-1, -2), new(2, 1), new(2, -1), new(-2, 1), new(-2, -1)
        }.ToImmutableArray();

        public override string Name => "Promoted Lance";
        public override char SymbolLetter => '杏';
        public override double Value => 11;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public PromotedLance(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int backwardsY = IsSente ? -1 : 1;
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dy != 0 || dx != 0) && (dy != backwardsY || dx == 0))
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override Lance Clone()
        {
            return new Lance(Position, IsSente);
        }
    }

    public class Pawn : Piece
    {
        public override string Name => "Pawn";
        public override char SymbolLetter => '歩';
        public override double Value => 1;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public Pawn(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            HashSet<Point> moves = new();
            int checkY = Position.Y + (IsSente ? 1 : -1);
            if (board[Position.X, checkY] is null || board[Position.X, checkY]!.IsSente != IsSente)
            {
                _ = moves.Add(new Point(Position.X, checkY));
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override Pawn Clone()
        {
            return new Pawn(Position, IsSente);
        }
    }

    public class PromotedPawn : Piece
    {
        public override string Name => "Promoted Pawn";
        public override char SymbolLetter => 'と';
        public override double Value => 11;
        public override bool IsSente { get; }
        public override Point Position { get; protected set; }

        public PromotedPawn(Point position, bool isSente)
        {
            Position = position;
            IsSente = isSente;
        }

        public override HashSet<Point> GetValidMoves(Piece?[,] board, bool enforceCheckLegality)
        {
            int backwardsY = IsSente ? -1 : 1;
            HashSet<Point> moves = new();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dy != 0 || dx != 0) && (dy != backwardsY || dx == 0))
                    {
                        Point newPos = new(Position.X + dx, Position.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && (board[newPos.X, newPos.Y] is null || board[newPos.X, newPos.Y]!.IsSente != IsSente))
                        {
                            _ = moves.Add(newPos);
                        }
                    }
                }
            }
            if (enforceCheckLegality)
            {
                _ = moves.RemoveWhere(m => BoardAnalysis.IsKingReachable(board.AfterMove(Position, m), IsSente));
            }
            return moves;
        }

        public override PromotedPawn Clone()
        {
            return new PromotedPawn(Position, IsSente);
        }
    }
}
