using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Petzold.Media2D;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private ShogiGame game = new(false);
        private readonly Settings config;

        private Pieces.Piece? grabbedPiece = null;
        /// <summary>
        /// <see langword="true"/> if the player has selected a piece but isn't dragging it, <see langword="false"/> otherwise
        /// </summary>
        private bool highlightGrabbedMoves = false;

        private Type? selectedDropType = null;

        private HashSet<System.Drawing.Point> squareHighlights = new();
        private HashSet<(System.Drawing.Point, System.Drawing.Point)> lineHighlights = new();
        private System.Drawing.Point? pointerPressedStartPoint = null;

        private bool senteIsComputer = false;
        private bool goteIsComputer = false;

        private BoardAnalysis.PossibleMove? currentBestMove = null;
        private bool manuallyEvaluating = false;

        private readonly Dictionary<Pieces.Piece, Border> pieceViews = new();

        private CancellationTokenSource cancelMoveComputation = new();

        private double tileWidth;
        private double tileHeight;

        public MainWindow()
        {
            string jsonPath = System.IO.Path.Join(AppDomain.CurrentDomain.BaseDirectory, "shogi-settings.json");
            config = File.Exists(jsonPath)
                ? JsonConvert.DeserializeObject<Settings>(File.ReadAllText(jsonPath)) ?? new Settings()
                : new Settings();

            InitializeComponent();

            shogiBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            miniShogiBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            flipBoardItem.IsChecked = config.FlipBoard;
            updateEvalAfterBotItem.IsChecked = config.UpdateEvalAfterBot;
            foreach (MenuItem? item in pieceSetItem.Items)
            {
                item?.IsChecked = config.PieceSet == item.Tag as string;
            }
            foreach (MenuItem? item in notationSetItem.Items)
            {
                item?.IsChecked = config.Notation == item.Tag as string;
            }
        }

        ~MainWindow()
        {
            Dispose();
        }

        public void Dispose()
        {
            cancelMoveComputation.Cancel();

            GC.SuppressFinalize(this);
        }

        public void UpdateGameDisplay()
        {
            shogiGameCanvas.Children.Clear();
            pieceViews.Clear();
            shogiBoardBackground.Children.Remove(sizeReference);
            miniShogiBoardBackground.Children.Remove(sizeReference);

            bool boardFlipped = config.FlipBoard && ((!game.CurrentTurnSente && !goteIsComputer) || (senteIsComputer && !goteIsComputer));
            bool minishogi = game.Board.GetLength(0) == 5;

            tileWidth = shogiGameCanvas.Bounds.Width / game.Board.GetLength(0);
            tileHeight = shogiGameCanvas.Bounds.Height / game.Board.GetLength(1);

            foreach (Grid dropItem in senteDropsPanel.Children.OfType<Grid>())
            {
                if (dropItem.Tag is not Type pieceType)
                {
                    continue;
                }
                int heldCount = game.SentePieceDrops[pieceType];
                dropItem.Opacity = heldCount == 0 ? 0.55 : 1;

                IBrush dropBackground;
                if (game.CurrentTurnSente)
                {
                    if (currentBestMove is not null && currentBestMove.Value.Source.X == -1
                        && ShogiGame.DropTypeOrder[currentBestMove.Value.Source.Y] == pieceType)
                    {
                        dropBackground = new SolidColorBrush(config.BestMoveSourceColor);
                    }
                    else if (selectedDropType == pieceType)
                    {
                        dropBackground = new SolidColorBrush(config.SelectedPieceColor);
                    }
                    else
                    {
                        dropBackground = Brushes.Transparent;
                    }
                }
                else
                {
                    dropBackground = Brushes.Transparent;
                }
                dropItem.Background = dropBackground;

                ((Label)dropItem.Children[1]).Content = heldCount;
                ((Label)dropItem.Children[1]).VerticalAlignment = boardFlipped ? VerticalAlignment.Bottom : VerticalAlignment.Top;
                ((Image)dropItem.Children[0]).Source = new Bitmap(AssetLoader.Open(new Uri(
                    $"avares://Shogi/Pieces/{config.PieceSet}/{(boardFlipped ? "Gote" : "Sente")}/{((Image)dropItem.Children[0]).Tag}.png")));
            }
            foreach (Grid dropItem in goteDropsPanel.Children.OfType<Grid>())
            {
                if (dropItem.Tag is not Type pieceType)
                {
                    continue;
                }
                int heldCount = game.GotePieceDrops[pieceType];
                dropItem.Opacity = heldCount == 0 ? 0.55 : 1;

                IBrush dropBackground;
                if (!game.CurrentTurnSente)
                {
                    if (currentBestMove is not null && currentBestMove.Value.Source.X == -1
                        && ShogiGame.DropTypeOrder[currentBestMove.Value.Source.Y] == pieceType)
                    {
                        dropBackground = new SolidColorBrush(config.BestMoveSourceColor);
                    }
                    else if (selectedDropType == pieceType)
                    {
                        dropBackground = new SolidColorBrush(config.SelectedPieceColor);
                    }
                    else
                    {
                        dropBackground = Brushes.Transparent;
                    }
                }
                else
                {
                    dropBackground = Brushes.Transparent;
                }
                dropItem.Background = dropBackground;

                ((Label)dropItem.Children[1]).Content = heldCount;
                ((Label)dropItem.Children[1]).VerticalAlignment = boardFlipped ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                ((Image)dropItem.Children[0]).Source = new Bitmap(AssetLoader.Open(new Uri(
                    $"avares://Shogi/Pieces/{config.PieceSet}/{(boardFlipped ? "Sente" : "Gote")}/{((Image)dropItem.Children[0]).Tag}.png")));
            }

            if (currentBestMove is null && !manuallyEvaluating)
            {
                if (game.CurrentTurnSente && !senteIsComputer)
                {
                    senteEvaluation.Content = "?";
                }
                else if (!goteIsComputer)
                {
                    goteEvaluation.Content = "?";
                }
            }

            if (boardFlipped)
            {
                Grid.SetColumn(senteEvaluationView, 2);
                Grid.SetRow(senteEvaluationView, 0);
                Grid.SetColumn(goteEvaluationView, 0);
                Grid.SetRow(goteEvaluationView, 2);

                Grid.SetRow(senteDropsContainer, 1);
                Grid.SetRow(goteDropsContainer, 3);

                foreach (Control child in ranksLeft.Children)
                {
                    DockPanel.SetDock(child, Dock.Bottom);
                }
                foreach (Control child in ranksRight.Children)
                {
                    DockPanel.SetDock(child, Dock.Bottom);
                }
                foreach (Control child in filesTop.Children)
                {
                    DockPanel.SetDock(child, Dock.Right);
                }
                foreach (Control child in filesBottom.Children)
                {
                    DockPanel.SetDock(child, Dock.Left);
                }
            }
            else
            {
                Grid.SetColumn(senteEvaluationView, 0);
                Grid.SetRow(senteEvaluationView, 2);
                Grid.SetColumn(goteEvaluationView, 2);
                Grid.SetRow(goteEvaluationView, 0);

                Grid.SetRow(senteDropsContainer, 3);
                Grid.SetRow(goteDropsContainer, 1);

                foreach (Control child in ranksLeft.Children)
                {
                    DockPanel.SetDock(child, Dock.Top);
                }
                foreach (Control child in ranksRight.Children)
                {
                    DockPanel.SetDock(child, Dock.Top);
                }
                foreach (Control child in filesTop.Children)
                {
                    DockPanel.SetDock(child, Dock.Left);
                }
                foreach (Control child in filesBottom.Children)
                {
                    DockPanel.SetDock(child, Dock.Right);
                }
            }

            if (minishogi)
            {
                shogiBoardBackground.IsVisible = false;
                miniShogiBoardBackground.IsVisible = true;
                miniShogiBoardBackground.Children.Add(sizeReference);
            }
            else
            {
                shogiBoardBackground.IsVisible = true;
                miniShogiBoardBackground.IsVisible = false;
                shogiBoardBackground.Children.Add(sizeReference);
            }

            movesPanel.Children.Clear();
            List<string> moves = config.Notation == "western"
                ? game.WesternMoveText
                : game.JapaneseMoveText;
            for (int i = 0; i < moves.Count; i++)
            {
                string text = $"{i + 1}. {moves[i]}";
                movesPanel.Children.Add(new Label()
                {
                    Content = text,
                    FontSize = 18
                });
            }

            GameState state = game.DetermineGameState();

            int boardMaxY = game.Board.GetLength(1) - 1;
            int boardMaxX = game.Board.GetLength(0) - 1;

            if (state is GameState.CheckMateSente or GameState.CheckMateGote)
            {
                System.Drawing.Point kingPosition = state == GameState.CheckMateSente ? game.SenteKing.Position : game.GoteKing.Position;
                Rectangle mateHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.CheckMateHighlightColor)
                };
                shogiGameCanvas.Children.Add(mateHighlight);
                Canvas.SetBottom(mateHighlight, (boardFlipped ? boardMaxY - kingPosition.Y : kingPosition.Y) * tileHeight);
                Canvas.SetLeft(mateHighlight, (boardFlipped ? boardMaxX - kingPosition.X : kingPosition.X) * tileWidth);
            }

            if (game.Moves.Count > 0)
            {
                (_, System.Drawing.Point lastMoveSource, System.Drawing.Point lastMoveDestination, _, _) = game.Moves[^1];

                if (lastMoveSource.X != -1)
                {
                    Rectangle sourceMoveHighlight = new()
                    {
                        Width = tileWidth,
                        Height = tileHeight,
                        Fill = new SolidColorBrush(config.LastMoveSourceColor)
                    };
                    shogiGameCanvas.Children.Add(sourceMoveHighlight);
                    Canvas.SetBottom(sourceMoveHighlight, (boardFlipped ? boardMaxY - lastMoveSource.Y : lastMoveSource.Y) * tileHeight);
                    Canvas.SetLeft(sourceMoveHighlight, (boardFlipped ? boardMaxX - lastMoveSource.X : lastMoveSource.X) * tileWidth);
                }

                Rectangle destinationMoveHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.LastMoveDestinationColor)
                };
                shogiGameCanvas.Children.Add(destinationMoveHighlight);
                Canvas.SetBottom(destinationMoveHighlight, (boardFlipped ? boardMaxY - lastMoveDestination.Y : lastMoveDestination.Y) * tileHeight);
                Canvas.SetLeft(destinationMoveHighlight, (boardFlipped ? boardMaxX - lastMoveDestination.X : lastMoveDestination.X) * tileWidth);

            }

            if (currentBestMove is not null
                // Prevent cases where there are no valid moves highlighting (0, 0)
                && currentBestMove.Value.Source != currentBestMove.Value.Destination)
            {
                if (currentBestMove.Value.Source.X != -1)
                {
                    Rectangle bestMoveSrcHighlight = new()
                    {
                        Width = tileWidth,
                        Height = tileHeight,
                        Fill = new SolidColorBrush(config.BestMoveSourceColor)
                    };
                    shogiGameCanvas.Children.Add(bestMoveSrcHighlight);
                    Canvas.SetBottom(bestMoveSrcHighlight,
                        (boardFlipped ? boardMaxY - currentBestMove.Value.Source.Y : currentBestMove.Value.Source.Y) * tileHeight);
                    Canvas.SetLeft(bestMoveSrcHighlight,
                        (boardFlipped ? boardMaxX - currentBestMove.Value.Source.X : currentBestMove.Value.Source.X) * tileWidth);
                }

                Rectangle bestMoveDstHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.BestMoveDestinationColor)
                };
                shogiGameCanvas.Children.Add(bestMoveDstHighlight);
                Canvas.SetBottom(bestMoveDstHighlight,
                    (boardFlipped ? boardMaxY - currentBestMove.Value.Destination.Y : currentBestMove.Value.Destination.Y) * tileHeight);
                Canvas.SetLeft(bestMoveDstHighlight,
                    (boardFlipped ? boardMaxX - currentBestMove.Value.Destination.X : currentBestMove.Value.Destination.X) * tileWidth);
            }

            if (grabbedPiece is not null && highlightGrabbedMoves)
            {
                foreach (System.Drawing.Point validMove in grabbedPiece.GetValidMoves(game.Board, true))
                {
                    Brush fillBrush;
                    if (game.Board[validMove.X, validMove.Y] is not null)
                    {
                        fillBrush = new SolidColorBrush(config.AvailableCaptureColor);
                    }
                    else
                    {
                        fillBrush = new SolidColorBrush(config.AvailableMoveColor);
                    }

                    Rectangle newRect = new()
                    {
                        Width = tileWidth,
                        Height = tileHeight,
                        Fill = fillBrush
                    };
                    shogiGameCanvas.Children.Add(newRect);
                    Canvas.SetBottom(newRect, (boardFlipped ? boardMaxY - validMove.Y : validMove.Y) * tileHeight);
                    Canvas.SetLeft(newRect, (boardFlipped ? boardMaxX - validMove.X : validMove.X) * tileWidth);
                }
            }

            if (selectedDropType is not null)
            {
                for (int x = 0; x < game.Board.GetLength(0); x++)
                {
                    for (int y = 0; y < game.Board.GetLength(1); y++)
                    {
                        if (game.IsDropPossible(selectedDropType, new System.Drawing.Point(x, y)))
                        {
                            Rectangle newRect = new()
                            {
                                Width = tileWidth,
                                Height = tileHeight,
                                Fill = new SolidColorBrush(config.AvailableMoveColor)
                            };
                            shogiGameCanvas.Children.Add(newRect);
                            Canvas.SetBottom(newRect, (boardFlipped ? boardMaxY - y : y) * tileHeight);
                            Canvas.SetLeft(newRect, (boardFlipped ? boardMaxX - x : x) * tileWidth);
                        }
                    }
                }
            }

            for (int x = 0; x < game.Board.GetLength(0); x++)
            {
                for (int y = 0; y < game.Board.GetLength(1); y++)
                {
                    Pieces.Piece? piece = game.Board[x, y];
                    if (piece is not null)
                    {
                        Brush? backgroundBrush = null;
                        if (piece is Pieces.King && ((piece.IsSente && state == GameState.CheckSente) || (!piece.IsSente && state == GameState.CheckGote)))
                        {
                            backgroundBrush = new SolidColorBrush(config.CheckedKingColor);
                        }
                        else if (highlightGrabbedMoves && piece == grabbedPiece)
                        {
                            backgroundBrush = new SolidColorBrush(config.SelectedPieceColor);
                        }

                        Border newPiece = new()
                        {
                            Child = new Image()
                            {
                                Source = new Bitmap(AssetLoader.Open(
                                    new Uri($"avares://Shogi/Pieces/{config.PieceSet}/{(piece.IsSente ^ boardFlipped ? "Sente" : "Gote")}/{piece.Name}.png")))
                            },
                            Width = tileWidth,
                            Height = tileHeight,
                            Background = backgroundBrush
                        };
                        RenderOptions.SetBitmapInterpolationMode(newPiece, BitmapInterpolationMode.HighQuality);
                        pieceViews[piece] = newPiece;
                        shogiGameCanvas.Children.Add(newPiece);
                        Canvas.SetBottom(newPiece, (boardFlipped ? boardMaxY - y : y) * tileHeight);
                        Canvas.SetLeft(newPiece, (boardFlipped ? boardMaxX - x : x) * tileWidth);
                    }
                }
            }

            foreach (System.Drawing.Point square in squareHighlights)
            {
                Ellipse ellipse = new()
                {
                    Fill = new SolidColorBrush(config.SelectedPieceColor),
                    Opacity = 0.5,
                    Width = tileWidth * 0.8,
                    Height = tileHeight * 0.8
                };
                shogiGameCanvas.Children.Add(ellipse);
                Canvas.SetBottom(ellipse, ((boardFlipped ? boardMaxY - square.Y : square.Y) * tileHeight) + (tileHeight * 0.1));
                Canvas.SetLeft(ellipse, (boardFlipped ? boardMaxX - square.X : square.X) * tileWidth + (tileWidth * 0.1));
            }

            foreach ((System.Drawing.Point lineStart, System.Drawing.Point lineEnd) in lineHighlights)
            {
                double arrowLength = Math.Min(tileWidth, tileHeight) / 4;
                ArrowLine line = new()
                {
                    Stroke = new SolidColorBrush(config.SelectedPieceColor),
                    Opacity = 0.5,
                    StrokeThickness = 10,
                    ArrowLength = arrowLength,
                    ArrowAngle = 0.785,
                    IsArrowClosed = true,
                    ArrowEnds = ArrowEnds.End,
                    X1 = (boardFlipped ? boardMaxX - lineStart.X : lineStart.X) * tileWidth + (tileWidth / 2),
                    X2 = (boardFlipped ? boardMaxX - lineEnd.X : lineEnd.X) * tileWidth + (tileWidth / 2),
                    Y1 = (boardFlipped ? lineStart.Y : boardMaxY - lineStart.Y) * tileHeight + (tileHeight / 2),
                    Y2 = (boardFlipped ? lineEnd.Y : boardMaxY - lineEnd.Y) * tileHeight + (tileHeight / 2)
                };
                shogiGameCanvas.Children.Add(line);
            }
        }

        private void UpdateCursor(Point mousePos)
        {
            if (game.GameOver)
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
                return;
            }
            if (grabbedPiece is not null && !highlightGrabbedMoves)
            {
                Cursor = new Cursor(StandardCursorType.SizeAll);
                return;
            }
            Pieces.Piece? checkPiece = GetPieceAtCanvasPoint(mousePos);
            if (checkPiece is not null && ((checkPiece.IsSente && game.CurrentTurnSente && !senteIsComputer)
                || (!checkPiece.IsSente && !game.CurrentTurnSente && !goteIsComputer)))
            {
                Cursor = new Cursor(StandardCursorType.Hand);
                return;
            }
            Cursor = new Cursor(StandardCursorType.Arrow);
        }

        /// <summary>
        /// If the game has ended, alert the user how it ended, otherwise do nothing
        /// </summary>
        private async Task PushEndgameMessage()
        {
            if (game.GameOver)
            {
                await new MessageBox(game.DetermineGameState() switch
                {
                    GameState.CheckMateSente => "Gote wins by checkmate!",
                    GameState.CheckMateGote => "Sente wins by checkmate!",
                    GameState.StalemateSente => "Gote wins by stalemate!",
                    GameState.StalemateGote => "Sente wins by stalemate!",
                    GameState.PerpetualCheckSente => "Sente wins as gote attempted to draw through perpetual check",
                    GameState.PerpetualCheckGote => "Gote wins as sente attempted to draw through perpetual check",
                    GameState.DrawRepetition => "Game drawn as the same position has occured four times",
                    _ => "Game over"
                }, "Game Over", MessageBoxButton.OK, MessageBoxImage.Information).ShowDialog(this);
            }
        }

        private void UpdateEvaluationMeter(BoardAnalysis.PossibleMove? bestMove, bool sente)
        {
            Label toUpdate = sente ? senteEvaluation : goteEvaluation;
            if (bestMove is null)
            {
                toUpdate.Content = "...";
                ToolTip.SetTip(toUpdate, null);
                return;
            }

            if ((bestMove.Value.SenteMateLocated && !bestMove.Value.GoteMateLocated)
                || double.IsNegativeInfinity(bestMove.Value.EvaluatedFutureValue))
            {
                toUpdate.Content = $"-M{bestMove.Value.DepthToSenteMate}";
            }
            else if ((bestMove.Value.GoteMateLocated && !bestMove.Value.SenteMateLocated)
                || double.IsPositiveInfinity(bestMove.Value.EvaluatedFutureValue))
            {
                toUpdate.Content = $"+M{bestMove.Value.DepthToGoteMate}";
            }
            else
            {
                toUpdate.Content = bestMove.Value.EvaluatedFutureValue.ToString("+0.00;-0.00;0.00");
            }

            string convertedBestLine = "";
            ShogiGame moveStringGenerator = game.Clone(false);
            foreach ((System.Drawing.Point source, System.Drawing.Point destination, bool doPromotion) in bestMove.Value.BestLine)
            {
                _ = moveStringGenerator.MovePiece(source, destination, forceMove: true, doPromotion: doPromotion);
                convertedBestLine += " " + moveStringGenerator.JapaneseMoveText[^1];
            }
            ToolTip.SetTip(toUpdate, convertedBestLine.Trim());
        }

        /// <summary>
        /// Get the best move according to either the built-in or external engine, depending on configuration
        /// </summary>
        private async Task<BoardAnalysis.PossibleMove> GetEngineMove(CancellationToken cancellationToken)
        {
            BoardAnalysis.PossibleMove? bestMove = null;
            // Search deeper in minishogi games
            bestMove ??= await BoardAnalysis.EstimateBestPossibleMove(game, game.Board.GetLength(0) == 5 ? 4 : 3, true, cancellationToken);
            return bestMove.Value;
        }

        /// <summary>
        /// Perform a computer move if necessary
        /// </summary>
        private async Task CheckComputerMove()
        {
            while (!game.GameOver && ((game.CurrentTurnSente && senteIsComputer) || (!game.CurrentTurnSente && goteIsComputer)))
            {
                CancellationToken cancellationToken = cancelMoveComputation.Token;
                if (config.UpdateEvalAfterBot)
                {
                    UpdateEvaluationMeter(null, game.CurrentTurnSente);
                }
                BoardAnalysis.PossibleMove bestMove = await GetEngineMove(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _ = game.MovePiece(bestMove.Source, bestMove.Destination, forceMove: true, doPromotion: bestMove.DoPromotion);
                UpdateGameDisplay();
                movesScroll.ScrollToEnd();
                if (config.UpdateEvalAfterBot)
                {
                    // Turn has been inverted already but we have value for the now old turn
                    UpdateEvaluationMeter(bestMove, !game.CurrentTurnSente);
                }
                await PushEndgameMessage();
            }
        }

        private System.Drawing.Point GetCoordFromCanvasPoint(Point position)
        {
            bool boardFlipped = config.FlipBoard && ((!game.CurrentTurnSente && !goteIsComputer) || (senteIsComputer && !goteIsComputer));
            // Canvas coordinates are relative to top-left, whereas shogi's are from bottom-left, so y is inverted
            return new System.Drawing.Point((int)((boardFlipped ? shogiGameCanvas.Bounds.Width - position.X : position.X) / tileWidth),
                (int)((!boardFlipped ? shogiGameCanvas.Bounds.Height - position.Y : position.Y) / tileHeight));
        }

        private Pieces.Piece? GetPieceAtCanvasPoint(Point position)
        {
            if (position.X < 0 || position.Y < 0
                || position.X > shogiGameCanvas.Bounds.Width || position.Y > shogiGameCanvas.Bounds.Height)
            {
                return null;
            }
            System.Drawing.Point coord = GetCoordFromCanvasPoint(position);
            return coord.X < 0 || coord.Y < 0 || coord.X >= game.Board.GetLength(0) || coord.Y >= game.Board.GetLength(1)
                ? null
                    : game.Board[coord.X, coord.Y];
        }

        private async Task NewGame(bool minishogi)
        {
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            game = new ShogiGame(minishogi);
            currentBestMove = null;
            manuallyEvaluating = false;
            grabbedPiece = null;
            highlightGrabbedMoves = false;
            selectedDropType = null;
            senteEvaluation.Content = "?";
            goteEvaluation.Content = "?";
            UpdateGameDisplay();
            UpdateCursor(new Point());
            await CheckComputerMove();
        }

        private async Task<bool?> GetDoPromotion(System.Drawing.Point destination)
        {
            if (grabbedPiece is not null && Pieces.Piece.PromotionMap.ContainsKey(grabbedPiece.GetType())
                && ((grabbedPiece.IsSente ? destination.Y >= game.PromotionZoneSenteStart : destination.Y <= game.PromotionZoneGoteStart)
                    || (grabbedPiece.IsSente ? grabbedPiece.Position.Y >= game.PromotionZoneSenteStart : grabbedPiece.Position.Y <= game.PromotionZoneGoteStart))
                && (grabbedPiece is not Pieces.Pawn and not Pieces.Lance || (destination.Y != (grabbedPiece.IsSente ? game.Board.GetLength(1) - 1 : 0)))
                && (grabbedPiece is not Pieces.Knight || (grabbedPiece.IsSente ? destination.Y < game.Board.GetLength(1) - 2 : destination.Y > 1)))
            {
                return await new MessageBox(
                    $"Do you want to promote the {grabbedPiece.Name} you just moved?", "Promotion",
                    MessageBoxButton.YesNo, MessageBoxImage.Question
                ).ShowDialog<MessageBoxResult>(this) == MessageBoxResult.Yes;
            }
            return null;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGameDisplay();
            await CheckComputerMove();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGameDisplay();
        }

        private void Window_PointerMoved(object sender, PointerEventArgs e)
        {
            Point mousePos = e.GetPosition(shogiGameCanvas);
            if (grabbedPiece is not null && !highlightGrabbedMoves)
            {
                Canvas.SetBottom(pieceViews[grabbedPiece], shogiGameCanvas.Bounds.Height - mousePos.Y - (tileHeight / 2));
                Canvas.SetLeft(pieceViews[grabbedPiece], mousePos.X - (tileWidth / 2));
            }
            UpdateCursor(mousePos);
        }

        private async void Window_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            Point mousePos = e.GetPosition(shogiGameCanvas);
            if (e.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                squareHighlights.Clear();
                lineHighlights.Clear();
                if (game.GameOver)
                {
                    return;
                }

                if (selectedDropType is not null && mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X <= shogiGameCanvas.Bounds.Width
                    && mousePos.Y <= shogiGameCanvas.Bounds.Height)
                {
                    System.Drawing.Point destination = GetCoordFromCanvasPoint(mousePos);
                    bool success = game.MovePiece(ShogiGame.PieceDropSources[selectedDropType], destination,
                        doPromotion: await GetDoPromotion(destination));
                    if (success)
                    {
                        highlightGrabbedMoves = false;
                        grabbedPiece = null;
                        currentBestMove = null;
                        selectedDropType = null;
                        UpdateCursor(mousePos);
                        UpdateGameDisplay();
                        movesScroll.ScrollToEnd();
                        await PushEndgameMessage();
                        await CheckComputerMove();
                        return;
                    }
                }
                selectedDropType = null;

                // If a piece is selected, try to move it
                if (grabbedPiece is not null && highlightGrabbedMoves)
                {
                    System.Drawing.Point destination = GetCoordFromCanvasPoint(mousePos);

                    bool success = game.MovePiece(grabbedPiece.Position, destination,
                        doPromotion: await GetDoPromotion(destination));
                    if (success)
                    {
                        highlightGrabbedMoves = false;
                        grabbedPiece = null;
                        currentBestMove = null;
                        UpdateCursor(mousePos);
                        UpdateGameDisplay();
                        movesScroll.ScrollToEnd();
                        await PushEndgameMessage();
                        await CheckComputerMove();
                        return;
                    }
                }

                highlightGrabbedMoves = false;
                Pieces.Piece? toCheck = GetPieceAtCanvasPoint(mousePos);
                if (toCheck is not null)
                {
                    if ((toCheck.IsSente && game.CurrentTurnSente && !senteIsComputer)
                        || (!toCheck.IsSente && !game.CurrentTurnSente && !goteIsComputer))
                    {
                        grabbedPiece = toCheck;
                        manuallyEvaluating = false;
                        cancelMoveComputation.Cancel();
                        cancelMoveComputation = new CancellationTokenSource();
                    }
                    else
                    {
                        grabbedPiece = null;
                    }
                }
                else
                {
                    grabbedPiece = null;
                }
            }
            else
            {
                if (mousePos.X < 0 || mousePos.Y < 0
                || mousePos.X > shogiGameCanvas.Bounds.Width || mousePos.Y > shogiGameCanvas.Bounds.Height)
                {
                    return;
                }
                pointerPressedStartPoint = GetCoordFromCanvasPoint(mousePos);
            }
            UpdateGameDisplay();
            UpdateCursor(mousePos);
        }

        private async void Window_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            Point mousePos = e.GetPosition(shogiGameCanvas);
            if (e.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                if (game.GameOver)
                {
                    return;
                }
                if (grabbedPiece is not null)
                {
                    System.Drawing.Point destination = GetCoordFromCanvasPoint(mousePos);
                    if (destination == grabbedPiece.Position)
                    {
                        highlightGrabbedMoves = true;
                        UpdateCursor(mousePos);
                        UpdateGameDisplay();
                        return;
                    }
                    bool success = game.MovePiece(grabbedPiece.Position, destination,
                        doPromotion: await GetDoPromotion(destination));
                    if (success)
                    {
                        grabbedPiece = null;
                        highlightGrabbedMoves = false;
                        currentBestMove = null;
                        UpdateCursor(mousePos);
                        UpdateGameDisplay();
                        movesScroll.ScrollToEnd();
                        await PushEndgameMessage();
                        await CheckComputerMove();
                        return;
                    }
                    else
                    {
                        highlightGrabbedMoves = true;
                    }
                }
            }
            else
            {
                if (mousePos.X < 0 || mousePos.Y < 0
                || mousePos.X > shogiGameCanvas.Bounds.Width || mousePos.Y > shogiGameCanvas.Bounds.Height)
                {
                    return;
                }
                System.Drawing.Point onSquare = GetCoordFromCanvasPoint(mousePos);
                if (pointerPressedStartPoint is null || pointerPressedStartPoint == onSquare)
                {
                    if (!squareHighlights.Add(onSquare))
                    {
                        _ = squareHighlights.Remove(onSquare);
                    }
                }
                else
                {
                    if (!lineHighlights.Add((pointerPressedStartPoint.Value, onSquare)))
                    {
                        _ = lineHighlights.Remove((pointerPressedStartPoint.Value, onSquare));
                    }
                }
            }
            UpdateCursor(mousePos);
            UpdateGameDisplay();
        }

        private void Window_PointerExited(object sender, PointerEventArgs e)
        {
            if (grabbedPiece is not null)
            {
                highlightGrabbedMoves = true;
            }
            UpdateGameDisplay();
        }

        private async void evaluation_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (currentBestMove is not null || game.GameOver
                || (game.CurrentTurnSente && senteIsComputer) || (!game.CurrentTurnSente && goteIsComputer))
            {
                return;
            }
            manuallyEvaluating = true;
            grabbedPiece = null;
            highlightGrabbedMoves = false;
            selectedDropType = null;
            UpdateEvaluationMeter(null, game.CurrentTurnSente);
            UpdateGameDisplay();
            UpdateCursor(e.GetPosition(shogiGameCanvas));

            CancellationToken cancellationToken = cancelMoveComputation.Token;
            BoardAnalysis.PossibleMove bestMove = await GetEngineMove(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            UpdateEvaluationMeter(bestMove, game.CurrentTurnSente);
            currentBestMove = bestMove;
            UpdateGameDisplay();
            manuallyEvaluating = false;
        }

        private async void NewGame_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = false;
            goteIsComputer = false;
            await NewGame(false);
        }

        private async void NewGameCpuSente_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = false;
            goteIsComputer = true;
            await NewGame(false);
        }

        private async void NewGameCpuGote_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = true;
            goteIsComputer = false;
            await NewGame(false);
        }

        private async void NewGameCpuOnly_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = true;
            goteIsComputer = true;
            await NewGame(false);
        }

        private async void NewMiniGame_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = false;
            goteIsComputer = false;
            await NewGame(true);
        }

        private async void NewMiniGameCpuSente_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = false;
            goteIsComputer = true;
            await NewGame(true);
        }

        private async void NewMiniGameCpuGote_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = true;
            goteIsComputer = false;
            await NewGame(true);
        }

        private async void NewMiniGameCpuOnly_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = true;
            goteIsComputer = true;
            await NewGame(true);
        }

        private void Window_Closing(object sender, WindowClosingEventArgs e)
        {
            cancelMoveComputation.Cancel();
            string jsonPath = System.IO.Path.Join(AppDomain.CurrentDomain.BaseDirectory, "shogi-settings.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(config));
        }

        private async void KIFExport_Click(object sender, RoutedEventArgs e)
        {
            manuallyEvaluating = false;
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            await new KIFExport(game, senteIsComputer, goteIsComputer).ShowDialog(this);
            await CheckComputerMove();
        }

        private async void CustomGame_Click(object sender, RoutedEventArgs e)
        {
            manuallyEvaluating = false;
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            CustomGame customDialog = new(config, false);
            await customDialog.ShowDialog(this);
            if (customDialog.GeneratedGame is not null)
            {
                game = customDialog.GeneratedGame;
                senteIsComputer = customDialog.SenteIsComputer;
                goteIsComputer = customDialog.GoteIsComputer;
                grabbedPiece = null;
                highlightGrabbedMoves = false;
                selectedDropType = null;
                currentBestMove = null;
                senteEvaluation.Content = "?";
                goteEvaluation.Content = "?";
                UpdateGameDisplay();
                await PushEndgameMessage();
            }
            await CheckComputerMove();
        }

        private async void CustomMiniGame_Click(object sender, RoutedEventArgs e)
        {
            manuallyEvaluating = false;
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            CustomGame customDialog = new(config, true);
            await customDialog.ShowDialog(this);
            if (customDialog.GeneratedGame is not null)
            {
                game = customDialog.GeneratedGame;
                senteIsComputer = customDialog.SenteIsComputer;
                goteIsComputer = customDialog.GoteIsComputer;
                grabbedPiece = null;
                highlightGrabbedMoves = false;
                selectedDropType = null;
                currentBestMove = null;
                senteEvaluation.Content = "?";
                goteEvaluation.Content = "?";
                UpdateGameDisplay();
                await PushEndgameMessage();
            }
            await CheckComputerMove();
        }

        private void SettingsCheckItem_Click(object sender, RoutedEventArgs e)
        {
            config.FlipBoard = flipBoardItem.IsChecked;
            config.UpdateEvalAfterBot = updateEvalAfterBotItem.IsChecked;

            UpdateGameDisplay();
        }

        private async void FENCopy_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard is null)
            {
                return;
            }
            await Clipboard.SetTextAsync(game.ToString());
        }

        private async void CustomiseItem_Click(object sender, RoutedEventArgs e)
        {
            await new Customisation(config).ShowDialog(this);
            shogiBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            miniShogiBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            UpdateGameDisplay();
        }

        private void PieceSetItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string chosenSet })
            {
                return;
            }
            config.PieceSet = chosenSet;
            UpdateGameDisplay();
        }

        private void NotationSetItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string chosenNotation })
            {
                return;
            }
            config.Notation = chosenNotation;
            UpdateGameDisplay();
        }

        private async void UndoMove_Click(object sender, RoutedEventArgs e)
        {
            if (game.PreviousGameState is not null
                && ((game.CurrentTurnSente && !senteIsComputer) || (!game.CurrentTurnSente && !goteIsComputer)))
            {
                game = game.PreviousGameState;
                if (senteIsComputer || goteIsComputer)
                {
                    // Reverse two moves if the opponent is computer controlled
                    game = game.PreviousGameState!;
                }

                grabbedPiece = null;
                highlightGrabbedMoves = false;
                selectedDropType = null;
                currentBestMove = null;
                senteEvaluation.Content = "?";
                goteEvaluation.Content = "?";
                manuallyEvaluating = false;
                cancelMoveComputation.Cancel();
                cancelMoveComputation = new CancellationTokenSource();

                UpdateGameDisplay();
                await CheckComputerMove();
            }
        }

        private void GoteDrop_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left && !game.GameOver)
            {
                if (sender is not Grid { Tag: Type clickedType })
                {
                    return;
                }
                if (game.CurrentTurnSente || goteIsComputer || game.GotePieceDrops[clickedType] == 0)
                {
                    return;
                }
                selectedDropType = clickedType;
                grabbedPiece = null;
                highlightGrabbedMoves = false;
            }
        }

        private void SenteDrop_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left && !game.GameOver)
            {
                if (sender is not Grid { Tag: Type clickedType })
                {
                    return;
                }
                if (!game.CurrentTurnSente || senteIsComputer || game.SentePieceDrops[clickedType] == 0)
                {
                    return;
                }
                selectedDropType = clickedType;
                grabbedPiece = null;
                highlightGrabbedMoves = false;
            }
        }
    }
}
