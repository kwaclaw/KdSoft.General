/**************************************************************************
 **           Demo - product inventory report with grouping              **
 **************************************************************************/

using System;
using System.Xml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Kds.Text;
using Org.System.Xml;
using Org.System.Xml.Sax;
using Org.System.Xml.Xpea;
using Org.System.Xml.Xpea.Helpers;
using Org.System.Xml.Xpea.StdImpl;
using Org.System.Xml.Xpea.Sax;
using Org.System.Xml.Xpea.Reader;
using System.Reflection;

namespace Xpea.Demo.Transform
{
  using StdXmlElementHandler = XmlElementHandler<StdMatchNode<TransformStatus>, TransformStatus>;
  using StdTextElementHandler = TextElementHandler<StdMatchNode<TransformStatus>, TransformStatus>;
  using IStdElementHandler = IElementHandler<StdMatchNode<TransformStatus>, TransformStatus>;

  partial class TransformFrm: Form
  {
    private XmlWriterSettings writerSettings;
    private IXmlReader reader;
    private StringTable strTable;
    private XmlStringIntern intern;

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

    public TransformFrm() {
      InitializeComponent();
      strTable = new StringTable();
      writerSettings = new XmlWriterSettings();
      writerSettings.ConformanceLevel = ConformanceLevel.Document;
      writerSettings.Indent = true;
      writerSettings.IndentChars = "  ";
    }

    private void SetupHandlers(StdEventDispatcher<TransformStatus> disp, XmlWriter writer) {
      RegionHandler region = new RegionHandler(writer);
      ProductHandler product = new ProductHandler(region);
      ProductIdHandler prodId = new ProductIdHandler(product);
      ProductNameHandler prodName = new ProductNameHandler(product);
      ProductPriceHandler prodPrice = new ProductPriceHandler(product);
      ProductQuantityHandler prodQuantity = new ProductQuantityHandler(product);

      // no prefix map passed, we are not using QNames
      StdPathFactory<TransformStatus> pathFactory =
        new StdPathFactory<TransformStatus>(null, intern);

      // create a path first, then add extra nodes to make it a tree
      StdPath<TransformStatus> path =
        pathFactory.Create("//region{0}/product{1}/prodid{2}", region, product, prodId);
      StdMatchNode<TransformStatus> prodNode = path.Leaf.Parent;
      // by default, a node will be absolute unless specified otherwise
      StdMatchNode<TransformStatus> tmpNode =
        new StdNamedNode<TransformStatus>("name", prodName);
      prodNode.Children.Add(tmpNode);
      tmpNode = new StdNamedNode<TransformStatus>("price", prodPrice);
      prodNode.Children.Add(tmpNode);
      tmpNode = new StdNamedNode<TransformStatus>("quantity", prodQuantity);
      prodNode.Children.Add(tmpNode);
      path.AddTo(disp.MatchTrees);  // same as disp.MatchTrees.Add(path.Root);
    }

    private bool active = false;

    private void transformBtn_Click(object sender, EventArgs e) {
      if (active)  // don't want recursive calls
        return;
      try {
        active = true;
        outputBrowser.Url = null;
        FileStream inStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
        try {
          FileStream outStream = new FileStream(outputFile, FileMode.Create);
          XmlWriter writer = XmlWriter.Create(outStream, writerSettings);
          try {
            if (reader != null) {
              SaxDispatcher<TransformStatus> disp = new SaxDispatcher<TransformStatus>();
              disp.DocHandler = new DocumentHandler(writer);
              SetupHandlers(disp, writer);
              disp.AttachTo(reader);
              InputSource<Stream> input = new InputSource<Stream>(inStream);
              reader.Parse(input);
              disp.MatchTrees.Clear();  // not really necessary here
            }
            else if (useXmlReader) {
              XmlReaderDispatcher<TransformStatus> disp = new XmlReaderDispatcher<TransformStatus>();
              disp.DocHandler = new DocumentHandler(writer); 
              SetupHandlers(disp, writer);
              XmlReaderSettings settings = new XmlReaderSettings();
              // ignore the DTD reference
              settings.ProhibitDtd = false;
              settings.ValidationType = ValidationType.None;
              settings.XmlResolver = null;
              XmlReader parser = XmlReader.Create(inStream, settings);
              disp.Parse(parser);
              disp.MatchTrees.Clear();  // not really necessary here
            }
          }
          finally {
            writer.Close();
            outStream.Close();
          }
        }
        finally {
          inStream.Close();
        }
        outputBrowser.Url = new Uri("file:///" + outputFile);
      }
      finally {
        active = false;
      }
    }

