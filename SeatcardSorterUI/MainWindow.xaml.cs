/**
 * Seattle Opera Seatcard Sorter
 * Copyright 2019 William Rall
 */
namespace SeatcardSorterUI
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using SeatcardSorter;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void Map_Click(object sender, RoutedEventArgs e)
        {
            this.StatusText.Text = "Starting...";

            try
            {
                string sourceFile = this.MapSourceFileBox.Text;
                string targetFile = this.MapTargetFileBox.Text;
                string listVersionNameFile = this.MapVersionNameFileBox.Text;
                string versionMappingFile = this.MapVersionMappingFileBox.Text;

                // Parse list version-name file:
                List<string> listVersions = CsvParsingHelper.DefaultListVersions;
                if (!string.IsNullOrEmpty(listVersionNameFile))
                {
                    this.StatusText.Text = "Parsing Version names";
                    listVersions = await CsvParsingHelper.GetListVersions(listVersionNameFile);
                }

                // Parse version mapping file:
                Dictionary<string, Dictionary<DateTime, string>> versionMapping = null;
                List<string> versionColumnsPresent = null;
                if (!string.IsNullOrEmpty(versionMappingFile))
                {
                    this.StatusText.Text = "Parsing Version mappings";
                    (versionMapping, versionColumnsPresent) = await CsvParsingHelper.GetVersionMappings(versionMappingFile, listVersions);
                }

                // Parse source rows from the CSV
                this.StatusText.Text = "Parsing Source rows";
                List<SourceRow> sourceRows = await CsvParsingHelper.GetSourceRows(sourceFile);

                this.StatusText.Text = "Mapping and sorting rows";
                (List<ResultRow> rows, List<string> headers) = CsvParsingHelper.MapSourceRowsToTargetRows(listVersions, versionMapping, versionColumnsPresent, sourceRows);

                this.StatusText.Text = "Outputting target rows";
                await CsvParsingHelper.WriteResultRows(targetFile, rows, headers);

                this.StatusText.Text = $"Completed writing to {targetFile}";
            }
            catch (SeatcardSorterException ex)
            {
                this.StatusText.Text = $"Failed to write file: {ex.Message}";
            }
        }

        private async void Resort_Click(object sender, RoutedEventArgs e)
        {
            this.StatusText.Text = "Starting...";

            try
            {
                string sourceFile = this.ResortSourceFileBox.Text;
                string targetFile = this.ResortTargetFileBox.Text;

                // Parse result rows from the CSV
                this.StatusText.Text = "Parsing Source rows";
                (List<ResultRow> rows, List<string> headers) = await CsvParsingHelper.GetResultRows(sourceFile);

                this.StatusText.Text = "Re-sorting rows";
                CsvParsingHelper.Resort(rows);

                this.StatusText.Text = "Outputting target rows";
                await CsvParsingHelper.WriteResultRows(targetFile, rows, headers);

                this.StatusText.Text = $"Completed writing to {targetFile}";
            }
            catch (SeatcardSorterException ex)
            {
                this.StatusText.Text = $"Failed to write file: {ex.Message}";
            }
        }

        private async void Dupe_Click(object sender, RoutedEventArgs e)
        {
            this.StatusText.Text = "Starting...";

            try
            {
                string sourceFile = this.DupeSourceFileBox.Text;
                string targetFile = this.DupeTargetFileBox.Text;

                // Parse result rows from the CSV
                this.StatusText.Text = "Parsing Source rows";
                (List<ResultRow> rows, List<string> headers) = await CsvParsingHelper.GetResultRows(sourceFile);

                this.StatusText.Text = "Finding duplicate rows";
                List<ResultRow> duplicates = CsvParsingHelper.FindDuplicates(rows);

                this.StatusText.Text = "Outputting target rows";
                await CsvParsingHelper.WriteResultRows(targetFile, duplicates, headers);

                this.StatusText.Text = $"Completed writing to {targetFile}";
            }
            catch (SeatcardSorterException ex)
            {
                this.StatusText.Text = $"Failed to write file: {ex.Message}";
            }
        }

        private void MapSourceFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.MapSourceFileBox);
        }

        private void MapSourceFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.MapSourceFileBox);
        }

        private void MapTargetFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.MapTargetFileBox, expectAbsent: true);
        }

        private void MapTargetFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.MapTargetFileBox, save: true);
        }

        private void MapVersionNameFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.MapVersionNameFileBox);
        }

        private void MapVersionNameFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.MapVersionNameFileBox, expectCSV: false);
        }

        private void MapVersionMappingFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.MapVersionMappingFileBox);
        }

        private void MapVersionMappingFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.MapVersionMappingFileBox);
        }

        private void ResortSourceFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.ResortSourceFileBox);
        }

        private void ResortSourceFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.ResortSourceFileBox);
        }

        private void ResortTargetFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.ResortTargetFileBox, expectAbsent: true);
        }

        private void ResortTargetFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.ResortTargetFileBox, save: true);
        }
        private void DupeSourceFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.DupeSourceFileBox);
        }

        private void DupeSourceFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.DupeSourceFileBox);
        }

        private void DupeTargetFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.VerifyTextBoxFile(this.DupeTargetFileBox, expectAbsent: true);
        }

        private void DupeTargetFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFile(this.DupeTargetFileBox, save: true);
        }

        private void VerifyTextBoxFile(TextBox fileTextBox, bool expectAbsent = false)
        {
            string text = fileTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                fileTextBox.BorderBrush = Brushes.Black;
            }

            if (File.Exists(text) == expectAbsent)
            {
                fileTextBox.BorderBrush = Brushes.Red;
            }
            else
            {
                fileTextBox.BorderBrush = Brushes.Black;
            }
        }

        private void BrowseFile(TextBox fileTextBox, bool save = false, bool expectCSV = true)
        {
            FileDialog fileDialog;
            if (save)
            {
                fileDialog = new SaveFileDialog();
            }
            else
            {
                fileDialog = new OpenFileDialog();
            }

            if (expectCSV)
            {
                fileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            }

            if (fileDialog.ShowDialog() == true)
            {
                fileTextBox.Text = fileDialog.FileName;
            }
        }
    }
}
