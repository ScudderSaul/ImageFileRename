using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Binding = System.Windows.Data.Binding;
using MessageBox = System.Windows.MessageBox;
using Path = System.Windows.Shapes.Path;


namespace ImageFileRename
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Random _rnd = new Random();
        private string _prefixName = "GenericPrefix";
        private ObservableCollection<string> PngFilesInFolder = new();

        private ObservableCollection<string> JpgFilesInFolder = new();

        public MainWindow()
        {
            InitializeComponent();
            ShowFormat();
            PngShowList.ItemsSource = PngFilesInFolder;
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

        private async Task<bool> GetFilePathsInFolderAsync(string extension, ObservableCollection<string> fileCollection)
        {
            fileCollection.Clear();
            string path = SourcePathTextBlock.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please select a folder path");
                return false;
            }

            try
            {
                string[] files = await Task.Run(() => Directory.GetFiles(path, $"*.{extension}"));
                foreach (string file in files)
                {
                    fileCollection.Add(file);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving files: {ex.Message}");
                return false;
            }
        }

        private void ShowFileList(ObservableCollection<string> fileCollection, System.Windows.Controls.ListView listView)
        {
            Binding binding = new Binding { Source = fileCollection };
            listView.SetBinding(ItemsControl.ItemsSourceProperty, binding);
        }

        public async Task RenameFileAsync(string oldName, string newName)
        {
            try
            {
                if (System.IO.File.Exists(newName))
                {
                    return;
                }
                await Task.Run(() => System.IO.File.Move(oldName, newName));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private async void RenameButton_OnClick(object sender, RoutedEventArgs e)
        {
            int cnt = _rnd.Next(10000, 99999);
            string changedfilename = string.Empty;

            if (!string.IsNullOrEmpty(PrefixTextBox.Text))
            {
                PrefixName = PrefixTextBox.Text;
            }

            PngFilesInFolder.Clear();
            JpgFilesInFolder.Clear();
            StatusTextBlock.Text = "Renaming files...";
            RenameProgressBar.Value = 0;

            int totalFiles = 0;
            if (PngCheck.IsChecked == true)
            {
                if (!await GetFilePathsInFolderAsync("png", PngFilesInFolder))
                {
                    StatusTextBlock.Text = "Failed to get PNG files.";
                    return;
                }
                totalFiles += PngFilesInFolder.Count;
            }

            if (JpgCheck.IsChecked == true)
            {
                if (!await GetFilePathsInFolderAsync("jpg", JpgFilesInFolder))
                {
                    StatusTextBlock.Text = "Failed to get JPG files.";
                    return;
                }
                totalFiles += JpgFilesInFolder.Count;
            }

            int processedFiles = 0;

            if (PngCheck.IsChecked == true)
            {
                foreach (var fff in PngFilesInFolder)
                {
                    changedfilename = GenerateNewFileName(fff, ref cnt);
                    FileInfo inf = new FileInfo(fff);
                   

                    // With these lines:
                    string npf = System.IO.Path.Combine(inf.DirectoryName, $"{changedfilename}.png");
                  
                    if (fff != npf)
                    {
                        await RenameFileAsync(fff, npf);
                    }

                    processedFiles++;
                    UpdateProgressBar(processedFiles, totalFiles);
                }
            }

            if (JpgCheck.IsChecked == true)
            {
                foreach (var fff in JpgFilesInFolder)
                {
                    changedfilename = GenerateNewFileName(fff, ref cnt);
                    FileInfo inf = new FileInfo(fff);
                    string npf = System.IO.Path.Combine(inf.DirectoryName, $"{changedfilename}.jpg");
                    if (fff != npf)
                    {
                        await RenameFileAsync(fff, npf);
                    }

                    processedFiles++;
                    UpdateProgressBar(processedFiles, totalFiles);
                }
            }

            StatusTextBlock.Text = "Renaming completed.";
            RenameProgressBar.Value = 0; // Reset the progress bar
        }

        private string GenerateNewFileName(string filePath, ref int cnt)
        {
            string changedfilename;
            if (DateTimeBox.IsChecked == true)
            {
                DateTime dt = DateTime.Now;
                string nd = (dt.Ticks % 10000L).ToString();
                cnt++;
                changedfilename = $"{PrefixName}_{cnt}_{nd}";
            }
            else if (CrcBox.IsChecked == true)
            {
                string crc = Crc(filePath);
                changedfilename = $"{PrefixName}_{crc}";
            }
            else
            {
                changedfilename = $"{PrefixName}_{cnt}";
                cnt++;
            }

            return changedfilename;
        }

        private void UpdateProgressBar(int processedFiles, int totalFiles)
        {
            Dispatcher.Invoke(() =>
            {
                RenameProgressBar.Value = (double)processedFiles / totalFiles * 100;
            });
        }

        private void ChooseSource_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            SourcePathTextBlock.Text = dialog.SelectedPath;
            ShowFormat();
            GetFilePathsInFolderAsync("png", PngFilesInFolder);
        }

        private string _formatString = "Prefix_Count";

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
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            UInt32 textCrc = 0;

            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                byte[] b = new byte[fs.Length];
                UTF8Encoding temp = new UTF8Encoding(true);
                int readLen = 0;
                if ((readLen = fs.Read(b, 0, b.Length)) > 0)
                {
                    textCrc = NullFX.CRC.Crc32.ComputeChecksum(b);
                }
            }

            return textCrc.ToString();
        }

        private void CrcBox_OnChecked(object sender, RoutedEventArgs e)
        {
            DateTimeBox.IsChecked = false;
            ShowFormat();
        }

        private void CrcBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            ShowFormat();
        }

        private void DateTimeBox_OnChecked(object sender, RoutedEventArgs e)
        {
            CrcBox.IsChecked = false;
            ShowFormat();
        }

        private void DateTimeBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            ShowFormat();
        }

        private async void ShowListButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (PngCheck.IsChecked == true)
            {
                JpgShowList.Visibility = Visibility.Collapsed;
                PngShowList.Visibility = Visibility.Visible;
                await GetFilePathsInFolderAsync("png", PngFilesInFolder);
                ShowFileList(PngFilesInFolder, PngShowList);
            }
            if (JpgCheck.IsChecked == true)
            {
                PngShowList.Visibility = Visibility.Collapsed;
                JpgShowList.Visibility = Visibility.Visible;
                await GetFilePathsInFolderAsync("jpg", JpgFilesInFolder);
                ShowFileList(JpgFilesInFolder, JpgShowList);
            }
        }

        private async void PngCheck_OnClick(object sender, RoutedEventArgs e)
        {
            JpgCheck.IsChecked = false;
            PngFilesInFolder.Clear();
            await GetFilePathsInFolderAsync("png", PngFilesInFolder);
            ShowFileList(PngFilesInFolder, PngShowList);
            RenameButton.Content = "Rename All .png Files";
        }

        private async void JpgCheck_OnClick(object sender, RoutedEventArgs e)
        {
            PngCheck.IsChecked = false;
            JpgFilesInFolder.Clear();
            await GetFilePathsInFolderAsync("jpg", JpgFilesInFolder);
            ShowFileList(JpgFilesInFolder, JpgShowList);
            RenameButton.Content = "Rename All .jpg Files";
        }
    }
}
