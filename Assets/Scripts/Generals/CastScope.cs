using System;
using System.Collections.Generic;
using static SkillRunner;

/*public static class CastScope
{
	[ThreadStatic] static Stack<Scope> _stack;
	public static IDisposable Push(int runnerId, int castId, IIntentSink sink)
	{
		_stack ??= new Stack<Scope>();
		_stack.Push(new Scope(runnerId, castId, sink));
		return new Popper();
	}
	public static bool TryAddIntent(int runnerId, int castId, CastIntent intent)
	{
		if (_stack == null || _stack.Count == 0) return false;
		var top = _stack.Peek();
		if (top.RunnerId != runnerId || top.CastId != castId) return false; // �� ������ ����
		top.Sink.Add(intent);
		return true;
	}
	public static IReadOnlyList<CastIntent> Drain(int runnerId, int castId)
	{
		var top = _stack?.Peek();
		if (top == null || top.RunnerId != runnerId || top.CastId != castId) return Array.Empty<CastIntent>();
		return top.Sink.Drain(); // sink�� ���� ����Ʈ�� ���� ����
	}
	// �� Scope, Popper, IIntentSink/IntentBuffer ���� (�Ʒ� ����)
}
public interface IIntentSink { void Add(CastIntent i); IReadOnlyList<CastIntent> Drain(); }
public sealed class IntentBuffer : IIntentSink
{
	readonly List<CastIntent> _list = new();
	public void Add(CastIntent i) => _list.Add(i);
	public IReadOnlyList<CastIntent> Drain() { var r = _list.ToArray(); _list.Clear(); return r; }
}*/