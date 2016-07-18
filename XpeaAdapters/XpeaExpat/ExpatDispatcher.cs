/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Org.System.Xml;
using Org.System.Xml.Xpea.StdImpl;
using Kds.Xml.Expat;
using Kds.Text;

namespace Org.System.Xml.Xpea.Expat
{
  /// <summary><see cref="StdEntityParseContext&lt;E, X, U>"/> subclass specific to
  /// XPEA processing.</summary>
  /// <remarks><see cref="StdEntityParseContext&lt;E, X, U>"/> can only be subclassed
  /// together with <see cref="StdExpatParser&lt;X, E, U>"/>.</remarks>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class ExpatEntityParseContext<S>: StdEntityParseContext<ExpatEntityParseContext<S>, XpeaExpatParser<S>, ExpatDispatcher<S>>
    where S: EventStatus, new()
  {
    // for use with XpeaExpatParser
    public ExpatEntityParseContext() { }
  }

  /// <summary><see cref="StdExpatParser&lt;X, E, U>"/> subclass specific to
  /// XPEA processing.</summary>
  /// <remarks>Subclassing and binding type parameters allows for type-safe 
  /// <see cref="ExpatParser&lt;X, E, U>.UserData"/> access in call-backs.</remarks>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class XpeaExpatParser<S>: StdExpatParser<XpeaExpatParser<S>, ExpatEntityParseContext<S>, ExpatDispatcher<S>>
    where S: EventStatus, new()
  {
    public XpeaExpatParser(string encoding, bool namespaces, ExpatDispatcher<S> userData)
      : base(encoding, namespaces, userData) 
    { }
  }

