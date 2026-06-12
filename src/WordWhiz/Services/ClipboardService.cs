using Windows.ApplicationModel.DataTransfer;

namespace WordWhiz.Services;

/// <summary>
/// Windows clipboard read/write service.
/// </summary>
public class ClipboardService
{
    /// <summary>
    /// Read current clipboard text content.
    /// </summary>
    public async Task<string?> ReadTextAsync()
    {
        try
        {
            var content = Clipboard.GetContent();
            if (content.Contains(StandardDataFormats.Text))
            {
                return await content.GetTextAsync();
            }
        }
        catch (Exception)
        {
            // Clipboard access can fail when app is not in foreground
        }
        return null;
    }

    /// <summary>
    /// Write text to clipboard.
    /// </summary>
    public void WriteText(string text)
    {
        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush(); // Ensure content persists after app exits
        }
        catch (Exception)
        {
            // Clipboard write can fail in certain scenarios
        }
    }

    /// <summary>
    /// Check if clipboard has text content.
    /// </summary>
    public bool HasText()
    {
        try
        {
            return Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }
        catch
        {
            return false;
        }
    }
}
