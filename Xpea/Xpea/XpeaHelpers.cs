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

namespace Org.System.Xml.Xpea.Helpers
{
  /// <summary>Default handler implementation base class - does nothing.</summary>
  /// <remarks>The advantage of deriving from this class is that it is not required for
  /// the subclass to implement members of <see cref="IXmlHandler&lt;S>"/> that would do nothing.</remarks>
  public class XmlHandler<S>: IXmlHandler<S> where S: EventStatus
  {
    /// <summary>No-op implementation of <see cref="IXmlHandler&lt;S>.Reset"/>.</summary>
    void IXmlHandler<S>.Reset() {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IXmlHandler&lt;S>.ProcessingInstruction"/>.</summary>
    void IXmlHandler<S>.ProcessingInstruction(string target, string data, S status) {
      // dIXmlHandler.efault - do nothing
    }

    /// <summary>No-op implementation of <see cref="IXmlHandler&lt;S>.SkippedEntity"/>.</summary>
    void IXmlHandler<S>.SkippedEntity(string name, S status) {
      // default - do nothing
    }
  }

  /// <summary>Default <see cref="IDocHandler&lt;S>"/> implementation - does nothing.</summary>
  /// <remarks>The advantage of deriving from this class is that it is not required for
  /// the subclass to implement members of <see cref="IDocHandler&lt;S>"/> that would do nothing.</remarks>
  public class XmlDocHandler<S>: XmlHandler<S>, IDocHandler<S>
    where S: EventStatus
  {
    /// <summary>No-op implementation of <see cref="IDocHandler&lt;S>.StartDocument"/>.</summary>
    void IDocHandler<S>.StartDocument(object context, S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IDocHandler&lt;S>.EndDocument"/>.</summary>
    void IDocHandler<S>.EndDocument(S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IDocHandler&lt;S>.StartRoot"/>.</summary>
    void IDocHandler<S>.StartRoot(QName name, IAttributeList attributes, S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IDocHandler&lt;S>.EndRoot"/>.</summary>
    void IDocHandler<S>.EndRoot(S status) {
      // default - do nothing
    }
  }

  /// <summary>Default <see cref="IElementHandler&lt;N, S>"/> implementation - does nothing.</summary>
  /// <remarks>The advantage of deriving from this class is that it is not required for the
  /// subclass to implement members of <see cref="IElementHandler&lt;N, S>"/> that would do nothing.</remarks>
  public class XmlElementHandler<N, S>: XmlHandler<S>, IElementHandler<N, S>
    where N: XmlMatchNode<N, S>
    where S: EventStatus
  {
    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.Activate"/>.</summary>
    void IElementHandler<N, S>.Activate(N matchedNode, XmlPathStep step, S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.Characters"/>.</summary>
    void IElementHandler<N, S>.Characters(char[] chars, int start, int length, S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.IgnorableWhitespace"/>.</summary>
    void IElementHandler<N, S>.IgnorableWhitespace(char[] ws, int start, int length, S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.StartChild"/>.</summary>
    void IElementHandler<N, S>.StartChild(QName name, IAttributeList attributes, S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.EndChild"/>.</summary>
    void IElementHandler<N, S>.EndChild(S status) {
      // default - do nothing
    }

    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.Deactivate"/>.</summary>
    void IElementHandler<N, S>.Deactivate(S status) {
      // default - do nothing
    }
  }

  /// <summary>This helper class is intended to be used with element types that
  /// allow character data, like #PCDATA or mixed content types.</summary>
  /// <remarks>It accumulates character data in a buffer and can handle nested
  /// activations (e.g if an element is a descendant of itself).
  /// Ignorable whitespace (unlikely to happen) is discarded by default.</remarks>
  public class CharDataHandler
  {
    /// <summary>Represents the buffer for one level of activation.</summary>
    /// <remarks>Note: As an array of structs, saving an array element to a
    /// variable copies it and the reference to the original is lost, which
    /// means that updates apply to the copy only.</remarks>
    public struct LevelBuffer
    {
      private char[] buffer;
      private int bufPos;

      internal void Reset(int newBufSize) {
        bufPos = 0;
        if (newBufSize >= 0)
          buffer = new char[newBufSize];
      }

      internal void AppendChars(char[] ch, int start, int length) {
        int newLen = bufPos + length;
        int len = buffer.Length;
        if (newLen > len) {
          // double  buffer size until it exceeds required size
          do {
            if (len < 4)
              len = 8;
            else
              len = len << 1;
          } while (len < newLen);
          char[] newBuffer = new char[len];
          buffer.CopyTo(newBuffer, 0);
          buffer = newBuffer;
        }
        Array.Copy(ch, start, buffer, bufPos, length);
        bufPos = newLen;
      }

      /// <summary>Character array representing the buffer.</summary>
      public char[] Buffer {
        get { return buffer; }
      }

      /// <summary>Current buffer position.</summary>
      public int BufPos {
        get { return bufPos; }
      }
    }

    private LevelBuffer[] levelBuffers;
    private short level;
    private bool useIgnorableWhitespace;

    private void IncLevelBuffersLength(short maxLevel) {
      int newLen;
      if (maxLevel < 2)
        newLen = 4;
      else
        newLen = maxLevel << 1;
      LevelBuffer[] newLevelBuffers = new LevelBuffer[newLen];
      levelBuffers.CopyTo(newLevelBuffers, 0);
      // old buffers copied, need to create new buffers
      for (int indx = levelBuffers.Length; indx < newLevelBuffers.Length; indx++)
        newLevelBuffers[indx].Reset(0);
      levelBuffers = newLevelBuffers;
    }

    private void IncLevel() {
      short newLevel = (short)(level + 1);
      if (newLevel >= levelBuffers.Length)
        IncLevelBuffersLength(newLevel);
      level = newLevel;
    }

    private void DecLevel() {
      level--;
    }

    /// <summary>Array of level states.</summary>
    protected LevelBuffer[] LevelBuffers {
      get { return levelBuffers; }
    }

    /// <summary>Appends character data to buffer at current nesting level.</summary>
    /// <param name="ch">Array containing characters to append.</param>
    /// <param name="start">Start index of characters.</param>
    /// <param name="length">Number of characters.</param>
    protected void AppendChars(char[] ch, int start, int length) {
      levelBuffers[level].AppendChars(ch, start, length);
    }

    public CharDataHandler() {
      level = -1;
      levelBuffers = new LevelBuffer[2];
      for (int indx = 0; indx < levelBuffers.Length; indx++)
        levelBuffers[indx].Reset(0);
    }

    /// <summary>Nesting level (of itself).</summary>
    public short Level {
      get { return level; }
    }

    /// <summary>Returns buffer at current nesting level.</summary>
    public char[] CurrentBuffer {
      get { return levelBuffers[level].Buffer; }
    }

    /// <summary>Returns buffer position at current nesting level.</summary>
    public int CurrentBufPos {
      get { return levelBuffers[level].BufPos; }
    }

    /// <summary>Resets buffer size and position at current nesting level.</summary>
    /// <param name="newBufSize">New buffer size. If it is <c>&lt; 0</c> then
    /// the buffers size will not be modified.</param>
    public void ResetCurrentLevel(int newBufSize) {
      levelBuffers[level].Reset(newBufSize);
    }

    /// <summary>If <c>true</c>, ignorable whitespace will not be discarded.</summary>
    public bool UseIgnorableWhitespace {
      get { return useIgnorableWhitespace; }
      set { useIgnorableWhitespace = value; }
    }

    /// <summary>Re-initializes internal state.</summary>
    public void Reset() {
      level = -1;
    }

    /// <summary>Appends ignorable whitespace to buffer at current nesting level.</summary>
    /// <remarks>Depends on <see cref="UseIgnorableWhitespace"/>.</remarks>
    /// <param name="ws">Array containing whitespace characters to append.</param>
    /// <param name="start">Start index of characters.</param>
    /// <param name="length">Number of characters.</param>
    public void IgnorableWhitespace(char[] ws, int start, int length) {
      if (UseIgnorableWhitespace)
        AppendChars(ws, start, length);
    }

    /// <summary>To be called when next nesting level gets activated.</summary>
    public void Activate() {
      IncLevel();
      levelBuffers[level].Reset(-1);
    }

    /// <summary>Appends character data to buffer at current nesting level.</summary>
    /// <param name="chars">Array containing characters to append.</param>
    /// <param name="start">Start index of characters.</param>
    /// <param name="length">Number of characters.</param>
    public void Characters(char[] chars, int start, int length) {
      AppendChars(chars, start, length);
    }

    /// <summary>To be called when current nesting level gets deactivated.</summary>
    public void Deactivate() {
      DecLevel();
    }
  }

  /// <summary><see cref="IElementHandler&lt;N, S>"/> implementation that
  /// buffers character data and is well suited for implementating handlers
  /// for #PCDATA or mixed content element types.</summary>
  /// <remarks>In many cases it is sufficient for derived classes to override
  /// <see cref="OnReset"/>, <see cref="OnActivate"/> and <see cref="OnDeactivate"/>
  /// without having to re-implement any members of <see cref="IElementHandler&lt;N, S>"/>.</remarks>
  public class TextElementHandler<N, S>: XmlHandler<S>, IElementHandler<N, S>
    where N: XmlMatchNode<N, S>
    where S: EventStatus
  {
    private CharDataHandler charData;

    /// <summary>Returns reference to internal <see cref="CharDataHandler"/> instance.</summary>
    protected CharDataHandler CharData {
      get { return charData; }
    }

    /// <summary>For convenience - clears buffer at current level of nesting.</summary>
    protected void ClearBuffer() {
      CharData.ResetCurrentLevel(-1);
    }

    /// <summary>Copies text accumulated in buffer so far.</summary>
    protected string CopyBuffer() {
      return new string(CharData.CurrentBuffer, 0, CharData.CurrentBufPos);
    }

    /// <summary>Copies text accumulated in buffer from <c>startPos</c> to the current position.</summary>
    protected string CopyBuffer(int startPos) {
      return new string(CharData.CurrentBuffer, startPos, CharData.CurrentBufPos);
    }

    /// <summary>Triggered by <see cref="IXmlHandler&lt;S>.Reset"/> event.</summary>
    /// <remarks>Override instead of re-implementing the interface method.</remarks>
    protected virtual void OnReset() {
      // implement in descendant
    }

    /// <summary>Triggered by <see cref="IElementHandler&lt;N, S>.Activate"/> event.</summary>
    /// <remarks>Override instead of re-implementing the interface method.</remarks>
    protected virtual void OnActivate(N matchedNode, XmlPathStep step, S status) {
      // implement in descendant
    }

    /// <summary>Triggered by <see cref="IElementHandler&lt;N, S>.Deactivate"/> event.</summary>
    /// <remarks>Override instead of re-implementing the interface method.</remarks>
    protected virtual void OnDeactivate(S status) {
      // implement in descendant
    }

    public TextElementHandler() {
      charData = new CharDataHandler();
    }

    // 
    /// <summary>If <c>true</c>, ignorable whitespace will not be discarded.</summary>
    public bool UseIgnorableWhitespace {
      get { return charData.UseIgnorableWhitespace; }
      set { charData.UseIgnorableWhitespace = value; }
    }

    /* IXmlHandler */

    /// <summary>Re-implements <see cref="IXmlHandler&lt;S>.Reset"/>.</summary>
    /// <remarks>Do not re-implement in derived classes - instead, override
    /// <see cref="OnReset"/></remarks>
    void IXmlHandler<S>.Reset() {
      CharData.Reset();
      OnReset();
    }

    /* IElementHandler */

    /// <summary>Implements <see cref="IElementHandler&lt;N, S>.Activate"/>.</summary>
    /// <remarks>Do not re-implement in derived classes - instead, override
    /// <see cref="OnActivate"/></remarks>
    void IElementHandler<N, S>.Activate(N matchedNode, XmlPathStep step, S status)
    {
      CharData.Activate();
      OnActivate(matchedNode, step, status);
    }

    /// <summary>Implements <see cref="IElementHandler&lt;N, S>.Characters"/>.</summary>
    /// <remarks>Needs re-implementing only if special treatment for CDATA is required.</remarks>
    void IElementHandler<N, S>.Characters(
      char[] chars,
      int start, 
      int length, 
      S status) 
    {
      CharData.Characters(chars, start, length);
    }

    /// <summary>Implements <see cref="IElementHandler&lt;N, S>.IgnorableWhitespace"/>.</summary>
    void IElementHandler<N, S>.IgnorableWhitespace(
      char[] ws,
      int start,
      int length,
      S status) 
    {
      CharData.IgnorableWhitespace(ws, start, length);
    }

    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.StartChild"/>.</summary>
    /// <remarks>Re-implement in derived class if needed.</remarks>
    void IElementHandler<N, S>.StartChild(QName name, IAttributeList attributes, S status) {
      // implement in descendant
    }

    /// <summary>No-op implementation of <see cref="IElementHandler&lt;N, S>.EndChild"/>.</summary>
    /// <remarks>Re-implement in derived class if needed.</remarks>
    void IElementHandler<N, S>.EndChild(S status) {
      // implement in descendant
    }

    /// <summary>Implements <see cref="IElementHandler&lt;N, S>.Deactivate"/>.</summary>
    /// <remarks>Do not re-implement in derived classes - instead, override
    /// <see cref="OnDeactivate"/></remarks>
    void IElementHandler<N, S>.Deactivate(S status) {
      OnDeactivate(status);
      CharData.Deactivate();
    }
  }

  /// <summary>Container for manipulating node paths (as opposed to trees).</summary>
  /// <remarks>This linear form of a pattern matching "tree" is conceptually similar
  /// to an XPath expression. It is therefore also reasonable to define a textual
  /// XPath-like represenation which can be passed to some <c>XmlPath</c> factory.</remarks>
  /// <typeparam name="N"><see cref="XmlMatchNode&lt;N, S>"/> subclass which is the
  /// base class for all nodes in a specific implementation.</typeparam>
  /// <typeparam name="S">Type of status object passed to call-backs.</typeparam>
  public class XmlPath<N, S>
    where N: XmlMatchNode<N, S>
    where S: EventStatus
  {
    private N root;
    private N leaf;

    /// <summary>Initializes a new instance of the <see cref="XmlPath&lt;N, S>"/> class.</summary>
    /// <remarks>It is the callers responsibility to pass a proper path.</remarks>
    /// <param name="root">Root (start) node of new path.</param>
    /// <param name="leaf">Leaf (end) node of new path.</param>
    protected XmlPath(N root, N leaf) {
      if (root == null)
        throw new ArgumentNullException("root");
      this.root = root;
      if (leaf == null)
        throw new ArgumentNullException("leaf");
      this.leaf = leaf;
    }

    /// <summary>Root (start) of path.</summary>
    public N Root {
      get { return root; }
    }

    /// <summary>Leaf (end) of path.</summary>
    public N Leaf {
      get { return leaf; }
    }

    /// <summary>Adds path (root) to a list of child nodes.</summary>
    /// <param name="childList">List of child nodes.</param>
    public void AddTo(IList<N> childList) {
      childList.Add(root);
    }

    /// <summary>Appends this path to another path (at leaf node).</summary>
    /// <param name="path">Path to append this path to.</param>
    public void AppendTo(XmlPath<N, S> path) {
      AddTo(path.leaf.Children);
    }

    /// <summary>Detaches path (root) from parent node.</summary>
    public void Detach() {
      root.Detach();
    }
  }
}