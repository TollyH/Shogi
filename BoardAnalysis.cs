﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shogi
{
    public static class BoardAnalysis
    {
        private static readonly Random rng = new();

        /// <summary>
        /// Determine whether a king can be reached by any of the opponents pieces
        /// </summary>
        /// <param name="board">The state of the board to check</param>
        /// <param name="isSente">Is the king to check sente?</param>
        /// <param name="target">Override the position of the king to check</param>
        /// <remarks><paramref name="target"/> should always be given if checking a not-yet-performed king move, as the king's internally stored position will be incorrect.</remarks>
        public static bool IsKingReachable(Pieces.Piece?[,] board, bool isSente, Point? target = null)
        {
            target ??= board.OfType<Pieces.King>().Where(x => x.IsSente == isSente).First().Position;

            int backwardsY = isSente ? -1 : 1;
            // King, promoted bishop straights, promoted rook diagonals, gold general (and equivalents), silver general check
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy != 0 || dx != 0)
                    {
                        Point newPos = new(target.Value.X + dx, target.Value.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && board[newPos.X, newPos.Y] is Pieces.Piece piece && board[newPos.X, newPos.Y]!.IsSente != isSente)
                        {
                            if (piece is Pieces.King or Pieces.PromotedBishop or Pieces.PromotedRook)
                            {
                                return true;
                            }
                            if (piece is Pieces.GoldGeneral or Pieces.PromotedKnight or Pieces.PromotedLance
                                or Pieces.PromotedPawn or Pieces.PromotedSilverGeneral && (dy != backwardsY || dx == 0))
                            {
                                return true;
                            }
                            if (piece is Pieces.SilverGeneral && (dy != backwardsY || dx != 0) && dy != 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // Straight checks (rook & lance)
            for (int dx = target.Value.X + 1; dx < board.GetLength(0); dx++)
            {
                Point newPos = new(dx, target.Value.Y);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dx = target.Value.X - 1; dx >= 0; dx--)
            {
                Point newPos = new(dx, target.Value.Y);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dy = target.Value.Y + 1; dy < board.GetLength(1); dy++)
            {
                Point newPos = new(target.Value.X, dy);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        (board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook
                            || (board[newPos.X, newPos.Y] is Pieces.Lance lance && !lance.IsSente)))
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dy = target.Value.Y - 1; dy >= 0; dy--)
            {
                Point newPos = new(target.Value.X, dy);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        (board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook
                            || (board[newPos.X, newPos.Y] is Pieces.Lance lance && lance.IsSente)))
                    {
                        return true;
                    }
                    break;
                }
            }

            // Diagonal checks (bishop)
            for (int dif = 1; target.Value.X + dif < board.GetLength(0) && target.Value.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(target.Value.X + dif, target.Value.Y + dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dif = 1; target.Value.X - dif >= 0 && target.Value.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(target.Value.X - dif, target.Value.Y + dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dif = 1; target.Value.X - dif >= 0 && target.Value.Y - dif >= 0; dif++)
            {
                Point newPos = new(target.Value.X - dif, target.Value.Y - dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dif = 1; target.Value.X + dif < board.GetLength(0) && target.Value.Y - dif >= 0; dif++)
            {
                Point newPos = new(target.Value.X + dif, target.Value.Y - dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }

            // Knight checks
            int knightDY = isSente ? 2 : -2;
            Point knightPos = new(target.Value.X + 1, target.Value.Y + knightDY);
            if (knightPos.X >= 0 && knightPos.Y >= 0 && knightPos.X < board.GetLength(0) && knightPos.Y < board.GetLength(1)
                && board[knightPos.X, knightPos.Y] is Pieces.Knight && board[knightPos.X, knightPos.Y]!.IsSente != isSente)
            {
                return true;
            }
            knightPos = new(target.Value.X - 1, target.Value.Y + knightDY);
            if (knightPos.X >= 0 && knightPos.Y >= 0 && knightPos.X < board.GetLength(0) && knightPos.Y < board.GetLength(1)
                && board[knightPos.X, knightPos.Y] is Pieces.Knight && board[knightPos.X, knightPos.Y]!.IsSente != isSente)
            {
                return true;
            }

            // Pawn checks
            int pawnYDiff = isSente ? 1 : -1;
            int newY = target.Value.Y + pawnYDiff;
            if (newY < board.GetLength(1) && newY >= 0)
            {
                if (board[target.Value.X, newY] is Pieces.Pawn && board[target.Value.X, newY]!.IsSente != isSente)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine the current state of the game with the given board.
        /// </summary>
        /// <remarks>
        /// This method will not detect states that depend on game history, such as repetition
        /// </remarks>
        public static GameState DetermineGameState(Pieces.Piece?[,] board, bool currentTurnSente,
            Point? senteKingPos = null, Point? goteKingPos = null)
        {
            IEnumerable<Pieces.Piece> sentePieces = board.OfType<Pieces.Piece>().Where(p => p.IsSente);
            IEnumerable<Pieces.Piece> gotePieces = board.OfType<Pieces.Piece>().Where(p => !p.IsSente);

            bool senteCheck = IsKingReachable(board, true, senteKingPos ?? null);
            // Sente and Gote cannot both be in check
            bool goteCheck = !senteCheck && IsKingReachable(board, false, goteKingPos ?? null);

            if (currentTurnSente && !sentePieces.SelectMany(p => p.GetValidMoves(board, true)).Any())
            {
                return senteCheck ? GameState.CheckMateSente : GameState.StalemateSente;
            }
            if (!currentTurnSente && !gotePieces.SelectMany(p => p.GetValidMoves(board, true)).Any())
            {
                return goteCheck ? GameState.CheckMateGote : GameState.StalemateGote;
            }

            return senteCheck ? GameState.CheckSente : goteCheck ? GameState.CheckGote : GameState.StandardPlay;
        }

        /// <summary>
        /// Calculate the value of the given game based on the pieces on the board and in hand
        /// </summary>
        /// <returns>
        /// A <see cref="double"/> representing the total piece value of the game.
        /// Positive means sente has stronger material, negative means gote does.
        /// </returns>
        public static double CalculateGameValue(ShogiGame game)
        {
            double inHandTotal = 0;
            foreach ((Type dropType, int count) in game.SentePieceDrops)
            {
                inHandTotal += count * Pieces.Piece.DefaultPieces[dropType].Value;
            }
            foreach ((Type dropType, int count) in game.GotePieceDrops)
            {
                inHandTotal -= count * Pieces.Piece.DefaultPieces[dropType].Value;
            }
            return inHandTotal + game.Board.OfType<Pieces.Piece>().Sum(p => p.IsSente ? p.Value : -p.Value);
        }

        public readonly struct PossibleMove
        {
            public Point Source { get; }
            public Point Destination { get; }
            public double EvaluatedFutureValue { get; }
            public bool SenteMateLocated { get; }
            public bool GoteMateLocated { get; }
            public int DepthToSenteMate { get; }
            public int DepthToGoteMate { get; }
            public bool DoPromotion { get; }
            public List<(Point, Point, bool)> BestLine { get;  }

            public PossibleMove(Point source, Point destination, double evaluatedFutureValue,
                bool senteMateLocated, bool goteMateLocated, int depthToSenteMate, int depthToGoteMate,
                bool doPromotion, List<(Point, Point, bool)> bestLine)
            {
                Source = source;
                Destination = destination;
                EvaluatedFutureValue = evaluatedFutureValue;
                SenteMateLocated = senteMateLocated;
                GoteMateLocated = goteMateLocated;
                DepthToSenteMate = depthToSenteMate;
                DepthToGoteMate = depthToGoteMate;
                DoPromotion = doPromotion;
                BestLine = bestLine;
            }
        }

        /// <summary>
        /// Use <see cref="EvaluatePossibleMoves"/> to find the best possible move in the current state of the game
        /// </summary>
        /// <param name="maxDepth">The maximum number of half-moves in the future to search</param>
        /// <param name="randomise">Whether or not to randomise the order of moves that have the same score</param>
        public static async Task<PossibleMove> EstimateBestPossibleMove(ShogiGame game, int maxDepth, bool randomise, CancellationToken cancellationToken)
        {
            PossibleMove[] moves = await EvaluatePossibleMoves(game, maxDepth, randomise, cancellationToken);
            PossibleMove bestMove = new(default, default,
                game.CurrentTurnSente ? double.NegativeInfinity : double.PositiveInfinity, false, false, 0, 0, false, new());
            foreach (PossibleMove potentialMove in moves)
            {
                if (game.CurrentTurnSente)
                {
                    if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                        || (!bestMove.GoteMateLocated && potentialMove.GoteMateLocated)
                        || (!bestMove.GoteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                        || (bestMove.GoteMateLocated && potentialMove.GoteMateLocated
                            && potentialMove.DepthToGoteMate < bestMove.DepthToGoteMate))
                    {
                        bestMove = potentialMove;
                    }
                }
                else
                {
                    if (bestMove.EvaluatedFutureValue == double.PositiveInfinity
                        || (!bestMove.SenteMateLocated && potentialMove.SenteMateLocated)
                        || (!bestMove.SenteMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                        || (bestMove.SenteMateLocated && potentialMove.SenteMateLocated
                            && potentialMove.DepthToSenteMate < bestMove.DepthToSenteMate))
                    {
                        bestMove = potentialMove;
                    }
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }
            return bestMove;
        }

        /// <summary>
        /// Evaluate each possible move in the current state of the game
        /// </summary>
        /// <param name="maxDepth">The maximum number of half-moves in the future to search</param>
        /// <param name="randomise">Whether or not to randomise the order of moves that have the same score</param>
        /// <returns>An array of all possible moves, with information on board value and ability to checkmate</returns>
        public static async Task<PossibleMove[]> EvaluatePossibleMoves(ShogiGame game, int maxDepth, bool randomise, CancellationToken cancellationToken)
        {
            List<Task<PossibleMove>> evaluationTasks = new();

            foreach (Pieces.Piece? piece in game.Board)
            {
                if (piece is not null)
                {
                    if (piece.IsSente != game.CurrentTurnSente)
                    {
                        continue;
                    }

                    foreach (Point validMove in GetValidMovesForEval(game, piece))
                    {
                        if (Pieces.Piece.PromotionMap.ContainsKey(piece.GetType())
                            && ((piece.IsSente ? validMove.Y >= game.PromotionZoneSenteStart : validMove.Y <= game.PromotionZoneGoteStart)
                                || (piece.IsSente ? piece.Position.Y >= game.PromotionZoneSenteStart : piece.Position.Y <= game.PromotionZoneGoteStart)))
                        {
                            Point promotionMove = validMove;
                            evaluationTasks.Add(Task.Run(() =>
                            {
                                ShogiGame promotionGameClone = game.Clone(false);
                                List<(Point, Point, bool)> promotionLine = new() { (piece.Position, promotionMove, true) };
                                _ = promotionGameClone.MovePiece(piece.Position, promotionMove, true,
                                    doPromotion: true, updateMoveText: false);

                                PossibleMove bestSubMove = MinimaxMove(promotionGameClone,
                                    double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, promotionLine, cancellationToken);

                                return new PossibleMove(piece.Position, promotionMove, bestSubMove.EvaluatedFutureValue,
                                    bestSubMove.SenteMateLocated, bestSubMove.GoteMateLocated,
                                    bestSubMove.DepthToSenteMate, bestSubMove.DepthToGoteMate, true, bestSubMove.BestLine);
                            }, cancellationToken));
                        }
                        if ((piece is not Pieces.Pawn and not Pieces.Lance || validMove.Y != (piece.IsSente ? game.Board.GetLength(1) - 1 : 0))
                            && (piece is not Pieces.Knight || !(piece.IsSente ? validMove.Y >= game.Board.GetLength(1) - 2 : validMove.Y <= 1)))
                        {
                            Point thisValidMove = validMove;
                            evaluationTasks.Add(Task.Run(() =>
                            {
                                ShogiGame gameClone = game.Clone(false);
                                List<(Point, Point, bool)> thisLine = new() { (piece.Position, thisValidMove, false) };
                                _ = gameClone.MovePiece(piece.Position, thisValidMove, true,
                                    doPromotion: false, updateMoveText: false);

                                PossibleMove bestSubMove = MinimaxMove(gameClone,
                                    double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                                
                                return new PossibleMove(piece.Position, thisValidMove, bestSubMove.EvaluatedFutureValue,
                                    bestSubMove.SenteMateLocated, bestSubMove.GoteMateLocated,
                                    bestSubMove.DepthToSenteMate, bestSubMove.DepthToGoteMate, false, bestSubMove.BestLine);
                            }, cancellationToken));
                        }
                    }
                }
            }

            Dictionary<Type, int> dropCounts = game.CurrentTurnSente ? game.SentePieceDrops : game.GotePieceDrops;
            foreach ((Type dropType, int count) in dropCounts)
            {
                if (count > 0)
                {
                    for (int x = 0; x < game.Board.GetLength(0); x++)
                    {
                        for (int y = 0; y < game.Board.GetLength(1); y++)
                        {
                            Point pt = new(x, y);
                            if (game.IsDropPossible(dropType, pt))
                            {
                                Point thisDropPoint = pt;
                                evaluationTasks.Add(Task.Run(() =>
                                {
                                    Point thisDropSource = ShogiGame.PieceDropSources[dropType];
                                    ShogiGame gameClone = game.Clone(false);
                                    List<(Point, Point, bool)> thisLine = new() { (thisDropSource, thisDropPoint, false) };
                                    _ = gameClone.MovePiece(thisDropSource, thisDropPoint, true,
                                        doPromotion: false, updateMoveText: false);

                                    PossibleMove bestSubMove = MinimaxMove(gameClone,
                                        double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                                    
                                    return new PossibleMove(thisDropSource, pt,
                                        bestSubMove.EvaluatedFutureValue, bestSubMove.SenteMateLocated, bestSubMove.GoteMateLocated,
                                        bestSubMove.DepthToSenteMate, bestSubMove.DepthToGoteMate, false, bestSubMove.BestLine);
                                }, cancellationToken));
                            }
                        }
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Array.Empty<PossibleMove>();
            }
            try
            {
                IEnumerable<PossibleMove> moves =
                    (await Task.WhenAll(evaluationTasks)).Where(m => m.Source != m.Destination);
                if (randomise)
                {
                    return moves.OrderBy(_ => rng.Next()).ToArray();
                }
                // Remove default moves from return value
                return moves.ToArray();
            }
            catch (TaskCanceledException)
            {
                return Array.Empty<PossibleMove>();
            }
        }

        private static HashSet<Point> GetValidMovesForEval(ShogiGame game, Pieces.Piece piece)
        {
            return piece.GetValidMoves(game.Board, true);
        }

        private static PossibleMove MinimaxMove(ShogiGame game, double alpha, double beta, int depth, int maxDepth,
            List<(Point, Point, bool)> currentLine, CancellationToken cancellationToken)
        {
            (_, Point lastMoveSrc, Point lastMoveDst, _, _) = game.Moves.Last();
            if (game.GameOver)
            {
                GameState state = game.DetermineGameState();
                if (state is GameState.CheckMateSente or GameState.PerpetualCheckGote or GameState.StalemateSente)
                {
                    return new PossibleMove(lastMoveSrc, lastMoveDst, double.NegativeInfinity, true, false, depth, 0, false,
                        currentLine);
                }
                else if (state is GameState.CheckMateGote or GameState.PerpetualCheckSente or GameState.StalemateGote)
                {
                    return new PossibleMove(lastMoveSrc, lastMoveDst, double.PositiveInfinity, false, true, 0, depth, false,
                        currentLine);
                }
                else
                {
                    // Draw
                    return new PossibleMove(lastMoveSrc, lastMoveDst, 0, false, false, 0, 0, false, currentLine);
                }
            }
            if (depth > maxDepth)
            {
                return new PossibleMove(lastMoveSrc, lastMoveDst, CalculateGameValue(game), false, false, 0, 0, false, currentLine);
            }

            PossibleMove bestMove = new(default, default,
                game.CurrentTurnSente ? double.NegativeInfinity : double.PositiveInfinity, false, false, 0, 0, false, new());

            foreach (Pieces.Piece? piece in game.Board)
            {
                if (piece is not null)
                {
                    if (piece.IsSente != game.CurrentTurnSente)
                    {
                        continue;
                    }

                    foreach (Point validMove in GetValidMovesForEval(game, piece))
                    {
                        List<bool> availablePromotions = new(2);
                        if (Pieces.Piece.PromotionMap.ContainsKey(piece.GetType())
                            && piece.IsSente ? validMove.Y >= game.PromotionZoneSenteStart : validMove.Y <= game.PromotionZoneGoteStart)
                        {
                            availablePromotions.Add(true);
                        }
                        if ((piece is not Pieces.Pawn and not Pieces.Lance || (validMove.Y != 0 && validMove.Y != game.Board.GetLength(1) - 1))
                            && (piece is not Pieces.Knight || (validMove.Y < game.Board.GetLength(1) - 2 && validMove.Y > 1)))
                        {
                            availablePromotions.Add(false);
                        }
                        foreach (bool doPromotion in availablePromotions)
                        {
                            ShogiGame gameClone = game.Clone(false);
                            List<(Point, Point, bool)> newLine = new(currentLine) { (piece.Position, validMove, doPromotion) };
                            _ = gameClone.MovePiece(piece.Position, validMove, true,
                                doPromotion: doPromotion, updateMoveText: false);
                            PossibleMove potentialMove = MinimaxMove(gameClone, alpha, beta, depth + 1, maxDepth, newLine, cancellationToken);
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return bestMove;
                            }
                            if (game.CurrentTurnSente)
                            {
                                if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                                    || (!bestMove.GoteMateLocated && potentialMove.GoteMateLocated)
                                    || (!bestMove.GoteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                                    || (bestMove.GoteMateLocated && potentialMove.GoteMateLocated
                                        && potentialMove.DepthToGoteMate < bestMove.DepthToGoteMate))
                                {
                                    bestMove = new PossibleMove(piece.Position, validMove, potentialMove.EvaluatedFutureValue,
                                        potentialMove.SenteMateLocated, potentialMove.GoteMateLocated,
                                        potentialMove.DepthToSenteMate, potentialMove.DepthToGoteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                }
                                if (potentialMove.EvaluatedFutureValue >= beta && !bestMove.GoteMateLocated)
                                {
                                    return bestMove;
                                }
                                if (potentialMove.EvaluatedFutureValue > alpha)
                                {
                                    alpha = potentialMove.EvaluatedFutureValue;
                                }
                            }
                            else
                            {
                                if (bestMove.EvaluatedFutureValue == double.PositiveInfinity
                                    || (!bestMove.SenteMateLocated && potentialMove.SenteMateLocated)
                                    || (!bestMove.SenteMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                                    || (bestMove.SenteMateLocated && potentialMove.SenteMateLocated
                                        && potentialMove.DepthToSenteMate < bestMove.DepthToSenteMate))
                                {
                                    bestMove = new PossibleMove(piece.Position, validMove, potentialMove.EvaluatedFutureValue,
                                        potentialMove.SenteMateLocated, potentialMove.GoteMateLocated,
                                        potentialMove.DepthToSenteMate, potentialMove.DepthToGoteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                }
                                if (potentialMove.EvaluatedFutureValue <= alpha && !bestMove.SenteMateLocated)
                                {
                                    return bestMove;
                                }
                                if (potentialMove.EvaluatedFutureValue < beta)
                                {
                                    beta = potentialMove.EvaluatedFutureValue;
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<Type, int> dropCounts = game.CurrentTurnSente ? game.SentePieceDrops : game.GotePieceDrops;
            foreach ((Type dropType, int count) in dropCounts)
            {
                if (count > 0)
                {
                    for (int x = 0; x < game.Board.GetLength(0); x++)
                    {
                        for (int y = 0; y < game.Board.GetLength(1); y++)
                        {
                            Point pt = new(x, y);
                            if (game.IsDropPossible(dropType, pt))
                            {
                                ShogiGame gameClone = game.Clone(false);
                                Point dropPoint = pt;
                                Point dropSource = ShogiGame.PieceDropSources[dropType];
                                List<(Point, Point, bool)> newLine = new(currentLine) { (dropSource, dropPoint, false) };
                                _ = gameClone.MovePiece(dropSource, dropPoint, true,
                                    doPromotion: true, updateMoveText: false);
                                PossibleMove potentialMove = MinimaxMove(gameClone, alpha, beta, depth + 1, maxDepth, newLine, cancellationToken);
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return bestMove;
                                }
                                if (game.CurrentTurnSente)
                                {
                                    if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                                        || (!bestMove.GoteMateLocated && potentialMove.GoteMateLocated)
                                        || (!bestMove.GoteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                                        || (bestMove.GoteMateLocated && potentialMove.GoteMateLocated
                                            && potentialMove.DepthToGoteMate < bestMove.DepthToGoteMate))
                                    {
                                        bestMove = new PossibleMove(dropSource, dropPoint, potentialMove.EvaluatedFutureValue,
                                            potentialMove.SenteMateLocated, potentialMove.GoteMateLocated,
                                            potentialMove.DepthToSenteMate, potentialMove.DepthToGoteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                    }
                                    if (potentialMove.EvaluatedFutureValue >= beta && !bestMove.GoteMateLocated)
                                    {
                                        return bestMove;
                                    }
                                    if (potentialMove.EvaluatedFutureValue > alpha)
                                    {
                                        alpha = potentialMove.EvaluatedFutureValue;
                                    }
                                }
                                else
                                {
                                    if (bestMove.EvaluatedFutureValue == double.PositiveInfinity
                                        || (!bestMove.SenteMateLocated && potentialMove.SenteMateLocated)
                                        || (!bestMove.SenteMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                                        || (bestMove.SenteMateLocated && potentialMove.SenteMateLocated
                                            && potentialMove.DepthToSenteMate < bestMove.DepthToSenteMate))
                                    {
                                        bestMove = new PossibleMove(dropSource, dropPoint, potentialMove.EvaluatedFutureValue,
                                            potentialMove.SenteMateLocated, potentialMove.GoteMateLocated,
                                            potentialMove.DepthToSenteMate, potentialMove.DepthToGoteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                    }
                                    if (potentialMove.EvaluatedFutureValue <= alpha && !bestMove.SenteMateLocated)
                                    {
                                        return bestMove;
                                    }
                                    if (potentialMove.EvaluatedFutureValue < beta)
                                    {
                                        beta = potentialMove.EvaluatedFutureValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return bestMove;
        }
    }
}
