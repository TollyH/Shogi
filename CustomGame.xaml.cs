using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for CustomGame.xaml
    /// </summary>
    public partial class CustomGame : Window
    {
        public Pieces.Piece?[,] Board { get; private set; }
        public System.Drawing.Point? EnPassantSquare { get; private set; }

        public ShogiGame? GeneratedGame { get; private set; }
        public bool SenteIsComputer { get; private set; }
        public bool GoteIsComputer { get; private set; }

        private Pieces.King? senteKing = null;
        private Pieces.King? goteKing = null;

        private double tileWidth;
        private double tileHeight;

        public CustomGame()
        {
            Board = new Pieces.Piece?[8, 8];
            EnPassantSquare = null;
            GeneratedGame = null;

            InitializeComponent();
        }

        public void UpdateBoard()
        {
            tileWidth = shogiGameCanvas.ActualWidth / Board.GetLength(0);
            tileHeight = shogiGameCanvas.ActualHeight / Board.GetLength(1);

            shogiGameCanvas.Children.Clear();

            if (EnPassantSquare is not null)
            {
                Rectangle enPassantHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = Brushes.OrangeRed
                };
                _ = shogiGameCanvas.Children.Add(enPassantHighlight);
                Canvas.SetBottom(enPassantHighlight, EnPassantSquare.Value.Y * tileHeight);
                Canvas.SetLeft(enPassantHighlight, EnPassantSquare.Value.X * tileWidth);
            }

            for (int x = 0; x < Board.GetLength(0); x++)
            {
                for (int y = 0; y < Board.GetLength(1); y++)
                {
                    Pieces.Piece? piece = Board[x, y];
                    if (piece is not null)
                    {
                        Viewbox newPiece = new()
                        {
                            Child = new TextBlock()
                            {
                                Text = piece.SymbolSpecial.ToString(),
                                FontFamily = new("Segoe UI Symbol")
                            },
                            Width = tileWidth,
                            Height = tileHeight
                        };
                        _ = shogiGameCanvas.Children.Add(newPiece);
                        Canvas.SetBottom(newPiece, y * tileHeight);
                        Canvas.SetLeft(newPiece, x * tileWidth);
                    }
                }
            }

            startButton.IsEnabled = senteKing is not null && goteKing is not null;

            if (senteKing is null || senteKing.Position != new System.Drawing.Point(4, 0))
            {
                castleSenteKingside.IsChecked = false;
                castleSenteKingside.IsEnabled = false;
                castleSenteQueenside.IsChecked = false;
                castleSenteQueenside.IsEnabled = false;
            }
            else
            {
                castleSenteKingside.IsEnabled = true;
                castleSenteQueenside.IsEnabled = true;
            }

            if (Board[0, 0] is not Pieces.Rook || !Board[0, 0]!.IsSente)
            {
                castleSenteQueenside.IsChecked = false;
                castleSenteQueenside.IsEnabled = false;
            }
            if (Board[7, 0] is not Pieces.Rook || !Board[7, 0]!.IsSente)
            {
                castleSenteKingside.IsChecked = false;
                castleSenteKingside.IsEnabled = false;
            }

            if (goteKing is null || goteKing.Position != new System.Drawing.Point(4, 7))
            {
                castleGoteKingside.IsChecked = false;
                castleGoteKingside.IsEnabled = false;
                castleGoteQueenside.IsChecked = false;
                castleGoteQueenside.IsEnabled = false;
            }
            else
            {
                castleGoteKingside.IsEnabled = true;
                castleGoteQueenside.IsEnabled = true;
            }

            if (Board[0, 7] is not Pieces.Rook || Board[0, 7]!.IsSente)
            {
                castleGoteQueenside.IsChecked = false;
                castleGoteQueenside.IsEnabled = false;
            }
            if (Board[7, 7] is not Pieces.Rook || Board[7, 7]!.IsSente)
            {
                castleGoteKingside.IsChecked = false;
                castleGoteKingside.IsEnabled = false;
            }
        }

        private void clearEnPassantButton_Click(object sender, RoutedEventArgs e)
        {
            EnPassantSquare = null;
            UpdateBoard();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            SenteIsComputer = computerSelectSente.IsChecked ?? false;
            GoteIsComputer = computerSelectGote.IsChecked ?? false;
            bool currentTurnSente = turnSelectSente.IsChecked ?? false;
            // For the PGN standard, if gote moves first then a single move "..." is added to the start of the move text list
            GeneratedGame = new ShogiGame(Board, currentTurnSente,
                ShogiGame.EndingStates.Contains(BoardAnalysis.DetermineGameState(Board, currentTurnSente)),
                new(), currentTurnSente ? new() : new() { "..." }, new(), EnPassantSquare, castleSenteKingside.IsChecked ?? false,
                castleSenteQueenside.IsChecked ?? false, castleGoteKingside.IsChecked ?? false,
                castleGoteQueenside.IsChecked ?? false, 0, new(), null);
            Close();
        }

        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            importOverlay.Visibility = Visibility.Visible;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point mousePoint = Mouse.GetPosition(shogiGameCanvas);
            if (mousePoint.X < 0 || mousePoint.Y < 0
                || mousePoint.X > shogiGameCanvas.ActualWidth || mousePoint.Y > shogiGameCanvas.ActualHeight)
            {
                return;
            }

            // Canvas coordinates are relative to top-left, whereas shogi's are from bottom-left, so y is inverted
            System.Drawing.Point coord = new((int)(mousePoint.X / tileWidth),
                (int)((shogiGameCanvas.ActualHeight - mousePoint.Y) / tileHeight));
            if (coord.X < 0 || coord.Y < 0 || coord.X >= Board.GetLength(0) || coord.Y >= Board.GetLength(1))
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Left && (enPassantSelect.IsChecked ?? false))
            {
                EnPassantSquare = coord;
            }
            else
            {
                if (Board[coord.X, coord.Y] is null)
                {
                    bool sente = e.ChangedButton == MouseButton.Left;
                    if (pieceSelectKing.IsChecked ?? false)
                    {
                        // Only allow one king of each colour
                        if (sente && senteKing is null)
                        {
                            senteKing = new Pieces.King(coord, true);
                            Board[coord.X, coord.Y] = senteKing;
                        }
                        else if (!sente && goteKing is null)
                        {
                            goteKing = new Pieces.King(coord, false);
                            Board[coord.X, coord.Y] = goteKing;
                        }
                    }
                    else if (pieceSelectQueen.IsChecked ?? false)
                    {
                        Board[coord.X, coord.Y] = new Pieces.Queen(coord, sente);
                    }
                    else if (pieceSelectRook.IsChecked ?? false)
                    {
                        Board[coord.X, coord.Y] = new Pieces.Rook(coord, sente);
                    }
                    else if (pieceSelectBishop.IsChecked ?? false)
                    {
                        Board[coord.X, coord.Y] = new Pieces.Bishop(coord, sente);
                    }
                    else if (pieceSelectKnight.IsChecked ?? false)
                    {
                        Board[coord.X, coord.Y] = new Pieces.Knight(coord, sente);
                    }
                    else if (pieceSelectPawn.IsChecked ?? false)
                    {
                        Board[coord.X, coord.Y] = new Pieces.Pawn(coord, sente);
                    }
                }
                else
                {
                    if (Board[coord.X, coord.Y] is Pieces.King king)
                    {
                        if (king.IsSente)
                        {
                            senteKing = null;
                        }
                        else
                        {
                            goteKing = null;
                        }
                    }
                    Board[coord.X, coord.Y] = null;
                }
            }

            UpdateBoard();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBoard();
        }

        private void submitFenButton_Click(object sender, RoutedEventArgs e)
        {
            SenteIsComputer = computerSelectSente.IsChecked ?? false;
            GoteIsComputer = computerSelectGote.IsChecked ?? false;
            try
            {
                GeneratedGame = ShogiGame.FromForsythEdwards(fenInput.Text);
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Forsyth–Edwards Notation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cancelFenButton_Click(object sender, RoutedEventArgs e)
        {
            importOverlay.Visibility = Visibility.Hidden;
        }
    }
}
