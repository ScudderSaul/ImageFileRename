using System;
using System.Collections;
using System.Collections.Generic;
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
        public MainWindow()
        {
            InitializeComponent();
            ShowFormat();
            PngShowList.ItemsSource = PngFilesInFolder;
        }
        private string _prefixName = "GenericPrefix";
        public string PrefixName
        {
            get
            {
                return (_prefixName);
            }
            set
            {
                _prefixName = value;
                PrefixTextBox.Text = _prefixName;
            }
        }
        List<string> PngFilesInFolder = new List<string>();
        private PngObservable PngData = new PngObservable(new List<string>());
        public async Task<bool> GetPngFilePathsInFolderAsync()
        {
            PngFilesInFolder.Clear();
            PngData.Clear();

            string path = SourcePathTextBlock.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please select a folder path");
                return false;
            }
            string[] PngFiles = await Task.Run(() => System.IO.Directory.GetFiles(path, "*.png"));
            foreach (string PngFile in PngFiles)
            {
                PngFilesInFolder.Add(PngFile);
                PngData.Add(PngFile);
            }

            return true;
        }

        List<string> JpgFilesInFolder = new List<string>();
        private JpgObservable JpgData = new JpgObservable(new List<string>());

        public async Task<bool> GetJpgFilePathsInFolderAsync()
        {
            JpgFilesInFolder.Clear();
            JpgData.Clear();

            string path = SourcePathTextBlock.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please select a folder path");
                return false;
            }
            string[] JpgFiles = await Task.Run(() => System.IO.Directory.GetFiles(path, "*.jpg"));
            foreach (string JpgFile in JpgFiles)
            {
                JpgFilesInFolder.Add(JpgFile);
                JpgData.Add(JpgFile);
            }

            return true;
        }

        void ShowPngList()
        {
            Binding PngBinding = new Binding();
            PngBinding.Source = PngData;
            PngShowList.SetBinding(ItemsControl.ItemsSourceProperty, PngBinding);
        }

        void ShowJpgList()
        {
            Binding JpgBinding = new Binding();
            JpgBinding.Source = JpgData;
            JpgShowList.SetBinding(ItemsControl.ItemsSourceProperty, JpgBinding);
        }

        // rename a file
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
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        private async void RenameButton_OnClick(object sender, RoutedEventArgs e)
        {
            int cnt = _rnd.Next(10000, 99999);
            string changedfilename = string.Empty;

            if (PrefixTextBox.Text != "")
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
                if (await GetPngFilePathsInFolderAsync() == false)
                {
                    StatusTextBlock.Text = "Failed to get PNG files.";
                    return;
                }
                totalFiles += PngFilesInFolder.Count;
            }

            if (JpgCheck.IsChecked == true)
            {
                if (await GetJpgFilePathsInFolderAsync() == false)
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
                    if (DateTimeBox.IsChecked == true)
                    {
                        DateTime dt = DateTime.Now;
                        string nd = (dt.Ticks % 10000L).ToString();
                        cnt++;
                        changedfilename = PrefixName + "_" + cnt.ToString() + "_" + nd;
                    }
                    else
                    {
                        if (CrcBox.IsChecked == true)
                        {
                            string crc = Crc(fff);
                            changedfilename = PrefixName + "_" + crc;
                        }
                        else
                        {
                            changedfilename = PrefixName + "_" + cnt.ToString();
                            cnt++;
                        }
                    }

                    FileInfo inf = new FileInfo(fff);
                    string npf = inf.DirectoryName + "\\" + changedfilename + ".png";
                    if (fff != npf)
                    {
                        await RenameFileAsync(fff, npf);
                    }

                    processedFiles++;
                    RenameProgressBar.Value = (double)processedFiles / totalFiles * 100;
                }
            }

            if (JpgCheck.IsChecked == true)
            {
                foreach (var fff in JpgFilesInFolder)
                {
                    if (DateTimeBox.IsChecked == true)
                    {
                        DateTime dt = DateTime.Now;
                        string nd = (dt.Ticks % 10000L).ToString();
                        cnt++;
                        changedfilename = PrefixName + "_" + cnt.ToString() + "_" + nd;
                    }
                    else
                    {
                        if (CrcBox.IsChecked == true)
                        {
                            string crc = Crc(fff);
                            changedfilename = PrefixName + "_" + crc;
                        }
                        else
                        {
                            changedfilename = PrefixName + "_" + cnt.ToString();
                            cnt++;
                        }
                    }

                    FileInfo inf = new FileInfo(fff);
                    string npf = inf.DirectoryName + "\\" + changedfilename + ".jpg";
                    if (fff != npf)
                    {
                        await RenameFileAsync(fff, npf);
                    }

                    processedFiles++;
                    RenameProgressBar.Value = (double)processedFiles / totalFiles * 100;
                }
            }

            StatusTextBlock.Text = "Renaming completed.";
            RenameProgressBar.Value = 0; // Reset the progress bar
        }

        // chose a source folder path
        private void ChooseSource_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            SourcePathTextBlock.Text = dialog.SelectedPath;
            ShowFormat();
            GetPngFilePathsInFolderAsync();
        }

        private string _formatString = "Prefix_Count";

        private void ShowFormat()
        {
            if (CrcBox.IsChecked == true)
            {
                _formatString = "Prefix_crc";
            }
            else
            {
                if (DateTimeBox.IsChecked == true)
                {
                    _formatString = "Prefix_ticks_cnt";
                }
                else
                {
                    _formatString = "Prefix_cnt";
                }
            }
            FormatTextBox.Text = _formatString;
        }

        // get file crc
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
                await GetPngFilePathsInFolderAsync();
                ShowPngList();
            }
            if (JpgCheck.IsChecked == true)
            {
                PngShowList.Visibility = Visibility.Collapsed;
                JpgShowList.Visibility = Visibility.Visible;
                await GetJpgFilePathsInFolderAsync();
                ShowJpgList();
            }
        }

        private async void PngCheck_OnClick(object sender, RoutedEventArgs e)
        {
            JpgCheck.IsChecked = false;
            PngFilesInFolder.Clear();
            await GetPngFilePathsInFolderAsync();
            ShowPngList();
            RenameButton.Content = "Rename All .png Files";
        }

        private async void JpgCheck_OnClick(object sender, RoutedEventArgs e)
        {
            PngCheck.IsChecked = false;
            JpgFilesInFolder.Clear();
            await GetJpgFilePathsInFolderAsync();
            ShowJpgList();
            RenameButton.Content = "Rename All .jpg Files";
        }
    }
}