  /// <summary>XPEA adapter for the Expat XML parser.</summary>
  /// <remarks>Processing must be done by calling the <see cref="Parse(ReadBuffer)"/> or
  /// <see cref="Resume"/> methods of the Expat adapter, not the equivalent methods
  /// of the underlying <see cref="ExpatParser&lt;X, E, U>">Expat parser</see>.</remarks>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public unsafe class ExpatDispatcher<S>: StdEventDispatcher<S>
    where S: EventStatus, new()
  {
    private StringTable strTable;
    private XMLSkippedEntityHandler skippedPEHandler;
    private S internalStatus;  // internal default status object
    private char[] charBuffer;
    private XpeaExpatHandlers expatHandlers;
    private XpeaExpatParser<S> parser;

    /// <summary>Initial character buffer size.</summary>
    public const int InitCharBufferSize = 256;

    public ExpatDispatcher(StringTable strTable) : base() {
      if (strTable == null)
        throw new ArgumentNullException("strTable");
      this.strTable = strTable;
      charBuffer = new char[InitCharBufferSize];
      expatHandlers = new XpeaExpatHandlers(this);
    }

    public ExpatDispatcher() : this(new StringTable()) { }

    /// <summary>Implements additional subclass tasks for
    /// <see cref="StdEventDispatcher&lt;S>.Reset"/></summary>
    public override void Reset() {
      base.Reset();
      internalStatus = null;
    }

    /// <summary>Reference to attached <see cref="XpeaExpatParser&lt;S>">Expat parser</see> instance.</summary>
    public XpeaExpatParser<S> Parser {
      get { return parser; }
    }

    /// <summary>Hash table used for string interning.</summary>
    public StringTable StrTable {
      get { return strTable; }
    }

    /// <summary>Gets or sets event handler for skipped parameter entity events.</summary>
    /// <remarks><see cref="AttachTo"/> already sets such a handler on the Expat parser,
    /// however, it ignores skipped parameter entities. If one is interested in these ignored
    /// events, one can set this handler.</remarks>
    public XMLSkippedEntityHandler SkippedParamEntityHandler {
      get { return skippedPEHandler; }
      set { skippedPEHandler = value; }
    }

    /// <summary>Copies the <see cref="StdEventDispatcher&lt;S>.StatusObject"/>
    /// internally, creating a new (private) instance if it is <c>null</c>.</summary>
    /// <remarks>The type of the internally created instance is determined
    /// by the generic type parameter <c>S</c>.</remarks>
    protected void CheckStatusObject() {
      if (base.StatusObject == null)
        internalStatus = new S();
      else
        internalStatus = base.StatusObject;
    }

    // Configures Expat parser for calling back to this instance. 
    // No check for null argument!
    private void AttachToExpat(XpeaExpatParser<S> parser) {
      parser.UserData = this;
      // parser.Namespaces = true;
      parser.AttlistDeclHandler = XpeaExpatHandlers.AttlistDeclHandlerImpl;
      parser.StartElementHandler = XpeaExpatHandlers.StartElementHandlerImpl;
      parser.EndElementHandler = XpeaExpatHandlers.EndElementHandlerImpl;
      parser.CharacterDataHandler = XpeaExpatHandlers.CharacterDataHandlerImpl;
      parser.ProcessingInstructionHandler = XpeaExpatHandlers.ProcessingInstructionHandlerImpl;
      parser.SkippedEntityHandler = XpeaExpatHandlers.SkippedEntityHandlerImpl;
      this.parser = parser;
    }

    /// <summary>Configures an <see cref="XpeaExpatParser&lt;S>"/> instance for calling back
    /// into the XPEA dispatcher.</summary>
    /// <remarks>Call this as the last step before parsing starts, to avoid overriding call-backs
    /// needed by XPEA. One exception: if the <see cref="StdEventDispatcher&lt;S>.AttDecls"/>
    /// dictionary does not need to be filled (that is, when the <see cref="IAttribute.Type">
    /// attribute type</see> is not needed), then one can clear the
    /// <see cref="ExpatParser&lt;X, E, U>.AttlistDeclHandler"/>.</remarks>
    /// <param name="parser"><see cref="StdExpatParser"/> instance to be configured.</param>
    public void AttachTo(XpeaExpatParser<S> parser) {
      if (parser == null)
        throw new ArgumentNullException("parser");
      AttachToExpat(parser);
    }

    /// <summary>Creates a new <see cref="XpeaExpatParser&lt;S>"/> instance configured for
    /// calling back into the XPEA dispatcher.</summary>
    /// <param name="encoding">Encoding argument for <see cref="XpeaExpatParser&lt;S>"/> constructor.</param>
    /// <param name="namespaces">Namespaces argument for <see cref="XpeaExpatParser&lt;S>"/> constructor.</param>
    public XpeaExpatParser<S> CreateAttachedParser(string encoding, bool namespaces) {
      XpeaExpatParser<S> result = new XpeaExpatParser<S>(encoding, namespaces, this);
      AttachToExpat(result);
      return result;
    }

    /// <summary>Adds subclass tasks to <see cref="StdEventDispatcher&lt;s>.ResetResources"/>.</summary>
    public override void ResetResources() {
      Array.Resize<char>(ref charBuffer, InitCharBufferSize);
      base.ResetResources();
    }

    private bool CheckStatus(S status) {
      switch (status.Code) {
        case EventStatusCode.Suspend:
          if (parser != null)
            parser.Suspend();
          return true;
        case EventStatusCode.Abort:
          if (parser != null)
            parser.Abort();
          return false;
        case EventStatusCode.Fatal:
          throw new XpeaException(status.Msg);
        default:
          return true;
      }
    }

    /// <summary>Performs XPEA processing using the attached instance of an Expat parser
    /// where input data is read through a generic <see cref="ReadBuffer"/> delegate.</summary>
    /// <remarks>Do not call the the <see cref="ExpatParser&lt;X, E, U>.Parse"/> method
    /// directly on the <see cref="ExpatParser&lt;X, E, U>">Expat parser</see>, as this
    /// will not perform correctly. Specifically, the <see cref="IDocHandler&lt;S>.StartDocument"/>
    /// and <see cref="IDocHandler&lt;S>.EndDocument"/> would not be called, as
    /// <see cref="ExpatParser&lt;X, E, U>"/> provides no call-backs for that.</remarks>
    /// <param name="read">Call-back delegeate to read the XML document from.</param>
    /// <returns>Status of parsing process.</returns>
    public ParseStatus Parse(ReadBuffer read) {
      if (read == null)
        throw new ArgumentNullException("read");
      if (parser == null)
        throw new ArgumentNullException("No parser attached.");  //kw resource string
      if (internalStatus != null)
        Reset();
      CheckStatusObject();
      S status = internalStatus;
      if (DocHandler != null) {
        status.Reset();
        DocHandler.StartDocument(this, status);
        status.Check();
        if (!CheckStatus(status))
          return ParseStatus.Aborted;
      }
      ParseStatus ps = parser.Parse(read);
      if (ps == ParseStatus.Finished  && DocHandler != null) {
        status.Reset();
        DocHandler.EndDocument(status);
        status.Check();
        // we may want the fatal error exception thrown
        if (status.Code == EventStatusCode.Fatal)
          throw new XpeaException(status.Msg);
      }
      return ps;
    }

    /// <summary>Performs XPEA processing of an input <see cref="Stream"/> using the
    /// attached instance of an Expat parser.</summary>
    /// <remarks>Do not call the the <see cref="ExpatParser&lt;X, E, U>.Parse"/> method
    /// directly on the <see cref="ExpatParser&lt;X, E, U>">Expat parser</see>, as this
    /// will not perform correctly. Specifically, the <see cref="IDocHandler&lt;S>.StartDocument"/>
    /// and <see cref="IDocHandler&lt;S>.EndDocument"/> would not be called, as
    /// <see cref="ExpatParser&lt;X, E, U>"/> provides no call-backs for that.</remarks>
    /// <param name="stream">Input <see cref="Stream"/> to read the XML document from.</param>
    /// <returns>Status of parsing process.</returns>
    public ParseStatus Parse(Stream stream) {
      StreamBufferReader sbr = new StreamBufferReader(stream);
      return Parse(sbr.Read);
    }

    /// <summary>Performs XPEA processing of a <see cref="TextReader"/> using the
    /// attached instance of an Expat parser.</summary>
    /// <remarks>Do not call the the <see cref="ExpatParser&lt;X, E, U>.Parse"/> method
    /// directly on the <see cref="ExpatParser&lt;X, E, U>">Expat parser</see>, as this
    /// will not perform correctly. Specifically, the <see cref="IDocHandler&lt;S>.StartDocument"/>
    /// and <see cref="IDocHandler&lt;S>.EndDocument"/> would not be called, as
    /// <see cref="ExpatParser&lt;X, E, U>"/> provides no call-backs for that.</remarks>
    /// <param name="reader"><see cref="TextReader"/> to read the XML document from.</param>
    /// <returns>Status of parsing process.</returns>
    public ParseStatus Parse(TextReader reader) {
      TextBufferReader tbr = new TextBufferReader(reader);
      return Parse(tbr.Read);
    }

    /// <summary>Resumes XPEA processing using the attached instance of an Expat parser.</summary>
    /// <remarks>This can be called only when the
    /// <c><see cref="ExpatParser&lt;X, E, U>.ParsingStatus"/>.Parsing</c> property has
    /// a value of <see cref="XMLParsing.SUSPENDED"/>.</remarks>
    /// <returns>Status of parsing process.</returns>
    public ParseStatus Resume() {
      if (parser == null)
        throw new ArgumentNullException("No parser attached.");  //kw resource string
      CheckStatusObject();
      S status = internalStatus;
      ParseStatus ps = parser.Resume();
      if (ps == ParseStatus.Finished && DocHandler != null) {
        status.Reset();
        DocHandler.EndDocument(status);
        status.Check();
        // we may want the fatal error exception thrown
        if (status.Code == EventStatusCode.Fatal)
          throw new XpeaException(status.Msg);
      }
      return ps;
    }

    // adds attributes to internal stack
    private int AddAttributes(char** atts, int specAttCount) {
      int specCount = specAttCount >> 1;
      int indx = 0;
      while (*atts != null) {
        string localName, nsUri, qualName;
        ExpatUtils.ParseNameSax(*atts, StrTable, out nsUri, out localName, out qualName);
        atts++;
        // we don't intern attribute values, but we we check if they are pre-loaded
        string value = *atts == null ? null : StrTable.Find(*atts);
        if (value == null)
          value = new string(*atts);
        PushAttribute(localName, nsUri, qualName, value, indx < specCount);
        atts++;
        indx++;
      }
      return indx;
    }

    /* Nested class containing all Expat handlers, call-back argument for Expat. */

    internal unsafe class XpeaExpatHandlers
    {
      private ExpatDispatcher<S> dispatcher;

      public XpeaExpatHandlers(ExpatDispatcher<S> dispatcher) {
        this.dispatcher = dispatcher;
      }

      public ExpatDispatcher<S> Dispatcher {
        get { return dispatcher; }
      }

      /* AttlistDeclHandler */

      /* Note: DTDs don't know about namespaces, therefore the elName and attName
       * arguments have to be taken literally.
       */
      public static void
      AttlistDeclHandlerImpl(IntPtr userData,
                             char* elName,
                             char* attName,
                             char* attType,
                             char* dflt,
                             int isRequired) 
      {
        ExpatDispatcher<S> disp = ((XpeaExpatParser<S>)((GCHandle)userData).Target).UserData;
        StringTable strTable = disp.StrTable;
        // StringTable.Intern never returns an emtpy string
        string elNameStr = strTable.Intern(elName);
        string attNameStr = strTable.Intern(attName);
        AttDeclKey declKey = new AttDeclKey(elNameStr, attNameStr);
        if (!disp.AttDecls.ContainsKey(declKey)) {
          string attTypeStr = strTable.Intern(attType);
          string dfltStr = strTable.Intern(dflt);
          disp.AttDecls[declKey] = new AttDecl(attTypeStr, dfltStr, isRequired != 0);
        }
        //kw maybe forward call to a second level handler, since not all data is processed
        // or pass AttDecls table in from outside, like StringTable; in this case we would
        // not need to pass an AttlistDeclHandler to Expat - it would be the apps responsibility
        // to load AttDecls up with attribute list declarations
      }

      /* StartElementHandler */

      /* Note: the atts argument is only valid for this call */
      public static void
      StartElementHandlerImpl(IntPtr userData, char* name, char** atts) {
        XpeaExpatParser<S> parser = (XpeaExpatParser<S>)((GCHandle)userData).Target;
        ExpatDispatcher<S> disp = parser.UserData;
        string uri, locName, qName;
        int specAttCount = parser.SpecifiedAttributeCount;
        ExpatUtils.ParseNameSax(name, disp.StrTable, out uri, out locName, out qName);

        S status = disp.internalStatus;
        StdPathStep currStep = disp.Current;
        QName elmName = new QName(locName, uri);

        int attStart = disp.AttIndex;
        int attCount = disp.AddAttributes(atts, specAttCount);
        StdEventDispatcher<S>.AttributeList attList =
          disp.PushAttributeList(qName, attStart, attCount);
        if (currStep != null) {
          currStep.CallStartChild(elmName, attList, status);
          if (!disp.CheckStatus(status))
            return;
        }
        else if (disp.DocHandler != null) {
          status.Reset();
          disp.DocHandler.StartRoot(elmName, attList, status);
          status.Check();
          if (!disp.CheckStatus(status))
            return;
        }
        disp.Activate(elmName, attList, status);  // updates disp.Current
        disp.CheckStatus(status);
      }

      /* EndElementHandler */

      public static void
      EndElementHandlerImpl(IntPtr userData, char* name) {
        ExpatDispatcher<S> disp = ((XpeaExpatParser<S>)((GCHandle)userData).Target).UserData;
        StdPathStep currStep = disp.Current;
        if (currStep == null)
          disp.DocLevelError();
        S status = disp.internalStatus;
        disp.Deactivate(status);  // updates disp.Current
        if (!disp.CheckStatus(status))
          return;

        disp.PopAttributeList();
        currStep = disp.Current;
        if (currStep != null) {
          currStep.CallEndChild(status);
          disp.CheckStatus(status);
        }
        else if (disp.DocHandler != null) {
          status.Reset();
          disp.DocHandler.EndRoot(status);
          status.Check();
          disp.CheckStatus(status);
        }
      }

      /* CharacterDataHandler */

      public static void
      CharacterDataHandlerImpl(IntPtr userData, char* s, int len) {
        ExpatDispatcher<S> disp = ((XpeaExpatParser<S>)((GCHandle)userData).Target).UserData;
        if (disp.charBuffer.Length < len)
          Array.Resize<char>(ref disp.charBuffer, len);
        Marshal.Copy((IntPtr)s, disp.charBuffer, 0, len);

        StdPathStep currStep = disp.Current;
        if (currStep == null)
          disp.DocLevelError();
        S status = disp.internalStatus;
        currStep.CallCharacters(disp.charBuffer, 0, len, status);
        disp.CheckStatus(status);
      }

      /* ProcessingInstructionHandler */

      /*  target and data are null terminated */
      public static void
      ProcessingInstructionHandlerImpl(IntPtr userData, char* target, char* data) {
        ExpatDispatcher<S> disp = ((XpeaExpatParser<S>)((GCHandle)userData).Target).UserData;
        StringTable strTable = disp.StrTable;
        string targetStr = strTable.Intern(target);
        string dataStr = (data == null) ? null : strTable.Intern(data);

        StdPathStep currStep = disp.Current;
        S status = disp.internalStatus;
        if (currStep != null) {
          currStep.CallProcessingInstruction(targetStr, dataStr, status);
          disp.CheckStatus(status);
        }
        else if (disp.DocHandler != null) {
          status.Reset();
          disp.DocHandler.ProcessingInstruction(targetStr, dataStr, status);
          status.Check();
          disp.CheckStatus(status);
        }
      }

      /* SkippedEntityHandler */

      public static void
      SkippedEntityHandlerImpl(IntPtr userData, char* entityName, int isParameterEntity) {
        ExpatDispatcher<S> disp = ((XpeaExpatParser<S>)((GCHandle)userData).Target).UserData;
        string entityNameStr = disp.StrTable.Intern(entityName);

        // we forward skipped parameter entity events
        if (isParameterEntity != 0) {
          if (disp.skippedPEHandler != null)
            disp.skippedPEHandler(userData, entityName, isParameterEntity);
          return;
        }

        StdPathStep currStep = disp.Current;
        S status = disp.internalStatus;
        if (currStep != null) {
          currStep.CallSkippedEntity(entityNameStr, status);
          disp.CheckStatus(status);
        }
        else if (disp.DocHandler != null) {
          status.Reset();
          disp.DocHandler.SkippedEntity(entityNameStr, status);
          status.Check();
          disp.CheckStatus(status);
        }
      }
    }
  }
}
