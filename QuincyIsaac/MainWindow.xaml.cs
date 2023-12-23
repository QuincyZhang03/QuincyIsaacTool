using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace QuincyIsaac
{
    public partial class MainWindow : Window
    {
        string CONFIG_PATH = Environment.GetEnvironmentVariable("USERPROFILE")
            + "\\Documents\\My Games\\Binding of Isaac Repentance\\options.ini";
        string original_content;
        bool initialized = false;
        string steamID;
        public MainWindow()
        {
            InitializeComponent();
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = (SystemParameters.WorkArea.Height - Height) / 2 - 100;

            if (!File.Exists(CONFIG_PATH))
            {
                MessageBox.Show($"对不起，程序未能在路径{CONFIG_PATH}游戏配置文件options.ini！\n" +
                "反馈BUG可以通过QQ1391070463或在百度贴吧@炎炎夏日Quincy 联系我！",
                "运行失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            if (IsIsaacLaunched())
            {
                MessageBox.Show("检测到以撒的结合正在运行，为防止配置文件出错，请先关闭游戏后再使用本程序。", "游戏正在运行", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            original_content = GetConfigContent();

            LoadSetting("EnableMods", mody, modn);
            LoadSetting("EnableDebugConsole", cony, conn);
            LoadSetting("MouseControl", mousey, mousen);
            LoadSetting("SteamCloud", cloudy, cloudn);

            initialized = true;
        }

        private string GetConfigContent()
        {
            StreamReader reader = new StreamReader(CONFIG_PATH);
            string config = reader.ReadToEnd();
            reader.Close();
            return config;
        }
        private string ExecuteContentReplacement(string original_key, string replacement)
        {
            string newcontent = Regex.Replace(original_content, original_key, replacement);
            StreamWriter writer = new StreamWriter(CONFIG_PATH, false);
            writer.Write(newcontent);
            writer.Close();
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
            string newcontent = ExecuteContentReplacement($"{key}=\\d", $"{key}={value}");
            original_content = newcontent;

            if (original_content == GetConfigContent())
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
            else
            {
                MessageBox.Show("很抱歉，向options.ini文件中写入数据失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void OpenHackerDirectory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("盗版存档一般在\"我的文档\"下的\"My Games\"目录下查找，但不同的盗版可能存档路径不同。\n点击\"确定\"继续查找，如未找到，请手动查找。", "准备查找"
                , MessageBoxButton.OK, MessageBoxImage.Information);
            System.Diagnostics.Process.Start("explorer.exe", CONFIG_PATH.Substring(0, CONFIG_PATH.LastIndexOf("\\")));
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
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            else
            {
                MessageBox.Show($"抱歉，在SteamID{steamID}下未找到以撒文件夹。\n试图寻找的路径为：\n{path}",
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

        private void tab_root_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if (tabControl != null)
            {
                TabItem item = tabControl.SelectedItem as TabItem;
                if (item != null)
                {
                    if (item.Name == "save_replace")
                    {
                        Height = 520;
                    }
                    else
                    {
                        Height = 150;
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
            Clipboard.SetText("rep_persistentgamedata1.dat");
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
    }
}
