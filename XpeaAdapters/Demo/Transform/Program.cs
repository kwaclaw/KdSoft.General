using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

namespace Xpea.Demo.Transform
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
      Application.EnableVisualStyles();
      Application.ThreadException += new ThreadExceptionEventHandler(HandleException);
      Application.Run(new TransformFrm());
    }

    static void HandleException(object sender, ThreadExceptionEventArgs e) {
      MessageBox.Show(e.Exception.Message,
                      "Application Error",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
    }
  }
}