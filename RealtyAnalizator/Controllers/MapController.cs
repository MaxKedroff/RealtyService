using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace RealtyAnalizator.Controllers
{

    [ApiController]
    [Route("api/v1/map")]
    public class MapController : ControllerBase
    {

        private IMapService _service;

        public MapController(IMapService service)
        {
            _service = service;
        }

        [HttpGet("{cityId}/buildings")]
        public async Task<ActionResult<GetBuildingsResultDTO>> GetBuildings(
            [FromRoute] Guid cityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {

            var buildingsResult = await _service.GetBuildingsAsync(cityId, page, pageSize);

            return Ok(buildingsResult);
        }

        [HttpGet("{cityId}/{buildingsId}/flats")]
        public async Task<ActionResult<IEnumerable<FlatDTO>>> GetFlatsInBuilding(
            [FromRoute] Guid cityId,
            [FromRoute] Guid buildingsId)
        {
            var result = await _service.GetFlatsInBuildingAsync(cityId, buildingsId);
            return Ok(result);
        }

        [HttpPost("search")]
        public async Task<ActionResult<IEnumerable<FlatDTO>>> GetFlatsByFilter([FromBody] FlatFilterDTO filterDTO)
        {
            var result = await _service.GetFlatsByFilter(filterDTO);
            return Ok(result);
        }

        [HttpGet("{cityId}/heatmap")]
        public async Task<ActionResult<HeatMapDto>> BuildHeatMap([FromRoute] Guid cityId)
        {
            var result = await _service.GetHeatMapData(cityId);
            return Ok(result);
        }
    }
}
