using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static QuincyIsaac.SaveTransferModule.SaveSlot;

namespace QuincyIsaac.SaveTransferModule
{
    public class HackerSavePos
    {
        public string Path { get; set; }
        public bool Latest { get; set; }
        public DateTime ModifiedTime
        {
            get
            {
                DateTime modifiedTime = DateTime.MinValue;
                if (Exists)
                {
                    string[] files = Directory.GetFiles(Path, "*.dat");
                    foreach (string file in files)
                    {
                        if (!IsValidSaveFileName(file)) continue;
                        DateTime fileModifiedTime = File.GetLastWriteTime(file);
                        if (fileModifiedTime > modifiedTime)
                        {
                            modifiedTime = fileModifiedTime;
                        }
                    }//找到目录中最晚的修改时间
                }
                return modifiedTime;
            }
        }
        public string Display
        {
            get
            {
                if (ModifiedTime == DateTime.MinValue) return $"(空) {Path}";
                return $"({ModifiedTime.ToString("yyyy/MM/dd")}) {Path}";
            }
        }
        public bool Exists
        {
            get
            {
                return Directory.Exists(Path);
            }
        }
        public HackerSavePos(string path)
        {
            Path = path;
        }
    }
    public class SteamSavePos : INotifyPropertyChanged
    {
        public string SteamID { get; }
        public string Path
        {
            get => $@"{SaveManagement.steamDir}\userdata\{SteamID}\250900\remote";
        }
        public bool IsaacFound
        {
            get => Directory.Exists(Path);
        }
        public SteamSavePos(string steamID)
        {
            SteamID = steamID;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RefreshSteamPos() //一旦有存档了，取消灰色显示
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsaacFound)));
        }
    }
    public abstract class SaveSlot //一个存档位置，可能是有效存档，也可能是空位
    {
        public readonly static Regex steamSaveNameRegex = new Regex(@"(ab_|abp_|rep_|rep\+)?persistentgamedata[1-3]\.dat");
        public readonly static Regex dateOnlyRegex = new Regex(@"^20\d{2}(0[1-9]|1[0-2])(0[1-9]|[1-2]\d|30|31)$");
        public readonly static Regex backupNameRegex = new Regex(@"^20\d{2}(0[1-9]|1[0-2])(0[1-9]|[1-2]\d|30|31)(\.(ab_|abp_|rep_|rep\+)?persistentgamedata[1-3]\.dat)?$");

        public enum DLCVersion
        {
            Unknown = 1 << 0, Rebirth = 1 << 1, AB = 1 << 2, ABP = 1 << 3, Rep = 1 << 4, RepPlus = 1 << 5
        }
        public enum Type
        {
            Hacker, Steam_Rebirth, Steam_AB, Steam_ABP, Steam_Rep, Steam_Repplus, GameBackup
        }

        public DLCVersion SaveVersion { get; protected set; }
        public Type SlotType { get; protected set; }
        public string DisplaySlotType
        {
            get
            {
                switch (SlotType)
                {
                    case Type.Hacker: return "盗版";
                    case Type.GameBackup:
                        return "备份";
                    default: return "";
                }
            }
        }

        public string DisplayVersion
        {
            get => VersionToDisplayVersion(SaveVersion);
        }
        public string Prefix
        {
            get
            {
                switch (SaveVersion)
                {
                    case DLCVersion.Rebirth: return "";
                    case DLCVersion.AB: return "ab_";
                    case DLCVersion.ABP: return "abp_";
                    case DLCVersion.Rep: return "rep_";
                    case DLCVersion.RepPlus: return "rep+";
                    default: return "unknown";
                }
            }
        }
        public string AllowedSaveVersionsText
        {
            get
            {
                switch (SaveVersion)
                {
                    case DLCVersion.Rebirth: return "重生";
                    case DLCVersion.AB: return "重生、胎衣";
                    case DLCVersion.ABP: return "胎衣+";
                    case DLCVersion.Rep: return "胎衣+、忏悔";
                    case DLCVersion.RepPlus: return "忏悔+";
                    default: return "任意";
                }
            }
        }
        public string FullPath { get; protected set; }
        public string DisplayName { get; private set; }
        public ImageSource Image => GetSaveImage();
        public abstract ImageSource GetSaveImage();

        public SaveSlot(string fullPath, DLCVersion saveVersion)
        {
            FullPath = fullPath;
            SaveVersion = saveVersion;
        }


        public SaveSlot SetDisplayName(string displayName)
        {
            DisplayName = displayName;
            return this;
        }

        public SaveSlot SetSlotType(Type slotType)
        {
            SlotType = slotType;
            return this;
        }


        public static DLCVersion PathToVersion(string path)
        {
            string fileName = Path.GetFileName(path);
            if (dateOnlyRegex.IsMatch(fileName)) return DLCVersion.Unknown;
            //纯日期存档，无法解析版本
            if (backupNameRegex.IsMatch(fileName))
                fileName = fileName.Substring(9);//剔除日期和点号

            if (fileName.StartsWith("persistent")) return DLCVersion.Rebirth;
            else if (fileName.StartsWith("ab_")) return DLCVersion.AB;
            else if (fileName.StartsWith("abp_")) return DLCVersion.ABP;
            else if (fileName.StartsWith("rep_")) return DLCVersion.Rep;
            else if (fileName.StartsWith("rep+")) return DLCVersion.RepPlus;
            return DLCVersion.Unknown;
        }
        public static string VersionToPrefix(DLCVersion version)
        {
            switch (version)
            {
                case DLCVersion.Rebirth: return "";
                case DLCVersion.AB: return "ab_";
                case DLCVersion.ABP: return "abp_";
                case DLCVersion.Rep: return "rep_";
                case DLCVersion.RepPlus: return "rep+";
                default: return null;
            }
        }
        public static string VersionToDisplayVersion(DLCVersion version)
        {
            switch (version)
            {
                case DLCVersion.Rebirth: return "重生";
                case DLCVersion.AB: return "胎衣";
                case DLCVersion.ABP: return "胎衣+";
                case DLCVersion.Rep: return "忏悔";
                case DLCVersion.RepPlus: return "忏悔+";
                default: return "未知";
            }
        }
        public static int GetAllowedDestinations(DLCVersion source) //source的存档可以迁移到destination的存档位
        {
            if (source == DLCVersion.Rebirth) return (int)(DLCVersion.Unknown | DLCVersion.Rebirth | DLCVersion.AB);
            else if (source == DLCVersion.AB) return (int)(DLCVersion.Unknown | DLCVersion.AB);
            else if (source == DLCVersion.ABP) return (int)(DLCVersion.Unknown | DLCVersion.ABP | DLCVersion.Rep);
            else if (source == DLCVersion.Rep) return (int)(DLCVersion.Unknown | DLCVersion.Rep);
            else if (source == DLCVersion.RepPlus) return (int)(DLCVersion.Unknown | DLCVersion.RepPlus);
            return (int)(DLCVersion.Unknown | DLCVersion.Rebirth | DLCVersion.AB | DLCVersion.ABP | DLCVersion.Rep | DLCVersion.RepPlus);
        }
        public static SaveSlot ParseSave(string path) //输入路径，解析存档
        {
            if (path == null) return null;
            DLCVersion version = PathToVersion(path);
            string index = null;
            int extensionIndex = path.IndexOf(".dat");
            if (extensionIndex != -1)
            {
                index = path.Substring(path.IndexOf(".dat") - 1, 1);
            }
            if (!File.Exists(path))
            {
                return new EmptySaveSlot(path, "存档" + index, version);
            }
            FileInfo fileInfo = new FileInfo(path);
            return new IsaacSave(path, "存档" + index, (int)(fileInfo.Length / 102.4) / 10.0, version, fileInfo.LastWriteTime);
        }

        public static bool IsValidSaveFileName(string filename)
        {
            return steamSaveNameRegex.IsMatch(filename) || dateOnlyRegex.IsMatch(filename) || backupNameRegex.IsMatch(filename);
        }

    }
    public class IsaacSave : SaveSlot //表示一个以撒存档
    {
        public double Size { get; } //单位为KB
        public DateTime ModifiedTime { get; }

        public TextBlock ToolTip
        {
            get
            {
                TextBlock result = new TextBlock();

                StringBuilder tooltip = new StringBuilder();
                tooltip.AppendLine("路径：" + FullPath);
                tooltip.AppendLine("DLC版本：" + DisplayVersion);
                tooltip.AppendLine("修改时间：" + ModifiedTime.ToString("F"));
                tooltip.AppendLine("文件大小：" + Size + " KB");
                tooltip.Append("存档鉴定：");
                result.Inlines.Add(tooltip.ToString());

                if ((SaveVersion == DLCVersion.Rep || SaveVersion == DLCVersion.RepPlus) && Size < 5)
                {
                    Run run = new Run("疑似空存档")
                    {
                        Foreground = Brushes.Red,
                        FontWeight = FontWeights.Bold
                    };
                    result.Inlines.Add(run);
                }
                else if (Size < 1 || Size > 20)
                {
                    Run run = new Run("疑似异常存档，请注意检查")
                    {
                        Foreground = Brushes.Red,
                        FontWeight = FontWeights.Bold
                    };
                    result.Inlines.Add(run);
                }
                else
                {
                    Run run = new Run("符合有效存档特征")
                    {
                        Foreground = Brushes.Green,
                        FontWeight = FontWeights.Bold
                    };
                    result.Inlines.Add(run);
                }
                return result;
            }
        }

        public IsaacSave(string fullPath, double size, DLCVersion saveVersion, DateTime modifiedTime) : base(fullPath, saveVersion)
        {
            Size = size;
            ModifiedTime = modifiedTime;
        }
        public IsaacSave(string fullPath, string displayName, double size, DLCVersion saveVersion, DateTime modifiedTime) : this(fullPath, size, saveVersion, modifiedTime)
        {
            SetDisplayName(displayName);
        }

        public override ImageSource GetSaveImage()
        {
            string imageName;
            if (!File.Exists(FullPath)) imageName = "empty.png";
            else if (SaveVersion == DLCVersion.Rebirth) imageName = "rebirth.png";
            else if (SaveVersion == DLCVersion.AB) imageName = "ab.png";
            else if (SaveVersion == DLCVersion.ABP) imageName = "abp.png";
            else if (SaveVersion == DLCVersion.Rep) imageName = "rep.png";
            else if (SaveVersion == DLCVersion.RepPlus) imageName = "repplus.png";
            else imageName = "unknown.png";
            return new BitmapImage(new Uri("/Resources/saveicons/" + imageName, UriKind.Relative));
        }
    }
    public class EmptySaveSlot : SaveSlot //表示一个空位
    {
        public override ImageSource GetSaveImage()
        {
            return new BitmapImage(new Uri("/Resources/saveicons/empty.png", UriKind.Relative));
        }
        public EmptySaveSlot(string fullPath, string displayName, DLCVersion version) : base(fullPath, version)
        {
            FullPath = fullPath;
            SetDisplayName(displayName);
        }
    }
    public class IsaacSaveSet //一组3个存档，这3个存档的版本是一致的
    {
        public DLCVersion SaveVersion
        {
            get
            {
                if (Save1 is IsaacSave save1) return save1.SaveVersion;
                if (Save2 is IsaacSave save2) return save2.SaveVersion;
                if (Save3 is IsaacSave save3) return save3.SaveVersion;
                return DLCVersion.Unknown;
            }
        }
        public SaveSlot Save1;
        public SaveSlot Save2;
        public SaveSlot Save3;

        public static IsaacSaveSet ReadSaveSet(string dir)
        {
            IsaacSaveSet saveSet = new IsaacSaveSet();
            IsaacSave maxVerSave = null;
            string[] files = Directory.GetFiles(dir, "*.dat");
            foreach (string file in files)
            {
                if (IsValidSaveFileName(file))
                {
                    SaveSlot slotData = ParseSave(file);
                    if (!(slotData is IsaacSave))
                    {
                        continue;//有这个文件，不可能读个空位出来
                    }
                    IsaacSave save = (IsaacSave)ParseSave(file);
                    if (save == null) continue;

                    if (maxVerSave == null || save.SaveVersion >= maxVerSave.SaveVersion)
                    {
                        maxVerSave = save;
                    }
                }
            }
            if (maxVerSave == null)
            {
                saveSet.Save1 = new EmptySaveSlot($@"{dir}\persistentgamedata1.dat", "存档1", DLCVersion.Unknown);
                saveSet.Save2 = new EmptySaveSlot($@"{dir}\persistentgamedata2.dat", "存档2", DLCVersion.Unknown);
                saveSet.Save3 = new EmptySaveSlot($@"{dir}\persistentgamedata3.dat", "存档3", DLCVersion.Unknown);
                //没有存档，生成3个没有前缀的空位
            }
            else
            {
                //查找结束之后，直接找最高版本的三个存档
                saveSet.Save1 = ParseSave($@"{dir}\{maxVerSave.Prefix}persistentgamedata1.dat");
                saveSet.Save2 = ParseSave($@"{dir}\{maxVerSave.Prefix}persistentgamedata2.dat");
                saveSet.Save3 = ParseSave($@"{dir}\{maxVerSave.Prefix}persistentgamedata3.dat");
            }
            return saveSet;
        }
        public static IsaacSaveSet ReadSaveSet(string dir, DLCVersion version) //重载方法，参数2确定读取的存档版本，省略则默认读取目录下的最高版本
        {
            IsaacSaveSet saveSet = new IsaacSaveSet();
            saveSet.Save1 = ParseSave($@"{dir}\{VersionToPrefix(version)}persistentgamedata1.dat");
            saveSet.Save2 = ParseSave($@"{dir}\{VersionToPrefix(version)}persistentgamedata2.dat");
            saveSet.Save3 = ParseSave($@"{dir}\{VersionToPrefix(version)}persistentgamedata3.dat");
            return saveSet;
        }
    }
}