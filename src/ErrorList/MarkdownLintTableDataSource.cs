using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace MarkdownLintVS.ErrorList
{
    /// <summary>
    /// Table data source for the Error List window.
    /// </summary>
    [Export(typeof(MarkdownLintTableDataSource))]
    public class MarkdownLintTableDataSource : ITableDataSource
    {
        private static MarkdownLintTableDataSource _instance;
        public static MarkdownLintTableDataSource Instance => _instance;

        private readonly List<SinkManager> _managers = [];
        private readonly Dictionary<string, TableEntriesSnapshot> _snapshots =
            new(StringComparer.OrdinalIgnoreCase);

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;
        public string Identifier => "MarkdownLint";
        public string DisplayName => Vsix.Name;

        [ImportingConstructor]
        public MarkdownLintTableDataSource([Import] ITableManagerProvider tableManagerProvider)
        {
            _instance = this;

            ITableManager tableManager = tableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            tableManager.AddSource(this,
                StandardTableColumnDefinitions.Column,
                StandardTableColumnDefinitions.DocumentName,
                StandardTableColumnDefinitions.ErrorCode,
                StandardTableColumnDefinitions.ErrorSeverity,
                StandardTableColumnDefinitions.Line,
                StandardTableColumnDefinitions.Text,
                StandardTableColumnDefinitions.ProjectName);
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            var manager = new SinkManager(this, sink);

            lock (_managers)
            {
                _managers.Add(manager);
            }

            // Send existing snapshots to new sink
            lock (_snapshots)
            {
                foreach (TableEntriesSnapshot snapshot in _snapshots.Values)
                {
                    sink.AddSnapshot(snapshot);
                }
            }

            return manager;
        }

        public void UpdateErrors(string filePath, IEnumerable<Linting.LintViolation> violations)
        {
            var errors = violations.Select(v => new MarkdownLintError(v, filePath)).ToList();

            lock (_snapshots)
            {
                if (_snapshots.TryGetValue(filePath, out TableEntriesSnapshot oldSnapshot))
                {
                    _snapshots.Remove(filePath);
                    NotifySinks(sink => sink.RemoveSnapshot(oldSnapshot));
                }

                if (errors.Count > 0)
                {
                    var snapshot = new TableEntriesSnapshot(filePath, errors);
                    _snapshots[filePath] = snapshot;
                    NotifySinks(sink => sink.AddSnapshot(snapshot));
                }
            }
        }

        public void ClearErrors(string filePath)
        {
            lock (_snapshots)
            {
                if (_snapshots.TryGetValue(filePath, out TableEntriesSnapshot snapshot))
                {
                    _snapshots.Remove(filePath);
                    NotifySinks(sink => sink.RemoveSnapshot(snapshot));
                }
            }
        }

        public void ClearAllErrors()
        {
            lock (_snapshots)
            {
                foreach (TableEntriesSnapshot snapshot in _snapshots.Values)
                {
                    NotifySinks(sink => sink.RemoveSnapshot(snapshot));
                }
                _snapshots.Clear();
            }
        }

        private void NotifySinks(Action<ITableDataSink> action)
        {
            lock (_managers)
            {
                foreach (SinkManager manager in _managers)
                {
                    action(manager.Sink);
                }
            }
        }

        internal void RemoveSinkManager(SinkManager manager)
        {
            lock (_managers)
            {
                _managers.Remove(manager);
            }
        }
    }

    /// <summary>
    /// Manages subscription to the table data sink.
    /// </summary>
    internal class SinkManager(MarkdownLintTableDataSource source, ITableDataSink sink) : IDisposable
    {
        public ITableDataSink Sink { get; } = sink;

        public void Dispose()
        {
            source.RemoveSinkManager(this);
        }
    }

    /// <summary>
    /// Snapshot of error entries for a file.
    /// </summary>
    internal class TableEntriesSnapshot(string filePath, List<MarkdownLintError> errors) : ITableEntriesSnapshot
    {

        public string FilePath { get; } = filePath;
        public int VersionNumber { get; } = 1;
        public int Count => errors.Count;

        public int IndexOf(int currentIndex, ITableEntriesSnapshot newerSnapshot)
        {
            return currentIndex;
        }

        public bool TryGetValue(int index, string keyName, out object content)
        {
            if (index < 0 || index >= errors.Count)
            {
                content = null;
                return false;
            }

            MarkdownLintError error = errors[index];

            switch (keyName)
            {
                case StandardTableKeyNames.DocumentName:
                    content = error.FilePath;
                    return true;

                case StandardTableKeyNames.Line:
                    content = error.Line;
                    return true;

                case StandardTableKeyNames.Column:
                    content = error.Column;
                    return true;

                case StandardTableKeyNames.Text:
                    content = error.Message;
                    return true;

                case StandardTableKeyNames.ErrorCode:
                    content = error.ErrorCode;
                    return true;

                case StandardTableKeyNames.ErrorSeverity:
                    content = error.Severity;
                    return true;

                case StandardTableKeyNames.ErrorCategory:
                    content = "Markdown";
                    return true;

                case StandardTableKeyNames.BuildTool:
                    content = "MarkdownLint";
                    return true;

                case StandardTableKeyNames.HelpLink:
                    content = error.HelpLink;
                    return true;

                case StandardTableKeyNames.ErrorCodeToolTip:
                    content = error.Description;
                    return true;

                default:
                    content = null;
                    return false;
            }
        }

        public void StartCaching()
        {
        }

        public void StopCaching()
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Represents an error in the error list.
    /// </summary>
    internal class MarkdownLintError
    {
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }
        public string Message { get; }
        public string ErrorCode { get; }
        public string Description { get; }
        public string HelpLink { get; }
        public __VSERRORCATEGORY Severity { get; }

        public MarkdownLintError(Linting.LintViolation violation, string filePath)
        {
            FilePath = filePath;
            Line = violation.LineNumber;
            Column = violation.ColumnStart;
            Message = violation.Message;
            ErrorCode = violation.Rule.Id;
            Description = violation.Rule.Description;
            HelpLink = violation.Rule.DocumentationUrl;
            Severity = GetSeverity(violation.Severity);
        }

        private __VSERRORCATEGORY GetSeverity(Linting.DiagnosticSeverity severity)
        {
            return severity switch
            {
                Linting.DiagnosticSeverity.Error => __VSERRORCATEGORY.EC_ERROR,
                Linting.DiagnosticSeverity.Warning => __VSERRORCATEGORY.EC_WARNING,
                _ => __VSERRORCATEGORY.EC_MESSAGE,
            };
        }
    }
}
