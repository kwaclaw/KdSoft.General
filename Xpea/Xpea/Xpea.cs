/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Org.System.Xml.Xpea
{
  /// <summary>Defines constants for the <see cref="Org.System.Xml.Xpea"/> namespace.</summary>
  public class Constants
  {
    private Constants() { }

    // public const int MaxPathLength = 64;
  }

  /// <summary>Indicates errors in XPEA processing or in how XPEA is used.</summary>
  public class XpeaException: ApplicationException
  {
    public XpeaException() { }

    public XpeaException(string message) : base(message) { }

    public XpeaException(string message, Exception e) : base(message, e) { }
  }

  /// <summary>Basic code describing status of handler call.</summary>
  public enum EventStatusCode
  {
    /// <summary>No problem.</summary>
    OK,
    /// <summary>Minor irregularity, processing can continue.</summary>
    Warning,
    /// <summary>Major irregularity, but processing can continue.</summary>
    Error,
    /// <summary>Fatal error, processing must stop. Will cause exception.</summary>
    /// <remarks>All remaining call-backs for the current event will still be made.</remarks>
    Fatal,
    /// <summary>Processing is to be stopped, but can be resumed with the next event.</summary>
    /// <remarks>All remaining call-backs for the current event will still be made, and
    /// possibly more if an event would get lost when resuming (e.g. end of element event
    /// for an empty element tag).</remarks>
    Suspend,
    /// <summary>Processing is to be aborted without the need to resume.</summary>
    /// <remarks>All remaining call-backs for the current event will still be made.</remarks>
    Abort
  }

  /// <summary>Instances of this class are passed to most handlers, allowing
  /// the implementation to pass back status and error information.</summary>
  /// <remarks>Subclass for application specific behaviour on return from 
  /// handler call-back.</remarks>
  public class EventStatus
  {
    private EventStatusCode code = EventStatusCode.OK;
    private string msg = String.Empty;

    /// <summary>The status message.</summary>
    /// <remarks>Generally set from within a call-back handler. Write access to this
    /// property is protected so that one can control (through sub-classing) whether
    /// modifications should be allowed.</remarks>
    public string Msg {
      get { return msg; }
      protected set { msg = value; }
    }

    /// <summary>Called by parser implementation on return from event handler.</summary>
    /// <remarks>Called as first step immediately after return from handler call-back.
    /// Override for application specific processing. For instance, the values
    /// of <see cref="Msg"/> or <see cref="Code"/> could be modified.</remarks>
    public virtual void Check() {
      // do nothing by default
    }

    /// <summary>Status code of event.</summary>
    /// <remarks>Generally set from within a call-back handler. Write access to this
    /// property is protected so that one can control (through sub-classing) whether
    /// modifications should be allowed. If the <see cref="Msg"/> property is empty,
    /// a default message related to the code will be created.</remarks>
    public EventStatusCode Code {
      get { return code; }
      protected set {
        code = value;
        if (value == EventStatusCode.Fatal || value == EventStatusCode.Error) {
          if (Msg == "")
            Msg = Resources.GetString(RsId.DefErrorMsg);
        }
      }
    }

    /// <summary>Resets status object to initial state. Should be called by an
    /// actual XPEA dispatcher implementation once before every event (not once
    /// for each call-back, even if there are multiple call-backs on that event).</summary>
    /// <remarks>Override if subclass adds its own state. The implementation
    /// must call <c>base.Reset()</c>.</remarks>
    public virtual void Reset() {
      code = EventStatusCode.OK;
      msg = "";
    }
  }

  /// <summary>Represents an XML attribute.</summary>
  public interface IAttribute
  {
    /// <summary>Namespace qualified name of attribute.</summary>
    QName Name { get; }
    /// <summary>Attribute value.</summary>
    string Value { get; }
    /// <summary>Attribute's type.</summary>
    /// <remarks>If no attribute declaration was processed then this will
    /// return "UNDECLARED".</remarks>
    string Type { get; }
    /// <summary>Returns <c>true</c> unless the attribute value was provided
    /// by DTD defaulting.</summary>
    bool IsSpecified { get; }
  }

  /// <summary>Represents an element's list of XML attributes.</summary>
  public interface IAttributeList
  {
    /// <summary>Number of attributes in the list.</summary>
    int Count { get; }
    /// <summary>Index of attribute with a given <see cref="QName"/>.</summary>
    /// <param name="name">Name of attribute to find.</param>
    /// <returns>Index of attribute with the given name, or <c>-1</c> if
    /// no such attribute exists.</returns>
    int GetIndex(QName name);
    //kw do we need this if we are not interested in prefixes?
    /// <summary>Index of attribute with a given prefixed name.</summary>
    /// <param name="qName">Prefixed (namespace qualified) name of attribute.</param>
    /// <returns>Index of attribute with the given name, or <c>-1</c> if
    /// no such attribute exists.</returns>
    int GetIndex(string qName);
    /// <summary>Retrieves attribute by index.</summary>
    /// <param name="index">List index of attribute.</param>
    /// <returns><see cref="IAttribute"/> reference to attribute.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when an invalid index is passed.</exception>
    IAttribute this[int index] { get; }
    /// <summary>Retrieves attribute with a given <see cref="QName"/>.</summary>
    /// <param name="name">Qualified name of attribute.</param>
    /// <returns><see cref="IAttribute"/> reference to attribute, or <c>null</c>
    /// if no such attribute exists.</returns>
    IAttribute this[QName name] { get; }
    //kw do we need this if we are not interested in prefixes?
    /// <summary>Retrieves attribute with a given prefixed name.</summary>
    /// <param name="qName">Prefixed (namespace qualified) name of attribute.</param>
    /// <returns><see cref="IAttribute"/> reference to attribute, or <c>null</c>
    /// if no such attribute exists.</returns>
    IAttribute this[string qName] { get; }
  }

  /// <summary>Base interface for handling XPEA events.</summary>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public interface IXmlHandler<S> where S: EventStatus
  {
    /// <summary>Resets internal state of handler.</summary>
    void Reset();
    /// <summary>Reports a processing instruction.</summary>
    /// <param name="target">The processing instruction target.</param>
    /// <param name="data">The processing instruction data, or <c>null</c> if none was supplied.
    /// The data does not include any whitespace separating it from the target.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void ProcessingInstruction(string target, string data, S status);
    /// <summary>Reports a skipped general entity reference.</summary>
    /// <param name="name">Name of general entity.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void SkippedEntity(string name, S status);
  }


  /// <summary>Interface for handling events at document level.</summary>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public interface IDocHandler<S>: IXmlHandler<S> where S: EventStatus
  {
    /// <summary>Called just before the parser starts processing the XML document.</summary>
    /// <param name="context">Implementation specific object. Often the
    /// <see cref="EventDispatcher&lt;N, S>"/> instance itself.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void StartDocument(object context, S status);
    /// <summary>Will be called at end of parsing, after all other events.</summary>
    /// <remarks>Will always be called, unless an exception was thrown.</remarks>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void EndDocument(S status);
    /// <summary>Called when the start tag of the root element in the XML document
    /// is encountered.</summary>
    /// <remarks>This is called once, before all the 
    /// <see cref="IElementHandler&lt;N, S>.Activate"/> call-backs for
    /// nodes matching the root element are called.</remarks>
    /// <param name="name">Name of the root element.</param>
    /// <param name="attributes">List of the root element's attributes.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void StartRoot(QName name, IAttributeList attributes, S status);
    /// <summary>Called after the end tag of the root element of the XML document
    /// was encountered.</summary>
    /// <remarks>This is called once, after all the 
    /// <see cref="IElementHandler&lt;N, S>.Deactivate"/> call-backs for
    /// nodes matching the root element are called.</remarks>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void EndRoot(S status);
  }

  /// <summary>Represents a step on the path down into an XML document.</summary>
  /// <remarks>The <see cref="Parent"/> property gives access to the ancestor axis.</remarks>
  public abstract class XmlPathStep
  {
    /// <summary>Element name associated with <see cref="XmlPathStep"/> instance.</summary>
    public abstract QName Name { get; }
    /// <summary>Attribute list associated with <see cref="XmlPathStep"/> instance.</summary>
    /// <remarks>The behaviour of the <see cref="IAttributeList"/> instance becomes
    /// undefined once the <see cref="XmlPathStep"/> instance is deactivated.</remarks>
    public abstract IAttributeList Attributes { get; }
    /// <summary>Parent (previous) step in path down the XML document.</summary>
    public abstract XmlPathStep Parent { get; }
  }

  // TODO what about namespace declarations (i.e. prefix mappings)? Are we interested at all?

  /// <summary>Interface for handling events at specific element level.</summary>
  /// <remarks>Typically associated with a node in an XML pattern matching tree.</remarks>
  /// <typeparam name="N">Implementation specific base class for matched nodes.</typeparam>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public interface IElementHandler<N, S>: IXmlHandler<S>
    where N: XmlMatchNode<N, S>
    where S: EventStatus
  {
    /// <summary>Called whenever the associated node in a pattern matching tree
    /// matches an element.</summary>
    /// <param name="matchedNode">Matched <see cref="XmlMatchNode&lt;N, S>"/> instance.</param>
    /// <param name="step">Current <see cref="XmlPathStep">step</see> on active path.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void Activate(N matchedNode, XmlPathStep step, S status);
    /// <summary>Called when character data in the immediate scope of the matching
    /// element are encountered.</summary>
    /// <param name="chars">Array holding a chunk of the element's character data.</param>
    /// <param name="start">Start index of character data.</param>
    /// <param name="length">Length of character data.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void Characters(char[] chars, int start, int length, S status);
    /// <summary>Called when ignorable whitespace in the immediate scope of the matching
    /// element is encountered.</summary>
    /// <param name="ws">Array holding a chunk of the element's whitespace.</param>
    /// <param name="start">Start index of whitespace.</param>
    /// <param name="length">Length of whitespace.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void IgnorableWhitespace(char[] ws, int start, int length, S status);
    /// <summary>Called for the start tag of each child of the matching element.</summary>
    /// <remarks>This call occurs just before any <see cref="Activate"/> call-backs
    /// for the child element are made.</remarks>
    /// <param name="name">Name of child element.</param>
    /// <param name="attributes">Attributes of child element.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void StartChild(QName name, IAttributeList attributes, S status);
    /// <summary>Called for the end tag of each child of the matching element.</summary>
    /// <remarks>This call occurs just after any <see cref="Deactivate"/> call-backs
    /// for the child element were made.</remarks>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void EndChild(S status);
    /// <summary>Called whenever the end tag for the matching element is encountered.</summary>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    void Deactivate(S status);
  }

  /// <summary>Represents a node in an XML pattern matching tree.</summary>
  /// <remarks>A node can represent an element name, or a wildcard, or other 
  /// patterns that can be matched.</remarks>
  /// <typeparam name="N"><c>XmlMatchNode</c> subclass which is the base
  /// class for all nodes in a specific implementation. Recursive type parameter
  /// which allows the implementation sub-class to avoid down-casting.</typeparam>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public abstract class XmlMatchNode<N, S>
    where N: XmlMatchNode<N, S>
    where S: EventStatus
  {
    /// <summary>Indicates if the node's pattern matching is 'absolute' with respect to
    /// its parent (matches evaluated against immediate child elements), or 'relative' 
    /// (matches evaluated against any descendant element, e.g. a grand-child).</summary>
    /// <remarks>The terms 'absolute' and 'relative' relate to the process of pattern
    /// matching, not to the child-parent relationships in the pattern matching tree.</remarks>
    public abstract bool IsAbsolute { get; }
    /// <summary>The handler instance associated with this node.</summary>
    public abstract IElementHandler<N, S> Handler { get; set; }

    /// <summary>The node's parent.</summary>
    public abstract N Parent { get; }
    /// <summary>Detaches node from parent.</summary>
    public abstract void Detach();
    /// <summary>An ordered list of the nodes children.</summary>
    public abstract IList<N> Children { get; }

    /// <summary>Resets all handlers in the sub-tree rooted at this node.</summary>
    public abstract void Reset();
    /// <summary>Indicates if the <see cref="XmlMatchNode&lt;N, S>"/> instance matches
    /// a specific step on the path in the XML document.</summary>
    /// <remarks>The implementation may process whatever information is available,
    /// including the <see cref="XmlPathStep.Name"/> and <see cref="XmlPathStep.Attributes"/>
    /// properties of ancestor steps.</remarks>
    /// <param name="step">Step on path in XML document.</param>
    /// <returns><c>true</c> if the node matches, <c>false</c> otherwise.</returns>
    public abstract bool Matches(XmlPathStep step);

    //kw  public abstract bool IsActive { get; } // true if currently active
    // or public abstract XmlPathStep ActiveStep { get; }

    /// <summary>Deactivates call-backs on all descendant nodes (sub-tree)
    /// of this <see cref="XmlMatchNode&lt;N, S>"/>.</summary>
    /// <remarks><list type="bullet">
    ///   <item>This can only be called when the node is active (on the active path).</item>
    ///   <item>Descendant deactivation is in effect only as long as the node is active.</item>
    ///   <item>It is recommended to call this from the handler associated with this
    ///     node. When called from an active node in the sub-tree, the deactivation
    ///     takes effect only the next time the sub-tree is matched, but does not affect
    ///     the currently active sub-tree.</item>
    /// </list></remarks>
    public abstract void DeactivateDescendants();
  }

  /// <summary>The central XPEA class. Manages pattern matching tree and handler
  /// registration, and coordinates processing.</summary>
  /// <remarks>An implementation of this class needs to be driven by some form
  /// of XML processor/parser. Any responsibilities not directly related to
  /// XPEA pattern matching and event dispatching should be left to the
  /// underlying parser implementation ("separation of concerns").</remarks>
  /// <typeparam name="N"><see cref="XmlMatchNode&lt;N, S>"/> subclass which is the
  /// base class for all nodes in a specific implementation.</typeparam>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public abstract class EventDispatcher<N, S>
    where N: XmlMatchNode<N, S>
    where S: EventStatus
  {
    /// <summary>Gets or sets the handler for document level events.</summary>
    public abstract IDocHandler<S> DocHandler { get; set; }
    /// <summary>Holds the root nodes of all pattern matching trees.</summary>
    /// <remarks>All trees must be added before processing begins. Adding or
    /// modifying a tree during processing has undefined effects.</remarks>
    public abstract IList<N> MatchTrees { get; }
    /// <summary>Resets state to get ready for next XML processing operation
    /// and calls <see cref="IXmlHandler&lt;S>.Reset"/> on all handlers.</summary>
    /// <remarks>Note: *Must not* be called while XML processing is under way!</remarks>
    public abstract void Reset();
    /// <summary>Application specific instance of <see cref="EventStatus"/>.</summary>
    /// <remarks>Allows for customized error handling and processing of call-back
    /// results. Changing the status object during processing will be ignored
    /// until processing is finished.</remarks>
    public abstract S StatusObject { get; set; }
  }
}
