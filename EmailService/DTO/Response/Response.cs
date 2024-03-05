using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;

namespace EmailService.DTO.Response
{
    public class Response<T>
    {
        public Response()
        {
        }
        // Successful response
        public Response(T data, string message = "Success")
        {
            Succeeded = true;
            Code = 200;
            Message = message;
            Data = data;
        }

        // Successful response
        public Response(string message, int code, bool succeeded)
        {
            Succeeded = succeeded;
            Code = code;
            Message = message;
        }

        public Response(string message, bool succeeded, int code, List<string> errors = null)
        {
            Succeeded = succeeded;
            Code = code;
            Message = message;
            Errors = errors;
        }

        public Response(string message, bool succeeded, int code)
        {
            Succeeded = succeeded;
            Code = code;
            Message = message;
            Errors = new List<string>();
        }

        [JsonProperty("succeeded")]
        public bool Succeeded { get; set; }
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("errors")]
        public List<string> Errors { get; set; }
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
