using Avalonia.Media;

namespace Shogi
{
    public class Settings
    {
        public bool FlipBoard { get; set; } = false;
        public bool UpdateEvalAfterBot { get; set; } = true;
        public string PieceSet { get; set; } = "1kanji";
        public string Notation { get; set; } = "japanese";

        public Color BoardColor { get; set; } = Color.FromRgb(249, 184, 83);
        public Color CheckedKingColor { get; set; } = Brushes.Red.Color;
        public Color SelectedPieceColor { get; set; } = Brushes.Blue.Color;
        public Color CheckMateHighlightColor { get; set; } = Brushes.IndianRed.Color;
        public Color LastMoveSourceColor { get; set; } = Brushes.CadetBlue.Color;
        public Color LastMoveDestinationColor { get; set; } = Brushes.Cyan.Color;
        public Color BestMoveSourceColor { get; set; } = Brushes.LightGreen.Color;
        public Color BestMoveDestinationColor { get; set; } = Brushes.Green.Color;
        public Color AvailableMoveColor { get; set; } = Brushes.Yellow.Color;
        public Color AvailableCaptureColor { get; set; } = Brushes.Red.Color;
    }
}
