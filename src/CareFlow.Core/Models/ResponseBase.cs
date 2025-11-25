namespace CareFlow.Core.Models
{
    public abstract class ResponseBase
    {
        public bool Success { get; set; }
        public string Message { get; set; }= string.Empty;
    }
}
