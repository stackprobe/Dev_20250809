using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Security.Permissions;
using System.Windows.Forms;
using Charlotte.Commons;

namespace Charlotte
{
	public partial class MainWin : Form
	{
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected override void WndProc(ref Message m)
		{
			const int WM_SYSCOMMAND = 0x112;
			const long SC_CLOSE = 0xF060L;
			const long WP_MASK = 0xFFF0L;

			if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & WP_MASK) == SC_CLOSE)
				return;

			base.WndProc(ref m);
		}

		public MainWin()
		{
			InitializeComponent();
		}

		private void MainWin_Load(object sender, EventArgs e)
		{
			// none
		}

		private void MainWin_Shown(object sender, EventArgs e)
		{
			this.Visible = false;
			this.MT_Enabled = true;
			this.TaskTrayIcon.Visible = true;
		}

		private void CloseWindow()
		{
			this.MT_Enabled = false;
			this.TaskTrayIcon.Visible = false;
			this.Close();
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			this.CloseWindow();
			return;
		}

		private bool MT_Enabled = false;
		private Func<bool> MT_Task = null;

		private void MainTimer_Tick(object sender, EventArgs e)
		{
			if (!this.MT_Enabled)
				return;

			if (this.MT_Task == null)
				this.MT_Task = SCommon.Supplier(this.E_MT_Task());

			if (!this.MT_Task())
			{
				this.CloseWindow();
				return;
			}
		}

		private IEnumerable<bool> E_MT_Task()
		{
			for (; ; )
			{
				yield return true;
			}
		}
	}
}
