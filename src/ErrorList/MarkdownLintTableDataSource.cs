using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MarkdownLintVS.Linting;
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

                if (_folderLintSnapshot != null)
                {
                    sink.AddSnapshot(_folderLintSnapshot);
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

                // Remove any folder lint errors for this file to avoid duplicates
                RemoveFolderLintErrorsForFile(filePath);
            }
        }

        public void ClearErrors(string filePath)
        {
            if (filePath == null)
            {
                return;
            }
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

                if (_folderLintSnapshot != null)
                {
                    NotifySinks(sink => sink.RemoveSnapshot(_folderLintSnapshot));
                    _folderLintSnapshot = null;
                }
            }
        }

        /// <summary>
        /// Clears all folder lint errors (from Lint Folder command).
        /// </summary>
        public void ClearFolderLintErrors()
        {
            lock (_snapshots)
            {
                if (_folderLintSnapshot != null)
                {
                    NotifySinks(sink => sink.RemoveSnapshot(_folderLintSnapshot));
                    _folderLintSnapshot = null;
                }
            }
        }

        /// <summary>
        /// Adds a folder lint error (from Lint Folder command).
        /// Call ClearFolderLintErrors first, then add all errors, for best performance.
        /// </summary>
        public void AddFolderLintError(
            string filePath,
            int line,
            int startColumn,
            string ruleId,
            string message,
            DiagnosticSeverity severity)
        {
            lock (_snapshots)
            {
                // Get existing errors or create new list
                List<MarkdownLintError> errors;
                if (_folderLintSnapshot != null)
                {
                    // Remove old snapshot, we'll create a new one
                    NotifySinks(sink => sink.RemoveSnapshot(_folderLintSnapshot));
                    errors = [.. _folderLintSnapshot.GetErrors()];
                }
                else
                {
                    errors = [];
                }

                // Add new error
                RuleInfo ruleInfo = Linting.RuleRegistry.GetRule(ruleId);
                errors.Add(new MarkdownLintError(filePath, line, startColumn, ruleId, message, ruleInfo?.Description, ruleInfo?.DocumentationUrl, severity));

                // Create and add new snapshot
                _folderLintSnapshot = new TableEntriesSnapshot(_folderLintPrefix + "Results", errors);
                NotifySinks(sink => sink.AddSnapshot(_folderLintSnapshot));
            }
        }

        /// <summary>
        /// Removes folder lint errors for a specific file.
        /// Called when a file is opened and linted individually to avoid duplicates.
        /// </summary>
        private void RemoveFolderLintErrorsForFile(string filePath)
        {
            // Must be called within lock(_snapshots)
            if (_folderLintSnapshot == null)
                return;

            var existingErrors = _folderLintSnapshot.GetErrors().ToList();
            var filteredErrors = existingErrors
                .Where(e => !string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Only update if we actually removed something
            if (filteredErrors.Count < existingErrors.Count)
            {
                NotifySinks(sink => sink.RemoveSnapshot(_folderLintSnapshot));

                if (filteredErrors.Count > 0)
                {
                    _folderLintSnapshot = new TableEntriesSnapshot(_folderLintPrefix + "Results", filteredErrors);
                    NotifySinks(sink => sink.AddSnapshot(_folderLintSnapshot));
                }
                else
                {
                    _folderLintSnapshot = null;
                }
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

        public IEnumerable<MarkdownLintError> GetErrors() => errors;

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
