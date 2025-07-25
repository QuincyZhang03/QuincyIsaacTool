using System.Diagnostics;
using System.Reflection;

namespace QuincyIsaac
{
    public class ProgramVersion
    {
        public static ProgramVersion version = new ProgramVersion();

        public int MAJOR_VERSION;
        public int MINOR_VERSION;
        public int AMEND_VERSION;

        public string MainWindowTitle
        {
            get => $"夏老师的以撒配置存档管理器  V{MAJOR_VERSION}.{MINOR_VERSION}{(AMEND_VERSION == 0 ? "" : "." + AMEND_VERSION)}";
        }

        public string SaveManagementTitle
        {
            get => $"夏老师的以撒存档管理器  V{MAJOR_VERSION}.{MINOR_VERSION}{(AMEND_VERSION == 0 ? "" : "." + AMEND_VERSION)}";
        }

        public ProgramVersion()
        {
            string[] VERSION = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion.Split('.');
            MAJOR_VERSION = int.Parse(VERSION[0]);
            MINOR_VERSION = int.Parse(VERSION[1]);
            AMEND_VERSION = int.Parse(VERSION[2]);
        }

        public bool HasNewerVersion(int majorVer, int minorVer, int amendVer)
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
    }
}
