using System.ComponentModel.Composition;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Handles the Save command for Markdown files to apply auto-fixes before saving.
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [Name(nameof(SaveCommandHandler))]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class SaveCommandHandler : ICommandHandler<SaveCommandArgs>
    {
        public string DisplayName => "Markdown Lint Fix on Save";

        public CommandState GetCommandState(SaveCommandArgs args)
        {
            // Return Unspecified to allow the save to proceed through the chain
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(SaveCommandArgs args, CommandExecutionContext executionContext)
        {
            // Check if linting is enabled
            if (!GeneralOptions.Instance.LintingEnabled)
                return false; // Let the next handler process the save

            // Check user preference for fix on save
            FixOnSaveBehavior behavior = GeneralOptions.Instance.FixOnSaveBehavior;

            if (behavior == FixOnSaveBehavior.Ask)
            {
                behavior = PromptUserForBehavior();
            }

            if (behavior == FixOnSaveBehavior.On)
            {
                MarkdownFixApplier.ApplyAllFixes(args.SubjectBuffer);
            }

            // Return false to let the save proceed through the command chain
            // We've made our modifications, now let VS actually save the file
            return false;
        }

        /// <summary>
        /// Prompts the user to choose their preferred fix on save behavior.
        /// </summary>
        private static FixOnSaveBehavior PromptUserForBehavior()
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                // Use YESNOCANCEL: Yes = Enable, No = Disable, Cancel = Skip
                VSConstants.MessageBoxResult result = await VS.MessageBox.ShowAsync(
                    "Markdown Lint - Fix on Save",
                    "Would you like Markdown Lint to automatically fix violations before saving Markdown files?\n\n" +
                    "Click 'Yes' to enable auto-fixing on save (recommended).\n" +
                    "Click 'No' to disable auto-fixing on save.\n" +
                    "Click 'Cancel' to skip this time without saving preference.\n\n" +
                    "You can change this setting later in Tools > Options > Markdown Lint > General.",
                    OLEMSGICON.OLEMSGICON_QUERY,
                    OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL);

                switch (result)
                {
                    case VSConstants.MessageBoxResult.IDYES:
                        GeneralOptions.Instance.FixOnSaveBehavior = FixOnSaveBehavior.On;
                        await GeneralOptions.Instance.SaveAsync();
                        return FixOnSaveBehavior.On;

                    case VSConstants.MessageBoxResult.IDNO:
                        GeneralOptions.Instance.FixOnSaveBehavior = FixOnSaveBehavior.Off;
                        await GeneralOptions.Instance.SaveAsync();
                        return FixOnSaveBehavior.Off;

                    default:
                        // Cancel - don't save, just skip this time
                        return FixOnSaveBehavior.Off;
                }
            });
        }
    }
}
