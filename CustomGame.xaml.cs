using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.Generic;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for CustomGame.xaml
    /// </summary>
    public partial class CustomGame : Window
    {
        public Pieces.Piece?[,] Board { get; private set; }

        public ShogiGame? GeneratedGame { get; private set; }
        public bool SenteIsComputer { get; private set; }
        public bool GoteIsComputer { get; private set; }

        private readonly Settings config;

        private readonly Dictionary<Type, int> sentePieceDrops = new()
        {
            { typeof(Pieces.GoldGeneral), 0 },
            { typeof(Pieces.SilverGeneral), 0 },
            { typeof(Pieces.Rook), 0 },
            { typeof(Pieces.Bishop), 0 },
            { typeof(Pieces.Knight), 0 },
            { typeof(Pieces.Lance), 0 },
            { typeof(Pieces.Pawn), 0 },
        };
        private readonly Dictionary<Type, int> gotePieceDrops = new()
        {
            { typeof(Pieces.GoldGeneral), 0 },
            { typeof(Pieces.SilverGeneral), 0 },
            { typeof(Pieces.Rook), 0 },
            { typeof(Pieces.Bishop), 0 },
            { typeof(Pieces.Knight), 0 },
            { typeof(Pieces.Lance), 0 },
            { typeof(Pieces.Pawn), 0 },
        };

        private Pieces.King? senteKing = null;
        private Pieces.King? goteKing = null;

        private double tileWidth;
        private double tileHeight;

        public CustomGame(Settings config)
        {
            Board = new Pieces.Piece?[9, 9];
            GeneratedGame = null;
            this.config = config;

            InitializeComponent();
        }

        public void UpdateBoard()
        {
            tileWidth = shogiGameCanvas.ActualWidth / Board.GetLength(0);
            tileHeight = shogiGameCanvas.ActualHeight / Board.GetLength(1);

            shogiGameCanvas.Children.Clear();

            for (int x = 0; x < Board.GetLength(0); x++)
            {
                for (int y = 0; y < Board.GetLength(1); y++)
                {
                    Pieces.Piece? piece = Board[x, y];
                    if (piece is not null)
                    {
                        Image newPiece = new()
                        {
                            Source = new BitmapImage(
                                new Uri($"pack://application:,,,/Pieces/{config.PieceSet}/{(piece.IsSente ? "Sente" : "Gote")}/{piece.Name}.png")),
                            Width = tileWidth,
                            Height = tileHeight
                        };
                        RenderOptions.SetBitmapScalingMode(newPiece, BitmapScalingMode.HighQuality);
                        _ = shogiGameCanvas.Children.Add(newPiece);
                        Canvas.SetBottom(newPiece, y * tileHeight);
                        Canvas.SetLeft(newPiece, x * tileWidth);
                    }
                }
            }

            foreach (Grid dropItem in senteDropsPanel.Children)
            {
                Type pieceType = (Type)dropItem.Tag;
                int heldCount = sentePieceDrops[pieceType];
                dropItem.Opacity = heldCount == 0 ? 0.55 : 1;
                ((Label)dropItem.Children[1]).Content = heldCount;
                ((Image)dropItem.Children[0]).Source = new BitmapImage(new Uri(
                    $"pack://application:,,,/Pieces/{config.PieceSet}/Sente/{((Image)dropItem.Children[0]).Tag}.png"));
            }
            foreach (Grid dropItem in goteDropsPanel.Children)
            {
                Type pieceType = (Type)dropItem.Tag;
                int heldCount = gotePieceDrops[pieceType];
                dropItem.Opacity = heldCount == 0 ? 0.55 : 1;
                ((Label)dropItem.Children[1]).Content = heldCount;
                ((Image)dropItem.Children[0]).Source = new BitmapImage(new Uri(
                    $"pack://application:,,,/Pieces/{config.PieceSet}/Gote/{((Image)dropItem.Children[0]).Tag}.png"));
            }

            startButton.IsEnabled = senteKing is not null && goteKing is not null;
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            SenteIsComputer = computerSelectSente.IsChecked ?? false;
            GoteIsComputer = computerSelectGote.IsChecked ?? false;
            bool currentTurnSente = turnSelectSente.IsChecked ?? false;
            GeneratedGame = new ShogiGame(Board, currentTurnSente,
                ShogiGame.EndingStates.Contains(BoardAnalysis.DetermineGameState(Board, currentTurnSente)),
                new(), new(), sentePieceDrops, gotePieceDrops, new(), null);
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
                else if (pieceSelectGoldGeneral.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.GoldGeneral(coord, sente);
                }
                else if (pieceSelectSilverGeneral.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.SilverGeneral(coord, sente);
                }
                else if (pieceSelectPromotedSilverGeneral.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedSilverGeneral(coord, sente);
                }
                else if (pieceSelectRook.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Rook(coord, sente);
                }
                else if (pieceSelectPromotedRook.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedRook(coord, sente);
                }
                else if (pieceSelectBishop.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Bishop(coord, sente);
                }
                else if (pieceSelectPromotedBishop.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedBishop(coord, sente);
                }
                else if (pieceSelectKnight.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Knight(coord, sente);
                }
                else if (pieceSelectPromotedKnight.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedKnight(coord, sente);
                }
                else if (pieceSelectLance.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Lance(coord, sente);
                }
                else if (pieceSelectPromotedLance.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedLance(coord, sente);
                }
                else if (pieceSelectPawn.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Pawn(coord, sente);
                }
                else if (pieceSelectPromotedPawn.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedPawn(coord, sente);
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
                GeneratedGame = ShogiGame.FromShogiForsythEdwards(sfenInput.Text);
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Shogi Forsyth–Edwards Notation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cancelFenButton_Click(object sender, RoutedEventArgs e)
        {
            importOverlay.Visibility = Visibility.Hidden;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBoard();
        }

        private void GoteDrop_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Type clickedType = (Type)((Grid)sender).Tag;
            if (e.ChangedButton == MouseButton.Left && gotePieceDrops[clickedType] < 9)
            {
                gotePieceDrops[clickedType]++;
            }
            else if (e.ChangedButton == MouseButton.Right && gotePieceDrops[clickedType] != 0)
            {
                gotePieceDrops[clickedType]--;
            }
            UpdateBoard();
        }

        private void SenteDrop_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Type clickedType = (Type)((Grid)sender).Tag;
            if (e.ChangedButton == MouseButton.Left && sentePieceDrops[clickedType] < 9)
            {
                sentePieceDrops[clickedType]++;
            }
            else if (e.ChangedButton == MouseButton.Right && sentePieceDrops[clickedType] != 0)
            {
                sentePieceDrops[clickedType]--;
            }
            UpdateBoard();
        }
    }
}