    private DirectoryInfo transformDir = null;
    string inputFile;
    string outputFile;

    private void transformFrm_Load(object sender, EventArgs e) {
      Assembly assem = Assembly.GetExecutingAssembly();
      string assemDir = assem.Location;
      DirectoryInfo parentDir = Directory.GetParent(assemDir);
      parentDir = parentDir.Parent;
      transformDir = parentDir.Parent;

      inputFile = Path.Combine(transformDir.FullName, "Products.xml");
      outputFile = Path.Combine(transformDir.FullName, "Products.html");
      inputBrowser.Url = new Uri("file:///" + inputFile);
    }

    private bool useXmlReader = true;

    private void xmlReaderBtn_CheckedChanged(object sender, EventArgs e) {
      useXmlReader = xmlReaderBtn.Checked;
    }
  }

  /* XPEA Handlers */

  internal class TransformStatus: EventStatus
  {
    // allow write access to message and code
    public void SetCodeMsg(EventStatusCode value, string msg) {
      Code = value;
      Msg = msg;
    }
  }

  // initializes transformation and processes root element
  internal class DocumentHandler: XmlDocHandler<TransformStatus>, IDocHandler<TransformStatus>
  {
    private XmlWriter writer;

    public DocumentHandler(XmlWriter writer) {
      this.writer = writer;
    }

    public void StartDocument(object context, TransformStatus status) {
      const string HTML = "html",
                   PUBID = "-//W3C//DTD XHTML 1.0 Strict//EN",
                   SYSID = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd";
      string[] style = new string[] {
		    "H1 {COLOR: red; FONT-FAMILY: Arial; FONT-SIZE: 14pt;}",
		    "H2 {COLOR: darkblue; FONT-FAMILY: Arial; FONT-SIZE: 12pt;}",
		    "H3 {COLOR: red; FONT-FAMILY: Arial; FONT-SIZE: 10pt;}",
  	    ".subhead {COLOR: darkblue; FONT-FAMILY: Arial; FONT-SIZE: 12pt;}",
  	    ".text {COLOR: black; FONT-FAMILY: Arial; FONT-SIZE: 12pt;}",
		    "TH {COLOR: white; FONT-FAMILY: Arial; background-color: darkblue;}",
		    "TD {COLOR: blue; FONT-FAMILY: Arial}",
		    "TR { background-color: beige;}",
		    "BODY { background-color: beige;}"
      };

      // we are generating XHTML
      writer.WriteStartDocument();
      writer.WriteDocType(HTML, PUBID, SYSID, null);
      writer.WriteStartElement("html");
      writer.WriteStartElement("body");
      writer.WriteStartElement("STYLE");
      // takes care of proper indentation
      for (int indx = 0; indx < style.Length; indx++) {
        writer.WriteWhitespace("\n      ");
        writer.WriteString(style[indx]);
      }
      writer.WriteEndElement();
      writer.WriteElementString("h1", "Product information for different regions");
    }

    public void EndDocument(TransformStatus status) {
      writer.WriteEndElement();
      writer.WriteEndElement();
      writer.WriteEndDocument();
    }

    public void StartRoot(QName name, IAttributeList attributes, TransformStatus status) {
      writer.WriteStartElement("span");
      writer.WriteAttributeString("class", "subhead");
      writer.WriteString("Inventory Listing");
      writer.WriteEndElement();
      writer.WriteElementString("br", "");
      writer.WriteElementString("br", "");
    }
  }

  internal struct Product
  {
    public string Id;
    public string Name;
    public string Price;
    public string Quantity;

    public static int Compare(Product p1, Product p2) {
      return string.Compare(p1.Name, p2.Name);
    }
  }

  // processes 'region' elements
  internal class RegionHandler: StdXmlElementHandler, IStdElementHandler
  {
    private XmlWriter writer;
    private List<Product> products;
    private int prodCount;
    private int quantity;

    public RegionHandler(XmlWriter writer) {
      this.writer = writer;
      products = new List<Product>();
    }

