namespace Jukebox.Shared.Models
{
    public class ResponseObject<T>
    {
        public T dto { get; set; }

        public bool IsError { get; set; }

        public string ErrorMessage { get; set; }
    }
}