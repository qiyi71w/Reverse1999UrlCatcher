using System.Runtime.InteropServices;
using System.Windows;

namespace Reverse1999UrlCatcher.App.Services;

public sealed class ClipboardService
{
    public async Task SetTextAsync(string value, CancellationToken cancellationToken = default)
    {
        const int clipbrdCantOpen = unchecked((int)0x800401D0);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(value);
                return;
            }
            catch (COMException ex) when (ex.HResult == clipbrdCantOpen && attempt < 4)
            {
                await Task.Delay(80, cancellationToken);
            }
        }

        throw new InvalidOperationException("剪贴板当前被占用，请稍后重试。");
    }
}
