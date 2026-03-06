using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace BlazorUI.Components.Common;

public partial class ImageCapture : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public string Accept { get; set; } = "image/jpeg,image/png,image/webp";

    [Parameter]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    [Parameter]
    public string UploadText { get; set; } = "Upload Image";

    [Parameter]
    public string CameraText { get; set; } = "Use Camera";

    [Parameter]
    public EventCallback<ImageCaptureResult> OnImageCaptured { get; set; }

    [Parameter]
    public bool ShowPreview { get; set; } = true;

    [Parameter]
    public string? PreviewUrl { get; set; }

    [Parameter]
    public string PreviewStyle { get; set; } = "max-width: 100%; max-height: 300px; border-radius: 8px; object-fit: contain;";

    private string? _previewDataUrl;
    private bool _isCameraActive;
    private bool _isCameraAvailable;
    private string? _errorMessage;
    private string _videoElementId = $"camera-{Guid.NewGuid():N}";
    private InputFile? _fileInput;

    private string? DisplayPreview => _previewDataUrl ?? PreviewUrl;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isCameraAvailable = await JS.InvokeAsync<bool>("CameraInterop.isCameraAvailable");
            StateHasChanged();
        }
    }

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs e)
    {
        _errorMessage = null;

        try
        {
            var file = e.File;

            if (file.Size > MaxFileSizeBytes)
            {
                _errorMessage = $"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.";
                return;
            }

            var buffer = new byte[file.Size];
            await using var stream = file.OpenReadStream(MaxFileSizeBytes);
            var bytesRead = await stream.ReadAsync(buffer);
            var actualBytes = buffer.AsMemory(0, bytesRead);

            _previewDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(actualBytes.ToArray())}";

            await OnImageCaptured.InvokeAsync(new ImageCaptureResult
            {
                Data = actualBytes.ToArray(),
                ContentType = file.ContentType,
                FileName = file.Name
            });
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to read file: {ex.Message}";
        }
    }

    private async Task StartCameraAsync()
    {
        _errorMessage = null;

        var result = await JS.InvokeAsync<CameraResult>("CameraInterop.startCamera", _videoElementId);

        if (result.Success)
        {
            _isCameraActive = true;
        }
        else
        {
            _errorMessage = result.Error ?? "Could not access camera.";
        }
    }

    private async Task CaptureFrameAsync()
    {
        var dataUrl = await JS.InvokeAsync<string?>("CameraInterop.captureFrame", _videoElementId);

        if (dataUrl is null)
        {
            _errorMessage = "Failed to capture image.";
            return;
        }

        _previewDataUrl = dataUrl;

        var base64 = dataUrl[(dataUrl.IndexOf(",", StringComparison.Ordinal) + 1)..];
        var bytes = Convert.FromBase64String(base64);

        await StopCameraInternalAsync();

        await OnImageCaptured.InvokeAsync(new ImageCaptureResult
        {
            Data = bytes,
            ContentType = "image/jpeg",
            FileName = $"capture-{DateTime.UtcNow:yyyyMMddHHmmss}.jpg"
        });
    }

    private async Task StopCameraAsync()
    {
        await StopCameraInternalAsync();
    }

    private async Task StopCameraInternalAsync()
    {
        await JS.InvokeVoidAsync("CameraInterop.stopCamera", _videoElementId);
        _isCameraActive = false;
    }

    private void ClearPreview()
    {
        _previewDataUrl = null;
        _errorMessage = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isCameraActive)
        {
            try
            {
                await JS.InvokeVoidAsync("CameraInterop.stopCamera", _videoElementId);
            }
            catch (JSDisconnectedException) { }
        }
    }

    private sealed record CameraResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
    }
}

public sealed class ImageCaptureResult
{
    public required byte[] Data { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
