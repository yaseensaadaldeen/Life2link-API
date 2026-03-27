using System.Net;

namespace LifeLink_V2.Helpers
{
    public static class ApiResponseHelper
    {
        public static ApiResponse Success(object data = null, string message = "تمت العملية بنجاح", int statusCode = 200)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = new List<string>()
            };
        }

        public static ApiResponse Error(string errorMessage, int statusCode = 400, List<string> errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = errorMessage,
                Data = null,
                Errors = errors ?? new List<string>()
            };
        }

        public static ApiResponse NotFound(string message = "الموجود غير موجود")
        {
            return Error(message, 404);
        }

        public static ApiResponse Unauthorized(string message = "غير مصرح بالوصول")
        {
            return Error(message, 401);
        }

        public static ApiResponse Forbidden(string message = "ممنوع الوصول")
        {
            return Error(message, 403);
        }

        public static ApiResponse ValidationError(List<string> validationErrors)
        {
            return Error("خطأ في التحقق", 400, validationErrors);
        }

        public static ApiResponse InternalError(string message = "حدث خطأ داخلي في الخادم")
        {
            return Error(message, 500);
        }
    }
}