/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Xml;
using Org.System.Xml.Xpea;
using Org.System.Xml.Xpea.StdImpl;

namespace Org.System.Xml.Xpea.Reader
{
  using XpeaStd = Org.System.Xml.Xpea.StdImpl;

  /// <summary>Processing state of <see cref="XmlReaderDispatcher&lt;S>"/>.</summary>
  public enum ProcessingState
  {
    /// <summary>Ready to start processing.</summary>
    Ready,
    /// <summary>Processing is under way.</summary>
    Parsing,
    /// <summary>Processing is suspended.</summary>
    Suspended
  }

  /// <summary>XPEA adapter for the standard .NET <see cref="XmlReader"/>.</summary>
  /// <remarks>The <see cref="IXmlHandler&lt;S>.SkippedEntity"/> call-back will only
  /// be made for external entities, as the .NET <see cref="XmlReader"/> does
  /// not allow for detecting skipped internal entities.</remarks>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class XmlReaderDispatcher<S>: StdEventDispatcher<S>
    where S: EventStatus, new()
  {
    private S internalStatus;  // internal reference to status object
    private char[] charBuffer;
    private ProcessingState state = ProcessingState.Ready;
    private XmlReader parser;

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

    /// <summary>Initial character buffer size.</summary>
    public const int CharBufferSize = 1024;

    public XmlReaderDispatcher() : base() {
      charBuffer = new char[CharBufferSize];
    }

    /// <summary>Implements additional subclass tasks for
    /// <see cref="StdEventDispatcher&lt;S>.Reset"/></summary>
    public override void Reset() {
      base.Reset();
      state = ProcessingState.Ready;
      parser = null;
    }

    /// <summary><see cref="XmlReader"/> instance passed to <see cref="Parse"/>.</summary>
    /// <remarks>May be <c>null</c> if the <see cref="XmlReaderDispatcher&lt;S>"/>
    /// instance was reset.</remarks>
    public XmlReader Parser {
      get { return parser; }
    }

    /// <summary>Processing state of <see cref="XmlReaderDispatcher&lt;S>"/>.</summary>
    /// <remarks>Unlike push parsers (Expat, SAX), a pull parser does not have a "suspended"
    /// state. As we are converting from pull to push we need to add such a state property.</remarks>
    public ProcessingState State {
      get { return state; }
    }

    private void FinishParsing() {
      XmlReader reader = parser;
      S status = internalStatus;
      // The character buffer may not be filled completely if the last character is the
      // first of a surrogate pair - in which case it will be left for the next buffer.
      int charBufferLimit = charBuffer.Length - 1;
      bool suspend = false;

      while (reader.Read()) {
        int charCount;
        // We suspend at the end of the node event, since we we would loose the rest of 
        // the event data (when resuming) if we stopped immediately.
        if (suspend) {
          state = ProcessingState.Suspended;
          return;
        }
        suspend = false;

        switch (reader.NodeType) {
          // case XmlNodeType.Document: never returned!!!

          case XmlNodeType.Element:
            QName elmName = new QName(reader.LocalName, reader.NamespaceURI);
            string qName = reader.Name;  // save qualified name of element
            int attStart = AttIndex;     // save current attribute index
            AttributeList attList;
            if (reader.HasAttributes) {
              while (reader.MoveToNextAttribute()) {
                PushAttribute(reader.LocalName,
                              reader.NamespaceURI,
                              reader.Name,
                              reader.Value,
                              !reader.IsDefault);
              }
              attList = PushAttributeList(qName, attStart, reader.AttributeCount);
              // reading gets messed up if position is not moved back to element
              reader.MoveToElement();
            }
            else
              attList = PushAttributeList(qName, attStart, 0);
            if (Current != null)
              Current.CallStartChild(elmName, attList, status);
            else if (DocHandler != null) {
              status.Reset();
              DocHandler.StartRoot(elmName, attList, status);
              status.Check();
            }
            switch (status.Code) {
              case EventStatusCode.Suspend:
                suspend = true;
                break;
              case EventStatusCode.Abort:
                goto abort;
              case EventStatusCode.Fatal:
                throw new XpeaException(status.Msg);
            }
            Activate(elmName, attList, status);
            switch (status.Code) {
              case EventStatusCode.Suspend:
                suspend = true;
                break;
              case EventStatusCode.Abort:
                goto abort;
              case EventStatusCode.Fatal:
                throw new XpeaException(status.Msg);
            }
            if (reader.IsEmptyElement)
              goto case XmlNodeType.EndElement;
            else
              break;

          case XmlNodeType.EndElement:
            if (Current == null)
              DocLevelError();
            Deactivate(status);
            switch (status.Code) {
              case EventStatusCode.Suspend:
                suspend = true;
                break;
              case EventStatusCode.Abort:
                goto abort;
              case EventStatusCode.Fatal:
                throw new XpeaException(status.Msg);
            }
            PopAttributeList();
            if (Current != null)
              Current.CallEndChild(status);
            else if (DocHandler != null) {
              status.Reset();
              DocHandler.EndRoot(status);
              status.Check();
            }
            switch (status.Code) {
              case EventStatusCode.Suspend:
                suspend = true;
                break;
              case EventStatusCode.Abort:
                goto abort;
              case EventStatusCode.Fatal:
                throw new XpeaException(status.Msg);
            }
            break;

          case XmlNodeType.CDATA:
          case XmlNodeType.SignificantWhitespace:
          case XmlNodeType.Text:
            if (Current == null)
              DocLevelError();
            if (reader.CanReadValueChunk) {
              do {
                charCount = reader.ReadValueChunk(charBuffer, 0, charBuffer.Length);
                Current.CallCharacters(charBuffer, 0, charCount, status);
                switch (status.Code) {
                  case EventStatusCode.Suspend:
                    suspend = true;
                    break;
                  case EventStatusCode.Abort:
                    goto abort;
                  case EventStatusCode.Fatal:
                    throw new XpeaException(status.Msg);
                }
              } while (charCount >= charBufferLimit);
            }
            else {
              int len = reader.Value.Length;
              int bufLen = charBuffer.Length;
              int indx = 0;
              while (true) {
                charCount = len < bufLen ? len : bufLen;
                reader.Value.CopyTo(indx, charBuffer, 0, charCount);
                Current.CallCharacters(charBuffer, 0, charCount, status);
                switch (status.Code) {
                  case EventStatusCode.Suspend:
                    suspend = true;
                    break;
                  case EventStatusCode.Abort:
                    goto abort;
                  case EventStatusCode.Fatal:
                    throw new XpeaException(status.Msg);
                }
                len -= bufLen;
                if (len <= 0)
                  break;
                indx += charCount;
              }
            }
            break;

          case XmlNodeType.Whitespace:
            // we don't report whitespace before or after root element (at document level)
            if (Current == null)
              break;
            if (reader.CanReadValueChunk) {
              do {
                charCount = reader.ReadValueChunk(charBuffer, 0, charBuffer.Length);
                Current.CallIgnorableWhitespace(charBuffer, 0, charCount, status);
                switch (status.Code) {
                  case EventStatusCode.Suspend:
                    suspend = true;
                    break;
                  case EventStatusCode.Abort:
                    goto abort;
                  case EventStatusCode.Fatal:
                    throw new XpeaException(status.Msg);
                }
              } while (charCount >= charBufferLimit);
            }
            else {
              int len = reader.Value.Length;
              int bufLen = charBuffer.Length;
              int indx = 0;
              while (true) {
                charCount = len < bufLen ? len : bufLen;
                reader.Value.CopyTo(indx, charBuffer, 0, charCount);
                Current.CallIgnorableWhitespace(charBuffer, 0, charCount, status);
                switch (status.Code) {
                  case EventStatusCode.Suspend:
                    suspend = true;
                    break;
                  case EventStatusCode.Abort:
                    goto abort;
                  case EventStatusCode.Fatal:
                    throw new XpeaException(status.Msg);
                }
                len -= bufLen;
                if (len <= 0)
                  break;
                indx += charCount;
              }
            }
            break;

          case XmlNodeType.ProcessingInstruction:
            if (Current != null)
              Current.CallProcessingInstruction(reader.Name, reader.Value, status);
            else if (DocHandler != null) {
              status.Reset();
              DocHandler.ProcessingInstruction(reader.Name, reader.Value, status);
              status.Check();
            }
            switch (status.Code) {
              case EventStatusCode.Suspend:
                suspend = true;
                break;
              case EventStatusCode.Abort:
                goto abort;
              case EventStatusCode.Fatal:
                throw new XpeaException(status.Msg);
            }
            break;

          // seems to be returned for external entities only
          case XmlNodeType.EntityReference:
            if (reader.CanResolveEntity)
              reader.ResolveEntity();
            else {
              Current.CallSkippedEntity(reader.Name, status);
              switch (status.Code) {
                case EventStatusCode.Suspend:
                  suspend = true;
                  break;
                case EventStatusCode.Abort:
                  goto abort;
                case EventStatusCode.Fatal:
                  throw new XpeaException(status.Msg);
              }
            }
            break;

          default:
            break;
        }
      }

      // if we got that far, we are done
      if (DocHandler != null) {
        status.Reset();
        DocHandler.EndDocument(status);
        status.Check();
      }
      // we may want the fatal error exception thrown
      if (status.Code == EventStatusCode.Fatal)
        throw new XpeaException(status.Msg);
    abort:
      state = ProcessingState.Ready;
    }

    /// <summary>Performs XPEA processing using an instance of <see cref="XmlReader"/>.</summary>
    /// <param name="reader"><see cref="XmlReader">Standard XML pull parser to use.</see> instance.</param>
    public void Parse(XmlReader reader) {
      if (reader == null)
        throw new ArgumentNullException("reader");
      if (this.parser != null)
        Reset();
      this.parser = reader;
      CheckStatusObject();
      S status = internalStatus;
      state = ProcessingState.Parsing;

      if (DocHandler != null) {
        status.Reset();
        DocHandler.StartDocument(this, status);
        status.Check();
      }
      switch (status.Code) {
        case EventStatusCode.Suspend:
          state = ProcessingState.Suspended;
          return;
        case EventStatusCode.Abort:
          state = ProcessingState.Ready;
          return;
        case EventStatusCode.Fatal:
          throw new XpeaException(status.Msg);
      }

      FinishParsing();
    }

    /// <summary>Resumes XPEA processing using the instance of <see cref="XmlReader"/>
    /// that was initially passed to <see cref="Parse"/>.</summary>
    public void Resume() {
      if (state != ProcessingState.Suspended)
        throw new XpeaException("Parser not suspended."); //kw resource string!
      CheckStatusObject();
      state = ProcessingState.Parsing;
      FinishParsing();
    }
  }
}
