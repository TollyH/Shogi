using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for KIFExport.xaml
    /// </summary>
    public partial class KIFExport : Window
    {
        private readonly ShogiGame game;
        private readonly bool senteIsComputer;
        private readonly bool goteIsComputer;

        public KIFExport(ShogiGame game, bool senteIsComputer, bool goteIsComputer)
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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                DefaultExtension = ".kifu",
                FileTypeChoices = new List<FilePickerFileType>()
                {
                    new("Kifu File") { Patterns = new List<string>() { "*.kifu"} }
                }
            });
            if (file is null)
            {
                return;
            }
            string eventName = eventNameBox.Text?.Trim() ?? "";
            string locationName = locationNameBox.Text?.Trim() ?? "";
            DateOnly? date = dateBox.SelectedDate is null ? null : DateOnly.FromDateTime(dateBox.SelectedDate.Value.DateTime);
            string senteName = senteNameBox.Text?.Trim() ?? "";
            string goteName = goteNameBox.Text?.Trim() ?? "";
            File.WriteAllText(file.Path.LocalPath, game.ToKIF(eventName != "" ? eventName : null,
                locationName != "" ? locationName : null, date, senteName != "" ? senteName : "Player",
                goteName != "" ? goteName : "Player", senteIsComputer, goteIsComputer));
            Close();
        }
    }
}
