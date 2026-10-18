using Avalonia.Controls;

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

            boardPicker.Color = Config.BoardColor;
            checkKingPicker.Color = Config.CheckedKingColor;
            selectedPiecePicker.Color = Config.SelectedPieceColor;
            checkmatePicker.Color = Config.CheckMateHighlightColor;
            lastMoveSourcePicker.Color = Config.LastMoveSourceColor;
            lastMoveDestinationPicker.Color = Config.LastMoveDestinationColor;
            bestMoveSourcePicker.Color = Config.BestMoveSourceColor;
            bestMoveDestinationPicker.Color = Config.BestMoveDestinationColor;
            availableMovePicker.Color = Config.AvailableMoveColor;
            availableCapturePicker.Color = Config.AvailableCaptureColor;

            performRefresh = true;
        }

        private void Picker_ColorChanged(object sender, ColorChangedEventArgs e)
        {
            if (!performRefresh)
            {
                return;
            }
            Config.BoardColor = boardPicker.Color;
            Config.CheckedKingColor = checkKingPicker.Color;
            Config.SelectedPieceColor = selectedPiecePicker.Color;
            Config.CheckMateHighlightColor = checkmatePicker.Color;
            Config.LastMoveSourceColor = lastMoveSourcePicker.Color;
            Config.LastMoveDestinationColor = lastMoveDestinationPicker.Color;
            Config.BestMoveSourceColor = bestMoveSourcePicker.Color;
            Config.BestMoveDestinationColor = bestMoveDestinationPicker.Color;
            Config.AvailableMoveColor = availableMovePicker.Color;
            Config.AvailableCaptureColor = availableCapturePicker.Color;
        }
    }
}
