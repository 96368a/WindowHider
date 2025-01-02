using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace WindowSharingHider
{
    public partial class MainWindow : Form
    {
        public class WindowInfo
        {
            public String Title { get; set; }
            public IntPtr Handle { get; set; }
            public Boolean stillExists = false;
            public override string ToString()
            {
                return Title;
            }
        }

        // 用于存储用户选中的进程名
        private HashSet<string> _selectedProcessNames = new HashSet<string>();

        public MainWindow()
        {
            InitializeComponent();

            // 从配置文件中加载选中的进程名
            _selectedProcessNames = ConfigHelper.LoadSelectedProcessNames();

            var timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
            timer.Start();

            // 绑定 CheckBox 的选中事件
            windowListCheckBox.ItemCheck += WindowListCheckBox_ItemCheck;
        }
        Boolean flagToPreserveSettings = false;

        // 处理 CheckBox 的选中事件
        private void WindowListCheckBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var window = windowListCheckBox.Items[e.Index] as WindowInfo;
            if (window != null)
            {
                // 获取窗口的进程名
                string processName = GetProcessNameByWindowHandle(window.Handle);

                if (e.NewValue == CheckState.Checked)
                {
                    // 如果窗口被选中，则将其进程名添加到选中列表中
                    _selectedProcessNames.Add(processName);
                }
                else
                {
                    // 如果窗口被取消选中，则将其进程名从选中列表中移除
                    _selectedProcessNames.Remove(processName);
                }
            }

            // 将选中的进程名保存到配置文件
            ConfigHelper.SaveSelectedProcessNames(_selectedProcessNames);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // 标记所有窗口为不存在
            foreach (WindowInfo window in windowListCheckBox.Items)
            {
                window.stillExists = false;
            }

            // 获取当前可见窗口
            var currWindows = WindowHandler.GetVisibleWindows();

            // 添加或更新窗口
            foreach (var window in currWindows)
            {
                // 获取窗口的进程名
                string processName = GetProcessNameByWindowHandle(window.Key);

                var existingWindow = windowListCheckBox.Items.Cast<WindowInfo>().FirstOrDefault(i => i.Handle == window.Key);
                if (existingWindow != null)
                {
                    existingWindow.stillExists = true;
                    existingWindow.Title = window.Value;
                }
                else
                {
                    // 创建新窗口项
                    var newWindow = new WindowInfo { Title = window.Value, Handle = window.Key, stillExists = true };
                    windowListCheckBox.Items.Add(newWindow);

                    // 如果进程名在选中列表中，则设置为选中状态
                    if (_selectedProcessNames.Contains(processName))
                    {
                        int index = windowListCheckBox.Items.IndexOf(newWindow);
                        windowListCheckBox.SetItemChecked(index, true);
                    }
                }
            }

            // 删除不存在的窗口
            foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray())
            {
                if (window.stillExists == false)
                {
                    windowListCheckBox.Items.Remove(window);
                }
            }

            // 更新窗口的显示亲和性状态
            foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray())
            {
                var status = WindowHandler.GetWindowDisplayAffinity(window.Handle) > 0;
                var target = windowListCheckBox.GetItemChecked(windowListCheckBox.Items.IndexOf(window));
                if (target != status && flagToPreserveSettings)
                {
                    WindowHandler.SetWindowDisplayAffinity(window.Handle, target ? 0x11 : 0x0);
                    status = WindowHandler.GetWindowDisplayAffinity(window.Handle) > 0;
                }
                windowListCheckBox.SetItemChecked(windowListCheckBox.Items.IndexOf(window), status);
            }

            // 允许更新窗口的显示亲和性
            flagToPreserveSettings = true;
        }

        // 根据窗口句柄获取进程名
        private string GetProcessNameByWindowHandle(IntPtr hWnd)
        {
            try
            {
                // 获取进程ID
                WindowHandler.GetWindowThreadProcessId(hWnd, out int processId);

                // 获取进程对象
                using (var process = System.Diagnostics.Process.GetProcessById(processId))
                {
                    return process.ProcessName;
                }
            }
            catch
            {
                return string.Empty; // 如果进程不存在，返回空字符串
            }
        }
    }
}
