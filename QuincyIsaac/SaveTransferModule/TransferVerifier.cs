
using System.Text;

namespace QuincyIsaac.SaveTransferModule
{
    public class TransferVerifier
    {
        private readonly IsaacSave _innerSave; //如果迁移是从程序内迁移到程序内，该值为被拖拽的存档，否则为null。
        private SaveSlot.DLCVersion SourceVersion { get; }
        private SaveSlot TargetSlot { get; }

        private readonly string _message;
        public string Message { get => _message; }
        public bool Allowed  //为true则允许操作
        {
            get
            {
                if (TargetSlot.SlotType == SaveSlot.Type.GameBackup) return false; //迁移到游戏备份存档位，不允许
                if (TargetSlot.SaveVersion == SaveSlot.DLCVersion.Unknown) return false; //迁移到未知版本存档位，不允许
                if (SourceVersion == SaveSlot.DLCVersion.Unknown) return true; //迁移进来第三方存档，默认允许，但弹出信息
                return (SaveSlot.GetAllowedDestinations(SourceVersion) & (int)TargetSlot.SaveVersion) != 0; //根据DLC版本信息判断迁移路径是否合法
            }
        }

        public bool MultipleStep { get; private set; }  //如果需要多步才能进行，该属性为true

        private string InitMessage()
        {
            if (TargetSlot.SlotType == SaveSlot.Type.GameBackup)
                return "My Games目录是游戏自动生成的存档备份。\n" +
                    "大多数情况下，无论是正版还是盗版游戏均不会读取这里的存档。\n" +
                    "如果您非常明确自己要做什么，请点击分栏上的\"打开\"按钮进行手动操作。";
            if (TargetSlot.SaveVersion == SaveSlot.DLCVersion.Unknown)
                return "程序无法识别该存档栏位对应的DLC版本。\n" +
                    "如果这是盗版游戏存档栏位，请先至少运行一次盗版游戏后再尝试使用本程序。";
            if (TargetSlot.SaveVersion < SourceVersion)
                return "以撒只允许将存档向同级或向后迁移，较新版本的存档无法迁移到早期版本。";
            if (SourceVersion == SaveSlot.DLCVersion.Unknown) //这个要弹信息框确认
                return $"无法从文件名识别存档对应的游戏版本，请自行甄别使用。\n" +
                    $"该存档槽是 {TargetSlot.DisplayVersion} 存档槽，\n" +
                    $"可接受的存档版本有： {TargetSlot.AllowedSaveVersionsText} \n" +
                    $"您确定要继续迁移吗？";
            int num = 1;
            StringBuilder steps = new StringBuilder();
            if (SourceVersion <= SaveSlot.DLCVersion.AB)
            {
                if (TargetSlot.SaveVersion >= SaveSlot.DLCVersion.ABP)
                {
                    if (TargetSlot.SlotType == SaveSlot.Type.Hacker) //重生、胎衣→盗版胎衣+
                        steps.AppendLine($"第{num++}步：点击\"打开\"按钮打开盗版存档目录，将你正在迁移的存档拖拽到打开的文件夹当中。");
                    else if (_innerSave == null || _innerSave.SlotType == SaveSlot.Type.Hacker || _innerSave.SlotType == SaveSlot.Type.GameBackup) //非正版重生、胎衣→正版胎衣+
                        steps.AppendLine($"第{num++}步：在本程序中将该存档拖拽到正版重生或胎衣存档槽。");
                    steps.AppendLine($"第{num++}步：只开启胎衣（Afterbirth）、胎衣+（Afterbirth+）两个DLC，打开游戏，使用游戏中的\"导入重生存档\"功能，将存档迁移到胎衣+。");
                }
                if (TargetSlot.SaveVersion >= SaveSlot.DLCVersion.Rep)
                {
                    if (TargetSlot.SlotType == SaveSlot.Type.Hacker)
                    {
                        if (TargetSlot.SaveVersion == SaveSlot.DLCVersion.Rep)
                            //胎衣+→盗版忏悔，迁移结束
                            steps.AppendLine($"第{num++}步：将盗版存档目录中的abp_开头的文件拖拽到本程序盗版存档槽中。");
                        else if (TargetSlot.SaveVersion == SaveSlot.DLCVersion.RepPlus)
                            //胎衣+→盗版忏悔，准备迁移到忏悔+
                            steps.AppendLine($"第{num++}步：将盗版存档目录中的abp_开头的文件前缀改为rep_");
                    }
                    else
                        //正版胎衣+→正版忏悔
                        steps.AppendLine($"第{num++}步：在本程序中将胎衣+存档拖拽到正版忏悔存档槽。");
                }
                if (TargetSlot.SaveVersion == SaveSlot.DLCVersion.RepPlus)
                {
                    steps.AppendLine($"第{num++}步：把目标位置所有已经存在的忏悔+存档拖拽到右下角垃圾箱删除。（建议先进行备份）");
                    if (TargetSlot.SlotType == SaveSlot.Type.Hacker) //盗版迁移到忏悔+结束
                        steps.AppendLine($"第{num++}步：启动忏悔+游戏，游戏会自动将存档迁移到忏悔+。");
                    else //正版迁移到忏悔+结束
                        steps.AppendLine($"第{num++}步：开启所有DLC，启动游戏，游戏会自动将存档迁移到忏悔+。");
                }
            }
            else if (SourceVersion == SaveSlot.DLCVersion.ABP && TargetSlot.SaveVersion == SaveSlot.DLCVersion.RepPlus)
            {
                if (TargetSlot.SlotType == SaveSlot.Type.Hacker) //胎衣+迁移到盗版忏悔，准备迁移到忏悔+
                {
                    steps.AppendLine($"第{num++}步：点击\"打开\"按钮打开盗版存档目录，将你正在迁移的存档拖拽到打开的文件夹当中。");
                    steps.AppendLine($"第{num++}步：将盗版存档目录中的abp_开头的文件前缀改为rep_");
                }
                else //正版胎衣+迁移到正版忏悔，准备迁移到忏悔+
                {
                    steps.AppendLine($"第{num++}步：在本程序中将胎衣+存档拖拽到正版忏悔存档槽。");
                }
                steps.AppendLine($"第{num++}步：把目标位置所有已经存在的忏悔+存档拖拽到右下角垃圾箱删除。（建议先进行备份）");
                if (TargetSlot.SlotType == SaveSlot.Type.Hacker) //盗版迁移到忏悔+结束
                    steps.AppendLine($"第{num++}步：启动忏悔+游戏，游戏会自动将存档迁移到忏悔+。");
                else //正版迁移到忏悔+结束
                    steps.AppendLine($"第{num++}步：开启所有DLC，启动游戏，游戏会自动将存档迁移到忏悔+。");
            }
            else if (SourceVersion == SaveSlot.DLCVersion.Rep && TargetSlot.SaveVersion == SaveSlot.DLCVersion.RepPlus)
            {
                if (TargetSlot.SlotType == SaveSlot.Type.Hacker) //忏悔迁移到盗版忏悔+
                {
                    steps.AppendLine($"第{num++}步：点击\"打开\"按钮打开盗版存档目录，将你正在迁移的存档拖拽到打开的文件夹当中。");
                }
                else if (_innerSave == null || _innerSave.SlotType == SaveSlot.Type.Hacker || _innerSave.SlotType == SaveSlot.Type.GameBackup) //非正版忏悔迁移到正版忏悔，准备迁移到忏悔+
                {
                    steps.AppendLine($"第{num++}步：在本程序中将该存档拖拽到正版忏悔存档槽。");
                }
                steps.AppendLine($"第{num++}步：把目标位置所有已经存在的忏悔+存档拖拽到右下角垃圾箱删除。（建议先进行备份）");
                if (TargetSlot.SlotType == SaveSlot.Type.Hacker) //盗版迁移到忏悔+结束
                    steps.AppendLine($"第{num++}步：启动忏悔+游戏，游戏会自动将存档迁移到忏悔+。");
                else //正版迁移到忏悔+结束
                    steps.AppendLine($"第{num++}步：开启所有DLC，启动游戏，游戏会自动将存档迁移到忏悔+。");
            }
            if (num > 1)
            {
                MultipleStep = true;
                steps.AppendLine();
                steps.AppendLine("注意：每步完成后，请及时点击\"刷新\"按钮重新读取存档。");
                if (TargetSlot.SlotType == SaveSlot.Type.Hacker)
                    steps.AppendLine("注意：如果您的盗版游戏不支持切换DLC版本，跨版本存档迁移过程可能无法完成。");
                if (_innerSave == null)
                    steps.AppendLine("当然，如果您了解自己在做什么，可以自行修改存档文件前缀后再次拖拽。");
                return steps.ToString();
            }
            return null;
        } //初始化消息内容和MultipleStep的值


        public TransferVerifier(IsaacSave sourceSave, SaveSlot targetSlot)
        {
            _innerSave = sourceSave;
            SourceVersion = sourceSave.SaveVersion;
            TargetSlot = targetSlot;
            _message = InitMessage(); //要在这里确认MultipleStep的值
        }

        public TransferVerifier(SaveSlot.DLCVersion sourceVersion, SaveSlot targetSlot)
        {
            SourceVersion = sourceVersion;
            TargetSlot = targetSlot;
            _message = InitMessage(); //要在这里确认MultipleStep的值
        }
    }
}
