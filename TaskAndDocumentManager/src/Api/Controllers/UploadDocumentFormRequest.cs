using Microsoft.AspNetCore.Http;

namespace TaskAndDocumentManager.Controllers;

public sealed class UploadDocumentFormRequest
{
    public IFormFile? File{ get; init;}
}