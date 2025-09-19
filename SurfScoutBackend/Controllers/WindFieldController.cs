using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SurfScoutBackend.Data;
using SurfScoutBackend.Utilities.GeoJson;
using SurfScoutBackend.Utilities.Gzip;
using SurfScoutBackend.Models.WindFieldModel;
using System.IO.Compression;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/windfields")]
    public class WindFieldController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WindFieldController(AppDbContext context)
        {
            this._context = context;
        }

        [Authorize]
        [HttpGet("windfields")]
        public async Task<IActionResult> ExportWindFieldsForSession([FromQuery] int? sessionId)
        {
            if (sessionId == null || sessionId <= 0)
                return BadRequest("Session ID is not valid!");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var query = _context.sessions
                .Where(s => s.Id == sessionId);

            var sessions = await query.ToListAsync();

            if (!sessions.Any())
                return NotFound($"No sessions found for spot ID '{sessionId}'.");

            // Get wind fields for the session
            List<WindField> windFields = await _context.windfields
                .Include(wf => wf.Points)
                .Where(wf => wf.SessionId == sessionId)
                .ToListAsync();

            return Ok(windFields);
        }

        [Authorize]
        [HttpGet("interpolatedwindfields")]
        public async Task<IActionResult> ExportInterpolatedWindFieldsForSession([FromQuery] int? sessionId)
        {
            if (sessionId == null || sessionId <= 0)
                return BadRequest("Session ID is not valid!");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var query = _context.sessions
                .Where(s => s.Id == sessionId);

            var sessions = await query.ToListAsync();

            if (!sessions.Any())
                return NotFound($"No sessions found for spot ID '{sessionId}'.");

            // Get interpolated wind fields for the session
            List<WindFieldInterpolated> windFields = await _context.windfieldsinterpolated
                .Include(wf => wf.Cells)
                .Where(wf => wf.SessionId == sessionId)
                .ToListAsync();

            // Create GeoJson including cells with metadata(date, time, celsize) and gzip it for export
            var builder = new WindFieldGeoJsonBuilder();
            var compressor = new GzipCompressor();

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var windField in windFields)
                {
                    string geoJson = builder.BuildGeoJson(windField);
                    byte[] compressed = compressor.CompressToGzip(geoJson);

                    string fileName = $"windfield_{windField.Date:yyyyMMdd}_{windField.Timestamp:HHmm}.geojson.gz";

                    var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    entryStream.Write(compressed, 0, compressed.Length);
                }
            }

            zipStream.Position = 0;     // Go to beginning of stream
            string zipFileName = $"windfields_session_{sessionId}.zip";
            return File(zipStream.ToArray(), "application/zip", zipFileName);
        }
    }
}
