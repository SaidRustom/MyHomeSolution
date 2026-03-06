namespace BlazorUI.Infrastructure.LocalStorage
{
    public class StorageEnvelope<T>
    {
        public T Value { get; set; } = default!;
        public DateTime? ExpireAt { get; set; }
    }

    public class StorageChangedEventArgs : EventArgs
    {
        public string Key { get; init; } = string.Empty;
    }
}
