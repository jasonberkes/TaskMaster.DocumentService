namespace TaskMaster.DocumentService.Processing.Interfaces;

/// <summary>
/// Service interface for processing files from the inbox blob storage container.
/// </summary>
public interface IInboxProcessorService
{
    /// <summary>
    /// Processes files from the inbox container.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of files processed.</returns>
    Task<int> ProcessInboxFilesAsync(CancellationToken cancellationToken = default);
}
