using MarkdownLintVS.Options;

namespace MarkdownLintVS.Commands
{
    [Command(PackageIds.EnabledCommand)]
    internal sealed class EnabledCommand : BaseCommand<EnabledCommand>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Checked = GeneralOptions.Instance.LintingEnabled;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            GeneralOptions.Instance.LintingEnabled = !GeneralOptions.Instance.LintingEnabled;
            await GeneralOptions.Instance.SaveAsync();
        }
    }
}
