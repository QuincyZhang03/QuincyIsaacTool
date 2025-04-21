using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace QuincyIsaac
{
    public partial class SteamIDSelection : Window
    {
        public delegate void ReturnSteamID(string id);
        ReturnSteamID returnSteamID;
        bool result = false;
        bool needToShow = false;

        public bool Result
        {
            get
            {
                return result;
            }
        }
        public bool NeedToShow
        {
            get
            {
                return needToShow;
            }
        }
        public SteamIDSelection()
        {
            InitializeComponent();
        }
        public SteamIDSelection(string path, ReturnSteamID receiver) : this()
        {
            returnSteamID += receiver;
            IEnumerable<string> dirs = Directory.EnumerateDirectories(path);
            if (dirs.Count() == 0)
            {
                MessageBox.Show("很抱歉，Steam userdata目录为空！");
                return;
            }
            foreach (string dir in dirs)
            {
                string e_id = dir.Substring(dir.LastIndexOf("\\") + 1);
                bool isValidID = true;
                foreach (char c in e_id)
                {
                    if (c < '0' || c > '9')
                    {
                        isValidID = false;
                        break;
                    }
                }
                if (isValidID)
                {
                    SteamIDComboBox.Items.Add(e_id);
                }
            }
            if (SteamIDComboBox.Items.Count == 1)
            {
                IDConfirm_Click(this, new RoutedEventArgs());
            }
            else
            {
                needToShow = true;
            }
        }

        private void IDConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                returnSteamID((string)SteamIDComboBox.SelectedItem);
                result = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void HowToFindID_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("点击Steam主界面左上角的Steam→设置，在打开的界面就可以看到\"好友代码\"一栏。", "如何查看好友码");
        }
    }
}
