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



//The MainWindow class in the provided code is a part of a WPF application designed to rename image files (specifically PNG and JPG files) in a selected folder. Here's a breakdown of how the code works:
//Class and Constructor
//•	Class Definition: public partial class MainWindow : Window
//•	This class inherits from Window, indicating it is a WPF window.
//•	Constructor: public MainWindow()
//•	Initializes the window, sets up the initial display format, and binds the PNG file list to a UI element.
//Fields and Properties
//•	Fields:
//•	_rnd: A Random instance used to generate random numbers.
//•	_prefixName: A string to store the prefix for renamed files.
//•	PngFilesInFolder and JpgFilesInFolder: Lists to store file paths of PNG and JPG files in the selected folder.
//•	PngData and JpgData: Observable collections to bind the file lists to UI elements.
//•	_formatString: A string to store the current format for renaming files.
//•	Properties:
//•	PrefixName: A property to get or set the _prefixName field and update the PrefixTextBox UI element.
//Methods
//•	GetPngFilePathsInFolder() and GetJpgFilePathsInFolder():
//•	These methods retrieve the file paths of PNG and JPG files in the selected folder, respectively, and store them in their corresponding lists and observable collections.
//•	ShowPngList() and ShowJpgList():
//•	These methods bind the observable collections (PngData and JpgData) to the UI elements (PngShowList and JpgShowList) to display the file lists.
//•	RenameFile(string oldName, string newName):
//•	This method renames a file from oldName to newName, handling any exceptions that may occur.
//•	RenameButton_OnClick(object sender, RoutedEventArgs e):
//•	This method handles the renaming process when the rename button is clicked. It generates new file names based on the selected options (prefix, CRC, or date-time) and renames the files accordingly.
//•	ChooseSource_OnClick(object sender, RoutedEventArgs e):
//•	This method opens a folder browser dialog to select the source folder and updates the file lists.
//•	ShowFormat():
//•	This method updates the _formatString based on the selected options (CRC or date-time) and displays it in the FormatTextBox UI element.
//•	Crc(string path):
//•	This method calculates the CRC checksum of a file and returns it as a string.
//•	Event Handlers:
//•	CrcBox_OnChecked, CrcBox_OnUnchecked, DateTimeBox_OnChecked, DateTimeBox_OnUnchecked: These methods handle the checking and unchecking of the CRC and date-time options, updating the format string accordingly.
//•	ShowListButton_OnClick: This method displays the appropriate file list (PNG or JPG) based on the selected options.
//•	PngCheck_OnClick, JpgCheck_OnClick: These methods handle the checking of the PNG and JPG options, updating the file lists and the rename button content.
//Summary
//The MainWindow class provides a user interface for selecting a folder, displaying lists of PNG and JPG files, and renaming these files based on user - defined options(prefix, CRC, or date - time).The class uses observable collections to bind the file lists to UI elements, ensuring that the UI updates automatically when the file lists change.



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
        private PngObservable PngData = null;
        public bool GetPngFilePathsInFolder()
        {
            PngFilesInFolder = new List<string>();
            

            string path = SourcePathTextBlock.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please select a folder path");
                return false;
            }
            string[] PngFiles = System.IO.Directory.GetFiles(path, "*.png");
            foreach (string PngFile in PngFiles)
            {
                PngFilesInFolder.Add(PngFile);
               
            }
            PngData = new PngObservable(PngFilesInFolder);

            return true;
        }

        List<string> JpgFilesInFolder = new List<string>();
        private JpgObservable JpgData = null;

        public bool GetJpgFilePathsInFolder()
        {
            JpgFilesInFolder = new List<string>();
           
            string path = SourcePathTextBlock.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please select a folder path");
                return false;
            }
            string[] JpgFiles = System.IO.Directory.GetFiles(path, "*.jpg");
            foreach (string JpgFile in JpgFiles)
            {
                JpgFilesInFolder.Add(JpgFile);
                
            }
            JpgData = new JpgObservable(JpgFilesInFolder);

            return true;
        }

        void ShowPngList()
        {
            Binding PngBinding = new Binding();
            PngBinding.Source = PngData;
            PngShowList.SetBinding(ItemsControl.ItemsSourceProperty, PngBinding); ;
        }

        void ShowJpgList()
        {

            Binding JpgBinding = new Binding();
            JpgBinding.Source = JpgData;
            JpgShowList.SetBinding(ItemsControl.ItemsSourceProperty, JpgBinding); ;
        }

        // rename a file
        public void RenameFile(string oldName, string newName)
        {
            try
            {
                if (System.IO.File.Exists(newName))
                {
                    return;
                }
                System.IO.File.Move(oldName, newName);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        private void RenameButton_OnClick(object sender, RoutedEventArgs e)
        {
            int cnt = _rnd.Next(10000, 99999);

            string changedfilename = string.Empty;

            if (PrefixTextBox.Text != "")
            {
                PrefixName = PrefixTextBox.Text;
            }


            PngFilesInFolder.Clear();
            JpgFilesInFolder.Clear();
            if (PngCheck.IsChecked == true)
            {
                if (GetPngFilePathsInFolder() == false)
                {
                    return;
                }
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

                        RenameFile(fff, npf);
                    }
                }
            }

            if (JpgCheck.IsChecked == true)
            {
                if (GetJpgFilePathsInFolder() == false)
                {
                    return;
                }
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
                    string npf = inf.DirectoryName + "\\" + changedfilename + ".png";
                    if (fff != npf)
                    {

                        RenameFile(fff, npf);
                    }
                }
            }
        }

        // chose a source folder path
        private void ChooseSource_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            SourcePathTextBlock.Text = dialog.SelectedPath;
            ShowFormat();
            GetPngFilePathsInFolder();
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

        private void ShowListButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (PngCheck.IsChecked == true)
            {
                JpgShowList.Visibility = Visibility.Collapsed;

                PngShowList.Visibility = Visibility.Visible;
                ShowPngList();
                
            }
            if (JpgCheck.IsChecked == true)
            {
                PngShowList.Visibility = Visibility.Collapsed;
                JpgShowList.Visibility = Visibility.Visible;
                ShowJpgList();
            }

        }

        private void PngCheck_OnClick(object sender, RoutedEventArgs e)
        {
            JpgCheck.IsChecked = false;
            PngFilesInFolder.Clear();
            GetPngFilePathsInFolder();
            ShowPngList();
            RenameButton.Content = "Rename All .png Files";
        }

        private void JpgCheck_OnClick(object sender, RoutedEventArgs e)
        {
           PngCheck.IsChecked= false;
           JpgFilesInFolder.Clear();
           GetJpgFilePathsInFolder();
           ShowJpgList();
           RenameButton.Content = "Rename All .jpg Files";
        }
    }
}
