using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;

namespace RealtyAnalizator.Controllers
{
    [Route("api/v1/saved/")]
    [ApiController]
    public class SavedController : ControllerBase
    {

        private IPolygonService _service;

        public SavedController(IPolygonService service)
        {
            _service = service;
        }

        [HttpGet("polygons")]
        public async Task<ActionResult<SavedPolygonsDto>> GetSavedPolygons()
        {
            try
            {
                var result = await _service.GetAllSavedPolygonsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("polygons/{id}")]
        public async Task<ActionResult<PolygonDto>> GetPolygonById(Guid id)
        {
            try
            {
                var polygons = await _service.GetAllSavedPolygonsAsync();
                var polygon = polygons.SavedPolygons.FirstOrDefault(p => p.id == id);

                if (polygon == null)
                {
                    return NotFound($"Polygon with id {id} not found");
                }

                return Ok(polygon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("polygons")]
        public async Task<ActionResult<PolygonDto>> CreatePolygon([FromBody] CreateUpdatePolygonDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return BadRequest("Polygon data is null");
                }

                if (string.IsNullOrEmpty(createDto.GeoPoints))
                {
                    return BadRequest("GeoPoints are required");
                }

                var result = await _service.CreateNewPolygonAsync(createDto);
                return CreatedAtAction(nameof(GetPolygonById), new { id = result.id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("polygons/{id}")]
        public async Task<ActionResult<PolygonDto>> UpdatePolygon(Guid id, [FromBody] CreateUpdatePolygonDto updateDto)
        {
            try
            {
                if (updateDto == null)
                {
                    return BadRequest("Update data is null");
                }

                var result = await _service.UpdatePolygonAsync(id, updateDto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("polygons/{id}")]
        public async Task<ActionResult> DeletePolygon(Guid id)
        {
            try
            {
                var result = await _service.DeletePolygonAsync(id);

                if (!result)
                {
                    return NotFound($"Polygon with id {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
