using Fig.Api.Attributes;
using Fig.Api.DataImport;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers;

[ApiController]
[Route("data")]
public class DataController : ControllerBase
{
    private readonly IImportExportService _importExportService;

    public DataController(IImportExportService importExportService)
    {
        _importExportService = importExportService;
    }

    [Authorize(Role.Administrator)]
    [HttpGet]
    public IActionResult GetExport([FromQuery] bool excludeSecrets)
    {
        var export = _importExportService.Export(excludeSecrets);
        return Ok(export);
    }

    [Authorize(Role.Administrator)]
    [HttpPut]
    public async Task<IActionResult> SubmitImport([FromBody] FigDataExportDataContract? data)
    {
        var result = await _importExportService.Import(data, ImportMode.Api);
        return Ok(result);
    }
}