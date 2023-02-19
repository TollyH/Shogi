using Newtonsoft.Json;
using System;
using System.Windows.Media;

namespace Shogi
{
    [Serializable]
    public class Settings
    {
        public bool FlipBoard { get; set; }
        public bool UpdateEvalAfterBot { get; set; }
        public string PieceSet { get; set; }

        public Color BoardColor { get; set; }
        public Color CheckedKingColor { get; set; }
        public Color SelectedPieceColor { get; set; }
        public Color CheckMateHighlightColor { get; set; }
        public Color LastMoveSourceColor { get; set; }
        public Color LastMoveDestinationColor { get; set; }
        public Color BestMoveSourceColor { get; set; }
        public Color BestMoveDestinationColor { get; set; }
        public Color AvailableMoveColor { get; set; }
        public Color AvailableCaptureColor { get; set; }

        public Settings()
        {
            FlipBoard = false;
            UpdateEvalAfterBot = true;
            PieceSet = "1kanji";

            BoardColor = Color.FromRgb(249, 184, 83);
            CheckedKingColor = Brushes.Red.Color;
            SelectedPieceColor = Brushes.Blue.Color;
            CheckMateHighlightColor = Brushes.IndianRed.Color;
            LastMoveSourceColor = Brushes.CadetBlue.Color;
            LastMoveDestinationColor = Brushes.Cyan.Color;
            BestMoveSourceColor = Brushes.LightGreen.Color;
            BestMoveDestinationColor = Brushes.Green.Color;
            AvailableMoveColor = Brushes.Yellow.Color;
            AvailableCaptureColor = Brushes.Red.Color;
        }

        [JsonConstructor]
        public Settings(bool flipBoard, bool updateEvalAfterBot, string pieceSet,
            Color boardColor, Color checkedKingColor, Color selectedPieceColor, Color checkMateHighlightColor,
            Color lastMoveSourceColor, Color lastMoveDestinationColor, Color bestMoveSourceColor,
            Color bestMoveDestinationColor, Color availableMoveColor, Color availableCaptureColor)
        {
            FlipBoard = flipBoard;
            UpdateEvalAfterBot = updateEvalAfterBot;
            PieceSet = pieceSet;
            BoardColor = boardColor;
            CheckedKingColor = checkedKingColor;
            SelectedPieceColor = selectedPieceColor;
            CheckMateHighlightColor = checkMateHighlightColor;
            LastMoveSourceColor = lastMoveSourceColor;
            LastMoveDestinationColor = lastMoveDestinationColor;
            BestMoveSourceColor = bestMoveSourceColor;
            BestMoveDestinationColor = bestMoveDestinationColor;
            AvailableMoveColor = availableMoveColor;
            AvailableCaptureColor = availableCaptureColor;
        }
    }
}
