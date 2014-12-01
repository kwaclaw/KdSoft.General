/**************************************************************************
 **           Demo - basic counting of specific elements                 **
 **************************************************************************/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.IO;
using Org.System.Xml;
using Org.System.Xml.Sax;
using Org.System.Xml.Sax.Helpers;
using Org.System.Xml.Xpea;
using Org.System.Xml.Xpea.StdImpl;
using Org.System.Xml.Xpea.Helpers;
using Org.System.Xml.Xpea.Sax;
using System.Xml;
using Org.System.Xml.Xpea.Reader;

namespace Xpea.Demo.Basic
{
  // make shortcuts for some bound generic types
  using IBasicElementHandler = IElementHandler<StdMatchNode<EventStatus>, EventStatus>;
  using BasicXmlElementHandler = XmlElementHandler<StdMatchNode<EventStatus>, EventStatus>;

  /// <summary>
  /// Main form.
  /// </summary>
  public class MainFrm: System.Windows.Forms.Form
  {
    private System.Windows.Forms.Button fileBtn;
    private System.Windows.Forms.OpenFileDialog fileDlg;
    private System.Windows.Forms.Button parserBtn;
    private System.Windows.Forms.TextBox fileBox;
    private System.Windows.Forms.Label parserLbl;
    private System.Windows.Forms.Button processBtn;
    private System.Windows.Forms.RichTextBox rtfBox;
    private System.Windows.Forms.Label helpLbl;
    private System.Windows.Forms.Label fileLbl;
    private Button clearBtn;
    private RadioButton xmlReaderBtn;
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.Container components = null;

    public MainFrm() {
      //
      // Required for Windows Form Designer support
      //
      InitializeComponent();

      //
      // TODO: Add any constructor code after InitializeComponent call
      //
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing) {
      if (disposing) {
        if (components != null) {
          components.Dispose();
        }
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
            this.fileBtn = new System.Windows.Forms.Button();
            this.fileDlg = new System.Windows.Forms.OpenFileDialog();
            this.parserBtn = new System.Windows.Forms.Button();
            this.fileBox = new System.Windows.Forms.TextBox();
            this.parserLbl = new System.Windows.Forms.Label();
            this.processBtn = new System.Windows.Forms.Button();
            this.rtfBox = new System.Windows.Forms.RichTextBox();
            this.helpLbl = new System.Windows.Forms.Label();
            this.fileLbl = new System.Windows.Forms.Label();
            this.clearBtn = new System.Windows.Forms.Button();
            this.xmlReaderBtn = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // fileBtn
            // 
            this.fileBtn.Location = new System.Drawing.Point(3, 77);
            this.fileBtn.Name = "fileBtn";
            this.fileBtn.Size = new System.Drawing.Size(72, 23);
            this.fileBtn.TabIndex = 8;
            this.fileBtn.Text = "XML File";
            this.fileBtn.Click += new System.EventHandler(this.fileBtn_Click);
            // 
            // fileDlg
            // 
            this.fileDlg.Filter = "XML Files (*.xml)|*.xml";
            // 
            // parserBtn
            // 
            this.parserBtn.AutoSize = true;
            this.parserBtn.Location = new System.Drawing.Point(3, 7);
            this.parserBtn.Name = "parserBtn";
            this.parserBtn.Size = new System.Drawing.Size(98, 23);
            this.parserBtn.TabIndex = 0;
            this.parserBtn.Text = "Load SAX Parser";
            this.parserBtn.Click += new System.EventHandler(this.parserBtn_Click);
            // 
            // fileBox
            // 
            this.fileBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileBox.Location = new System.Drawing.Point(81, 79);
            this.fileBox.Name = "fileBox";
            this.fileBox.Size = new System.Drawing.Size(369, 20);
            this.fileBox.TabIndex = 9;
            // 
            // parserLbl
            // 
            this.parserLbl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.parserLbl.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.parserLbl.Location = new System.Drawing.Point(107, 10);
            this.parserLbl.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3);
            this.parserLbl.Name = "parserLbl";
            this.parserLbl.Size = new System.Drawing.Size(298, 16);
            this.parserLbl.TabIndex = 1;
            // 
            // processBtn
            // 
            this.processBtn.Location = new System.Drawing.Point(3, 106);
            this.processBtn.Name = "processBtn";
            this.processBtn.Size = new System.Drawing.Size(72, 23);
            this.processBtn.TabIndex = 10;
            this.processBtn.Text = "Process";
            this.processBtn.Click += new System.EventHandler(this.processBtn_Click);
            // 
            // rtfBox
            // 
            this.rtfBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtfBox.Location = new System.Drawing.Point(3, 130);
            this.rtfBox.Name = "rtfBox";
            this.rtfBox.Size = new System.Drawing.Size(447, 296);
            this.rtfBox.TabIndex = 11;
            this.rtfBox.Text = "";
            // 
            // helpLbl
            // 
            this.helpLbl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.helpLbl.Location = new System.Drawing.Point(3, 36);
            this.helpLbl.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.helpLbl.Name = "helpLbl";
            this.helpLbl.Size = new System.Drawing.Size(149, 16);
            this.helpLbl.TabIndex = 3;
            this.helpLbl.Text = "If no SAX parser is chosen, use";
            // 
            // fileLbl
            // 
            this.fileLbl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileLbl.Location = new System.Drawing.Point(80, 64);
            this.fileLbl.Name = "fileLbl";
            this.fileLbl.Size = new System.Drawing.Size(282, 15);
            this.fileLbl.TabIndex = 7;
            this.fileLbl.Text = "An XML file can be found in the Demo\\Basic directory";
            // 
            // clearBtn
            // 
            this.clearBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clearBtn.Location = new System.Drawing.Point(411, 7);
            this.clearBtn.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(39, 23);
            this.clearBtn.TabIndex = 2;
            this.clearBtn.Text = "Clear";
            this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);
            // 
            // xmlReaderBtn
            // 
            this.xmlReaderBtn.AutoSize = true;
            this.xmlReaderBtn.Checked = true;
            this.xmlReaderBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xmlReaderBtn.Location = new System.Drawing.Point(163, 34);
            this.xmlReaderBtn.Name = "xmlReaderBtn";
            this.xmlReaderBtn.Size = new System.Drawing.Size(86, 17);
            this.xmlReaderBtn.TabIndex = 4;
            this.xmlReaderBtn.TabStop = true;
            this.xmlReaderBtn.Text = "XmlReader";
            this.xmlReaderBtn.UseVisualStyleBackColor = true;
            this.xmlReaderBtn.CheckedChanged += new System.EventHandler(this.parser_CheckedChanged);
            // 
            // MainFrm
            // 
            this.ClientSize = new System.Drawing.Size(454, 434);
            this.Controls.Add(this.xmlReaderBtn);
            this.Controls.Add(this.clearBtn);
            this.Controls.Add(this.fileLbl);
            this.Controls.Add(this.helpLbl);
            this.Controls.Add(this.rtfBox);
            this.Controls.Add(this.processBtn);
            this.Controls.Add(this.parserLbl);
            this.Controls.Add(this.parserBtn);
            this.Controls.Add(this.fileBox);
            this.Controls.Add(this.fileBtn);
            this.Name = "MainFrm";
            this.Text = "Basic Use";
            this.Load += new System.EventHandler(this.MainFrm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

    }
    #endregion

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
      Application.EnableVisualStyles();
      Application.ThreadException += new ThreadExceptionEventHandler(HandleException);
      Application.Run(new MainFrm());
    }

