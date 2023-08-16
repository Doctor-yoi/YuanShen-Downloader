namespace YS_Downloader.Downloader
{
    public interface IDownloadProgressListener
    {
        void OnDownloadSize(long size);
    }
}
