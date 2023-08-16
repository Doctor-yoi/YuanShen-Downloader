using System;
using System.Windows;

namespace YS_Downloader
{
    public class ExceptionMessage
    {
        private string exMessage = "An exception occured";
        public ExceptionMessage(Exception ex)
        {
            this.exMessage = ex.Message;
        }
        public void exceptionInfo()
        {
            MessageBox.Show("出现不可预料的错误！", "错误！", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public void exceptionDebug()
        {
#if DEBUG
            MessageBox.Show("出现错误!\n" + exMessage);
#endif
        }
    }
}
