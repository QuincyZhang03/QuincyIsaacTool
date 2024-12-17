using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
/*
 修复盗版存档列表滚动UI，修复手动删除配置文件导致崩溃
 */
namespace QuincyIsaac
{
    public partial class MainWindow : Window
    {
        const string UPDATE_CHECK_WEBSITE = "https://gitee.com/quincyzhang/quincy-isaac-tool-ver/blob/master/version.txt";
        readonly int MAJOR_VERSION;
        readonly int MINOR_VERSION;
        readonly int AMEND_VERSION;

        readonly string CONFIG_PATH = Environment.GetEnvironmentVariable("USERPROFILE")
            + "\\Documents\\My Games\\Binding of Isaac Repentance\\options.ini";

        readonly string CONFIG_PATH_PLUS = Environment.GetEnvironmentVariable("USERPROFILE")
            + "\\Documents\\My Games\\Binding of Isaac Repentance+\\options.ini";
        readonly string REG_GAME_VER_KEY = "DefaultGameVer";
        readonly RegistryKey registry = Registry.CurrentUser.CreateSubKey("SOFTWARE\\QuincyIsaacTool");

        const int NORMAL_HEIGHT = 170;
        const int EXPANDED_HEIGHT = 350;

        string active_option;
        string original_content;

        readonly bool rep_exists;
        readonly bool plus_exists;
        bool initialized = false;
        string steamID;
        public MainWindow()
        {
            string[] VERSION = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion.Split('.');
            MAJOR_VERSION = int.Parse(VERSION[0]);
            MINOR_VERSION = int.Parse(VERSION[1]);
            AMEND_VERSION = int.Parse(VERSION[2]);
            string displayed_amend_version = AMEND_VERSION == 0 ? "" : "." + AMEND_VERSION;

            InitializeComponent();
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = (SystemParameters.WorkArea.Height - Height) / 2 - 100;
            Title = $"夏老师的以撒快捷开关 V{MAJOR_VERSION}.{MINOR_VERSION}{displayed_amend_version}";

            rep_exists = File.Exists(CONFIG_PATH);
            plus_exists = File.Exists(CONFIG_PATH_PLUS);
            ver_rep.IsEnabled = rep_exists;
            ver_plus.IsEnabled = plus_exists;

            if (!rep_exists && !plus_exists)
            {
                MessageBox.Show($"对不起，程序未能在路径{CONFIG_PATH}或{CONFIG_PATH_PLUS}下找到游戏配置文件options.ini！\n" +
                "注意：本工具仅适用于忏悔或忏悔+。如果尚未运行过忏悔或忏悔+，请至少运行一次后再使用本工具。\n" +
                "反馈BUG可以通过QQ1391070463或在百度贴吧@炎炎夏日Quincy 联系我！",
                "运行失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            else
            {
                int defaultGameVer;
                if (registry != null)
                {
                    object regData = registry.GetValue(REG_GAME_VER_KEY);
                    if (regData is int v)
                    {
                        defaultGameVer = v;
                    }
                    else //数据被篡改或为null，置1
                    {
                        defaultGameVer = 1;
                        registry.SetValue(REG_GAME_VER_KEY, 1);
                    }

                    if (defaultGameVer == 1 && rep_exists)
                    {
                        ver_rep.IsChecked = true;
                        CheckRep(false);
                    }
                    else if (defaultGameVer == 2 && plus_exists)
                    {
                        ver_plus.IsChecked = true;
                        CheckPlus(false);
                    }
                    else //注册表默认版本不再存在，有什么选什么，并对注册表进行修正
                    {
                        if (rep_exists)
                        {
                            ver_rep.IsChecked = true;
                            CheckRep();
                        }
                        else
                        {
                            ver_plus.IsChecked = true;
                            CheckPlus();
                        }
                    }
                }
                else //registry为空，跳过所有跟注册表相关的逻辑，有什么选什么。
                     //但一般情况下不会出现这种情况。
                {
                    if (rep_exists)
                        ver_rep.IsChecked = true;
                    else
                        ver_plus.IsChecked = true;
                }
            }

            if (IsIsaacLaunched())
            {
                MessageBox.Show("检测到以撒的结合正在运行，为防止配置文件出错，建议先关闭游戏后再使用本程序。", "游戏正在运行", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            ReloadSettings();
        }

        private void ReloadSettings()
        {
            initialized = false;
            original_content = GetConfigContent();
            LoadSetting("EnableMods", mody, modn);
            LoadSetting("EnableDebugConsole", cony, conn);
            LoadSetting("MouseControl", mousey, mousen);
            LoadSetting("SteamCloud", cloudy, cloudn);

            l_mod.Text = "是否启用模组";
            l_mod.Foreground = Brushes.Black;
            l_con.Text = "是否启用控制台";
            l_con.Foreground = Brushes.Black;
            l_mouse.Text = "鼠标控制";
            l_mouse.Foreground = Brushes.Black;
            l_cloud.Text = "Steam云存档";
            l_cloud.Foreground = Brushes.Black;

            initialized = true;
        }

        private void Update()
        {
            try
            {
                TimedWebClient webClient = new TimedWebClient();
                byte[] data = webClient.DownloadData(UPDATE_CHECK_WEBSITE);
                string content = Encoding.UTF8.GetString(data).Replace("&lt;", "<").Replace("&gt;", ">");
                int majorVer = int.Parse(GetMidString(content, "<主版本>", "</主版本>"));
                int minorVer = int.Parse(GetMidString(content, "<子版本>", "</子版本>"));
                int amendVer = int.Parse(GetMidString(content, "<修正版本>", "</修正版本>"));
                string notice = GetMidString(content, "<公告>", "</公告>").Replace("<lb>", "\n");
                //访问网络资源的行为不能放在UI线程里
                tab_update.Dispatcher.Invoke(new Action(() =>
                {
                    p_update_all_content.Visibility = Visibility.Visible;
                    if (HasNewerVersion(majorVer, minorVer, amendVer))
                    {
                        tab_update.Background = Brushes.YellowGreen;
                        l_version_title.Text = "发现新版本:";

                        string displayed_new_amend_version = amendVer == 0 ? "" : "." + amendVer;
                        l_latest_version.Text = $"V{majorVer}.{minorVer}{displayed_new_amend_version}";

                    }
                    else
                    {
                        p_new_version_info.Visibility = Visibility.Collapsed;
                    }
                    l_notice.Text = notice;
                }));
            }
            catch
            {
                tab_update.Dispatcher.Invoke(new Action(() => tab_update.IsEnabled = false));
            }

        }
        private bool HasNewerVersion(int majorVer, int minorVer, int amendVer)
        {
            if (majorVer > MAJOR_VERSION)
            {
                return true;
            }
            if (majorVer == MAJOR_VERSION && minorVer > MINOR_VERSION)
            {
                return true;
            }
            if (majorVer == MAJOR_VERSION && minorVer == MINOR_VERSION && amendVer > AMEND_VERSION)
            {
                return true;
            }
            return false;
        }

        private string GetMidString(string source, string pref, string suff)
        {
            int prefindex = source.IndexOf(pref);
            int suffindex = source.IndexOf(suff);
            //Console.WriteLine(prefindex + " " + suffindex);
            if (prefindex == -1 || suffindex == -1 || prefindex >= suffindex)
            {
                //Console.WriteLine(source);
                return null;
            }
            int startindex = prefindex + pref.Length;
            return source.Substring(startindex, suffindex - startindex);
        }

        private string GetConfigContent()
        {
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(active_option);
                string config = reader.ReadToEnd();
                reader.Close();
                return config;
            }
            catch
            {
                if (reader != null) reader.Close();
                return null;
            }
        }
        private string ExecuteContentReplacement(string original_key, string replacement)
        {
            string newcontent = Regex.Replace(original_content, original_key, replacement);
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(active_option, false);
                writer.Write(newcontent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("修改配置文件失败！\n异常信息：\n" + ex, "更改配置失败");
            }
            finally
            {
                if (writer != null) writer.Close();
            }
            return newcontent;
        }
        private bool IsIsaacLaunched()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                if (process.ProcessName == "isaac-ng")
                {
                    return true;
                }
            }
            return false;
        }

        private void LoadSetting(string key, RadioButton callbackY, RadioButton callbackN)
        {
            string item_name = key + "=";
            string value = original_content.Substring(original_content.IndexOf(item_name) + item_name.Length, 1);
            if (value == "1")
            {
                callbackY.IsChecked = true;
            }
            else if (value == "0")
            {
                callbackN.IsChecked = true;
            }
            else
            {
                MessageBox.Show($"对不起，在读取{key}时配置文件异常！", "文件异常", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void ModifySetting(string key, bool enabled, TextBlock callback, string setting_name)
        {
            if (!initialized) return;
            string value = enabled ? "1" : "0";
            original_content = ExecuteContentReplacement($"{key}=\\d", $"{key}={value}");
            string newFileContent = GetConfigContent();

            if (original_content == newFileContent) //校验
            {
                if (enabled)
                {
                    callback.Text = $"启用{setting_name}成功";
                    callback.Foreground = new SolidColorBrush(Color.FromRgb(0, 204, 102));
                }
                else
                {
                    callback.Text = $"禁用{setting_name}成功";
                    callback.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 255));
                }
            }
            else if (newFileContent != null) //为null说明文件被删除，已经报过错了。此处仅为校验不通过的报错。
            {
                MessageBox.Show("很抱歉，向options.ini文件中写入数据失败！", "写入配置校验失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mody_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("EnableMods", true, l_mod, "模组");
        }

        private void modn_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("EnableMods", false, l_mod, "模组");
        }

        private void cony_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("EnableDebugConsole", true, l_con, "控制台");
        }

        private void conn_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("EnableDebugConsole", false, l_con, "控制台");
        }

        private void OpenBackupDirectory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("盗版存档一般在\"我的文档\"下的\"My Games\"目录下查找，但不同的盗版可能存档路径不同。\n点击\"确定\"继续查找，如未找到，请手动查找。\n\n注意：如果要把存档迁移到盗版游戏里，请不要试图覆盖此处的任何文件，而应该尝试使用“迁移到盗版”选项卡中的功能。", "准备查找"
                , MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start("explorer.exe", active_option.Substring(0, active_option.LastIndexOf("\\")));
        }


        private void OpenSteamVersionDirectory_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", null);
            if (path != null)
            {
                path += "\\userdata";
            }
            bool success = false;

            if (Directory.Exists(path))
            {
                SteamIDSelection selection = new SteamIDSelection(path, (id) => steamID = id);
                if (selection.NeedToShow)
                {
                    selection.Owner = this;
                    selection.ShowDialog();
                }
                success = selection.Result;
            }
            if (!success)
            {
                return;
            }
            path += $"\\{steamID}\\250900\\remote";
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path);
            }
            else
            {
                MessageBox.Show($"抱歉，在SteamID{steamID}下未找到以撒文件夹。\n试图寻找的路径为：\n{path}\n注意：如果您刚购买游戏/DLC还从未运行过，请至少运行一次。",
                    "未查找到目录", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void link_github_repo_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/QuincyZhang03/QuincyIsaacTool"));
        }

        private void link_tieba2_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://tieba.baidu.com/p/8485174405?pid=148619573223#148619573223"));
        }

