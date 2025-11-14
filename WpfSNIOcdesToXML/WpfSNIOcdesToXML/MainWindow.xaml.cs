using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfSNIOcdesToXML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private XMLData XmlCSVMerger = new XMLData();

        public MainWindow()
        {
            InitializeComponent();
            TextBox_XmlFile.Text = @"C:\temp\test\SNI\Test-SNItree.xml";
            TextBox_CsvFile.Text = @"C:\temp\test\SNI\Test.nyckel-sni2007.csv";
            TextBox_CsvFile2.Text = @"C:\temp\test\SNI\struktur-refactored-light.csv";
        }

        private void BtnLoadXML_Click(object sender, RoutedEventArgs e)
        {
            XmlCSVMerger.XmlFIle = TextBox_XmlFile.Text;
            Label_XML.Content = XmlCSVMerger.XmlFIle;
        }

        private void BtnLoadCSV_Click(object sender, RoutedEventArgs e)
        {
            XmlCSVMerger.CsvFIle = TextBox_CsvFile.Text;
            Label_CSV.Content = XmlCSVMerger.CsvFIle;
        }

        private void BtnLoadCSV2_Click(object sender, RoutedEventArgs e)
        {
            XmlCSVMerger.CsvFile2 = TextBox_CsvFile2.Text;
            Label_CSV2.Content = XmlCSVMerger.CsvFile2;
        }

        private void BtnMERGE_Click(object sender, RoutedEventArgs e)
        {
            XmlCSVMerger.RUN();
            TextBox_ResultFile.Text = XmlCSVMerger.ResultFileXML;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


    }
}
