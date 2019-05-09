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
  /// Implementation of <see cref="AccessBridgeFunctions"/> using <code>WindowsAccessBridge DLL</code>
  /// entry points implemented in <see cref="AccessBridgeEntryPointsLegacy"/>
  /// </summary>
  internal partial class AccessBridgeNativeFunctionsLegacy : AccessBridgeFunctions {

    #region Function implementations

    public override bool ActivateAccessibleHyperlink(int vmid, JavaObjectHandle accessibleContext, JavaObjectHandle accessibleHyperlink) {
      var result = EntryPoints.ActivateAccessibleHyperlink(vmid, Unwrap(vmid, accessibleContext), Unwrap(vmid, accessibleHyperlink));
      GC.KeepAlive(accessibleContext);
      GC.KeepAlive(accessibleHyperlink);
      return ToBool(result);
    }

    public override void AddAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel, int i) {
      EntryPoints.AddAccessibleSelectionFromContext(vmid, Unwrap(vmid, asel), i);
      GC.KeepAlive(asel);
    }

    public override void ClearAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel) {
      EntryPoints.ClearAccessibleSelectionFromContext(vmid, Unwrap(vmid, asel));
      GC.KeepAlive(asel);
    }

    public override bool DoAccessibleActions(int vmid, JavaObjectHandle accessibleContext, ref AccessibleActionsToDo actionsToDo, out int failure) {
      var result = EntryPoints.DoAccessibleActions(vmid, Unwrap(vmid, accessibleContext), ref actionsToDo, out failure);
      GC.KeepAlive(accessibleContext);
      return Succeeded(result);
    }

    public override bool GetAccessibleActions(int vmid, JavaObjectHandle accessibleContext, out AccessibleActions actions) {
      actions = new AccessibleActions();
      var result = EntryPoints.GetAccessibleActions(vmid, Unwrap(vmid, accessibleContext), actions);
      GC.KeepAlive(accessibleContext);
      return Succeeded(result);
    }

    public override JavaObjectHandle GetAccessibleChildFromContext(int vmid, JavaObjectHandle ac, int i) {
      var result = EntryPoints.GetAccessibleChildFromContext(vmid, Unwrap(vmid, ac), i);
      GC.KeepAlive(ac);
      return Wrap(vmid, result);
    }

    public override bool GetAccessibleContextAt(int vmid, JavaObjectHandle acParent, int x, int y, out JavaObjectHandle ac) {
      JOBJECT32 acTemp;
      var result = EntryPoints.GetAccessibleContextAt(vmid, Unwrap(vmid, acParent), x, y, out acTemp);
      GC.KeepAlive(acParent);
      if (Succeeded(result)) {
        ac = Wrap(vmid, acTemp);
      } else {
        acTemp = default(JOBJECT32);
        ac = Wrap(vmid, acTemp);
      }
      return Succeeded(result);
    }

    public override bool GetAccessibleContextFromHWND(WindowHandle window, out int vmid, out JavaObjectHandle ac) {
      JOBJECT32 acTemp;
      var result = EntryPoints.GetAccessibleContextFromHWND(window, out vmid, out acTemp);
      if (Succeeded(result)) {
        ac = Wrap(vmid, acTemp);
      } else {
        acTemp = default(JOBJECT32);
        ac = Wrap(vmid, acTemp);
      }
      return Succeeded(result);
    }

    public override bool GetAccessibleContextInfo(int vmid, JavaObjectHandle ac, out AccessibleContextInfo info) {
      info = new AccessibleContextInfo();
      var result = EntryPoints.GetAccessibleContextInfo(vmid, Unwrap(vmid, ac), info);
      GC.KeepAlive(ac);
      return Succeeded(result);
    }

    public override bool GetAccessibleContextWithFocus(WindowHandle window, out int vmid, out JavaObjectHandle ac) {
      JOBJECT32 acTemp;
      var result = EntryPoints.GetAccessibleContextWithFocus(window, out vmid, out acTemp);
      if (Succeeded(result)) {
        ac = Wrap(vmid, acTemp);
      } else {
        acTemp = default(JOBJECT32);
        ac = Wrap(vmid, acTemp);
      }
      return Succeeded(result);
    }

    public override bool GetAccessibleHyperlink(int vmid, JavaObjectHandle hypertext, int nIndex, out AccessibleHyperlinkInfo hyperlinkInfo) {
      AccessibleHyperlinkInfoNativeLegacy hyperlinkInfoTemp;
      var result = EntryPoints.GetAccessibleHyperlink(vmid, Unwrap(vmid, hypertext), nIndex, out hyperlinkInfoTemp);
      GC.KeepAlive(hypertext);
      if (Succeeded(result))
        hyperlinkInfo = Wrap(vmid, hyperlinkInfoTemp);
      else
        hyperlinkInfo = default(AccessibleHyperlinkInfo);
      return Succeeded(result);
    }

    public override int GetAccessibleHyperlinkCount(int vmid, JavaObjectHandle accessibleContext) {
      var result = EntryPoints.GetAccessibleHyperlinkCount(vmid, Unwrap(vmid, accessibleContext));
      GC.KeepAlive(accessibleContext);
      return result;
    }

    public override bool GetAccessibleHypertext(int vmid, JavaObjectHandle accessibleContext, out AccessibleHypertextInfo hypertextInfo) {
      AccessibleHypertextInfoNativeLegacy hypertextInfoTemp;
      var result = EntryPoints.GetAccessibleHypertext(vmid, Unwrap(vmid, accessibleContext), out hypertextInfoTemp);
      GC.KeepAlive(accessibleContext);
      if (Succeeded(result))
        hypertextInfo = Wrap(vmid, hypertextInfoTemp);
      else
        hypertextInfo = default(AccessibleHypertextInfo);
      return Succeeded(result);
    }

    public override bool GetAccessibleHypertextExt(int vmid, JavaObjectHandle accessibleContext, int nStartIndex, out AccessibleHypertextInfo hypertextInfo) {
      AccessibleHypertextInfoNativeLegacy hypertextInfoTemp;
      var result = EntryPoints.GetAccessibleHypertextExt(vmid, Unwrap(vmid, accessibleContext), nStartIndex, out hypertextInfoTemp);
      GC.KeepAlive(accessibleContext);
      if (Succeeded(result))
        hypertextInfo = Wrap(vmid, hypertextInfoTemp);
      else
        hypertextInfo = default(AccessibleHypertextInfo);
      return Succeeded(result);
    }

    public override int GetAccessibleHypertextLinkIndex(int vmid, JavaObjectHandle hypertext, int nIndex) {
      var result = EntryPoints.GetAccessibleHypertextLinkIndex(vmid, Unwrap(vmid, hypertext), nIndex);
      GC.KeepAlive(hypertext);
      return result;
    }

    public override bool GetAccessibleIcons(int vmid, JavaObjectHandle accessibleContext, out AccessibleIcons icons) {
      var result = EntryPoints.GetAccessibleIcons(vmid, Unwrap(vmid, accessibleContext), out icons);
      GC.KeepAlive(accessibleContext);
      return Succeeded(result);
    }

    public override bool GetAccessibleKeyBindings(int vmid, JavaObjectHandle accessibleContext, out AccessibleKeyBindings keyBindings) {
      var result = EntryPoints.GetAccessibleKeyBindings(vmid, Unwrap(vmid, accessibleContext), out keyBindings);
      GC.KeepAlive(accessibleContext);
      return Succeeded(result);
    }

    public override JavaObjectHandle GetAccessibleParentFromContext(int vmid, JavaObjectHandle ac) {
      var result = EntryPoints.GetAccessibleParentFromContext(vmid, Unwrap(vmid, ac));
      GC.KeepAlive(ac);
      return Wrap(vmid, result);
    }

    public override bool GetAccessibleRelationSet(int vmid, JavaObjectHandle accessibleContext, out AccessibleRelationSetInfo relationSetInfo) {
      AccessibleRelationSetInfoNativeLegacy relationSetInfoTemp;
      var result = EntryPoints.GetAccessibleRelationSet(vmid, Unwrap(vmid, accessibleContext), out relationSetInfoTemp);
      GC.KeepAlive(accessibleContext);
      if (Succeeded(result))
        relationSetInfo = Wrap(vmid, relationSetInfoTemp);
      else
        relationSetInfo = default(AccessibleRelationSetInfo);
      return Succeeded(result);
    }

    public override int GetAccessibleSelectionCountFromContext(int vmid, JavaObjectHandle asel) {
      var result = EntryPoints.GetAccessibleSelectionCountFromContext(vmid, Unwrap(vmid, asel));
      GC.KeepAlive(asel);
      return result;
    }

    public override JavaObjectHandle GetAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel, int i) {
      var result = EntryPoints.GetAccessibleSelectionFromContext(vmid, Unwrap(vmid, asel), i);
      GC.KeepAlive(asel);
      return Wrap(vmid, result);
    }

    public override bool GetAccessibleTableCellInfo(int vmid, JavaObjectHandle at, int row, int column, out AccessibleTableCellInfo tableCellInfo) {
      AccessibleTableCellInfoNativeLegacy tableCellInfoTemp = new AccessibleTableCellInfoNativeLegacy();
      var result = EntryPoints.GetAccessibleTableCellInfo(vmid, Unwrap(vmid, at), row, column, tableCellInfoTemp);
      GC.KeepAlive(at);
      tableCellInfo = new AccessibleTableCellInfo();
      if (Succeeded(result))
        CopyWrap(vmid, tableCellInfoTemp, tableCellInfo);
      return Succeeded(result);
    }

    public override int GetAccessibleTableColumn(int vmid, JavaObjectHandle table, int index) {
      var result = EntryPoints.GetAccessibleTableColumn(vmid, Unwrap(vmid, table), index);
      GC.KeepAlive(table);
      return result;
    }

    public override JavaObjectHandle GetAccessibleTableColumnDescription(int vmid, JavaObjectHandle acParent, int column) {
      var result = EntryPoints.GetAccessibleTableColumnDescription(vmid, Unwrap(vmid, acParent), column);
      GC.KeepAlive(acParent);
      return Wrap(vmid, result);
    }

    public override bool GetAccessibleTableColumnHeader(int vmid, JavaObjectHandle acParent, out AccessibleTableInfo tableInfo) {
      AccessibleTableInfoNativeLegacy tableInfoTemp = new AccessibleTableInfoNativeLegacy();
      var result = EntryPoints.GetAccessibleTableColumnHeader(vmid, Unwrap(vmid, acParent), tableInfoTemp);
      GC.KeepAlive(acParent);
      tableInfo = new AccessibleTableInfo();
      if (Succeeded(result))
        CopyWrap(vmid, tableInfoTemp, tableInfo);
      return Succeeded(result);
    }

    public override int GetAccessibleTableColumnSelectionCount(int vmid, JavaObjectHandle table) {
      var result = EntryPoints.GetAccessibleTableColumnSelectionCount(vmid, Unwrap(vmid, table));
      GC.KeepAlive(table);
      return result;
    }

    public override bool GetAccessibleTableColumnSelections(int vmid, JavaObjectHandle table, int count, [Out]int[] selections) {
      var result = EntryPoints.GetAccessibleTableColumnSelections(vmid, Unwrap(vmid, table), count, selections);
      GC.KeepAlive(table);
      return Succeeded(result);
    }

    public override int GetAccessibleTableIndex(int vmid, JavaObjectHandle table, int row, int column) {
      var result = EntryPoints.GetAccessibleTableIndex(vmid, Unwrap(vmid, table), row, column);
      GC.KeepAlive(table);
      return result;
    }

    public override bool GetAccessibleTableInfo(int vmid, JavaObjectHandle ac, out AccessibleTableInfo tableInfo) {
      AccessibleTableInfoNativeLegacy tableInfoTemp = new AccessibleTableInfoNativeLegacy();
      var result = EntryPoints.GetAccessibleTableInfo(vmid, Unwrap(vmid, ac), tableInfoTemp);
      GC.KeepAlive(ac);
      tableInfo = new AccessibleTableInfo();
      if (Succeeded(result))
        CopyWrap(vmid, tableInfoTemp, tableInfo);
      return Succeeded(result);
    }

    public override int GetAccessibleTableRow(int vmid, JavaObjectHandle table, int index) {
      var result = EntryPoints.GetAccessibleTableRow(vmid, Unwrap(vmid, table), index);
      GC.KeepAlive(table);
      return result;
    }

    public override JavaObjectHandle GetAccessibleTableRowDescription(int vmid, JavaObjectHandle acParent, int row) {
      var result = EntryPoints.GetAccessibleTableRowDescription(vmid, Unwrap(vmid, acParent), row);
      GC.KeepAlive(acParent);
      return Wrap(vmid, result);
    }

    public override bool GetAccessibleTableRowHeader(int vmid, JavaObjectHandle acParent, out AccessibleTableInfo tableInfo) {
      AccessibleTableInfoNativeLegacy tableInfoTemp = new AccessibleTableInfoNativeLegacy();
      var result = EntryPoints.GetAccessibleTableRowHeader(vmid, Unwrap(vmid, acParent), tableInfoTemp);
      GC.KeepAlive(acParent);
      tableInfo = new AccessibleTableInfo();
      if (Succeeded(result))
        CopyWrap(vmid, tableInfoTemp, tableInfo);
      return Succeeded(result);
    }

    public override int GetAccessibleTableRowSelectionCount(int vmid, JavaObjectHandle table) {
      var result = EntryPoints.GetAccessibleTableRowSelectionCount(vmid, Unwrap(vmid, table));
      GC.KeepAlive(table);
      return result;
    }

    public override bool GetAccessibleTableRowSelections(int vmid, JavaObjectHandle table, int count, [Out]int[] selections) {
      var result = EntryPoints.GetAccessibleTableRowSelections(vmid, Unwrap(vmid, table), count, selections);
      GC.KeepAlive(table);
      return Succeeded(result);
    }

    public override bool GetAccessibleTextAttributes(int vmid, JavaObjectHandle at, int index, out AccessibleTextAttributesInfo attributes) {
      attributes = new AccessibleTextAttributesInfo();
      var result = EntryPoints.GetAccessibleTextAttributes(vmid, Unwrap(vmid, at), index, attributes);
      GC.KeepAlive(at);
      return Succeeded(result);
    }

    public override bool GetAccessibleTextInfo(int vmid, JavaObjectHandle at, out AccessibleTextInfo textInfo, int x, int y) {
      var result = EntryPoints.GetAccessibleTextInfo(vmid, Unwrap(vmid, at), out textInfo, x, y);
      GC.KeepAlive(at);
      return Succeeded(result);
    }

    public override bool GetAccessibleTextItems(int vmid, JavaObjectHandle at, out AccessibleTextItemsInfo textItems, int index) {
      var result = EntryPoints.GetAccessibleTextItems(vmid, Unwrap(vmid, at), out textItems, index);
      GC.KeepAlive(at);
      return Succeeded(result);
    }

    public override bool GetAccessibleTextLineBounds(int vmid, JavaObjectHandle at, int index, out int startIndex, out int endIndex) {
      var result = EntryPoints.GetAccessibleTextLineBounds(vmid, Unwrap(vmid, at), index, out startIndex, out endIndex);
      GC.KeepAlive(at);
      return Succeeded(result);
    }

    public override bool GetAccessibleTextRange(int vmid, JavaObjectHandle at, int start, int end, [Out]char[] text, short len) {
      var result = EntryPoints.GetAccessibleTextRange(vmid, Unwrap(vmid, at), start, end, text, len);
      GC.KeepAlive(at);
      return Succeeded(result);
    }

    public override bool GetAccessibleTextRect(int vmid, JavaObjectHandle at, out AccessibleTextRectInfo rectInfo, int index) {
      var result = EntryPoints.GetAccessibleTextRect(vmid, Unwrap(vmid, at), out rectInfo, index);
      GC.KeepAlive(at);
      return Succeeded(result);
    }

    public override bool GetAccessibleTextSelectionInfo(int vmid, JavaObjectHandle at, out AccessibleTextSelectionInfo textSelection) {
      var result = EntryPoints.GetAccessibleTextSelectionInfo(vmid, Unwrap(vmid, at), out textSelection);
      GC.KeepAlive(at);
      return Succeeded(result);
    }

    public override JavaObjectHandle GetActiveDescendent(int vmid, JavaObjectHandle ac) {
      var result = EntryPoints.GetActiveDescendent(vmid, Unwrap(vmid, ac));
      GC.KeepAlive(ac);
      return Wrap(vmid, result);
    }

    public override bool GetCaretLocation(int vmid, JavaObjectHandle ac, out AccessibleTextRectInfo rectInfo, int index) {
      var result = EntryPoints.GetCaretLocation(vmid, Unwrap(vmid, ac), out rectInfo, index);
      GC.KeepAlive(ac);
      return Succeeded(result);
    }

    public override bool GetCurrentAccessibleValueFromContext(int vmid, JavaObjectHandle av, StringBuilder value, short len) {
      var result = EntryPoints.GetCurrentAccessibleValueFromContext(vmid, Unwrap(vmid, av), value, len);
      GC.KeepAlive(av);
      return Succeeded(result);
    }

    public override WindowHandle GetHWNDFromAccessibleContext(int vmid, JavaObjectHandle ac) {
      var result = EntryPoints.GetHWNDFromAccessibleContext(vmid, Unwrap(vmid, ac));
      GC.KeepAlive(ac);
      return result;
    }

    public override bool GetMaximumAccessibleValueFromContext(int vmid, JavaObjectHandle av, StringBuilder value, short len) {
      var result = EntryPoints.GetMaximumAccessibleValueFromContext(vmid, Unwrap(vmid, av), value, len);
      GC.KeepAlive(av);
      return Succeeded(result);
    }

    public override bool GetMinimumAccessibleValueFromContext(int vmid, JavaObjectHandle av, StringBuilder value, short len) {
      var result = EntryPoints.GetMinimumAccessibleValueFromContext(vmid, Unwrap(vmid, av), value, len);
      GC.KeepAlive(av);
      return Succeeded(result);
    }

    public override int GetObjectDepth(int vmid, JavaObjectHandle ac) {
      var result = EntryPoints.GetObjectDepth(vmid, Unwrap(vmid, ac));
      GC.KeepAlive(ac);
      return result;
    }

    public override JavaObjectHandle GetParentWithRole(int vmid, JavaObjectHandle ac, string role) {
      var result = EntryPoints.GetParentWithRole(vmid, Unwrap(vmid, ac), role);
      GC.KeepAlive(ac);
      return Wrap(vmid, result);
    }

    public override JavaObjectHandle GetParentWithRoleElseRoot(int vmid, JavaObjectHandle ac, string role) {
      var result = EntryPoints.GetParentWithRoleElseRoot(vmid, Unwrap(vmid, ac), role);
      GC.KeepAlive(ac);
      return Wrap(vmid, result);
    }

    public override bool GetTextAttributesInRange(int vmid, JavaObjectHandle accessibleContext, int startIndex, int endIndex, out AccessibleTextAttributesInfo attributes, out short len) {
      attributes = new AccessibleTextAttributesInfo();
      var result = EntryPoints.GetTextAttributesInRange(vmid, Unwrap(vmid, accessibleContext), startIndex, endIndex, attributes, out len);
      GC.KeepAlive(accessibleContext);
      return Succeeded(result);
    }

    public override JavaObjectHandle GetTopLevelObject(int vmid, JavaObjectHandle ac) {
      var result = EntryPoints.GetTopLevelObject(vmid, Unwrap(vmid, ac));
      GC.KeepAlive(ac);
      return Wrap(vmid, result);
    }

    public override bool GetVersionInfo(int vmid, out AccessBridgeVersionInfo info) {
      var result = EntryPoints.GetVersionInfo(vmid, out info);
      return Succeeded(result);
    }

    public override bool GetVirtualAccessibleName(int vmid, JavaObjectHandle ac, StringBuilder name, int len) {
      var result = EntryPoints.GetVirtualAccessibleName(vmid, Unwrap(vmid, ac), name, len);
      GC.KeepAlive(ac);
      return Succeeded(result);
    }

    public override bool GetVisibleChildren(int vmid, JavaObjectHandle accessibleContext, int startIndex, out VisibleChildrenInfo children) {
      VisibleChildrenInfoNativeLegacy childrenTemp;
      var result = EntryPoints.GetVisibleChildren(vmid, Unwrap(vmid, accessibleContext), startIndex, out childrenTemp);
      GC.KeepAlive(accessibleContext);
      if (Succeeded(result))
        children = Wrap(vmid, childrenTemp);
      else
        children = default(VisibleChildrenInfo);
      return Succeeded(result);
    }

    public override int GetVisibleChildrenCount(int vmid, JavaObjectHandle accessibleContext) {
      var result = EntryPoints.GetVisibleChildrenCount(vmid, Unwrap(vmid, accessibleContext));
      GC.KeepAlive(accessibleContext);
      return result;
    }

    public override bool IsAccessibleChildSelectedFromContext(int vmid, JavaObjectHandle asel, int i) {
      var result = EntryPoints.IsAccessibleChildSelectedFromContext(vmid, Unwrap(vmid, asel), i);
      GC.KeepAlive(asel);
      return ToBool(result);
    }

    public override bool IsAccessibleTableColumnSelected(int vmid, JavaObjectHandle table, int column) {
      var result = EntryPoints.IsAccessibleTableColumnSelected(vmid, Unwrap(vmid, table), column);
      GC.KeepAlive(table);
      return ToBool(result);
    }

    public override bool IsAccessibleTableRowSelected(int vmid, JavaObjectHandle table, int row) {
      var result = EntryPoints.IsAccessibleTableRowSelected(vmid, Unwrap(vmid, table), row);
      GC.KeepAlive(table);
      return ToBool(result);
    }

    public override bool IsJavaWindow(WindowHandle window) {
      var result = EntryPoints.IsJavaWindow(window);
      return ToBool(result);
    }

    public override bool IsSameObject(int vmid, JavaObjectHandle obj1, JavaObjectHandle obj2) {
      var result = EntryPoints.IsSameObject(vmid, Unwrap(vmid, obj1), Unwrap(vmid, obj2));
      GC.KeepAlive(obj1);
      GC.KeepAlive(obj2);
      return ToBool(result);
    }

    public override void RemoveAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel, int i) {
      EntryPoints.RemoveAccessibleSelectionFromContext(vmid, Unwrap(vmid, asel), i);
      GC.KeepAlive(asel);
    }

    public override void SelectAllAccessibleSelectionFromContext(int vmid, JavaObjectHandle asel) {
      EntryPoints.SelectAllAccessibleSelectionFromContext(vmid, Unwrap(vmid, asel));
      GC.KeepAlive(asel);
    }

    public override bool SetTextContents(int vmid, JavaObjectHandle ac, string text) {
      var result = EntryPoints.SetTextContents(vmid, Unwrap(vmid, ac), text);
      GC.KeepAlive(ac);
      return Succeeded(result);
    }

    public override void Windows_run() {
      EntryPoints.Windows_run();
    }

    #endregion

    #region Wrap/Unwrap structs

    private AccessibleHyperlinkInfo Wrap(int vmid, AccessibleHyperlinkInfoNativeLegacy info) {
      var result = new AccessibleHyperlinkInfo();
      result.text = info.text;
      result.startIndex = info.startIndex;
      result.endIndex = info.endIndex;
      result.accessibleHyperlink = Wrap(vmid, info.accessibleHyperlink);
      return result;
    }

    private AccessibleHyperlinkInfoNativeLegacy Unwrap(int vmid, AccessibleHyperlinkInfo info) {
      var result = new AccessibleHyperlinkInfoNativeLegacy();
      result.text = info.text;
      result.startIndex = info.startIndex;
      result.endIndex = info.endIndex;
      result.accessibleHyperlink = Unwrap(vmid, info.accessibleHyperlink);
      return result;
    }

    private AccessibleHypertextInfo Wrap(int vmid, AccessibleHypertextInfoNativeLegacy info) {
      var result = new AccessibleHypertextInfo();
      result.linkCount = info.linkCount;
      if (info.links != null) {
        var count = info.linkCount;
        result.links = new AccessibleHyperlinkInfo[count];
        for(var i = 0; i < count; i++) {
          result.links[i] = Wrap(vmid, info.links[i]);
        }
      }
      result.accessibleHypertext = Wrap(vmid, info.accessibleHypertext);
      return result;
    }

    private AccessibleHypertextInfoNativeLegacy Unwrap(int vmid, AccessibleHypertextInfo info) {
      var result = new AccessibleHypertextInfoNativeLegacy();
      result.linkCount = info.linkCount;
      if (info.links != null) {
        var count = info.linkCount;
        result.links = new AccessibleHyperlinkInfoNativeLegacy[count];
        for(var i = 0; i < count; i++) {
          result.links[i] = Unwrap(vmid, info.links[i]);
        }
      }
      result.accessibleHypertext = Unwrap(vmid, info.accessibleHypertext);
      return result;
    }

    private AccessibleRelationInfo Wrap(int vmid, AccessibleRelationInfoNativeLegacy info) {
      var result = new AccessibleRelationInfo();
      result.key = info.key;
      result.targetCount = info.targetCount;
      if (info.targets != null) {
        var count = info.targetCount;
        result.targets = new JavaObjectHandle[count];
        for(var i = 0; i < count; i++) {
          result.targets[i] = Wrap(vmid, info.targets[i]);
        }
      }
      return result;
    }

    private AccessibleRelationInfoNativeLegacy Unwrap(int vmid, AccessibleRelationInfo info) {
      var result = new AccessibleRelationInfoNativeLegacy();
      result.key = info.key;
      result.targetCount = info.targetCount;
      if (info.targets != null) {
        var count = info.targetCount;
        result.targets = new JOBJECT32[count];
        for(var i = 0; i < count; i++) {
          result.targets[i] = Unwrap(vmid, info.targets[i]);
        }
      }
      return result;
    }

    private AccessibleRelationSetInfo Wrap(int vmid, AccessibleRelationSetInfoNativeLegacy info) {
      var result = new AccessibleRelationSetInfo();
      result.relationCount = info.relationCount;
      if (info.relations != null) {
        var count = info.relationCount;
        result.relations = new AccessibleRelationInfo[count];
        for(var i = 0; i < count; i++) {
          result.relations[i] = Wrap(vmid, info.relations[i]);
        }
      }
      return result;
    }

    private AccessibleRelationSetInfoNativeLegacy Unwrap(int vmid, AccessibleRelationSetInfo info) {
      var result = new AccessibleRelationSetInfoNativeLegacy();
      result.relationCount = info.relationCount;
      if (info.relations != null) {
        var count = info.relationCount;
        result.relations = new AccessibleRelationInfoNativeLegacy[count];
        for(var i = 0; i < count; i++) {
          result.relations[i] = Unwrap(vmid, info.relations[i]);
        }
      }
      return result;
    }

    private VisibleChildrenInfo Wrap(int vmid, VisibleChildrenInfoNativeLegacy info) {
      var result = new VisibleChildrenInfo();
      result.returnedChildrenCount = info.returnedChildrenCount;
      if (info.children != null) {
        var count = info.returnedChildrenCount;
        result.children = new JavaObjectHandle[count];
        for(var i = 0; i < count; i++) {
          result.children[i] = Wrap(vmid, info.children[i]);
        }
      }
      return result;
    }

    private VisibleChildrenInfoNativeLegacy Unwrap(int vmid, VisibleChildrenInfo info) {
      var result = new VisibleChildrenInfoNativeLegacy();
      result.returnedChildrenCount = info.returnedChildrenCount;
      if (info.children != null) {
        var count = info.returnedChildrenCount;
        result.children = new JOBJECT32[count];
        for(var i = 0; i < count; i++) {
          result.children[i] = Unwrap(vmid, info.children[i]);
        }
      }
      return result;
    }

    #endregion

    #region CopyWrap/CopyUnwrap classes

    private void CopyWrap(int vmid, AccessibleTableCellInfoNativeLegacy infoSrc, AccessibleTableCellInfo infoDest) {
      infoDest.accessibleContext = Wrap(vmid, infoSrc.accessibleContext);
      infoDest.index = infoSrc.index;
      infoDest.row = infoSrc.row;
      infoDest.column = infoSrc.column;
      infoDest.rowExtent = infoSrc.rowExtent;
      infoDest.columnExtent = infoSrc.columnExtent;
      infoDest.isSelected = infoSrc.isSelected;
    }

    private void CopyUnwrap(int vmid, AccessibleTableCellInfo infoSrc, AccessibleTableCellInfoNativeLegacy infoDest) {
      infoDest.accessibleContext = Unwrap(vmid, infoSrc.accessibleContext);
      infoDest.index = infoSrc.index;
      infoDest.row = infoSrc.row;
      infoDest.column = infoSrc.column;
      infoDest.rowExtent = infoSrc.rowExtent;
      infoDest.columnExtent = infoSrc.columnExtent;
      infoDest.isSelected = infoSrc.isSelected;
    }

    private void CopyWrap(int vmid, AccessibleTableInfoNativeLegacy infoSrc, AccessibleTableInfo infoDest) {
      infoDest.caption = Wrap(vmid, infoSrc.caption);
      infoDest.summary = Wrap(vmid, infoSrc.summary);
      infoDest.rowCount = infoSrc.rowCount;
      infoDest.columnCount = infoSrc.columnCount;
      infoDest.accessibleContext = Wrap(vmid, infoSrc.accessibleContext);
      infoDest.accessibleTable = Wrap(vmid, infoSrc.accessibleTable);
    }

    private void CopyUnwrap(int vmid, AccessibleTableInfo infoSrc, AccessibleTableInfoNativeLegacy infoDest) {
      infoDest.caption = Unwrap(vmid, infoSrc.caption);
      infoDest.summary = Unwrap(vmid, infoSrc.summary);
      infoDest.rowCount = infoSrc.rowCount;
      infoDest.columnCount = infoSrc.columnCount;
      infoDest.accessibleContext = Unwrap(vmid, infoSrc.accessibleContext);
      infoDest.accessibleTable = Unwrap(vmid, infoSrc.accessibleTable);
    }

    #endregion

  }

  /// <summary>
  /// Implementation of <see cref="AccessBridgeEvents"/> over Legacy WindowsAccessBridge entry points
  /// </summary>
  internal partial class AccessBridgeNativeEventsLegacy : AccessBridgeEvents {
    #region Event fields
    private CaretUpdateEventHandler _caretUpdate;
    private FocusGainedEventHandler _focusGained;
    private FocusLostEventHandler _focusLost;
    private JavaShutdownEventHandler _javaShutdown;
    private MenuCanceledEventHandler _menuCanceled;
    private MenuDeselectedEventHandler _menuDeselected;
    private MenuSelectedEventHandler _menuSelected;
    private MouseClickedEventHandler _mouseClicked;
    private MouseEnteredEventHandler _mouseEntered;
    private MouseExitedEventHandler _mouseExited;
    private MousePressedEventHandler _mousePressed;
    private MouseReleasedEventHandler _mouseReleased;
    private PopupMenuCanceledEventHandler _popupMenuCanceled;
    private PopupMenuWillBecomeInvisibleEventHandler _popupMenuWillBecomeInvisible;
    private PopupMenuWillBecomeVisibleEventHandler _popupMenuWillBecomeVisible;
    private PropertyActiveDescendentChangeEventHandler _propertyActiveDescendentChange;
    private PropertyCaretChangeEventHandler _propertyCaretChange;
    private PropertyChangeEventHandler _propertyChange;
    private PropertyChildChangeEventHandler _propertyChildChange;
    private PropertyDescriptionChangeEventHandler _propertyDescriptionChange;
    private PropertyNameChangeEventHandler _propertyNameChange;
    private PropertySelectionChangeEventHandler _propertySelectionChange;
    private PropertyStateChangeEventHandler _propertyStateChange;
    private PropertyTableModelChangeEventHandler _propertyTableModelChange;
    private PropertyTextChangeEventHandler _propertyTextChange;
    private PropertyValueChangeEventHandler _propertyValueChange;
    private PropertyVisibleDataChangeEventHandler _propertyVisibleDataChange;
    #endregion

    #region Native callback keep-alive fields
    private AccessBridgeEntryPointsLegacy.CaretUpdateEventHandler _forwardCaretUpdateKeepAlive;
    private AccessBridgeEntryPointsLegacy.FocusGainedEventHandler _forwardFocusGainedKeepAlive;
    private AccessBridgeEntryPointsLegacy.FocusLostEventHandler _forwardFocusLostKeepAlive;
    private AccessBridgeEntryPointsLegacy.JavaShutdownEventHandler _forwardJavaShutdownKeepAlive;
    private AccessBridgeEntryPointsLegacy.MenuCanceledEventHandler _forwardMenuCanceledKeepAlive;
    private AccessBridgeEntryPointsLegacy.MenuDeselectedEventHandler _forwardMenuDeselectedKeepAlive;
    private AccessBridgeEntryPointsLegacy.MenuSelectedEventHandler _forwardMenuSelectedKeepAlive;
    private AccessBridgeEntryPointsLegacy.MouseClickedEventHandler _forwardMouseClickedKeepAlive;
    private AccessBridgeEntryPointsLegacy.MouseEnteredEventHandler _forwardMouseEnteredKeepAlive;
    private AccessBridgeEntryPointsLegacy.MouseExitedEventHandler _forwardMouseExitedKeepAlive;
    private AccessBridgeEntryPointsLegacy.MousePressedEventHandler _forwardMousePressedKeepAlive;
    private AccessBridgeEntryPointsLegacy.MouseReleasedEventHandler _forwardMouseReleasedKeepAlive;
    private AccessBridgeEntryPointsLegacy.PopupMenuCanceledEventHandler _forwardPopupMenuCanceledKeepAlive;
    private AccessBridgeEntryPointsLegacy.PopupMenuWillBecomeInvisibleEventHandler _forwardPopupMenuWillBecomeInvisibleKeepAlive;
    private AccessBridgeEntryPointsLegacy.PopupMenuWillBecomeVisibleEventHandler _forwardPopupMenuWillBecomeVisibleKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyActiveDescendentChangeEventHandler _forwardPropertyActiveDescendentChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyCaretChangeEventHandler _forwardPropertyCaretChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyChangeEventHandler _forwardPropertyChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyChildChangeEventHandler _forwardPropertyChildChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyDescriptionChangeEventHandler _forwardPropertyDescriptionChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyNameChangeEventHandler _forwardPropertyNameChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertySelectionChangeEventHandler _forwardPropertySelectionChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyStateChangeEventHandler _forwardPropertyStateChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyTableModelChangeEventHandler _forwardPropertyTableModelChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyTextChangeEventHandler _forwardPropertyTextChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyValueChangeEventHandler _forwardPropertyValueChangeKeepAlive;
    private AccessBridgeEntryPointsLegacy.PropertyVisibleDataChangeEventHandler _forwardPropertyVisibleDataChangeKeepAlive;
    #endregion

    #region Event properties
    public override event CaretUpdateEventHandler CaretUpdate {
      add {
        if (_caretUpdate == null) {
          _forwardCaretUpdateKeepAlive = ForwardCaretUpdate;
          EntryPoints.SetCaretUpdate(_forwardCaretUpdateKeepAlive);
        }
        _caretUpdate += value;
      }
      remove{
        _caretUpdate -= value;
        if (_caretUpdate == null) {
          EntryPoints.SetCaretUpdate(null);
          _forwardCaretUpdateKeepAlive = null;
        }
      }
    }
    public override event FocusGainedEventHandler FocusGained {
      add {
        if (_focusGained == null) {
          _forwardFocusGainedKeepAlive = ForwardFocusGained;
          EntryPoints.SetFocusGained(_forwardFocusGainedKeepAlive);
        }
        _focusGained += value;
      }
      remove{
        _focusGained -= value;
        if (_focusGained == null) {
          EntryPoints.SetFocusGained(null);
          _forwardFocusGainedKeepAlive = null;
        }
      }
    }
    public override event FocusLostEventHandler FocusLost {
      add {
        if (_focusLost == null) {
          _forwardFocusLostKeepAlive = ForwardFocusLost;
          EntryPoints.SetFocusLost(_forwardFocusLostKeepAlive);
        }
        _focusLost += value;
      }
      remove{
        _focusLost -= value;
        if (_focusLost == null) {
          EntryPoints.SetFocusLost(null);
          _forwardFocusLostKeepAlive = null;
        }
      }
    }
    public override event JavaShutdownEventHandler JavaShutdown {
      add {
        if (_javaShutdown == null) {
          _forwardJavaShutdownKeepAlive = ForwardJavaShutdown;
          EntryPoints.SetJavaShutdown(_forwardJavaShutdownKeepAlive);
        }
        _javaShutdown += value;
      }
      remove{
        _javaShutdown -= value;
        if (_javaShutdown == null) {
          EntryPoints.SetJavaShutdown(null);
          _forwardJavaShutdownKeepAlive = null;
        }
      }
    }
    public override event MenuCanceledEventHandler MenuCanceled {
      add {
        if (_menuCanceled == null) {
          _forwardMenuCanceledKeepAlive = ForwardMenuCanceled;
          EntryPoints.SetMenuCanceled(_forwardMenuCanceledKeepAlive);
        }
        _menuCanceled += value;
      }
      remove{
        _menuCanceled -= value;
        if (_menuCanceled == null) {
          EntryPoints.SetMenuCanceled(null);
          _forwardMenuCanceledKeepAlive = null;
        }
      }
    }
    public override event MenuDeselectedEventHandler MenuDeselected {
      add {
        if (_menuDeselected == null) {
          _forwardMenuDeselectedKeepAlive = ForwardMenuDeselected;
          EntryPoints.SetMenuDeselected(_forwardMenuDeselectedKeepAlive);
        }
        _menuDeselected += value;
      }
      remove{
        _menuDeselected -= value;
        if (_menuDeselected == null) {
          EntryPoints.SetMenuDeselected(null);
          _forwardMenuDeselectedKeepAlive = null;
        }
      }
    }
    public override event MenuSelectedEventHandler MenuSelected {
      add {
        if (_menuSelected == null) {
          _forwardMenuSelectedKeepAlive = ForwardMenuSelected;
          EntryPoints.SetMenuSelected(_forwardMenuSelectedKeepAlive);
        }
        _menuSelected += value;
      }
      remove{
        _menuSelected -= value;
        if (_menuSelected == null) {
          EntryPoints.SetMenuSelected(null);
          _forwardMenuSelectedKeepAlive = null;
        }
      }
    }
    public override event MouseClickedEventHandler MouseClicked {
      add {
        if (_mouseClicked == null) {
          _forwardMouseClickedKeepAlive = ForwardMouseClicked;
          EntryPoints.SetMouseClicked(_forwardMouseClickedKeepAlive);
        }
        _mouseClicked += value;
      }
      remove{
        _mouseClicked -= value;
        if (_mouseClicked == null) {
          EntryPoints.SetMouseClicked(null);
          _forwardMouseClickedKeepAlive = null;
        }
      }
    }
    public override event MouseEnteredEventHandler MouseEntered {
      add {
        if (_mouseEntered == null) {
          _forwardMouseEnteredKeepAlive = ForwardMouseEntered;
          EntryPoints.SetMouseEntered(_forwardMouseEnteredKeepAlive);
        }
        _mouseEntered += value;
      }
      remove{
        _mouseEntered -= value;
        if (_mouseEntered == null) {
          EntryPoints.SetMouseEntered(null);
          _forwardMouseEnteredKeepAlive = null;
        }
      }
    }
    public override event MouseExitedEventHandler MouseExited {
      add {
        if (_mouseExited == null) {
          _forwardMouseExitedKeepAlive = ForwardMouseExited;
          EntryPoints.SetMouseExited(_forwardMouseExitedKeepAlive);
        }
        _mouseExited += value;
      }
      remove{
        _mouseExited -= value;
        if (_mouseExited == null) {
          EntryPoints.SetMouseExited(null);
          _forwardMouseExitedKeepAlive = null;
        }
      }
    }
    public override event MousePressedEventHandler MousePressed {
      add {
        if (_mousePressed == null) {
          _forwardMousePressedKeepAlive = ForwardMousePressed;
          EntryPoints.SetMousePressed(_forwardMousePressedKeepAlive);
        }
        _mousePressed += value;
      }
      remove{
        _mousePressed -= value;
        if (_mousePressed == null) {
          EntryPoints.SetMousePressed(null);
          _forwardMousePressedKeepAlive = null;
        }
      }
    }
    public override event MouseReleasedEventHandler MouseReleased {
      add {
        if (_mouseReleased == null) {
          _forwardMouseReleasedKeepAlive = ForwardMouseReleased;
          EntryPoints.SetMouseReleased(_forwardMouseReleasedKeepAlive);
        }
        _mouseReleased += value;
      }
      remove{
        _mouseReleased -= value;
        if (_mouseReleased == null) {
          EntryPoints.SetMouseReleased(null);
          _forwardMouseReleasedKeepAlive = null;
        }
      }
    }
    public override event PopupMenuCanceledEventHandler PopupMenuCanceled {
      add {
        if (_popupMenuCanceled == null) {
          _forwardPopupMenuCanceledKeepAlive = ForwardPopupMenuCanceled;
          EntryPoints.SetPopupMenuCanceled(_forwardPopupMenuCanceledKeepAlive);
        }
        _popupMenuCanceled += value;
      }
      remove{
        _popupMenuCanceled -= value;
        if (_popupMenuCanceled == null) {
          EntryPoints.SetPopupMenuCanceled(null);
          _forwardPopupMenuCanceledKeepAlive = null;
        }
      }
    }
    public override event PopupMenuWillBecomeInvisibleEventHandler PopupMenuWillBecomeInvisible {
      add {
        if (_popupMenuWillBecomeInvisible == null) {
          _forwardPopupMenuWillBecomeInvisibleKeepAlive = ForwardPopupMenuWillBecomeInvisible;
          EntryPoints.SetPopupMenuWillBecomeInvisible(_forwardPopupMenuWillBecomeInvisibleKeepAlive);
        }
        _popupMenuWillBecomeInvisible += value;
      }
      remove{
        _popupMenuWillBecomeInvisible -= value;
        if (_popupMenuWillBecomeInvisible == null) {
          EntryPoints.SetPopupMenuWillBecomeInvisible(null);
          _forwardPopupMenuWillBecomeInvisibleKeepAlive = null;
        }
      }
    }
    public override event PopupMenuWillBecomeVisibleEventHandler PopupMenuWillBecomeVisible {
      add {
        if (_popupMenuWillBecomeVisible == null) {
          _forwardPopupMenuWillBecomeVisibleKeepAlive = ForwardPopupMenuWillBecomeVisible;
          EntryPoints.SetPopupMenuWillBecomeVisible(_forwardPopupMenuWillBecomeVisibleKeepAlive);
        }
        _popupMenuWillBecomeVisible += value;
      }
      remove{
        _popupMenuWillBecomeVisible -= value;
        if (_popupMenuWillBecomeVisible == null) {
          EntryPoints.SetPopupMenuWillBecomeVisible(null);
          _forwardPopupMenuWillBecomeVisibleKeepAlive = null;
        }
      }
    }
    public override event PropertyActiveDescendentChangeEventHandler PropertyActiveDescendentChange {
      add {
        if (_propertyActiveDescendentChange == null) {
          _forwardPropertyActiveDescendentChangeKeepAlive = ForwardPropertyActiveDescendentChange;
          EntryPoints.SetPropertyActiveDescendentChange(_forwardPropertyActiveDescendentChangeKeepAlive);
        }
        _propertyActiveDescendentChange += value;
      }
      remove{
        _propertyActiveDescendentChange -= value;
        if (_propertyActiveDescendentChange == null) {
          EntryPoints.SetPropertyActiveDescendentChange(null);
          _forwardPropertyActiveDescendentChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyCaretChangeEventHandler PropertyCaretChange {
      add {
        if (_propertyCaretChange == null) {
          _forwardPropertyCaretChangeKeepAlive = ForwardPropertyCaretChange;
          EntryPoints.SetPropertyCaretChange(_forwardPropertyCaretChangeKeepAlive);
        }
        _propertyCaretChange += value;
      }
      remove{
        _propertyCaretChange -= value;
        if (_propertyCaretChange == null) {
          EntryPoints.SetPropertyCaretChange(null);
          _forwardPropertyCaretChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyChangeEventHandler PropertyChange {
      add {
        if (_propertyChange == null) {
          _forwardPropertyChangeKeepAlive = ForwardPropertyChange;
          EntryPoints.SetPropertyChange(_forwardPropertyChangeKeepAlive);
        }
        _propertyChange += value;
      }
      remove{
        _propertyChange -= value;
        if (_propertyChange == null) {
          EntryPoints.SetPropertyChange(null);
          _forwardPropertyChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyChildChangeEventHandler PropertyChildChange {
      add {
        if (_propertyChildChange == null) {
          _forwardPropertyChildChangeKeepAlive = ForwardPropertyChildChange;
          EntryPoints.SetPropertyChildChange(_forwardPropertyChildChangeKeepAlive);
        }
        _propertyChildChange += value;
      }
      remove{
        _propertyChildChange -= value;
        if (_propertyChildChange == null) {
          EntryPoints.SetPropertyChildChange(null);
          _forwardPropertyChildChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyDescriptionChangeEventHandler PropertyDescriptionChange {
      add {
        if (_propertyDescriptionChange == null) {
          _forwardPropertyDescriptionChangeKeepAlive = ForwardPropertyDescriptionChange;
          EntryPoints.SetPropertyDescriptionChange(_forwardPropertyDescriptionChangeKeepAlive);
        }
        _propertyDescriptionChange += value;
      }
      remove{
        _propertyDescriptionChange -= value;
        if (_propertyDescriptionChange == null) {
          EntryPoints.SetPropertyDescriptionChange(null);
          _forwardPropertyDescriptionChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyNameChangeEventHandler PropertyNameChange {
      add {
        if (_propertyNameChange == null) {
          _forwardPropertyNameChangeKeepAlive = ForwardPropertyNameChange;
          EntryPoints.SetPropertyNameChange(_forwardPropertyNameChangeKeepAlive);
        }
        _propertyNameChange += value;
      }
      remove{
        _propertyNameChange -= value;
        if (_propertyNameChange == null) {
          EntryPoints.SetPropertyNameChange(null);
          _forwardPropertyNameChangeKeepAlive = null;
        }
      }
    }
    public override event PropertySelectionChangeEventHandler PropertySelectionChange {
      add {
        if (_propertySelectionChange == null) {
          _forwardPropertySelectionChangeKeepAlive = ForwardPropertySelectionChange;
          EntryPoints.SetPropertySelectionChange(_forwardPropertySelectionChangeKeepAlive);
        }
        _propertySelectionChange += value;
      }
      remove{
        _propertySelectionChange -= value;
        if (_propertySelectionChange == null) {
          EntryPoints.SetPropertySelectionChange(null);
          _forwardPropertySelectionChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyStateChangeEventHandler PropertyStateChange {
      add {
        if (_propertyStateChange == null) {
          _forwardPropertyStateChangeKeepAlive = ForwardPropertyStateChange;
          EntryPoints.SetPropertyStateChange(_forwardPropertyStateChangeKeepAlive);
        }
        _propertyStateChange += value;
      }
      remove{
        _propertyStateChange -= value;
        if (_propertyStateChange == null) {
          EntryPoints.SetPropertyStateChange(null);
          _forwardPropertyStateChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyTableModelChangeEventHandler PropertyTableModelChange {
      add {
        if (_propertyTableModelChange == null) {
          _forwardPropertyTableModelChangeKeepAlive = ForwardPropertyTableModelChange;
          EntryPoints.SetPropertyTableModelChange(_forwardPropertyTableModelChangeKeepAlive);
        }
        _propertyTableModelChange += value;
      }
      remove{
        _propertyTableModelChange -= value;
        if (_propertyTableModelChange == null) {
          EntryPoints.SetPropertyTableModelChange(null);
          _forwardPropertyTableModelChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyTextChangeEventHandler PropertyTextChange {
      add {
        if (_propertyTextChange == null) {
          _forwardPropertyTextChangeKeepAlive = ForwardPropertyTextChange;
          EntryPoints.SetPropertyTextChange(_forwardPropertyTextChangeKeepAlive);
        }
        _propertyTextChange += value;
      }
      remove{
        _propertyTextChange -= value;
        if (_propertyTextChange == null) {
          EntryPoints.SetPropertyTextChange(null);
          _forwardPropertyTextChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyValueChangeEventHandler PropertyValueChange {
      add {
        if (_propertyValueChange == null) {
          _forwardPropertyValueChangeKeepAlive = ForwardPropertyValueChange;
          EntryPoints.SetPropertyValueChange(_forwardPropertyValueChangeKeepAlive);
        }
        _propertyValueChange += value;
      }
      remove{
        _propertyValueChange -= value;
        if (_propertyValueChange == null) {
          EntryPoints.SetPropertyValueChange(null);
          _forwardPropertyValueChangeKeepAlive = null;
        }
      }
    }
    public override event PropertyVisibleDataChangeEventHandler PropertyVisibleDataChange {
      add {
        if (_propertyVisibleDataChange == null) {
          _forwardPropertyVisibleDataChangeKeepAlive = ForwardPropertyVisibleDataChange;
          EntryPoints.SetPropertyVisibleDataChange(_forwardPropertyVisibleDataChangeKeepAlive);
        }
        _propertyVisibleDataChange += value;
      }
      remove{
        _propertyVisibleDataChange -= value;
        if (_propertyVisibleDataChange == null) {
          EntryPoints.SetPropertyVisibleDataChange(null);
          _forwardPropertyVisibleDataChangeKeepAlive = null;
        }
      }
    }
    #endregion

    #region Event handlers
    protected virtual void OnCaretUpdate(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _caretUpdate;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnFocusGained(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _focusGained;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnFocusLost(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _focusLost;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnJavaShutdown(int vmid) {
      var handler = _javaShutdown;
      if (handler != null)
        handler(vmid);
    }
    protected virtual void OnMenuCanceled(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _menuCanceled;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnMenuDeselected(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _menuDeselected;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnMenuSelected(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _menuSelected;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnMouseClicked(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _mouseClicked;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnMouseEntered(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _mouseEntered;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnMouseExited(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _mouseExited;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnMousePressed(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _mousePressed;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnMouseReleased(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _mouseReleased;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnPopupMenuCanceled(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _popupMenuCanceled;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnPopupMenuWillBecomeInvisible(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _popupMenuWillBecomeInvisible;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnPopupMenuWillBecomeVisible(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _popupMenuWillBecomeVisible;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnPropertyActiveDescendentChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, JavaObjectHandle oldActiveDescendent, JavaObjectHandle newActiveDescendent) {
      var handler = _propertyActiveDescendentChange;
      if (handler != null)
        handler(vmid, evt, source, oldActiveDescendent, newActiveDescendent);
    }
    protected virtual void OnPropertyCaretChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, int oldPosition, int newPosition) {
      var handler = _propertyCaretChange;
      if (handler != null)
        handler(vmid, evt, source, oldPosition, newPosition);
    }
    protected virtual void OnPropertyChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string property, string oldValue, string newValue) {
      var handler = _propertyChange;
      if (handler != null)
        handler(vmid, evt, source, property, oldValue, newValue);
    }
    protected virtual void OnPropertyChildChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, JavaObjectHandle oldChild, JavaObjectHandle newChild) {
      var handler = _propertyChildChange;
      if (handler != null)
        handler(vmid, evt, source, oldChild, newChild);
    }
    protected virtual void OnPropertyDescriptionChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldDescription, string newDescription) {
      var handler = _propertyDescriptionChange;
      if (handler != null)
        handler(vmid, evt, source, oldDescription, newDescription);
    }
    protected virtual void OnPropertyNameChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldName, string newName) {
      var handler = _propertyNameChange;
      if (handler != null)
        handler(vmid, evt, source, oldName, newName);
    }
    protected virtual void OnPropertySelectionChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _propertySelectionChange;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnPropertyStateChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldState, string newState) {
      var handler = _propertyStateChange;
      if (handler != null)
        handler(vmid, evt, source, oldState, newState);
    }
    protected virtual void OnPropertyTableModelChange(int vmid, JavaObjectHandle evt, JavaObjectHandle src, string oldValue, string newValue) {
      var handler = _propertyTableModelChange;
      if (handler != null)
        handler(vmid, evt, src, oldValue, newValue);
    }
    protected virtual void OnPropertyTextChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _propertyTextChange;
      if (handler != null)
        handler(vmid, evt, source);
    }
    protected virtual void OnPropertyValueChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source, string oldValue, string newValue) {
      var handler = _propertyValueChange;
      if (handler != null)
        handler(vmid, evt, source, oldValue, newValue);
    }
    protected virtual void OnPropertyVisibleDataChange(int vmid, JavaObjectHandle evt, JavaObjectHandle source) {
      var handler = _propertyVisibleDataChange;
      if (handler != null)
        handler(vmid, evt, source);
    }
    #endregion

    private void DetachForwarders() {
      EntryPoints.SetCaretUpdate(null);
      _caretUpdate = null;
      _forwardCaretUpdateKeepAlive = null;

      EntryPoints.SetFocusGained(null);
      _focusGained = null;
      _forwardFocusGainedKeepAlive = null;

      EntryPoints.SetFocusLost(null);
      _focusLost = null;
      _forwardFocusLostKeepAlive = null;

      EntryPoints.SetJavaShutdown(null);
      _javaShutdown = null;
      _forwardJavaShutdownKeepAlive = null;

      EntryPoints.SetMenuCanceled(null);
      _menuCanceled = null;
      _forwardMenuCanceledKeepAlive = null;

      EntryPoints.SetMenuDeselected(null);
      _menuDeselected = null;
      _forwardMenuDeselectedKeepAlive = null;

      EntryPoints.SetMenuSelected(null);
      _menuSelected = null;
      _forwardMenuSelectedKeepAlive = null;

      EntryPoints.SetMouseClicked(null);
      _mouseClicked = null;
      _forwardMouseClickedKeepAlive = null;

      EntryPoints.SetMouseEntered(null);
      _mouseEntered = null;
      _forwardMouseEnteredKeepAlive = null;

      EntryPoints.SetMouseExited(null);
      _mouseExited = null;
      _forwardMouseExitedKeepAlive = null;

      EntryPoints.SetMousePressed(null);
      _mousePressed = null;
      _forwardMousePressedKeepAlive = null;

      EntryPoints.SetMouseReleased(null);
      _mouseReleased = null;
      _forwardMouseReleasedKeepAlive = null;

      EntryPoints.SetPopupMenuCanceled(null);
      _popupMenuCanceled = null;
      _forwardPopupMenuCanceledKeepAlive = null;

      EntryPoints.SetPopupMenuWillBecomeInvisible(null);
      _popupMenuWillBecomeInvisible = null;
      _forwardPopupMenuWillBecomeInvisibleKeepAlive = null;

      EntryPoints.SetPopupMenuWillBecomeVisible(null);
      _popupMenuWillBecomeVisible = null;
      _forwardPopupMenuWillBecomeVisibleKeepAlive = null;

      EntryPoints.SetPropertyActiveDescendentChange(null);
      _propertyActiveDescendentChange = null;
      _forwardPropertyActiveDescendentChangeKeepAlive = null;

      EntryPoints.SetPropertyCaretChange(null);
      _propertyCaretChange = null;
      _forwardPropertyCaretChangeKeepAlive = null;

      EntryPoints.SetPropertyChange(null);
      _propertyChange = null;
      _forwardPropertyChangeKeepAlive = null;

      EntryPoints.SetPropertyChildChange(null);
      _propertyChildChange = null;
      _forwardPropertyChildChangeKeepAlive = null;

      EntryPoints.SetPropertyDescriptionChange(null);
      _propertyDescriptionChange = null;
      _forwardPropertyDescriptionChangeKeepAlive = null;

      EntryPoints.SetPropertyNameChange(null);
      _propertyNameChange = null;
      _forwardPropertyNameChangeKeepAlive = null;

      EntryPoints.SetPropertySelectionChange(null);
      _propertySelectionChange = null;
      _forwardPropertySelectionChangeKeepAlive = null;

      EntryPoints.SetPropertyStateChange(null);
      _propertyStateChange = null;
      _forwardPropertyStateChangeKeepAlive = null;

      EntryPoints.SetPropertyTableModelChange(null);
      _propertyTableModelChange = null;
      _forwardPropertyTableModelChangeKeepAlive = null;

      EntryPoints.SetPropertyTextChange(null);
      _propertyTextChange = null;
      _forwardPropertyTextChangeKeepAlive = null;

      EntryPoints.SetPropertyValueChange(null);
      _propertyValueChange = null;
      _forwardPropertyValueChangeKeepAlive = null;

      EntryPoints.SetPropertyVisibleDataChange(null);
      _propertyVisibleDataChange = null;
      _forwardPropertyVisibleDataChangeKeepAlive = null;

    }

    #region Event forwarders
    private void ForwardCaretUpdate(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnCaretUpdate(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardFocusGained(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnFocusGained(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardFocusLost(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnFocusLost(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardJavaShutdown(int vmid) {
      OnJavaShutdown(vmid);
    }
    private void ForwardMenuCanceled(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMenuCanceled(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardMenuDeselected(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMenuDeselected(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardMenuSelected(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMenuSelected(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardMouseClicked(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMouseClicked(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardMouseEntered(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMouseEntered(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardMouseExited(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMouseExited(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardMousePressed(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMousePressed(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardMouseReleased(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnMouseReleased(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardPopupMenuCanceled(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnPopupMenuCanceled(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardPopupMenuWillBecomeInvisible(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnPopupMenuWillBecomeInvisible(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardPopupMenuWillBecomeVisible(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnPopupMenuWillBecomeVisible(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardPropertyActiveDescendentChange(int vmid, JOBJECT32 evt, JOBJECT32 source, JOBJECT32 oldActiveDescendent, JOBJECT32 newActiveDescendent) {
      OnPropertyActiveDescendentChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), Wrap(vmid, oldActiveDescendent), Wrap(vmid, newActiveDescendent));
    }
    private void ForwardPropertyCaretChange(int vmid, JOBJECT32 evt, JOBJECT32 source, int oldPosition, int newPosition) {
      OnPropertyCaretChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), oldPosition, newPosition);
    }
    private void ForwardPropertyChange(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string property, [MarshalAs(UnmanagedType.LPWStr)]string oldValue, [MarshalAs(UnmanagedType.LPWStr)]string newValue) {
      OnPropertyChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), property, oldValue, newValue);
    }
    private void ForwardPropertyChildChange(int vmid, JOBJECT32 evt, JOBJECT32 source, JOBJECT32 oldChild, JOBJECT32 newChild) {
      OnPropertyChildChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), Wrap(vmid, oldChild), Wrap(vmid, newChild));
    }
    private void ForwardPropertyDescriptionChange(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldDescription, [MarshalAs(UnmanagedType.LPWStr)]string newDescription) {
      OnPropertyDescriptionChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), oldDescription, newDescription);
    }
    private void ForwardPropertyNameChange(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldName, [MarshalAs(UnmanagedType.LPWStr)]string newName) {
      OnPropertyNameChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), oldName, newName);
    }
    private void ForwardPropertySelectionChange(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnPropertySelectionChange(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardPropertyStateChange(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldState, [MarshalAs(UnmanagedType.LPWStr)]string newState) {
      OnPropertyStateChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), oldState, newState);
    }
    private void ForwardPropertyTableModelChange(int vmid, JOBJECT32 evt, JOBJECT32 src, [MarshalAs(UnmanagedType.LPWStr)]string oldValue, [MarshalAs(UnmanagedType.LPWStr)]string newValue) {
      OnPropertyTableModelChange(vmid, Wrap(vmid, evt), Wrap(vmid, src), oldValue, newValue);
    }
    private void ForwardPropertyTextChange(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnPropertyTextChange(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    private void ForwardPropertyValueChange(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldValue, [MarshalAs(UnmanagedType.LPWStr)]string newValue) {
      OnPropertyValueChange(vmid, Wrap(vmid, evt), Wrap(vmid, source), oldValue, newValue);
    }
    private void ForwardPropertyVisibleDataChange(int vmid, JOBJECT32 evt, JOBJECT32 source) {
      OnPropertyVisibleDataChange(vmid, Wrap(vmid, evt), Wrap(vmid, source));
    }
    #endregion
  }

  /// <summary>
  /// Container of Legacy WindowAccessBridge DLL entry points
  /// </summary>
  internal class AccessBridgeEntryPointsLegacy {
    #region Functions
    public ActivateAccessibleHyperlinkFP ActivateAccessibleHyperlink { get; set; }
    public AddAccessibleSelectionFromContextFP AddAccessibleSelectionFromContext { get; set; }
    public ClearAccessibleSelectionFromContextFP ClearAccessibleSelectionFromContext { get; set; }
    public DoAccessibleActionsFP DoAccessibleActions { get; set; }
    public GetAccessibleActionsFP GetAccessibleActions { get; set; }
    public GetAccessibleChildFromContextFP GetAccessibleChildFromContext { get; set; }
    public GetAccessibleContextAtFP GetAccessibleContextAt { get; set; }
    public GetAccessibleContextFromHWNDFP GetAccessibleContextFromHWND { get; set; }
    public GetAccessibleContextInfoFP GetAccessibleContextInfo { get; set; }
    public GetAccessibleContextWithFocusFP GetAccessibleContextWithFocus { get; set; }
    public GetAccessibleHyperlinkFP GetAccessibleHyperlink { get; set; }
    public GetAccessibleHyperlinkCountFP GetAccessibleHyperlinkCount { get; set; }
    public GetAccessibleHypertextFP GetAccessibleHypertext { get; set; }
    public GetAccessibleHypertextExtFP GetAccessibleHypertextExt { get; set; }
    public GetAccessibleHypertextLinkIndexFP GetAccessibleHypertextLinkIndex { get; set; }
    public GetAccessibleIconsFP GetAccessibleIcons { get; set; }
    public GetAccessibleKeyBindingsFP GetAccessibleKeyBindings { get; set; }
    public GetAccessibleParentFromContextFP GetAccessibleParentFromContext { get; set; }
    public GetAccessibleRelationSetFP GetAccessibleRelationSet { get; set; }
    public GetAccessibleSelectionCountFromContextFP GetAccessibleSelectionCountFromContext { get; set; }
    public GetAccessibleSelectionFromContextFP GetAccessibleSelectionFromContext { get; set; }
    public GetAccessibleTableCellInfoFP GetAccessibleTableCellInfo { get; set; }
    public GetAccessibleTableColumnFP GetAccessibleTableColumn { get; set; }
    public GetAccessibleTableColumnDescriptionFP GetAccessibleTableColumnDescription { get; set; }
    public GetAccessibleTableColumnHeaderFP GetAccessibleTableColumnHeader { get; set; }
    public GetAccessibleTableColumnSelectionCountFP GetAccessibleTableColumnSelectionCount { get; set; }
    public GetAccessibleTableColumnSelectionsFP GetAccessibleTableColumnSelections { get; set; }
    public GetAccessibleTableIndexFP GetAccessibleTableIndex { get; set; }
    public GetAccessibleTableInfoFP GetAccessibleTableInfo { get; set; }
    public GetAccessibleTableRowFP GetAccessibleTableRow { get; set; }
    public GetAccessibleTableRowDescriptionFP GetAccessibleTableRowDescription { get; set; }
    public GetAccessibleTableRowHeaderFP GetAccessibleTableRowHeader { get; set; }
    public GetAccessibleTableRowSelectionCountFP GetAccessibleTableRowSelectionCount { get; set; }
    public GetAccessibleTableRowSelectionsFP GetAccessibleTableRowSelections { get; set; }
    public GetAccessibleTextAttributesFP GetAccessibleTextAttributes { get; set; }
    public GetAccessibleTextInfoFP GetAccessibleTextInfo { get; set; }
    public GetAccessibleTextItemsFP GetAccessibleTextItems { get; set; }
    public GetAccessibleTextLineBoundsFP GetAccessibleTextLineBounds { get; set; }
    public GetAccessibleTextRangeFP GetAccessibleTextRange { get; set; }
    public GetAccessibleTextRectFP GetAccessibleTextRect { get; set; }
    public GetAccessibleTextSelectionInfoFP GetAccessibleTextSelectionInfo { get; set; }
    public GetActiveDescendentFP GetActiveDescendent { get; set; }
    public GetCaretLocationFP GetCaretLocation { get; set; }
    public GetCurrentAccessibleValueFromContextFP GetCurrentAccessibleValueFromContext { get; set; }
    public GetHWNDFromAccessibleContextFP GetHWNDFromAccessibleContext { get; set; }
    public GetMaximumAccessibleValueFromContextFP GetMaximumAccessibleValueFromContext { get; set; }
    public GetMinimumAccessibleValueFromContextFP GetMinimumAccessibleValueFromContext { get; set; }
    public GetObjectDepthFP GetObjectDepth { get; set; }
    public GetParentWithRoleFP GetParentWithRole { get; set; }
    public GetParentWithRoleElseRootFP GetParentWithRoleElseRoot { get; set; }
    public GetTextAttributesInRangeFP GetTextAttributesInRange { get; set; }
    public GetTopLevelObjectFP GetTopLevelObject { get; set; }
    public GetVersionInfoFP GetVersionInfo { get; set; }
    public GetVirtualAccessibleNameFP GetVirtualAccessibleName { get; set; }
    public GetVisibleChildrenFP GetVisibleChildren { get; set; }
    public GetVisibleChildrenCountFP GetVisibleChildrenCount { get; set; }
    public IsAccessibleChildSelectedFromContextFP IsAccessibleChildSelectedFromContext { get; set; }
    public IsAccessibleTableColumnSelectedFP IsAccessibleTableColumnSelected { get; set; }
    public IsAccessibleTableRowSelectedFP IsAccessibleTableRowSelected { get; set; }
    public IsJavaWindowFP IsJavaWindow { get; set; }
    public IsSameObjectFP IsSameObject { get; set; }
    public RemoveAccessibleSelectionFromContextFP RemoveAccessibleSelectionFromContext { get; set; }
    public SelectAllAccessibleSelectionFromContextFP SelectAllAccessibleSelectionFromContext { get; set; }
    public SetTextContentsFP SetTextContents { get; set; }
    public Windows_runFP Windows_run { get; set; }
    #endregion

    #region Event functions
    public SetCaretUpdateFP SetCaretUpdate { get; set; }
    public SetFocusGainedFP SetFocusGained { get; set; }
    public SetFocusLostFP SetFocusLost { get; set; }
    public SetJavaShutdownFP SetJavaShutdown { get; set; }
    public SetMenuCanceledFP SetMenuCanceled { get; set; }
    public SetMenuDeselectedFP SetMenuDeselected { get; set; }
    public SetMenuSelectedFP SetMenuSelected { get; set; }
    public SetMouseClickedFP SetMouseClicked { get; set; }
    public SetMouseEnteredFP SetMouseEntered { get; set; }
    public SetMouseExitedFP SetMouseExited { get; set; }
    public SetMousePressedFP SetMousePressed { get; set; }
    public SetMouseReleasedFP SetMouseReleased { get; set; }
    public SetPopupMenuCanceledFP SetPopupMenuCanceled { get; set; }
    public SetPopupMenuWillBecomeInvisibleFP SetPopupMenuWillBecomeInvisible { get; set; }
    public SetPopupMenuWillBecomeVisibleFP SetPopupMenuWillBecomeVisible { get; set; }
    public SetPropertyActiveDescendentChangeFP SetPropertyActiveDescendentChange { get; set; }
    public SetPropertyCaretChangeFP SetPropertyCaretChange { get; set; }
    public SetPropertyChangeFP SetPropertyChange { get; set; }
    public SetPropertyChildChangeFP SetPropertyChildChange { get; set; }
    public SetPropertyDescriptionChangeFP SetPropertyDescriptionChange { get; set; }
    public SetPropertyNameChangeFP SetPropertyNameChange { get; set; }
    public SetPropertySelectionChangeFP SetPropertySelectionChange { get; set; }
    public SetPropertyStateChangeFP SetPropertyStateChange { get; set; }
    public SetPropertyTableModelChangeFP SetPropertyTableModelChange { get; set; }
    public SetPropertyTextChangeFP SetPropertyTextChange { get; set; }
    public SetPropertyValueChangeFP SetPropertyValueChange { get; set; }
    public SetPropertyVisibleDataChangeFP SetPropertyVisibleDataChange { get; set; }
    #endregion

    #region Function delegate types
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL ActivateAccessibleHyperlinkFP(int vmid, JOBJECT32 accessibleContext, JOBJECT32 accessibleHyperlink);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void AddAccessibleSelectionFromContextFP(int vmid, JOBJECT32 asel, int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void ClearAccessibleSelectionFromContextFP(int vmid, JOBJECT32 asel);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL DoAccessibleActionsFP(int vmid, JOBJECT32 accessibleContext, ref AccessibleActionsToDo actionsToDo, out int failure);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleActionsFP(int vmid, JOBJECT32 accessibleContext, [Out]AccessibleActions actions);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetAccessibleChildFromContextFP(int vmid, JOBJECT32 ac, int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleContextAtFP(int vmid, JOBJECT32 acParent, int x, int y, out JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleContextFromHWNDFP(WindowHandle window, out int vmid, out JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleContextInfoFP(int vmid, JOBJECT32 ac, [Out]AccessibleContextInfo info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleContextWithFocusFP(WindowHandle window, out int vmid, out JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleHyperlinkFP(int vmid, JOBJECT32 hypertext, int nIndex, out AccessibleHyperlinkInfoNativeLegacy hyperlinkInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleHyperlinkCountFP(int vmid, JOBJECT32 accessibleContext);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleHypertextFP(int vmid, JOBJECT32 accessibleContext, out AccessibleHypertextInfoNativeLegacy hypertextInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleHypertextExtFP(int vmid, JOBJECT32 accessibleContext, int nStartIndex, out AccessibleHypertextInfoNativeLegacy hypertextInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleHypertextLinkIndexFP(int vmid, JOBJECT32 hypertext, int nIndex);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleIconsFP(int vmid, JOBJECT32 accessibleContext, out AccessibleIcons icons);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleKeyBindingsFP(int vmid, JOBJECT32 accessibleContext, out AccessibleKeyBindings keyBindings);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetAccessibleParentFromContextFP(int vmid, JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleRelationSetFP(int vmid, JOBJECT32 accessibleContext, out AccessibleRelationSetInfoNativeLegacy relationSetInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleSelectionCountFromContextFP(int vmid, JOBJECT32 asel);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetAccessibleSelectionFromContextFP(int vmid, JOBJECT32 asel, int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTableCellInfoFP(int vmid, JOBJECT32 at, int row, int column, [Out]AccessibleTableCellInfoNativeLegacy tableCellInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleTableColumnFP(int vmid, JOBJECT32 table, int index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetAccessibleTableColumnDescriptionFP(int vmid, JOBJECT32 acParent, int column);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTableColumnHeaderFP(int vmid, JOBJECT32 acParent, [Out]AccessibleTableInfoNativeLegacy tableInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleTableColumnSelectionCountFP(int vmid, JOBJECT32 table);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTableColumnSelectionsFP(int vmid, JOBJECT32 table, int count, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]int[] selections);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleTableIndexFP(int vmid, JOBJECT32 table, int row, int column);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTableInfoFP(int vmid, JOBJECT32 ac, [Out]AccessibleTableInfoNativeLegacy tableInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleTableRowFP(int vmid, JOBJECT32 table, int index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetAccessibleTableRowDescriptionFP(int vmid, JOBJECT32 acParent, int row);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTableRowHeaderFP(int vmid, JOBJECT32 acParent, [Out]AccessibleTableInfoNativeLegacy tableInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetAccessibleTableRowSelectionCountFP(int vmid, JOBJECT32 table);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTableRowSelectionsFP(int vmid, JOBJECT32 table, int count, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]int[] selections);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTextAttributesFP(int vmid, JOBJECT32 at, int index, [Out]AccessibleTextAttributesInfo attributes);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTextInfoFP(int vmid, JOBJECT32 at, out AccessibleTextInfo textInfo, int x, int y);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTextItemsFP(int vmid, JOBJECT32 at, out AccessibleTextItemsInfo textItems, int index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTextLineBoundsFP(int vmid, JOBJECT32 at, int index, out int startIndex, out int endIndex);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTextRangeFP(int vmid, JOBJECT32 at, int start, int end, [Out]char[] text, short len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTextRectFP(int vmid, JOBJECT32 at, out AccessibleTextRectInfo rectInfo, int index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetAccessibleTextSelectionInfoFP(int vmid, JOBJECT32 at, out AccessibleTextSelectionInfo textSelection);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetActiveDescendentFP(int vmid, JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetCaretLocationFP(int vmid, JOBJECT32 ac, out AccessibleTextRectInfo rectInfo, int index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetCurrentAccessibleValueFromContextFP(int vmid, JOBJECT32 av, StringBuilder value, short len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate WindowHandle GetHWNDFromAccessibleContextFP(int vmid, JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetMaximumAccessibleValueFromContextFP(int vmid, JOBJECT32 av, StringBuilder value, short len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetMinimumAccessibleValueFromContextFP(int vmid, JOBJECT32 av, StringBuilder value, short len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetObjectDepthFP(int vmid, JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetParentWithRoleFP(int vmid, JOBJECT32 ac, [MarshalAs(UnmanagedType.LPWStr)]string role);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetParentWithRoleElseRootFP(int vmid, JOBJECT32 ac, [MarshalAs(UnmanagedType.LPWStr)]string role);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetTextAttributesInRangeFP(int vmid, JOBJECT32 accessibleContext, int startIndex, int endIndex, [Out]AccessibleTextAttributesInfo attributes, out short len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate JOBJECT32 GetTopLevelObjectFP(int vmid, JOBJECT32 ac);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetVersionInfoFP(int vmid, out AccessBridgeVersionInfo info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetVirtualAccessibleNameFP(int vmid, JOBJECT32 ac, StringBuilder name, int len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL GetVisibleChildrenFP(int vmid, JOBJECT32 accessibleContext, int startIndex, out VisibleChildrenInfoNativeLegacy children);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate int GetVisibleChildrenCountFP(int vmid, JOBJECT32 accessibleContext);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL IsAccessibleChildSelectedFromContextFP(int vmid, JOBJECT32 asel, int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL IsAccessibleTableColumnSelectedFP(int vmid, JOBJECT32 table, int column);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL IsAccessibleTableRowSelectedFP(int vmid, JOBJECT32 table, int row);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL IsJavaWindowFP(WindowHandle window);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL IsSameObjectFP(int vmid, JOBJECT32 obj1, JOBJECT32 obj2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void RemoveAccessibleSelectionFromContextFP(int vmid, JOBJECT32 asel, int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void SelectAllAccessibleSelectionFromContextFP(int vmid, JOBJECT32 asel);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetTextContentsFP(int vmid, JOBJECT32 ac, string text);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void Windows_runFP();
    #endregion

    #region Event delegate types
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void CaretUpdateEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void FocusGainedEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void FocusLostEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void JavaShutdownEventHandler(int vmid);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MenuCanceledEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MenuDeselectedEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MenuSelectedEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MouseClickedEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MouseEnteredEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MouseExitedEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MousePressedEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void MouseReleasedEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PopupMenuCanceledEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PopupMenuWillBecomeInvisibleEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PopupMenuWillBecomeVisibleEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyActiveDescendentChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, JOBJECT32 oldActiveDescendent, JOBJECT32 newActiveDescendent);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyCaretChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, int oldPosition, int newPosition);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string property, [MarshalAs(UnmanagedType.LPWStr)]string oldValue, [MarshalAs(UnmanagedType.LPWStr)]string newValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyChildChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, JOBJECT32 oldChild, JOBJECT32 newChild);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyDescriptionChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldDescription, [MarshalAs(UnmanagedType.LPWStr)]string newDescription);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyNameChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldName, [MarshalAs(UnmanagedType.LPWStr)]string newName);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertySelectionChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyStateChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldState, [MarshalAs(UnmanagedType.LPWStr)]string newState);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyTableModelChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 src, [MarshalAs(UnmanagedType.LPWStr)]string oldValue, [MarshalAs(UnmanagedType.LPWStr)]string newValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyTextChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyValueChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source, [MarshalAs(UnmanagedType.LPWStr)]string oldValue, [MarshalAs(UnmanagedType.LPWStr)]string newValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void PropertyVisibleDataChangeEventHandler(int vmid, JOBJECT32 evt, JOBJECT32 source);
    #endregion

    #region Event function delegate types
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetCaretUpdateFP(CaretUpdateEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetFocusGainedFP(FocusGainedEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetFocusLostFP(FocusLostEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetJavaShutdownFP(JavaShutdownEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMenuCanceledFP(MenuCanceledEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMenuDeselectedFP(MenuDeselectedEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMenuSelectedFP(MenuSelectedEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMouseClickedFP(MouseClickedEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMouseEnteredFP(MouseEnteredEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMouseExitedFP(MouseExitedEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMousePressedFP(MousePressedEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetMouseReleasedFP(MouseReleasedEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPopupMenuCanceledFP(PopupMenuCanceledEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPopupMenuWillBecomeInvisibleFP(PopupMenuWillBecomeInvisibleEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPopupMenuWillBecomeVisibleFP(PopupMenuWillBecomeVisibleEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyActiveDescendentChangeFP(PropertyActiveDescendentChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyCaretChangeFP(PropertyCaretChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyChangeFP(PropertyChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyChildChangeFP(PropertyChildChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyDescriptionChangeFP(PropertyDescriptionChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyNameChangeFP(PropertyNameChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertySelectionChangeFP(PropertySelectionChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyStateChangeFP(PropertyStateChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyTableModelChangeFP(PropertyTableModelChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyTextChangeFP(PropertyTextChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyValueChangeFP(PropertyValueChangeEventHandler handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate BOOL SetPropertyVisibleDataChangeFP(PropertyVisibleDataChangeEventHandler handler);
    #endregion
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct AccessibleHyperlinkInfoNativeLegacy {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string text;
    public int startIndex;
    public int endIndex;
    public JOBJECT32 accessibleHyperlink;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct AccessibleHypertextInfoNativeLegacy {
    public int linkCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public AccessibleHyperlinkInfoNativeLegacy[] links;
    public JOBJECT32 accessibleHypertext;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct AccessibleRelationInfoNativeLegacy {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string key;
    public int targetCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
    public JOBJECT32[] targets;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct AccessibleRelationSetInfoNativeLegacy {
    public int relationCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public AccessibleRelationInfoNativeLegacy[] relations;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct VisibleChildrenInfoNativeLegacy {
    public int returnedChildrenCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public JOBJECT32[] children;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal class AccessibleTableCellInfoNativeLegacy {
    public JOBJECT32 accessibleContext;
    public int index;
    public int row;
    public int column;
    public int rowExtent;
    public int columnExtent;
    public byte isSelected;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal class AccessibleTableInfoNativeLegacy {
    public JOBJECT32 caption;
    public JOBJECT32 summary;
    public int rowCount;
    public int columnCount;
    public JOBJECT32 accessibleContext;
    public JOBJECT32 accessibleTable;
  }

}
