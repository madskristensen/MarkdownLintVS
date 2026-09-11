using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.ComponentModelHost;
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

        // Separate storage for folder lint results (keyed by "FolderLint:" prefix)
        private const string _folderLintPrefix = "FolderLint:";
        private TableEntriesSnapshot _folderLintSnapshot;
        private readonly List<MarkdownLintError> _folderLintErrors = [];

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;
        public string Identifier => "MarkdownLint";
        public string DisplayName => Vsix.Name;

        /// <summary>
        /// Ensures the data source singleton is initialized.
        /// Call this from commands that need the data source before any document is opened.
        /// </summary>
        public static async System.Threading.Tasks.Task<MarkdownLintTableDataSource> EnsureInitializedAsync()
        {
            if (_instance != null)
            {
                return _instance;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Get the component model to trigger MEF composition
            IComponentModel componentModel = await VS.GetServiceAsync<SComponentModel, IComponentModel>();
            if (componentModel != null)
            {
                // This will trigger MEF to create the instance
                return componentModel.GetService<MarkdownLintTableDataSource>();
            }

            return null;
        }

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
            List<TableEntriesSnapshot> snapshots;

            lock (_snapshots)
            {
                snapshots = [.. _snapshots.Values];
                if (_folderLintSnapshot != null)
                {
                    snapshots.Add(_folderLintSnapshot);
                }
            }

            lock (_managers)
            {
                _managers.Add(manager);
            }

            // Send existing snapshots without holding either storage lock.
            foreach (TableEntriesSnapshot snapshot in snapshots)
            {
                sink.AddSnapshot(snapshot);
            }

            return manager;
        }

        public void UpdateErrors(string filePath, IEnumerable<Linting.LintViolation> violations)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            violations ??= [];

            var errors = violations.Select(v => new MarkdownLintError(v, filePath)).ToList();

            var removedSnapshots = new List<TableEntriesSnapshot>();
            var addedSnapshots = new List<TableEntriesSnapshot>();

            lock (_snapshots)
            {
                if (_snapshots.TryGetValue(filePath, out TableEntriesSnapshot oldSnapshot))
                {
                    _snapshots.Remove(filePath);
                    removedSnapshots.Add(oldSnapshot);
                }

                if (errors.Count > 0)
                {
                    var snapshot = new TableEntriesSnapshot(filePath, errors);
                    _snapshots[filePath] = snapshot;
                    addedSnapshots.Add(snapshot);
                }

                // Remove any folder lint errors for this file to avoid duplicates.
                RemoveFolderLintErrorsForFile(filePath, removedSnapshots, addedSnapshots);
            }

            NotifySnapshots(removedSnapshots, addedSnapshots);
        }

        public void ClearErrors(string filePath)
        {
            if (filePath == null)
            {
                return;
            }
            TableEntriesSnapshot snapshot = null;
            lock (_snapshots)
            {
                if (_snapshots.TryGetValue(filePath, out snapshot))
                {
                    _snapshots.Remove(filePath);
                }
            }

            if (snapshot != null)
            {
                NotifySinks(sink => sink.RemoveSnapshot(snapshot));
            }
        }

        public void ClearAllErrors()
        {
            List<TableEntriesSnapshot> removedSnapshots;
            lock (_snapshots)
            {
                removedSnapshots = [.. _snapshots.Values];
                _snapshots.Clear();

                if (_folderLintSnapshot != null)
                {
                    removedSnapshots.Add(_folderLintSnapshot);
                    _folderLintSnapshot = null;
                }

                _folderLintErrors.Clear();
            }

            NotifySnapshots(removedSnapshots, []);
        }

        /// <summary>
        /// Clears all folder lint errors (from Lint Folder command).
        /// </summary>
        public void ClearFolderLintErrors()
        {
            TableEntriesSnapshot removedSnapshot = null;
            lock (_snapshots)
            {
                if (_folderLintSnapshot != null)
                {
                    removedSnapshot = _folderLintSnapshot;
                    _folderLintSnapshot = null;
                }

                _folderLintErrors.Clear();
            }

            if (removedSnapshot != null)
            {
                NotifySinks(sink => sink.RemoveSnapshot(removedSnapshot));
            }
        }

        /// <summary>
        /// Adds multiple folder lint errors in a single batch operation.
        /// Much more efficient than calling AddFolderLintError repeatedly.
        /// </summary>
        public void AddFolderLintErrors(IEnumerable<(string FilePath, int Line, int StartColumn, string RuleId, string Message, DiagnosticSeverity Severity)> errors)
        {
            TableEntriesSnapshot removedSnapshot = null;
            lock (_snapshots)
            {
                if (_folderLintSnapshot != null)
                {
                    removedSnapshot = _folderLintSnapshot;
                    _folderLintSnapshot = null;
                }

                _folderLintErrors.Clear();
            }

            if (removedSnapshot != null)
            {
                NotifySinks(sink => sink.RemoveSnapshot(removedSnapshot));
            }

            AppendFolderLintErrors(errors);
        }

        /// <summary>
        /// Appends folder lint errors to the current folder-lint snapshot.
        /// </summary>
        public void AppendFolderLintErrors(IEnumerable<(string FilePath, int Line, int StartColumn, string RuleId, string Message, DiagnosticSeverity Severity)> errors)
        {
            TableEntriesSnapshot removedSnapshot = null;
            TableEntriesSnapshot addedSnapshot = null;
            lock (_snapshots)
            {
                var errorList = new List<MarkdownLintError>();
                foreach ((var FilePath, var Line, var StartColumn, var RuleId, var Message, DiagnosticSeverity Severity) in errors)
                {
                    if (!ErrorListDeduplication.ShouldIncludeFolderLintError(FilePath, _snapshots.Keys))
                        continue;

                    RuleInfo ruleInfo = RuleRegistry.GetRule(RuleId);
                    errorList.Add(new MarkdownLintError(
                        FilePath,
                        Line,
                        StartColumn,
                        RuleId,
                        Message,
                        ruleInfo?.Description,
                        ruleInfo?.DocumentationUrl,
                        Severity));
                }

                if (errorList.Count > 0)
                {
                    removedSnapshot = _folderLintSnapshot;
                    _folderLintErrors.AddRange(errorList);
                    _folderLintSnapshot = new TableEntriesSnapshot(_folderLintPrefix + "Results", _folderLintErrors);
                    addedSnapshot = _folderLintSnapshot;
                }
            }

            if (removedSnapshot != null)
            {
                NotifySinks(sink => sink.RemoveSnapshot(removedSnapshot));
            }

            if (addedSnapshot != null)
            {
                NotifySinks(sink => sink.AddSnapshot(addedSnapshot));
            }
        }

        /// <summary>
        /// Adds a folder lint error (from Lint Folder command).
        /// For bulk operations, prefer AddFolderLintErrors for better performance.
        /// </summary>
        public void AddFolderLintError(
            string filePath,
            int line,
            int startColumn,
            string ruleId,
            string message,
            DiagnosticSeverity severity)
        {
            TableEntriesSnapshot removedSnapshot;
            TableEntriesSnapshot addedSnapshot;
            lock (_snapshots)
            {
                if (!ErrorListDeduplication.ShouldIncludeFolderLintError(filePath, _snapshots.Keys))
                    return;

                // Get existing errors or create new list.
                List<MarkdownLintError> errors = _folderLintSnapshot == null
                    ? []
                    : [.. _folderLintSnapshot.GetErrors()];

                removedSnapshot = _folderLintSnapshot;
                RuleInfo ruleInfo = Linting.RuleRegistry.GetRule(ruleId);
                errors.Add(new MarkdownLintError(filePath, line, startColumn, ruleId, message, ruleInfo?.Description, ruleInfo?.DocumentationUrl, severity));

                _folderLintSnapshot = new TableEntriesSnapshot(_folderLintPrefix + "Results", errors);
                addedSnapshot = _folderLintSnapshot;
            }

            if (removedSnapshot != null)
            {
                NotifySinks(sink => sink.RemoveSnapshot(removedSnapshot));
            }

            NotifySinks(sink => sink.AddSnapshot(addedSnapshot));
        }

        /// <summary>
        /// Removes folder lint errors for a specific file.
        /// Called when a file is opened and linted individually to avoid duplicates.
        /// </summary>
        private void RemoveFolderLintErrorsForFile(
            string filePath,
            List<TableEntriesSnapshot> removedSnapshots,
            List<TableEntriesSnapshot> addedSnapshots)
        {
            // Must be called within lock(_snapshots).
            if (_folderLintSnapshot == null)
                return;

            var filteredErrors = _folderLintErrors
                .Where(e => !string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Only update if we actually removed something
            if (filteredErrors.Count < _folderLintErrors.Count)
            {
                removedSnapshots.Add(_folderLintSnapshot);
                _folderLintErrors.Clear();
                _folderLintErrors.AddRange(filteredErrors);

                if (_folderLintErrors.Count > 0)
                {
                    _folderLintSnapshot = new TableEntriesSnapshot(_folderLintPrefix + "Results", _folderLintErrors);
                    addedSnapshots.Add(_folderLintSnapshot);
                }
                else
                {
                    _folderLintSnapshot = null;
                }
            }
        }

        private void NotifySnapshots(
            IEnumerable<TableEntriesSnapshot> removedSnapshots,
            IEnumerable<TableEntriesSnapshot> addedSnapshots)
        {
            foreach (TableEntriesSnapshot snapshot in removedSnapshots)
            {
                NotifySinks(sink => sink.RemoveSnapshot(snapshot));
            }

            foreach (TableEntriesSnapshot snapshot in addedSnapshots)
            {
                NotifySinks(sink => sink.AddSnapshot(snapshot));
            }
        }

        private void NotifySinks(Action<ITableDataSink> action)
        {
            SinkManager[] managers;
            lock (_managers)
            {
                managers = [.. _managers];
            }

            foreach (SinkManager manager in managers)
            {
                action(manager.Sink);
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
    internal class TableEntriesSnapshot : ITableEntriesSnapshot
    {
        private readonly IReadOnlyList<MarkdownLintError> _errors;

        public TableEntriesSnapshot(string filePath, IEnumerable<MarkdownLintError> errors)
        {
            FilePath = filePath;
            _errors = errors?.ToArray() ?? [];
        }

        public string FilePath { get; }
        public int VersionNumber { get; } = 1;
        public int Count => _errors.Count;

        public IEnumerable<MarkdownLintError> GetErrors() => _errors;

        public int IndexOf(int currentIndex, ITableEntriesSnapshot newerSnapshot)
        {
            return currentIndex;
        }

        public bool TryGetValue(int index, string keyName, out object content)
        {
            if (index < 0 || index >= _errors.Count)
            {
                content = null;
                return false;
            }

            MarkdownLintError error = _errors[index];

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

        public MarkdownLintError(
            string filePath,
            int line,
            int column,
            string errorCode,
            string message,
            string description,
            string helpLink,
            Linting.DiagnosticSeverity severity)
        {
            FilePath = filePath;
            Line = line;
            Column = column;
            ErrorCode = errorCode;
            Message = message;
            Description = description ?? "";
            HelpLink = helpLink ?? "";
            Severity = GetSeverity(severity);
        }

        private static __VSERRORCATEGORY GetSeverity(Linting.DiagnosticSeverity severity)
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
