using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using MessageBox = HandyControl.Controls.MessageBox;

namespace Osakabehime
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MediaDecode(object sender, RoutedEventArgs e)
        {
            Start_DecryptMedia.IsEnabled = false;
            var path = Directory.GetCurrentDirectory();
            var inputdialog = new CommonOpenFileDialog { IsFolderPicker = true, Title = "需要解密的资源文件目录" };
            var resultinput = inputdialog.ShowDialog();
            var inputfolder = "";
            if (resultinput == CommonFileDialogResult.Ok) inputfolder = inputdialog.FileName;
            if (inputfolder == "")
            {
                MessageBox.Error("错误的文件夹.", "温馨提示:");
                Start_DecryptMedia.IsEnabled = true;
                return;
            }
            var arg = "";
            if (SelAudio.IsChecked == true)
            {
                arg = inputfolder + " " + "2";
            }
            else
            {
                arg = inputfolder + " " + "4";
            }
            var process = new Process
            {
                StartInfo =
                {
                    FileName = path + @"\FGOAudioDecoder.exe",
                    Arguments = arg,
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            MessageBox.Info("完成解包,点击确定打开文件夹.", "完成");
            Process.Start(inputfolder);
            Start_DecryptMedia.IsEnabled = true;
        }
    }
}
