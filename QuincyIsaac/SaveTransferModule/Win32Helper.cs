using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuincyIsaac.SaveTransferModule
{
    public class Win32Helper
    {
        private const int GWL_EXSTYLE = -20;   //表示设定的是窗口的扩展样式（相对的是基本样式）
        private const int WS_EX_TRANSPARENT = 0x00000020;   //设置扩展样式“透明”
        private const int WS_EX_NOACTIVATE = 0x08000000;    //设置扩展样式“禁止激活”，不允许窗口获得焦点

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        //用DllImport注解，调用win32库里的C语言函数，用于获取窗口属性。参数是库里定义好的，不能随便写。

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong); //win32库里的C语言函数，用于设置窗口属性

        public static void SetWindowClickable(Window window)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window); 
            //Interop是Interoperability(互操作性)的缩写，用于实现托管代码（C#代码）和非托管代码（Win32底层代码）的互操作。
            //要想拿到窗口句柄，就得用它。
            helper.EnsureHandle();//确保窗口有窗口句柄，如果没有的话则创建一个
            IntPtr windowHandle = helper.Handle;
            int originEXStyle = GetWindowLong(windowHandle, GWL_EXSTYLE); //获取本来的扩展样式
            SetWindowLong(windowHandle, GWL_EXSTYLE, originEXStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);//设置新的扩展样式
        }
    }
}
