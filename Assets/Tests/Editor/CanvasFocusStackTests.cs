using System.Collections.Generic;
using MyEnums;
using NUnit.Framework;

/// <summary>
/// CanvasFocusStack（纯 C# 焦点栈）单元测试：开闭顺序、order 计算、ESC 行为、互斥遍历快照。
/// 对应重构清单 4.4 / 4.6。
/// </summary>
public class CanvasFocusStackTests
{
    private CanvasFocusStack stack;
    private readonly List<(CanvasToToggle canvas, bool state)> toggleLog = new();
    private readonly List<CanvasToToggle> focusLog = new();

    [SetUp]
    public void SetUp()
    {
        stack = new CanvasFocusStack();
        toggleLog.Clear();
        focusLog.Clear();
        stack.OnCanvasToggleRequested = (c, s) => toggleLog.Add((c, s));
        stack.OnFocusRefreshRequested = c => focusLog.Add(c);
    }

    [Test]
    public void ReportState_OpenAddsToOpenOrder()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        Assert.IsTrue(stack.IsCanvasOpen(CanvasToToggle.Stats));
        Assert.IsTrue(stack.IsAnyCanvasOpen);
        Assert.AreEqual(CanvasToToggle.Stats, stack.LastOpenCanvas);
    }

    [Test]
    public void ReportState_CloseRemovesFromOpenOrder()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.ReportState(CanvasToToggle.Stats, false);
        Assert.IsFalse(stack.IsCanvasOpen(CanvasToToggle.Stats));
        Assert.IsFalse(stack.IsAnyCanvasOpen);
    }

    [Test]
    public void ReportState_DefaultIsIgnored()
    {
        stack.ReportState(CanvasToToggle.Default, true);
        Assert.IsFalse(stack.IsAnyCanvasOpen);
    }

    [Test]
    public void ReportState_ReopenMovesCanvasToTop()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.ReportState(CanvasToToggle.Backpack, true);
        stack.ReportState(CanvasToToggle.Stats, true); // 重复上报开启 = 提到顶层
        CollectionAssert.AreEqual(
            new[] { CanvasToToggle.Backpack, CanvasToToggle.Stats },
            stack.GetOpenCanvases());
    }

    [Test]
    public void GetCanvasOrder_TopIsHighest_SecondIsOneStepLower()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.ReportState(CanvasToToggle.Backpack, true);

        Assert.AreEqual(CanvasFocusStack.FocusOrder, stack.GetCanvasOrder(CanvasToToggle.Backpack, true));
        Assert.AreEqual(CanvasFocusStack.FocusOrder - 10, stack.GetCanvasOrder(CanvasToToggle.Stats, true));
        Assert.AreEqual(CanvasFocusStack.DefaultOrder, stack.GetCanvasOrder(CanvasToToggle.Stats, false));
    }

    [Test]
    public void ApplyFocusChange_UnopenedCanvasRaisesOpenEvent()
    {
        stack.ApplyFocusChange(CanvasToToggle.Shop);
        CollectionAssert.Contains(toggleLog, (CanvasToToggle.Shop, true));
        // 上报后才真正进入 open-order
        Assert.IsFalse(stack.IsCanvasOpen(CanvasToToggle.Shop));
        stack.ReportState(CanvasToToggle.Shop, true);
        Assert.IsTrue(stack.IsCanvasOpen(CanvasToToggle.Shop));
    }

    [Test]
    public void ApplyFocusChange_OpenedCanvasMovesToTopWithoutNewToggleEvent()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.ReportState(CanvasToToggle.Backpack, true);
        int eventsBefore = toggleLog.Count;

        stack.ApplyFocusChange(CanvasToToggle.Stats); // 已打开：仅提升，不重发开事件

        Assert.AreEqual(eventsBefore, toggleLog.Count);
        CollectionAssert.AreEqual(
            new[] { CanvasToToggle.Backpack, CanvasToToggle.Stats },
            stack.GetOpenCanvases());
    }

    [Test]
    public void HandleESCOrCloseTop_NothingOpen_OpensESC()
    {
        stack.HandleESCOrCloseTop();
        CollectionAssert.Contains(toggleLog, (CanvasToToggle.ESC, true));
    }

    [Test]
    public void HandleESCOrCloseTop_ClosesTopOnly()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.ReportState(CanvasToToggle.Backpack, true);

        stack.HandleESCOrCloseTop();

        CollectionAssert.Contains(toggleLog, (CanvasToToggle.Backpack, false));
        CollectionAssert.DoesNotContain(toggleLog, (CanvasToToggle.Stats, false));
        // 事件只发不拆栈：真实流程由面板收到事件后回报 ReportState(false) 才出栈
        stack.ReportState(CanvasToToggle.Backpack, false);
        Assert.IsTrue(stack.IsCanvasOpen(CanvasToToggle.Stats));
        Assert.IsFalse(stack.IsCanvasOpen(CanvasToToggle.Backpack));
    }

    [Test]
    public void HandleESCOrOpen_AlwaysRequestsESCOpen()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.HandleESCOrOpen();
        CollectionAssert.Contains(toggleLog, (CanvasToToggle.ESC, true));
    }

    [Test]
    public void RequestClose_OnlyWorksOnOpenCanvas()
    {
        int eventsBefore = toggleLog.Count;
        stack.RequestClose(CanvasToToggle.Stats); // 未打开：应无事件
        Assert.AreEqual(eventsBefore, toggleLog.Count);

        stack.ReportState(CanvasToToggle.Stats, true);
        stack.RequestClose(CanvasToToggle.Stats);
        CollectionAssert.Contains(toggleLog, (CanvasToToggle.Stats, false));
    }

    [Test]
    public void HandleFocus_IgnoresDefaultAndCurrentFocus()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.ApplyFocusChange(CanvasToToggle.Stats);
        int eventsBefore = toggleLog.Count;

        stack.HandleFocus(CanvasToToggle.Default);
        stack.HandleFocus(CanvasToToggle.Stats); // 已是焦点且已打开

        Assert.AreEqual(eventsBefore, toggleLog.Count);
    }

    [Test]
    public void Clear_ResetsAllState()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.Clear();
        Assert.IsFalse(stack.IsAnyCanvasOpen);
        Assert.AreEqual(CanvasToToggle.Default, stack.CurrentFocus);
        Assert.IsEmpty(stack.GetOpenCanvases());
    }

    [Test]
    public void GetOpenCanvases_ReturnsSnapshotCopy()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        var snapshot = stack.GetOpenCanvases();
        snapshot.Clear(); // 修改副本不应影响栈内部
        Assert.IsTrue(stack.IsCanvasOpen(CanvasToToggle.Stats));
    }

    [Test]
    public void ReportState_RefreshesOrdersOfAllOpenCanvases()
    {
        stack.ReportState(CanvasToToggle.Stats, true);
        stack.ReportState(CanvasToToggle.Backpack, true);
        focusLog.Clear();
        stack.ReportState(CanvasToToggle.Quest, true);
        // 链表变化后，所有已打开画布都应收到 order 刷新请求
        CollectionAssert.AreEquivalent(
            new[] { CanvasToToggle.Stats, CanvasToToggle.Backpack, CanvasToToggle.Quest },
            focusLog);
    }
}
