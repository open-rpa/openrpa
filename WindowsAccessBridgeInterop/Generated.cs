// Copyright 2016 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// ReSharper disable InconsistentNaming
// ReSharper disable DelegateSubtraction
// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
using System;
using System.Runtime.InteropServices;
using System.Text;
using WindowHandle = System.IntPtr;
using BOOL = System.Int32;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Common (i.e. legacy and non-legacy) abstraction over <code>WindowsAccessBridge DLL</code> functions
  /// </summary>
  public abstract class AccessBridgeFunctions {
    public abstract bool ActivateAccessibleHyperlink(int vmid, JavaObjectHandle accessibleContext, JavaObjectHandle accessibleHyperlink);
    public abstract void AddAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel, int i);
    public abstract void ClearAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel);
    public abstract bool DoAccessibleActions(int vmid, JavaObjectHandle accessibleContext, ref AccessibleActionsToDo actionsToDo, out int failure);
    public abstract bool GetAccessibleActions(int vmid, JavaObjectHandle accessibleContext, out AccessibleActions actions);
    /// <summary>
    /// Returns the accessible context of the <paramref name="i" />'th child of
    /// the component associated to <paramref name="ac" />. Returns an
    /// <code>null</code> accessible context if there is no such child.
    /// </summary>
    public abstract JavaObjectHandle GetAccessibleChildFromContext(int vmid, JavaObjectHandle ac, int i);
    public abstract bool GetAccessibleContextAt(int vmid, JavaObjectHandle acParent, int x, int y, out JavaObjectHandle ac);
    public abstract bool GetAccessibleContextFromHWND(WindowHandle window, out int vmid, out JavaObjectHandle ac);
    /// <summary>
    /// Retrieves the <see cref="AccessibleContextInfo" /> of the component
    /// corresponding to the given accessible context.
    /// </summary>
    public abstract bool GetAccessibleContextInfo(int vmid, JavaObjectHandle ac, out AccessibleContextInfo info);
    public abstract bool GetAccessibleContextWithFocus(WindowHandle window, out int vmid, out JavaObjectHandle ac);
    public abstract bool GetAccessibleHyperlink(int vmid, JavaObjectHandle hypertext, int nIndex, out AccessibleHyperlinkInfo hyperlinkInfo);
    public abstract int GetAccessibleHyperlinkCount(int vmid, JavaObjectHandle accessibleContext);
    public abstract bool GetAccessibleHypertext(int vmid, JavaObjectHandle accessibleContext, out AccessibleHypertextInfo hypertextInfo);
    public abstract bool GetAccessibleHypertextExt(int vmid, JavaObjectHandle accessibleContext, int nStartIndex, out AccessibleHypertextInfo hypertextInfo);
    public abstract int GetAccessibleHypertextLinkIndex(int vmid, JavaObjectHandle hypertext, int nIndex);
    public abstract bool GetAccessibleIcons(int vmid, JavaObjectHandle accessibleContext, out AccessibleIcons icons);
    public abstract bool GetAccessibleKeyBindings(int vmid, JavaObjectHandle accessibleContext, out AccessibleKeyBindings keyBindings);
    /// <summary>
    /// Returns the accessible context of the parent of the component associated
    /// to <paramref name="ac" />. Returns an <code>null</code> accessible
    /// context if there is no parent.
    /// </summary>
    public abstract JavaObjectHandle GetAccessibleParentFromContext(int vmid, JavaObjectHandle ac);
    public abstract bool GetAccessibleRelationSet(int vmid, JavaObjectHandle accessibleContext, out AccessibleRelationSetInfo relationSetInfo);
    public abstract int GetAccessibleSelectionCountFromContext(int vmid, JavaObjectHandle asel);
    public abstract JavaObjectHandle GetAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel, int i);
    public abstract bool GetAccessibleTableCellInfo(int vmid, JavaObjectHandle at, int row, int column, out AccessibleTableCellInfo tableCellInfo);
    /// <summary>
    /// Return the column number for a cell at a given index
    /// </summary>
    public abstract int GetAccessibleTableColumn(int vmid, JavaObjectHandle table, int index);
    public abstract JavaObjectHandle GetAccessibleTableColumnDescription(int vmid, JavaObjectHandle acParent, int column);
    public abstract bool GetAccessibleTableColumnHeader(int vmid, JavaObjectHandle acParent, out AccessibleTableInfo tableInfo);
    public abstract int GetAccessibleTableColumnSelectionCount(int vmid, JavaObjectHandle table);
    public abstract bool GetAccessibleTableColumnSelections(int vmid, JavaObjectHandle table, int count, [Out]int[] selections);
    /// <summary>
    /// Return the index of a cell at a given row and column
    /// </summary>
    public abstract int GetAccessibleTableIndex(int vmid, JavaObjectHandle table, int row, int column);
    public abstract bool GetAccessibleTableInfo(int vmid, JavaObjectHandle ac, out AccessibleTableInfo tableInfo);
    /// <summary>
    /// Return the row number for a cell at a given index
    /// </summary>
    public abstract int GetAccessibleTableRow(int vmid, JavaObjectHandle table, int index);
    public abstract JavaObjectHandle GetAccessibleTableRowDescription(int vmid, JavaObjectHandle acParent, int row);
    public abstract bool GetAccessibleTableRowHeader(int vmid, JavaObjectHandle acParent, out AccessibleTableInfo tableInfo);
    public abstract int GetAccessibleTableRowSelectionCount(int vmid, JavaObjectHandle table);
    public abstract bool GetAccessibleTableRowSelections(int vmid, JavaObjectHandle table, int count, [Out]int[] selections);
    public abstract bool GetAccessibleTextAttributes(int vmid, JavaObjectHandle at, int index, out AccessibleTextAttributesInfo attributes);
    public abstract bool GetAccessibleTextInfo(int vmid, JavaObjectHandle at, out AccessibleTextInfo textInfo, int x, int y);
    public abstract bool GetAccessibleTextItems(int vmid, JavaObjectHandle at, out AccessibleTextItemsInfo textItems, int index);
    public abstract bool GetAccessibleTextLineBounds(int vmid, JavaObjectHandle at, int index, out int startIndex, out int endIndex);
    public abstract bool GetAccessibleTextRange(int vmid, JavaObjectHandle at, int start, int end, [Out]char[] text, short len);
    public abstract bool GetAccessibleTextRect(int vmid, JavaObjectHandle at, out AccessibleTextRectInfo rectInfo, int index);
    public abstract bool GetAccessibleTextSelectionInfo(int vmid, JavaObjectHandle at, out AccessibleTextSelectionInfo textSelection);
    public abstract JavaObjectHandle GetActiveDescendent(int vmid, JavaObjectHandle ac);
    public abstract bool GetCaretLocation(int vmid, JavaObjectHandle ac, out AccessibleTextRectInfo rectInfo, int index);
    public abstract bool GetCurrentAccessibleValueFromContext(int vmid, JavaObjectHandle av, StringBuilder value, short len);
    /// <summary>
    /// Returns the window handle corresponding to given root component
    /// accessible context.
    /// </summary>
    public abstract WindowHandle GetHWNDFromAccessibleContext(int vmid, JavaObjectHandle ac);
    public abstract bool GetMaximumAccessibleValueFromContext(int vmid, JavaObjectHandle av, StringBuilder value, short len);
    public abstract bool GetMinimumAccessibleValueFromContext(int vmid, JavaObjectHandle av, StringBuilder value, short len);
    public abstract int GetObjectDepth(int vmid, JavaObjectHandle ac);
    public abstract JavaObjectHandle GetParentWithRole(int vmid, JavaObjectHandle ac, string role);
    public abstract JavaObjectHandle GetParentWithRoleElseRoot(int vmid, JavaObjectHandle ac, string role);
    public abstract bool GetTextAttributesInRange(int vmid, JavaObjectHandle accessibleContext, int startIndex, int endIndex, out AccessibleTextAttributesInfo attributes, out short len);
    public abstract JavaObjectHandle GetTopLevelObject(int vmid, JavaObjectHandle ac);
    public abstract bool GetVersionInfo(int vmid, out AccessBridgeVersionInfo info);
    public abstract bool GetVirtualAccessibleName(int vmid, JavaObjectHandle ac, StringBuilder name, int len);
    public abstract bool GetVisibleChildren(int vmid, JavaObjectHandle accessibleContext, int startIndex, out VisibleChildrenInfo children);
    public abstract int GetVisibleChildrenCount(int vmid, JavaObjectHandle accessibleContext);
    public abstract bool IsAccessibleChildSelectedFromContext(int vmid, JavaObjectHandle asel, int i);
    public abstract bool IsAccessibleTableColumnSelected(int vmid, JavaObjectHandle table, int column);
    public abstract bool IsAccessibleTableRowSelected(int vmid, JavaObjectHandle table, int row);
    /// <summary>
    /// Returns <code>true</code> if the given window handle belongs to a Java
    /// application.
    /// </summary>
    public abstract bool IsJavaWindow(WindowHandle window);
    /// <summary>
    /// Returns <code>true</code> if both java object handles reference the same
    /// object. Note that the function may return <cdoe>false</cdoe> for
    /// tansient objects, e.g. for items of tables/list/trees.
    /// </summary>
    public abstract bool IsSameObject(int vmid, JavaObjectHandle obj1, JavaObjectHandle obj2);
    public abstract void RemoveAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel, int i);
    public abstract void SelectAllAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel);
    public abstract bool SetTextContents(int vmid, JavaObjectHandle ac, string text);
    /// <summary>
    /// Initialization, needs to be called before any other entry point.
    /// </summary>
    public abstract void Windows_run();
  }

  /// <summary>
  /// Common (i.e. legacy and non-legacy)  abstraction over <code>WindowsAccessBridge DLL</code> events
  /// </summary>
  public abstract class AccessBridgeEvents : IDisposable {
    /// <summary>
    /// Occurs when the caret location inside an accessible text component has changed.
    /// </summary>
    public abstract event CaretUpdateEventHandler CaretUpdate;
    /// <summary>
    /// Occurs when an accessible component has gained the input focus.
    /// </summary>
    public abstract event FocusGainedEventHandler FocusGained;
    /// <summary>
    /// Occurs when an accessible component has lost the input focus.
    /// </summary>
    public abstract event FocusLostEventHandler FocusLost;
    /// <summary>
    /// Occurs when a JVM has shutdown.
    /// </summary>
    public abstract event JavaShutdownEventHandler JavaShutdown;
    public abstract event MenuCanceledEventHandler MenuCanceled;
    public abstract event MenuDeselectedEventHandler MenuDeselected;
    public abstract event MenuSelectedEventHandler MenuSelected;
    /// <summary>
    /// Occurs when the mouse button has been clicked on an accessible component.
    /// </summary>
    public abstract event MouseClickedEventHandler MouseClicked;
    /// <summary>
    /// Occurs when the mouse cursor has moved over the region of an accessible component.
    /// </summary>
    public abstract event MouseEnteredEventHandler MouseEntered;
    /// <summary>
    /// Occurs when the mouse cursor has exited the region of an accessible component.
    /// </summary>
    public abstract event MouseExitedEventHandler MouseExited;
    /// <summary>
    /// Occurs when the mouse button has been pressed of an accessible component.
    /// </summary>
    public abstract event MousePressedEventHandler MousePressed;
    /// <summary>
    /// Occurs when the mouse button has been released of an accessible component.
    /// </summary>
    public abstract event MouseReleasedEventHandler MouseReleased;
    public abstract event PopupMenuCanceledEventHandler PopupMenuCanceled;
    public abstract event PopupMenuWillBecomeInvisibleEventHandler PopupMenuWillBecomeInvisible;
    public abstract event PopupMenuWillBecomeVisibleEventHandler PopupMenuWillBecomeVisible;
    /// <summary>
    /// Occurs the active/selected item of a tree/list/table has changed.
    /// </summary>
    public abstract event PropertyActiveDescendentChangeEventHandler PropertyActiveDescendentChange;
    public abstract event PropertyCaretChangeEventHandler PropertyCaretChange;
    /// <summary>
    /// Unused.
    /// </summary>
    public abstract event PropertyChangeEventHandler PropertyChange;
    public abstract event PropertyChildChangeEventHandler PropertyChildChange;
    public abstract event PropertyDescriptionChangeEventHandler PropertyDescriptionChange;
    public abstract event PropertyNameChangeEventHandler PropertyNameChange;
    public abstract event PropertySelectionChangeEventHandler PropertySelectionChange;
    public abstract event PropertyStateChangeEventHandler PropertyStateChange;
    public abstract event PropertyTableModelChangeEventHandler PropertyTableModelChange;
    public abstract event PropertyTextChangeEventHandler PropertyTextChange;
    public abstract event PropertyValueChangeEventHandler PropertyValueChange;
    public abstract event PropertyVisibleDataChangeEventHandler PropertyVisibleDataChange;

    public abstract void Dispose();
  }

  #region Delegate types for events defined in AccessBridgeEvents
  /// <summary>Delegate type for <code>CaretUpdate</code> event</summary>
  public delegate void CaretUpdateEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>FocusGained</code> event</summary>
  public delegate void FocusGainedEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>FocusLost</code> event</summary>
  public delegate void FocusLostEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>JavaShutdown</code> event</summary>
  public delegate void JavaShutdownEventHandler(int vmid);

  /// <summary>Delegate type for <code>MenuCanceled</code> event</summary>
  public delegate void MenuCanceledEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>MenuDeselected</code> event</summary>
  public delegate void MenuDeselectedEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>MenuSelected</code> event</summary>
  public delegate void MenuSelectedEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>MouseClicked</code> event</summary>
  public delegate void MouseClickedEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>MouseEntered</code> event</summary>
  public delegate void MouseEnteredEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>MouseExited</code> event</summary>
  public delegate void MouseExitedEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>MousePressed</code> event</summary>
  public delegate void MousePressedEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>MouseReleased</code> event</summary>
  public delegate void MouseReleasedEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>PopupMenuCanceled</code> event</summary>
  public delegate void PopupMenuCanceledEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>PopupMenuWillBecomeInvisible</code> event</summary>
  public delegate void PopupMenuWillBecomeInvisibleEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>PopupMenuWillBecomeVisible</code> event</summary>
  public delegate void PopupMenuWillBecomeVisibleEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>PropertyActiveDescendentChange</code> event</summary>
  public delegate void PropertyActiveDescendentChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, JavaObjectHandle oldActiveDescendent, JavaObjectHandle newActiveDescendent);

  /// <summary>Delegate type for <code>PropertyCaretChange</code> event</summary>
  public delegate void PropertyCaretChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, int oldPosition, int newPosition);

  /// <summary>Delegate type for <code>PropertyChange</code> event</summary>
  public delegate void PropertyChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string property, string oldValue, string newValue);

  /// <summary>Delegate type for <code>PropertyChildChange</code> event</summary>
  public delegate void PropertyChildChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, JavaObjectHandle oldChild, JavaObjectHandle newChild);

  /// <summary>Delegate type for <code>PropertyDescriptionChange</code> event</summary>
  public delegate void PropertyDescriptionChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldDescription, string newDescription);

  /// <summary>Delegate type for <code>PropertyNameChange</code> event</summary>
  public delegate void PropertyNameChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldName, string newName);

  /// <summary>Delegate type for <code>PropertySelectionChange</code> event</summary>
  public delegate void PropertySelectionChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>PropertyStateChange</code> event</summary>
  public delegate void PropertyStateChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldState, string newState);

  /// <summary>Delegate type for <code>PropertyTableModelChange</code> event</summary>
  public delegate void PropertyTableModelChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle src, string oldValue, string newValue);

  /// <summary>Delegate type for <code>PropertyTextChange</code> event</summary>
  public delegate void PropertyTextChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  /// <summary>Delegate type for <code>PropertyValueChange</code> event</summary>
  public delegate void PropertyValueChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldValue, string newValue);

  /// <summary>Delegate type for <code>PropertyVisibleDataChange</code> event</summary>
  public delegate void PropertyVisibleDataChangeEventHandler(int vmid, JavaObjectHandle evt, JavaObjectHandle source);

  #endregion

  [Flags]
  public enum AccessibleInterfaces {
    cAccessibleValueInterface = 1,
    cAccessibleActionInterface = 2,
    cAccessibleComponentInterface = 4,
    cAccessibleSelectionInterface = 8,
    cAccessibleTableInterface = 16,
    cAccessibleTextInterface = 32,
    cAccessibleHypertextInterface = 64,
  }

  public enum AccessibleKeyCode : ushort {
    ACCESSIBLE_VK_BACK_SPACE = 8,
    ACCESSIBLE_VK_DELETE = 127,
    ACCESSIBLE_VK_DOWN = 40,
    ACCESSIBLE_VK_END = 35,
    ACCESSIBLE_VK_HOME = 36,
    ACCESSIBLE_VK_INSERT = 155,
    ACCESSIBLE_VK_KP_DOWN = 225,
    ACCESSIBLE_VK_KP_LEFT = 226,
    ACCESSIBLE_VK_KP_RIGHT = 227,
    ACCESSIBLE_VK_KP_UP = 224,
    ACCESSIBLE_VK_LEFT = 37,
    ACCESSIBLE_VK_PAGE_DOWN = 34,
    ACCESSIBLE_VK_PAGE_UP = 33,
    ACCESSIBLE_VK_RIGHT = 39,
    ACCESSIBLE_VK_UP = 38,
  }

  [Flags]
  public enum AccessibleModifiers {
    ACCESSIBLE_SHIFT_KEYSTROKE = 1,
    ACCESSIBLE_CONTROL_KEYSTROKE = 2,
    ACCESSIBLE_META_KEYSTROKE = 4,
    ACCESSIBLE_ALT_KEYSTROKE = 8,
    ACCESSIBLE_ALT_GRAPH_KEYSTROKE = 16,
    ACCESSIBLE_BUTTON1_KEYSTROKE = 32,
    ACCESSIBLE_BUTTON2_KEYSTROKE = 64,
    ACCESSIBLE_BUTTON3_KEYSTROKE = 128,
    ACCESSIBLE_FKEY_KEYSTROKE = 256,
    ACCESSIBLE_CONTROLCODE_KEYSTROKE = 512,
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessBridgeVersionInfo {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string VMversion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string bridgeJavaClassVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string bridgeJavaDLLVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string bridgeWinDLLVersion;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleActionInfo {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string name;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleActionsToDo {
    public int actionsCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public AccessibleActionInfo[] actions;
  }

  public struct AccessibleHyperlinkInfo {
    public string text;
    public int startIndex;
    public int endIndex;
    public JavaObjectHandle accessibleHyperlink;
  }

  public struct AccessibleHypertextInfo {
    public int linkCount;
    public AccessibleHyperlinkInfo[] links;
    public JavaObjectHandle accessibleHypertext;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleIconInfo {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string description;
    public int height;
    public int width;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleIcons {
    public int iconsCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public AccessibleIconInfo[] iconInfo;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleKeyBindingInfo {
    public AccessibleKeyCode character;
    public AccessibleModifiers modifiers;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleKeyBindings {
    public int keyBindingsCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public AccessibleKeyBindingInfo[] keyBindingInfo;
  }

  public struct AccessibleRelationInfo {
    public string key;
    public int targetCount;
    public JavaObjectHandle[] targets;
  }

  public struct AccessibleRelationSetInfo {
    public int relationCount;
    public AccessibleRelationInfo[] relations;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleTextInfo {
    public int charCount;
    public int caretIndex;
    public int indexAtPoint;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleTextItemsInfo {
    public char letter;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string word;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string sentence;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleTextRectInfo {
    public int x;
    public int y;
    public int width;
    public int height;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct AccessibleTextSelectionInfo {
    public int selectionStartIndex;
    public int selectionEndIndex;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string selectedText;
  }

  public struct VisibleChildrenInfo {
    public int returnedChildrenCount;
    public JavaObjectHandle[] children;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public class AccessibleActions {
    public int actionsCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public AccessibleActionInfo[] actionInfo;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public class AccessibleContextInfo {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string name;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string description;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string role;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string role_en_US;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string states;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string states_en_US;
    public int indexInParent;
    public int childrenCount;
    public int x;
    public int y;
    public int width;
    public int height;
    public int accessibleComponent;
    public int accessibleAction;
    public int accessibleSelection;
    public int accessibleText;
    public AccessibleInterfaces accessibleInterfaces;
  }

  public class AccessibleTableCellInfo {
    public JavaObjectHandle accessibleContext;
    public int index;
    public int row;
    public int column;
    public int rowExtent;
    public int columnExtent;
    public byte isSelected;
  }

  public class AccessibleTableInfo {
    public JavaObjectHandle caption;
    public JavaObjectHandle summary;
    public int rowCount;
    public int columnCount;
    public JavaObjectHandle accessibleContext;
    public JavaObjectHandle accessibleTable;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public class AccessibleTextAttributesInfo {
    public int bold;
    public int italic;
    public int underline;
    public int strikethrough;
    public int superscript;
    public int subscript;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string backgroundColor;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string foregroundColor;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string fontFamily;
    public int fontSize;
    public int alignment;
    public int bidiLevel;
    public float firstLineIndent;
    public float leftIndent;
    public float rightIndent;
    public float lineSpacing;
    public float spaceAbove;
    public float spaceBelow;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string fullAttributesString;
  }

}
