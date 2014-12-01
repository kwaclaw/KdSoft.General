namespace Xpea.Demo.Transform
{
  partial class TransformFrm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            this.transformBtn = new System.Windows.Forms.Button();
            this.webTab = new System.Windows.Forms.TabControl();
            this.inputPage = new System.Windows.Forms.TabPage();
            this.inputBrowser = new System.Windows.Forms.WebBrowser();
            this.outputPage = new System.Windows.Forms.TabPage();
            this.outputBrowser = new System.Windows.Forms.WebBrowser();
            this.clearBtn = new System.Windows.Forms.Button();
            this.parserLbl = new System.Windows.Forms.Label();
            this.parserBtn = new System.Windows.Forms.Button();
            this.xmlReaderBtn = new System.Windows.Forms.RadioButton();
            this.helpLbl = new System.Windows.Forms.Label();
            this.webTab.SuspendLayout();
            this.inputPage.SuspendLayout();
            this.outputPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // transformBtn
            // 
            this.transformBtn.Location = new System.Drawing.Point(4, 66);
            this.transformBtn.Name = "transformBtn";
            this.transformBtn.Size = new System.Drawing.Size(75, 23);
            this.transformBtn.TabIndex = 7;
            this.transformBtn.Text = "Transform";
            this.transformBtn.Click += new System.EventHandler(this.transformBtn_Click);
            // 
            // webTab
            // 
            this.webTab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webTab.Controls.Add(this.inputPage);
            this.webTab.Controls.Add(this.outputPage);
            this.webTab.Location = new System.Drawing.Point(4, 90);
            this.webTab.Name = "webTab";
            this.webTab.SelectedIndex = 0;
            this.webTab.Size = new System.Drawing.Size(448, 376);
            this.webTab.TabIndex = 8;
            // 
            // inputPage
            // 
            this.inputPage.Controls.Add(this.inputBrowser);
            this.inputPage.Location = new System.Drawing.Point(4, 22);
            this.inputPage.Name = "inputPage";
            this.inputPage.Padding = new System.Windows.Forms.Padding(3);
            this.inputPage.Size = new System.Drawing.Size(440, 350);
            this.inputPage.TabIndex = 0;
            this.inputPage.Text = "Input";
            // 
            // inputBrowser
            // 
            this.inputBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.inputBrowser.Location = new System.Drawing.Point(0, 0);
            this.inputBrowser.Name = "inputBrowser";
            this.inputBrowser.Size = new System.Drawing.Size(440, 350);
            this.inputBrowser.TabIndex = 0;
            // 
            // outputPage
            // 
            this.outputPage.Controls.Add(this.outputBrowser);
            this.outputPage.Location = new System.Drawing.Point(4, 22);
            this.outputPage.Name = "outputPage";
            this.outputPage.Padding = new System.Windows.Forms.Padding(3);
            this.outputPage.Size = new System.Drawing.Size(440, 350);
            this.outputPage.TabIndex = 1;
            this.outputPage.Text = "Output";
            // 
            // outputBrowser
            // 
            this.outputBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputBrowser.Location = new System.Drawing.Point(3, 3);
            this.outputBrowser.Name = "outputBrowser";
            this.outputBrowser.Size = new System.Drawing.Size(434, 344);
            this.outputBrowser.TabIndex = 0;
            // 
            // clearBtn
            // 
            this.clearBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clearBtn.AutoSize = true;
            this.clearBtn.Location = new System.Drawing.Point(400, 8);
            this.clearBtn.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(48, 23);
            this.clearBtn.TabIndex = 2;
            this.clearBtn.Text = "Clear";
            this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);
            // 
            // parserLbl
            // 
            this.parserLbl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.parserLbl.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.parserLbl.Location = new System.Drawing.Point(108, 12);
            this.parserLbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3);
            this.parserLbl.Name = "parserLbl";
            this.parserLbl.Size = new System.Drawing.Size(286, 16);
            this.parserLbl.TabIndex = 1;
            // 
            // parserBtn
            // 
            this.parserBtn.AutoSize = true;
            this.parserBtn.Location = new System.Drawing.Point(4, 8);
            this.parserBtn.Name = "parserBtn";
            this.parserBtn.Size = new System.Drawing.Size(98, 23);
            this.parserBtn.TabIndex = 0;
            this.parserBtn.Text = "Load SAX Parser";
            this.parserBtn.Click += new System.EventHandler(this.parserBtn_Click);
            // 
            // xmlReaderBtn
            // 
            this.xmlReaderBtn.AutoSize = true;
            this.xmlReaderBtn.Checked = true;
            this.xmlReaderBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xmlReaderBtn.Location = new System.Drawing.Point(165, 35);
            this.xmlReaderBtn.Name = "xmlReaderBtn";
            this.xmlReaderBtn.Size = new System.Drawing.Size(86, 17);
            this.xmlReaderBtn.TabIndex = 4;
            this.xmlReaderBtn.TabStop = true;
            this.xmlReaderBtn.Text = "XmlReader";
            this.xmlReaderBtn.UseVisualStyleBackColor = true;
            this.xmlReaderBtn.CheckedChanged += new System.EventHandler(this.xmlReaderBtn_CheckedChanged);
            // 
            // helpLbl
            // 
            this.helpLbl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.helpLbl.Location = new System.Drawing.Point(5, 36);
            this.helpLbl.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.helpLbl.Name = "helpLbl";
            this.helpLbl.Size = new System.Drawing.Size(171, 16);
            this.helpLbl.TabIndex = 3;
            this.helpLbl.Text = "If no SAX parser is chosen, use";
            // 
            // TransformFrm
            // 
            this.ClientSize = new System.Drawing.Size(455, 469);
            this.Controls.Add(this.xmlReaderBtn);
            this.Controls.Add(this.helpLbl);
            this.Controls.Add(this.clearBtn);
            this.Controls.Add(this.parserLbl);
            this.Controls.Add(this.parserBtn);
            this.Controls.Add(this.webTab);
            this.Controls.Add(this.transformBtn);
            this.Name = "TransformFrm";
            this.Text = "Transformation Demo";
            this.Load += new System.EventHandler(this.transformFrm_Load);
            this.webTab.ResumeLayout(false);
            this.inputPage.ResumeLayout(false);
            this.outputPage.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button transformBtn;
    private System.Windows.Forms.TabControl webTab;
    private System.Windows.Forms.TabPage inputPage;
    private System.Windows.Forms.TabPage outputPage;
    private System.Windows.Forms.WebBrowser inputBrowser;
    private System.Windows.Forms.WebBrowser outputBrowser;
    private System.Windows.Forms.Button clearBtn;
    private System.Windows.Forms.Label parserLbl;
    private System.Windows.Forms.Button parserBtn;
    private System.Windows.Forms.RadioButton xmlReaderBtn;
    private System.Windows.Forms.Label helpLbl;
  }
}

