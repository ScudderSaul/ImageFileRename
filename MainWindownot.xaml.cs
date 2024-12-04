using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using Binding = System.Windows.Data.Binding;
using MessageBox = System.Windows.MessageBox;

namespace ImageFileRename
{
    public partial class MainWindow : Window
    {
        private readonly Random _rnd = new Random();
        private const string PngExtension = "*.png";
        private const string JpgExtension = "*.jpg";
        private string _prefixName = "GenericPrefix";
        private string _formatString = "Prefix_Count";
        private List<string> _filesInFolder = new List<string>();
        private System.Collections.ObjectModel.ObservableCollection<string> _fileData = null;

        public MainWindow()
        {
            InitializeComponent();
            ShowFormat();
        }

        public string PrefixName
        {
            get => _prefixName;
            set
            {
                _prefixName = value;
                PrefixTextBox.Text = _prefixName;
            }
        }

        private async Task<bool> GetFilePathsInFolder(string extension)
        {
            _filesInFolder = new List<string>();
            string path = SourcePathTextBlock.Text;

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please select a folder path");
                return false;
            }

            try
            {
                string[] files = await Task.Run(() => Directory.GetFiles(path, extension));
                _filesInFolder.AddRange(files);
                _fileData = new System.Collections.ObjectModel.ObservableCollection<string>(_filesInFolder);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving files: {ex.Message}");
                return false;
            }
        }

        private void ShowFileList()
        {
            Binding fileBinding = new Binding { Source = _fileData };
            FileListView.SetBinding(ItemsControl.ItemsSourceProperty, fileBinding);
        }

        private async void RenameButton_OnClick(object sender, RoutedEventArgs e)
        {
            int cnt = _rnd.Next(10000, 99999);
            string changedFilename = string.Empty;

            if (!string.IsNullOrEmpty(PrefixTextBox.Text))
            {
                PrefixName = PrefixTextBox.Text;
            }

            _filesInFolder.Clear();

            if (PngCheck.IsChecked == true)
            {
                if (!await GetFilePathsInFolder(PngExtension)) return;
                await RenameFiles(cnt, ".png");
            }

            if (JpgCheck.IsChecked == true)
            {
                if (!await GetFilePathsInFolder(JpgExtension)) return;
                await RenameFiles(cnt, ".jpg");
            }
        }

        private async Task RenameFiles(int cnt, string extension)
        {
            foreach (var file in _filesInFolder)
            {
                string newFileName = GenerateNewFileName(file, ref cnt);
                string newFilePath = Path.Combine(Path.GetDirectoryName(file), newFileName + extension);

                if (file != newFilePath)
                {
                    await Task.Run(() => RenameFile(file, newFilePath));
                }
            }
        }

        private string GenerateNewFileName(string file, ref int cnt)
        {
            if (DateTimeBox.IsChecked == true)
            {
                DateTime dt = DateTime.Now;
                string nd = (dt.Ticks % 10000L).ToString();
                cnt++;
                return $"{PrefixName}_{cnt}_{nd}";
            }
            else if (CrcBox.IsChecked == true)
            {
                string crc = Crc(file);
                return $"{PrefixName}_{crc}";
            }
            else
            {
                cnt++;
                return $"{PrefixName}_{cnt}";
            }
        }

        private void RenameFile(string oldName, string newName)
        {
            try
            {
                if (File.Exists(newName)) return;
                File.Move(oldName, newName);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error renaming file: {e.Message}");
            }
        }

        private void ChooseSource_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourcePathTextBlock.Text = dialog.SelectedPath;
                ShowFormat();
            }
        }

        private void ShowFormat()
        {
            if (CrcBox.IsChecked == true)
            {
                _formatString = "Prefix_crc";
            }
            else if (DateTimeBox.IsChecked == true)
            {
                _formatString = "Prefix_ticks_cnt";
            }
            else
            {
                _formatString = "Prefix_cnt";
            }
            FormatTextBox.Text = _formatString;
        }

        private string Crc(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            try
            {
                using (FileStream fs = File.OpenRead(path))
                {
                    byte[] b = new byte[fs.Length];
                    fs.Read(b, 0, b.Length);
                    return NullFX.CRC.Crc32.ComputeChecksum(b).ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating CRC: {ex.Message}");
                return string.Empty;
            }
        }

        private void CrcBox_OnChecked(object sender, RoutedEventArgs e)
        {
            DateTimeBox.IsChecked = false;
            ShowFormat();
        }

        private void DateTimeBox_OnChecked(object sender, RoutedEventArgs e)
        {
            CrcBox.IsChecked = false;
            ShowFormat();
        }

        private async void ShowListButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (PngCheck.IsChecked == true)
            {
                JpgShowList.Visibility = Visibility.Collapsed;
                PngShowList.Visibility = Visibility.Visible;
                if (await GetFilePathsInFolder(PngExtension)) ShowFileList();
            }
            if (JpgCheck.IsChecked == true)
            {
                PngShowList.Visibility = Visibility.Collapsed;
                JpgShowList.Visibility = Visibility.Visible;
                if (await GetFilePathsInFolder(JpgExtension)) ShowFileList();
            }
        }

        private async void PngCheck_OnClick(object sender, RoutedEventArgs e)
        {
            JpgCheck.IsChecked = false;
            _filesInFolder.Clear();
            if (await GetFilePathsInFolder(PngExtension)) ShowFileList();
            RenameButton.Content = "Rename All .png Files";
        }

        private async void JpgCheck_OnClick(object sender, RoutedEventArgs e)
        {
            PngCheck.IsChecked = false;
            _filesInFolder.Clear();
            if (await GetFilePathsInFolder(JpgExtension)) ShowFileList();
            RenameButton.Content = "Rename All .jpg Files";
        }
    }
}
