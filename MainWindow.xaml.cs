using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ImageFileRename
{
    public partial class MainWindow : Window
    {
        private enum NamingStrategy { Sequential, Timestamp, Crc32 }

        private const int PreviewDisplayLimit = 50; // adjust as desired

        private readonly ObservableCollection<FileRenameItem> _items = new(); // bound sample
        private readonly List<FileRenameItem> _allItems = new();              // full set for rename
        private bool _previewValid;
        private int _totalFilesEnumerated;

        public MainWindow()
        {
            InitializeComponent();
            PreviewList.ItemsSource = _items;
            UpdateSamplePattern();
            Status("Ready.");
        }

        #region UI Event Handlers

        private void FullPreviewCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            InvalidatePreview();
        }

        private void ChooseSource_OnClick(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog { Description = "Select the folder containing image files" };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourcePathTextBlock.Text = dlg.SelectedPath;
                InvalidatePreview();
            }
        }

        private void NamingStrategyRadio_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateSamplePattern();
            InvalidatePreview();
        }

        private void FileTypeCheckBox_OnClick(object sender, RoutedEventArgs e) => InvalidatePreview();

        private void PreviewButton_OnClick(object sender, RoutedEventArgs e) => _ = BuildPreviewAsync();

        private void RenameButton_OnClick(object sender, RoutedEventArgs e) => _ = ExecuteRenameAsync();

        #endregion

        #region Preview & Rename

        private async Task BuildPreviewAsync()
        {
            if (!ValidateConfiguration(out string error))
            {
                Status(error, isError: true);
                return;
            }

            try
            {
                DisableActions();
                Status("Scanning files...");
                _items.Clear();
                _allItems.Clear();
                _totalFilesEnumerated = 0;

                var path = SourcePathTextBlock.Text;
                var extensions = GetSelectedExtensions().ToList();
                var strategy = GetStrategy();
                var prefix = PrefixTextBox.Text.Trim();
                var proposedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                bool wantFull = FullPreviewCheckBox.IsChecked == true;
                int sequence = 1;
                int index = 1;

                // Move file enumeration and processing to a background thread
                await Task.Run(() =>
                {
                    foreach (var file in EnumerateFiles(path, extensions))
                    {
                        _totalFilesEnumerated++;

                        var fi = new FileInfo(file);

                        // Build the base name (CRC now computed immediately so uniqueness works on final name)
                        string proposedBase;
                        if (strategy == NamingStrategy.Crc32)
                        {
                            string crc = ComputeCrc32(fi.FullName);
                            proposedBase = $"{prefix}_{crc}";
                        }
                        else
                        {
                            proposedBase = GenerateBaseName(strategy, prefix, fi, sequence);
                            if (strategy == NamingStrategy.Sequential || strategy == NamingStrategy.Timestamp)
                                sequence++;
                        }

                        string proposedFullName = EnsureUnique(proposedBase, fi.Extension, proposedSet, fi.DirectoryName!);
                        proposedSet.Add(proposedFullName);

                        string action = string.Equals(fi.Name, proposedFullName, StringComparison.OrdinalIgnoreCase)
                            ? "Skip"
                            : "Rename";

                        var item = new FileRenameItem
                        {
                            Index = index++,
                            OriginalPath = fi.FullName,
                            OriginalName = fi.Name,
                            ProposedName = proposedFullName,
                            Extension = fi.Extension,
                            Action = action,
                            Status = "Pending",
                            DeferredCrcPath = null // no longer deferring CRC
                        };

                        _allItems.Add(item);

                        if (wantFull || _items.Count < PreviewDisplayLimit)
                        {
                            // UI updates must be dispatched to the UI thread
                            System.Windows.Application.Current.Dispatcher.Invoke(() => _items.Add(item));
                        }

                        if (!wantFull && _items.Count == PreviewDisplayLimit)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                Status($"Sampling {PreviewDisplayLimit} files... ({_totalFilesEnumerated} scanned)")
                            );
                        }
                    }
                });

                // If sampling, add a summary pseudo-row
                if (!wantFull && _totalFilesEnumerated > _items.Count)
                {
                    _items.Add(new FileRenameItem
                    {
                        Index = 0,
                        OriginalPath = "",
                        OriginalName = "",
                        ProposedName = $"... ({_totalFilesEnumerated - _items.Count} more not shown)",
                        Extension = "",
                        Action = "",
                        Status = "Summary"
                    });
                }

                if (_totalFilesEnumerated == 0)
                {
                    Status("No matching files found.");
                    _previewValid = false;
                    RenameButton.IsEnabled = false;
                    return;
                }

                _previewValid = true;
                RenameButton.IsEnabled = true;

                if (wantFull)
                    Status($"Preview generated for {_totalFilesEnumerated} files.");
                else
                    Status($"Preview sample {_items.Count} of {_totalFilesEnumerated} files. (Use Full Preview for all)");
            }
            catch (Exception ex)
            {
                Status($"Preview failed: {ex.Message}", isError: true);
                _previewValid = false;
                RenameButton.IsEnabled = false;
            }
            finally
            {
                EnableActions();
            }
        }

        private async Task ExecuteRenameAsync()
        {
            if (!_previewValid)
            {
                Status("Preview is invalid. Generate preview first.", isError: true);
                return;
            }

            try
            {
                DisableActions();
                Status("Renaming...");
                RenameProgressBar.Value = 0;

                int total = _allItems.Count(i => i.Action == "Rename");
                int processed = 0;

                foreach (var item in _allItems)
                {
                    if (item.Action != "Rename")
                    {
                        item.Status = "Skipped";
                        continue;
                    }

                    string targetPath = Path.Combine(Path.GetDirectoryName(item.OriginalPath)!, item.ProposedName);
                    try
                    {
                        if (!File.Exists(targetPath))
                        {
                            await Task.Run(() => File.Move(item.OriginalPath, targetPath));
                            item.Status = "Renamed";
                        }
                        else
                        {
                            item.Status = "Conflict";
                        }
                    }
                    catch (Exception ex)
                    {
                        item.Status = $"Error: {ex.Message}";
                    }

                    processed++;
                    if (total > 0)
                        RenameProgressBar.Value = (double)processed / total * 100.0;
                }

                // Reflect status changes for any displayed sample items
                RefreshSampleStatuses();

                Status("Rename complete.");
                _previewValid = false;
                RenameButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Status($"Rename failed: {ex.Message}", isError: true);
            }
            finally
            {
                EnableActions();
                RenameProgressBar.Value = 100;
            }
        }

        #endregion

        #region Naming Helpers

        private NamingStrategy GetStrategy()
        {
            if (TimestampRadio?.IsChecked == true) return NamingStrategy.Timestamp;
            if (CrcRadio?.IsChecked == true) return NamingStrategy.Crc32;
            return NamingStrategy.Sequential;
        }

        private string GenerateBaseName(NamingStrategy strategy, string prefix, FileInfo fi, int sequence) =>
            strategy switch
            {
                NamingStrategy.Sequential => $"{prefix}_{sequence:0000}",
                NamingStrategy.Timestamp => $"{prefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{sequence:0000}",
                NamingStrategy.Crc32 => $"{prefix}_PENDINGCRC", // not used now (CRC computed inline)
                _ => $"{prefix}_{sequence:0000}"
            };

        private string RebuildNameWithDeferredCrc(string prefix, string filePath, string extension)
        {
            string crc = ComputeCrc32(filePath);
            return $"{prefix}_{crc}{extension}";
        }

        private string EnsureUnique(string baseName, string extension, HashSet<string> existingFullNames, string directory)
        {
            string candidate = $"{baseName}{extension.ToLowerInvariant()}";
            int suffix = 1;
            while (existingFullNames.Contains(candidate) ||
                   File.Exists(Path.Combine(directory, candidate)))
            {
                candidate = $"{baseName}_{suffix++}{extension.ToLowerInvariant()}";
            }
            return candidate;
        }

        private string ComputeCrc32(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                uint crc = NullFX.CRC.Crc32.ComputeChecksum(bytes);
                return crc.ToString("X8");
            }
            catch
            {
                return "ERRORCRC";
            }
        }

        #endregion

        #region Utility

        private IEnumerable<string> GetSelectedExtensions()
        {
            if (PngCheck?.IsChecked == true) yield return "png";
            if (JpgCheck?.IsChecked == true) yield return "jpg";
        }

        private bool ValidateConfiguration(out string error)
        {
            if (string.IsNullOrWhiteSpace(SourcePathTextBlock.Text) || !Directory.Exists(SourcePathTextBlock.Text))
            {
                error = "Select a valid source folder.";
                return false;
            }
            if (!GetSelectedExtensions().Any())
            {
                error = "Select at least one file type.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(PrefixTextBox.Text))
            {
                error = "Enter a prefix.";
                return false;
            }
            error = "";
            return true;
        }

        private IEnumerable<string> EnumerateFiles(string root, IEnumerable<string> exts)
        {
            foreach (var ext in exts)
            {
                foreach (var f in Directory.EnumerateFiles(root, $"*.{ext}", SearchOption.TopDirectoryOnly))
                    yield return f;
            }
        }

        private void UpdateSamplePattern()
        {
            var strategy = GetStrategy();
            string prefix = string.IsNullOrWhiteSpace(PrefixTextBox.Text) ? "Prefix" : PrefixTextBox.Text.Trim();
            string sample = strategy switch
            {
                NamingStrategy.Sequential => $"{prefix}_0001",
                NamingStrategy.Timestamp => $"{prefix}_20250101_104512_123_0001",
                NamingStrategy.Crc32 => $"{prefix}_DEADBEEF",
                _ => $"{prefix}_0001"
            };
            SamplePatternTextBlock.Text = sample;
        }

        private void InvalidatePreview()
        {
            _previewValid = false;
            if (RenameButton != null)
                RenameButton.IsEnabled = false;
            Status("Preview invalidated. Generate a new preview.");
        }

        private void DisableActions()
        {
            PreviewButton.IsEnabled = false;
            RenameButton.IsEnabled = false;
            ChooseSourceButton.IsEnabled = false;
        }

        private void EnableActions()
        {
            PreviewButton.IsEnabled = true;
            ChooseSourceButton.IsEnabled = true;
        }

        private void Status(string message, bool isError = false)
        {
            if (StatusTextBlock == null) return;
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
        }

        private void RefreshSampleStatuses()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.Status == "Summary") continue;
            }
        }

        #endregion

        #region Data Model

        private class FileRenameItem
        {
            public int Index { get; set; }
            public string OriginalPath { get; set; } = "";
            public string OriginalName { get; set; } = "";
            public string ProposedName { get; set; } = "";
            public string Extension { get; set; } = "";
            public string Action { get; set; } = "";
            public string Status { get; set; } = "";
            public string? DeferredCrcPath { get; set; } // retained for compatibility; unused
        }

        #endregion
    }
}
