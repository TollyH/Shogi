using System;
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
        DrawRepetition,
        CheckSente,
        CheckGote,
        PerpetualCheckSente,
        PerpetualCheckGote,
        StalemateSente,
        StalemateGote,
        CheckMateSente,
        CheckMateGote
    }

    public class ShogiGame
    {
        public static readonly ImmutableHashSet<GameState> EndingStates = new HashSet<GameState>()
        {
            GameState.DrawRepetition,
            GameState.PerpetualCheckSente,
            GameState.PerpetualCheckGote,
            GameState.StalemateSente,
            GameState.StalemateGote,
            GameState.CheckMateSente,
            GameState.CheckMateGote
        }.ToImmutableHashSet();

        /// <summary>
        /// Used to give to <see cref="MovePiece"/> as the source position to declare that a piece drop should occur
        /// </summary>
        public static readonly Dictionary<Type, Point> PieceDropSources = new()
        {
            { typeof(Pieces.GoldGeneral), new Point(-1, 0) },
            { typeof(Pieces.SilverGeneral), new Point(-1, 1) },
            { typeof(Pieces.Rook), new Point(-1, 2) },
            { typeof(Pieces.Bishop), new Point(-1, 3) },
            { typeof(Pieces.Knight), new Point(-1, 4) },
            { typeof(Pieces.Lance), new Point(-1, 5) },
            { typeof(Pieces.Pawn), new Point(-1, 6) },
        };
        public static readonly Type[] DropTypeOrder = new Type[7]
        {
            typeof(Pieces.GoldGeneral), typeof(Pieces.SilverGeneral), typeof(Pieces.Rook),
            typeof(Pieces.Bishop), typeof(Pieces.Knight), typeof(Pieces.Lance), typeof(Pieces.Pawn)
        };

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
        public Dictionary<Type, int> SentePieceDrops { get; }
        public Dictionary<Type, int> GotePieceDrops { get; }

        // Used to detect repetition
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
            GoteKing = new Pieces.King(new Point(4, 8), false);

            Moves = new List<(Point, Point)>();
            MoveText = new List<string>();
            SentePieceDrops = new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
            GotePieceDrops = new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };

            BoardCounts = new Dictionary<string, int>();

            Board = new Pieces.Piece?[9, 9]
            {
                { new Pieces.Lance(new Point(0, 0), true), null, new Pieces.Pawn(new Point(0, 2), true), null, null, null, new Pieces.Pawn(new Point(0, 6), false), null, new Pieces.Lance(new Point(0, 8), false) },
                { new Pieces.Knight(new Point(1, 0), true), new Pieces.Bishop(new Point(1, 1), true), new Pieces.Pawn(new Point(1, 2), true), null, null, null, new Pieces.Pawn(new Point(1, 6), false), new Pieces.Rook(new Point(1, 7), false), new Pieces.Knight(new Point(1, 8), false) },
                { new Pieces.SilverGeneral(new Point(2, 0), true), null, new Pieces.Pawn(new Point(2, 2), true), null, null, null, new Pieces.Pawn(new Point(2, 6), false), null, new Pieces.SilverGeneral(new Point(2, 8), false) },
                { new Pieces.GoldGeneral(new Point(3, 0), true), null, new Pieces.Pawn(new Point(3, 2), true), null, null, null, new Pieces.Pawn(new Point(3, 6), false), null, new Pieces.GoldGeneral(new Point(3, 8), false) },
                { SenteKing, null, new Pieces.Pawn(new Point(4, 2), true), null, null, null, new Pieces.Pawn(new Point(4, 6), false), null, GoteKing },
                { new Pieces.GoldGeneral(new Point(5, 0), true), null, new Pieces.Pawn(new Point(5, 2), true), null, null, null, new Pieces.Pawn(new Point(5, 6), false), null, new Pieces.GoldGeneral(new Point(5, 8), false) },
                { new Pieces.SilverGeneral(new Point(6, 0), true), null, new Pieces.Pawn(new Point(6, 2), true), null, null, null, new Pieces.Pawn(new Point(6, 6), false), null, new Pieces.SilverGeneral(new Point(6, 8), false) },
                { new Pieces.Knight(new Point(7, 0), true), new Pieces.Rook(new Point(7, 1), true), new Pieces.Pawn(new Point(7, 2), true), null, null, null, new Pieces.Pawn(new Point(7, 6), false), new Pieces.Bishop(new Point(7, 7), false), new Pieces.Knight(new Point(7, 8), false) },
                { new Pieces.Lance(new Point(8, 0), true), null, new Pieces.Pawn(new Point(8, 2), true), null, null, null, new Pieces.Pawn(new Point(8, 6), false), null, new Pieces.Lance(new Point(8, 8), false) }
            };

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a shogi game, setting each game parameter to a non-default value
        /// </summary>
        public ShogiGame(Pieces.Piece?[,] board, bool currentTurnSente, bool gameOver, List<(Point, Point)> moves, List<string> moveText,
            Dictionary<Type, int>? sentePieceDrops, Dictionary<Type, int>? gotePieceDrops,
            Dictionary<string, int> boardCounts, string? initialState)
        {
            if (board.GetLength(0) != 9 || board.GetLength(1) != 9)
            {
                throw new ArgumentException("Boards must be 9x9 in size");
            }

            Board = board;
            SenteKing = Board.OfType<Pieces.King>().Where(k => k.IsSente).First();
            GoteKing = Board.OfType<Pieces.King>().Where(k => !k.IsSente).First();

            CurrentTurnSente = currentTurnSente;
            GameOver = gameOver;
            Moves = moves;
            MoveText = moveText;
            SentePieceDrops = sentePieceDrops ?? new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
            GotePieceDrops = gotePieceDrops ?? new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
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
                new Dictionary<Type, int>(SentePieceDrops), new Dictionary<Type, int>(GotePieceDrops),
                new(BoardCounts), InitialState);
        }

        /// <summary>
        /// Determine the current state of the game.
        /// </summary>
        /// <remarks>
        /// This method is similar to <see cref="BoardAnalysis.DetermineGameState"/>, however it can also detect repetition.
        /// </remarks>
        public GameState DetermineGameState(bool includeRepetition = true)
        {
            GameState staticState = BoardAnalysis.DetermineGameState(Board, CurrentTurnSente,
                SenteKing.Position, GoteKing.Position);
            if (EndingStates.Contains(staticState))
            {
                return staticState;
            }
            if (includeRepetition && BoardCounts.GetValueOrDefault(ToString(true)) >= 4)
            {
                if (ToString(true)[^1] == '!')
                {
                    return CurrentTurnSente ? GameState.PerpetualCheckGote : GameState.PerpetualCheckSente;
                }
                return GameState.DrawRepetition;
            }
            return staticState;
        }

        /// <summary>
        /// Determine whether a drop of the given piece type to the given destination is valid or not.
        /// </summary>
        public bool IsDropPossible(Type dropType, Point destination)
        {
            if (destination.X < 0 || destination.Y < 0
                || destination.X >= Board.GetLength(0) || destination.Y >= Board.GetLength(1))
            {
                return false;
            }
            Pieces.Piece piece = (Pieces.Piece)Activator.CreateInstance(dropType, destination, CurrentTurnSente)!;
            if (Board[destination.X, destination.Y] is not null)
            {
                return false;
            }
            if ((piece is Pieces.Pawn or Pieces.Lance && (destination.Y == (piece.IsSente ? 8 : 0)))
                || (piece is Pieces.Knight && (piece.IsSente ? destination.Y >= 7 : destination.Y <= 1)))
            {
                return false;
            }

            bool pawnPresentOnFile = false;
            for (int y = 0; y < Board.GetLength(1); y++)
            {
                if (Board[destination.X, y] is Pieces.Pawn
                    && Board[destination.X, y]!.IsSente == CurrentTurnSente)
                {
                    pawnPresentOnFile = true;
                    break;
                }
            }

            ShogiGame checkmateTest = Clone();
            _ = checkmateTest.MovePiece(new Point(-1, Array.IndexOf(DropTypeOrder, dropType)),
                destination, forceMove: true, updateMoveText: false);
            if (piece is Pieces.Pawn && (pawnPresentOnFile
                || checkmateTest.DetermineGameState() is GameState.CheckMateSente or GameState.CheckMateGote))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Move a piece on the board from a <paramref name="source"/> coordinate to a <paramref name="destination"/> coordinate.
        /// To perform a piece drop, set <paramref name="source"/> to a value within <see cref="PieceDropSources"/>.
        /// </summary>
        /// <param name="forceMove">
        /// Whether or not a move should always be allowed to occur. If <see langword="true"/> when performing a piece drop, the held piece counters will not be decremented.
        /// </param>
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

            Pieces.Piece? piece;
            if (source.X == -1)
            {
                // Piece drop
                Type dropType = DropTypeOrder[source.Y];
                piece = (Pieces.Piece)Activator.CreateInstance(dropType, destination, CurrentTurnSente)!;
                if (!forceMove && !IsDropPossible(dropType, destination))
                {
                    return false;
                }
                if (!forceMove)
                {
                    if (CurrentTurnSente)
                    {
                        SentePieceDrops[dropType]--;
                    }
                    else
                    {
                        GotePieceDrops[dropType]--;
                    }
                }
            }
            else
            {
                piece = Board[source.X, source.Y];
                if (piece is null)
                {
                    return false;
                }
                if (!forceMove && piece.IsSente != CurrentTurnSente)
                {
                    return false;
                }
            }

            // Used for generating new move text
            ShogiGame? oldGame = null;
            if (updateMoveText)
            {
                oldGame = Clone();
            }

            bool pieceMoved = piece.Move(Board, destination, forceMove || source.X == -1);

            if (pieceMoved)
            {
                Moves.Add((source, destination));
                if (Board[destination.X, destination.Y] is not null)
                {
                    Type targetPiece = Board[destination.X, destination.Y]!.GetType();
                    if (Pieces.Piece.DemotionMap.ContainsKey(targetPiece))
                    {
                        targetPiece = Pieces.Piece.DemotionMap[targetPiece];
                    }
                    if (SentePieceDrops.ContainsKey(targetPiece))
                    {
                        if (CurrentTurnSente)
                        {
                            SentePieceDrops[targetPiece]++;
                        }
                        else
                        {
                            GotePieceDrops[targetPiece]++;
                        }
                    }
                }

                Type pieceType = piece.GetType();
                if (source.X != -1 && Pieces.Piece.PromotionMap.ContainsKey(pieceType))
                {
                    if ((piece.IsSente ? destination.Y >= 6 : destination.Y <= 2)
                        || (piece.IsSente ? source.Y >= 6 : source.Y <= 2))
                    {
                        if ((piece is Pieces.Pawn or Pieces.Lance && (destination.Y == (piece.IsSente ? 8 : 0)))
                            || (piece is Pieces.Knight && (piece.IsSente ? destination.Y >= 7 : destination.Y <= 1)))
                        {
                            // Always promote pawns and lances upon reaching the last rank
                            // Always promote knights upon reaching the last two ranks
                            doPromotion = true;
                        }
                        AwaitingPromotionResponse = true;
                        doPromotion ??= System.Windows.MessageBox.Show(
                            $"Do you want to promote the {piece.Name} you just moved?", "Promotion",
                            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question
                        ) == System.Windows.MessageBoxResult.Yes;
                        AwaitingPromotionResponse = false;
                        if (doPromotion.Value)
                        {
                            piece = (Pieces.Piece)Activator.CreateInstance(Pieces.Piece.PromotionMap[pieceType], piece.Position, piece.IsSente)!;
                        }
                        Board[source.X, source.Y] = piece;
                    }
                }

                Board[destination.X, destination.Y] = piece;
                if (source.X != -1)
                {
                    Board[source.X, source.Y] = null;
                }

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
                    if (source.X != -1 && oldGame!.Board[source.X, source.Y] is Pieces.Pawn)
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
        /// unless <paramref name="appendCheckStatus"/> is <see langword="true"/>
        /// </remarks>
        public string ToString(bool appendCheckStatus)
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
                        _ = result.Append(piece.IsSente ? piece.SFENLetter.ToUpper() : piece.SFENLetter.ToLower());
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

            _ = result.Append(CurrentTurnSente ? " b " : " w ");

            bool anyHeldPieces = false;
            foreach ((Type pieceType, int count) in SentePieceDrops)
            {
                Pieces.Piece piece = (Pieces.Piece)Activator.CreateInstance(pieceType, new Point(), CurrentTurnSente)!;
                if (count == 0)
                {
                    continue;
                }
                if (count != 1)
                {
                    _ = result.Append(count);
                }
                _ = result.Append(piece.SFENLetter.ToUpper());
            }
            foreach ((Type pieceType, int count) in GotePieceDrops)
            {
                Pieces.Piece piece = (Pieces.Piece)Activator.CreateInstance(pieceType, new Point(), CurrentTurnSente)!;
                if (count == 0)
                {
                    continue;
                }
                anyHeldPieces = true;
                if (count != 1)
                {
                    _ = result.Append(count);
                }
                _ = result.Append(piece.SFENLetter.ToLower());
            }
            if (!anyHeldPieces)
            {
                _ = result.Append('-');
            }

            // Append whether in check or not for checking whether perpetual check occurred
            _ = !appendCheckStatus ? null
                : result.Append(DetermineGameState(false) is GameState.CheckSente or GameState.CheckGote ? " !" : " -");

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
        /// Convert Shogi Forsyth–Edwards Notation (SFEN) to a shogi game instance.
        /// </summary>
        public static ShogiGame FromShogiForsythEdwards(string forsythEdwards)
        {
            string[] fields = forsythEdwards.Split(' ');
            if (fields.Length != 3)
            {
                throw new FormatException("Shogi Forsyth–Edwards Notation requires 3 fields separated by spaces");
            }

            string[] ranks = fields[0].Split('/');
            if (ranks.Length != 9)
            {
                throw new FormatException("Board definitions must have 9 ranks separated by a forward slash");
            }
            Pieces.Piece?[,] board = new Pieces.Piece?[9, 9];
            for (int r = 0; r < ranks.Length; r++)
            {
                int fileIndex = 0;
                bool promoteNextPiece = false;
                foreach (char pieceChar in ranks[r])
                {
                    switch (pieceChar)
                    {
                        case '+':
                            promoteNextPiece = true;
                            continue;
                        case 'K':
                            board[fileIndex, 8 - r] = new Pieces.King(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'G':
                            board[fileIndex, 8 - r] = new Pieces.GoldGeneral(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'S':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedSilverGeneral(new Point(fileIndex, 8 - r), true)
                                : new Pieces.SilverGeneral(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'R':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedRook(new Point(fileIndex, 8 - r), true)
                                : new Pieces.Rook(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'B':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedBishop(new Point(fileIndex, 8 - r), true)
                                : new Pieces.Bishop(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'N':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedKnight(new Point(fileIndex, 8 - r), true)
                                : new Pieces.Knight(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'L':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedLance(new Point(fileIndex, 8 - r), true)
                                : new Pieces.Lance(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'P':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedPawn(new Point(fileIndex, 8 - r), true)
                                : new Pieces.Pawn(new Point(fileIndex, 8 - r), true);
                            break;
                        case 'k':
                            board[fileIndex, 8 - r] = new Pieces.King(new Point(fileIndex, 8 - r), false);
                            break;
                        case 'g':
                            board[fileIndex, 8 - r] = new Pieces.GoldGeneral(new Point(fileIndex, 8 - r), false);
                            break;
                        case 's':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedSilverGeneral(new Point(fileIndex, 8 - r), false)
                                : new Pieces.SilverGeneral(new Point(fileIndex, 8 - r), false);
                            break;
                        case 'r':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedRook(new Point(fileIndex, 8 - r), false)
                                : new Pieces.Rook(new Point(fileIndex, 8 - r), false);
                            break;
                        case 'b':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedBishop(new Point(fileIndex, 8 - r), false)
                                : new Pieces.Bishop(new Point(fileIndex, 8 - r), false);
                            break;
                        case 'n':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedKnight(new Point(fileIndex, 8 - r), false)
                                : new Pieces.Knight(new Point(fileIndex, 8 - r), false);
                            break;
                        case 'l':
                            board[fileIndex, 8 - r] = promoteNextPiece
                                ? new Pieces.PromotedLance(new Point(fileIndex, 8 - r), false)
                                : new Pieces.Lance(new Point(fileIndex, 8 - r), false);
                            break;
                        case 'p':
                            board[fileIndex, 8 - r] = promoteNextPiece 
                                ? new Pieces.PromotedPawn(new Point(fileIndex, 8 - r), false)
                                : new Pieces.Pawn(new Point(fileIndex, 8 - r), false);
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
                    promoteNextPiece = false;
                }
                if (fileIndex != 9)
                {
                    throw new FormatException("Each rank in a board definition must contain definitions for 9 files");
                }
            }

            bool currentTurnSente = fields[1] == "b" || (fields[1] == "w" ? false
                : throw new FormatException("Current turn specifier must be either w or b"));

            Dictionary<Type, int> sentePieceDrops = new()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
            Dictionary<Type, int> gotePieceDrops = new()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };

            int numberToAdd = 1;
            foreach (char pieceChar in fields[2])
            {
                switch (pieceChar)
                {
                    case '-':
                        continue;
                    case 'G':
                        sentePieceDrops[typeof(Pieces.GoldGeneral)] += numberToAdd;
                        break;
                    case 'S':
                        sentePieceDrops[typeof(Pieces.SilverGeneral)] += numberToAdd;
                        break;
                    case 'R':
                        sentePieceDrops[typeof(Pieces.Rook)] += numberToAdd;
                        break;
                    case 'B':
                        sentePieceDrops[typeof(Pieces.Bishop)] += numberToAdd;
                        break;
                    case 'N':
                        sentePieceDrops[typeof(Pieces.Knight)] += numberToAdd;
                        break;
                    case 'L':
                        sentePieceDrops[typeof(Pieces.Lance)] += numberToAdd;
                        break;
                    case 'P':
                        sentePieceDrops[typeof(Pieces.Pawn)] += numberToAdd;
                        break;
                    case 'g':
                        gotePieceDrops[typeof(Pieces.GoldGeneral)] += numberToAdd;
                        break;
                    case 's':
                        gotePieceDrops[typeof(Pieces.SilverGeneral)] += numberToAdd;
                        break;
                    case 'r':
                        gotePieceDrops[typeof(Pieces.Rook)] += numberToAdd;
                        break;
                    case 'b':
                        gotePieceDrops[typeof(Pieces.Bishop)] += numberToAdd;
                        break;
                    case 'n':
                        gotePieceDrops[typeof(Pieces.Knight)] += numberToAdd;
                        break;
                    case 'l':
                        gotePieceDrops[typeof(Pieces.Lance)] += numberToAdd;
                        break;
                    case 'p':
                        gotePieceDrops[typeof(Pieces.Pawn)] += numberToAdd;
                        break;
                    default:
                        if (pieceChar is > '0' and <= '9')
                        {
                            // char - '0' gets numeric value of ASCII number
                            numberToAdd = pieceChar - '0';
                            continue;
                        }
                        else
                        {
                            throw new FormatException($"{pieceChar} is not a valid piece character");
                        }
                }
                numberToAdd = 1;
            }

            // Shogi Forsyth–Edwards doesn't define what the previous moves were, so they moves list starts empty
            return new ShogiGame(board, currentTurnSente, EndingStates.Contains(BoardAnalysis.DetermineGameState(board, currentTurnSente)),
                new(), currentTurnSente ? new() : new() { "..." }, sentePieceDrops, gotePieceDrops, new(), null);
        }
    }
}
