﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for PGNExport.xaml
    /// </summary>
    public partial class PGNExport : Window
    {
        private readonly ShogiGame game;
        private readonly bool whiteIsComputer;
        private readonly bool blackIsComputer;

        public PGNExport(ShogiGame game, bool whiteIsComputer, bool blackIsComputer)
        {
            this.game = game;
            this.whiteIsComputer = whiteIsComputer;
            this.blackIsComputer = blackIsComputer;
            InitializeComponent();

            if (whiteIsComputer)
            {
                whiteNameBox.Text = "Computer";
                whiteNameBox.IsReadOnly = true;
                whiteNameBox.IsEnabled = false;
            }
            if (blackIsComputer)
            {
                blackNameBox.Text = "Computer";
                blackNameBox.IsReadOnly = true;
                blackNameBox.IsEnabled = false;
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
            string whiteName = whiteNameBox.Text.Trim();
            string blackName = blackNameBox.Text.Trim();
            File.WriteAllText(saveDialog.FileName, game.ToPGN(eventName != "" ? eventName : null,
                locationName != "" ? locationName : null, date, whiteName != "" ? whiteName : "Player",
                blackName != "" ? blackName : "Player", whiteIsComputer, blackIsComputer));
            Close();
        }
    }
}
