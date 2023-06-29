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

        public bool Result {
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
                SteamIDComboBox.Items.Add(dir.Substring(dir.LastIndexOf("\\") + 1));
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
            }catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
