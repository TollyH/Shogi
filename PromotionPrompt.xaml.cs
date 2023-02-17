using System;
using System.Windows;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for PromotionPrompt.xaml
    /// </summary>
    public partial class PromotionPrompt : Window
    {
        /// <remarks>
        /// Defaults to <see cref="Pieces.Queen"/>
        /// </remarks>
        public Type ChosenPieceType { get; private set; }

        public PromotionPrompt(bool isSente)
        {
            ChosenPieceType = typeof(Pieces.Queen);

            InitializeComponent();

            queenButtonLabel.Content = isSente ? "♕" : "♛";
            rookButtonLabel.Content = isSente ? "♖" : "♜";
            bishopButtonLabel.Content = isSente ? "♗" : "♝";
            knightButtonLabel.Content = isSente ? "♘" : "♞";
        }

        private void queenButton_Click(object sender, RoutedEventArgs e)
        {
            ChosenPieceType = typeof(Pieces.Queen);
            Close();
        }

        private void rookButton_Click(object sender, RoutedEventArgs e)
        {
            ChosenPieceType = typeof(Pieces.Rook);
            Close();
        }

        private void bishopButton_Click(object sender, RoutedEventArgs e)
        {
            ChosenPieceType = typeof(Pieces.Bishop);
            Close();
        }

        private void knightButton_Click(object sender, RoutedEventArgs e)
        {
            ChosenPieceType = typeof(Pieces.Knight);
            Close();
        }
    }
}
