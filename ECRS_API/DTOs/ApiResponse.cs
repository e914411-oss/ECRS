namespace ECRS_API.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Code { get; set; } = "OK";
        public string Message { get; set; } = "OK";
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "OK")
            => new() { Success = true, Code = "OK", Message = message, Data = data };

        public static ApiResponse<T> Fail(string code, string message)
            => new() { Success = false, Code = code, Message = message, Data = default };

    }
}
