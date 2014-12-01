// NO WARRANTY!  This code is in the Public Domain.
// Written by Karl Waclawek (karl@waclawek.net).

using System;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using Org.System.Xml.Sax;
using Org.System.Xml.Sax.Helpers;

namespace Xpea.Demo
{
  /// <summary>
  /// Dialog to pick a SAX parser by assembly name and class name.
  /// </summary>
  public class pickDlg: System.Windows.Forms.Form
  {
    private System.ComponentModel.IContainer components;
    private System.Windows.Forms.Label asmLbl;
    private System.Windows.Forms.TextBox assemBox;
    private System.Windows.Forms.OpenFileDialog fileDlg;
    private System.Windows.Forms.Button okBtn;
    private System.Windows.Forms.Button cancelBtn;
    private System.Windows.Forms.Button browseBtn;
    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.Label classLbl;
    private System.Windows.Forms.TextBox classBox;
    public pickDlg() {
      //
      // The InitializeComponent() call is required for Windows Forms designer support.
      //
      InitializeComponent();

      //
      // TODO: Add constructor code after the InitializeComponent() call.
      //
    }

    #region Windows Forms Designer generated code
    /// <summary>
    /// This method is required for Windows Forms designer support.
    /// Do not change the method contents inside the source code editor. The Forms designer might
    /// not be able to load this method if it was changed manually.
    /// </summary>
    private void InitializeComponent() {
      this.components = new System.ComponentModel.Container();
      this.classBox = new System.Windows.Forms.TextBox();
      this.toolTip = new System.Windows.Forms.ToolTip(this.components);
      this.classLbl = new System.Windows.Forms.Label();
      this.browseBtn = new System.Windows.Forms.Button();
      this.assemBox = new System.Windows.Forms.TextBox();
      this.asmLbl = new System.Windows.Forms.Label();
      this.cancelBtn = new System.Windows.Forms.Button();
      this.okBtn = new System.Windows.Forms.Button();
      this.fileDlg = new System.Windows.Forms.OpenFileDialog();
      this.SuspendLayout();
      // 
      // classBox
      // 
      this.classBox.Location = new System.Drawing.Point(64, 45);
      this.classBox.Name = "classBox";
      this.classBox.Size = new System.Drawing.Size(264, 20);
      this.classBox.TabIndex = 2;
      this.toolTip.SetToolTip(this.classBox, "Fully qualified class name (optional)");
      // 
      // classLbl
      // 
      this.classLbl.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.classLbl.Location = new System.Drawing.Point(8, 47);
      this.classLbl.Name = "classLbl";
      this.classLbl.Size = new System.Drawing.Size(40, 15);
      this.classLbl.TabIndex = 6;
      this.classLbl.Text = "Class";
      this.toolTip.SetToolTip(this.classLbl, "Fully qualified class name (optional)");
      // 
      // browseBtn
      // 
      this.browseBtn.BackColor = System.Drawing.SystemColors.Control;
      this.browseBtn.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.browseBtn.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.browseBtn.Location = new System.Drawing.Point(328, 12);
      this.browseBtn.Name = "browseBtn";
      this.browseBtn.Size = new System.Drawing.Size(29, 22);
      this.browseBtn.TabIndex = 1;
      this.browseBtn.Text = "...";
      this.toolTip.SetToolTip(this.browseBtn, "Pick assembly file");
      this.browseBtn.UseVisualStyleBackColor = false;
      this.browseBtn.Click += new System.EventHandler(this.BrowseBtnClick);
      // 
      // assemBox
      // 
      this.assemBox.Location = new System.Drawing.Point(64, 13);
      this.assemBox.Name = "assemBox";
      this.assemBox.Size = new System.Drawing.Size(264, 20);
      this.assemBox.TabIndex = 0;
      this.toolTip.SetToolTip(this.assemBox, "Assembly name or full file path");
      // 
      // asmLbl
      // 
      this.asmLbl.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.asmLbl.Location = new System.Drawing.Point(8, 15);
      this.asmLbl.Name = "asmLbl";
      this.asmLbl.Size = new System.Drawing.Size(56, 15);
      this.asmLbl.TabIndex = 5;
      this.asmLbl.Text = "Assembly";
      this.toolTip.SetToolTip(this.asmLbl, "Assembly name or full file path");
      // 
      // cancelBtn
      // 
      this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.cancelBtn.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.cancelBtn.Location = new System.Drawing.Point(200, 82);
      this.cancelBtn.Name = "cancelBtn";
      this.cancelBtn.Size = new System.Drawing.Size(75, 21);
      this.cancelBtn.TabIndex = 4;
      this.cancelBtn.Text = "Cancel";
      // 
      // okBtn
      // 
      this.okBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.okBtn.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.okBtn.Location = new System.Drawing.Point(88, 82);
      this.okBtn.Name = "okBtn";
      this.okBtn.Size = new System.Drawing.Size(75, 21);
      this.okBtn.TabIndex = 3;
      this.okBtn.Text = "OK";
      this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
      // 
      // pickDlg
      // 
      this.AcceptButton = this.okBtn;
      this.CancelButton = this.cancelBtn;
      this.ClientSize = new System.Drawing.Size(369, 118);
      this.Controls.Add(this.browseBtn);
      this.Controls.Add(this.classBox);
      this.Controls.Add(this.assemBox);
      this.Controls.Add(this.classLbl);
      this.Controls.Add(this.asmLbl);
      this.Controls.Add(this.cancelBtn);
      this.Controls.Add(this.okBtn);
      this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "pickDlg";
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Pick SAX Parser";
      this.ResumeLayout(false);
      this.PerformLayout();

    }
    #endregion

    private void BrowseBtnClick(object sender, System.EventArgs e) {
      if (fileDlg.ShowDialog() == DialogResult.OK)
        assemBox.Text = fileDlg.FileName;
    }

    private IXmlReader LoadParser(string assemName, string readerClass) {
      Assembly assem = null;
      if (File.Exists(assemName))
        assem = Assembly.LoadFrom(assemName);
      else
        assem = Assembly.Load(assemName);
      if (assem == null)
        throw new ApplicationException(
                   String.Format("Assembly not found: {0}.", assemName));

      IXmlReader newReader = null;
      if (readerClass == null || readerClass == String.Empty)
        // we have only an assembly, so load whatever IXmlReader we can find
        newReader = SaxReaderFactory.CreateReader(assem, null);
      else
        // class and assembly specified, we know exactly what we want
        newReader = SaxReaderFactory.CreateReader(assem, readerClass, null);
      if (newReader == null)
        throw new ApplicationException(
          String.Format("Cannot create parser {0}.", readerClass));
      return newReader;
    }

    private void okBtn_Click(object sender, EventArgs e) {
      try {
        Reader = LoadParser(ParserAssembly, ParserClass);
      }
      catch {
        DialogResult = DialogResult.Abort;
        throw;
      }
    }

    private IXmlReader reader;

    public IXmlReader Reader {
      get { return reader; }
      set {
        reader = value;
        if (reader != null) {
          Type readerType = reader.GetType();
          Assembly assem = Assembly.GetAssembly(readerType);
          AssemblyName asmName = assem.GetName();
          ParserAssembly = asmName.Name;
          ParserClass = readerType.FullName;
        }
        else {
          ParserAssembly = "";
          ParserClass = "";
        }
      }
    }

    public string ParserAssembly {
      get { return assemBox.Text; }
      internal set { assemBox.Text = value; }
    }

    public string ParserClass {
      get { return classBox.Text; }
      internal set { classBox.Text = value; }
    }
  }
}
