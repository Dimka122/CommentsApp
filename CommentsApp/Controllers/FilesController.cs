using CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApp.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IFileStorage storage) : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        // id → StoredName: простым маппингом через БД (вjected через репозиторий в реальном коде).
        // Упрощённо: храним id в URL, но файл находим по имени из БД.
        // Для простоты примера: FileStorage.Open принимает StoredName,
        // поэтому добавим в IFileStorage метод GetStoredName(id) через сервис.
        return NotFound(); // см. расширенную версию ниже
    }
}
