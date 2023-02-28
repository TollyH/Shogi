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

        public int PromotionZoneSenteStart { get; }
        public int PromotionZoneGoteStart { get; }

        public Pieces.King SenteKing { get; }
        public Pieces.King GoteKing { get; }

        public bool CurrentTurnSente { get; private set; }
        public bool GameOver { get; private set; }
        public bool AwaitingPromotionResponse { get; private set; }

        /// <summary>
        /// A list of the moves made this game as
        /// (pieceLetter, sourcePosition, destinationPosition, promotionHappened, dropHappened)
        /// </summary>
        public List<(string, Point, Point, bool, bool)> Moves { get; }
        public List<string> JapaneseMoveText { get; }
        public List<string> WesternMoveText { get; }
        public Dictionary<Type, int> SentePieceDrops { get; }
        public Dictionary<Type, int> GotePieceDrops { get; }

        // Used to detect repetition
        public Dictionary<string, int> BoardCounts { get; }

        /// <summary>
        /// Create a new standard shogi game with all values at their defaults
        /// </summary>
        public ShogiGame(bool minishogi)
        {
            CurrentTurnSente = true;
            GameOver = false;
            AwaitingPromotionResponse = false;

            SenteKing = new Pieces.King(new Point(minishogi ? 0 : 4, 0), true);
            GoteKing = new Pieces.King(new Point(4, minishogi ? 4 : 8), false);

            Moves = new List<(string, Point, Point, bool, bool)>();
            JapaneseMoveText = new List<string>();
            WesternMoveText = new List<string>();
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

            Board = minishogi
            ? new Pieces.Piece?[5, 5]
                {
                    { SenteKing, new Pieces.Pawn(new Point(0, 1), true), null, null, new Pieces.Rook(new Point(0, 4), false) },
                    { new Pieces.GoldGeneral(new Point(1, 0), true), null, null, null, new Pieces.Bishop(new Point(1, 4), false) },
                    { new Pieces.SilverGeneral(new Point(2, 0), true), null, null, null, new Pieces.SilverGeneral(new Point(2, 4), false) },
                    { new Pieces.Bishop(new Point(3, 0), true), null, null, null, new Pieces.GoldGeneral(new Point(3, 4), false) },
                    { new Pieces.Rook(new Point(4, 0), true), null, null, new Pieces.Pawn(new Point(4, 3), false), GoteKing }
                }
            : new Pieces.Piece?[9, 9]
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
            PromotionZoneSenteStart = minishogi ? 4 : 6;
            PromotionZoneGoteStart = minishogi ? 0 : 2;

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a shogi game, setting each game parameter to a non-default value
        /// </summary>
        public ShogiGame(Pieces.Piece?[,] board, bool currentTurnSente, bool gameOver,
            List<(string, Point, Point, bool, bool)> moves, List<string> japaneseMoveText,
            List<string> westernMoveText, Dictionary<Type, int>? sentePieceDrops,
            Dictionary<Type, int>? gotePieceDrops, Dictionary<string, int> boardCounts,
            string? initialState)
        {
            if (board.GetLength(0) is not 9 and not 5 || board.GetLength(1) is not 9 and not 5)
            {
                throw new ArgumentException("Boards must be 9x9 or 5x5 in size");
            }

            bool minishogi = board.GetLength(0) == 5;

            Board = board;
            PromotionZoneSenteStart = minishogi ? 4 : 6;
            PromotionZoneGoteStart = minishogi ? 0 : 2;
            SenteKing = Board.OfType<Pieces.King>().Where(k => k.IsSente).First();
            GoteKing = Board.OfType<Pieces.King>().Where(k => !k.IsSente).First();

            CurrentTurnSente = currentTurnSente;
            GameOver = gameOver;
            Moves = moves;
            JapaneseMoveText = japaneseMoveText;
            WesternMoveText = westernMoveText;
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

            return new ShogiGame(boardClone, CurrentTurnSente, GameOver, new(Moves), new(JapaneseMoveText),
                new(WesternMoveText), new Dictionary<Type, int>(SentePieceDrops),
                new Dictionary<Type, int>(GotePieceDrops), new(BoardCounts), InitialState);
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
                bool endAvoidableWithDrop = false;
                foreach ((Type dropType, int count) in CurrentTurnSente ? SentePieceDrops : GotePieceDrops)
                {
                    if (count > 0)
                    {
                        for (int x = 0; x < Board.GetLength(0); x++)
                        {
                            for (int y = 0; y < Board.GetLength(1); y++)
                            {
                                Point pt = new(x, y);
                                if (IsDropPossible(dropType, pt))
                                {
                                    endAvoidableWithDrop = true;
                                    break;
                                }
                            }
                            if (endAvoidableWithDrop)
                            {
                                break;
                            }
                        }
                    }
                    if (endAvoidableWithDrop)
                    {
                        break;
                    }
                }
                if (!endAvoidableWithDrop)
                {
                    return staticState;
                }
                else
                {
                    staticState = staticState is GameState.CheckMateSente or GameState.StalemateSente
                        ? GameState.CheckSente : GameState.CheckGote;
                }
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
            if ((CurrentTurnSente && SentePieceDrops[dropType] == 0)
                || (!CurrentTurnSente && GotePieceDrops[dropType] == 0))
            {
                return false;
            }
            if (Board[destination.X, destination.Y] is not null)
            {
                return false;
            }
            if (((dropType == typeof(Pieces.Pawn) || dropType == typeof(Pieces.Lance))
                    && (destination.Y == (CurrentTurnSente ? Board.GetLength(1) - 1 : 0)))
                || (dropType == typeof(Pieces.Knight)
                    && (CurrentTurnSente ? destination.Y >= Board.GetLength(1) - 2 : destination.Y <= 1)))
            {
                return false;
            }

            ShogiGame checkmateTest = Clone();
            _ = checkmateTest.MovePiece(new Point(-1, Array.IndexOf(DropTypeOrder, dropType)),
                destination, forceMove: true, updateMoveText: false, determineGameState: false);
            GameState resultingGameState = BoardAnalysis.DetermineGameState(checkmateTest.Board, checkmateTest.CurrentTurnSente,
                checkmateTest.SenteKing.Position, checkmateTest.GoteKing.Position);

            if ((CurrentTurnSente && BoardAnalysis.IsKingReachable(checkmateTest.Board,
                    true, checkmateTest.SenteKing.Position))
                || (!CurrentTurnSente && BoardAnalysis.IsKingReachable(checkmateTest.Board,
                    false, checkmateTest.GoteKing.Position)))
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

            if (dropType == typeof(Pieces.Pawn) && (pawnPresentOnFile
                || resultingGameState is GameState.CheckMateSente or GameState.CheckMateGote))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Move a piece on the board from a <paramref name="source"/> coordinate to a <paramref name="destination"/> coordinate.
        /// To perform a piece drop, set <paramref name="source"/> to a value within <see cref="PieceDropSources"/>.
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
        public bool MovePiece(Point source, Point destination, bool forceMove = false, bool? doPromotion = null, bool updateMoveText = true,
            bool determineGameState = true)
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
                if (CurrentTurnSente && SentePieceDrops[dropType] > 0)
                {
                    SentePieceDrops[dropType]--;
                }
                else if (GotePieceDrops[dropType] > 0)
                {
                    GotePieceDrops[dropType]--;
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
                Moves.Add((piece.SymbolLetter, source, destination, false, source.X == -1));
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

                bool promotionPossible = false;
                bool promotionHappened = false;
                Pieces.Piece beforePromotion = piece;
                Type pieceType = piece.GetType();
                if (source.X != -1 && Pieces.Piece.PromotionMap.ContainsKey(pieceType))
                {
                    if ((piece.IsSente ? destination.Y >= PromotionZoneSenteStart : destination.Y <= PromotionZoneGoteStart)
                        || (piece.IsSente ? source.Y >= PromotionZoneSenteStart : source.Y <= PromotionZoneGoteStart))
                    {
                        promotionPossible = true;
                        if ((piece is Pieces.Pawn or Pieces.Lance && (destination.Y == (piece.IsSente ? Board.GetLength(1) - 1 : 0)))
                            || (piece is Pieces.Knight && (piece.IsSente ? destination.Y >= Board.GetLength(1) - 2 : destination.Y <= 1)))
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
                            promotionHappened = true;
                            Moves[^1] = (Moves[^1].Item1, source, destination, true, false);
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
                if (determineGameState)
                {
                    GameOver = EndingStates.Contains(DetermineGameState());
                }

                if (updateMoveText)
                {
                    string newJapaneseMove = (CurrentTurnSente ? "☖" : "☗")
                        + (Moves.Count > 1 && destination == Moves[^2].Item3 ? "同　" : destination.ToShogiCoordinate(Board.GetLength(0) == 5))
                        + beforePromotion.SymbolLetter;
                    string newWesternMove = beforePromotion.SFENLetter;

                    // Disambiguate moving piece if two pieces of the same type can reach destination
                    IEnumerable<Pieces.Piece> canReachDest = oldGame!.Board.OfType<Pieces.Piece>().Where(
                        p => beforePromotion.GetType() == p.GetType() && p.Position != source && p.IsSente == beforePromotion.IsSente
                            && p.GetValidMoves(oldGame.Board, true).Contains(destination));
                    if (canReachDest.Any())
                    {
                        newWesternMove += $"{Board.GetLength(0) - source.X}{Board.GetLength(1) - source.Y}";
                        if (source.X == -1)
                        {
                            newJapaneseMove += '打';
                        }
                        else if (destination.Y > source.Y && !canReachDest.Where(p => destination.Y > p.Position.Y).Any())
                        {
                            newJapaneseMove += CurrentTurnSente ? '引' : '上';
                        }
                        else if (destination.Y < source.Y && !canReachDest.Where(p => destination.Y < p.Position.Y).Any())
                        {
                            newJapaneseMove += CurrentTurnSente ? '上' : '引';
                        }
                        else if (destination.Y == source.Y && !canReachDest.Where(p => destination.Y == p.Position.Y).Any())
                        {
                            newJapaneseMove += '寄';
                        }
                        else if (destination.X > source.X && !canReachDest.Where(p => destination.X > p.Position.X).Any())
                        {
                            newJapaneseMove += CurrentTurnSente ? "右" : "左";
                        }
                        else if (destination.X < source.X && !canReachDest.Where(p => destination.X < p.Position.X).Any())
                        {
                            newJapaneseMove += CurrentTurnSente ? "左" : "右";
                        }
                        else
                        {
                            newJapaneseMove += "直";
                        }
                    }

                    newWesternMove += source.X == -1 ? '*'
                        : oldGame.Board[destination.X, destination.Y] is not null ? 'x'
                        : '-';
                    newWesternMove += $"{Board.GetLength(0) - destination.X}{Board.GetLength(1) - destination.Y}";

                    if (promotionPossible)
                    {
                        newJapaneseMove += promotionHappened ? "成" : "不成";
                        newWesternMove += promotionHappened ? '+' : '=';
                    }

                    JapaneseMoveText.Add(newJapaneseMove);
                    WesternMoveText.Add(newWesternMove);
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
                if (count == 0)
                {
                    continue;
                }
                anyHeldPieces = true;
                if (count != 1)
                {
                    _ = result.Append(count);
                }
                _ = result.Append(Pieces.Piece.DefaultPieces[pieceType].SFENLetter.ToUpper());
            }
            foreach ((Type pieceType, int count) in GotePieceDrops)
            {
                if (count == 0)
                {
                    continue;
                }
                anyHeldPieces = true;
                if (count != 1)
                {
                    _ = result.Append(count);
                }
                _ = result.Append(Pieces.Piece.DefaultPieces[pieceType].SFENLetter.ToLower());
            }
            if (!anyHeldPieces)
            {
                _ = result.Append('-');
            }

            // Append whether in check or not for checking whether perpetual check occurred
            Pieces.King currentKing = CurrentTurnSente ? SenteKing : GoteKing;
            _ = !appendCheckStatus ? null
                : result.Append(BoardAnalysis.IsKingReachable(Board, CurrentTurnSente, currentKing.Position) ? " !" : " -");

            return result.ToString();
        }

        /// <summary>
        /// Convert this game to a KIF file for use in other shogi programs
        /// </summary>
        public string ToKIF(string? eventName, string? siteName, DateOnly? startDate, string senteName, string goteName,
            bool senteIsComputer, bool goteIsComputer)
        {
            bool minishogi = Board.GetLength(0) == 5;

            GameState state = DetermineGameState();
            string kif = (minishogi ? "手合割：5五将棋\n" : "") +
                $"先手：{senteName}\n" +
                $"後手：{goteName}\n" +
                (startDate is not null ? $"開始日時：{startDate.Value:yyyy'/'MM'/'dd}\n" : "") +
                (eventName is not null ? $"棋戦：{eventName}\n" : "") +
                (siteName is not null ? $"場所：{siteName}\n" : "") +
                $"先手タイプ：{(senteIsComputer ? "プログラム" : "人間")}\n" +
                $"後手タイプ：{(goteIsComputer ? "プログラム" : "人間")}\n";

            // Include initial state if not a standard shogi game
            if ((InitialState != "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b -" && !minishogi)
                || (InitialState != "rbsgk/4p/5/P4/KGSBR b -" && minishogi))
            {
                kif += "後手の持駒：";
                ShogiGame initialGame = FromShogiForsythEdwards(InitialState);

                bool anyDrops = false;
                foreach ((Type dropType, int count) in initialGame.GotePieceDrops)
                {
                    if (count != 0)
                    {
                        anyDrops = true;
                        kif += $" {Pieces.Piece.DefaultPieces[dropType].SymbolLetter}{count.ToJapaneseKanji()}";
                    }
                }
                if (!anyDrops)
                {
                    kif += " なし";
                }

                kif += minishogi ? "\n５ ４ ３ ２ １\n+---------------+" : "\n９ ８ ７ ６ ５ ４ ３ ２ １\n+---------------------------+";
                for (int y = initialGame.Board.GetLength(1) - 1; y >= 0; y--)
                {
                    kif += "\n|";
                    for (int x = 0; x < initialGame.Board.GetLength(0); x++)
                    {
                        if (initialGame.Board[x, y] is null)
                        {
                            kif += " ・";
                            continue;
                        }
                        Pieces.Piece piece = initialGame.Board[x, y]!;
                        kif += $"{(piece.IsSente ? ' ' : 'v')}{piece.SingleLetter}";
                    }
                    kif += $"|{(Board.GetLength(1) - y).ToJapaneseKanji()}";
                }

                kif += minishogi ? "\n+---------------+\n先手の持駒：" : "\n+---------------------------+\n先手の持駒：";
                anyDrops = false;
                foreach ((Type dropType, int count) in initialGame.SentePieceDrops)
                {
                    if (count != 0)
                    {
                        anyDrops = true;
                        kif += $" {Pieces.Piece.DefaultPieces[dropType].SymbolLetter}{count.ToJapaneseKanji()}";
                    }
                }
                if (!anyDrops)
                {
                    kif += " なし";
                }
                if (!initialGame.CurrentTurnSente)
                {
                    kif += "\n後手番";
                }
                kif += '\n';
            }

            string compiledMoveText = "";
            Point lastDest = new(-1, -1);
            for (int i = 0; i < Moves.Count; i += 1)
            {
                (string pieceLetter, Point source, Point destination, bool promotion, bool drop) = Moves[i];
                compiledMoveText += $"\n {i + 1}  {(destination == lastDest ? "同　" : destination.ToShogiCoordinate(minishogi))}{pieceLetter}";
                if (promotion)
                {
                    compiledMoveText += '成';
                }
                if (drop)
                {
                    compiledMoveText += '打';
                }
                else
                {
                    compiledMoveText += $"({Board.GetLength(0) - source.X}{Board.GetLength(1) - source.Y})";
                }
                lastDest = destination;

            }
            if (compiledMoveText.Length > 0)
            {
                // Trim starting newline
                compiledMoveText = compiledMoveText[1..] + '\n';
            }
            kif += compiledMoveText + $" {Moves.Count + 1}  ";
            kif += !GameOver ? "中断\n\n" : state == GameState.DrawRepetition ? "千日手\n\n"
                : state is GameState.PerpetualCheckGote or GameState.PerpetualCheckSente ? "反則勝ち\n\n" : "詰み\n\n";

            return kif;
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
            if (ranks.Length is not 9 and not 5)
            {
                throw new FormatException("Board definitions must have 9 or 5 ranks separated by a forward slash");
            }

            bool minishogi = ranks.Length == 5;
            int maxIndex = minishogi ? 4 : 8;

            Pieces.Piece?[,] board = minishogi ? new Pieces.Piece?[5, 5] : new Pieces.Piece?[9, 9];
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
                            board[fileIndex, maxIndex - r] = new Pieces.King(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'G':
                            board[fileIndex, maxIndex - r] = new Pieces.GoldGeneral(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'S':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedSilverGeneral(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.SilverGeneral(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'R':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedRook(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Rook(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'B':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedBishop(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Bishop(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'N':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedKnight(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Knight(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'L':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedLance(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Lance(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'P':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedPawn(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Pawn(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'k':
                            board[fileIndex, maxIndex - r] = new Pieces.King(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'g':
                            board[fileIndex, maxIndex - r] = new Pieces.GoldGeneral(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 's':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedSilverGeneral(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.SilverGeneral(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'r':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedRook(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Rook(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'b':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedBishop(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Bishop(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'n':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedKnight(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Knight(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'l':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedLance(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Lance(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'p':
                            board[fileIndex, maxIndex - r] = promoteNextPiece 
                                ? new Pieces.PromotedPawn(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Pawn(new Point(fileIndex, maxIndex - r), false);
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
                if ((fileIndex != 9 && !minishogi) || (fileIndex != 5 && minishogi))
                {
                    throw new FormatException("Each rank in a board definition must contain definitions for 9 or 5 files");
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
            bool numberChanged = false;
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
                            int charValue = pieceChar - '0';
                            if (!numberChanged)
                            {
                                numberToAdd = charValue;
                                numberChanged = true;
                            }
                            else
                            {
                                numberToAdd *= 10;
                                numberToAdd += charValue;
                            }
                            continue;
                        }
                        else
                        {
                            throw new FormatException($"{pieceChar} is not a valid piece character");
                        }
                }
                numberToAdd = 1;
                numberChanged = false;
            }

            // Shogi Forsyth–Edwards doesn't define what the previous moves were, so they moves list starts empty
            return new ShogiGame(board, currentTurnSente, EndingStates.Contains(BoardAnalysis.DetermineGameState(board, currentTurnSente)),
                new(), new(), new(), sentePieceDrops, gotePieceDrops, new(), null);
        }
    }
}
