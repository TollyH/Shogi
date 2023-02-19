using System.Windows;
using System.Windows.Media;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for Customisation.xaml
    /// </summary>
    public partial class Customisation : Window
    {
        public Settings Config { get; }

        private bool performRefresh = false;

        public Customisation(Settings config)
        {
            Config = config;
            InitializeComponent();

            boardPicker.SelectedColor = Config.BoardColor;
            checkKingPicker.SelectedColor = Config.CheckedKingColor;
            selectedPiecePicker.SelectedColor = Config.SelectedPieceColor;
            checkmatePicker.SelectedColor = Config.CheckMateHighlightColor;
            lastMoveSourcePicker.SelectedColor = Config.LastMoveSourceColor;
            lastMoveDestinationPicker.SelectedColor = Config.LastMoveDestinationColor;
            bestMoveSourcePicker.SelectedColor = Config.BestMoveSourceColor;
            bestMoveDestinationPicker.SelectedColor = Config.BestMoveDestinationColor;
            availableMovePicker.SelectedColor = Config.AvailableMoveColor;
            availableCapturePicker.SelectedColor = Config.AvailableCaptureColor;

            performRefresh = true;
        }

        private void Picker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (!performRefresh)
            {
                return;
            }
            Config.BoardColor = boardPicker.SelectedColor ?? default;
            Config.CheckedKingColor = checkKingPicker.SelectedColor ?? default;
            Config.SelectedPieceColor = selectedPiecePicker.SelectedColor ?? default;
            Config.CheckMateHighlightColor = checkmatePicker.SelectedColor ?? default;
            Config.LastMoveSourceColor = lastMoveSourcePicker.SelectedColor ?? default;
            Config.LastMoveDestinationColor = lastMoveDestinationPicker.SelectedColor ?? default;
            Config.BestMoveSourceColor = bestMoveSourcePicker.SelectedColor ?? default;
            Config.BestMoveDestinationColor = bestMoveDestinationPicker.SelectedColor ?? default;
            Config.AvailableMoveColor = availableMovePicker.SelectedColor ?? default;
            Config.AvailableCaptureColor = availableCapturePicker.SelectedColor ?? default;
        }
    }
}