    static void HandleException(object sender, ThreadExceptionEventArgs e) {
      MessageBox.Show(e.Exception.Message,
                      "Application Error",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
    }

    private IXmlReader reader;
    private XmlStringIntern intern;

    private void fileBtn_Click(object sender, System.EventArgs e) {
      if (fileDlg.ShowDialog() == DialogResult.OK)
        fileBox.Text = fileDlg.FileName;
    }

    private void parserBtn_Click(object sender, System.EventArgs e) {
      intern = String.Intern;
      pickDlg pickDlg = new pickDlg();
      pickDlg.Reader = reader;
      if (pickDlg.ShowDialog() != DialogResult.OK)
        return;
      try {
        IXmlReader oldReader = reader;
        reader = pickDlg.Reader;
        IDisposable disp = oldReader as IDisposable;
        if (disp != null) disp.Dispose();
      }
      catch (Exception ex) {
        MessageBox.Show(ex.Message,
          "Cannot load parser", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }
      parserLbl.Text = pickDlg.ParserClass;
      if (reader == null)
        return;
      // if we picked ExpatReader, let's set the string table
      Type readerType = reader.GetType();
      if (readerType.FullName == "Kds.Xml.Expat.ExpatReader") {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        PropertyInfo[] props = readerType.GetProperties(flags);
        foreach (PropertyInfo prop in props) {
          if (prop.PropertyType == typeof(Kds.Text.StringTable) && prop.CanWrite) {
            prop.SetValue(reader, strTable, null);
            intern = strTable.Intern;
            break;
          }
        }
      }
    }

    private void clearBtn_Click(object sender, EventArgs e) {
      if (reader != null) {
        IDisposable disp = reader as IDisposable;
        if (disp != null) disp.Dispose();
        reader = null;
        parserLbl.Text = "";
      }
    }

    private bool active = false;

    private void SetupPaths(StdEventDispatcher<EventStatus> dispatcher) {
      Div1Handler div1 = new Div1Handler(rtfBox);
      Div2Handler div2 = new Div2Handler(div1);

      StdPathFactory<EventStatus> pathFactory =
        new StdPathFactory<EventStatus>(null, intern);
      // configure predicates array
      Predicate<XmlPathStep>[] predicates = new Predicate<XmlPathStep>[] {
        delegate(XmlPathStep step) {
          return step.Attributes[new QName("id")].Value != "sec-conformance";
        }
      };
      // no prefix map passed, we are not using QNames
      StdPath<EventStatus> path =
        pathFactory.Create("/spec/*//div1[0]{0}/div2{1}", predicates, div1, div2);
      path.AddTo(dispatcher.MatchTrees);

      /*  building the path without parsing a path expression
        StdMatchNode<EventStatus> parentNode = new StdNamedNode<EventStatus>("spec");
        dispatcher.MatchTrees.Add(parentNode);

        StdMatchNode<EventStatus> childNode = new StdWildCardNode<EventStatus>();  // wild card
        parentNode.Children.Add(childNode);

        parentNode = childNode;
        childNode = new StdNamedNode<EventStatus>("div1", false, div1);  // relative path
        childNode.Predicate = delegate(XmlPathStep step) {
          return step.Attributes[new QName("id")].Value != "sec-conformance";
        };
        parentNode.Children.Add(childNode);

        parentNode = childNode;
        childNode = new StdNamedNode<EventStatus>("div2", div2);
        parentNode.Children.Add(childNode);
      */
    }

    private void processBtn_Click(object sender, System.EventArgs e) {
      if (active)  // this handler is not re-entrant
        return;
      try {
        active = true;
        strTable.Clear();
        rtfBox.Clear();
        FileStream fs = new FileStream(fileBox.Text, FileMode.Open, FileAccess.Read);
        try {
          if (reader != null) {
            SaxDispatcher<EventStatus> disp = new SaxDispatcher<EventStatus>();
            SetupPaths(disp);
            disp.AttachTo(reader);
            InputSource<Stream> input = new InputSource<Stream>(fs);
            reader.Parse(input);
            disp.MatchTrees.Clear();  // not really necessary here
          }
          else if (useXmlReader) {
            XmlReaderDispatcher<EventStatus> disp = new XmlReaderDispatcher<EventStatus>();
            SetupPaths(disp);
            XmlReaderSettings settings = new XmlReaderSettings();
            // ignore the DTD reference
            settings.ProhibitDtd = false;
            settings.ValidationType = ValidationType.None;
            //settings.XmlResolver = null;
            XmlReader parser = XmlReader.Create(fs, settings);
            disp.Parse(parser);
            disp.MatchTrees.Clear();  // not really necessary here
          }
        }
        finally {
          fs.Close();
        }

        // using a file URI hangs on the third or fourth time with NullReferenceException
        // in System.Runtime.Remoting.Proxies.AgileAsyncWorkerItem.ThreadPoolCallBack(Object o)
        // followed by System.Net.WebException with message "The operation has timed-out.".
        // reader.Parse("file://" + fileBox.Text);
      }
      finally {
        active = false;
      }
    }

    private Kds.Text.StringTable strTable = null;

    private void MainFrm_Load(object sender, EventArgs e) {
      strTable = new Kds.Text.StringTable();
    }

    private bool useXmlReader = true;

    private void parser_CheckedChanged(object sender, EventArgs e) {
      useXmlReader = xmlReaderBtn.Checked;
    }
  }

  internal class Div1Handler: BasicXmlElementHandler, IBasicElementHandler
  {
    private int div2Count = 0;
    private RichTextBox rtfBox;
    private string id;

    public Div1Handler(RichTextBox rtfBox) {
      this.rtfBox = rtfBox;
    }

    public void Activate(StdMatchNode<EventStatus> matchedNode, XmlPathStep step, EventStatus status) {
      div2Count = 0;
      id = step.Attributes[new QName("id")].Value;
    }

    public void Deactivate(EventStatus status) {
      rtfBox.AppendText("div section: ");
      if (id != null)
        rtfBox.AppendText(id);
      rtfBox.AppendText(", " + div2Count.ToString() + " subsections" + Environment.NewLine);
    }

    public void IncDiv2() {
      div2Count++;
    }
  }

  internal class Div2Handler: BasicXmlElementHandler, IBasicElementHandler
  {
    private Div1Handler div1Handler;

    public Div2Handler(Div1Handler div1Handler) {
      this.div1Handler = div1Handler;
    }

    public void Deactivate(EventStatus status) {
      div1Handler.IncDiv2();
    }
  }
}
