/*
    This file is part of Imgur Snipping Tool.

    Imgur Snipping Tool is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Imgur Snipping Tool is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Imgur Snipping Tool.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Net;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace snippingtool
{
	public static class FlashWindow
{
[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

[StructLayout(LayoutKind.Sequential)]
private struct FLASHWINFO
{
/// <summary>
/// The size of the structure in bytes.
/// </summary>
public uint cbSize;
/// <summary>
/// A Handle to the Window to be Flashed. The window can be either opened or minimized.
/// </summary>
public IntPtr hwnd;
/// <summary>
/// The Flash Status.
/// </summary>
public uint dwFlags;
/// <summary>
/// The number of times to Flash the window.
/// </summary>
public uint uCount;
/// <summary>
/// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
/// </summary>
public uint dwTimeout;
}

/// <summary>
/// Stop flashing. The system restores the window to its original stae.
/// </summary>
public const uint FLASHW_STOP = 0;

/// <summary>
/// Flash the window caption.
/// </summary>
public const uint FLASHW_CAPTION = 1;

/// <summary>
/// Flash the taskbar button.
/// </summary>
public const uint FLASHW_TRAY = 2;

/// <summary>
/// Flash both the window caption and taskbar button.
/// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
/// </summary>
public const uint FLASHW_ALL = 3;

/// <summary>
/// Flash continuously, until the FLASHW_STOP flag is set.
/// </summary>
public const uint FLASHW_TIMER = 4;

/// <summary>
/// Flash continuously until the window comes to the foreground.
/// </summary>
public const uint FLASHW_TIMERNOFG = 12;
/// <summary>
/// Flash the spacified Window (Form) until it recieves focus.
/// </summary>
/// <param name=”form”>The Form (Window) to Flash.</param>
/// <returns></returns>
public static bool Flash(System.Windows.Forms.Form form)
{
// Make sure we’re running under Windows 2000 or later
if (Win2000OrLater)
{
FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue, 0);
return FlashWindowEx(ref fi);
}
return false;
}

private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
{
FLASHWINFO fi = new FLASHWINFO();
fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
fi.hwnd = handle;
fi.dwFlags = flags;
fi.uCount = count;
fi.dwTimeout = timeout;
return fi;
}

/// <summary>
/// Flash the specified Window (form) for the specified number of times
/// </summary>
/// <param name=”form”>The Form (Window) to Flash.</param>
/// <param name=”count”>The number of times to Flash.</param>
/// <returns></returns>
public static bool Flash(System.Windows.Forms.Form form, uint count)
{
if (Win2000OrLater)
{
FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_ALL, count, 0);
return FlashWindowEx(ref fi);
}
return false;
}

/// <summary>
/// Start Flashing the specified Window (form)
/// </summary>
/// <param name=”form”>The Form (Window) to Flash.</param>
/// <returns></returns>
public static bool Start(System.Windows.Forms.Form form)
{
if (Win2000OrLater)
{
FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_ALL, uint.MaxValue, 0);
return FlashWindowEx(ref fi);
}
return false;
}

/// <summary>
/// Stop Flashing the specified Window (form)
/// </summary>
/// <param name=”form”></param>
/// <returns></returns>
public static bool Stop(System.Windows.Forms.Form form)
{
if (Win2000OrLater)
{
FLASHWINFO fi = Create_FLASHWINFO(form.Handle, FLASHW_STOP, uint.MaxValue, 0);
return FlashWindowEx(ref fi);
}
return false;
}

/// <summary>
/// A boolean value indicating whether the application is running on Windows 2000 or later.
/// </summary>
private static bool Win2000OrLater
{
get { return System.Environment.OSVersion.Version.Major >= 5; }
}
}
	
    public partial class MainForm : Form
    {
        private const string API_KEY = "830bececb56919ddd399ee27d45ffec4";
        private Imgur imgur;
        private const string COPIED_CLIPBOARD = "{0} link was copied to your clipboard";

        public void uploadProgressUpdateThreadSafe(snippingtool.Imgur.ProgressData data)
        {
            progressBar1.Maximum = data.max_value;
            progressBar1.Value = data.value;
        }
        
        
        public void uploadCompleteThreadSafe(snippingtool.Imgur.UploadResults results)
        {
        	
            imgur_textbox.Text = results.imgur_page;
            original_textbox.Text = results.original;
            delete_textbox.Text = results.delete_page;
            Clipboard.SetText(results.original);
            copied_label.Text = String.Format(COPIED_CLIPBOARD, "Original");
            tabControl1.SelectedTab = tabPage2;
            ((Control)this.tabPage2).Enabled = true;
            FlashWindow.Flash(this);
        }

        /// <summary>
        /// Gets called from another thread when the upload has a progress update
        /// </summary>
        /// <param name="data"></param>
        public void uploadProgressUpdate(snippingtool.Imgur.ProgressData data)
        {
            this.Invoke(new snippingtool.Imgur.uploadProgressUpdate(uploadProgressUpdateThreadSafe), new object[] { data });
        }

        /// <summary>
        /// Gets called from another thread when the upload has completed
        /// </summary>
        /// <param name="data"></param>
        public void uploadComplete(snippingtool.Imgur.UploadResults results)
        {
            this.Invoke(new snippingtool.Imgur.uploadComplete(uploadCompleteThreadSafe), new object[] { results });
        }

        public MainForm()
        {
            InitializeComponent();
            ((Control)this.tabPage2).Enabled = false;

            imgur = new Imgur(API_KEY);
            imgur.UploadProgressUpdateProperty = uploadProgressUpdate;
            imgur.UploadCompleteProperty = uploadComplete;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
            	this.Opacity = .0;
                progressBar1.Value = 0;
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var bmp = SnippingTool.Snip();
this.Opacity = 1;
            if (bmp != null)
            {
                try
                {
                    imgur.PostImage(bmp, ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void OpenBrowser_click(object sender, EventArgs e)
        {
            Label box = (Label)sender;
            // load up the browser link

            switch (box.Text)
            { 
                case "Original":
                    Process.Start(original_textbox.Text);
                    break;
            	case "Imgur":
                    Process.Start(imgur_textbox.Text);
                    break;
                case "Delete":
                    Process.Start(delete_textbox.Text);
                    break;
                default:
                    break;
            }
        }

        private void Copy_click(object sender, EventArgs e)
        {
            TextBox box = (TextBox)sender;
            Clipboard.SetText(box.Text);
            
            switch (box.Name)
            { 
                case "original_textbox":
                    copied_label.Text = String.Format(COPIED_CLIPBOARD, "Original");
                    break;
                case "imgur_textbox":
                    copied_label.Text = String.Format(COPIED_CLIPBOARD, "Imgur");
                    break;
                case "delete_textbox":
                    copied_label.Text = String.Format(COPIED_CLIPBOARD, "Delete");
                    break;

                default:
                    break;
            }
        }
    }
}
