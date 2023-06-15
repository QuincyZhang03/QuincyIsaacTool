using System;
using System.IO;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuincyIsaac
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string CONFIG_PATH = Environment.GetEnvironmentVariable("USERPROFILE")
            + "\\Documents\\My Games\\Binding of Isaac Repentance\\options.ini";
        string original_content;
        bool initialized = false;
        public MainWindow()
        {
            InitializeComponent();
            if (!File.Exists(CONFIG_PATH))
            {
                MessageBox.Show($"对不起，程序未能在路径{CONFIG_PATH}游戏配置文件options.ini！\n" +
                "可以通过QQ1391070463或在百度贴吧@炎炎夏日Quincy 联系我！",
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
    }
}