        private void tab_root_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if (tabControl != null)
            {
                TabItem item = tabControl.SelectedItem as TabItem;
                if (item != null)
                {
                    if (item.Name == "save_replace")
                    {
                        Height = EXPANDED_HEIGHT;
                    }
                    else
                    {
                        Height = NORMAL_HEIGHT;
                    }
                }
            }
        }

        private void file_name_text_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Run run)
            {
                run.TextDecorations = TextDecorations.Underline;
                run.Foreground = Brushes.Red;
            }
        }

        private void file_name_text_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Run run)
            {
                run.TextDecorations = null;
                run.Foreground = Brushes.BlueViolet;
            }
        }

        private void file_name_text_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Clipboard.SetDataObject("rep_persistentgamedata1.dat");
            MessageBox.Show("已将\"rep_persistentgamedata1.dat\"复制到剪切板！\n如需要对应存档栏位2，将1改为2即可。", "复制成功"
                , MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void mousey_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("MouseControl", true, l_mouse, "鼠标");
        }

        private void mousen_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("MouseControl", false, l_mouse, "鼠标");
        }

        private void cloudy_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("SteamCloud", true, l_cloud, "云存档");
        }

        private void cloudn_Checked(object sender, RoutedEventArgs e)
        {
            ModifySetting("SteamCloud", false, l_cloud, "云存档");
        }

        private void link_demonstration_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.bilibili.com/video/BV1nQ4y1u7sM/"));
        }

        private void link_tieba_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://tieba.baidu.com/p/8485174405"));
        }

        private void link_demonstration_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Hyperlink hyperlink)
            {
                hyperlink.Foreground = Brushes.Red;
            }
        }

        private void link_demonstration_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Hyperlink hyperlink)
            {
                hyperlink.Foreground = Brushes.Blue;
            }
        }

        private void SearchLoadedHackerSave_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                LoadedHackerSavePos[] datalist = {
                    new LoadedHackerSavePos("CODEX", "C:\\Users\\Public\\Documents\\Steam\\CODEX\\250900\\remote"),
                    new LoadedHackerSavePos("Goldberg", Environment.GetEnvironmentVariable("USERPROFILE") + "\\AppData\\Roaming\\Goldberg SteamEmu Saves\\250900\\remote"),
                    new LoadedHackerSavePos("GSE", Environment.GetEnvironmentVariable("USERPROFILE") + "\\AppData\\Roaming\\GSE Saves\\250900\\remote")
                };
                list_LoadedHackerSave.ItemsSource = datalist;
                int num_found = 0;
                foreach (LoadedHackerSavePos poses in datalist)
                {
                    if (poses.Exists)
                    {
                        num_found++;
                    }
                }
                button.IsEnabled = false;
                if (num_found == 0)
                {
                    button.Content = "无结果";
                }
                else
                {
                    button.Content = $"找到{num_found}个";
                }
            }
        }

        private void VisitLoadedHackerSave_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
            {
                return;
            }
            LoadedHackerSavePos savepos = button.DataContext as LoadedHackerSavePos;
            if (savepos != null)
            {
                Process.Start("explorer.exe", savepos.Path);
            }
        }

        private void OtherSettingsLink_Click(object sender, RoutedEventArgs e)
        {
            tab_root.SelectedItem = other_settings;
        }

        private void CheckRep(bool updateReg = true)
        {
            active_option = CONFIG_PATH;
            OpenBackupDirectory.Content = "备份存档目录(忏悔)";
            if (updateReg && registry != null)
                registry.SetValue(REG_GAME_VER_KEY, 1);
        }
        private void CheckPlus(bool updateReg = true)
        {
            active_option = CONFIG_PATH_PLUS;
            OpenBackupDirectory.Content = "备份存档目录(忏悔+)";
            if (updateReg && registry != null)
                registry.SetValue(REG_GAME_VER_KEY, 2);
        }

        private void ver_rep_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                CheckRep();
                ReloadSettings();
            }
        }

        private void ver_plus_Checked(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                CheckPlus();
                ReloadSettings();
            }
        }

        private void link_download_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://quincyzhang.lanzoul.com/b04k53rkf"));
        }

        private void tab_update_Loaded(object sender, RoutedEventArgs e)
        {
            p_update_all_content.Visibility = Visibility.Hidden;
            new Thread(Update).Start();
        }
    }
    public class LoadedHackerSavePos
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Exists
        {
            get
            {
                return Directory.Exists(Path);
            }
        }
        public string Result
        {
            get
            {
                return Exists ? "找到存档" : "无";
            }
        }
        public LoadedHackerSavePos(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    class TimedWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = 5000; //获取更新等待超时5秒
            return request;
        }
    }
}
