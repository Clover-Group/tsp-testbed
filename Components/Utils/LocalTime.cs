using TspTestbed.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TspTestbed.Components.Utils;

public sealed class LocalTime : ComponentBase, IDisposable
{
    [Inject]
    public BrowserTimeProvider TimeProvider { get; set; } = default!;

    [Parameter]
    public DateTime? DateTime { get; set; }

    protected override void OnInitialized()
    {
        TimeProvider.LocalTimeZoneChanged += LocalTimeZoneChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (DateTime != null)
        {
            builder.AddContent(0, TimeProvider.ToLocalDateTime(DateTime.Value).ToString("dd.MM.yyyy HH:mm:ss"));
        }
    }

    public void Dispose()
    {
        TimeProvider.LocalTimeZoneChanged -= LocalTimeZoneChanged;
    }

    private void LocalTimeZoneChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }
}