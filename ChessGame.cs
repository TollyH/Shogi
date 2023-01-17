﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Chess
{
    /// <remarks>
    /// CheckWhite and CheckMateWhite mean that the check is against white,
    /// or that white has lost respectively, and vice versa.
    /// </remarks>
    public enum GameState
    {
        StandardPlay,
        DrawStalemate,
        DrawFiftyMove,
        DrawThreeFold,
        DrawInsufficientMaterial,
        CheckWhite,
        CheckBlack,
        CheckMateWhite,
        CheckMateBlack
    }

    public class ChessGame
    {
        public static readonly ImmutableHashSet<GameState> EndingStates = new HashSet<GameState>()
        {
            GameState.DrawFiftyMove,
            GameState.DrawStalemate,
            GameState.DrawThreeFold,
            GameState.DrawInsufficientMaterial,
            GameState.CheckMateWhite,
            GameState.CheckMateBlack
        }.ToImmutableHashSet();

        public Pieces.Piece?[,] Board { get; }
        public string InitialState { get; }

        public Pieces.King WhiteKing { get; }
        public Pieces.King BlackKing { get; }

        public bool CurrentTurnWhite { get; private set; }
        public bool GameOver => EndingStates.Contains(DetermineGameState());
        public bool AwaitingPromotionResponse { get; private set; }

        /// <summary>
        /// A list of the moves made this game as (sourcePosition, destinationPosition)
        /// </summary>
        public List<(Point, Point)> Moves { get; }
        public List<Pieces.Piece> CapturedPieces { get; }

        public Point? EnPassantSquare { get; private set; }
        public bool WhiteMayCastleKingside { get; private set; }
        public bool WhiteMayCastleQueenside { get; private set; }
        public bool BlackMayCastleKingside { get; private set; }
        public bool BlackMayCastleQueenside { get; private set; }

        // Used for the 50-move rule
        public int StaleMoveCounter { get; private set; }
        // Used to detect three-fold repetition
        public Dictionary<string, int> BoardCounts { get; }

        /// <summary>
        /// Create a new standard chess game with all values at their defaults
        /// </summary>
        public ChessGame()
        {
            CurrentTurnWhite = true;
            AwaitingPromotionResponse = false;

            WhiteKing = new Pieces.King(new Point(4, 0), true);
            BlackKing = new Pieces.King(new Point(4, 7), false);

            Moves = new List<(Point, Point)>();
            CapturedPieces = new List<Pieces.Piece>();

            EnPassantSquare = null;
            WhiteMayCastleKingside = true;
            WhiteMayCastleQueenside = true;
            BlackMayCastleKingside = true;
            BlackMayCastleQueenside = true;

            StaleMoveCounter = 0;
            BoardCounts = new Dictionary<string, int>();

            Board = new Pieces.Piece?[8, 8]
            {
                { new Pieces.Rook(new Point(0, 0), true), new Pieces.Pawn(new Point(0, 1), true), null, null, null, null, new Pieces.Pawn(new Point(0, 6), false), new Pieces.Rook(new Point(0, 7), false) },
                { new Pieces.Knight(new Point(1, 0), true), new Pieces.Pawn(new Point(1, 1), true), null, null, null, null, new Pieces.Pawn(new Point(1, 6), false), new Pieces.Knight(new Point(1, 7), false) },
                { new Pieces.Bishop(new Point(2, 0), true), new Pieces.Pawn(new Point(2, 1), true), null, null, null, null, new Pieces.Pawn(new Point(2, 6), false), new Pieces.Bishop(new Point(2, 7), false) },
                { new Pieces.Queen(new Point(3, 0), true), new Pieces.Pawn(new Point(3, 1), true), null, null, null, null, new Pieces.Pawn(new Point(3, 6), false), new Pieces.Queen(new Point(3, 7), false) },
                { WhiteKing, new Pieces.Pawn(new Point(4, 1), true), null, null, null, null, new Pieces.Pawn(new Point(4, 6), false), BlackKing },
                { new Pieces.Bishop(new Point(5, 0), true), new Pieces.Pawn(new Point(5, 1), true), null, null, null, null, new Pieces.Pawn(new Point(5, 6), false), new Pieces.Bishop(new Point(5, 7), false) },
                { new Pieces.Knight(new Point(6, 0), true), new Pieces.Pawn(new Point(6, 1), true), null, null, null, null, new Pieces.Pawn(new Point(6, 6), false), new Pieces.Knight(new Point(6, 7), false) },
                { new Pieces.Rook(new Point(7, 0), true), new Pieces.Pawn(new Point(7, 1), true), null, null, null, null, new Pieces.Pawn(new Point(7, 6), false), new Pieces.Rook(new Point(7, 7), false) }
            };

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a chess game, setting each game parameter to a non-default value
        /// </summary>
        public ChessGame(Pieces.Piece?[,] board, bool currentTurnWhite, List<(Point, Point)> moves,
            List<Pieces.Piece> capturedPieces, Point? enPassantSquare, bool whiteMayCastleKingside, bool whiteMayCastleQueenside,
            bool blackMayCastleKingside, bool blackMayCastleQueenside, int staleMoveCounter, Dictionary<string, int> boardCounts,
            string? initialState)
        {
            if (board.GetLength(0) != 8 || board.GetLength(1) != 8)
            {
                throw new ArgumentException("Boards must be 8x8 in size");
            }

            Board = board;
            WhiteKing = Board.OfType<Pieces.King>().Where(k => k.IsWhite).First();
            BlackKing = Board.OfType<Pieces.King>().Where(k => !k.IsWhite).First();

            if ((whiteMayCastleKingside && (Board[7, 0] is not Pieces.Rook || !Board[7, 0]!.IsWhite
                    || Board[4, 0] is not Pieces.King || !Board[4, 0]!.IsWhite))
                || (whiteMayCastleQueenside && (Board[0, 0] is not Pieces.Rook || !Board[0, 0]!.IsWhite
                    || Board[4, 0] is not Pieces.King || !Board[4, 0]!.IsWhite))
                || (blackMayCastleKingside && (Board[7, 7] is not Pieces.Rook || Board[7, 7]!.IsWhite
                    || Board[4, 7] is not Pieces.King || Board[4, 7]!.IsWhite))
                || (blackMayCastleQueenside && (Board[0, 7] is not Pieces.Rook || Board[0, 7]!.IsWhite
                    || Board[4, 7] is not Pieces.King || Board[4, 7]!.IsWhite)))
            {
                throw new ArgumentException(
                    "At least one castling allowed flag was set to true without a valid position for performing it");
            }

            CurrentTurnWhite = currentTurnWhite;
            Moves = moves;
            CapturedPieces = capturedPieces;
            EnPassantSquare = enPassantSquare;
            WhiteMayCastleKingside = whiteMayCastleKingside;
            WhiteMayCastleQueenside = whiteMayCastleQueenside;
            BlackMayCastleKingside = blackMayCastleKingside;
            BlackMayCastleQueenside = blackMayCastleQueenside;
            StaleMoveCounter = staleMoveCounter;
            BoardCounts = boardCounts;

            InitialState = initialState ?? ToString();
        }

        /// <summary>
        /// Create a deep copy of all parameters to this chess game
        /// </summary>
        public ChessGame Clone()
        {
            Pieces.Piece?[,] boardClone = new Pieces.Piece?[Board.GetLength(0), Board.GetLength(1)];
            for (int x = 0; x < boardClone.GetLength(0); x++)
            {
                for (int y = 0; y < boardClone.GetLength(1); y++)
                {
                    boardClone[x, y] = Board[x, y]?.Clone();
                }
            }

            return new ChessGame(boardClone, CurrentTurnWhite, new(Moves),
                CapturedPieces.Select(c => c.Clone()).ToList(), EnPassantSquare, WhiteMayCastleKingside,
                WhiteMayCastleQueenside, BlackMayCastleKingside, BlackMayCastleQueenside, StaleMoveCounter,
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
            GameState staticState = BoardAnalysis.DetermineGameState(Board, CurrentTurnWhite,
                WhiteKing.Position, BlackKing.Position);
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
        /// Determine if the player who's turn it is may castle in a given direction on this turn
        /// </summary>
        /// <param name="kingside"><see langword="true"/> if checking kingside, <see langword="false"/> if checking queenside</param>
        /// <remarks>
        /// This method is similar to <see cref="BoardAnalysis.IsCastlePossible"/>,
        /// however it also accounts for whether a king or rook have moved before
        /// </remarks>
        public bool IsCastlePossible(bool kingside)
        {
            if (GameOver)
            {
                return false;
            }

            if (kingside)
            {
                if (CurrentTurnWhite && !WhiteMayCastleKingside)
                {
                    return false;
                }
                if (!CurrentTurnWhite && !BlackMayCastleKingside)
                {
                    return false;
                }
            }
            else
            {
                if (CurrentTurnWhite && !WhiteMayCastleQueenside)
                {
                    return false;
                }
                if (!CurrentTurnWhite && !BlackMayCastleQueenside)
                {
                    return false;
                }
            }

            return BoardAnalysis.IsCastlePossible(Board, CurrentTurnWhite, kingside);
        }

        /// <summary>
        /// Move a piece on the board from a <paramref name="source"/> coordinate to a <paramref name="destination"/> coordinate.
        /// </summary>
        /// <param name="autoQueen">
        /// If a pawn is promoted, should it automatically become a queen (<see langword="true"/>),
        /// or should the user be prompted for a promotion type (<see langword="false"/>)
        /// </param>
        /// <returns><see langword="true"/> if the move was valid and executed, <see langword="false"/> otherwise</returns>
        /// <remarks>This method will check if the move is completely valid, unless <paramref name="forceMove"/> is <see langword="true"/>. No other validity checks are required.</remarks>
        public bool MovePiece(Point source, Point destination, bool forceMove = false, bool autoQueen = true)
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
            if (!forceMove && piece.IsWhite != CurrentTurnWhite)
            {
                return false;
            }

            bool pieceMoved;
            int homeY = CurrentTurnWhite ? 0 : 7;
            if (piece is Pieces.King && source.X == 4 && destination.Y == homeY
                && ((destination.X == 6 && (forceMove || IsCastlePossible(true)))
                    || (destination.X == 2 && (forceMove || IsCastlePossible(false)))))
            {
                // King performed castle, move correct rook
                pieceMoved = true;
                _ = piece.Move(Board, destination, true);

                int rookXPos = destination.X == 2 ? 0 : 7;
                int newRookXPos = destination.X == 2 ? 3 : 5;
                _ = Board[rookXPos, homeY]!.Move(Board, new Point(newRookXPos, homeY), true);
                Board[newRookXPos, homeY] = Board[rookXPos, homeY];
                Board[rookXPos, homeY] = null;
            }
            else if (piece is Pieces.Pawn && destination == EnPassantSquare && (forceMove ||
                (Math.Abs(source.X - destination.X) == 1 && source.Y == (CurrentTurnWhite ? 4 : 3)
                && !BoardAnalysis.IsKingReachable(Board.AfterMove(source, destination), CurrentTurnWhite))))
            {
                pieceMoved = true;
                _ = piece.Move(Board, destination, true);
                // Take pawn after en passant
                if (Board[destination.X, source.Y] is not null)
                {
                    CapturedPieces.Add(Board[destination.X, source.Y]!);
                    Board[destination.X, source.Y] = null;
                }
            }
            else
            {
                pieceMoved = piece.Move(Board, destination, forceMove);
            }

            if (pieceMoved)
            {
                StaleMoveCounter++;
                Moves.Add((source, destination));
                if (Board[destination.X, destination.Y] is not null)
                {
                    if (Board[destination.X, destination.Y] is Pieces.Rook)
                    {
                        if (destination == new Point(0, 0))
                        {
                            WhiteMayCastleQueenside = false;
                        }
                        else if (destination == new Point(7, 0))
                        {
                            WhiteMayCastleKingside = false;
                        }
                        else if (destination == new Point(0, 7))
                        {
                            BlackMayCastleQueenside = false;
                        }
                        else if (destination == new Point(7, 7))
                        {
                            BlackMayCastleKingside = false;
                        }
                    }
                    CapturedPieces.Add(Board[destination.X, destination.Y]!);
                    StaleMoveCounter = 0;
                }

                EnPassantSquare = null;
                if (piece is Pieces.Pawn)
                {
                    StaleMoveCounter = 0;
                    if (Math.Abs(destination.Y - source.Y) > 1)
                    {
                        EnPassantSquare = new Point(source.X, source.Y + (piece.IsWhite ? 1 : -1));
                    }
                    if (destination.Y == (piece.IsWhite ? 7 : 0))
                    {
                        if (autoQueen)
                        {
                            piece = new Pieces.Queen(piece.Position, piece.IsWhite);
                        }
                        else
                        {
                            AwaitingPromotionResponse = true;
                            PromotionPrompt prompt = new(CurrentTurnWhite);
                            _ = prompt.ShowDialog();
                            piece = (Pieces.Piece)Activator.CreateInstance(prompt.ChosenPieceType, piece.Position, piece.IsWhite)!;
                            AwaitingPromotionResponse = false;
                        }
                        Board[source.X, source.Y] = piece;
                    }
                }
                else if (piece is Pieces.King)
                {
                    if (piece.IsWhite)
                    {
                        WhiteMayCastleKingside = false;
                        WhiteMayCastleQueenside = false;
                    }
                    else
                    {
                        BlackMayCastleKingside = false;
                        BlackMayCastleQueenside = false;
                    }
                }
                else if (piece is Pieces.Rook)
                {
                    if (piece.IsWhite)
                    {
                        if (source.X == 0)
                        {
                            WhiteMayCastleQueenside = false;
                        }
                        else if (source.X == 7)
                        {
                            WhiteMayCastleKingside = false;
                        }
                    }
                    else
                    {
                        if (source.X == 0)
                        {
                            BlackMayCastleQueenside = false;
                        }
                        else if (source.X == 7)
                        {
                            BlackMayCastleKingside = false;
                        }
                    }
                }

                Board[destination.X, destination.Y] = piece;
                Board[source.X, source.Y] = null;

                string newBoardString = ToString(true);
                if (BoardCounts.ContainsKey(newBoardString))
                {
                    BoardCounts[newBoardString]++;
                }
                else
                {
                    BoardCounts[newBoardString] = 1;
                }

                CurrentTurnWhite = !CurrentTurnWhite;
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
        /// unless <paramref name="omitTurnAndMoveCounts"/> is <see langword="true"/>
        /// </remarks>
        public string ToString(bool omitTurnAndMoveCounts)
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
                        _ = result.Append(piece.IsWhite ? char.ToUpper(piece.SymbolLetter) : char.ToLower(piece.SymbolLetter));
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

            _ = omitTurnAndMoveCounts ? result.Append(' ') : result.Append(CurrentTurnWhite ? " w " : " b ");

            bool atLeastOneCastle = false;
            if (WhiteMayCastleKingside)
            {
                atLeastOneCastle = true;
                _ = result.Append('K');
            }
            if (WhiteMayCastleQueenside)
            {
                atLeastOneCastle = true;
                _ = result.Append('Q');
            }
            if (BlackMayCastleKingside)
            {
                atLeastOneCastle = true;
                _ = result.Append('k');
            }
            if (WhiteMayCastleQueenside)
            {
                atLeastOneCastle = true;
                _ = result.Append('q');
            }
            if (!atLeastOneCastle)
            {
                _ = result.Append('-');
            }

            _ = EnPassantSquare is null
                ? result.Append(" -")
                : result.Append(' ').Append(EnPassantSquare.Value.ToChessCoordinate());

            _ = omitTurnAndMoveCounts ? null : result.Append(' ').Append(StaleMoveCounter).Append(' ').Append((Moves.Count / 2) + 1);

            return result.ToString();
        }

        /// <summary>
        /// Convert Forsyth–Edwards Notation to a chess game instance.
        /// </summary>
        public static ChessGame FromForsythEdwards(string forsythEdwards)
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

            bool currentTurnWhite = fields[1] == "w" || (fields[1] == "b" ? false
                : throw new FormatException("Current turn specifier must be either w or b"));

            bool whiteKingside = false;
            bool whiteQueenside = false;
            bool blackKingside = false;
            bool blackQueenside = false;
            foreach (char castleSpecifier in fields[2])
            {
                switch (castleSpecifier)
                {
                    case 'K':
                        whiteKingside = true;
                        break;
                    case 'Q':
                        whiteQueenside = true;
                        break;
                    case 'k':
                        blackKingside = true;
                        break;
                    case 'q':
                        blackQueenside = true;
                        break;
                    case '-': break;
                    default:
                        throw new FormatException($"{castleSpecifier} is not a valid castling specifier");
                }
            }

            Point? enPassant = fields[3] == "-" ? null : fields[3].FromChessCoordinate();

            int staleMoves = int.Parse(fields[4]);

            // Forsyth–Edwards doesn't define what the previous moves were, so they moves list starts empty
            return new ChessGame(board, currentTurnWhite, new(), new(), enPassant,
                whiteKingside, whiteQueenside, blackKingside, blackQueenside, staleMoves, new(), null);
        }
    }
}
