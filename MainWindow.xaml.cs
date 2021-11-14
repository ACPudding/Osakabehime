using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Osakabehime.Properties;
using MessageBox = HandyControl.Controls.MessageBox;

namespace Osakabehime
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string path = Directory.GetCurrentDirectory();
        private static readonly DirectoryInfo folder = new DirectoryInfo(path + @"\Android\");
        private static readonly DirectoryInfo gamedata = new DirectoryInfo(path + @"\Android\masterdata\");
        private static readonly string AssetStorageFilePath = gamedata.FullName + "AssetStorage.txt";
        private static readonly string AssetStorageLastFilePath = gamedata.FullName + "AssetStorage_last.txt";
        private static readonly DirectoryInfo AssetsFolder = new DirectoryInfo(folder + @"\assets\bin\Data\");
        private static string[,] tmp;
        private static string[,] tmpold;
        private static readonly object LockedList = new object();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MediaDecode(object sender, RoutedEventArgs e)
        {
            Start_DecryptMedia.IsEnabled = false;
            var path = Directory.GetCurrentDirectory();
            var inputdialog = new CommonOpenFileDialog {IsFolderPicker = true, Title = "需要解密的资源文件目录"};
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
                arg = inputfolder + " " + "2";
            else
                arg = inputfolder + " " + "4";
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

        private void DecryptBinfileSub(string[] assetStore, DirectoryInfo outputdest)
        {
            var decrypt = outputdest;
            var AudioArray = new JArray();
            var MovieArray = new JArray();
            var AssetArray = new JArray();
            for (var i = 2; i < assetStore.Length; ++i)
            {
                var tmp = assetStore[i].Split(',');
                string assetName;
                string fileName;
                if (tmp[4].Contains("Audio"))
                {
                    assetName = tmp[tmp.Length - 1].Replace('/', '@');
                    fileName = CatAndMouseGame.GetMD5String(assetName);
                    AudioArray.Add(new JObject(new JProperty("audioName", assetName),
                        new JProperty("fileName", fileName)));
                }
                else if (tmp[4].Contains("Movie"))
                {
                    assetName = tmp[tmp.Length - 1].Replace('/', '@');
                    fileName = CatAndMouseGame.GetMD5String(assetName);
                    MovieArray.Add(new JObject(new JProperty("movieName", assetName),
                        new JProperty("fileName", fileName)));
                }
                else if (!tmp[4].Contains("Movie"))
                {
                    assetName = tmp[tmp.Length - 1].Replace('/', '@') + ".unity3d";
                    fileName = CatAndMouseGame.GetShaName(assetName);
                    AssetArray.Add(new JObject(new JProperty("assetName", assetName),
                        new JProperty("fileName", fileName)));
                }
            }

            File.WriteAllText(decrypt.FullName + @"\AudioName.json", AudioArray.ToString());
            File.WriteAllText(decrypt.FullName + @"\MovieName.json", MovieArray.ToString());
            File.WriteAllText(decrypt.FullName + @"\AssetName.json", AssetArray.ToString());
        }

        private async Task DecryptBinFileFolderAsync(DirectoryInfo inputdest, DirectoryInfo outputdest)
        {
            var folder = inputdest;
            var decrypt = outputdest;
            if (!Directory.Exists(decrypt.FullName))
                Directory.CreateDirectory(decrypt.FullName);
            var renamedAudio = new DirectoryInfo(outputdest.FullName + @"\Audio\");
            var renamedMovie = new DirectoryInfo(outputdest.FullName + @"\Movie\");
            var renamedAssets = new DirectoryInfo(outputdest.FullName + @"\Assets\");
            byte[] raw;
            byte[] output;
            var RemindLog = "";
            var isDeleteFile = false;
            Dispatcher.Invoke(() =>
            {
                if (SrvJP.IsChecked == true) CatAndMouseGame.JP();
                if (SrvCN.IsChecked == true) CatAndMouseGame.CN();
                if (SrvEN.IsChecked == true) CatAndMouseGame.EN();
            });
            Dispatcher.Invoke(() =>
            {
                if (DelYes.IsChecked == true) isDeleteFile = true;
            });
            Dispatcher.Invoke(() => { Decrypt_Status.Items.Clear(); });
            var ifcontinue = true;
            Dispatcher.Invoke(() =>
            {
                if (RenameYes.IsChecked != true) return;
                if (File.Exists(folder.FullName + @"\cfb1d36393fd67385e046b084b7cf7ed"))
                {
                    if (File.Exists(decrypt.FullName + @"\AssetStorage.txt"))
                        File.Delete(decrypt.FullName + @"\AssetStorage.txt");
                    if (isDeleteFile)
                        File.Move(folder.FullName + @"\cfb1d36393fd67385e046b084b7cf7ed",
                            decrypt.FullName + @"\AssetStorage.txt");
                    else
                        File.Copy(folder.FullName + @"\cfb1d36393fd67385e046b084b7cf7ed",
                            decrypt.FullName + @"\AssetStorage.txt");
                    if (File.Exists(folder.FullName + @"\4fb2705e743f2eed610a17b9eaba5541"))
                    {
                        if (File.Exists(decrypt.FullName + @"\AssetStorageBack.txt"))
                            File.Delete(decrypt.FullName + @"\AssetStorageBack.txt");
                        if (isDeleteFile)
                            File.Move(folder.FullName + @"\4fb2705e743f2eed610a17b9eaba5541",
                                decrypt.FullName + @"\AssetStorageBack.txt");
                        else
                            File.Copy(folder.FullName + @"\4fb2705e743f2eed610a17b9eaba5541",
                                decrypt.FullName + @"\AssetStorageBack.txt");
                    }

                    var data = File.ReadAllText(decrypt.FullName + @"\AssetStorage.txt");
                    var loadData = CatAndMouseGame.MouseGame8(data);
                    File.WriteAllText(decrypt.FullName + @"\AssetStorage_dec.txt", loadData);
                    RemindLog = "写入: " + decrypt.FullName + @"\AssetStorage_dec.txt";
                    Decrypt_Status.Items.Insert(0, RemindLog);
                    var assetStore = File.ReadAllLines(decrypt.FullName + @"\AssetStorage_dec.txt");
                    var Sub1 = new Task(() => { DecryptBinfileSub(assetStore, decrypt); });
                    Sub1.Start();
                    Task.WaitAll(Sub1);
                }
                else
                {
                    MessageBox.Error(
                        "AssetStorage.txt文件不存在\r\n请检查输入文件夹内是否存在\"cfb1d36393fd67385e046b084b7cf7ed\"\r\n或者\"AssetStorage.txt\"文件.\r\n本次将跳过重命名内容.",
                        "错误");
                    ifcontinue = false;
                }
            });
            Dispatcher.Invoke(() => { Decrypt_Status.Items.Insert(0, "开始解密bin."); });
            var fileCountbin = Directory.GetFiles(folder.FullName, "*.bin").Length;
            if (fileCountbin == 0)
            {
                MessageBox.Error(
                    "不存在bin文件,请查看目录是否选择错误.",
                    "错误");
                Dispatcher.Invoke(() => { Decrypt_Status.Items.Clear(); });
                return;
            }

            var fileCountall = Directory.GetFiles(folder.FullName).Length;
            var progressValuebin = Convert.ToDouble(100000 / fileCountbin);
            var progressValueall = Convert.ToDouble(100000 / fileCountall);
            Parallel.ForEach(folder.GetFiles("*.bin"), file =>
            {
                try
                {
                    RemindLog = "解密: " + file.FullName;
                    Dispatcher.Invoke(() => { Decrypt_Status.Items.Insert(0, RemindLog); });
                    raw = File.ReadAllBytes(file.FullName);
                    output = CatAndMouseGame.MouseGame4(raw);
                    if (!Directory.Exists(renamedAssets.FullName))
                        Directory.CreateDirectory(renamedAssets.FullName);
                    File.WriteAllBytes(renamedAssets.FullName + @"\" + file.Name, output);
                    Dispatcher.Invoke(() => { Decrypt_Progress.Value += progressValuebin; });
                    if (isDeleteFile)
                        File.Delete(file.FullName);
                    Thread.Sleep(10);
                }
                catch (Exception)
                {
                    Dispatcher.Invoke(() => { MessageBox.Error("解密时遇到错误.\r\n", "错误"); });
                }
            });

            Dispatcher.Invoke(() =>
            {
                if (RenameYes.IsChecked == true) ifcontinue = true;
            });
            Dispatcher.Invoke(() => { Decrypt_Progress.Value = Decrypt_Progress.Maximum; });
            if (!ifcontinue)
            {
                Decrypt_Status.Items.Insert(0, "完成.");
                return;
            }

            Thread.Sleep(500);
            Dispatcher.Invoke(() => { Decrypt_Status.Items.Insert(0, "现在开始重命名所有资源文件.读取json中..."); });
            Thread.Sleep(1500);
            Dispatcher.Invoke(() => { Decrypt_Progress.Value = 0; });
            var AssetJsonName = File.ReadAllText(decrypt.FullName + @"\AssetName.json");
            var AssetJsonNameArray = (JArray) JsonConvert.DeserializeObject(AssetJsonName);
            var binCountall = Directory.GetFiles(renamedAssets.FullName).Length;
            progressValueall = Convert.ToDouble(100000 / binCountall);
            Parallel.ForEach(renamedAssets.GetFiles("*.bin"), file =>
            {
                Parallel.ForEach(AssetJsonNameArray, FileNametmp =>
                {
                    if (((JObject) FileNametmp)["fileName"].ToString() != file.Name) return;
                    var FileNameObjtmp = JObject.Parse(FileNametmp.ToString());
                    var FileAssetNametmp = FileNameObjtmp["assetName"].ToString();
                    RemindLog = "重命名: " + file.Name + " → \r\n" + FileAssetNametmp + "\n";
                    Dispatcher.Invoke(() => { Decrypt_Status.Items.Insert(0, RemindLog); });
                    if (File.Exists(renamedAssets.FullName + @"\" + FileAssetNametmp))
                        File.Delete(renamedAssets.FullName + @"\" + FileAssetNametmp);
                    File.Move(renamedAssets.FullName + @"\" + file.Name,
                        renamedAssets.FullName + @"\" + FileAssetNametmp);
                    Dispatcher.Invoke(() => { Decrypt_Progress.Value += progressValueall; });
                    Thread.Sleep(10);
                });
            });
        }

        private async void AssetDecrypt(object sender, RoutedEventArgs e)
        {
            var inputdialog = new CommonOpenFileDialog {IsFolderPicker = true, Title = "需要解密的资源文件目录"};
            var resultinput = inputdialog.ShowDialog();
            var inputfolder = "";
            if (resultinput == CommonFileDialogResult.Ok) inputfolder = inputdialog.FileName;
            if (inputfolder == "") return;
            var outputfolder = inputfolder + @"\Decrypted";
            var isDeleteFile = false;
            var input = new DirectoryInfo(inputfolder);
            var output = new DirectoryInfo(outputfolder);
            Start_Decrypt.IsEnabled = false;
            Decrypt_Progress.Value = 0;
            await Task.Run(async () => { await DecryptBinFileFolderAsync(input, output); });
            Thread.Sleep(1500);
            Start_Decrypt.IsEnabled = true;
        }

        private void DownloadOn(object sender, RoutedEventArgs e)
        {
            var Starter = new Task(DownloadAssets);
            Starter.Start();
        }

        private void DownloadAssets()
        {
            Dispatcher.Invoke(() => { Download_Status.Items.Clear(); });
            Dispatcher.Invoke(() => { Download_Progress.Value = 0.0; });
            Dispatcher.Invoke(() => { Start.IsEnabled = false; });
            if (!AssetsFolder.Exists)
            {
                AssetsFolder.Create();
            }
            else if (!File.Exists(AssetStorageFilePath))
            {
                MessageBox.Error("未找到AssetStorage.txt文件,请通过Altera进行下载.", "错误");
                Dispatcher.Invoke(() => { Start.IsEnabled = true; });
                return;
            }

            Dispatcher.Invoke(async () =>
            {
                if (Mode1.IsChecked != true)
                {
                    await Task.Run(DownloadAssetsSub).ConfigureAwait(false);
                    if (isDownloadAudio.IsChecked == true) await Task.Run(DownloadAudioSub).ConfigureAwait(false);
                    if (isDownloadMovie.IsChecked == true) await Task.Run(DownloadMovieSub).ConfigureAwait(false);
                    GC.Collect();
                }
                else
                {
                    await Task.Run(DownloadHighAcc).ConfigureAwait(false);
                    GC.Collect();
                }
            });
        }

        private async Task DHASub(IReadOnlyCollection<int> DownloadLine)
        {
            var ASLine = File.ReadAllLines(AssetStorageFilePath);
            var ASLineCount = ASLine.Length;
            var DataTimeStringINFO = ASLine[1].Split(',');
            var DataVersion = DataTimeStringINFO[2];
            var DownloadParallel = 1;
            _ = Dispatcher.Invoke(async () =>
            {
                if (TwoThread.IsChecked == true) DownloadParallel = 2;
                if (FourThread.IsChecked == true) DownloadParallel = 4;
            });
            var paralleloptions = new ParallelOptions {MaxDegreeOfParallelism = DownloadParallel};
            var ProgressBarValueAdd = 50000 / DownloadLine.Count;
            var assetBundleFolder = File.ReadAllText(gamedata.FullName + "assetBundleFolder.txt");
            Parallel.ForEach(DownloadLine, paralleloptions, async DownloadItem =>
            {
                if (tmp[DownloadItem, 4].Contains("Audio") || tmp[DownloadItem, 4].Contains("Movie"))
                {
                    var downloadName = tmp[DownloadItem, 4].Replace('/', '_');
                    var downloadfile = downloadName;
                    var writePath = AssetsFolder.FullName.Substring(0, AssetsFolder.FullName.Length - 1) + "@Version@" +
                                    DataVersion.Replace(":", "") + "\\" + tmp[DownloadItem, 4].Replace("/", "\\");
                    var writeDirectory = Path.GetDirectoryName(writePath);
                    if (!Directory.Exists(writeDirectory)) Directory.CreateDirectory(writeDirectory);
                    File.Delete(writePath);
                    await Task.Run(() =>
                    {
                        DownloadAssetsSpecialSub(assetBundleFolder, downloadfile, writePath, tmp[DownloadItem, 4],
                            ProgressBarValueAdd);
                    }).ConfigureAwait(false);
                }
                else
                {
                    var tmpname = tmp[DownloadItem, 4].Replace('/', '@') + ".unity3d";
                    var downloadName = CatAndMouseGame.GetShaName(tmpname);
                    var downloadfile = downloadName;
                    var writePath = AssetsFolder.FullName + tmpname.Replace('@', '\\').Replace("/", "\\");
                    var writeDirectory = Path.GetDirectoryName(writePath);
                    if (!Directory.Exists(writeDirectory)) Directory.CreateDirectory(writeDirectory);
                    File.Delete(writePath);
                    await Task.Run(() =>
                    {
                        DownloadAssetsSpecialSub(assetBundleFolder, downloadfile, writePath, tmp[DownloadItem, 4],
                            ProgressBarValueAdd);
                    }).ConfigureAwait(false);
                }
            });
        }

        private async Task<List<int>> FindASDiffer(int min, int max)
        {
            var resultlist = new List<int>();
            var ASLine = File.ReadAllLines(AssetStorageFilePath);
            var ASOldLine = File.ReadAllLines(AssetStorageLastFilePath);
            tmpold = new string[ASOldLine.Length, 5];
            tmp = new string[ASLine.Length, 5];
            for (var kk = 0; kk < ASLine.Length; kk++)
            {
                var tmpkk = ASLine[kk].Split(',');
                if (tmpkk.Length != 5)
                {
                    tmp[kk, 0] = "0";
                    tmp[kk, 1] = "0";
                    tmp[kk, 2] = "0";
                    tmp[kk, 3] = "0";
                    tmp[kk, 4] = "0";
                    continue;
                }

                tmp[kk, 0] = tmpkk[0];
                tmp[kk, 1] = tmpkk[1];
                tmp[kk, 2] = tmpkk[2];
                tmp[kk, 3] = tmpkk[3];
                tmp[kk, 4] = tmpkk[4];
            }

            for (var jj = 0; jj < ASOldLine.Length; jj++)
            {
                var tmpkk = ASOldLine[jj].Split(',');
                if (tmpkk.Length != 5)
                {
                    tmpold[jj, 0] = "0";
                    tmpold[jj, 1] = "0";
                    tmpold[jj, 2] = "0";
                    tmpold[jj, 3] = "0";
                    tmpold[jj, 4] = "0";
                    continue;
                }

                tmpold[jj, 0] = tmpkk[0];
                tmpold[jj, 1] = tmpkk[1];
                tmpold[jj, 2] = tmpkk[2];
                tmpold[jj, 3] = tmpkk[3];
                tmpold[jj, 4] = tmpkk[4];
            }

            for (var i = max - 1; i >= min; i--)
            {
                if (tmp[i, 0] == "0") continue;
                try
                {
                    var addValueTask = await FindASDifferNiceSub(tmp[i, 4], tmp[i, 2], tmp[i, 3]);
                    switch (addValueTask)
                    {
                        case "true":
                            resultlist.Add(i);
                            break;
                        case "false":
                            break;
                        case "NeedSecCheck":
                            resultlist.Add(i);
                            break;
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            GC.Collect();
            return resultlist;
        }

        private async Task<string> FindASDifferNiceSub(string FindStr, string check1, string check2)
        {
            var value = "false";
            var countno = 0;
            Parallel.For(0, tmpold.GetLength(0), j =>
            {
                if (tmpold[j, 0] == "0" || tmpold[j, 4] != FindStr)
                {
                    lock (LockedList)
                    {
                        countno++;
                    }

                    return;
                }

                if (tmpold[j, 2] == check1 && tmpold[j, 3] == check2) return;
                value = "true";
                _ = Dispatcher.InvokeAsync(() => { Download_Status.Items.Insert(0, "差异: " + tmpold[j, 4]); });
            });
            if (countno != tmpold.GetLength(0)) return value;
            value = "true";
            return value;
        }

        private async void DownloadHighAcc()
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                Download_Status.Items.Insert(0, "正在检测需要下载的文件... ");
                Download_Progress.Value = 0;
            });
            int ASLineCount;
            await Task.Delay(2000);
            try
            {
                ASLineCount = File.ReadAllLines(AssetStorageFilePath).Length;
            }
            catch (Exception)
            {
                MessageBox.Error("未找到AssetStorage.txt文件,请通过Altera进行下载.", "错误");
                Dispatcher.Invoke(() =>
                {
                    Start.IsEnabled = true;
                    Download_Status.Items.Clear();
                });
                return;
            }

            var n = ASLineCount / 8;
            var mod = ASLineCount % 8;
            var task1 = FindASDiffer(0, n);
            var task2 = FindASDiffer(n, 2 * n);
            var task3 = FindASDiffer(2 * n, 3 * n);
            var task4 = FindASDiffer(3 * n, 4 * n);
            var task5 = FindASDiffer(4 * n, 5 * n);
            var task6 = FindASDiffer(5 * n, 6 * n);
            var task7 = FindASDiffer(6 * n, 7 * n);
            var task8 = FindASDiffer(7 * n, 8 * n + mod);
            var DownloadLinePart1List = new List<int>();
            var DownloadLinePart2List = new List<int>();
            var DownloadLinePart3List = new List<int>();
            var DownloadLinePart4List = new List<int>();
            var DownloadLinePart5List = new List<int>();
            var DownloadLinePart6List = new List<int>();
            var DownloadLinePart7List = new List<int>();
            var DownloadLinePart8List = new List<int>();
            var actions = new Action[]
            {
                async () => { DownloadLinePart1List.AddRange(await task1); },
                async () => { DownloadLinePart2List.AddRange(await task2); },
                async () => { DownloadLinePart3List.AddRange(await task3); },
                async () => { DownloadLinePart4List.AddRange(await task4); },
                async () => { DownloadLinePart5List.AddRange(await task5); },
                async () => { DownloadLinePart6List.AddRange(await task6); },
                async () => { DownloadLinePart7List.AddRange(await task7); },
                async () => { DownloadLinePart8List.AddRange(await task8); }
            };
            Parallel.Invoke(actions);
            var DownloadLine = new List<int>();
            DownloadLine.AddRange(DownloadLinePart1List);
            DownloadLine.AddRange(DownloadLinePart2List);
            DownloadLine.AddRange(DownloadLinePart3List);
            DownloadLine.AddRange(DownloadLinePart4List);
            DownloadLine.AddRange(DownloadLinePart5List);
            DownloadLine.AddRange(DownloadLinePart6List);
            DownloadLine.AddRange(DownloadLinePart7List);
            DownloadLine.AddRange(DownloadLinePart8List);
            _ = Dispatcher.InvokeAsync(() =>
            {
                Download_Status.Items.Clear();
                Download_Status.Items.Insert(0, "核对完成,准备下载... ");
                Download_Progress.Value = 0;
            });
            await Task.Delay(2000);
            var DownloadLineArray = DownloadLine.ToArray();
            _ = Dispatcher.InvokeAsync(() =>
            {
                Download_Status.Items.Insert(0, "共需下载" + DownloadLineArray.Length + "个差异文件.");
                Download_Status.Items.Insert(0, "下载按钮将不再可用,如需重置请选择右侧的\"重置进度\"按钮.");
                Download_Status.Items.Insert(0, "若进度条长期不动以及VPN没有流量即下载完成.");
                ResetDownloadStatus.Visibility = Visibility.Visible;
            });
            await Task.Delay(2000);
            await Task.Run(() => { _ = DHASub(DownloadLineArray); }).ConfigureAwait(false);

            GC.Collect();
        }

        private void DownloadAssetsSpecialSub(string assetBundleFolder, string filename, string writePath, string names,
            int ProgressBarValueAdd)
        {
            try
            {
                var raw = HttpRequest
                    .Get($"https://cdn.data.fate-go.jp/AssetStorages/{assetBundleFolder}Android/{filename}")
                    .ToBinary();
                var output = writePath.Contains("unity3d") ? CatAndMouseGame.MouseGame4(raw) : raw;
                using (var fs = new FileStream(writePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Write(output, 0, output.Length);
                }

                _ = Dispatcher.InvokeAsync(() =>
                {
                    Download_Status.Items.Insert(0, "下载: " + names);
                    Download_Progress.Value += ProgressBarValueAdd;
                });
            }
            catch (Exception ex)
            {
                _ = Dispatcher.InvokeAsync(() =>
                {
                    Download_Status.Items.Insert(0, "下载错误: " + names);
                    Download_Status.Items.Insert(0, ex);
                });
            }
        }

        private async void DownloadAssetsSub()
        {
            var ASLine = File.ReadAllLines(AssetStorageFilePath);
            var ASLineCount = ASLine.Length;
            var DataTimeStringINFO = ASLine[1].Split(',');
            var DataVersion = DataTimeStringINFO[2];
            var ProgressBarValueAdd = Convert.ToInt32(50000 / ASLineCount);
            var assetBundleFolder = File.ReadAllText(gamedata.FullName + "assetBundleFolder.txt");
            var assetList = JArray.Parse(File.ReadAllText(gamedata.FullName + "AssetName.json"));
            var DownloadParallel = 1;
            Dispatcher.Invoke(() =>
            {
                if (TwoThread.IsChecked == true) DownloadParallel = 2;
                if (FourThread.IsChecked == true) DownloadParallel = 4;
            });
            var paralleloptions = new ParallelOptions {MaxDegreeOfParallelism = DownloadParallel};
            Parallel.ForEach(assetList, paralleloptions, asset =>
            {
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    var filename = asset["fileName"].ToString();
                    var assetName = asset["assetName"].ToString();
                    if (!assetName.EndsWith(".unity3d")) return;
                    var writePath = AssetsFolder.FullName.Substring(0, AssetsFolder.FullName.Length - 1) + "@Version@" +
                                    DataVersion.Replace(":", "") + "\\";
                    var names = assetName.Split('@');
                    if (names.Length > 1)
                    {
                        writePath += string.Join(@"\", names);
                        var writeDirectory = Path.GetDirectoryName(writePath);
                        if (!Directory.Exists(writeDirectory)) Directory.CreateDirectory(writeDirectory);
                    }
                    else
                    {
                        writePath = AssetsFolder.FullName + assetName;
                    }

                    if (File.Exists(writePath))
                    {
                        if (Mode2.IsChecked == true)
                        {
                            Download_Status.Items.Insert(0, "跳过: " + $"{string.Join(@"\", names)}");
                            Download_Progress.Value += ProgressBarValueAdd;
                            return;
                        }

                        File.Delete(writePath);
                    }

                    await Task.Run(() =>
                    {
                        DownloadAssetSub1(assetBundleFolder, filename, writePath, names, ProgressBarValueAdd);
                    }).ConfigureAwait(false);
                });
            });
            _ = Dispatcher.InvokeAsync(() => { Start.IsEnabled = true; });
            GC.Collect();
        }

        private async void DownloadAudioSub()
        {
            var ASLine = File.ReadAllLines(AssetStorageFilePath);
            var ASLineCount = ASLine.Length;
            var DataTimeStringINFO = ASLine[1].Split(',');
            var DataVersion = DataTimeStringINFO[2];
            var ProgressBarValueAdd = Convert.ToInt32(50000 / ASLineCount);
            var assetBundleFolder = File.ReadAllText(gamedata.FullName + "assetBundleFolder.txt");
            var audioList = JArray.Parse(File.ReadAllText(gamedata.FullName + "AudioName.json"));
            var DownloadParallel = 1;
            Dispatcher.Invoke(() =>
            {
                if (TwoThread.IsChecked == true) DownloadParallel = 2;
                if (FourThread.IsChecked == true) DownloadParallel = 4;
            });
            var paralleloptions = new ParallelOptions {MaxDegreeOfParallelism = DownloadParallel};
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (isDownloadAudio.IsChecked == true)
                    Parallel.ForEach(audioList, paralleloptions, async audio =>
                    {
                        var audioName = audio["audioName"].ToString();
                        var writePath = AssetsFolder.FullName.Substring(0, AssetsFolder.FullName.Length - 1) +
                                        "@Version@" + DataVersion.Replace(":", "") + "\\";
                        var names = audioName.Split('@');
                        if (names.Length > 1)
                        {
                            writePath += string.Join(@"\", names);
                            var writeDirectory = Path.GetDirectoryName(writePath);
                            if (!Directory.Exists(writeDirectory)) Directory.CreateDirectory(writeDirectory);
                        }
                        else
                        {
                            writePath = AssetsFolder.FullName + audioName;
                        }

                        if (File.Exists(writePath))
                        {
                            if (Mode2.IsChecked == true)
                            {
                                Download_Status.Items.Insert(0, "跳过: " + $"{string.Join(@"\", names)}");
                                Download_Progress.Value += ProgressBarValueAdd;
                                return;
                            }

                            File.Delete(writePath);
                        }

                        var realAudioDownloadName = audioName.Replace("@", "_");
                        await Task.Run(() =>
                        {
                            DownloadAssetSub2(assetBundleFolder, realAudioDownloadName, writePath, names,
                                ProgressBarValueAdd);
                        }).ConfigureAwait(false);
                    });
            });
            GC.Collect();
        }

        private void DownloadMovieSub()
        {
            var ASLine = File.ReadAllLines(AssetStorageFilePath);
            var ASLineCount = ASLine.Length;
            var DataTimeStringINFO = ASLine[1].Split(',');
            var DataVersion = DataTimeStringINFO[2];
            var ProgressBarValueAdd = Convert.ToInt32(50000 / ASLineCount);
            var assetBundleFolder = File.ReadAllText(gamedata.FullName + "assetBundleFolder.txt");
            var movieList = JArray.Parse(File.ReadAllText(gamedata.FullName + "MovieName.json"));
            var DownloadParallel = 1;
            Dispatcher.Invoke(() =>
            {
                if (TwoThread.IsChecked == true) DownloadParallel = 2;
                if (FourThread.IsChecked == true) DownloadParallel = 4;
            });
            var paralleloptions = new ParallelOptions {MaxDegreeOfParallelism = DownloadParallel};
            Dispatcher.InvokeAsync(() =>
            {
                if (isDownloadMovie.IsChecked == true)
                    Parallel.ForEach(movieList, paralleloptions, async movie =>
                    {
                        var movieName = movie["movieName"].ToString();
                        var writePath = AssetsFolder.FullName.Substring(0, AssetsFolder.FullName.Length - 1) +
                                        "@Version@" + DataVersion.Replace(":", "") + "\\";
                        var names = movieName.Split('@');
                        if (names.Length > 1)
                        {
                            writePath += string.Join(@"\", names);
                            var writeDirectory = Path.GetDirectoryName(writePath);
                            if (!Directory.Exists(writeDirectory)) Directory.CreateDirectory(writeDirectory);
                        }
                        else
                        {
                            writePath = AssetsFolder.FullName + movieName;
                        }

                        if (File.Exists(writePath))
                        {
                            if (Mode2.IsChecked == true)
                            {
                                Download_Status.Items.Insert(0, "跳过: " + $"{string.Join(@"\", names)}");
                                Download_Progress.Value += ProgressBarValueAdd;
                                return;
                            }

                            File.Delete(writePath);
                        }

                        var realAudioDownloadName = movieName.Replace("@", "_");
                        await Task.Run(() =>
                        {
                            DownloadAssetSub2(assetBundleFolder, realAudioDownloadName, writePath, names,
                                ProgressBarValueAdd);
                        }).ConfigureAwait(false);
                    });
            });
            GC.Collect();
        }

        private void DownloadAssetSub1(string assetBundleFolder, string filename, string writePath, string[] names,
            int ProgressBarValueAdd)
        {
            try
            {
                var raw = HttpRequest
                    .Get($"https://cdn.data.fate-go.jp/AssetStorages/{assetBundleFolder}Android/{filename}")
                    .ToBinary();
                var output = CatAndMouseGame.MouseGame4(raw);
                using (var fs = new FileStream(writePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Write(output, 0, output.Length);
                }

                _ = Dispatcher.InvokeAsync(() =>
                {
                    Download_Status.Items.Insert(0, "下载: " + $"{string.Join(@"\", names)}");
                    Download_Progress.Value += ProgressBarValueAdd;
                });
            }
            catch (Exception ex)
            {
                _ = Dispatcher.InvokeAsync(() =>
                {
                    Download_Status.Items.Insert(0, "下载错误: " + $"{string.Join(@"\", names)}");
                    Download_Progress.Value += ProgressBarValueAdd;
                    Download_Status.Items.Insert(0, ex);
                });
            }
        }

        private void DownloadAssetSub2(string assetBundleFolder, string filename, string writePath, string[] names,
            int ProgressBarValueAdd)
        {
            try
            {
                var raw = HttpRequest
                    .Get($"https://cdn.data.fate-go.jp/AssetStorages/{assetBundleFolder}Android/{filename}")
                    .ToBinary();
                var output = raw;
                using (var fs = new FileStream(writePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Write(output, 0, output.Length);
                }

                _ = Dispatcher.InvokeAsync(() =>
                {
                    Download_Status.Items.Insert(0, "下载: " + $"{string.Join(@"\", names)}");
                    Download_Progress.Value += ProgressBarValueAdd;
                });
            }
            catch (Exception ex)
            {
                _ = Dispatcher.InvokeAsync(() =>
                {
                    Download_Status.Items.Insert(0, "下载错误: " + $"{string.Join(@"\", names)}");
                    Download_Status.Items.Insert(0, ex);
                });
            }
        }

        private void On_Load(object sender, EventArgs e)
        {
            VersionLabel.Dispatcher.Invoke(() => { VersionLabel.Text = CommonStrings.Version; });
        }

        private void ClearSuper1(object sender, RoutedEventArgs e)
        {
            ResetDownloadStatus.Visibility = Visibility.Collapsed;
            Start.IsEnabled = true;
            Download_Status.Items.Clear();
        }

        private void VersionCheckEvent()
        {
            string VerChkRaw;
            JObject VerChk;
            JArray VerAssetsJArray;
            CommonStrings.ExeUpdateUrl = "";
            CommonStrings.NewerVersion = "";
            try
            {
                VerChkRaw = HttpRequest.GetApplicationUpdateJson();
                VerChk = JObject.Parse(VerChkRaw);
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() => { MessageBox.Error("网络连接异常,请检查网络连接并重试.\r\n" + e, "网络连接异常"); });
                CheckUpdate.Dispatcher.Invoke(() => { CheckUpdate.IsEnabled = true; });
                return;
            }

            if (CommonStrings.VersionTag != VerChk["tag_name"].ToString())
            {
                Dispatcher.Invoke(() =>
                {
                    CommonStrings.SuperMsgBoxRes = MessageBox.Show(
                        Application.Current.MainWindow,
                        "检测到软件更新\r\n\r\n新版本为:  " + VerChk["tag_name"] + "    当前版本为:  " + CommonStrings.VersionTag +
                        "\r\n\r\nChangeLog:\r\n" + VerChk["body"] + "\r\n\r\n点击\"确认\"按钮可选择更新.", "检查更新",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Question);
                });
                if (CommonStrings.SuperMsgBoxRes == MessageBoxResult.OK)
                {
                    VerAssetsJArray = (JArray) JsonConvert.DeserializeObject(VerChk["assets"].ToString());
                    for (var i = 0; i <= VerAssetsJArray.Count - 1; i++)
                        if (VerAssetsJArray[i]["name"].ToString() == "Osakabehime.exe")
                            CommonStrings.ExeUpdateUrl = VerAssetsJArray[i]["browser_download_url"].ToString();
                    if (CommonStrings.ExeUpdateUrl == "")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                Application.Current.MainWindow, "确认到新版本更新,但是获取下载Url失败.\r\n", "获取Url失败",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        });
                        MessageBox.Error("确认到新版本更新,但是获取下载Url失败.\r\n", "获取Url失败");
                        CheckUpdate.Dispatcher.Invoke(() => { CheckUpdate.IsEnabled = true; });
                        return;
                    }

                    var Sub = new Task(() => { DownloadFilesSub(VerChk["tag_name"].ToString()); });
                    Sub.Start();
                }
                else
                {
                    CheckUpdate.Dispatcher.Invoke(() => { CheckUpdate.IsEnabled = true; });
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Info("当前版本为:  " + CommonStrings.VersionTag + "\r\n\r\n无需更新.", "检查更新");
                });
                CheckUpdate.Dispatcher.Invoke(() => { CheckUpdate.IsEnabled = true; });
            }
        }

        private void DownloadFilesSub(object VerChk)
        {
            var path = Directory.GetCurrentDirectory();
            try
            {
                DownloadFile(CommonStrings.ExeUpdateUrl, path + "/Osakabehime(Update " + VerChk + ").exe");
                CommonStrings.NewerVersion = VerChk.ToString();
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        Application.Current.MainWindow, "写入文件异常.\r\n" + e, "异常", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
                CheckUpdate.Dispatcher.Invoke(() => { CheckUpdate.IsEnabled = true; });
                throw;
            }
        }

        public void DownloadFile(string url, string filePath)
        {
            var Downloads = new WebClient();
            CommonStrings.StartTime = DateTime.Now;
            progressbar1.Dispatcher.Invoke(() =>
            {
                progressbar1.Visibility = Visibility.Visible;
                progressbar1.Value = 0;
            });
            updatestatus2.Dispatcher.Invoke(() => { updatestatus2.Text = ""; });
            Downloads.DownloadProgressChanged += OnDownloadProgressChanged;
            Downloads.DownloadFileCompleted += OnDownloadFileCompleted;
            Downloads.DownloadFileAsync(new Uri(url), filePath);
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var path = Directory.GetCurrentDirectory();
            Dispatcher.Invoke(() =>
            {
                CommonStrings.SuperMsgBoxRes = MessageBox.Show(
                    Application.Current.MainWindow,
                    "下载完成.下载目录为: \r\n" + path + "\\Osakabehime(Update " +
                    CommonStrings.NewerVersion +
                    ").exe\r\n\r\n请自行替换文件.\r\n\r\n您是否要关闭当前版本的程序?", "检查更新", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });
            if (CommonStrings.SuperMsgBoxRes == MessageBoxResult.Yes)
                Dispatcher.Invoke(Close);
            CheckUpdate.Dispatcher.Invoke(() => { CheckUpdate.IsEnabled = true; });
            progressbar1.Dispatcher.Invoke(() =>
            {
                progressbar1.Visibility = Visibility.Hidden;
                progressbar1.Value = 0;
            });
            updatestatus2.Dispatcher.Invoke(() => { updatestatus2.Text = ""; });
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressbar1.Dispatcher.Invoke(() => { progressbar1.Value = e.ProgressPercentage; });
            var s = (DateTime.Now - CommonStrings.StartTime).TotalSeconds;
            var sd = HttpRequest.ReadableFilesize(e.BytesReceived / s);
            updatestatus2.Dispatcher.Invoke(() =>
            {
                updatestatus2.Text = "下载速度: " + sd + "/s" + ", 已下载: " +
                                     HttpRequest.ReadableFilesize(e.BytesReceived) + " / 总计: " +
                                     HttpRequest.ReadableFilesize(e.TotalBytesToReceive);
            });
        }

        private void CheckUpdateThread(object sender, RoutedEventArgs e)
        {
            CheckUpdate.IsEnabled = false;
            var VCE = new Task(VersionCheckEvent);
            VCE.Start();
        }
    }
}