    public void Activate(StdMatchNode<TransformStatus> matchedNode, XmlPathStep step, TransformStatus status) {
      products.Clear();
      prodCount = 0;
      quantity = 0;
      writer.WriteStartElement("span");
      writer.WriteAttributeString("class", "subhead");
      writer.WriteString("Region");
      writer.WriteEndElement();
      writer.WriteStartElement("span");
      writer.WriteAttributeString("class", "text");
      int attIndx = step.Attributes.GetIndex(new QName("area"));
      if (attIndx < 0)
        status.SetCodeMsg(EventStatusCode.Error, "Attribute not found: 'area'");
      else
        writer.WriteString(step.Attributes[attIndx].Value);
      writer.WriteEndElement();
      writer.WriteElementString("br", "");
      writer.WriteElementString("br", "");
      writer.WriteStartElement("table");
      writer.WriteAttributeString("border", "1");
      writer.WriteStartElement("tr");
      writer.WriteElementString("th", "ID");
      writer.WriteElementString("th", "Name");
      writer.WriteElementString("th", "Price");
      writer.WriteElementString("th", "Quantity");
      writer.WriteEndElement();
    }

    public void Deactivate(TransformStatus status) {
      string nl = Environment.NewLine;
      products.Sort(Product.Compare);
      foreach (Product prod in products) {
        writer.WriteStartElement("tr");
        writer.WriteElementString("td", prod.Id);
        writer.WriteElementString("td", prod.Name);
        writer.WriteElementString("td", prod.Price);
        writer.WriteElementString("td", prod.Quantity);
        writer.WriteEndElement();
      }
      writer.WriteEndElement();
      writer.WriteStartElement("p");
      writer.WriteString("Total Products in region: " + prodCount.ToString());
      writer.WriteElementString("br", "");
      writer.WriteString("Total Inventory in region: " + quantity.ToString());
      writer.WriteEndElement();
      writer.WriteElementString("br", "");
    }

    internal void AddProduct(ref Product prod, int qty) {
      products.Add(prod);
      quantity += qty;
      prodCount++;
    }
  }

  // aggregates product details and adds complete product instance to list
  internal class ProductHandler: StdXmlElementHandler, IStdElementHandler
  {
    private RegionHandler region;
    internal Product product;
    internal int quantity;

    public ProductHandler(RegionHandler region) {
      this.region = region;
    }

    public void Deactivate(TransformStatus status) {
      region.AddProduct(ref product, quantity);
    }
  }

  // processes 'prodid' elements
  internal class ProductIdHandler: StdTextElementHandler
  {
    private ProductHandler prodHandler;

    public ProductIdHandler(ProductHandler prodHandler) {
      this.prodHandler = prodHandler;
    }

    protected override void OnDeactivate(TransformStatus status) {
      prodHandler.product.Id = CopyBuffer();
    }
  }

  // processes 'name' elements
  internal class ProductNameHandler: StdTextElementHandler
  {
    private ProductHandler prodHandler;

    public ProductNameHandler(ProductHandler prodHandler) {
      this.prodHandler = prodHandler;
    }

    protected override void OnDeactivate(TransformStatus status) {
      prodHandler.product.Name = CopyBuffer();
    }
  }

  // processes 'price' elements
  internal class ProductPriceHandler: StdTextElementHandler
  {
    private ProductHandler prodHandler;

    public ProductPriceHandler(ProductHandler prodHandler) {
      this.prodHandler = prodHandler;
    }

    protected override void OnDeactivate(TransformStatus status) {
      prodHandler.product.Price = CopyBuffer();
    }
  }

  // processes 'quantity' elements
  internal class ProductQuantityHandler: StdTextElementHandler
  {
    private ProductHandler prodHandler;

    public ProductQuantityHandler(ProductHandler prodHandler) {
      this.prodHandler = prodHandler;
    }

    protected override void OnDeactivate(TransformStatus status) {
      string qtyStr = CopyBuffer();
      int qty;
      if (int.TryParse(qtyStr, out qty))
        prodHandler.quantity = qty;
      else {
        prodHandler.quantity = 0;
        status.SetCodeMsg(EventStatusCode.Error, "Non-numeric quantity");
      }
      // save it anyway
      prodHandler.product.Quantity = qtyStr;
    }
  }
}