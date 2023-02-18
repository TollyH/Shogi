﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Shogi
{
    /// <remarks>
    /// CheckSente and CheckMateSente mean that the check is against sente,
    /// or that sente has lost respectively, and vice versa.
    /// </remarks>
    public enum GameState
    {
        StandardPlay,
        DrawStalemate,
        DrawFiftyMove,
        DrawThreeFold,
        DrawInsufficientMaterial,
        CheckSente,
        CheckGote,
        CheckMateSente,
        CheckMateGote
    }

    public class ShogiGame
    {
        public static readonly ImmutableHashSet<GameState> EndingStates = new HashSet<GameState>()
        {
            GameState.DrawFiftyMove,
            GameState.DrawStalemate,
            GameState.DrawThreeFold,
            GameState.DrawInsufficientMaterial,
            GameState.CheckMateSente,
            GameState.CheckMateGote
        }.ToImmutableHashSet();

        public Pieces.Piece?[,] Board { get; }
        public string InitialState { get; }

        public Pieces.King SenteKing { get; }
        public Pieces.King GoteKing { get; }

        public bool CurrentTurnSente { get; private set; }
        public bool GameOver { get; private set; }
        public bool AwaitingPromotionResponse { get; private set; }

        /// <summary>
        /// A list of the moves made this game as (sourcePosition, destinationPosition)
        /// </summary>
        public List<(Point, Point)> Moves { get; }
        public List<string> MoveText { get; }
        public List<Pieces.Piece> CapturedPieces { get; }

        // Used for the 50-move rule
        public int StaleMoveCounter { get; private set; }
        // Used to detect three-fold repetition
        public Dictionary<string, int> BoardCounts { get; }

        /// <summary>
        /// Create a new standard shogi game with all values at their defaults
        /// </summary>
        public ShogiGame()
        {
            CurrentTurnSente = true;
            GameOver = false;
            AwaitingPromotionResponse = false;

            SenteKing = new Pieces.King(new Point(4, 0), true);
            GoteKing = new Pieces.King(new Point(4, 7), false);

            Moves = new List<(Point, Point)>();
            MoveText = new List<string>();
            CapturedPieces = new List<Pieces.Piece>();

            StaleMoveCounter = 0;
            BoardCounts = new Dictionary<string, int>();

            Board = new Pieces.Piece?[8, 8]
            {
                { new Pieces.Rook(new Point(0, 0), true), new Pieces.Pawn(new Point(0, 1), true), null, null, null, null, new Pieces.Pawn(new Point(0, 6), false), new Pieces.Rook(new Point(0, 7), false) },
                { new Pieces.Knight(new Point(1, 0), true), new Pieces.Pawn(new Point(1, 1), true), null, null, null, null, new Pieces.Pawn(new Point(1, 6), false), new Pieces.Knight(new Point(1, 7), false) },
                { new Pieces.Bishop(new Point(2, 0), true), new Pieces.Pawn(new Point(2, 1), true), null, null, null, null, new Pieces.Pawn(new Point(2, 6), false), new Pieces.Bishop(new Point(2, 7), false) },
                { new Pieces.Queen(new Point(3, 0), true), new Pieces.Pawn(new Point(3, 1), true), null, null, null, null, new Pieces.Pawn(new Point(3, 6), false), new Pieces.Queen(new Point(3, 7), false) },
                { SenteKing, new Pieces.Pawn(new Point(4, 1), true), null, null, null, null, new Pieces.Pawn(new Point(4, 6), false), GoteKing },
                { new Pieces.Bishop(new Point(5, 0), true), new Pieces.Pawn(new Point(5, 1), true), null, null, null, null, new Pieces.Pawn(new Point(5, 6), false), new Pieces.Bishop(new Point(5, 7), false) },
                { new Pieces.Knight(new Point(6, 0), true), new Pieces.Pawn(new Point(6, 1), true), null, null, null, null, new Pieces.Pawn(new Point(6, 6), false), new Pieces.Knight(new Point(6, 7), false) },
                { new Pieces.Rook(new Point(7, 0), true), new Pieces.Pawn(new Point(7, 1), true), null, null, null, null, new Pieces.Pawn(new Point(7, 6), false), new Pieces.Rook(new Point(7, 7), false) }
            };

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a shogi game, setting each game parameter to a non-default value
        /// </summary>
        public ShogiGame(Pieces.Piece?[,] board, bool currentTurnSente, bool gameOver, List<(Point, Point)> moves, List<string> moveText,
            List<Pieces.Piece> capturedPieces, int staleMoveCounter, Dictionary<string, int> boardCounts,
            string? initialState)
        {
            if (board.GetLength(0) != 8 || board.GetLength(1) != 8)
            {
                throw new ArgumentException("Boards must be 8x8 in size");
            }

            Board = board;
            SenteKing = Board.OfType<Pieces.King>().Where(k => k.IsSente).First();
            GoteKing = Board.OfType<Pieces.King>().Where(k => !k.IsSente).First();

            CurrentTurnSente = currentTurnSente;
            GameOver = gameOver;
            Moves = moves;
            MoveText = moveText;
            CapturedPieces = capturedPieces;
            StaleMoveCounter = staleMoveCounter;
            BoardCounts = boardCounts;

            InitialState = initialState ?? ToString();
        }

        /// <summary>
        /// Create a deep copy of all parameters to this shogi game
        /// </summary>
        public ShogiGame Clone()
        {
            Pieces.Piece?[,] boardClone = new Pieces.Piece?[Board.GetLength(0), Board.GetLength(1)];
            for (int x = 0; x < boardClone.GetLength(0); x++)
            {
                for (int y = 0; y < boardClone.GetLength(1); y++)
                {
                    boardClone[x, y] = Board[x, y]?.Clone();
                }
            }

            return new ShogiGame(boardClone, CurrentTurnSente, GameOver, new(Moves), new(MoveText),
                CapturedPieces.Select(c => c.Clone()).ToList(), StaleMoveCounter,
                new(BoardCounts), InitialState);
        }

        /// <summary>
        /// Determine the current state of the game.
        /// </summary>
        /// <remarks>
        /// This method is similar to <see cref="BoardAnalysis.DetermineGameState"/>,
        /// however it can also detect the 50-move rule and three-fold repetition.
        /// </remarks>
        public GameState DetermineGameState()
        {
            GameState staticState = BoardAnalysis.DetermineGameState(Board, CurrentTurnSente,
                SenteKing.Position, GoteKing.Position);
            if (EndingStates.Contains(staticState))
            {
                return staticState;
            }
            if (BoardCounts.GetValueOrDefault(ToString(true)) >= 3)
            {
                return GameState.DrawThreeFold;
            }
            // 100 because the 50-move rule needs 50 stale moves from *each* player
            if (StaleMoveCounter >= 100)
            {
                return GameState.DrawFiftyMove;
            }
            return staticState;
        }

        /// <summary>
        /// Move a piece on the board from a <paramref name="source"/> coordinate to a <paramref name="destination"/> coordinate.
        /// </summary>
        /// <param name="doPromotion">
        /// If a piece can be promoted, should it be? <see langword="null"/> means the user should be prompted.
        /// </param>
        /// <param name="updateMoveText">
        /// Whether the move should update the game move text. This should usually be <see langword="true"/>,
        /// but may be set to <see langword="false"/> for performance optimisations in clone games for analysis.
        /// </param>
        /// <returns><see langword="true"/> if the move was valid and executed, <see langword="false"/> otherwise</returns>
        /// <remarks>This method will check if the move is completely valid, unless <paramref name="forceMove"/> is <see langword="true"/>. No other validity checks are required.</remarks>
        public bool MovePiece(Point source, Point destination, bool forceMove = false, bool? doPromotion = null, bool updateMoveText = true)
        {
            if (!forceMove && GameOver)
            {
                return false;
            }

            Pieces.Piece? piece = Board[source.X, source.Y];
            if (piece is null)
            {
                return false;
            }
            if (!forceMove && piece.IsSente != CurrentTurnSente)
            {
                return false;
            }

            // Used for generating new move text
            ShogiGame? oldGame = null;
            if (updateMoveText)
            {
                oldGame = Clone();
            }

            bool pieceMoved = piece.Move(Board, destination, forceMove);

            if (pieceMoved)
            {
                StaleMoveCounter++;
                Moves.Add((source, destination));
                if (Board[destination.X, destination.Y] is not null)
                {
                    CapturedPieces.Add(Board[destination.X, destination.Y]!);
                    StaleMoveCounter = 0;
                }

                Type pieceType = piece.GetType();
                if (Pieces.Piece.PromotionMap.ContainsKey(pieceType))
                {
                    if (piece.IsSente ? destination.Y >= 6 : destination.Y <= 2)
                    {
                        if ((piece is Pieces.Pawn or Pieces.Lance && destination.Y is 0 or 8)
                            || (piece is Pieces.Knight && destination.Y is >= 7 or <= 1))
                        {
                            // Always promote pawns and lances upon reaching the last rank
                            // Always promote knights upon reaching the last two ranks
                            doPromotion = true;
                        }
                        doPromotion ??= System.Windows.MessageBox.Show(
                            $"Do you want to promote the {piece.Name} you just moved?", "Promotion",
                            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question
                        ) == System.Windows.MessageBoxResult.Yes;
                        if (doPromotion.Value)
                        {
                            piece = (Pieces.Piece)Activator.CreateInstance(Pieces.Piece.PromotionMap[pieceType], piece.Position, piece.IsSente)!;
                        }
                        Board[source.X, source.Y] = piece;
                    }
                }

                Board[destination.X, destination.Y] = piece;
                Board[source.X, source.Y] = null;

                CurrentTurnSente = !CurrentTurnSente;

                string newBoardString = ToString(true);
                if (BoardCounts.ContainsKey(newBoardString))
                {
                    BoardCounts[newBoardString]++;
                }
                else
                {
                    BoardCounts[newBoardString] = 1;
                }
                GameOver = EndingStates.Contains(DetermineGameState());

                if (updateMoveText)
                {
                    string newMove = destination.ToShogiCoordinate();
                    if (oldGame!.Board[source.X, source.Y] is Pieces.Pawn)
                    {
                        if (oldGame!.Board[destination.X, destination.Y] is not null)
                        {
                            newMove = source.ToShogiCoordinate()[0] + "x" + newMove;
                        }
                        if (destination.Y == (piece.IsSente ? 7 : 0))
                        {
                            newMove += "=" + piece.SymbolLetter;
                        }
                    }
                    else
                    {
                        if (oldGame!.Board[destination.X, destination.Y] is not null)
                        {
                            newMove = 'x' + newMove;
                        }

                        // Disambiguate moving piece if two pieces of the same type can reach destination
                        IEnumerable<Pieces.Piece> canReachDest = oldGame.Board.OfType<Pieces.Piece>().Where(
                            p => piece.GetType() == p.GetType() && p.Position != source && p.IsSente == piece.IsSente
                                && p.GetValidMoves(oldGame.Board, true).Contains(destination));
                        if (canReachDest.Any())
                        {
                            bool success = false;
                            string coordinate = source.ToShogiCoordinate();
                            // Other pieces on same file, disambiguate with rank
                            if (canReachDest.Where(p => p.Position.X == source.X).Any())
                            {
                                success = true;
                                newMove = coordinate[1] + newMove;
                            }
                            // Other pieces on same rank, disambiguate with file
                            if (canReachDest.Where(p => p.Position.Y == source.Y).Any())
                            {
                                success = true;
                                newMove = coordinate[0] + newMove;
                            }
                            if (!success)
                            {
                                // Pieces are on different rank and file, but can reach same square
                                // Prefer disambiguating with file
                                newMove = coordinate[0] + newMove;
                            }
                        }
                        newMove = piece.SymbolLetter + newMove;
                    }

                    GameState state = DetermineGameState();
                    if (state is GameState.CheckSente or GameState.CheckGote)
                    {
                        newMove += '+';
                    }
                    else if (state is GameState.CheckMateSente or GameState.CheckMateGote)
                    {
                        newMove += '#';
                    }

                    MoveText.Add(newMove);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get a string representation of the given board.
        /// </summary>
        /// <remarks>The resulting string complies with the Forsyth–Edwards Notation standard</remarks>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Get a string representation of the given board.
        /// </summary>
        /// <remarks>
        /// The resulting string complies with the Forsyth–Edwards Notation standard,
        /// unless <paramref name="omitMoveCounts"/> is <see langword="true"/>
        /// </remarks>
        public string ToString(bool omitMoveCounts)
        {
            StringBuilder result = new(90);

            for (int y = Board.GetLength(1) - 1; y >= 0; y--)
            {
                int consecutiveNull = 0;
                for (int x = 0; x < Board.GetLength(0); x++)
                {
                    Pieces.Piece? piece = Board[x, y];
                    if (piece is null)
                    {
                        consecutiveNull++;
                    }
                    else
                    {
                        if (consecutiveNull > 0)
                        {
                            _ = result.Append(consecutiveNull);
                            consecutiveNull = 0;
                        }
                        _ = result.Append(piece.IsSente ? char.ToUpper(piece.SymbolLetter) : char.ToLower(piece.SymbolLetter));
                    }
                }
                if (consecutiveNull > 0)
                {
                    _ = result.Append(consecutiveNull);
                }
                if (y > 0)
                {
                    _ = result.Append('/');
                }
            }

            _ = result.Append(CurrentTurnSente ? " w " : " b ");
            _ = result.Append("- -");

            _ = omitMoveCounts ? null : result.Append(' ').Append(StaleMoveCounter).Append(' ').Append((Moves.Count / 2) + 1);

            return result.ToString();
        }

        /// <summary>
        /// Convert this game to a PGN file for use in other shogi programs
        /// </summary>
        public string ToPGN(string? eventName, string? siteName, DateOnly? startDate, string senteName, string goteName,
            bool senteIsComputer, bool goteIsComputer)
        {
            GameState state = DetermineGameState();
            string pgn = $"[Event \"{eventName?.Replace("\\", "\\\\")?.Replace("\"", "\\\"") ?? "?"}\"]\n" +
                $"[Site \"{siteName?.Replace("\\", "\\\\")?.Replace("\"", "\\\"") ?? "?"}\"]\n" +
                $"[Date \"{startDate?.ToString("yyyy.MM.dd") ?? "????.??.??"}\"]\n" +
                "[Round \"?\"]\n" +
                $"[White \"{senteName?.Replace("\\", "\\\\")?.Replace("\"", "\\\"") ?? "?"}\"]\n" +
                $"[Black \"{goteName?.Replace("\\", "\\\\")?.Replace("\"", "\\\"") ?? "?"}\"]\n" +
                $"[Result \"{(!GameOver ? "*" : state == GameState.CheckMateGote ? "1-0" : state == GameState.CheckMateSente ? "0-1" : "1/2-1/2")}\"]\n" +
                $"[WhiteType \"{(senteIsComputer ? "program" : "human")}\"]\n" +
                $"[BlackType \"{(goteIsComputer ? "program" : "human")}\"]\n\n";

            // Include initial state if not a standard shogi game
            if (InitialState != "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
            {
                // Strip last newline
                pgn = pgn[..^1] + $"[SetUp \"1\"]\n[FEN \"{InitialState}\"]\n\n";
            }

            string compiledMoveText = "";
            for (int i = 0; i < MoveText.Count; i += 2)
            {
                compiledMoveText += $" {(i / 2) + 1}. {MoveText[i]}";
                if (i + 1 < MoveText.Count)
                {
                    compiledMoveText += $" {MoveText[i + 1]}";
                }
            }
            pgn += compiledMoveText.Trim();
            pgn += !GameOver ? " *\n\n" : state == GameState.CheckMateGote ? " 1-0\n\n" : state == GameState.CheckMateSente ? " 0-1\n\n" : " 1/2-1/2\n\n";

            return pgn;
        }

        /// <summary>
        /// Convert Forsyth–Edwards Notation to a shogi game instance.
        /// </summary>
        public static ShogiGame FromForsythEdwards(string forsythEdwards)
        {
            string[] fields = forsythEdwards.Split(' ');
            if (fields.Length != 6)
            {
                throw new FormatException("Forsyth–Edwards Notation requires 6 fields separated by spaces");
            }

            string[] ranks = fields[0].Split('/');
            if (ranks.Length != 8)
            {
                throw new FormatException("Board definitions must have 8 ranks separated by a forward slash");
            }
            Pieces.Piece?[,] board = new Pieces.Piece?[8, 8];
            for (int r = 0; r < ranks.Length; r++)
            {
                int fileIndex = 0;
                foreach (char pieceChar in ranks[r])
                {
                    switch (pieceChar)
                    {
                        case 'K':
                            board[fileIndex, 7 - r] = new Pieces.King(new Point(fileIndex, 7 - r), true);
                            break;
                        case 'Q':
                            board[fileIndex, 7 - r] = new Pieces.Queen(new Point(fileIndex, 7 - r), true);
                            break;
                        case 'R':
                            board[fileIndex, 7 - r] = new Pieces.Rook(new Point(fileIndex, 7 - r), true);
                            break;
                        case 'B':
                            board[fileIndex, 7 - r] = new Pieces.Bishop(new Point(fileIndex, 7 - r), true);
                            break;
                        case 'N':
                            board[fileIndex, 7 - r] = new Pieces.Knight(new Point(fileIndex, 7 - r), true);
                            break;
                        case 'P':
                            board[fileIndex, 7 - r] = new Pieces.Pawn(new Point(fileIndex, 7 - r), true);
                            break;
                        case 'k':
                            board[fileIndex, 7 - r] = new Pieces.King(new Point(fileIndex, 7 - r), false);
                            break;
                        case 'q':
                            board[fileIndex, 7 - r] = new Pieces.Queen(new Point(fileIndex, 7 - r), false);
                            break;
                        case 'r':
                            board[fileIndex, 7 - r] = new Pieces.Rook(new Point(fileIndex, 7 - r), false);
                            break;
                        case 'b':
                            board[fileIndex, 7 - r] = new Pieces.Bishop(new Point(fileIndex, 7 - r), false);
                            break;
                        case 'n':
                            board[fileIndex, 7 - r] = new Pieces.Knight(new Point(fileIndex, 7 - r), false);
                            break;
                        case 'p':
                            board[fileIndex, 7 - r] = new Pieces.Pawn(new Point(fileIndex, 7 - r), false);
                            break;
                        default:
                            if (pieceChar is > '0' and <= '9')
                            {
                                // char - '0' gets numeric value of ASCII number
                                // Leaves the specified number of squares as null
                                fileIndex += pieceChar - '0' - 1; 
                                // Subtract 1 as fileIndex gets incremented by 1 as well later
                            }
                            else
                            {
                                throw new FormatException($"{pieceChar} is not a valid piece character");
                            }
                            break;
                    }
                    fileIndex++;
                }
                if (fileIndex != 8)
                {
                    throw new FormatException("Each rank in a board definition must contain definitions for 8 files");
                }
            }

            bool currentTurnSente = fields[1] == "w" || (fields[1] == "b" ? false
                : throw new FormatException("Current turn specifier must be either w or b"));

            int staleMoves = int.Parse(fields[4]);

            // Forsyth–Edwards doesn't define what the previous moves were, so they moves list starts empty
            // For the PGN standard, if gote moves first then a single move "..." is added to the start of the move text list
            return new ShogiGame(board, currentTurnSente, EndingStates.Contains(BoardAnalysis.DetermineGameState(board, currentTurnSente)),
                new(), currentTurnSente ? new() : new() { "..." }, new(), staleMoves, new(), null);
        }
    }
}
