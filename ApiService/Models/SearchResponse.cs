namespace ApiService.Models
{
    /// <summary>
    /// Generic response wrapper ใช้ห่อผลลัพธ์จาก service layer ต่าง ๆ
    /// </summary>
    /// <typeparam name="T">ชนิดของ data ที่ห่อ</typeparam>
    public class SearchResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static SearchResponse<T> Ok(T data, string message = null)
        {
            return new SearchResponse<T> { Success = true, Data = data, Message = message };
        }

        public static SearchResponse<T> Fail(string message)
        {
            return new SearchResponse<T> { Success = false, Data = default(T), Message = message };
        }
    }
}
