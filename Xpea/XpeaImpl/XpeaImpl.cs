/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Org.System.Xml.Xpea.Helpers;

namespace Org.System.Xml.Xpea.StdImpl
{
  /// <summary>Base class for standard implementation of <see cref="XmlMatchNode&lt;N, S>"/>.</summary>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class StdMatchNode<S>: XmlMatchNode<StdMatchNode<S>, S>
      where S: EventStatus
  {
    /// <summary>Child node container class.</summary>
    public class ChildList: IList<StdMatchNode<S>>
    {
      private StdMatchNode<S> owner;
      private StdMatchNode<S>[] items;
      private int count = 0;

      internal ChildList(StdMatchNode<S> owner, int capacity) {
        this.owner = owner;
        items = new StdMatchNode<S>[capacity];
      }

      internal ChildList(StdMatchNode<S> owner) : this(owner, 0) { }

      /* IEnumerable<StdMatchNode<S>> */

      /// <summary>Implementation of <see cref="IEnumerable&lt;T>.GetEnumerator"/>.</summary>
      public IEnumerator<StdMatchNode<S>> GetEnumerator() {
        for (int index = 0; index < count; index++)
          yield return items[index];
      }

      /* IEnumerable */

      /// <summary>Implementation of <see cref="IEnumerable.GetEnumerator"/>.</summary>
      IEnumerator IEnumerable.GetEnumerator() {
        for (int index = 0; index < count; index++)
          yield return items[index];
      }

      /* ICollection<StdMatchNode<S>> */

      /// <summary>Implementation of <see cref="ICollection&lt;T>.Add"/>.</summary>
      public void Add(StdMatchNode<S> item) {
        if (item == null)
          throw new ArgumentNullException("item");
        if (Contains(item)) {
          string msg = Resources.GetString(RsId.DuplicateNode);
          throw new ArgumentException(msg, "item");
        }
        int newCount = count + 1;
        if (newCount > items.Length)  // double capacity
          Array.Resize<StdMatchNode<S>>(ref items, newCount << 1);
        item.Detach();
        items[count] = item;
        item.parent = owner;
        count = newCount;
      }

      /// <summary>Implementation of <see cref="ICollection&lt;T>.Clear"/>.</summary>
      public void Clear() {
        for (int index = 0; index < count; index++)
          items[index].parent = null;
        Array.Clear(items, 0, count);
        count = 0;
      }

      /// <summary>Implementation of <see cref="ICollection&lt;T>.Contains"/>.</summary>
      public bool Contains(StdMatchNode<S> item) {
        return Array.IndexOf<StdMatchNode<S>>(items, item) != -1;
      }

      /// <summary>Implementation of <see cref="ICollection&lt;T>.CopyTo"/>.</summary>
      public void CopyTo(StdMatchNode<S>[] array, int arrayIndex) {
        items.CopyTo(array, arrayIndex);
      }

      /// <summary>Implementation of <see cref="ICollection&lt;T>.Count"/>.</summary>
      public int Count {
        get { return count; }
      }

      /// <summary>Implementation of <see cref="ICollection&lt;T>.IsReadOnly"/>.</summary>
      public bool IsReadOnly {
        get { return false; }
      }

      /// <summary>Implementation of <see cref="ICollection&lt;T>.Remove"/>.</summary>
      public bool Remove(StdMatchNode<S> item) {
        int index = Array.IndexOf<StdMatchNode<S>>(items, item);
        bool result = index != -1;
        if (result) {
          count--;
          Array.Copy(items, index + 1, items, index, count - index);
          item.parent = null;
        }
        return result;
      }

      /* IList<StdMatchNode<S>> */

      /// <summary>Implementation of <see cref="IList&lt;T>.IndexOf"/>.</summary>
      public int IndexOf(StdMatchNode<S> item) {
        return Array.IndexOf<StdMatchNode<S>>(items, item);
      }

      /// <summary>Implementation of <see cref="IList&lt;T>.Insert"/>.</summary>
      public void Insert(int index, StdMatchNode<S> item) {
        if (item == null)
          throw new ArgumentNullException("item");
        if (Contains(item)) {
          string msg = Resources.GetString(RsId.DuplicateNode);
          throw new ArgumentException(msg, "item");
        }
        int newCount = count + 1;
        if (newCount > items.Length)  // double capacity
          Array.Resize<StdMatchNode<S>>(ref items, newCount << 1);
        // move all items one up from index on
        Array.Copy(items, index, items, index + 1, count - index);
        item.Detach();
        items[index] = item;
        item.parent = owner;
        count = newCount;
      }

      /// <summary>Implementation of <see cref="IList&lt;T>.RemoveAt"/>.</summary>
      public void RemoveAt(int index) {
        StdMatchNode<S> item = items[index];
        count--;
        Array.Copy(items, index + 1, items, index, count - index);
        item.parent = null;
      }

      /// <summary>Node at a given index.</summary>
      /// <param name="index">Index of node to get or set.</param>
      public StdMatchNode<S> this[int index] {
        get { return items[index]; }
        set {
          if (value == null)
            throw new ArgumentNullException("Item");  // name of indexer property
          StdMatchNode<S> item = items[index];
          if (value == item)
            return;
          if (Contains(value)) {
            string msg = Resources.GetString(RsId.DuplicateNode);
            throw new ArgumentException(msg, "Item");
          }
          value.Detach();
          items[index] = value;
          item.parent = null;
          value.parent = owner;
        }
      }
    }

    internal StdMatchNode() {
      children = new ChildList(this);
    }

    protected StdMatchNode(bool isAbsolute, IElementHandler<StdMatchNode<S>, S> handler) {
      children = new ChildList(this);
      this.isAbsolute = isAbsolute;
      this.handler = handler;
    }

    internal readonly bool isAbsolute;
    internal readonly ChildList children;
    internal StdMatchNode<S> parent;
    internal StdEventDispatcher<S>.StdPathStep activeStep = null;
    private IElementHandler<StdMatchNode<S>, S> handler = null;
    private Predicate<XmlPathStep> predicate;

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.IsAbsolute"/>.</summary>
    public override bool IsAbsolute {
      get { return isAbsolute; }
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Handler"/>.</summary>
    public override IElementHandler<StdMatchNode<S>, S> Handler {
      get { return handler; }
      set { handler = value; }
    }

    /// <summary>Predicate to be evaluated when <see cref="StdMatchNode&lt;S>"/> is matched.</summary>
    /// <remarks>Called by implementation of <see cref="XmlMatchNode&lt;N, S>.Matches"/>.</remarks>
    public Predicate<XmlPathStep> Predicate {
      get { return predicate; }
      set { predicate = value; }
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Parent"/>.</summary>
    public override StdMatchNode<S> Parent {
      get { return parent; }
    }

    /// <summary>Matched step on active path, or <c>null</c>.</summary>
    public XmlPathStep ActiveStep {
      get { return activeStep; }
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Detach"/>.</summary>
    public override void Detach() {
      if (parent == null)
        return;
      if (!parent.children.Remove(this)) {
        string msg = Resources.GetString(RsId.CorruptMatchTree);
        throw new XpeaException(msg);
      }
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Children"/>.</summary>
    public override IList<StdMatchNode<S>> Children {
      get { return children; }
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Reset"/>.</summary>
    /// <remarks>Calls <c>Reset</c> on children  recursively.</remarks>
    public override void Reset() {
      if (handler != null)
        handler.Reset();
      for (int index = 0; index < children.Count; index++) {
        children[index].Reset();
      }
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Matches"/>.</summary>
    /// <remarks>Default implementation, always returns <c>false</c>.</remarks>
    public override bool Matches(XmlPathStep step) {
      return false;
    }

    private void CheckActive() {
      if (activeStep == null)
        throw new XpeaException("Node not active.");
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.DeactivateDescendants"/>.</summary>
    public override void DeactivateDescendants() {
      CheckActive();
      if (children.Count != 0 && activeStep.Child != null)
        activeStep.Child.RemoveCandidates(children[0], children.Count);
    }
  }

  /// <summary>Node type which matches an element name exactly.</summary>
  /// <remarks>Match result depends on <see cref="StdMatchNode&lt;S>.Predicate"/> evaluation.</remarks>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class StdNamedNode<S>: StdMatchNode<S> where S: EventStatus
  {
    private QName name;

    public StdNamedNode(QName name,
                        bool isAbsolute,
                        IElementHandler<StdMatchNode<S>, S> handler)
      : base(isAbsolute, handler) 
    {
      this.name = name;
    }

    public StdNamedNode(string localName,
                        string nsUri,
                        bool isAbsolute,
                        IElementHandler<StdMatchNode<S>, S> handler)
      : base(isAbsolute, handler)
    {
      name = new QName(localName, nsUri);
    }

    public StdNamedNode(string localName,
                        bool isAbsolute,
                        IElementHandler<StdMatchNode<S>, S> handler)
      : base(isAbsolute, handler)
    {
      name = new QName(localName);
    }

    /// <remarks>An absolute node will be created.</remarks>
    public StdNamedNode(QName name,
                        IElementHandler<StdMatchNode<S>, S> handler)
      : base(true, handler)
    {
      this.name = name;
    }

    /// <remarks>An absolute node will be created.</remarks>
    public StdNamedNode(string localName,
                        string nsUri,
                        IElementHandler<StdMatchNode<S>, S> handler)
      : base(true, handler) 
    {
      name = new QName(localName, nsUri);
    }

    /// <remarks>An absolute node will be created.</remarks>
    public StdNamedNode(string localName,
                        IElementHandler<StdMatchNode<S>, S> handler)
      : base(true, handler) 
    {
      name = new QName(localName);
    }

    public StdNamedNode(QName name, bool isAbsolute) : base(isAbsolute, null) {
      this.name = name;
    }

    public StdNamedNode(string localName, string nsUri, bool isAbsolute) : base(isAbsolute, null) {
      name = new QName(localName, nsUri);
    }

    public StdNamedNode(string localName, bool isAbsolute) : base(isAbsolute, null) {
      name = new QName(localName);
    }

    /// <remarks>An absolute node will be created.</remarks>
    public StdNamedNode(QName name)
      : base(true, null) {
      this.name = name;
    }

    /// <remarks>An absolute node will be created.</remarks>
    public StdNamedNode(string localName, string nsUri)
      : base(true, null) {
      name = new QName(localName, nsUri);
    }

    /// <remarks>An absolute node will be created.</remarks>
    public StdNamedNode(string localName)
      : base(true, null) {
      name = new QName(localName);
    }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Matches"/>.</summary>
    /// <remarks>Compares names for an exact match and then evaluates the
    /// <see cref="StdMatchNode&lt;S>.Predicate"/> as well.</remarks>
    public override bool Matches(XmlPathStep step) {
      bool result = name == step.Name;
      result = result && (Predicate == null || Predicate(step));
      return result;
    }

    /// <summary>Element name to match.</summary>
    public QName ElementName {
      get { return name; }
    }
  }

  /// <summary>Node type which matches any element name.</summary>
  /// <remarks>Match result depends on <see cref="StdMatchNode&lt;S>.Predicate"/> evaluation.</remarks>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class StdWildCardNode<S>: StdMatchNode<S> where S: EventStatus
  {
    /// <remarks>An absolute node will be created.</remarks>
    public StdWildCardNode() : base(true, null) { }

    public StdWildCardNode(bool isAbsolute) : base(isAbsolute, null) { }

    public StdWildCardNode(bool isAbsolute, IElementHandler<StdMatchNode<S>, S> handler)
      : base(isAbsolute, handler) 
    { }

    /// <summary>Implements <see cref="XmlMatchNode&lt;N, S>.Matches"/>.</summary>
    /// <remarks>Always evaluates the <see cref="StdMatchNode&lt;S>.Predicate"/>.</remarks>
    public override bool Matches(XmlPathStep step) {
      return Predicate == null || Predicate(step);
    }
  }

  /// <summary>Convenience declaration.</summary>
  public class StdPath<S>: XmlPath<StdMatchNode<S>, S>
    where S: EventStatus
  {
    protected internal StdPath(StdMatchNode<S> root, StdMatchNode<S> leaf)
      : base(root, leaf)
    { }
  }

  /// <summary>Delegate type to be used when a string should be interned.</summary>
  public delegate string XmlStringIntern(string str);

  /// <summary>Factory class that creates <see cref="StdPath&lt;S>"/> instances from
  /// their textual representation.</summary>
  public class StdPathFactory<S> where S: EventStatus
  {
    private IDictionary<string, string> prefixMap;
    private XmlStringIntern intern;

    private string NoIntern(string str) {
      return str;
    }

    /// <summary>Factory method for nodes.</summary>
    /// <param name="qName">Pattern that determines which type of node is created
    /// (e.g. a name or wildcard).</param>
    /// <param name="isAbsolute">Determines if the node's pattern matching will be
    /// 'absolute' with respect to its parent. See <see cref="XmlMatchNode&lt;N, S>"/>.</param>
    /// <param name="predicate">Predicate instance associated with the node. Can be <c>null</c>.</param>
    /// <param name="handler">Handler instance associated with the node. Can be <c>null</c>.</param>
    /// <returns>A new node configured for pattern matching.</returns>
    protected StdMatchNode<S> CreateNode(
      string qName,
      bool isAbsolute,
      Predicate<XmlPathStep> predicate,
      IElementHandler<StdMatchNode<S>, S> handler)
    {
      StdMatchNode<S> node;
      if (qName == "*")
        node = new StdWildCardNode<S>(isAbsolute, handler);
      else {
        string prefix, localName;
        XmlChars.SplitQName(qName, out prefix, out localName);
        string uri = String.Empty;
        if (prefix != String.Empty) {
          if (prefixMap == null) {
            string msg = Resources.GetString(RsId.NoPrefixMap);
            throw new InvalidOperationException(msg);
          }
          uri = prefixMap[prefix];
        }
        localName = intern(localName);
        QName name;
        if (string.IsNullOrEmpty(uri))
          name = QName.Checked(localName);
        else {
          uri = intern(uri);
          name = QName.Checked(localName, uri);
        }
        node = new StdNamedNode<S>(name, isAbsolute, handler);
      }
      node.Predicate = predicate;
      return node;
    }

    private void SyntaxError(int index) {
      string errMsg = Resources.GetString(RsId.PathSyntaxError);
      throw new XpeaException(string.Format(errMsg, index));
    }

    /// <summary>Parses a string expression into a path of properly configured and
    /// connected <see cref="StdMatchNode&lt;S>"/> instances.</summary>
    /// <param name="expr">String expression to parse.</param>
    /// <param name="predicates">Array of predicate instances to be associated with
    /// correspondingly tagged nodes.</param>
    /// <param name="handlers">Array of handler instances to be associated with
    /// correspondingly tagged nodes.</param>
    /// <returns>Leaf node of path.</returns>
    protected StdMatchNode<S> ParseExpression(
      string expr,
      Predicate<XmlPathStep>[] predicates,
      params IElementHandler<StdMatchNode<S>, S>[] handlers)
    {
      if (string.IsNullOrEmpty(expr)) {
        string msg = Resources.GetString(RsId.EmptyMatchExpression);
        throw new ArgumentException(msg, "expr");
      }

      StdMatchNode<S> leaf = null;
      int nodeStart = 0;
      int nodeEnd = -1;
      int handlerStart = -1;
      int handlerIndex = -1;
      int predicateStart = -1;
      int predicateIndex = -1;
      bool isAbsolute = true;
      int index = 0;

      // we allow starting with no slash - implying there is one
      bool slashFound = true;
      if (expr[0] == '/')
        index++;

      for (; ; ) {
        bool exit = false;
        if (index == expr.Length) {
          exit = true;
          goto AddNode;
        }

        switch (expr[index]) {
          case '/': {
              if (slashFound) {
                isAbsolute = false;
              }
              else {  // we are at end of character sequence for node
                slashFound = true;
                goto AddNode;
              }
              break;
            }
          case '[': {
              if (handlerStart >= 0)
                SyntaxError(index);
              if (nodeEnd == -1)
                nodeEnd = index;
              predicateStart = index + 1;
              break;
            }
          case ']': {
              if (predicateStart == -1)
                SyntaxError(index);
              string predicateRef = expr.Substring(predicateStart, index - predicateStart);
              predicateIndex = Convert.ToInt32(predicateRef);
              predicateStart = -1;
              break;
            }
          case '{': {
              if (predicateStart >= 0)
                SyntaxError(index);
              if (nodeEnd == -1)
                nodeEnd = index;
              handlerStart = index + 1;
              break;
            }
          case '}': {
              if (handlerStart == -1)
                SyntaxError(index);
              string handlerRef = expr.Substring(handlerStart, index - handlerStart);
              handlerIndex = Convert.ToInt32(handlerRef);
              handlerStart = -1;
              break;
            }
          default: {
              if (slashFound) {
                nodeStart = index;
                nodeEnd = -1;
                handlerIndex = -1;
                predicateIndex = -1;
                slashFound = false;
              }
              break;
            }
        }
        index++;
        continue;

      AddNode:
        if (predicateStart >= 0)
          SyntaxError(index);
        if (handlerStart >= 0)
          SyntaxError(index);
        if (nodeEnd == -1)  // in case no handler is specified
          nodeEnd = index;
        string nodeName = expr.Substring(nodeStart, nodeEnd - nodeStart);
        Predicate<XmlPathStep> predicate;
        if (predicateIndex == -1)
          predicate = null;
        else {
          if (predicates == null || predicateIndex >= predicates.Length) {
            string errMsg = Resources.GetString(RsId.InvalidPredicateIndex);
            throw new XpeaException(string.Format(errMsg, predicateIndex));
          }
          predicate = predicates[predicateIndex];
        }
        IElementHandler<StdMatchNode<S>, S> handler;
        if (handlerIndex == -1)
          handler = null;
        else {
          if (handlers == null || handlerIndex >= handlers.Length) {
            string errMsg = Resources.GetString(RsId.InvalidHandlerIndex);
            throw new XpeaException(string.Format(errMsg, handlerIndex));
          }
          handler = handlers[handlerIndex];
        }
        StdMatchNode<S> node = CreateNode(nodeName, isAbsolute, predicate, handler);
        if (leaf != null)
          leaf.children.Add(node);
        leaf = node;

        if (exit)
          break;
        index++;
      }

      return leaf;
    }

    public StdPathFactory(IDictionary<string, string> prefixMap, XmlStringIntern intern) {
      StringIntern = intern;
      PrefixMap = prefixMap;
    }

    public StdPathFactory(IDictionary<string, string> prefixMap) : this(prefixMap, null) { }

    public StdPathFactory() : this(null, null) { }

    /// <overloads>
    /// <summary>Creates a new <see cref="StdPath&lt;S>"/> instance from a pattern matching
    /// string expression.</summary>
    /// </overloads>
    /// <remarks>The syntax of the expression must follow these rules:
    /// <list type="bullet">
    /// <item>An absolute node is preceded by a single slash '/', a relative node
    /// is preceded by a double slash '//', except for the start of the path, where
    /// the single slash for an absolute node can be omitted.</item>
    /// <item>An exact match node is indicated by a well-formed element name. If it contains
    /// a namespace prefix, then this prefix must have been added to the 
    /// <see cref="PrefixMap"/> of the path factory.</item>
    /// <item>A wildcard node matches any element name. It is represented by an asterisk '*'.</item>    /// 
    /// <item>An integer index between square brackets after a name or wildcard associates the
    /// node with the predicate at that index in the predicates array.</item>
    /// <item>An integer index between curly braces after a name or wildcard associates the
    /// node with the handler at that index in the handlers array.</item>
    /// </list>
    /// <para>Examples of pattern matching expressions:</para>
    /// <list type="table">
    /// <listheader>
    ///   <term>Path Expression</term>
    ///   <description>Matches</description>
    /// </listheader>
    /// <item>
    ///   <term>//A/*/C{2}</term>
    ///   <description>Any occurrence of A followed by some element followed by C,
    ///     with handler <c>2</c> to be called whenever element C in this path
    ///     is encountered.</description>
    /// </item>
    /// <item>
    ///   <term>A[0]/B{0}/*</term>
    ///   <description>Any element at the third level whose ancestors are A/B,
    ///     with predicate <c>0</c> to be evaluated for every element A and handler
    ///     <c>0</c> to be called for every element B, as long as predicate <c>0</c>
    ///     was <c>true</c>.</description>
    /// </item>
    /// <item>
    ///   <term>//A{1}/B/*</term>
    ///   <description>Any element whose ancestors are A/B, with handler <c>1</c>
    ///     to be called whenever element A in this path is encountered.</description>
    /// </item>
    /// <item>
    ///   <term>//*/B[1]{0}/C{1}</term>
    ///   <description>Any occurrence of path B/C with at least one ancestor,
    ///     with predicate <c>1</c> to be evaluated for every element B in this path,
    ///     and as long as the predicate is <c>true</c>, with handler <c>0</c> to be
    ///     called for every element B and handler <c>1</c> to be called for every
    ///     element C in this path.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="pathExpr">"Textual representation of pattern matching path."</param>
    /// <param name="predicates">Array of predicates to associate with correspondingly tagged nodes.</param>
    /// <param name="handlers">Array of handlers to associate with correspondingly tagged nodes.</param>
    /// <returns>New <see cref="StdPath&lt;S>"/> instance.</returns>
    public StdPath<S> Create(
      string pathExpr,
      Predicate<XmlPathStep>[] predicates,
      params IElementHandler<StdMatchNode<S>, S>[] handlers)
    {
      StdMatchNode<S> leaf = ParseExpression(pathExpr, predicates, handlers);
      StdMatchNode<S> root = leaf;
      while (root.parent != null)
        root = root.parent;
      return new StdPath<S>(root, leaf);
    }

    /// <remarks>The syntax of the expression must follow these rules:
    /// <list type="bullet">
    /// <item>An absolute node is preceded by a single slash '/', a relative node
    /// is preceded by a double slash '//', except for the start of the path, where
    /// the single slash for an absolute node can be omitted.</item>
    /// <item>An exact match node is indicated by a well-formed element name. If it contains
    /// a namespace prefix, then this prefix must have been added to the 
    /// <see cref="PrefixMap"/> of the path factory.</item>
    /// <item>A wildcard node matches any element name. It is represented by an asterisk '*'.</item>    /// 
    /// <item>An integer index between curly braces after a name or wildcard associates the
    /// node with the handler at that index in the handlers array.</item>
    /// </list>
    /// <para>Examples of pattern matching expressions:</para>
    /// <list type="table">
    /// <listheader>
    ///   <term>Path Expression</term>
    ///   <description>Matches</description>
    /// </listheader>
    /// <item>
    ///   <term>//A/*/C{2}</term>
    ///   <description>Any occurrence of A followed by some element followed by C,
    ///     with handler <c>2</c> to be called whenever element C in this path
    ///     is encountered.</description>
    /// </item>
    /// <item>
    ///   <term>A/B{0}/*</term>
    ///   <description>Any element at the third level whose ancestors are A/B,
    ///     with handler <c>0</c> to be called for every element B in this path.</description>
    /// </item>
    /// <item>
    ///   <term>//A{1}/B/*</term>
    ///   <description>Any element whose ancestors are A/B, with handler <c>1</c>
    ///     to be called whenever element A in this path is encountered.</description>
    /// </item>
    /// <item>
    ///   <term>//*/B{0}/C{1}</term>
    ///   <description>Any occurrence of path B/C with at least one ancestor,
    ///     with handler <c>0</c> to be called for every element B and handler
    ///     <c>1</c> to be called for every element C in this path.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="pathExpr">"Textual representation of pattern matching path."</param>
    /// <param name="handlers">Array of handlers to associate with correspondingly tagged nodes.</param>
    /// <returns>New <see cref="StdPath&lt;S>"/> instance.</returns>
    public StdPath<S> Create(
      string pathExpr,
      params IElementHandler<StdMatchNode<S>, S>[] handlers) 
    {
      return Create(pathExpr, null, handlers);
    }
    
    /// <summary>Maps prefixes to namespace URIs.</summary>
    /// <remarks>Required to correctly parse prefixed names into
    /// <see cref="QName"/> instances.</remarks>
    public IDictionary<string, string> PrefixMap {
      get { return prefixMap; }
      set { prefixMap = value; }
    }

    /// <summary>Call-back delegate for string interning.</summary>
    /// <remarks>Makes string comparisons for name matching more efficient.</remarks>
    public XmlStringIntern StringIntern {
      get {
        if (intern == NoIntern)
          return null;
        else
          return intern;
      }
      set {
        if (value == null)
          intern = NoIntern;
        else
          intern = value;
      }
    }
  }

  /// <summary>Name part of attribute declaration.</summary>
  /// <remarks>Key type for <see cref="StdEventDispatcher&lt;S>.AttDecls">
  /// attribute declaration dictionary</see>.</remarks>
  public struct AttDeclKey
  {
    // no check for null arguments!
    public AttDeclKey(string elmName, string attName) {
      ElmName = elmName;
      AttName = attName;
    }

    public readonly string ElmName;
    public readonly string AttName;

    // Precondition: ElmName != null, AttName != null
    public override int GetHashCode() {
      return ElmName.GetHashCode() ^ AttName.GetHashCode();
    }

    public override bool Equals(object obj) {
      return obj is AttDeclKey && this == (AttDeclKey)obj;
    }

    public static bool operator ==(AttDeclKey x, AttDeclKey y) {
      return x.ElmName == y.ElmName && x.AttName == y.AttName;
    }

    public static bool operator !=(AttDeclKey x, AttDeclKey y) {
      return !(x == y);
    }
  }

  /// <summary>Attribute information in attribute declaration.</summary>
  /// <remarks>Value type type for <see cref="StdEventDispatcher&lt;S>.AttDecls">
  /// attribute declaration dictionary</see>.</remarks>
  public struct AttDecl
  {
    public AttDecl(string attType, string dflt, bool isRequired) {
      AttType = attType;
      Default = dflt;
      IsRequired = isRequired;
    }

    public readonly string AttType;
    public readonly string Default;
    public readonly bool IsRequired;
  }

  /// <summary>Base class for standard implementation of <see cref="EventDispatcher&lt;N, S>"/>.</summary>
  /// <remarks><list type="bullet">
  /// <item><see cref="IElementHandler&lt;N, S>"/> instances are registered by storing
  ///   them as children of an internal wild card node (property <see cref="MatchTrees"/>).</item>
  /// <item>Inside of <c>StdEventDispatcher</c> the currently active path while
  ///   processing the XML document is tracked through a linked list of 
  ///   <see cref="XmlPathStep"/> instances. The <see cref="Current"/> property
  ///   points to the <see cref="XmlPathStep"/> instance associated with the current
  ///   element in the XML document. By calling the <see cref="Activate"/> and
  ///   <see cref="Deactivate"/> methods the value of <see cref="Current"/> is
  ///   kept in sync with the parser when traversing the document tree.</item>
  /// <item>Each <see cref="StdPathStep"/> instance contains a list of
  ///   <see cref="StdMatchNode&lt;S>"/> instances called <see cref="StdPathStep.CandidateNodes"/>,
  ///   which represents the nodes of all registered pattern matching trees that currently
  ///   have a potential to match.</item>
  /// <item>On every new activation (see the <see cref="Activate"/> and 
  ///   <see cref="StdPathStep.Activate"/> calls), these steps are performed:
  ///   <list type="number">
  ///     <item>The next <see cref="XmlPathStep"/> instance in the path is made 
  ///       <see cref="Current"/>.</item>
  ///     <item>The list of <see cref="StdPathStep.CandidateNodes"/> on the 
  ///       <see cref="StdPathStep.Child">next path step</see> is cleared.</item>
  ///     <item>The children of all *relative* <see cref="StdPathStep.CandidateNodes"/>
  ///       are added to the <see cref="StdPathStep.Child">next path step's</see> list of
  ///       candidate nodes, as they are a potential match further down the path.</item>
  ///     <item>All current <see cref="StdPathStep.CandidateNodes"/> are checked for
  ///       a match on the current element name, and if they are a match, corresponding
  ///       context objects are added to the list of <see cref="StdPathStep.MatchingNodes">
  ///       matching context objects</see>. Also, all their
  ///       <see cref="StdMatchNode&lt;S>.Children">children</see> are added to the 
  ///       <see cref="StdPathStep.Child">next path step's</see> list of candidate nodes,
  ///       as they are a potential match further down the path as well.</item>
  ///     <item>It is then the XML parser adapter's responsibility to call first
  ///       <see cref="StdPathStep.Activate"/>, then any of the other "CallXXX(...)"
  ///       methods, and at last <see cref="StdPathStep.Deactivate"/> for the current
  ///       path level. In each of these calls, the list of <see cref="StdPathStep.MatchingNodes">
  ///       matching context objects</see> is iterated over, and for each associated 
  ///       <see cref="StdMatchNode&lt;S>">matching node</see> the handler is called.</item>
  ///     </list></item>
  /// <item>On every deactivation (see the <see cref="Deactivate"/> and 
  ///   <see cref="StdPathStep.Deactivate"/> calls), these steps are performed:
  ///   <list type="number">
  ///     <item>On the current <see cref="XmlPathStep"/> instance (before moving 
  ///       back up the path), the list of <see cref="StdPathStep.MatchingNodes">
  ///       matching context objects</see> is cleared.</item>
  ///     <item>Then the current <see cref="XmlPathStep"/> instance is
  ///       <see cref="StdPathStep.Deactivate">deactivated</see> and the previous
  ///       <see cref="XmlPathStep"/> instance in the path is made <see cref="Current"/>
  ///       again, allowing for further parser call-backs at this level.</item>
  ///     </list></item>
  /// <item>For multiple nodes matching the same element, the order of call-backs
  ///   corresponds to the level order of the nodes in the pattern matching trees,
  ///   starting the tree registered first.</item>
  /// </list></remarks>
  public abstract class StdEventDispatcher<S>: EventDispatcher<StdMatchNode<S>, S>
    where S: EventStatus
  {
    /// <summary>Represents a node of the currently active path in the XML document.</summary>
    /// <remarks>Exposes most of the interface through which a streaming XML parser can
    /// be adapted to drive the XPEA pattern matching process. The public methods of this
    /// API are all named like "CallXXX(...)".</remarks>
    protected internal class StdPathStep: XmlPathStep
    {
      private StdEventDispatcher<S> owner;
      private StdMatchNode<S>[] candidateNodes = new StdMatchNode<S>[2];
      private int candidateCount = 0;
      private StdMatchNode<S>[] matchingNodes = new StdMatchNode<S>[2];
      private int matchingCount = 0;
      private StdPathStep child;
      internal readonly StdPathStep parent;

      private QName name;
      private AttributeList attributes;

      /// <summary>Adds properly configured <see cref="StdMatchNode&lt;S>"/>
      /// instance to <see cref="MatchingNodes"/>.</summary>
      /// <param name="node">Node to add.</param>
      protected void AddMatchingNode(StdMatchNode<S> node) {
        Debug.Assert(node != null);
        int newCount = matchingCount + 1;
        if (newCount > matchingNodes.Length)
          Array.Resize<StdMatchNode<S>>(ref matchingNodes, newCount << 1);
        node.activeStep = this;
        matchingNodes[matchingCount] = node;
        matchingCount = newCount;
      }

      /// <summary>Adds new node to list of candidate nodes.</summary>
      /// <param name="node">Node to add.</param>
      protected void AddCandidateNode(StdMatchNode<S> node) {
        Debug.Assert(node != null);
        int newCount = candidateCount + 1;
        if (newCount > candidateNodes.Length)
          Array.Resize<StdMatchNode<S>>(ref candidateNodes, newCount << 1);
        candidateNodes[candidateCount] = node;
        candidateCount = newCount;
      }

      /// <summary>Adds a list of new nodes to the list of candidate nodes.</summary>
      /// <param name="candidates">List of nodes to add.</param>
      protected void AddCandidateNodes(StdMatchNode<S>.ChildList candidates) {
        Debug.Assert(candidates != null);
        int newCount = candidateCount + candidates.Count;
        if (newCount > candidateNodes.Length)
          Array.Resize<StdMatchNode<S>>(ref candidateNodes, newCount << 1);
        newCount = candidateCount;
        for (int candIndex = 0; candIndex < candidates.Count; candIndex++) {
          candidateNodes[newCount] = candidates[candIndex];
          newCount++;
        }
        candidateCount = newCount;
      }

      /// <summary>Adds a child <see cref="XmlPathStep"/> if there isn't one already.</summary>
      /// <returns>Child instance.</returns>
      internal protected StdPathStep GetChildNew() {
        if (child == null)
          child = new StdPathStep(owner, this);
        return child;
      }

      /// <summary>Array holding candidate nodes.</summary>
      protected StdMatchNode<S>[] CandidateNodes {
        get { return candidateNodes; }
      }

      /// <summary>Count of candidate nodes.</summary>
      protected int CandidateCount {
        get { return candidateCount; }
      }

      /// <summary>Array holding matching context objects.</summary>
      protected StdMatchNode<S>[] MatchingNodes {
        get { return matchingNodes; }
      }

      /// <summary>Count of matching nodes.</summary>
      protected int MatchingCount {
        get { return matchingCount; }
      }

      /// <summary>Clears list of candidate nodes.</summary>
      protected void ClearCandidateNodes() {
        candidateCount = 0;
      }

      /// <summary>Clears list of matching nodes.</summary>
      protected void ClearMatchingNodes() {
        for (int indx = 0; indx < matchingCount; indx++)
          matchingNodes[indx].activeStep = null;
        matchingCount = 0;
      }

      internal void InitCandidateNodes(StdMatchNode<S>.ChildList candidates) {
        Debug.Assert(candidates != null);
        ClearCandidateNodes();
        AddCandidateNodes(candidates);
      }

      public StdPathStep(StdEventDispatcher<S> owner, StdPathStep parent) {
        Debug.Assert(owner != null);
        this.owner = owner;
        this.parent = parent;
      }

      /// <summary><see cref="StdEventDispatcher&lt;S>"/> instance which owns this
      /// <see cref="XmlPathStep"/> instance.</summary>
      public StdEventDispatcher<S> Owner {
        get { return owner; }
      }

      /// <summary>Implementation of <see cref="XmlPathStep.Parent"/>.</summary>
      public override XmlPathStep Parent {
        get { return parent; }
      }

      /// <summary>Child (next) step in path down the XML document.</summary>
      public StdPathStep Child {
        get { return child; }
      }

      /// <summary>Implementation of <see cref="XmlPathStep.Name"/>.</summary>
      public override QName Name {
        get { return name; }
      }

      /// <summary>Implementation of <see cref="XmlPathStep.Attributes"/>.</summary>
      public override IAttributeList Attributes {
        get { return attributes; }
      }

      /// <summary>Resets the state, prepared for next activation.</summary>
      public void Reset() {
        ClearMatchingNodes();
        Array.Clear(matchingNodes, 0, matchingNodes.Length);
      }

      /// <summary>Resets internally allocated resources to initial size.</summary>
      public void ResetResources(bool clearChild) {
        ClearCandidateNodes();
        candidateNodes = new StdMatchNode<S>[2];
        ClearMatchingNodes();
        matchingNodes = new StdMatchNode<S>[2];
        name = QName.Empty;
        attributes = null;
        if (clearChild)
          child = null;
      }

      /// <summary>Activates the next path step (when the start tag of the next element
      /// is encountered), generating a list of matching nodes, and a list of candidate
      /// nodes for the following level down.</summary>
      /// <seealso cref="IElementHandler&lt;N, S>.Activate"/>
      /// <param name="name">Element name to match.</param>
      /// <param name="attributes">Attribute list to match.</param>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void Activate(QName name, AttributeList attributes, S status) {
        this.name = name;
        this.attributes = attributes;

        StdPathStep child = GetChildNew();
        child.ClearCandidateNodes();
        // match all candidate nodes
        for (int candIndex = 0; candIndex < candidateCount; candIndex++) {
          StdMatchNode<S> candNode = candidateNodes[candIndex];
          // add relative node to matching nodes list of child XmlPathStep
          if (!candNode.isAbsolute)
            child.AddCandidateNode(candNode);
          // if a match, add children to candidate-list of child XmlPathStep
          if (candNode.Matches(this)) {
            child.AddCandidateNodes(candNode.children);
            AddMatchingNode(candNode);
          }
        }

        CallActivate(this, status);
      }

      /// <summary>Deactivates the current path step (when the end tag of the matching
      /// element is encountered), calling <see cref="IElementHandler&lt;N, S>.Deactivate"/>
      /// on all matching nodes and then clearing the list of matching nodes.</summary>
      /// <remarks>Also pops all associated attributes from the stack.</remarks>
      /// <seealso cref="IElementHandler&lt;N, S>.Deactivate"/>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void Deactivate(S status) {
        CallDeactivate(status);
        ClearMatchingNodes();
        // name = QName.Empty;
        // attributes = null;
      }

      /// <summary>Removes a given number of nodes from the list of candidate nodes,
      /// starting at a given node instance.</summary>
      /// <param name="start">Starting node.</param>
      /// <param name="count">Number of nodes to remove.</param>
      public void RemoveCandidates(StdMatchNode<S> start, int count) {
        Debug.Assert(start != null);
        int startIndex = Array.IndexOf<StdMatchNode<S>>(candidateNodes, start);
        if (startIndex == -1)
          return;
        int blockIndex = startIndex + count;
        Array.Copy(candidateNodes, blockIndex,
                   candidateNodes, startIndex,
                   candidateCount - blockIndex);
        candidateCount -= count;
      }

      /* handler call-back routines - these iterate over the matching path nodes */

      private void CallActivate(StdPathStep step, S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          StdMatchNode<S> matchedNode = matchingNodes[matchIndex];
          IElementHandler<StdMatchNode<S>, S> handler = matchedNode.Handler;
          if (handler != null) {
            handler.Activate(matchedNode, step, status);
            status.Check();
          }
        }
      }

      private void CallDeactivate(S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          IElementHandler<StdMatchNode<S>, S> handler = matchingNodes[matchIndex].Handler;
          if (handler != null) {
            handler.Deactivate(status);
            status.Check();
          }
        }
      }

      /// <summary>Called when character data in the immediate scope
      /// of the active element are encountered.</summary>
      /// <seealso cref="IElementHandler&lt;N, S>.Characters"/>
      /// <param name="chars">Array holding a chunk of the element's character data.</param>
      /// <param name="start">Start index of character data.</param>
      /// <param name="length">Length of character data.</param>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void CallCharacters(char[] chars, int start, int length, S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          IElementHandler<StdMatchNode<S>, S> handler = matchingNodes[matchIndex].Handler;
          if (handler != null) {
            handler.Characters(chars, start, length, status);
            status.Check();
          }
        }
      }

      /// <summary>Called when ignorable whitespace in the immediate scope
      /// of the active element is encountered.</summary>
      /// <seealso cref="IElementHandler&lt;N, S>.IgnorableWhitespace"/>
      /// <param name="ws">Array holding a chunk of the element's whitespace.</param>
      /// <param name="start">Start index of whitespace.</param>
      /// <param name="length">Length of whitespace.</param>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void CallIgnorableWhitespace(char[] ws, int start, int length, S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          IElementHandler<StdMatchNode<S>, S> handler = matchingNodes[matchIndex].Handler;
          if (handler != null) {
            handler.IgnorableWhitespace(ws, start, length, status);
            status.Check();
          }
        }
      }

      /// <summary>Called when the start tag of a child element of
      /// the currently active element is encountered.</summary>
      /// <remarks>This call must occur just before the next path step for the child
      /// element is activated. See <see cref="StdEventDispatcher&lt;S>.Activate"/>
      /// and <see cref="Activate"/>.</remarks>
      /// <seealso cref="IElementHandler&lt;N, S>.StartChild"/>
      /// <param name="name">Name of child element.</param>
      /// <param name="attributes">Attributes of child element.</param>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void CallStartChild(QName name, IAttributeList attributes, S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          IElementHandler<StdMatchNode<S>, S> handler = matchingNodes[matchIndex].Handler;
          if (handler != null) {
            handler.StartChild(name, attributes, status);
            status.Check();
          }
        }
      }

      /// <summary>Called when the end tag of a child element of
      /// the currently active element is encountered.</summary>
      /// <remarks>This call must occur just after the next path step for the child
      /// element was deactivated. See <see cref="StdEventDispatcher&lt;S>.Deactivate"/>
      /// and <see cref="Deactivate"/>.</remarks>
      /// <seealso cref="IElementHandler&lt;N, S>.EndChild"/>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void CallEndChild(S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          IElementHandler<StdMatchNode<S>, S> handler = matchingNodes[matchIndex].Handler;
          if (handler != null) {
            handler.EndChild(status);
            status.Check();
          }
        }
      }

      /// <summary>Called when a processing instruction in the immediate
      /// scope of the active element is encountered.</summary>
      /// <param name="target">The processing instruction target.</param>
      /// <param name="data">The processing instruction data, or <c>null</c> if none
      /// was supplied.</param>
      /// <seealso cref="IXmlHandler&lt;S>.ProcessingInstruction"/>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void CallProcessingInstruction(string target, string data, S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          IElementHandler<StdMatchNode<S>, S> handler = matchingNodes[matchIndex].Handler;
          if (handler != null) {
            handler.ProcessingInstruction(target, data, status);
            status.Check();
          }
        }
      }

      /// <summary>Called when a general entity reference is skipped.</summary>
      /// <seealso cref="IXmlHandler&lt;S>.SkippedEntity"/>
      /// <param name="name">Name of general entity.</param>
      /// <param name="status"><see cref="EventStatus"/> instance assigned to 
      /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
      public void CallSkippedEntity(string name, S status) {
        status.Reset();
        for (int matchIndex = 0; matchIndex < matchingCount; matchIndex++) {
          IElementHandler<StdMatchNode<S>, S> handler = matchingNodes[matchIndex].Handler;
          if (handler != null) {
            handler.SkippedEntity(name, status);
            status.Check();
          }
        }
      }
    }

    #region Attribute Handling Support

    /// <summary>Internal implementation of <see cref="IAttribute"/>.</summary>
    public class Attribute: IAttribute
    {
      internal AttributeList owner;
      internal string localName;
      internal string nsUri;
      internal string qualName;
      internal string value;
      internal bool isSpecified;

      /// <summary>Clears all fields.</summary>
      internal protected void Clear() {
        owner = null;
        localName = null;
        nsUri = null;
        qualName = null;
        value = null;
      }

      /// <summary>Performs match against a qualified name.</summary>
      public bool NameMatches(QName name) {
        return localName == name.LocalName && nsUri == name.NsUri;
      }

      /// <summary>Performs match against a prefixed name.</summary>
      public bool NameMatches(string qName) {
        return qualName == qName;
      }

      #region IAttribute

      /// <summary>Implements <see cref="IAttribute.Name"/>.</summary>
      public QName Name {
        get { return new QName(localName, nsUri); }
      }

      /// <summary>Implements <see cref="IAttribute.Value"/>.</summary>
      public string Value {
        get { return value; }
      }

      /// <summary>Implements <see cref="IAttribute.Type"/>.</summary>
      /// <remarks>Enumerated attributes are reported as declared, and not
      /// as 'ENUMERATION' (according to the InfoSet spec), or as 'NMTOKEN'
      /// (as required by the SAX2 spec).</remarks>
      public string Type {
        get { return owner.GetAttType(qualName); }
      }

      /// <summary>Implements <see cref="IAttribute.IsSpecified"/>.</summary>
      public bool IsSpecified {
        get { return isSpecified; }
      }

      #endregion
    }

    /// <summary>Internal implementation of <see cref="IAttributeList"/>.</summary>
    public class AttributeList: IAttributeList
    {
      private StdEventDispatcher<S> owner;
      internal string elmName;
      internal int start;
      internal int count;

      internal AttributeList(StdEventDispatcher<S> owner) {
        this.owner = owner;
      }

      internal protected void Clear() {
        elmName = null;
        start = 0;
        count = 0;
      }

      /// <summary>Returns attribute type, as declared in DTD.</summary>
      /// <remarks>Uses the <see cref="StdEventDispatcher&lt;S>.AttDecls"/>
      /// dictionary of attribute declarations. This is based on matching XML
      /// QNames, i.e. the declarations in the DTD must have the same prefixes
      /// and local names as the element and attribute names encountered in the
      /// document, since DTDs are not namespace aware. Matching Uri + local name
      /// would not work.</remarks>
      /// <param name="qName">Prefix qualified name.</param>
      /// <returns>Declared attribute type, or "UNDECLARED" if no attribute 
      /// declaration was read.</returns>
      internal protected string GetAttType(string qName) {
        AttDecl decl;
        if (owner.AttDecls.TryGetValue(new AttDeclKey(elmName, qName), out decl))
          return decl.AttType;
        else
          return "UNDECLARED";  //kw define constant
      }

      /// <summary>Prefix qualified name of element.</summary>
      public string ElmName {
        get { return elmName; }
      }

      /// <summary>Start index into attributes array.</summary>
      public int Start {
        get { return start; }
      }

      #region IAttributeList

      /// <summary>Implements <see cref="IAttributeList.Count"/>.</summary>
      public int Count {
        get { return count; }
      }

      /// <summary>Implements <see cref="IAttributeList.GetIndex(QName)"/>.</summary>
      public int GetIndex(QName name) {
        int indx = count - 1;
        int attIndx = start + indx;
        while (indx >= 0) {
          if (owner.attributes[attIndx].NameMatches(name))
            break;
          attIndx--;
          indx--;
        }
        return indx;
      }

      /// <summary>Implements <see cref="IAttributeList.GetIndex(string)"/>.</summary>
      public int GetIndex(string qName) {
        int indx = count - 1;
        int attIndx = start + indx;
        while (indx >= 0) {
          if (owner.attributes[attIndx].NameMatches(qName))
            break;
          attIndx--;
          indx--;
        }
        return indx;
      }

      /// <summary>Implements <see cref="IAttributeList.this[int]"/>.</summary>
      public IAttribute this[int index] {
        get {
          if (index < 0 || index >= count)
            throw new IndexOutOfRangeException();
          return owner.attributes[start + index];
        }
      }

      /// <summary>Implements <see cref="IAttributeList.this[QName]"/>.</summary>
      public IAttribute this[QName name] {
        get {
          int index = GetIndex(name);
          if (index < 0)
            return null;
          return owner.attributes[start + index];
        }
      }

      /// <summary>Implements <see cref="IAttributeList.this[string]"/>.</summary>
      public IAttribute this[string qName] {
        get {
          int index = GetIndex(qName);
          if (index < 0)
            return null;
          return owner.attributes[start + index];
        }
      }

      #endregion
    }

    public const int InitAttributeCapacity = 8;
    public const int InitAttributeListCapacity = 4;
    public const int InitPathDepth = 6;

    private Attribute[] attributes;
    private int attIndex;  // next free attribute index
    private AttributeList[] attributeLists;
    private int attListIndex;
    private Dictionary<AttDeclKey, AttDecl> attDecls;

    // assumes attributes.Length > 0! not checked!
    private void CheckAttributeIndex(int index) {
      int oldSize = attributes.Length;
      if (index < oldSize)
        return;
      int newSize = oldSize;
      do {
        newSize = newSize << 1;
      } while (newSize <= index);
      Array.Resize(ref attributes, newSize);
      for (index = oldSize; index < newSize; index++)
        attributes[index] = new Attribute();
    }

    /// <summary>Pushes new <see cref="Attribute"/> instance on attribute stack
    /// and initializes it.</summary>
    /// <remarks>Should only be called in conjunction with initializing a new
    /// <see cref="StdPathStep">path step</see>.</remarks>
    /// <returns>New <see cref="Attribute"/> instance.</returns>
    protected Attribute PushAttribute(
      string localName,
      string nsUri,
      string qualName,
      string value,
      bool isSpecified)
    {
      CheckAttributeIndex(attIndex);
      Attribute result = attributes[attIndex++];
      result.localName = localName;
      result.nsUri = nsUri;
      result.qualName = qualName;
      result.value = value;
      result.isSpecified = isSpecified;
      return result;
    }

    /// <summary>Trims internal attribute list to the initial size.</summary>
    protected void ResetAttributes() {
      // no need to create new attributes, as this is the minimum capacity
      Array.Resize(ref attributes, InitAttributeCapacity);
      Array.Resize(ref attributeLists, InitAttributeListCapacity);
    }

    // assumes attributes.Length > 0! not checked!
    private void CheckAttributeListIndex(int index) {
      int oldSize = attributeLists.Length;
      if (index < oldSize)
        return;
      int newSize = oldSize;
      do {
        newSize = newSize << 1;
      } while (newSize <= index);
      Array.Resize(ref attributeLists, newSize);
      for (index = oldSize; index < newSize; index++)
        attributeLists[index] = new AttributeList(this);
    }

    /// <summary>Pushes new <see cref="AttributeList"/> instance on attribute list
    /// stack and initializes it.</summary>
    /// <remarks>Assumes that all related attributes are already on the attribute
    /// stack. Should only be called in conjunction with initializing a new
    /// <see cref="StdPathStep">path step</see>.</remarks>
    /// <returns>New <see cref="AttributeList"/> instance.</returns>
    protected AttributeList PushAttributeList(string elmName, int start, int count) {
      CheckAttributeListIndex(attListIndex);
      AttributeList result = attributeLists[attListIndex++];
      result.elmName = elmName;
      result.start = start;
      result.count = count;
      // set owner of associated attributes
      int indx = start + count;
      while (count > 0) {
        attributes[--indx].owner = result;
        count--;
      }
      return result;
    }

    /// <summary>Pops the current attribute list and its related attributes from
    /// their respective stacks.</summary>
    /// <remarks>Should only be called in conjunction with deactivating the current
    /// <see cref="StdPathStep">path step</see>.</remarks>
    protected void PopAttributeList() {
      attListIndex--;
      AttributeList poppedList = attributeLists[attListIndex];
      attIndex = poppedList.Start;
      /* instead, we could clear the attributes and attribute lists
      int count = poppedList.Count;
      while (count > 0) {
        attributes[--attIndex].Clear();
        count--;
      }
      poppedList.Clear();  // needed?
      */
    }

    /// <summary>Index of next unused attribute on stack.</summary>
    protected int AttIndex {
      get { return attIndex; }
    }

    /// <summary>Index of next unused attribute list on stack.</summary>
    protected int AttListIndex {
      get { return attListIndex; }
    }

    /// <summary>Contains the attribute declarations found in the document's DTD.</summary>
    public Dictionary<AttDeclKey, AttDecl> AttDecls {
      get { return attDecls; }
    }

    #endregion

    private IDocHandler<S> docHandler;
    private StdMatchNode<S> matchRoot;
    private S statusObject;

    private StdPathStep pathRoot;
    private StdPathStep current = null;

    /// <summary>Throws an <see cref="XpeaException"/> indicating that some processing
    /// was done at an incorrect hierarchy level in the XML document.</summary>
    protected void DocLevelError() {
      string levelStr;
      if (Current == null)
        levelStr = Resources.GetString(RsId.InDocEntity);
      else
        levelStr = Resources.GetString(RsId.InElement);
      string msg = Resources.GetString(RsId.DocLevelError);
      throw new XpeaException(String.Format(msg, levelStr));
    }

    /// <summary>Internal root node that holds all registered pattern matching
    /// trees as its children.</summary>
    protected StdMatchNode<S> MatchRoot {
      get { return matchRoot; }
    }

    /// <summary>Root of processing path.</summary>
    protected StdPathStep PathRoot {
      get { return pathRoot; }
    }

    /// <summary>Currently active step/level in processing path.</summary>
    /// <remarks>This property is the main entry point for an XML parser
    /// adapter to access the XPEA parser interface.</remarks>
    protected StdPathStep Current {
      get { return current; }
    }

    /// <summary>Called when the start tag of an XML element is encountered.</summary>
    /// <remarks>The internal path representation advances to the next level down, all
    /// candidate <see cref="XmlMatchNode&lt;N, S>"/> instances are evaluated for a match,
    /// and for those that match, the <see cref="IElementHandler&lt;N, S>.Activate"/>
    /// handlers are called.</remarks>
    /// <param name="name">Name of element becoming active.</param>
    /// <param name="attributes">Attribute list of newly active element.</param>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    protected void Activate(QName name, AttributeList attributes, S status) {
      if (Current == null) {
        // initialize root candidates
        PathRoot.InitCandidateNodes(MatchRoot.children);
        current = PathRoot;
      }
      else
        current = Current.Child;
      Current.Activate(name, attributes, status);  // ensures a child exists
    }

    /// <summary>Called when the end tag of an XML element is encountered.</summary>
    /// <remarks>The <see cref="IElementHandler&lt;N, S>.Deactivate"/> handlers are called
    /// for all currently matching nodes, and the internal path representation moves back
    /// to the previous level up.</remarks>
    /// <param name="status"><see cref="EventStatus"/> instance assigned to 
    /// <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</param>
    protected void Deactivate(S status) {
      if (Current == null)
        DocLevelError();
      Current.Deactivate(status);
      current = Current.parent;
    }

    public StdEventDispatcher() {
      matchRoot = new StdMatchNode<S>();
      pathRoot = new StdPathStep(this, null);
      // allocate default path depth
      StdPathStep step = pathRoot;
      for (int depth = 0; depth < InitPathDepth; depth++ ) {
        step = step.GetChildNew();
      }
      attDecls = new Dictionary<AttDeclKey, AttDecl>();
      // CheckAttributeIndex requires attributes.Length > 0
      attributes = new Attribute[1] { new Attribute() };
      CheckAttributeIndex(InitAttributeCapacity - 1);
      attIndex = 0;
      // CheckAttributeListIndex requires attributeLists.Length > 0
      attributeLists = new AttributeList[1] { new AttributeList(this) };
      CheckAttributeListIndex(InitAttributeListCapacity - 1);
      attListIndex = 0;
    }

    /// <summary>Implements <see cref="EventDispatcher&lt;N, S>.DocHandler"/>.</summary>
    public override IDocHandler<S> DocHandler {
      get { return docHandler; }
      set { docHandler = value; }
    }

    /// <summary>Implements <see cref="EventDispatcher&lt;N, S>.MatchTrees"/>.</summary>
    public override IList<StdMatchNode<S>> MatchTrees {
      get { return matchRoot.Children; }
    }

    /// <summary>Implements <see cref="EventDispatcher&lt;N, S>.Reset"/>.</summary>
    public override void Reset() {
      current = null;
      attDecls.Clear();
      // call Reset on all handlers
      if (docHandler != null)
        docHandler.Reset();
      MatchRoot.Reset();
      // clear the matching nodes lists on all path elements (in case of left-overs)
      StdPathStep pathStep = PathRoot;  // root is always <> null
      do {
        pathStep.Reset();
        pathStep = pathStep.Child;
      } while (pathStep != null);
    }

    /// <summary>Implements <see cref="EventDispatcher&lt;N, S>.StatusObject"/>.</summary>
    public override S StatusObject {
      get { return statusObject; }
      set { statusObject = value; }
    }

    /// <summary>Resets internally allocated resources to initial size.</summary>
    /// <remarks>Useful to reduce the amount of allocated resources.
    /// Note: *Must not* be called while XML processing is under way!</remarks>
    public virtual void ResetResources() {
      ResetAttributes();
      attDecls = new Dictionary<AttDeclKey, AttDecl>();
      StdPathStep pathStep = PathRoot;  // root is always <> null
      // limit number of path steps to InitPathDepth
      int stepCount = 1;
      do {
        pathStep.ResetResources(stepCount >= InitPathDepth);  // clears Child when passing true
        pathStep = pathStep.Child;
        stepCount++;
      } while (pathStep != null);
    }
  }
}
