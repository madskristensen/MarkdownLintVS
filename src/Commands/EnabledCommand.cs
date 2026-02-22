using MarkdownLintVS.Options;

namespace MarkdownLintVS.Commands
{
    [Command(PackageIds.EnabledCommand)]
    internal sealed class EnabledCommand : BaseCommand<EnabledCommand>
    {
        //protected override void BeforeQueryStatus(EventArgs e)
        //{
        //    Command.Checked = GeneralOptions.Instance.LintingEnabled;

        //    ThreadHelper.JoinableTaskFactory.Run(async () =>
        //    {
        //        DocumentView doc = await VS.Documents.GetActiveDocumentViewAsync();
        //        if (doc != null)
        //        {
        //            Command.Visible = doc.TextBuffer.ContentType.IsOfType("markdown") || doc.TextBuffer.ContentType.IsOfType("vs-markdown");
        //        }
        //    });
        //}

        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            GeneralOptions.Instance.LintingEnabled = !GeneralOptions.Instance.LintingEnabled;
            await GeneralOptions.Instance.SaveAsync();
        }
    }
}
