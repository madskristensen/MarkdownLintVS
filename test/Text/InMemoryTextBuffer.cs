using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.Test;

internal sealed class InMemoryTextBuffer
{
    private string _text;
    private int _version;

    public InMemoryTextBuffer(string text)
    {
        _text = text;
        Buffer = Proxy<ITextBuffer>(HandleBuffer);
    }

    public ITextBuffer Buffer { get; }
    public string Text => _text;

    private object? HandleBuffer(MethodInfo method, object?[]? args)
    {
        return method.Name switch
        {
            "get_CurrentSnapshot" => CreateSnapshot(),
            "CreateEdit" => CreateEdit(CreateSnapshot()),
            _ => Default(method.ReturnType),
        };
    }

    private ITextSnapshot CreateSnapshot()
    {
        string text = _text;
        int version = _version;
        ITextSnapshot? snapshot = null;
        snapshot = Proxy<ITextSnapshot>((method, args) => HandleSnapshot(snapshot!, text, version, method, args));
        return snapshot;
    }

    private object? HandleSnapshot(
        ITextSnapshot snapshot,
        string text,
        int version,
        MethodInfo method,
        object?[]? args)
    {
        switch (method.Name)
        {
            case "get_TextBuffer":
                return Buffer;
            case "get_Length":
                return text.Length;
            case "get_LineCount":
                return GetLines(text).Count;
            case "get_Version":
                return Proxy<ITextVersion>((versionMethod, _) =>
                    versionMethod.Name == "get_VersionNumber" ? version : Default(versionMethod.ReturnType));
            case "GetText":
                return GetText(text, args);
            case "GetLineFromLineNumber":
                return CreateLine(snapshot, text, (int)args![0]!);
            case "GetLineFromPosition":
                return CreateLine(snapshot, text, GetPosition(args![0]), findByPosition: true);
            case "GetLineNumberFromPosition":
                return FindLine(GetLines(text), GetPosition(args![0]));
            default:
                return Default(method.ReturnType);
        }
    }

    private static string GetText(string text, object?[]? args)
    {
        if (args == null || args.Length == 0)
            return text;

        if (args[0] is Span span)
            return text.Substring(span.Start, span.Length);

        return text.Substring(GetPosition(args[0]), (int)args[1]!);
    }

    private static ITextSnapshotLine CreateLine(
        ITextSnapshot snapshot,
        string text,
        int value,
        bool findByPosition = false)
    {
        IReadOnlyList<LineInfo> lines = GetLines(text);
        int lineNumber = findByPosition ? FindLine(lines, value) : value;
        LineInfo line = lines[lineNumber];

        return Proxy<ITextSnapshotLine>((method, _) => method.Name switch
        {
            "get_Snapshot" => snapshot,
            "get_LineNumber" => lineNumber,
            "get_Start" => new SnapshotPoint(snapshot, line.Start),
            "get_End" => new SnapshotPoint(snapshot, line.Start + line.Length),
            "get_EndIncludingLineBreak" => new SnapshotPoint(snapshot, line.Start + line.LengthIncludingLineBreak),
            "get_Length" => line.Length,
            "get_LengthIncludingLineBreak" => line.LengthIncludingLineBreak,
            "get_LineBreakLength" => line.LengthIncludingLineBreak - line.Length,
            "get_Extent" => new SnapshotSpan(snapshot, line.Start, line.Length),
            "get_ExtentIncludingLineBreak" => new SnapshotSpan(snapshot, line.Start, line.LengthIncludingLineBreak),
            "GetText" => text.Substring(line.Start, line.Length),
            "GetTextIncludingLineBreak" => text.Substring(line.Start, line.LengthIncludingLineBreak),
            "GetLineBreakText" => text.Substring(
                line.Start + line.Length,
                line.LengthIncludingLineBreak - line.Length),
            _ => Default(method.ReturnType),
        });
    }

    private ITextEdit CreateEdit(ITextSnapshot snapshot)
    {
        var changes = new List<EditChange>();

        return Proxy<ITextEdit>((method, args) =>
        {
            switch (method.Name)
            {
                case "Replace":
                    Span replacementSpan = GetSpan(args!);
                    changes.Add(new(replacementSpan.Start, replacementSpan.Length, (string)args!.Last()!));
                    return true;
                case "Delete":
                    Span deletionSpan = GetSpan(args!);
                    changes.Add(new(deletionSpan.Start, deletionSpan.Length, string.Empty));
                    return true;
                case "Insert":
                    changes.Add(new(GetPosition(args![0]), 0, (string)args[1]!));
                    return true;
                case "Apply":
                    foreach (EditChange change in changes.OrderByDescending(change => change.Start))
                    {
                        _text = _text.Remove(change.Start, change.Length).Insert(change.Start, change.Text);
                    }

                    _version++;
                    return CreateSnapshot();
                case "Dispose":
                case "Cancel":
                    return null;
                case "get_Snapshot":
                    return snapshot;
                case "get_HasEffectiveChanges":
                    return changes.Count > 0;
                case "get_Canceled":
                    return false;
                default:
                    return Default(method.ReturnType);
            }
        });
    }

    private static Span GetSpan(object?[] args)
    {
        if (args[0] is Span span)
            return span;
        if (args[0] is SnapshotSpan snapshotSpan)
            return snapshotSpan.Span;
        return new Span(GetPosition(args[0]), (int)args[1]!);
    }

    private static int GetPosition(object? value)
    {
        return value is SnapshotPoint point ? point.Position : (int)value!;
    }

    private static int FindLine(IReadOnlyList<LineInfo> lines, int position)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (position <= lines[i].Start + lines[i].LengthIncludingLineBreak - 1
                || i == lines.Count - 1)
            {
                return i;
            }
        }

        return lines.Count - 1;
    }

    private static IReadOnlyList<LineInfo> GetLines(string text)
    {
        var lines = new List<LineInfo>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
                continue;

            int length = i - start;
            if (length > 0 && text[i - 1] == '\r')
                length--;
            lines.Add(new(start, length, i - start + 1));
            start = i + 1;
        }

        lines.Add(new(start, text.Length - start, text.Length - start));
        return lines;
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        return (T)new InterfaceProxy<T>(handler).GetTransparentProxy();
    }

    private static object? Default(Type type)
    {
        return type == typeof(void) || !type.IsValueType ? null : Activator.CreateInstance(type);
    }

    private sealed class InterfaceProxy<T> : RealProxy where T : class
    {
        private readonly Func<MethodInfo, object?[]?, object?> _handler;

        public InterfaceProxy(Func<MethodInfo, object?[]?, object?> handler)
            : base(typeof(T))
        {
            _handler = handler;
        }

        public override IMessage Invoke(IMessage message)
        {
            var call = (IMethodCallMessage)message;
            var method = (MethodInfo)call.MethodBase;

            try
            {
                object? result = _handler(method, call.Args);
                return new ReturnMessage(result, null, 0, call.LogicalCallContext, call);
            }
            catch (Exception ex)
            {
                return new ReturnMessage(ex, call);
            }
        }
    }

    private sealed record LineInfo(int Start, int Length, int LengthIncludingLineBreak);
    private sealed record EditChange(int Start, int Length, string Text);
}
