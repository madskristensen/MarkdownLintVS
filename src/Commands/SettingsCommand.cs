namespace MarkdownLintVS.Commands
{
    [Command(PackageIds.SettingsCommand)]
    internal sealed class SettingsCommand : BaseCommand<SettingsCommand>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await VS.Settings.OpenAsync<Options.OptionsProvider.GeneralOptionsPage>();
        }
    }
}
