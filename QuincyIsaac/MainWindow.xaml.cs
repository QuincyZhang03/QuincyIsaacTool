using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
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
            Top = (SystemParameters.WorkArea.Height - Height) / 2-100;

            if (!File.Exists(CONFIG_PATH))
            {
                MessageBox.Show($"对不起，程序未能在路径{CONFIG_PATH}游戏配置文件options.ini！\n" +
                "反馈BUG可以通过QQ1391070463或在百度贴吧@炎炎夏日Quincy 联系我！",
                "运行失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            original_content = GetConfigContent();
            string modconfig = original_content.Substring(original_content.IndexOf("EnableMods=") + "EnableMods=".Length, 1);
            if (modconfig == "1")
            {
                mody.IsChecked = true;
            }
            else if (modconfig == "0")
            {
                modn.IsChecked = true;
            }
            else
            {
                MessageBox.Show("对不起，配置文件异常！", "文件异常", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            string conconfig = original_content.Substring(original_content.IndexOf("EnableDebugConsole=") + "EnableDebugConsole=".Length, 1);
            if (conconfig == "1")
            {
                cony.IsChecked = true;
            }
            else if (conconfig == "0")
            {
                conn.IsChecked = true;
            }
            else
            {
                MessageBox.Show("对不起，配置文件异常！", "文件异常", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
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
        private void mody_Checked(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            string newcontent = ExecuteContentReplacement("EnableMods=\\d", "EnableMods=1");
            original_content = newcontent;

            if (original_content == GetConfigContent())
            {
                l1.Text = "启用模组成功";
                l1.Foreground = new SolidColorBrush(Color.FromRgb(0, 204, 102));
            }
            else
            {
                MessageBox.Show("很抱歉，向options.ini文件中写入数据失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void modn_Checked(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            string newcontent = ExecuteContentReplacement("EnableMods=\\d", "EnableMods=0");
            original_content = newcontent;

            if (original_content == GetConfigContent())
            {
                l1.Text = "禁用模组成功";
                l1.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 255));
            }
            else
            {
                MessageBox.Show("很抱歉，向options.ini文件中写入数据失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cony_Checked(object sender, RoutedEventArgs e)
        {
            if (!initialized) return;
            string newcontent = ExecuteContentReplacement("EnableDebugConsole=\\d", "EnableDebugConsole=1");
            original_content = newcontent;

            if (original_content == GetConfigContent())
            {
                l2.Text = "启用控制台成功";
                l2.Foreground = new SolidColorBrush(Color.FromRgb(0, 204, 102));
            }
            else
            {
                MessageBox.Show("很抱歉，向options.ini文件中写入数据失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void conn_Checked(object sender, RoutedEventArgs e)
        {

            if (!initialized) return;
            string newcontent = ExecuteContentReplacement("EnableDebugConsole=\\d", "EnableDebugConsole=0");
            original_content = newcontent;

            if (original_content == GetConfigContent())
            {
                l2.Text = "禁用控制台成功";
                l2.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 255));
            }
            else
            {
                MessageBox.Show("很抱歉，向options.ini文件中写入数据失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyNewFileName_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("rep_persistentgamedata1.dat");
            MessageBox.Show("已将\"rep_persistentgamedata1.dat\"复制到剪切板！\n如需要对应存档栏位2，将1改为2即可。", "复制成功"
                , MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}
