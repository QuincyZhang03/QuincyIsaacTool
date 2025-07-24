using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace QuincyIsaac.SaveTransferModule
{
    public partial class SaveManagement : Window
    {
        public readonly static string steamDir = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", null);
        public readonly static string myGamesDir = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Documents\\My Games";

        private readonly HackerSavePos[] dataList = new HackerSavePos[]
                {
                    new HackerSavePos( "C:\\Users\\Public\\Documents\\Steam\\CODEX\\250900\\remote"),
                    new HackerSavePos(Environment.GetEnvironmentVariable("USERPROFILE") + "\\AppData\\Roaming\\Goldberg SteamEmu Saves\\250900\\remote"),
                    new HackerSavePos(Environment.GetEnvironmentVariable("USERPROFILE") + "\\AppData\\Roaming\\GSE Saves\\250900\\remote"),
                    new HackerSavePos(Environment.GetEnvironmentVariable("USERPROFILE") + "\\AppData\\Roaming\\GSE Saves\\0\\remote"),
                    new HackerSavePos("C:\\Users\\Public\\Documents\\Steam\\RUNE\\250900\\remote")
                };
        private readonly string[] backupDirNames = new string[] {
            "Binding of Isaac Rebirth",
            "Binding of Isaac Afterbirth",
            "Binding of Isaac Afterbirth+",
            "Binding of Isaac Repentance",
            "Binding of Isaac Repentance\\save_backups",
            "Binding of Isaac Repentance+",
            "Binding of Isaac Repentance+\\save_backups"
        };
        private readonly ObservableCollection<HackerSavePos> foundHackerSaveList = new ObservableCollection<HackerSavePos>();
        private readonly ObservableCollection<SteamSavePos> foundSteamSaveList = new ObservableCollection<SteamSavePos>();

        private static bool released = false; //鼠标松开后给予released一个true脉冲，把窗口关掉

        private void InitContextAndSource() //绑定内部数据源和列表项数据源
        {
            List_HackerSave.DataContext = GeneralUIController.Hacker;
            List_Rebirth.DataContext = GeneralUIController.Rebirth;
            List_AB.DataContext = GeneralUIController.AB;
            List_ABP.DataContext = GeneralUIController.ABP;
            List_Rep.DataContext = GeneralUIController.Rep;
            List_RepPlus.DataContext = GeneralUIController.RepPlus;
            List_GameBackup.DataContext = GeneralUIController.GameBackup;

            Image_Trashbin.DataContext = GeneralUIController.Trashbin;

            ComboBox_HackerPath.ItemsSource = foundHackerSaveList;
            ComboBox_SteamID.ItemsSource = foundSteamSaveList;

            List_HackerSave.ItemsSource = ListItemController.hackerSaves;
            List_Rebirth.ItemsSource = ListItemController.steamRebirth;
            List_AB.ItemsSource = ListItemController.steamAb;
            List_ABP.ItemsSource = ListItemController.steamAbp;
            List_Rep.ItemsSource = ListItemController.steamRep;
            List_RepPlus.ItemsSource = ListItemController.steamRepplus;
            List_GameBackup.ItemsSource = ListItemController.gameBackup;
        }

        public SaveManagement()
        {
            InitializeComponent();
            Left = SystemParameters.PrimaryScreenWidth * GetDpiFactorX() / 2 - Width / 2;
            Top = SystemParameters.PrimaryScreenHeight * GetDpiFactorY() / 2 - Height / 2;
            DataContext = ProgramVersion.version;
            InitContextAndSource();
            InitHackerPath();
            InitSteamPath();
            InitGameBackup();
        }

        private void InitHackerPath()
        {
            foreach (HackerSavePos pos in dataList)
            {
                if (pos.Exists)
                {
                    foundHackerSaveList.Add(pos);
                }
            }
            if (foundHackerSaveList.Count == 0) //没有盗版存档
            {
                TextBlock failText = new TextBlock();
                failText.Text = "盗版存档目录（未查找到结果）";
                failText.FontSize = 15;
                failText.FontWeight = FontWeights.Bold;
                Group_HackerSave.Header = failText;
                ComboBox_HackerPath.IsEnabled = false;
            }
            else//找到盗版存档
            {
                HackerSavePos latest = null;
                foreach (HackerSavePos pos in foundHackerSaveList)//默认选中最晚修改的存档路径
                {
                    if (latest == null || pos.ModifiedTime > latest.ModifiedTime)
                    {
                        if (latest != null) latest.Latest = false;
                        pos.Latest = true;
                        latest = pos;
                    }
                }
                CollectionViewSource.GetDefaultView(ComboBox_HackerPath.ItemsSource).SortDescriptions.Add(new SortDescription("Latest", ListSortDirection.Descending));
                ComboBox_HackerPath.SelectedItem = latest;
                ReadHackerSave(latest.Path);
            }
        }

        private void InitSteamPath()
        {
            Regex steamIDRegex = new Regex(@"^\d*$");
            if (steamDir != null && Directory.Exists(steamDir + "\\userdata"))
            {
                string[] users = Directory.GetDirectories(steamDir + "\\userdata");
                foreach (string user in users)
                {
                    string id = user.Substring(user.LastIndexOf('\\') + 1);
                    if (steamIDRegex.IsMatch(id))
                    { //合法的Steam好友码
                        foundSteamSaveList.Add(new SteamSavePos(id));
                    }
                }
            }
            if (foundSteamSaveList.Count > 0)
            {
                CollectionViewSource.GetDefaultView(ComboBox_SteamID.ItemsSource).SortDescriptions.Add(new SortDescription("IsaacFound", ListSortDirection.Descending));
                CollectionViewSource.GetDefaultView(ComboBox_SteamID.ItemsSource).SortDescriptions.Add(new SortDescription("SteamID", ListSortDirection.Ascending));
                ComboBox_SteamID.SelectedIndex = 0;

                //获取Steam用户信息成功，开始读取存档
                ReadSteamSave((SteamSavePos)ComboBox_SteamID.SelectedItem);
            }
            else
            {
                TextBlock failText = new TextBlock();
                failText.Text = "Steam正版存档目录（未查找到结果）";
                failText.FontSize = 15;
                failText.FontWeight = FontWeights.Bold;
                Group_SteamSave.Header = failText;
                ComboBox_SteamID.IsEnabled = false;
            }
        }

        private void InitGameBackup()
        {
            ListItemController.gameBackup.Clear();
            List<IsaacSave> saves = new List<IsaacSave>();
            DateTime latest = DateTime.MinValue; //最新备份的时间
            if (Directory.Exists(myGamesDir))
            {
                Regex backupRegex = new Regex(@"^20\d{2}(0[1-9]|1[0-2])(0[1-9]|[1-2]\d|30|31)(\.(ab_|abp_|rep_|rep\+)?persistentgamedata[1-3]\.dat)?$");
                foreach (string dirName in backupDirNames)
                {
                    string path = myGamesDir + "\\" + dirName;
                    if (Directory.Exists(path))
                    {
                        string[] files = Directory.GetFiles(path);
                        foreach (string file in files)
                        {
                            if (backupRegex.IsMatch(Path.GetFileName(file))) //合法存档文件名，尝试解析存档
                            {
                                SaveSlot parsedSave = SaveSlot.ParseSave(file);
                                if (parsedSave is IsaacSave save)
                                {
                                    if (save.ModifiedTime > latest) latest = save.ModifiedTime;
                                    saves.Add((IsaacSave)save.SetDisplayName($"{save.ModifiedTime.ToString("yy/MM/dd")}").SetSlotType(SaveSlot.Type.GameBackup));
                                }
                            }
                        }
                    }
                }
            }
            if (saves.Count == 0)
            {//My Games目录不存在或未找到有效存档
                TextBlock failText = new TextBlock();
                failText.Text = "游戏自动备份（My Games）（未查找到结果）";
                failText.FontSize = 15;
                failText.FontWeight = FontWeights.Bold;
                Group_GameBackup.Header = failText;
            }
            else
            {//找出距离最新备份不足15天的所有存档加入列表
                foreach (IsaacSave save in saves)
                {
                    if (latest.Subtract(save.ModifiedTime).CompareTo(new TimeSpan(15, 0, 0, 0)) <= 0)
                    {
                        ListItemController.gameBackup.Add(save);
                    }
                }
                CollectionViewSource.GetDefaultView(List_GameBackup.ItemsSource).SortDescriptions.Add(new SortDescription("ModifiedTime_Date", ListSortDirection.Descending));
                CollectionViewSource.GetDefaultView(List_GameBackup.ItemsSource).SortDescriptions.Add(new SortDescription("SaveVersion", ListSortDirection.Descending));
                CollectionViewSource.GetDefaultView(List_GameBackup.ItemsSource).SortDescriptions.Add(new SortDescription("FullPath", ListSortDirection.Ascending));
            }
            ListInteractableController.NotifyUI();
        }

        private void ReadHackerSave(string path)
        {
            ListItemController.hackerSaves.Clear();
            if (path != null && Directory.Exists(path))
            {
                IsaacSaveSet hackerSaveSet = IsaacSaveSet.ReadSaveSet(path);
                if (hackerSaveSet != null)
                {
                    ListItemController.hackerSaves.Add(hackerSaveSet.Save1.SetSlotType(SaveSlot.Type.Hacker));
                    ListItemController.hackerSaves.Add(hackerSaveSet.Save2.SetSlotType(SaveSlot.Type.Hacker));
                    ListItemController.hackerSaves.Add(hackerSaveSet.Save3.SetSlotType(SaveSlot.Type.Hacker));
                }
            }
            ListInteractableController.NotifyUI();
        }

        private void ReadSteamSave(SteamSavePos steamUser)
        {
            ListItemController.steamRebirth.Clear();
            ListItemController.steamAb.Clear();
            ListItemController.steamAbp.Clear();
            ListItemController.steamRep.Clear();
            ListItemController.steamRepplus.Clear();

            foreach (SteamSavePos steamuser in foundSteamSaveList)
            {
                steamuser.RefreshSteamPos(); //更新是否灰色显示
            }

            if (steamUser != null)
            {

                IsaacSaveSet rebirth = IsaacSaveSet.ReadSaveSet(steamUser.Path, SaveSlot.DLCVersion.Rebirth);
                IsaacSaveSet ab = IsaacSaveSet.ReadSaveSet(steamUser.Path, SaveSlot.DLCVersion.AB);
                IsaacSaveSet abp = IsaacSaveSet.ReadSaveSet(steamUser.Path, SaveSlot.DLCVersion.ABP);
                IsaacSaveSet rep = IsaacSaveSet.ReadSaveSet(steamUser.Path, SaveSlot.DLCVersion.Rep);
                IsaacSaveSet repplus = IsaacSaveSet.ReadSaveSet(steamUser.Path, SaveSlot.DLCVersion.RepPlus);

                ListItemController.steamRebirth.Add(rebirth.Save1.SetSlotType(SaveSlot.Type.Steam_Rebirth));
                ListItemController.steamRebirth.Add(rebirth.Save2.SetSlotType(SaveSlot.Type.Steam_Rebirth));
                ListItemController.steamRebirth.Add(rebirth.Save3.SetSlotType(SaveSlot.Type.Steam_Rebirth));
                ListItemController.steamAb.Add(ab.Save1.SetSlotType(SaveSlot.Type.Steam_AB));
                ListItemController.steamAb.Add(ab.Save2.SetSlotType(SaveSlot.Type.Steam_AB));
                ListItemController.steamAb.Add(ab.Save3.SetSlotType(SaveSlot.Type.Steam_AB));
                ListItemController.steamAbp.Add(abp.Save1.SetSlotType(SaveSlot.Type.Steam_ABP));
                ListItemController.steamAbp.Add(abp.Save2.SetSlotType(SaveSlot.Type.Steam_ABP));
                ListItemController.steamAbp.Add(abp.Save3.SetSlotType(SaveSlot.Type.Steam_ABP));
                ListItemController.steamRep.Add(rep.Save1.SetSlotType(SaveSlot.Type.Steam_Rep));
                ListItemController.steamRep.Add(rep.Save2.SetSlotType(SaveSlot.Type.Steam_Rep));
                ListItemController.steamRep.Add(rep.Save3.SetSlotType(SaveSlot.Type.Steam_Rep));
                ListItemController.steamRepplus.Add(repplus.Save1.SetSlotType(SaveSlot.Type.Steam_Repplus));
                ListItemController.steamRepplus.Add(repplus.Save2.SetSlotType(SaveSlot.Type.Steam_Repplus));
                ListItemController.steamRepplus.Add(repplus.Save3.SetSlotType(SaveSlot.Type.Steam_Repplus));
            }
            ListInteractableController.NotifyUI();
        }

        private void Button_OpenHackerPath_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", ((HackerSavePos)ComboBox_HackerPath.SelectedItem).Path);
        }

        private void ComboBox_HackerPath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReadHackerSave(((HackerSavePos)ComboBox_HackerPath.SelectedItem).Path);
        }

        private void Button_OpenSteamPath_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", ((SteamSavePos)ComboBox_SteamID.SelectedItem).Path);
        }

        private void ComboBox_SteamID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SteamSavePos pos = (SteamSavePos)ComboBox_SteamID.SelectedItem;
            if (!pos.IsaacFound)
            {
                MessageBox.Show(this, $"在Steam用户 {pos.SteamID} 的数据中没有找到以撒数据。\n" +
                    $"如果您刚购买游戏，建议先运行一次游戏后再使用本程序。\n" +
                    $"如果您是老玩家，可以继续使用本工具进行存档迁移，程序会自动创建不存在的文件夹。", "未找到数据", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            ReadSteamSave(pos);
        }

        private void Button_OpenGameBackup_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", myGamesDir);
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ListBox)sender).SelectedItem = null;
        }

        private T FindUIChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) //遍历视觉树找指定类型的UI元素
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                if (VisualTreeHelper.GetChildrenCount(child) > 0)
                {
                    DependencyObject resultInChild = FindUIChild<T>(child);
                    if (resultInChild != null) return (T)resultInChild;
                }
            }
            return null;
        }

        private double GetDpiFactorX() //逻辑位置乘以dpi系数为实际位置
        {
            PresentationSource screen = PresentationSource.FromVisual(this);
            if (screen != null)
            {
                return screen.CompositionTarget.TransformFromDevice.M11; //矩阵主对角线上分别是X倍率和Y倍率
            }
            return 1;
        }
        private double GetDpiFactorY()
        {
            PresentationSource screen = PresentationSource.FromVisual(this);
            if (screen != null)
            {
                return screen.CompositionTarget.TransformFromDevice.M22; //矩阵主对角线上分别是X倍率和Y倍率
            }
            return 1;
        }

        private void OnSaveSelected(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            SaveSlot selected = (SaveSlot)border.DataContext;
            if (selected is IsaacSave save)
            {
                ListInteractableController.DraggedSave = save;
                released = false;

                //开始进行拖拽动作
                double dpiFactorX = GetDpiFactorX();
                double dpiFactorY = GetDpiFactorY();


                Image image = FindUIChild<Image>(border);

                if (image != null)
                {
                    System.Drawing.Point mousePos = System.Windows.Forms.Cursor.Position;
                    Window window = new Window
                    {
                        Content = new Image
                        {
                            Source = image.Source,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            IsHitTestVisible = false
                        },
                        Width = image.ActualWidth,
                        Height = image.ActualHeight,
                        Opacity = 0.6,
                        Background = null,
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = true,
                        Left = (mousePos.X - image.ActualWidth / 2) * dpiFactorX,
                        Top = (mousePos.Y - image.ActualHeight * 0.6) * dpiFactorY,
                        IsHitTestVisible = false,  //禁止碰撞检测
                        Topmost = true,
                        ShowInTaskbar = false
                    };
                    window.Show();
                    Win32Helper.SetWindowClickable(window);
                    DispatcherTimer timer = new DispatcherTimer //定时器在DragDrop时不会阻塞，用定时器实现跟踪
                    {
                        Interval = new TimeSpan(0, 0, 0, 0, 20)
                    };
                    timer.Tick += (sender1, e1) =>
                    {
                        if (released && window.IsVisible)
                        {
                            window.Close();
                            released = false;
                        }
                        else
                        {
                            mousePos = System.Windows.Forms.Cursor.Position;
                            window.Left = (mousePos.X - image.ActualWidth / 2) * dpiFactorX;
                            window.Top = (mousePos.Y - image.ActualHeight * 0.6) * dpiFactorY;
                        }
                    };
                    timer.Start();
                    DataObject data = new DataObject(DataFormats.FileDrop, new string[] { save.FullPath }); //复制文件的data需要文件路径的数组
                    DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
                    released = false; //防止下一次悬浮窗被秒关
                    timer.Stop();
                    if (window.IsVisible) window.Close();
                    ListInteractableController.DraggedSave = null;
                }
            }
            else
            {
                ListInteractableController.DraggedSave = null;
            }
        }

        private void OnSaveDrop(object sender, DragEventArgs e)
        {
            released = true; //在弹出对话框前关闭悬浮窗
            DataObject data = e.Data as DataObject;
            StringCollection fileList = data?.GetFileDropList();
            if (fileList == null || fileList.Count == 0) return;
            if (fileList.Count > 1)
            {
                MessageBox.Show(this, "为确保您的存档栏位对应正确，请逐个迁移存档。", "存档过多", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string sourceFile = fileList[0]; //源文件路径

            if (sourceFile == null || !File.Exists(sourceFile)) return;
            FileInfo fileInfo = new FileInfo(sourceFile);
            if (fileInfo.Extension != "" && fileInfo.Extension != ".dat")
            {
                MessageBox.Show(this, "这不是以撒存档，以撒存档的扩展名是.dat。\n路径：" + sourceFile, "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            double size = (int)(fileInfo.Length / 102.4) / 10.0;
            if (size < 1 || size > 25)
            {
                MessageBox.Show(this, "该文件大小不符合以撒存档规格。\n路径：" + sourceFile, "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if ((SaveSlot)((Border)sender).DataContext is SaveSlot slot) //slot是被放入的存档位置
            {
                if (slot.FullPath == sourceFile) return; //不允许迁移到自己身上

                StringBuilder routeBuilder = new StringBuilder();
                routeBuilder.Append("您正在进行的迁移路线是：");

                TransferVerifier verifier;
                SaveSlot.DLCVersion sourceVersion = SaveSlot.PathToVersion(sourceFile);
                if (ListInteractableController.DraggedSave != null) //来自程序内
                {
                    IsaacSave sourceSave = ListInteractableController.DraggedSave;
                    verifier = new TransferVerifier(sourceSave, slot);
                    routeBuilder.Append(sourceSave.DisplaySlotType + sourceSave.DisplayVersion);
                }
                else //来自程序外，Version一般是Unknown
                {
                    verifier = new TransferVerifier(sourceVersion, slot);
                    routeBuilder.Append("外部" + SaveSlot.VersionToDisplayVersion(sourceVersion));
                }
                routeBuilder.Append(" ——> ").Append(slot.DisplaySlotType + slot.DisplayVersion).Append("\n");

                string route = routeBuilder.ToString();
                if (verifier.Allowed) //允许进行
                {
                    if (verifier.Message != null) //允许但有信息，需要确认
                    {
                        MessageBoxResult result = MessageBox.Show(this, route + verifier.Message, "存档版本确认", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                        if (result == MessageBoxResult.Cancel) return;
                    }
                    bool doTransfer = true;
                    if (slot is IsaacSave) //已有存档，覆盖确认
                    {
                        MessageBoxResult result = MessageBox.Show(this, "您确定要覆盖存档吗？该操作不可撤销！\n" +
                            "建议您先将已有存档拖拽到桌面或其他文件夹进行备份哦~\n\n" +
                            "确定：覆盖已有存档\n" +
                            "取消：不做任何处理", "覆盖确认", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                        if (result == MessageBoxResult.Cancel)
                        {
                            doTransfer = false;
                        }
                    }
                    try //执行
                    {
                        if (doTransfer)
                        {
                            Directory.CreateDirectory(slot.FullPath.Substring(0, slot.FullPath.LastIndexOf('\\')));
                            File.Copy(sourceFile, slot.FullPath, true);
                            if (slot.SlotType >= SaveSlot.Type.Steam_Rebirth && slot.SlotType <= SaveSlot.Type.Steam_Repplus)
                                ((SteamSavePos)ComboBox_SteamID.SelectedItem).RefreshSteamPos();

                            //帮用户开启Steam云存档
                            if (OpenCloudSave())
                            {
                                MessageBox.Show(this, "迁移完成。检测到您的\"Steam云存档\"设置未全部开启，已自动帮您开启。", "云存档提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "对不起，程序发生错误。\n请联系我进行反馈，我会进行修复，谢谢！\n错误信息：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        if (GeneralUIController.Hacker.IsPartActive)
                            ReadHackerSave(((HackerSavePos)ComboBox_HackerPath.SelectedItem).Path);
                        if (GeneralUIController.Rebirth.IsPartActive)
                            ReadSteamSave((SteamSavePos)ComboBox_SteamID.SelectedItem);
                    }
                }
                else //不允许进行的路径
                {
                    if (verifier.MultipleStep)
                    {
                        MessageBox.Show(this, route + "该迁移过程需要一个或多个步骤才能完成，请依次进行以下步骤：\n\n" + verifier.Message, "迁移步骤");
                    }
                    else
                    {
                        MessageBox.Show(this, route + "该迁移过程无法完成，原因为：\n" + verifier.Message, "无法迁移");
                    }
                }
            }
        }

        private bool OpenCloudSave()
        {
            bool modified = false;
            string[] configPaths = { MainWindow.CONFIG_PATH, MainWindow.CONFIG_PATH_PLUS };
            foreach (string path in configPaths)
            {
                if (File.Exists(path))
                {
                    string config = MainWindow.GetConfigContent(path);
                    string newconfig = config.Replace("SteamCloud=0", "SteamCloud=1");
                    MainWindow.WriteToFile(path, newconfig);
                    if (config != newconfig) modified = true;
                }
            }
            return modified;
        }

        private void Button_ReloadHackerPath_Click(object sender, RoutedEventArgs e)
        {
            ReadHackerSave(((HackerSavePos)ComboBox_HackerPath.SelectedItem).Path);
        }

        private void Button_ReloadSteamPath_Click(object sender, RoutedEventArgs e)
        {
            ReadSteamSave((SteamSavePos)ComboBox_SteamID.SelectedItem);
        }

        private void Button_ReloadGameBackup_Click(object sender, RoutedEventArgs e)
        {
            InitGameBackup();
        }

        private void Image_Trashbin_Drop(object sender, DragEventArgs e)
        {
            released = true; //在弹出对话框前关闭悬浮窗
            DataObject data = e.Data as DataObject;
            if (data != null && ListInteractableController.DraggedSave != null)
            {
                //本程序内拖拽进来的才算数
                IsaacSave save = ListInteractableController.DraggedSave;
                string path = save.FullPath;
                MessageBoxResult result = MessageBox.Show(this, $"您确定要删除  {save.DisplaySlotType}{save.DisplayVersion} {save.DisplayName}  吗？\n" +
                    "建议在删除前将存档拖拽至桌面或其他文件夹进行备份，以便找回。\n" +
                    "警告：该删除不会经过回收站，一旦删除将无法恢复！\n\n" +
                    "确定：删除该存档\n" +
                    "取消：不进行任何操作", "删除确认", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                if (result == MessageBoxResult.OK)
                {
                    File.Delete(path);
                    if (GeneralUIController.Hacker.IsPartActive)
                        ReadHackerSave(((HackerSavePos)ComboBox_HackerPath.SelectedItem).Path);
                    if (GeneralUIController.Rebirth.IsPartActive)
                        ReadSteamSave((SteamSavePos)ComboBox_SteamID.SelectedItem);
                    if (GeneralUIController.GameBackup.IsPartActive)
                        InitGameBackup();
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            new MainWindow().Show();
        }

        private void Link_Demo_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.bilibili.com/video/BV1nV8AzKEJh/"));
        }
    }

    public class ListInteractableController : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static event Action ProperyChangedBroadcast;

        private static IsaacSave _draggedSave = null; //UI中当前选中的存档
        public static IsaacSave DraggedSave
        {
            get => _draggedSave;
            set
            {
                _draggedSave = value;
                NotifyUI();
                GeneralUIController.Trashbin.UpdateTrashbinOpacity();
            }
        }

        public bool IsPartActive { get => _relatedList.Count > 0; }
        private readonly bool _allowDropTo; //路径决定的是否允许在此处放置存档

        public bool CanInteract
        {
            get
            {
                if (_draggedSave == null) return IsPartActive; //未选中任何存档
                //选中存档，则判断被选中存档的版本是否为允许的存档版本
                return (_allowDropTo && IsPartActive && ((int)DLCLevel & SaveSlot.GetAllowedDestinations(_draggedSave.SaveVersion)) != 0);
            }
        }
        public SaveSlot.DLCVersion DLCLevel //列表框对应的DLC版本
        {
            get
            {
                if (_relatedList != null && _relatedList.Count > 0)
                {
                    return _relatedList[0].SaveVersion;
                }
                return SaveSlot.DLCVersion.Unknown;
            }
        }
        private readonly ObservableCollection<SaveSlot> _relatedList;

        public ListInteractableController(object relatedSaveList, bool allowDropTo = true)
        {
            ProperyChangedBroadcast += () => //PropertyChanged不是静态事件，这里额外创建了一个静态事件，每个实例默认订阅这个事件，在这个事件里调用各个实例的PropertyChanged。
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPartActive)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanInteract)));
            };
            if (relatedSaveList is ObservableCollection<SaveSlot> saves)
            {
                _relatedList = saves;
            }
            _allowDropTo = allowDropTo;
        }

        public static void NotifyUI()
        {
            ProperyChangedBroadcast?.Invoke();
        }
    }
    public class TrashbinController : INotifyPropertyChanged
    {
        public double TrashbinOpacity
        {
            get => ListInteractableController.DraggedSave == null ? 0.3 : 1;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void UpdateTrashbinOpacity()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrashbinOpacity)));
        }
    }
    public class GeneralUIController
    {
        public static ListInteractableController Hacker = new ListInteractableController(ListItemController.hackerSaves);
        public static ListInteractableController Rebirth = new ListInteractableController(ListItemController.steamRebirth);
        public static ListInteractableController AB = new ListInteractableController(ListItemController.steamAb);
        public static ListInteractableController ABP = new ListInteractableController(ListItemController.steamAbp);
        public static ListInteractableController Rep = new ListInteractableController(ListItemController.steamRep);
        public static ListInteractableController RepPlus = new ListInteractableController(ListItemController.steamRepplus);
        public static ListInteractableController GameBackup = new ListInteractableController(ListItemController.gameBackup, false);
        public static TrashbinController Trashbin = new TrashbinController();
    }
    public class ListItemController //控制各个列表中显示的内容
    {
        public static ObservableCollection<SaveSlot> hackerSaves = new ObservableCollection<SaveSlot>();
        public static ObservableCollection<SaveSlot> steamRebirth = new ObservableCollection<SaveSlot>();
        public static ObservableCollection<SaveSlot> steamAb = new ObservableCollection<SaveSlot>();
        public static ObservableCollection<SaveSlot> steamAbp = new ObservableCollection<SaveSlot>();
        public static ObservableCollection<SaveSlot> steamRep = new ObservableCollection<SaveSlot>();
        public static ObservableCollection<SaveSlot> steamRepplus = new ObservableCollection<SaveSlot>();
        public static ObservableCollection<SaveSlot> gameBackup = new ObservableCollection<SaveSlot>();
    }
}