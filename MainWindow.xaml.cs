using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

using YS_Downloader.Downloader;
using YS_Downloader.Utils;

using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.Forms.MessageBox;

namespace YS_Downloader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ApiLoader apiLoader = new ApiLoader();
        private TextBlock logBlock;
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<DownloadInfo> _List = new ObservableCollection<DownloadInfo>();
        public ObservableCollection<DownloadInfo> List
        {
            get { return _List; }
            set
            {
                _List = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("List"));
            }
        }

        private int threadNum = 6;
        private string savePath;
        private string serverType = "CN";
        public int RemainingThread = 0;

        private bool clicked = false;

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string resourceName = "YS_Downloader." + new AssemblyName(args.Name).Name + ".dll";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }
        public MainWindow()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            InitializeComponent();
            Title = " 原神 下载器   GenshinImpact Downloader";
            setting_ServerType_ComboBox.SelectedIndex = 0;
            logBlock = status_LoggingBlock_TextBlock;
        }

        private void setting_ServerType_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            switch (setting_ServerType_ComboBox.SelectedIndex)
            {
                case 0:
                    this.serverType = "CN";
                    break;
                case 1:
                    this.serverType = "OS";
                    break;
                default:
                    this.serverType = "CN";
                    setting_ServerType_ComboBox.SelectedIndex = 0;
                    break;
            }
        }

        private void setting_Thread_UserInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]+");
            threadNum = int.Parse(e.Text.Trim());
            e.Handled = re.IsMatch(e.Text);
        }

        private void setting_savePath_OpenSelector_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if ((dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) &&
                !string.IsNullOrEmpty(dialog.SelectedPath.Trim()))
            {
                savePath = dialog.SelectedPath.Trim();
                setting_savePath_Display.Text = dialog.SelectedPath.Trim();
                e.Handled = true;
            }
            else
            {
                MessageBox.Show("文件夹不存在！", "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            if (clicked)
            {
                return;
            }
            clicked = true;
            logBlock.Text = "正在检查参数...";
            bool test = CheckUserInput();
            if (CheckUserInput())
            {
                logBlock.Text = "正在尝试获取启动器api地址...";
                try
                {
                    string launcherApi = await Task.Run(() =>
                    {
                        return apiLoader.GetLauncherApi(serverType);
                    });
                    if (!string.IsNullOrEmpty(launcherApi))
                    {
                        logBlock.Text = "成功获取到启动器api地址，正在获取版本信息...";

                        string version = await Task.Run(() =>
                        {
                            return apiLoader.GetLatestVersion(launcherApi);
                        });
                        string downloadApi = await Task.Run(() =>
                        {
                            return apiLoader.GetDownloadApi(launcherApi);
                        });
                        if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(downloadApi))
                        {
                            logBlock.Text = "成功获取到下载地址和版本信息！\n最新版本：" + version;
                            /*await Task.Run(() =>
                            {
                                Thread.Sleep(500);
                            });
                            */
                            logBlock.Text = "正在尝试获取文件列表...";
                            string response = await Task.Run(() =>
                            {
                                return apiLoader.GetFileInfoList(downloadApi);
                            });
                            if (!string.IsNullOrEmpty(response))
                            {
                                logBlock.Text = "成功获取文件列表，正在解析...";
                                string[] jsonList = await Task.Run(() =>
                                {
                                    return Parser.ParseContent(response);
                                });
                                string[] fileUrlList = await Task.Run(() =>
                                {
                                    return Parser.GetFileUrl(jsonList, downloadApi);
                                });
                                string[] filePathList = await Task.Run(() =>
                                {
                                    return Parser.GetFilePath(jsonList, savePath);
                                });
                                string[] fileSavePathList = await Task.Run(() =>
                                {
                                    return Parser.GetFileSavePath(filePathList);
                                });
                                string[] fileNameList = await Task.Run(() =>
                                {
                                    return Parser.GetFileNameList(filePathList);
                                });
                                logBlock.Text = "解析完毕，开始下载";
                                DownLoadFile downloader = new DownLoadFile();
                                downloader.ThreadNum = threadNum;
                                downloader.doSendMsg += SendMsgHander;
                                ListBox listBox = status_DownloadList_InfoView;
                                listBox.ItemsSource = List;
                                await Task.Run(async () =>
                                {
                                    for (int i = 0; i < fileUrlList.Length; i++)
                                    {
                                        string url = fileUrlList[i];
                                        string savePath = fileSavePathList[i];
                                        string fileName = fileNameList[i];
                                        App.Current.Dispatcher.Invoke(new Action(() =>
                                        {
                                            List.Add(new DownloadInfo(i + 1, fileName));
                                        }));
                                        
                                        downloader.AddDown(url, savePath, fileName, i + 1);
                                        RemainingThread++;
                                    }
                                    /*foreach(DownloadInfo d in List)
                                    {
                                        App.Current.Dispatcher.Invoke(() =>
                                        {
                                            listBox.Items.Add(d);
                                        });
                                    }*/
                                    downloader.StartDown();
                                    bool tmp = true;
                                    while (tmp)
                                    {
                                        await Task.Run(() =>
                                        {
                                            Thread.Sleep(3000);
                                        });
                                        if (RemainingThread == 0)
                                        {
                                            tmp = false;
                                            break;
                                        }
                                    }
                                    return;
                                });
                                if ((bool)setting_isGenerateConfig_CheckBox.IsChecked)
                                {
                                    logBlock.Text = "下载完成,正在创建启动器识别文件...";
                                    if(CreateConfig(version, savePath))
                                    {
                                        logBlock.Text = "创建完成，现在可以使用启动器加载游戏了！";
                                    }
                                    else
                                    {
                                        logBlock.Text = "创建失败，请手动创建config.ini，内容已复制到剪贴板";
                                    }
                                }
                                else
                                {
                                    logBlock.Text = "下载完成！";
                                }
                                

                            }
                            else
                            {
                                logBlock.Text = "获取失败，请检查网络后重试";
                                return;
                            }
                        }
                        else
                        {
                            logBlock.Text = "获取失败，请检查网络后重试";
                            return;
                        }
                    }
                    else
                    {
                        logBlock.Text = "获取失败，请检查网络后重试";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logBlock.Text = "出现错误！";
                    ExceptionMessage m = new ExceptionMessage(ex);
                    m.exceptionDebug();
                    m.exceptionInfo();
                    return;
                }
            }
            else
            {
                logBlock.Text = "参数验证失败，请检查参数";
                return;
            }
        }

        private void SendMsgHander(DownMsg msg)
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                DownloadInfo Item = List[msg.Id - 1];
                switch (msg.Tag)
                {
                    case DownStatus.Start:
                        Item.status = "正在下载";
                        break;
                    case DownStatus.GetLength:
                        Item.size = msg.LengthInfo; 
                        break;
                    case DownStatus.End:
                        Item.status = "下载完成";
                        Item.progress = "100%";
                        break;
                    case DownStatus.DownLoad:
                        //Item.size = msg.SizeInfo;
                        Item.size = msg.LengthInfo;
                        Item.progress = msg.Progress.ToString() + "%";
                        break;
                    case DownStatus.Error:
                        Item.status = "下载失败";
                        break;
                }
            }));
            


        }

        private Boolean CheckUserInput()
        {
            Boolean result = false;
            //检查非空
            if (!string.IsNullOrEmpty(serverType) && !string.IsNullOrEmpty(savePath) && !string.IsNullOrEmpty(threadNum.ToString()))
            {
                result = true;
                if (threadNum > 9)
                {
                    threadNum = 10;
                }
            }
            return result;
        }

        private static Boolean CreateConfig(string version, string filePath)
        {

            Boolean result = false;
            filePath = filePath + "/config.ini" ?? string.Empty;
            string INI_INFO = $"[General]\nchannel=1\ncps=mihoyo\ngame_version={version}\nsub_channel=1\nplugin_7_version=1.0.0\n";
            try
            {
                File.WriteAllText(filePath, INI_INFO);
                result = true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                MessageBox.Show($"Error: {ex.Message}");
#endif
                System.Windows.Clipboard.SetDataObject(INI_INFO);
            }
            return result;
        }
    }
    public class DownloadInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _id;
        private string _name;
        private string _progress;
        private string _size;
        private string _status;

        public int id
        {
            get { return _id; }
            set
            {
                _id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("id"));
            }
        }
        public string name
        {
            get { return _name; }
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("name"));
            }
        }
        public string progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("progress"));
            }
        }
        public string size
        {
            get { return _size; }
            set
            {
                _size = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("size"));
            }
        }
        public string status
        {
            get { return _status; }
            set
            {
                _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("status"));
            }
        }
        public DownloadInfo(int id, string name)
        {
            _id = id;
            _name = name;
            _progress = "0%";
            _size = "0";
            _status = "等待中";
        }
    }
}
