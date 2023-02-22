using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for PGNExport.xaml
    /// </summary>
    public partial class PGNExport : Window
    {
        private readonly ShogiGame game;
        private readonly bool senteIsComputer;
        private readonly bool goteIsComputer;

        public PGNExport(ShogiGame game, bool senteIsComputer, bool goteIsComputer)
        {
            this.game = game;
            this.senteIsComputer = senteIsComputer;
            this.goteIsComputer = goteIsComputer;
            InitializeComponent();

            if (senteIsComputer)
            {
                senteNameBox.Text = "Computer";
                senteNameBox.IsReadOnly = true;
                senteNameBox.IsEnabled = false;
            }
            if (goteIsComputer)
            {
                goteNameBox.Text = "Computer";
                goteNameBox.IsReadOnly = true;
                goteNameBox.IsEnabled = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new()
            {
                AddExtension = true,
                DefaultExt = ".pgn",
                Filter = "Portable Game Notation|*.pgn",
                CheckPathExists = true
            };
            if (!saveDialog.ShowDialog() ?? true)
            {
                return;
            }
            string eventName = eventNameBox.Text.Trim();
            string locationName = locationNameBox.Text.Trim();
            DateOnly? date = dateBox.SelectedDate is null ? null : DateOnly.FromDateTime(dateBox.SelectedDate.Value);
            string senteName = senteNameBox.Text.Trim();
            string goteName = goteNameBox.Text.Trim();
            File.WriteAllText(saveDialog.FileName, game.ToPGN(eventName != "" ? eventName : null,
                locationName != "" ? locationName : null, date, senteName != "" ? senteName : "Player",
                goteName != "" ? goteName : "Player", senteIsComputer, goteIsComputer));
            Close();
        }
    }
}
