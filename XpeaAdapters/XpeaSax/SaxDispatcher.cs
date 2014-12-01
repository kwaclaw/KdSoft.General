/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using Org.System.Xml.Sax;
using Org.System.Xml.Xpea;
using Org.System.Xml.Xpea.StdImpl;

namespace Org.System.Xml.Xpea.Sax
{
  using XpeaStd = Org.System.Xml.Xpea.StdImpl;

  /// <summary>XPEA adapter for SAX parsers.</summary>
  /// <remarks>Unlike the other adapters, the SAX adapter does not have a <c>Parse()</c>
  /// or <c>Resume()</c> method. Therefore parsing is driven by calling these methods
  /// on an <see cref="AttachTo">attached</see> SAX parser. This design requires that
  /// <see cref="Reset"/> is called before the SAX adapter can be re-used.</remarks>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class SaxDispatcher<S>: StdEventDispatcher<S>, IContentHandler
    where S: EventStatus, new()
  {
    private S internalStatus;  // internal default status object
    private IContentHandler saxContentHandler;
    private IXmlReader parser;

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

    /// <summary>Implements additional subclass tasks for
    /// <see cref="StdEventDispatcher&lt;S>.Reset"/></summary>
    /// <remarks>Must be called every time before processing begins, unless
    /// the <see cref="SaxDispatcher&lt;S>"/> instance was just created.</remarks>
    public override void Reset() {
      base.Reset();
      internalStatus = null;
    }

    /// <summary>Assigns this <see cref="SaxDispatcher&lt;S>"/> instance to the
    /// <see cref="IXmlReader.ContentHandler"/> property of the <see cref="IXmlReader"/>
    /// argument, saving the old content handler in the
    /// <see cref="SaxDispatcher&lt;S>.ContentHandler"/> property for call forwarding,
    /// unless this property has already been given a non-<c>null</c> value..</summary>
    public void AttachTo(IXmlReader reader) {
      // detach from old parser
      if (parser != null && ReferenceEquals(parser.ContentHandler, this))
        parser.ContentHandler = null;
      parser = reader;
      if (reader != null) {
        if (reader.ContentHandler != null && saxContentHandler == null)
          saxContentHandler = reader.ContentHandler;
        reader.ContentHandler = this;
      }
    }

    /// <summary><see cref="IContentHandler"/> instance that receives call-backs not
    /// used by <see cref="SaxDispatcher&lt;S>"/>.</summary>
    /// <remarks>Only these call-backs are forwarded: 
    /// <see cref="IContentHandler.SetDocumentLocator"/>, 
    /// <see cref="IContentHandler.StartPrefixMapping"/> and
    /// <see cref="IContentHandler.EndPrefixMapping"/>.</remarks>
    public IContentHandler ContentHandler {
      get { return saxContentHandler; }
      set { saxContentHandler = value; }
    }

    /// <summary>Reference to attached <see cref="IXmlReader">SAX parser</see> instance.</summary>
    public IXmlReader Parser {
      get { return parser; }
    }

    /* IContentHandler */

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

    /// <summary>Implements <see cref="IContentHandler.SetDocumentLocator"/>.</summary>
    /// <remarks>Not used for XPEA, forwarded to <see cref="ContentHandler"/> property.</remarks>
    public void SetDocumentLocator(ILocator locator) {
      if (saxContentHandler != null)
        saxContentHandler.SetDocumentLocator(locator);
    }

    /// <summary>Implements <see cref="IContentHandler.StartDocument"/>.</summary>
    public void StartDocument() {
      CheckStatusObject();
      if (DocHandler != null) {
        S status = internalStatus;
        status.Reset();
        DocHandler.StartDocument(this, status);
        status.Check();
        CheckStatus(status);
      }
    }

    /// <summary>Implements <see cref="IContentHandler.EndDocument"/>.</summary>
    public void EndDocument() {
      S status = internalStatus;
      // SAX mandates that EndDocument is always called, even after an error;
      // do not check document level here, as this event could be rightfully
      // called after a fatal error, deep down in the document hierarchy
      if (DocHandler != null) {
        status.Reset();
        DocHandler.EndDocument(status);
        status.Check();
        CheckStatus(status);
      }
    }

    /// <summary>Implements <see cref="IContentHandler.StartPrefixMapping"/>.</summary>
    /// <remarks>Not used for XPEA, forwarded to <see cref="ContentHandler"/> property.</remarks>
    public void StartPrefixMapping(string prefix, string uri) {
      if (saxContentHandler != null)
        saxContentHandler.StartPrefixMapping(prefix, uri);
    }

    /// <summary>Implements <see cref="IContentHandler.EndPrefixMapping"/>.</summary>
    /// <remarks>Not used for XPEA, forwarded to <see cref="ContentHandler"/> property.</remarks>
    public void EndPrefixMapping(string prefix) {
      if (saxContentHandler != null)
        saxContentHandler.EndPrefixMapping(prefix);
    }

    /// <summary>Implements <see cref="IContentHandler.StartElement"/>.</summary>
    public void StartElement(string uri, string localName, string qName, IAttributes atts) {
      QName elmName = new QName(localName, uri);
      int attStart = AttIndex;
      int indx = 0;
      for (; indx < atts.Length; indx++) {
        PushAttribute(atts.GetLocalName(indx),
                      atts.GetUri(indx),
                      atts.GetQName(indx),
                      atts.GetValue(indx),
                      atts.IsSpecified(indx));
      }
      AttributeList attList = PushAttributeList(qName, attStart, indx);
      S status = internalStatus;
      if (Current != null) {
        Current.CallStartChild(elmName, attList, status);
        if (!CheckStatus(status))
          return;
      }
      else if (DocHandler != null) {
        status.Reset();
        DocHandler.StartRoot(elmName, attList, status);
        status.Check();
        if (!CheckStatus(status))
          return;
      }
      Activate(elmName, attList, status);
      CheckStatus(status);
    }

    /// <summary>Implements <see cref="IContentHandler.EndElement"/>.</summary>
    public void EndElement(string uri, string localName, string qName) {
      if (Current == null)
        DocLevelError();
      S status = internalStatus;
      Deactivate(status);
      if (!CheckStatus(status))
        return;

      PopAttributeList();
      if (Current != null) {
        Current.CallEndChild(status);
        CheckStatus(status);
      }
      else if (DocHandler != null) {
        status.Reset();
        DocHandler.EndRoot(status);
        status.Check();
        CheckStatus(status);
      }
    }

    /// <summary>Implements <see cref="IContentHandler.Characters"/>.</summary>
    public void Characters(char[] ch, int start, int length) {
      if (Current == null)
        DocLevelError();
      S status = internalStatus;
      Current.CallCharacters(ch, start, length, status);
      CheckStatus(status);
    }

    /// <summary>Implements <see cref="IContentHandler.IgnorableWhitespace"/>.</summary>
    public void IgnorableWhitespace(char[] ch, int start, int length) {
      if (Current == null)
        DocLevelError();
      S status = internalStatus;
      Current.CallIgnorableWhitespace(ch, start, length, status);
      CheckStatus(status);
    }

    /// <summary>Implements <see cref="IContentHandler.ProcessingInstruction"/>.</summary>
    public void ProcessingInstruction(string target, string data) {
      S status = internalStatus;
      if (Current != null) {
        Current.CallProcessingInstruction(target, data, status);
        CheckStatus(status);
      }
      else if (DocHandler != null) {
        status.Reset();
        DocHandler.ProcessingInstruction(target, data, status);
        status.Check();
        CheckStatus(status);
      }
    }

    /// <summary>Implements <see cref="IContentHandler.SkippedEntity"/>.</summary>
    public void SkippedEntity(string name) {
      S status = internalStatus;
      if (Current != null) {
        Current.CallSkippedEntity(name, status);
        CheckStatus(status);
      }
      else if (DocHandler != null) {
        status.Reset();
        DocHandler.SkippedEntity(name, status);
        status.Check();
        CheckStatus(status);
      }
    }
  }
}